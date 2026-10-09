# Google AdMob: everything you need to know for The Dash

## What's already set up in the game
| Item | Value / behaviour |
|---|---|
| AdMob **App ID** | `ca-app-pub-6735253086451753~4985520369` (in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`) |
| **Banner** unit | `ca-app-pub-6735253086451753/6107480053`, shown **only on the main menu**, never during gameplay |
| **Interstitial** unit | `ca-app-pub-6735253086451753/9084547472`, shown **between runs**: at most every 3rd game over, at least 2 minutes apart, and **never at app launch** |
| **Rewarded** unit | **Not created yet** (see step 1 below). Powers "Continue" and "x2 Coins". |
| Consent (GDPR/UK) | Google UMP consent form runs at startup; players can change it in Settings → **Ad Privacy** |
| Test ads | Editor + **Development** builds automatically use Google's test ads. Release builds use your real IDs. |

These rules follow AdMob placement policy: no ads on launch, no ads where gameplay taps could hit them by accident, rewarded ads always optional.

---

## Steps you need to do in AdMob

### 1. Create the Rewarded ad unit (needed for "Continue" + "x2 Coins")
1. https://admob.google.com → **Apps → The Dash → Ad units → Add ad unit → Rewarded**.
2. Name it `TheDash_Rewarded`. Reward: `1` `continue` (the value doesn't matter, the game handles it).
3. Copy the ID (`ca-app-pub-6735253086451753/xxxxxxxxxx`).
4. Paste it into `Assets/Scripts/AdsManager.cs`:
   ```csharp
   const string ReleaseRewardedId = "ca-app-pub-6735253086451753/xxxxxxxxxx";
   ```
Until you do this, release builds simply hide the ad buttons. Coin-based continue still works.

### 2. Create the GDPR consent message (required for Europe/UK)
**Privacy & messaging → European regulations → Create message**: select the app, add your privacy policy URL, publish.
Without it, the consent form has nothing to show, and ads to EEA/UK users are limited.
Also consider **US states regulations → Create message** (same screen).

### 3. Link the app to Google Play (after it's published)
**Apps → The Dash → App settings → App store details → Add** → search Google Play for *The Dash*.
AdMob then reviews the app ("app readiness"). Ads serve in a limited way until this passes, which usually takes a few days.

### 4. app-ads.txt (protects your ad revenue)
1. Put a **developer website** in the Play Console store listing (e.g. your GitHub Pages site).
2. Upload `app-ads.txt` (in this folder) to the **root** of that site, so it's reachable at `https://yoursite/app-ads.txt`.
3. AdMob → **Apps → View all apps → app-ads.txt** tab shows when it's verified (can take ~24 h).
Without it, some advertisers won't bid and you earn less.

### 5. Payments
**Payments → Payment info**: add your address, tax info and bank account. Google pays monthly once you pass the payment threshold (US$100 or local equivalent), after verifying your address with a PIN letter at about US$10.

---

## Rules that can get your AdMob account banned
- **Never tap your own real ads**, and don't ask friends to. Test with the test APK (it shows test ads), or add your phone as a test device: **Settings → Test devices → Add test device** (needs your phone's advertising ID).
- Don't encourage taps ("click the ad to support us").
- Don't move ads close to gameplay buttons. The current layout is compliant; keep it that way.
- If AdMob ever shows a policy issue, check **Policy center** in AdMob.

## Earning more later (optional)
- **Mediation** (AdMob → Mediation) adds other networks (Unity Ads, AppLovin, Meta) that compete for your ad space.
- Rewarded ads usually earn the most. The "Continue" button is a natural, player-friendly placement.
- The interstitial frequency (every 3 runs, 2 minutes apart) is at the top of `AdsManager.cs` (`RunsBetweenInterstitials`, `MinSecondsBetweenInterstitials`). Making it more aggressive earns more but annoys players, and ratings matter more for growth.
