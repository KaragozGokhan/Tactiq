import 'dart:async';
import 'dart:convert';
import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';

import 'platform_api.dart';

void main() => runApp(const TactiqApp());

class TactiqApp extends StatelessWidget {
  const TactiqApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Tactiq',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff0f766e), brightness: Brightness.dark),
        useMaterial3: true,
      ),
      home: const HomePage(),
    );
  }
}

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  final apiBase = TextEditingController(text: defaultTargetPlatform == TargetPlatform.android ? 'http://10.0.2.2:5000/api' : 'http://localhost:5000/api');
  final username = TextEditingController(text: 'admin');
  final email = TextEditingController(text: 'admin@tactiq.test');
  final password = TextEditingController(text: 'Pass123!');

  final playerName = TextEditingController();
  final playerPosition = TextEditingController();
  final strongFoot = TextEditingController();
  final height = TextEditingController();
  final weight = TextEditingController();
  final playstyles = TextEditingController();
  final pace = TextEditingController();
  final shoot = TextEditingController();
  final pass = TextEditingController();
  final dribbling = TextEditingController();
  final def = TextEditingController();
  final phy = TextEditingController();

  final homeScore = TextEditingController();
  final awayScore = TextEditingController();
  final duration = TextEditingController();
  final formation = TextEditingController(text: '2-3-1');
  final bulkJson = TextEditingController();
  final playerSearch = TextEditingController();

  String? token;
  String role = 'User';
  String output = 'Ready';
  bool showOutput = true;
  bool showAdvanced = false;
  int tab = 0;
  int resultTeamTab = 0;
  int teamSize = 7;
  int? editingPlayerId;
  final players = <Map<String, dynamic>>[];
  final matches = <Map<String, dynamic>>[];
  final selected = <int>{};
  final slotPlayerIds = List<int?>.filled(11, null);
  final statInputs = <int, Map<String, TextEditingController>>{};
  Map<String, dynamic>? balanced;
  Map<String, dynamic>? teamAnalysis;
  String playerSearchTerm = '';

  bool get loggedIn => token != null;
  bool get isAdmin => role == 'Admin';

  @override
  void initState() {
    super.initState();
    loadSession();
  }

  Future<void> loadSession() async {
    try {
      final data = await loadSessionData();
      if (data == null) return;
      final savedApiBase = data['apiBase'] as String?;
      await clearSessionData();
      setState(() {
        token = null;
        role = 'User';
        tab = 0;
        apiBase.text = savedApiBase ?? apiBase.text;
      });
      await saveSession();
    } catch (_) {}
  }

  Future<void> saveSession() async {
    await saveSessionData({'apiBase': apiBase.text});
  }

  Future<dynamic> api(String path, {String method = 'GET', Object? body}) async {
    final uri = Uri.parse('${apiBase.text.replaceAll(RegExp(r"/$"), "")}$path');
    return platformRequest(uri, method: method, token: token, body: body);
  }

  Future<void> run(String label, Future<dynamic> Function() action) async {
    setState(() {
      showOutput = true;
      output = '$label running...';
    });
    try {
      final data = await action();
      setState(() => output = '$label OK\n${pretty(data)}');
    } catch (e) {
      final message = errorMessage(e);
      setState(() => output = '$label ERROR\n$message');
      if (mounted) await showError(message);
    }
  }

  Future<void> showError(Object message) {
    return showDialog<void>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Hata'),
        content: SelectableText('$message'),
        actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('OK'))],
      ),
    );
  }

  String errorMessage(Object error) {
    if (error is Map && error['message'] != null) return '${error['message']}';
    return '$error';
  }

  Future<Map<String, dynamic>> login({bool registerFirst = false}) async {
    if (registerFirst) {
      try {
        await api('/auth/register', method: 'POST', body: {'username': username.text, 'email': email.text, 'password': password.text});
      } catch (_) {}
    }
    final data = await api('/auth/login', method: 'POST', body: {'email': email.text, 'password': password.text}) as Map<String, dynamic>;
    final user = data['user'] as Map<String, dynamic>?;
    token = data['token'] as String;
    role = email.text == 'admin@tactiq.test' ? 'Admin' : ((user?['role'] as String?) ?? 'User');
    tab = 0;
    await saveSession();
    await loadPlayers(silent: true);
    await loadMatches(silent: true);
    return {'loggedIn': true, 'role': role};
  }

  Future<List<Map<String, dynamic>>> loadPlayers({bool silent = false}) async {
    final data = await api('/players') as List<dynamic>;
    final list = data.cast<Map<String, dynamic>>();
    setState(() {
      players
        ..clear()
        ..addAll(list);
      selected.removeWhere((id) => players.every((player) => player['id'] != id));
      for (var i = 0; i < slotPlayerIds.length; i++) {
        if (slotPlayerIds[i] != null && players.every((player) => player['id'] != slotPlayerIds[i])) slotPlayerIds[i] = null;
      }
      if (!silent) output = 'Loaded ${players.length} players';
    });
    return list;
  }

  Future<List<Map<String, dynamic>>> loadMatches({bool silent = false}) async {
    final data = await api('/matches') as List<dynamic>;
    final list = data.cast<Map<String, dynamic>>();
    setState(() {
      matches
        ..clear()
        ..addAll(list);
      if (!silent) output = 'Loaded ${matches.length} matches';
    });
    return list;
  }

  @override
  Widget build(BuildContext context) {
    if (!loggedIn) return loginScreen();
    final destinations = [
      const NavigationDestination(icon: Icon(Icons.sports_soccer), label: 'Kadro Kur'),
      const NavigationDestination(icon: Icon(Icons.upload_file), label: 'Maç Yükle'),
      const NavigationDestination(icon: Icon(Icons.groups), label: 'Oyuncular'),
      if (isAdmin) const NavigationDestination(icon: Icon(Icons.admin_panel_settings), label: 'Admin'),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Tactiq'),
        actions: [TextButton(onPressed: logout, child: const Text('Logout'))],
      ),
      body: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          if (tab == 0) squadBuilder(),
          if (tab == 1) matchUpload(),
          if (tab == 2) playersScreen(),
          if (tab == 3 && isAdmin) adminTools(),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: min(tab, destinations.length - 1),
        onDestinationSelected: (i) => setState(() {
          tab = i;
          if (i == 1) unawaited(loadMatches(silent: true));
          if (i == 2) {
            selected.clear();
            clearSlots();
          }
        }),
        destinations: destinations,
      ),
    );
  }

  Widget loginScreen() {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 430),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(18),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Text('Tactiq', textAlign: TextAlign.center, style: Theme.of(context).textTheme.displayMedium?.copyWith(fontWeight: FontWeight.w900)),
            const Text('Login, kadro kur, maç istatistiği gir.'),
            const SizedBox(height: 22),
            input(username, 'Username'),
            input(email, 'Email'),
            input(password, 'Password', obscure: true),
            buttons([
              ('Admin test', () {
                username.text = 'admin';
                email.text = 'admin@tactiq.test';
                password.text = 'Pass123!';
                return run('Admin login', () => login(registerFirst: true));
              }),
              ('Login', () => run('Login', () => login())),
            ]),
            ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: const Text('Connection'),
              children: [input(apiBase, 'API Base'), adminApiButtons()],
            ),
                  ]),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget squadBuilder() {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      section('Kadro Kur', [
        Row(children: [
          Expanded(
            child: DropdownButtonFormField<int>(
              initialValue: teamSize,
              decoration: const InputDecoration(labelText: 'Team size'),
              items: [6, 7, 8, 9, 10, 11].map((v) => DropdownMenuItem(value: v, child: Text('${v}v$v'))).toList(),
              onChanged: (v) => setState(() {
                teamSize = v ?? 7;
                formation.text = formationOptions(teamSize).first;
                balanced = null;
                teamAnalysis = null;
                clearSlots();
              }),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(child: input(formation, 'Diziliş')),
        ]),
        formationChips(),
        const SizedBox(height: 10),
        pitch(),
        const SizedBox(height: 10),
        buttons([
          ('Oyuncu Ekle', () => setState(() {
                tab = 2;
                selected.clear();
                clearSlots();
              })),
          ('Oyuncu Seç', () => run('Load players', () => loadPlayers())),
        ]),
        playerPicker(compact: true),
        FilledButton.icon(
          icon: const Icon(Icons.auto_awesome),
          label: const Text('AI Build Balanced Squad'),
          onPressed: () => run('Balance', buildBalancedSquad),
        ),
        if (balanced != null) resultBoard(),
      ]),
      Card(
        child: ExpansionTile(
          initiallyExpanded: false,
          title: const Text('Kadro Output'),
          childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
          children: [
            if (teamAnalysis != null) analysisBox(teamAnalysis!),
            SelectableText(output, maxLines: 10),
          ],
        ),
      ),
    ]);
  }

  Widget matchUpload() {
    return section('Maç Yükle', [
      ExpansionTile(
        initiallyExpanded: false,
        tilePadding: EdgeInsets.zero,
        title: const Text('Kayıtlı Maçlar'),
        subtitle: Text('${matches.length} maç'),
        children: [
          buttons([('Maçları Yükle', () => run('Load matches', () => loadMatches()))]),
          SizedBox(
            height: 220,
            child: ListView.builder(
              itemCount: matches.length,
              itemBuilder: (_, i) => matchTile(matches[i]),
            ),
          ),
        ],
      ),
      ExpansionTile(
        initiallyExpanded: false,
        tilePadding: EdgeInsets.zero,
        title: const Text('Yeni Maç Yükle'),
        children: [
      const Text('Seçili oyuncular için maç sonu istatistiklerini gir. Bu veriler form ve sonraki kadro dengesini besler.'),
      const SizedBox(height: 8),
      Row(children: [
        Expanded(child: input(homeScore, 'Home')),
        const SizedBox(width: 8),
        Expanded(child: input(awayScore, 'Away')),
        const SizedBox(width: 8),
        Expanded(child: input(duration, 'Süre')),
      ]),
      playerPicker(compact: true),
      for (final player in selectedPlayers) statRow(player),
      const SizedBox(height: 8),
      FilledButton.icon(
        icon: const Icon(Icons.cloud_upload),
        label: const Text('Upload Match Stats'),
        onPressed: () => run('Upload match', uploadDetailedMatch),
      ),
        ],
      ),
      if (isAdmin) advancedJson(),
    ]);
  }

  Widget playersScreen() {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      playerForm(),
      Card(child: ExpansionTile(initiallyExpanded: false, title: const Text('Oyuncu Listesi'), subtitle: Text('${players.length} oyuncu'), childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12), children: [
        searchBox(),
        buttons([
          ('Yenile', () => run('Oyuncuları yenile', () => loadPlayers())),
          ('Seçimi Kaldır', () => setState(() {
                selected.clear();
                clearSlots();
              })),
          ('Seçili Olanları Sil', () => run('Seçili oyuncuları sil', deleteSelectedPlayers)),
        ]),
        SizedBox(
          height: 430,
          child: ListView.builder(
            itemCount: filteredPlayers.length,
            itemBuilder: (_, i) => playerTile(filteredPlayers[i]),
          ),
        ),
      ])),
    ]);
  }

  Widget playerForm() {
    final isEditing = editingPlayerId != null;
    return Card(
      child: ExpansionTile(
        key: ValueKey(editingPlayerId),
        initiallyExpanded: isEditing,
        title: Text(isEditing ? 'Oyuncu Güncelle' : 'Oyuncu Ekle'),
        childrenPadding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
        children: [
      input(playerName, 'İsim'),
      Row(children: [
        Expanded(child: input(playerPosition, 'Mevki')),
        const SizedBox(width: 8),
        Expanded(child: input(strongFoot, 'Ayak')),
      ]),
      Row(children: [
        Expanded(child: input(height, 'Boy')),
        const SizedBox(width: 8),
        Expanded(child: input(weight, 'Kilo')),
      ]),
      input(playstyles, 'Playstyles'),
      Wrap(spacing: 8, runSpacing: 8, children: [
        tinyInput(pace, 'PAC'),
        tinyInput(shoot, 'SHO'),
        tinyInput(pass, 'PAS'),
        tinyInput(dribbling, 'DRI'),
        tinyInput(def, 'DEF'),
        tinyInput(phy, 'PHY'),
      ]),
      const SizedBox(height: 10),
      FilledButton.icon(
        icon: Icon(isEditing ? Icons.save : Icons.person_add),
        label: Text(isEditing ? 'Oyuncu Güncelle' : 'Oyuncu Ekle'),
        onPressed: () => run(isEditing ? 'Update player' : 'Create player', savePlayer),
      ),
      if (isEditing) TextButton(onPressed: () => setState(clearPlayerForm), child: const Text('Vazgeç')),
        ],
      ),
    );
  }

  Widget adminTools() {
    return section('Admin / Debug', [
      outputBox(),
      input(apiBase, 'API Base'),
      adminApiButtons(),
      buttons([
        ('Ping API', () => run('Ping API', () => api('/health'))),
        ('Reload Players', () => run('Reload players', () => loadPlayers())),
        ('5 Maça Tamamla', () => run('Seed matches', seedRecentMatches)),
      ]),
      SwitchListTile(
        value: showAdvanced,
        onChanged: (v) => setState(() => showAdvanced = v),
        title: const Text('Show bulk/debug JSON'),
      ),
      if (showAdvanced) TextField(controller: bulkJson, minLines: 8, maxLines: 12, decoration: const InputDecoration(border: OutlineInputBorder())),
      if (showAdvanced) FilledButton(onPressed: () => run('DB players JSON', loadPlayersJson), child: const Text('DB Oyuncularını JSON’a Al')),
      if (showAdvanced) FilledButton(onPressed: () => run('Bulk create', () => api('/players', method: 'POST', body: bulkJson.text)), child: const Text('Bulk Create Players')),
    ]);
  }

  Widget outputBox() {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Row(children: [
            const Expanded(child: Text('Output', style: TextStyle(fontWeight: FontWeight.w700))),
            TextButton(onPressed: () => setState(() => showOutput = !showOutput), child: Text(showOutput ? 'Hide' : 'Show')),
          ]),
          if (showOutput) SelectableText(output, maxLines: 10),
        ]),
      ),
    );
  }

  Widget loginOutput() {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      TextButton(onPressed: () => setState(() => showOutput = !showOutput), child: Text(showOutput ? 'Hide output' : 'Show output')),
      if (showOutput) SelectableText(output, maxLines: 6),
    ]);
  }

  Widget section(String title, List<Widget> children) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text(title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
          const SizedBox(height: 10),
          ...children,
        ]),
      ),
    );
  }

