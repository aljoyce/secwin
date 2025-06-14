import 'dart:developer' as developer;
import 'dart:io';
import 'package:flutter/foundation.dart';

enum LogLevel { debug, info, warning, error }

class LoggerService {
  static final LoggerService _instance = LoggerService._internal();
  factory LoggerService() => _instance;
  LoggerService._internal();

  // Enable/disable logging based on build mode
  bool get _isLoggingEnabled => kDebugMode;

  void debug(String message, {String? tag, Object? error, StackTrace? stackTrace}) {
    _log(LogLevel.debug, message, tag: tag, error: error, stackTrace: stackTrace);
  }

  void info(String message, {String? tag, Object? error, StackTrace? stackTrace}) {
    _log(LogLevel.info, message, tag: tag, error: error, stackTrace: stackTrace);
  }

  void warning(String message, {String? tag, Object? error, StackTrace? stackTrace}) {
    _log(LogLevel.warning, message, tag: tag, error: error, stackTrace: stackTrace);
  }

  void error(String message, {String? tag, Object? error, StackTrace? stackTrace}) {
    _log(LogLevel.error, message, tag: tag, error: error, stackTrace: stackTrace);
  }

  void _log(
    LogLevel level,
    String message, {
    String? tag,
    Object? error,
    StackTrace? stackTrace,
  }) {
    if (!_isLoggingEnabled) return;

    final timestamp = DateTime.now().toIso8601String();
    final levelStr = level.toString().split('.').last.toUpperCase();
    final tagStr = tag != null ? '[$tag] ' : '';
    
    final logMessage = '$timestamp [$levelStr] $tagStr$message';

    switch (level) {
      case LogLevel.debug:
        developer.log(logMessage, name: tag ?? 'DEBUG');
      case LogLevel.info:
        developer.log(logMessage, name: tag ?? 'INFO');
      case LogLevel.warning:
        developer.log(logMessage, name: tag ?? 'WARNING');
      case LogLevel.error:
        developer.log(
          logMessage,
          name: tag ?? 'ERROR',
          error: error,
          stackTrace: stackTrace,
        );
    }

    // Optional: Write to file in production
    if (kReleaseMode) {
      _writeToFile(logMessage, error, stackTrace);
    }
  }

  Future<void> _writeToFile(String message, Object? error, StackTrace? stackTrace) async {
    try {
      // TODO Use path_provider for proper file paths
      final file = File('app_logs.txt');
      await file.writeAsString(
        '$message\n${error != null ? 'Error: $error\n' : ''}${stackTrace != null ? 'StackTrace: $stackTrace\n' : ''}\n',
        mode: FileMode.append,
      );
    } catch (e) {
      // Silently fail - don't log errors in the logger itself
    }
  }
}

// Global logger instance
final logger = LoggerService();
