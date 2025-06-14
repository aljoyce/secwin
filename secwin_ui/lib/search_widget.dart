import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:secwin_ui/search_results_widget.dart';
import 'package:secwin_ui/main.dart';

class Search extends StatefulWidget {
  const Search({super.key, required this.connected});

  final bool connected;

  @override
  State<Search> createState() => _SearchState();
}

class _SearchState extends State<Search> {
  final _formKey = GlobalKey<FormState>();
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    _searchController.text = "Search";
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  String? _validateSearch(String? value) {
    if (value == null || value.isEmpty) {
      return 'A search term is required';
    }
    return null;
  }

  void _handleSearch(MyAppState appState) {
    if (_formKey.currentState!.validate()) {
      appState.runSearch(_searchController.text);

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Search for "${_searchController.text}"')),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    var appState = context.watch<MyAppState>();

    return Container(
      child: !widget.connected
          ? const Text("Please connect to the server")
          : Form(
              key: _formKey,
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Expanded(
                        flex: 8,
                        child: Padding(
                          padding: const EdgeInsets.all(20.0),
                          child: TextFormField(
                            controller: _searchController,
                            enabled: widget.connected && !appState.isLoading,
                            decoration: const InputDecoration(
                              labelText: 'Search',
                              hintText: 'Enter a search term',
                              border: OutlineInputBorder(),
                              isDense: true,
                            ),
                            validator: _validateSearch,
                          ),
                        ),
                      ),
                      Expanded(
                        flex: 2,
                        child: Padding(
                          padding: const EdgeInsets.all(20.0),
                          child: ElevatedButton.icon(
                            onPressed: appState.isLoading
                                ? null
                                : () => _handleSearch(appState),
                            label: Text("Search"),
                            icon: Icon(Icons.search),
                          ),
                        ),
                      ),
                    ],
                  ),
                  SearchResults(searchResults: appState.searchResults),
                ],
              ),
            ),
    );
  }
}