Widget pitch() {
  final hasResult = balanced != null;
  final shownTeamSize = hasResult ? int.tryParse('${balanced!['teamSize']}') ?? teamSize : teamSize;
  final shownFormation = hasResult ? '${balanced!['formation'] ?? formation.text}' : formation.text;
  final slots = formationSlots(shownFormation, shownTeamSize);
  final roles = formationSlotRoles(shownFormation, shownTeamSize);
  final picked = slotPlayers;
  final resultTeam = hasResult ? (balanced![resultTeamTab == 0 ? 'teamA' : 'teamB'] as List).cast<Map<String, dynamic>>() : null;
  final resultPlayers = resultTeam == null ? null : arrangeForFormation(resultTeam, roles);
  return LayoutBuilder(
    builder: (_, constraints) {
      final width = min(constraints.maxWidth, 520.0);
      return Center(
        child: SizedBox(
          width: width,
          child: AspectRatio(
            aspectRatio: .82,
            child: Container(
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(12),
                gradient: const LinearGradient(
                  colors: [Color(0xff0b3d2e), Color(0xff115e46)],
                ),
              ),
              child: CustomPaint(
                painter: PitchPainter(),
                child: Stack(
                  children: [
                    if (hasResult)
                      Align(
                        alignment: const Alignment(0, -.98),
                        child: Text(resultTeamTab == 0 ? 'Team A' : 'Team B', style: const TextStyle(fontWeight: FontWeight.w900)),
                      ),
                    for (var i = 0; i < slots.length; i++)
                      Align(
                        alignment: slots[i],
                        child: playerCard(
                          resultPlayers == null ? picked[i] : (i < resultPlayers.length ? resultPlayers[i] : null),
                          i,
                          roles[i],
                          tappable: !hasResult,
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ),
        ),
      );
    },
  );
}

  Widget playerCard(Map<String, dynamic>? player, int index, String role, {bool tappable = true}) {
    final score = player == null ? '${index + 1}' : '${player['overall'] ?? player['powerScore'] ?? ''}';
    final realRole = normalizePosition(player?['position']);
    final roleText = player == null
        ? (tappable ? 'Tap' : slotLabel(role))
        : (realRole == role ? '${player['position']}' : '${player['position']} > ${slotLabel(role)}');
    return InkWell(
      onTap: tappable ? () => pickForSlot(index, role) : null,
      borderRadius: BorderRadius.circular(8),
      child: Container(
        width: 68,
        padding: const EdgeInsets.all(5),
        decoration: BoxDecoration(
          color: player == null ? const Color(0xff164e63) : const Color(0xfffff7cc),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: const Color(0xff22d3ee), width: 1.4),
        ),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          Text(score, style: TextStyle(color: player == null ? Colors.white70 : Colors.black, fontWeight: FontWeight.w900, fontSize: 12)),
          Text(player == null ? slotLabel(role) : '${player['name']}', maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: player == null ? Colors.white70 : Colors.black, fontSize: 9)),
          Text(roleText, maxLines: 1, overflow: TextOverflow.ellipsis, style: TextStyle(color: player == null ? Colors.white54 : Colors.black54, fontSize: 9)),
        ]),
      ),
    );
  }

  Widget resultBoard() {
    final a = (balanced!['teamA'] as List).cast<Map<String, dynamic>>();
    final b = (balanced!['teamB'] as List).cast<Map<String, dynamic>>();
    final team = resultTeamTab == 0 ? a : b;
    return Container(
      margin: const EdgeInsets.only(top: 10),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(color: const Color(0xff07111f), borderRadius: BorderRadius.circular(10)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
        Text('Balance ${balanced!['balancePercentage']}% | ${balanced!['formation'] ?? formation.text}'),
        const SizedBox(height: 8),
        SegmentedButton<int>(
          segments: const [
            ButtonSegment(value: 0, label: Text('Team A')),
            ButtonSegment(value: 1, label: Text('Team B')),
          ],
          selected: {resultTeamTab},
          onSelectionChanged: (value) => setState(() => resultTeamTab = value.first),
        ),
        const SizedBox(height: 8),
        teamColumn(resultTeamTab == 0 ? 'Team A' : 'Team B', team),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          icon: const Icon(Icons.visibility),
          label: const Text('Kadro Önizle'),
          onPressed: showSquadPreview,
        ),
      ]),
    );
  }

  Widget teamColumn(String title, List<Map<String, dynamic>> team) {
    return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
      Text(title, textAlign: TextAlign.center, style: const TextStyle(fontWeight: FontWeight.w800)),
      for (final p in team)
        Container(
          margin: const EdgeInsets.only(top: 5),
          padding: const EdgeInsets.all(7),
          decoration: BoxDecoration(color: const Color(0xff12233a), borderRadius: BorderRadius.circular(7)),
          child: Text('${p['name']}  ${p['position']}  ${num.parse('${p['powerScore']}').toStringAsFixed(1)}', maxLines: 1, overflow: TextOverflow.ellipsis),
      ),
    ]);
  }

  List<Map<String, dynamic>> arrangeForFormation(List<Map<String, dynamic>> team, List<String> roles) {
    final remaining = [...team];
    return [
      for (final role in roles.take(team.length))
        remaining.removeAt(max(0, remaining.indexWhere((player) => roleMatches(player, role))))
    ];
  }

  Future<void> showSquadPreview() async {
    if (balanced == null) return;
    final a = (balanced!['teamA'] as List).cast<Map<String, dynamic>>();
    final b = (balanced!['teamB'] as List).cast<Map<String, dynamic>>();
    await showDialog<void>(
      context: context,
      builder: (_) => Dialog(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 760),
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(12),
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              Row(children: [
                const Expanded(child: Text('Kadro Önizleme', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 18))),
                IconButton(onPressed: () => Navigator.pop(context), icon: const Icon(Icons.close)),
              ]),
              combinedPreviewPitch(a, b),
            ]),
          ),
        ),
      ),
    );
  }

  Widget combinedPreviewPitch(List<Map<String, dynamic>> a, List<Map<String, dynamic>> b) {
    final activeFormation = '${balanced?['formation'] ?? formation.text}';
    final teamLength = max(1, max(a.length, b.length));
    final slots = formationSlots(activeFormation, teamLength);
    final roles = formationSlotRoles(activeFormation, teamLength);
    final arrangedA = arrangeForFormation(a, roles);
    final arrangedB = arrangeForFormation(b, roles);
    Alignment halfSlot(Alignment slot, bool top) => Alignment(slot.x * .92, top ? -.5 - slot.y * .42 : .5 + slot.y * .42);

    return SizedBox(
      height: 520,
      child: Container(
        decoration: BoxDecoration(borderRadius: BorderRadius.circular(12), gradient: const LinearGradient(colors: [Color(0xff064e3b), Color(0xff052e24)])),
        child: CustomPaint(
          painter: PitchPainter(),
          child: Stack(children: [
            const Align(alignment: Alignment(0, -.96), child: Text('Team A', style: TextStyle(fontWeight: FontWeight.w900))),
            const Align(alignment: Alignment(0, .96), child: Text('Team B', style: TextStyle(fontWeight: FontWeight.w900))),
            for (var i = 0; i < min(arrangedA.length, slots.length); i++)
              Align(alignment: halfSlot(slots[i], true), child: previewCard(arrangedA[i], const Color(0xfffff7cc), roles[i])),
            for (var i = 0; i < min(arrangedB.length, slots.length); i++)
              Align(alignment: halfSlot(slots[i], false), child: previewCard(arrangedB[i], const Color(0xffdbeafe), roles[i])),
          ]),
        ),
      ),
    );
  }

  Widget previewCard(Map<String, dynamic> player, Color color, String assignedRole) {
    final realRole = normalizePosition(player['position']);
    final roleText = realRole == assignedRole ? '${player['position']}' : '${player['position']} > ${slotLabel(assignedRole)}';
    return Container(
      width: 74,
      padding: const EdgeInsets.all(5),
      decoration: BoxDecoration(color: color, borderRadius: BorderRadius.circular(8), border: Border.all(color: Colors.white70)),
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Text('${player['overall'] ?? player['powerScore'] ?? ''}', style: const TextStyle(color: Colors.black, fontWeight: FontWeight.w900, fontSize: 12)),
        Text('${player['name']}', maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(color: Colors.black, fontSize: 9)),
        Text(roleText, maxLines: 1, overflow: TextOverflow.ellipsis, style: const TextStyle(color: Colors.black54, fontSize: 9)),
      ]),
    );
  }

  Widget analysisBox(Map<String, dynamic> analysis) {
    return Container(
      margin: const EdgeInsets.only(top: 10),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(color: const Color(0xff10251f), borderRadius: BorderRadius.circular(10)),
      child: Text('${analysis['summary']}\n${((analysis['suggestions'] as List?) ?? []).join('\n')}'),
    );
  }

  Widget playerPicker({bool compact = false}) {
    return ExpansionTile(
      initiallyExpanded: false,
      tilePadding: EdgeInsets.zero,
      title: const Text('Oyuncular'),
      subtitle: Text('${selected.length} seçili | ${filteredPlayers.length} listede'),
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text('Hedef: ${teamSize * 2} oyuncu | Seçili: ${selected.length}'),
        ),
        Padding(
          padding: const EdgeInsets.only(bottom: 8),
          child: Text(positionCountText(), style: const TextStyle(fontSize: 12)),
        ),
        searchBox(),
        buttons([
          ('Yükle', () => run('Load players', () => loadPlayers())),
          ('Hepsini Seç', () => setState(() {
                for (final player in filteredPlayers) {
                  if (selected.length >= teamSize * 2) break;
                  selected.add(player['id'] as int);
                }
                output = 'Selected ${selected.length} players';
              })),
          ('Seçimi Kaldır', () => setState(() {
                selected.clear();
                clearSlots();
              })),
        ]),
        SizedBox(
          height: compact ? 260 : 420,
          child: ListView.builder(
            itemCount: filteredPlayers.length,
            itemBuilder: (_, i) => playerTile(filteredPlayers[i], compact: compact),
          ),
        ),
      ],
    );
  }

  Widget searchBox() {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: TextField(
        controller: playerSearch,
        onChanged: (value) => setState(() => playerSearchTerm = value.trim().toLowerCase()),
        decoration: InputDecoration(
          labelText: 'Oyuncu ara',
          prefixIcon: const Icon(Icons.search),
          suffixIcon: IconButton(
            icon: const Icon(Icons.search),
            onPressed: () => setState(() => playerSearchTerm = playerSearch.text.trim().toLowerCase()),
          ),
          border: const OutlineInputBorder(),
        ),
      ),
    );
  }

  Widget playerTile(Map<String, dynamic> p, {bool compact = false}) {
    return CheckboxListTile(
      dense: compact,
      value: selected.contains(p['id']),
      onChanged: (v) => setState(() => v == true ? selectPlayer(p) : unselectPlayer(p['id'] as int)),
      secondary: IconButton(icon: const Icon(Icons.edit), onPressed: () => setState(() => editPlayer(p))),
      title: Text('#${p['id']} ${p['name']}'),
      subtitle: Text('${p['position']} | OVR ${p['overall']} | FORM ${p['form']} | ${(p['playstyles'] as List).join(', ')}', maxLines: 1, overflow: TextOverflow.ellipsis),
    );
  }

  Widget matchTile(Map<String, dynamic> match) {
    final players = (match['players'] as List?) ?? [];
    return ListTile(
      title: Text('#${match['id']}  ${match['homeScore']}-${match['awayScore']}'),
      subtitle: Text('${match['matchDate']} | ${players.length} oyuncu', maxLines: 1, overflow: TextOverflow.ellipsis),
      onTap: () => showDialog<void>(
        context: context,
        builder: (_) => AlertDialog(
          title: Text('Maç #${match['id']}'),
          content: SingleChildScrollView(child: SelectableText(pretty(match))),
          actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('OK'))],
        ),
      ),
    );
  }

  Widget statRow(Map<String, dynamic> player) {
    final id = player['id'] as int;
    final stats = statsFor(id);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(8),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text('${player['name']} (${player['position']})', style: const TextStyle(fontWeight: FontWeight.w700)),
          Wrap(spacing: 8, runSpacing: 8, children: [
            tinyInput(stats['rating']!, 'RTG'),
            tinyInput(stats['goals']!, 'G'),
            tinyInput(stats['assists']!, 'A'),
            tinyInput(stats['shotsOnTarget']!, 'SOT'),
            tinyInput(stats['successfulPasses']!, 'PAS'),
            tinyInput(stats['tackles']!, 'TCK'),
            tinyInput(stats['saves']!, 'SAV'),
          ]),
        ]),
      ),
    );
  }

  Widget advancedJson() {
    final jsonText = pretty(buildDetailedMatch());
    return ExpansionTile(
      title: const Text('Advanced JSON'),
      children: [SelectableText(jsonText)],
    );
  }

  Widget formationChips() {
    return Wrap(
      spacing: 8,
      children: [
        for (final f in formationOptions(teamSize))
          ChoiceChip(label: Text(f), selected: formation.text == f, onSelected: (_) => setState(() {
                formation.text = f;
                balanced = null;
                teamAnalysis = null;
                clearSlots();
              })),
      ],
    );
  }

  Widget adminApiButtons() {
    return buttons([
      ('USB', () => setApiBase('http://127.0.0.1:5000/api')),
      ('Emulator', () => setApiBase('http://10.0.2.2:5000/api')),
      ('WiFi', () => setApiBase('http://192.168.1.109:5000/api')),
    ]);
  }

  Widget input(TextEditingController controller, String label, {bool obscure = false}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: TextField(controller: controller, obscureText: obscure, decoration: InputDecoration(labelText: label, border: const OutlineInputBorder())),
    );
  }

  Widget tinyInput(TextEditingController controller, String label) {
    return SizedBox(
      width: 72,
      child: TextField(controller: controller, keyboardType: TextInputType.number, decoration: InputDecoration(labelText: label, border: const OutlineInputBorder())),
    );
  }

  Widget buttons(List<(String, FutureOr<void> Function())> items) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(children: [
        for (final item in items)
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(right: 8),
              child: FilledButton(onPressed: () => Future.sync(item.$2), child: Text(item.$1, overflow: TextOverflow.ellipsis)),
            ),
          ),
      ]),
    );
  }

  Future<Map<String, dynamic>> createPlayer() async {
    final body = [buildPlayer()];
    final created = await api('/players', method: 'POST', body: body);
    if (created is List) {
      for (final player in created.whereType<Map<String, dynamic>>()) {
        selectPlayer(player);
        assignToFirstSlot(player);
      }
    }
    clearPlayerForm();
    await loadPlayers(silent: true);
    return {'created': created};
  }

  Future<Map<String, dynamic>> savePlayer() {
    return editingPlayerId == null ? createPlayer() : updatePlayer();
  }

  Future<Map<String, dynamic>> updatePlayer() async {
    final id = editingPlayerId;
    if (id == null) return {'updated': false};
    final updated = await api('/players/$id', method: 'PUT', body: buildPlayer()) as Map<String, dynamic>;
    clearPlayerForm();
    await loadPlayers(silent: true);
    return {'updated': updated};
  }

  void editPlayer(Map<String, dynamic> player) {
    editingPlayerId = player['id'] as int;
    playerName.text = '${player['name'] ?? ''}';
    playerPosition.text = '${player['position'] ?? ''}';
    strongFoot.text = '${player['strongFoot'] ?? ''}';
    height.text = '${player['height'] ?? ''}';
    weight.text = '${player['weight'] ?? ''}';
    playstyles.text = ((player['playstyles'] as List?) ?? []).join(', ');
    pace.text = '${player['pace'] ?? ''}';
    shoot.text = '${player['shoot'] ?? ''}';
    pass.text = '${player['pass'] ?? ''}';
    dribbling.text = '${player['dribbling'] ?? ''}';
    def.text = '${player['def'] ?? ''}';
    phy.text = '${player['phy'] ?? ''}';
  }

  void clearPlayerForm() {
    editingPlayerId = null;
    for (final controller in [playerName, playerPosition, strongFoot, height, weight, playstyles, pace, shoot, pass, dribbling, def, phy]) {
      controller.clear();
    }
  }

  Map<String, dynamic> buildPlayer() {
    return {
      'name': playerName.text.trim(),
      'position': playerPosition.text.trim(),
      'strongFoot': strongFoot.text.trim(),
      'height': number(height),
      'weight': number(weight),
      'playstyles': playstyles.text.split(',').map((x) => x.trim()).where((x) => x.isNotEmpty).toList(),
      'pace': integer(pace),
      'shoot': integer(shoot),
      'pass': integer(pass),
      'dribbling': integer(dribbling),
      'def': integer(def),
      'phy': integer(phy),
    };
  }

  Future<Map<String, dynamic>> buildBalancedSquad() async {
    if (selected.length != teamSize * 2) {
      throw 'Kadro kurmak için ${teamSize * 2} oyuncu seçmelisin. Şu an ${selected.length} seçili.';
    }

    final data = await api('/team-builder/balance', method: 'POST', body: {
      'teamSize': teamSize,
      'formation': formation.text,
      'playerIds': selected.toList(),
    }) as Map<String, dynamic>;
    final analysis = await api('/ai/team-analysis', method: 'POST', body: data) as Map<String, dynamic>;
    setState(() {
      balanced = data;
      teamAnalysis = analysis;
      resultTeamTab = 0;
    });
    return {'teams': data, 'analysis': analysis};
  }

  Future<Map<String, dynamic>> uploadDetailedMatch() async {
    final data = await api('/matches', method: 'POST', body: buildDetailedMatch()) as Map<String, dynamic>;
    await loadPlayers(silent: true);
    return data;
  }

  Future<Map<String, dynamic>> deleteSelectedPlayers() async {
    final data = await api('/players/bulk-delete', method: 'POST', body: {'playerIds': selected.toList()}) as Map<String, dynamic>;
    selected.clear();
    await loadPlayers(silent: true);
    return data;
  }

  Future<Map<String, dynamic>> loadPlayersJson() async {
    final data = await loadPlayers(silent: true);
    bulkJson.text = pretty(data);
    return {'players': data.length};
  }

  Future<Map<String, dynamic>> seedRecentMatches() async {
    final data = await api('/matches/seed-recent', method: 'POST') as Map<String, dynamic>;
    await loadPlayers(silent: true);
    await loadMatches();
    return data;
  }

  Map<String, int> requiredPositionCounts() {
    final roles = formationSlotRoles(formation.text, teamSize);
    final counts = <String, int>{};
    for (final role in roles) {
      counts[role] = (counts[role] ?? 0) + 2;
    }
    return counts;
  }

  String positionCountText() {
    final required = requiredPositionCounts();
    final selectedCounts = <String, int>{};
    for (final player in selectedPlayers) {
      final role = normalizePosition(player['position']);
      selectedCounts[role] = (selectedCounts[role] ?? 0) + 1;
    }

    return required.entries
        .map((entry) => '${slotLabel(entry.key)} ${selectedCounts[entry.key] ?? 0}/${entry.value}')
        .join('  ');
  }

  Map<String, dynamic> buildDetailedMatch() {
    final ids = selected.toList();
    return {
      'matchDate': DateTime.now().toUtc().toIso8601String(),
      'duration': int.tryParse(duration.text) ?? 0,
      'homeScore': int.tryParse(homeScore.text) ?? 0,
      'awayScore': int.tryParse(awayScore.text) ?? 0,
      'players': [
        for (var i = 0; i < ids.length; i++)
          {
            'playerId': ids[i],
            'team': i.isEven ? 'A' : 'B',
            'stats': {
              'goals': statInt(ids[i], 'goals'),
              'assists': statInt(ids[i], 'assists'),
              'shotsOnTarget': statInt(ids[i], 'shotsOnTarget'),
              'successfulPasses': statInt(ids[i], 'successfulPasses'),
              'tackles': statInt(ids[i], 'tackles'),
              'saves': statInt(ids[i], 'saves'),
              'rating': statDouble(ids[i], 'rating'),
            }
          }
      ]
    };
  }

  Map<String, TextEditingController> statsFor(int playerId) {
    return statInputs.putIfAbsent(playerId, () => {
          'rating': TextEditingController(),
          'goals': TextEditingController(),
          'assists': TextEditingController(),
          'shotsOnTarget': TextEditingController(),
          'successfulPasses': TextEditingController(),
          'tackles': TextEditingController(),
          'saves': TextEditingController(),
        });
  }

  List<Map<String, dynamic>> get selectedPlayers {
    final byId = {for (final player in players) player['id'] as int: player};
    return [for (final id in selected) if (byId[id] != null) byId[id]!];
  }

  List<Map<String, dynamic>?> get slotPlayers {
    final byId = {for (final player in players) player['id'] as int: player};
    return [for (var i = 0; i < teamSize; i++) slotPlayerIds[i] == null ? null : byId[slotPlayerIds[i]]];
  }

  Future<void> pickForSlot(int index, String role) async {
    if (players.isEmpty) await loadPlayers(silent: true);
    if (!mounted) return;
    final currentId = slotPlayerIds[index];
    final options = players.where((player) => roleMatches(player, role)).toList();
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (_) => SafeArea(
        child: ListView(
          children: [
            ListTile(title: Text('${slotLabel(role)} seç'), subtitle: Text('${options.length} uygun oyuncu')),
            if (currentId != null)
              ListTile(
                leading: const Icon(Icons.close),
                title: const Text('Slotu boşalt'),
                onTap: () {
                  Navigator.pop(context);
                  setState(() => removeFromSlots(currentId));
                },
              ),
            for (final player in options)
              ListTile(
                selected: slotPlayerIds[index] == player['id'],
                title: Text('${player['name']}'),
                subtitle: Text('${player['position']} | OVR ${player['overall']} | FORM ${player['form']}', maxLines: 1, overflow: TextOverflow.ellipsis),
                onTap: () {
                  Navigator.pop(context);
                  setState(() => assignToSlot(index, player));
                },
              ),
          ],
        ),
      ),
    );
  }

  void assignToSlot(int index, Map<String, dynamic> player) {
    final id = player['id'] as int;
    if (!roleMatches(player, formationSlotRoles(formation.text, teamSize)[index])) return;
    if (selected.length >= teamSize * 2 && !selected.contains(id)) {
      output = 'En fazla ${teamSize * 2} oyuncu seçebilirsin.';
      return;
    }
    removeFromSlots(id);
    slotPlayerIds[index] = id;
    selected.add(id);
  }

  void assignToFirstSlot(Map<String, dynamic> player) {
    final roles = formationSlotRoles(formation.text, teamSize);
    int? index;
    for (var i = 0; i < teamSize; i++) {
      if (slotPlayerIds[i] == null && roleMatches(player, roles[i])) {
        index = i;
        break;
      }
    }
    if (index == null) {
      output = '${player['name']} için boş ${slotLabel(normalizePosition(player['position']))} slotu yok';
      return;
    }
    assignToSlot(index, player);
  }

  void removeFromSlots(int id) {
    for (var i = 0; i < slotPlayerIds.length; i++) {
      if (slotPlayerIds[i] == id) slotPlayerIds[i] = null;
    }
  }

  void selectPlayer(Map<String, dynamic> player) {
    if (selected.length >= teamSize * 2 && !selected.contains(player['id'])) {
      output = 'En fazla ${teamSize * 2} oyuncu seçebilirsin.';
      return;
    }
    selected.add(player['id'] as int);
  }

  void unselectPlayer(int id) {
    selected.remove(id);
    removeFromSlots(id);
  }

  void clearSlots() {
    for (var i = 0; i < slotPlayerIds.length; i++) {
      slotPlayerIds[i] = null;
    }
  }

  List<Map<String, dynamic>> get filteredPlayers {
    if (playerSearchTerm.isEmpty) return players;
    return players.where((p) {
      final text = '${p['name']} ${p['position']} ${(p['playstyles'] as List).join(' ')}'.toLowerCase();
      return text.contains(playerSearchTerm);
    }).toList();
  }

  int integer(TextEditingController controller) => int.tryParse(controller.text) ?? 50;
  double number(TextEditingController controller) => double.tryParse(controller.text) ?? 0;
  int statInt(int id, String key) => int.tryParse(statsFor(id)[key]!.text) ?? 0;
  double statDouble(int id, String key) => double.tryParse(statsFor(id)[key]!.text) ?? 0;

  void setApiBase(String value) {
    setState(() {
      apiBase.text = value;
      output = 'API Base set to $value';
    });
    saveSession();
  }

  void logout() {
    token = null;
    role = 'User';
    unawaited(clearSessionData());
    setState(() {});
  }
}

class PitchPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final line = Paint()
      ..color = Colors.white30
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.2;
    canvas.drawRect(Offset.zero & size, line);
    canvas.drawLine(Offset(0, size.height / 2), Offset(size.width, size.height / 2), line);
    canvas.drawCircle(Offset(size.width / 2, size.height / 2), size.width * .11, line);
    canvas.drawRect(Rect.fromCenter(center: Offset(size.width / 2, size.height * .08), width: size.width * .48, height: size.height * .15), line);
    canvas.drawRect(Rect.fromCenter(center: Offset(size.width / 2, size.height * .92), width: size.width * .48, height: size.height * .15), line);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

List<String> formationOptions(int teamSize) {
  return switch (teamSize) {
    6 => ['2-2-1', '3-1-1', '1-3-1'],
    7 => ['2-3-1', '3-2-1', '3-3-0'],
    8 => ['3-3-1', '2-4-1', '3-2-2'],
    9 => ['3-3-2', '4-3-1', '3-4-1'],
    10 => ['4-3-2', '3-4-2', '4-4-1'],
    _ => ['4-3-3', '4-4-2', '3-5-2'],
  };
}

List<Alignment> formationSlots(String formation, int teamSize) {
  final rows = formation.split('-').map((x) => int.tryParse(x.trim()) ?? 0).where((x) => x >= 0).toList();
  final total = rows.fold<int>(1, (sum, x) => sum + x);
  if (total != teamSize) {
    rows
      ..clear()
      ..addAll([max(1, teamSize - 5), 3, 1]);
  }

  final slots = <Alignment>[const Alignment(0, .9)];
  for (var r = 0; r < rows.length; r++) {
    final count = rows[r];
    final y = .38 - (r * 1.3 / max(1, rows.length - 1));
    for (var i = 0; i < count; i++) {
      final x = count == 1 ? 0.0 : -0.86 + (1.72 * i / (count - 1));
      slots.add(Alignment(x, y));
    }
  }
  return slots.take(teamSize).toList();
}

