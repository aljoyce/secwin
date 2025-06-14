import 'dart:io';
import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter/services.dart' show rootBundle;
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:secwin_ui/logger.dart';
import 'package:secwin_ui/search_page.dart';
//import 'package:secwin_ui/monitor_page.dart';
import 'package:secwin_ui/search_results_widget.dart';

// Have the app mimize to the system tray
import 'package:window_manager/window_manager.dart';
import 'package:system_tray/system_tray.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  if (Platform.isWindows || Platform.isMacOS || Platform.isLinux) {
    await windowManager.ensureInitialized();
    logger.debug("Window manager is initialized");

    WindowOptions windowOptions = WindowOptions(
      size: Size(1200, 600),
      center: true,
      backgroundColor: Colors.transparent,
      skipTaskbar: false,
      titleBarStyle: TitleBarStyle.hidden,
    );

    windowManager.waitUntilReadyToShow(windowOptions, () async {
      await windowManager.show();
    });
  }

  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (context) => MyAppState(),
      child: MaterialApp(
        title: 'SecWin',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(seedColor: Colors.lightBlue),
        ),
        home: Consumer<MyAppState>(
          builder: (context, appState, child) {
            return Stack(
              children: [
                MyHomePage(),
                if (appState.isLoading)
                  Container(
                    color: Colors.black54,
                    child: Center(child: CircularProgressIndicator()),
                  ),
              ],
            );
          },
        ),
      ),
    );
  }
}

class MyAppState extends ChangeNotifier {
  // State of the connection to the windows service
  var connected = false;

  // Lisf of search results returned from the windows service
  List<String> searchResults = List.empty(growable: true);

  // Flag to display the loading indicator and lock the screen
  var isLoading = false;

  // The socket to connect to the windows service
  String? socketUrl;
  String? socketHost;
  int socketPort = 0;
  String socketError = "";
  String socketConnectMessage = "";

  SecureSocket? socket;

  // Files other than images have to be loaded to be used
  Future<File> writeAssetToTempFile(String assetPath) async {
    final ByteData data = await rootBundle.load(assetPath);
    final Uint8List bytes = data.buffer.asUint8List();
    final tempDir = await Directory.systemTemp.createTemp();
    final file = File('${tempDir.path}/${assetPath.split('/').last}');
    await file.writeAsBytes(bytes, flush: true);
    return file;
  }

  Future<void> _connect() async {
    try {
      socketError = "";
      socketConnectMessage = "";

      await Future.delayed(Duration(seconds: 2));

      final certFile = await writeAssetToTempFile('assets/certs/secwin-cert.pem');
      final keyFile = await writeAssetToTempFile('assets/certs/secwin-key.pem');

      String certPassword = 'secwin123';
      SecurityContext context = SecurityContext();
      context.useCertificateChain(certFile.path);
      context.usePrivateKey(keyFile.path, password: certPassword);

      socket = await SecureSocket.connect(
                        socketHost,
                        socketPort,
                        context: context,
                        onBadCertificate: (X509Certificate cert) {
                          logger.warning("Using self signed certificate");
                          return true;
                        },
                      );

      connected = true;

      socket?.listen(
        (data) {
          // Received data
          String jsonString = utf8.decode(data);
          Map<String, dynamic> messageMap = jsonDecode(jsonString);

          if (messageMap['Type'] == "CONNECTION") {
            socketConnectMessage = messageMap['Message'];
          } else {
            searchResults.add(messageMap['Message']);

            SearchResults(searchResults: searchResults);
            isLoading = false;
            notifyListeners();
          }
        },
        onError: (error) {
          searchResults.add("ERROR: ${error.toString()}");
        },
        onDone: () {},
      );
    } catch (error) {
      connected = false;
      logger.error("Error occurred");

      socketError =
          "Connection Failed, please ensure the service is running and available";
    }
  }

  Future<void> _disconnect() async {
    await Future.delayed(Duration(seconds: 2));

    if (socket != null) {
      socket!.close();
      socket = null;
    }

    socketConnectMessage = "You disconnected from the secwin service";
    connected = false;
  }

  Future<void> toggleServiceConnection() async {
    isLoading = true;
    notifyListeners();

    if (connected) {
      searchResults.clear();
      await _disconnect();
    } else {
      await _connect();
    }

    isLoading = false;
    notifyListeners();
  }

  Future<void> runSearch(String searchTerm) async {
    isLoading = true;
    notifyListeners();

    searchResults.clear();

    await Future.delayed(Duration(seconds: 2));

    if (socket != null) {
      var data = utf8.encode(searchTerm);
      socket!.add(data);
    }

    // isLoading = false;
    notifyListeners();
  }
}

class MyHomePage extends StatefulWidget {
  @override
  State<MyHomePage> createState() => _MyHomePageState();
}

class _MyHomePageState extends State<MyHomePage> with WindowListener {
  // System Tray - Start
  final SystemTray _systemTray = SystemTray();
  final Menu _menuMain = Menu();

