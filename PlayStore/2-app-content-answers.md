# Play Console "App content" answers

Play Console → **Policy and programs → App content**. All of these must be completed before you can publish.
The answers below match what the game actually does: only ads collect data, and progress stays on the device.

---

## 1. Privacy policy
Paste the public URL where you host `privacy-policy.html` (fill in your name and email first).

**Free hosting options:**
- **GitHub Pages:** create a public repo, upload the file as `index.html`, then go to Settings → Pages → enable.
- **Google Sites:** create a page and paste the text.

Then also paste the same URL into `Game.cs` → `PrivacyPolicyUrl` so the in-game "Privacy Policy" button appears.

## 2. Ads
- **Does your app contain ads?** → **Yes**

## 3. App access
- **All functionality is available without special access** (no login).

## 4. Content rating (IARC questionnaire)
- Category: **Game**
- Violence: **No** (a cube jumps over spikes, with no characters harmed or blood)
- Fear / sexuality / language / drugs / gambling: **No** to all
- Users can interact or exchange content: **No**
- Shares user location with others: **No**
- Allows purchases of digital goods: **No** (coins are earned in-game only, with no in-app purchases)
- Unrestricted internet access: **No**

Expected result: **Everyone / PEGI 3** (ads are listed separately).

## 5. Target audience and content
- Target age groups: tick **13–15, 16–17, 18 and over** only.
- Do **not** tick ages under 13. Including children brings the app under the **Families policy**, which needs Families-certified ad settings, child-directed ad requests and extra review. If you want kids later, ask me and I'll make the code changes.
- "Could your store listing unintentionally appeal to children?" → **No** (if Google flags it, the answer stays truthful: the game isn't designed for children).

## 6. News app
- **No**

## 7. Advertising ID
- **Does your app use advertising ID?** → **Yes**
- Purposes: **Advertising or marketing**, **Analytics**, **Fraud prevention, security, and compliance**

(The Google Mobile Ads SDK adds the `AD_ID` permission automatically.)

## 8. Government apps / Financial features / Health
- **No** to all.

## 9. Data safety
Google's own guidance for apps that use the **Google Mobile Ads SDK**:

**Overview questions**
- Does your app collect or share any of the required user data types? → **Yes**
- Is all user data encrypted in transit? → **Yes**
- Do you provide a way for users to request that their data is deleted? → **No**. The game stores nothing on a server; ad data is governed by Google. If you'd rather answer Yes, add your contact email for requests.

**Data types to declare** (for each one: **Collected: Yes**, **Shared: Yes**, **Processed ephemerally: No**, **Required: Yes**, i.e. users can't turn it off)

| Data type | Purposes |
|---|---|
| **Location → Approximate location** (from IP address) | Advertising or marketing, Analytics, Fraud prevention/security/compliance |
| **App activity → App interactions** (ad views/taps) | Advertising or marketing, Analytics, Fraud prevention/security/compliance |
| **App info and performance → Crash logs** | Analytics, Fraud prevention/security/compliance |
| **App info and performance → Diagnostics** | Analytics, Fraud prevention/security/compliance |
| **Device or other IDs** (advertising ID) | Advertising or marketing, Analytics, Fraud prevention/security/compliance |

Do **not** declare: personal info, financial info, health, messages, photos, audio, files, calendar, contacts, web history or precise location. The game doesn't touch any of them.

Official reference: https://developers.google.com/admob/android/privacy/play-data-disclosure
