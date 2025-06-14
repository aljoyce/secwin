import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:secwin_ui/main.dart';
import 'package:secwin_ui/search_widget.dart';
import 'package:secwin_ui/service_connect_widget.dart';

class SearchPage extends StatelessWidget {
  @override
  Widget build(BuildContext context) {

    var appState = context.watch<MyAppState>();

    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.start,
        children: [
          ServiceConnect(connected: appState.connected),
          SizedBox(height: 10),
          Search(connected: appState.connected),
        ],
      ),
    );
  }
}
