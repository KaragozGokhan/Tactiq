import 'dart:async';
import 'dart:convert';
import 'dart:io';

File get _sessionFile => File('${Directory.systemTemp.path}${Platform.pathSeparator}tactiq_session.json');

Future<dynamic> platformRequest(Uri uri, {required String method, String? token, Object? body}) async {
  final client = HttpClient()..connectionTimeout = const Duration(seconds: 5);
  try {
    final request = await client.openUrl(method, uri).timeout(const Duration(seconds: 8));
    request.headers.contentType = ContentType.json;
    if (token != null) request.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token');
    if (body != null) request.write(body is String ? body : jsonEncode(body));

    final response = await request.close().timeout(const Duration(seconds: 15));
    final text = await response.transform(utf8.decoder).join().timeout(const Duration(seconds: 15));
    final data = _decode(text);
    if (response.statusCode < 200 || response.statusCode >= 300) throw data ?? 'HTTP ${response.statusCode}';
    return data;
  } finally {
    client.close(force: true);
  }
}

Future<Map<String, dynamic>?> loadSessionData() async {
  if (!await _sessionFile.exists()) return null;
  final data = jsonDecode(await _sessionFile.readAsString());
  return data is Map<String, dynamic> ? data : null;
}

Future<void> saveSessionData(Map<String, dynamic> data) => _sessionFile.writeAsString(jsonEncode(data));

Future<void> clearSessionData() async {
  try {
    await _sessionFile.delete();
  } catch (_) {}
}

dynamic _decode(String text) {
  if (text.isEmpty) return null;
  try {
    return jsonDecode(text);
  } catch (_) {
    return text;
  }
}