  bool _systemTrayInitialized = false;

  @override
  void initState() {
    super.initState();
    if (Platform.isWindows || Platform.isMacOS) {
      windowManager.addListener(this);
      _initSystemTray();
      logger.debug("System tray initialized");
    }
  }

  @override
  void dispose() {
    if (Platform.isWindows || Platform.isMacOS) {
      windowManager.removeListener(this);
    }
    super.dispose();
  }

  @override
  void onWindowClose() async {
    bool isPreventClose = await windowManager.isPreventClose();
    logger.debug(
      "Window close event - prevent close: $isPreventClose, tray initialized: $_systemTrayInitialized",
    );

    if (isPreventClose && _systemTrayInitialized) {
      logger.debug("Preventing window close, hiding window to system tray");
      await windowManager.hide();
    } else {
      logger.debug("Allowing app to close");
      _exitApp();
    }
  }

  Future<void> _initSystemTray() async {
    try {
      logger.debug("Create system tray icon path");
      String iconPath = Platform.isWindows
          ? 'assets/icons/app_icon.ico'
          : 'assets/icons/app_icon.png';

      logger.debug("Initialize system tray");
      await _systemTray.initSystemTray(
        iconPath: iconPath,
        toolTip: "SecWin is running",
      );

      logger.debug("Create tray menu");
      await _menuMain.buildFrom([
        MenuItemLabel(
          label: 'Show Window',
          onClicked: (menuItem) => _showWindow(),
        ),
        MenuSeparator(),
        MenuItemLabel(
          label: 'Settings',
          onClicked: (menuItem) => _showSettings(),
        ),
        MenuSeparator(),
        MenuItemLabel(label: 'Exit', onClicked: (menuItem) => _exitApp()),
      ]);

      logger.debug("Set the menu");
      await _systemTray.setContextMenu(_menuMain);

      logger.debug("Handle tray icon click");
      _systemTray.registerSystemTrayEventHandler((eventName) {
        logger.debug("System tray event: $eventName");
        if (eventName == kSystemTrayEventClick) {
          _showWindow();
        } else if (eventName == kSystemTrayEventRightClick) {
          _systemTray.popUpContextMenu();
        }
      });

      await windowManager.setPreventClose(true);
      _systemTrayInitialized = true;
      logger.debug("System tray initialized successfully");
    } catch (error) {
      logger.error("Failed to initialize system tray: $error");
      _systemTrayInitialized = false;
      await windowManager.setPreventClose(false);
    }
  }

  void _showWindow() async {
    logger.debug("Showing window");
    await windowManager.show();
    await windowManager.focus();
  }

  void _showSettings() {
    // Show settings dialog or navigate to settings page
    _showWindow();

    if (mounted) {
      logger.debug("Context mounted and showing dialog box");
      showDialog(
        context: context,
        builder: (context) => AlertDialog(
          title: Text('Settings'),
          content: Text('Settings would go here'),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('Close'),
            ),
          ],
        ),
      );
    }
  }

  void _exitApp() async {
    logger.debug("Exiting app, destroying tray and window");
    await _systemTray.destroy();
    await windowManager.destroy();
  }

  void _minimizeToTray() async {
    if (!_systemTrayInitialized) {
      print('System tray not initialized, cannot minimize');
      return;
    }

    if (Platform.isWindows) {
      if (mounted) {
        logger.debug("Notification that app is still running");
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('App minimized to system tray'),
            duration: Duration(seconds: 2),
          ),
        );
      }
    }

    logger.debug("Minimizing to tray");
    await windowManager.hide();
  }
  // System Tray - End

  var selectedIndex = 0;

  @override
  Widget build(BuildContext context) {
    Widget page;
    switch (selectedIndex) {
      case 0:
        page = SearchPage();
      // Removing this page because it's broken and I don't have time to investigate why
      // Hire me and I'll figure it out ;-)
      // case 1:
      //   page = MonitorPage();
      default:
        throw UnimplementedError('no widget for $selectedIndex');
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        return Scaffold(
          appBar: AppBar(
            title: Text("SecWin"),
            actions: [
              IconButton(
                onPressed: _minimizeToTray,
                icon: Icon(Icons.minimize),
                tooltip: "Minimize to system trap",
              ),
            ],
          ),
          body: Row(
            children: [
              SafeArea(
                child: NavigationRail(
                  extended: constraints.maxWidth >= 600,
                  destinations: [
                    NavigationRailDestination(
                      icon: Icon(Icons.home),
                      label: Text('Search'),
                    ),
                    // NavigationRailDestination(
                    //   icon: Icon(Icons.monitor),
                    //   label: Text('Monitor'),
                    // ),
                  ],
                  selectedIndex: selectedIndex,
                  onDestinationSelected: (value) {
                    setState(() {
                      selectedIndex = value;
                    });
                  },
                ),
              ),

              Expanded(
                child: Container(
                  color: Theme.of(context).colorScheme.primaryContainer,
                  child: page,
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}
