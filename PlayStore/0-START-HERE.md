# The Dash: Play Store release kit

Everything Google Play needs is in this folder. Follow the steps in order.

## What's in here
| File / folder | What it's for |
|---|---|
| `TheDash-TEST-install-on-phone.apk` | Test build for **your phone** (Google test ads, debug-signed). **Do not upload to Play.** |
| `icon-512.png` | Play Store app icon |
| `feature-graphic-1024x500.png` | Play Store feature graphic (banner) |
| `screenshots/phone`, `tablet-7inch`, `tablet-10inch` | Store screenshots with captions (8 each) |
| `screenshots/raw-no-captions` | Same screenshots without captions |
| `1-store-listing.md` | App name, short and full description, category |
| `2-app-content-answers.md` | Answers for content rating, target audience, ads, Data safety |
| `3-build-release-aab.md` | How to make the signed `.aab` you upload |
| `4-admob-guide.md` | AdMob setup, rewarded ad unit, consent, app-ads.txt, payments |
| `privacy-policy.html` | Privacy policy (fill in name and email, then host it) |
| `app-ads.txt` | Upload to your website root (see AdMob guide) |
| `icon-1024.png`, `adaptive-*.png` | Source art (the icons are already wired into Unity) |

## Release checklist
1. ☐ Install `TheDash-TEST-install-on-phone.apk` on your phone and play a few runs. Check touch controls, sound, the menus, and that test ads appear in the menu and between runs.
2. ☐ AdMob: create the **Rewarded** ad unit and paste its ID into `AdsManager.cs` (see `4-admob-guide.md`).
3. ☐ Fill in `privacy-policy.html` (name and email), host it, and put the URL in `Game.cs → PrivacyPolicyUrl`.
4. ☐ Build the signed release `.aab` (see `3-build-release-aab.md`).
5. ☐ Play Console → create or replace the release with the new `.aab` (version code **3**, version **1.1.0**).
6. ☐ Store listing → paste text from `1-store-listing.md`, upload icon, feature graphic and screenshots.
7. ☐ App content → answer using `2-app-content-answers.md`.
8. ☐ Submit for review. Then in AdMob: link the Play Store listing, create the consent message, add app-ads.txt.

## Already handled in the build
- ✅ Targets **API 36** (Android 16)
- ✅ **64-bit** (arm64-v8a) + 32-bit native code, IL2CPP
- ✅ **16 KB page size** aligned native libraries (required for new apps)
- ✅ Adaptive app icon, app name "The Dash", landscape only
- ✅ Ads follow AdMob placement rules; GDPR consent (UMP) included
- ✅ Safe-area support for notches and edge-to-edge screens; Android back button works (back = pause / close / quit)
