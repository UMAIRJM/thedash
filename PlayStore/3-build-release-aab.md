# Building the signed release AAB (the file you upload to Play)

Google Play only accepts an `.aab` signed with **your upload key** (`user.keystore`).
Unity never saves keystore passwords, so you do this final step yourself. It takes about 5 minutes.

1. Open the project in **Unity 6000.4.0f1** (Unity Hub → Projects → thedash).
2. **Edit → Project Settings → Player → Android tab → Publishing Settings:**
   - Custom Keystore: ✔ already selected (`D:/Umair/Unity Projects/Builds/The Dash/The Dash Key Store/user.keystore`)
   - Enter the **keystore password**, choose alias **thedash**, and enter the **alias password**.
3. **File → Build Profiles → Android** (already the active platform):
   - ✔ **Build App Bundle (Google Play)**
   - ✘ Development Build must be **unticked** (otherwise test ads and the dev watermark)
4. Click **Build** and save it, for example as `TheDash-1.1.0.aab`.
5. Play Console → your app → **Test and release → Production** (or Internal testing first) → **Create new release** → upload the `.aab`.

### Notes
- The previous upload used **version code 1**. This build is **version code 3** (version name 1.1.0). Every future upload needs a higher version code: **Player Settings → Other Settings → Bundle Version Code**.
- If Play says the app is signed with the wrong key, you used a different keystore from the first upload. Use the same `user.keystore`.
- **Keep `user.keystore` and its passwords backed up.** If you're enrolled in Play App Signing (the default), Google can reset a lost upload key, but it's a slow process.
- Tip: release to **Internal testing** first. You get a Play Store link for your own phone within minutes, before going public.
