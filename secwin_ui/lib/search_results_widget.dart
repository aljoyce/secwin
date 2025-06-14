import 'package:flutter/material.dart';
import 'package:secwin_ui/search_result_item_widget.dart';

class SearchResults extends StatelessWidget {
  const SearchResults({super.key, required this.searchResults});

  final List<String> searchResults;

  @override
  Widget build(BuildContext context) {
    return Container(
      child: searchResults.isEmpty
          ? const Center(child: Text("No Results"))
          : Padding(
              padding: const EdgeInsets.all(20.0),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  for (var result in searchResults)
                    SearchResultItem(data: result),
                ],
              ),
            ),
    );
  }
}
