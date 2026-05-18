# Nile Android Client (minimal)

This is a lightweight Android client to list posts from the Nile Functions backend.

## Run
1. Start the backend (Functions app) on your Mac:
   ```bash
   cd ../Functions
   func start --port 7071
   ```
2. Use the Android emulator and point to the host with `10.0.2.2` (already set in `ServiceLocator`). If you use a physical device, replace `BASE_URL` in `ServiceLocator.kt` with your Mac's LAN IP (e.g., `http://192.168.x.x:7071/`) and update `res/xml/network_security_config.xml` with that host.
3. Open `android-client` in Android Studio, let it sync Gradle, then press Run.

## Notes
- Networking uses Retrofit + Moshi; logs are enabled via OkHttp logging interceptor.
- ViewBinding is enabled for simple RecyclerView binding.
- Cleartext HTTP is allowed for the configured host in `network_security_config.xml`.
