# Tactiq Mobile

Minimal Flutter MVP client.

```powershell
cd C:\Users\karag\Desktop\Tactiq\TactiqMobile
flutter create .
flutter pub get
flutter run
```

API base:

- Windows/Desktop: `http://localhost:5000/api`
- Android emulator: `http://10.0.2.2:5000/api`
- Real phone: use your PC LAN IP, for example `http://192.168.1.20:5000/api`
- USB debug with adb reverse: `http://127.0.0.1:5000/api`

Run the API so mobile devices can reach it:

```powershell
dotnet run --project C:\Users\karag\Desktop\Tactiq\TactiqAPI
```

For Android USB/emulator, this is usually the least painful route:

```powershell
adb reverse tcp:5000 tcp:5000
```

Then press `USB` in the app and `Ping API`.

If Android blocks local HTTP later, add cleartext traffic in `android/app/src/main/AndroidManifest.xml`.