List<String> formationSlotRoles(String formation, int teamSize) {
  final rows = formation.split('-').map((x) => int.tryParse(x.trim()) ?? 0).where((x) => x >= 0).toList();
  final total = rows.fold<int>(1, (sum, x) => sum + x);
  if (total != teamSize) {
    rows
      ..clear()
      ..addAll([max(1, teamSize - 5), 3, 1]);
  }

  final roles = <String>['keeper'];
  for (var r = 0; r < rows.length; r++) {
    final role = r == 0 ? 'defender' : (r == rows.length - 1 ? 'striker' : 'mid');
    roles.addAll(List.filled(rows[r], role));
  }
  return roles.take(teamSize).toList();
}

bool roleMatches(Map<String, dynamic> player, String role) => normalizePosition(player['position']) == role;

String normalizePosition(Object? value) {
  final text = '$value'.toLowerCase();
  if (text.contains('keeper') || text.contains('kaleci') || text == 'gk') return 'keeper';
  if (text.contains('def') || text.contains('stoper') || text.contains('bek')) return 'defender';
  if (text.contains('mid') || text.contains('orta') || text.contains('cm') || text.contains('dm')) return 'mid';
  if (text.contains('striker') || text.contains('forvet') || text.contains('forward') || text == 'st' || text.contains('wing')) return 'striker';
  return text;
}

String slotLabel(String role) {
  return switch (role) {
    'keeper' => 'GK',
    'defender' => 'DEF',
    'mid' => 'MID',
    'striker' => 'ST',
    _ => role.toUpperCase(),
  };
}

String pretty(Object? value) => const JsonEncoder.withIndent('  ').convert(value);
