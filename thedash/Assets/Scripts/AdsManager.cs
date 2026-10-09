using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;

/// <summary>
/// AdMob wrapper: GDPR consent (UMP), banner on menus only, an interstitial between runs
/// (never on app launch, never mid-run) and a rewarded ad for "continue" / "double coins".
/// Development builds and the editor always use Google's test IDs.
/// </summary>
public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    // Real ad unit IDs (used only in non-development builds)
    const string ReleaseBannerId = "ca-app-pub-6735253086451753/6107480053";
    const string ReleaseInterstitialId = "ca-app-pub-6735253086451753/9084547472";
    // TODO: create a "Rewarded" ad unit in AdMob and paste its ID here. Until then rewarded buttons are hidden in release builds.
    const string ReleaseRewardedId = "";

    const string TestBannerId = "ca-app-pub-3940256099942544/6300978111";
    const string TestInterstitialId = "ca-app-pub-3940256099942544/1033173712";
    const string TestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    const int RunsBetweenInterstitials = 3;
    const float MinSecondsBetweenInterstitials = 120f;

    static bool UseTestIds => Application.isEditor || Debug.isDebugBuild;
    static string BannerId => UseTestIds ? TestBannerId : ReleaseBannerId;
    static string InterstitialId => UseTestIds ? TestInterstitialId : ReleaseInterstitialId;
    static string RewardedId => UseTestIds ? TestRewardedId : ReleaseRewardedId;

    BannerView banner;
    InterstitialAd interstitial;
    RewardedAd rewarded;
    bool initialized, bannerWanted;
    int runsSinceInterstitial;
    float lastFullscreenTime = -999f;

    /// <summary>True while a full-screen ad is on screen.</summary>
    public bool ShowingFullscreen { get; private set; }

    public bool RewardedReady => rewarded != null && rewarded.CanShowAd();

    /// <summary>The Google SDKs only run on Android, iOS and in the editor (placeholder ads).</summary>
    static bool Supported => Application.isEditor || Application.platform == RuntimePlatform.Android ||
                             Application.platform == RuntimePlatform.IPhonePlayer;

    public bool PrivacyOptionsRequired
    {
        get
        {
            if (!Supported) return false;
            try
            {
                return ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Consent status unavailable: " + e.Message);
                return false;
            }
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (!Supported) return;
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // Ask for consent (EEA/UK) first, then start the SDK.
        var request = new ConsentRequestParameters();
        ConsentInformation.Update(request, updateError =>
        {
            if (updateError != null)
            {
                Debug.LogWarning("Consent update failed: " + updateError.Message);
                InitializeAds();
                return;
            }
            ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            {
                if (formError != null) Debug.LogWarning("Consent form error: " + formError.Message);
                if (ConsentInformation.CanRequestAds()) InitializeAds();
            });
        });

        // Returning users who already consented can start immediately.
        if (ConsentInformation.CanRequestAds()) InitializeAds();
    }

    void InitializeAds()
    {
        if (initialized) return;
        initialized = true;
        MobileAds.Initialize(_ =>
        {
            LoadBanner();
            LoadInterstitial();
            LoadRewarded();
        });
    }

    public void ShowPrivacyOptions()
    {
        if (!Supported) return;
        ConsentForm.ShowPrivacyOptionsForm(e =>
        {
            if (e != null) Debug.LogWarning("Privacy options error: " + e.Message);
        });
    }

    // ------------------------------------------------------------------ banner

    void LoadBanner()
    {
        banner?.Destroy();
        banner = new BannerView(BannerId, AdSize.Banner, AdPosition.Bottom);
        banner.LoadAd(new AdRequest());
        if (bannerWanted) banner.Show(); else banner.Hide();
    }

    /// <summary>Banner shows on menus/game over only - never during gameplay (avoids accidental taps).</summary>
    public void SetBannerVisible(bool visible)
    {
        bannerWanted = visible;
        if (banner == null) return;
        if (visible) banner.Show(); else banner.Hide();
    }

    // ------------------------------------------------------------------ interstitial

    void LoadInterstitial()
    {
        interstitial?.Destroy();
        interstitial = null;
        InterstitialAd.Load(InterstitialId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Interstitial failed to load: " + error);
                Invoke(nameof(LoadInterstitial), 30f);
                return;
            }
            interstitial = ad;
            ad.OnAdFullScreenContentClosed += () =>
            {
                ShowingFullscreen = false;
                LoadInterstitial();
            };
            ad.OnAdFullScreenContentFailed += _ =>
            {
                ShowingFullscreen = false;
                LoadInterstitial();
            };
        });
    }

    /// <summary>Call when the player leaves the game-over screen. Shows an interstitial occasionally.</summary>
    public void OnRunFinished()
    {
        runsSinceInterstitial++;
        if (runsSinceInterstitial < RunsBetweenInterstitials) return;
        if (Time.realtimeSinceStartup - lastFullscreenTime < MinSecondsBetweenInterstitials) return;
        if (interstitial == null || !interstitial.CanShowAd()) return;
        runsSinceInterstitial = 0;
        lastFullscreenTime = Time.realtimeSinceStartup;
        ShowingFullscreen = true;
        interstitial.Show();
    }

    // ------------------------------------------------------------------ rewarded

    void LoadRewarded()
    {
        if (string.IsNullOrEmpty(RewardedId)) return;
        rewarded?.Destroy();
        rewarded = null;
        RewardedAd.Load(RewardedId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning("Rewarded failed to load: " + error);
                Invoke(nameof(LoadRewarded), 30f);
                return;
            }
            rewarded = ad;
        });
    }

    /// <summary>Shows a rewarded ad. <paramref name="done"/> receives true if the reward was earned.</summary>
    public void ShowRewarded(Action<bool> done)
    {
        if (!RewardedReady)
        {
            done?.Invoke(false);
            return;
        }
        bool earned = false, finished = false;
        var ad = rewarded;
        rewarded = null;
        ShowingFullscreen = true;
        lastFullscreenTime = Time.realtimeSinceStartup;

        void Finish()
        {
            if (finished) return;
            finished = true;
            ShowingFullscreen = false;
            ad.Destroy();
            LoadRewarded();
            done?.Invoke(earned);
        }

        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += _ => Finish();
        ad.Show(_ => earned = true);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        banner?.Destroy();
        interstitial?.Destroy();
        rewarded?.Destroy();
    }
}
