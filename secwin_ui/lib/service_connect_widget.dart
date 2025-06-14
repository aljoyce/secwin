import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:secwin_ui/main.dart';

class ServiceConnect extends StatefulWidget {
  const ServiceConnect({super.key, required this.connected});

  final bool connected;

  @override
  State<ServiceConnect> createState() => _ServiceConnectState();
}

class _ServiceConnectState extends State<ServiceConnect> with ChangeNotifier {
  final _formKey = GlobalKey<FormState>();
  final _serverController = TextEditingController();
  final _portController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _serverController.text = "127.0.0.1";
    _portController.text = "42042";
  }

  @override
  void dispose() {
    _serverController.dispose();
    _portController.dispose();
    super.dispose();
  }

  String? _validateServer(String? value) {
    if (value == null || value.isEmpty) {
      return 'Server is required';
    }
    // Basic server validation (can be IP or hostname)
    if (value.length < 3) {
      return 'Server name too short';
    }
    return null;
  }

  String? _validatePort(String? value) {
    if (value == null || value.isEmpty) {
      return 'Port is required';
    }
    final port = int.tryParse(value);
    if (port == null) {
      return 'Port must be a number';
    }
    if (port < 1 || port > 65535) {
      return 'Port must be 1-65535';
    }
    return null;
  }

  Future<void> _handleConnect(MyAppState appState) async {
    if (_formKey.currentState!.validate()) {
      appState.socketHost = _serverController.text;
      appState.socketPort = int.parse(_portController.text);

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            widget.connected
                ? 'Disconnecting from ${_serverController.text}:${_portController.text}'
                : 'Connecting to ${_serverController.text}:${_portController.text}',
          ),
        ),
      );

      await appState.toggleServiceConnection();

      if (appState.socketError.isNotEmpty) {
        if (mounted) {
          ScaffoldMessenger.of(context).hideCurrentSnackBar();

          ScaffoldMessenger.of(
            context,
          ).showSnackBar(SnackBar(content: Text(appState.socketError)));
        }
      }

      if (appState.socketConnectMessage.isNotEmpty) {
        if (mounted) {
          ScaffoldMessenger.of(context).hideCurrentSnackBar();

          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(appState.socketConnectMessage)),
          );
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    IconData connectionIcon;
    String connectedText;
    Color? connectedBackgroundColor;

    var appState = context.watch<MyAppState>();

    if (widget.connected == true) {
      connectionIcon = Icons.add_box_outlined;
      connectedText = "Disconnect";
      connectedBackgroundColor = Colors.red[900];
    } else {
      connectionIcon = Icons.add_box;
      connectedText = "Connect";
      connectedBackgroundColor = Colors.green[900];
    }

    return Container(
      padding: const EdgeInsets.all(0),
      margin: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        border: Border.all(color: Colors.black, width: 2),
      ),
      child: Form(
        key: _formKey,
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Expanded(
              flex: 5,
              child: Padding(
                padding: const EdgeInsets.all(10.0),
                child: TextFormField(
                  controller: _serverController,
                  enabled: !widget.connected && !appState.isLoading,
                  decoration: const InputDecoration(
                    labelText: 'Server',
                    hintText: 'localhost or IP address',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  validator: _validateServer,
                ),
              ),
            ),
            Expanded(
              flex: 2,
              child: Padding(
                padding: const EdgeInsets.all(10.0),
                child: TextFormField(
                  controller: _portController,
                  enabled: !widget.connected && !appState.isLoading,
                  decoration: const InputDecoration(
                    labelText: 'Port',
                    hintText: '42042',
                    border: OutlineInputBorder(),
                    isDense: true,
                  ),
                  keyboardType: TextInputType.number,
                  validator: _validatePort,
                ),
              ),
            ),
            Flexible(
              flex: 3,
              child: Padding(
                padding: const EdgeInsets.all(10.0),
                child: ElevatedButton.icon(
                  onPressed: appState.isLoading
                      ? null
                      : () => _handleConnect(appState),
                  icon: Icon(connectionIcon),
                  label: Text(connectedText),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: connectedBackgroundColor,
                    foregroundColor: Colors.white,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
