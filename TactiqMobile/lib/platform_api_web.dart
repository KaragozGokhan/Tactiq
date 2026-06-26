import 'dart:async';
import 'dart:convert';
import 'dart:html' as html;

const _sessionKey = 'tactiq_session';

Future<dynamic> platformRequest(Uri uri, {required String method, String? token, Object? body}) async {
  final headers = {'Content-Type': 'application/json'};
  if (token != null) headers['Authorization'] = 'Bearer $token';

  final response = await html.HttpRequest.request(
    uri.toString(),
    method: method,
    requestHeaders: headers,
    sendData: body == null ? null : (body is String ? body : jsonEncode(body)),
  ).timeout(const Duration(seconds: 15));

  final status = response.status ?? 0;
  final data = _decode(response.responseText ?? '');
  if (status < 200 || status >= 300) throw data ?? 'HTTP $status';
  return data;
}

Future<Map<String, dynamic>?> loadSessionData() async {
  final value = html.window.localStorage[_sessionKey];
  if (value == null) return null;
  final data = jsonDecode(value);
  return data is Map<String, dynamic> ? data : null;
}

Future<void> saveSessionData(Map<String, dynamic> data) async {
  html.window.localStorage[_sessionKey] = jsonEncode(data);
}

Future<void> clearSessionData() async {
  html.window.localStorage.remove(_sessionKey);
}

dynamic _decode(String text) {
  if (text.isEmpty) return null;
  try {
    return jsonDecode(text);
  } catch (_) {
    return text;
  }
}
