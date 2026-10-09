using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TheDash
{
    /// <summary>
    /// Entry point and state machine. The whole game lives in one scene and is built from code,
    /// so menu -> run -> retry is instant (no scene loads).
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        public const string SceneName = "TheDash";
        // Hosted privacy policy (repo: github.com/UMAIRJM/thedash-policy).
        // The "Privacy Policy" button in Settings stays hidden until this is set.
        public const string PrivacyPolicyUrl = "https://umairjm.github.io/thedash-policy/";
        const float ZoneLength = World.ZoneLength, ZoneBlend = 70f, MilestoneEvery = 250f;
        const int ReviveCost = 150;

        enum State { Menu, Countdown, Playing, Paused, Dead, GameOver }

        State state;
        Player player;
        World world;
        Backdrop backdrop;
        CameraRig rig;
        Fx fx;
        GameUI ui;

        float countdown, deadTimer, reviveTimer;
        bool revived, reviveOffered, doubled, newBestAnnounced, dailyShownThisSession;
        int coinsRun, perfectArcs, lastZone, lastMilestone, bestAtStart, lastReportedMeters, powerupsRun;
        float lastThemeKey = -1, speedFx, streakTimer;
        readonly HashSet<int> uiFingers = new HashSet<int>();

        // ------------------------------------------------------------------ bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != SceneName) return;
            if (FindAnyObjectByType<Game>() != null) return;
            new GameObject("TheDash").AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60;   // Android defaults to 30 fps - the main cause of the old "not smooth" feel
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Input.multiTouchEnabled = true;

            new GameObject("Sfx").AddComponent<Sfx>().transform.SetParent(transform, false);
            if (AdsManager.Instance == null) new GameObject("Ads").AddComponent<AdsManager>();

            rig = new GameObject("Camera").AddComponent<CameraRig>();
            rig.transform.SetParent(transform, false);
            rig.Build();

            backdrop = new GameObject("Backdrop").AddComponent<Backdrop>();
            backdrop.transform.SetParent(transform, false);
            backdrop.Build();

            fx = new GameObject("Fx").AddComponent<Fx>();
            fx.transform.SetParent(transform, false);

            world = new GameObject("World").AddComponent<World>();
            world.transform.SetParent(transform, false);
            world.Build(fx);

            player = new GameObject("Player").AddComponent<Player>();
            player.transform.SetParent(transform, false);
            player.Build();

            ui = new GameObject("UI").AddComponent<GameUI>();
            ui.transform.SetParent(transform, false);
            ui.Build(this);

            Missions.Load();
            Missions.Completed += OnMissionCompleted;
        }

        void Start() => EnterMenu();

        void OnDestroy() => Missions.Completed -= OnMissionCompleted;

        // ------------------------------------------------------------------ states

        void EnterMenu()
        {
            state = State.Menu;
            world.ResetWorld();
            fx.Clear();
            player.ResetAt(new Vector2(0, Player.Half));
            player.ApplySkin();
            rig.Snap(0);
            ApplyTheme(0, true);
            ui.ShowMenu();
            Sfx.I.SetMusicIntensity(false);
            Sfx.I.SetMusicPitch(1f);
            AdsManager.Instance?.SetBannerVisible(true);

            if (!dailyShownThisSession && SaveData.DailyAvailable)
            {
                dailyShownThisSession = true;
                Invoke(nameof(OpenDailyDelayed), 0.7f);
            }
        }

        void OpenDailyDelayed()
        {
            if (state == State.Menu) ui.OpenDaily();
        }

        public void StartRun()
        {
            if (state != State.Menu && state != State.GameOver) return;
            ui.CloseAll();
            world.ResetWorld();
            fx.Clear();
            player.ResetAt(new Vector2(0, Player.Half));
            rig.Snap(0);
            ApplyTheme(0, true);

            coinsRun = perfectArcs = powerupsRun = 0;
            revived = reviveOffered = doubled = newBestAnnounced = false;
            lastZone = 0;
            lastMilestone = 0;
            lastReportedMeters = 0;
            bestAtStart = SaveData.Best;
            Missions.BeginRun();

            ui.ShowHud(bestAtStart);
            speedFx = 0f;
            Sfx.I.SetMusicPitch(1f);
            AdsManager.Instance?.SetBannerVisible(false);
            Sfx.I.SetMusicIntensity(true);
            Sfx.I.ApplySettings();

            // If an interstitial is on screen, wait for it with a countdown; otherwise go instantly.
            if (AdsManager.Instance != null && AdsManager.Instance.ShowingFullscreen) BeginCountdown(3);
            else state = State.Playing;
        }

        void BeginCountdown(int seconds)
        {
            state = State.Countdown;
            countdown = seconds;
        }

        public void Pause()
        {
            if (state != State.Playing && state != State.Countdown) return;
            state = State.Paused;
            ui.SetCountdown("");
            ui.ShowPause();
            Sfx.I.SetMusicIntensity(false);
        }

        public void Resume()
        {
            if (state != State.Paused) return;
            ui.CloseAll();
            Sfx.I.SetMusicIntensity(true);
            BeginCountdown(3);
        }

        public void Restart()
        {
            if (state == State.GameOver) AdsManager.Instance?.OnRunFinished();
            else if (state == State.Paused) FinishRunStats();
            else return;
            state = State.GameOver;
            StartRun();
        }

        public void GoHome()
        {
            if (state == State.Paused) FinishRunStats();
            else if (state == State.GameOver) AdsManager.Instance?.OnRunFinished();
            else return;
            ui.CloseAll();
            EnterMenu();
        }

        // ------------------------------------------------------------------ main loop

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            ReadInput(out bool pressed, out bool held);

            if (Input.GetKeyDown(KeyCode.Escape)) OnBack();

            switch (state)
            {
                case State.Menu:
                    player.Idle(dt);
                    backdrop.AddDrift(dt * 4f);
                    break;

                case State.Countdown:
                    if (AdsManager.Instance != null && AdsManager.Instance.ShowingFullscreen) countdown = 3;
                    countdown -= dt * 1.5f;   // a quick "3-2-1"
                    ui.SetCountdown(countdown > 0 ? Mathf.CeilToInt(countdown).ToString() : "");
                    if (countdown <= 0) state = State.Playing;
                    break;

                case State.Playing:
                    TickRun(dt, pressed, held);
                    break;

                case State.Dead:
                    TickDead(dt);
                    break;
            }
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 20f);
            rig.Follow(player.pos.x, player.pos.y, dt);
            backdrop.Follow(rig.cam);
            world.Tick(rig.Left, rig.Right, player, state == State.Playing ? dt : 0f);
        }

        void TickRun(float dt, bool pressed, bool held)
        {
            float speed = World.SpeedAt(player.pos.x);
            int jumpsBefore = player.jumps;
            player.Step(dt, speed, pressed, held, world, fx);
            if (player.jumps >= 3 && jumpsBefore < 3 && !SaveData.TutorialDone)
            {
                SaveData.TutorialDone = true;
                ui.HideTutorial();
            }
            if (state != State.Playing) return; // died this frame

            int meters = Mathf.Max(0, Mathf.FloorToInt(player.pos.x));
            float mult = World.SpeedMultiplierAt(player.pos.x);
            ui.UpdateHud(meters, coinsRun, player.hasShield, world.MagnetTime > 0 ? world.MagnetTime / 10f : 0f, mult);
            rig.SetSpeedZoom(mult);
            Sfx.I.SetMusicPitch(1f + 0.025f * World.BoostLevel(player.pos.x));
            SpeedLines(dt, speed, mult);

            int zone = Mathf.FloorToInt(player.pos.x / ZoneLength);
            if (zone != lastZone)
            {
                lastZone = zone;
                var t = Theme.All[zone % Theme.All.Length];
                ui.Toast($"ZONE {zone + 1}  •  {t.name}", t.groundEdge, true);
                Sfx.I.Play("milestone");
                if (zone <= World.MaxBoosts)
                {
                    float next = World.SpeedMultiplierAt(zone * ZoneLength + World.BoostRamp);
                    ui.Toast($"SPEED UP!  x{next:0.00}", UIColors.Cyan);
                    ui.PunchSpeed();
                    speedFx = 1f;
                    rig.BoostPulse();
                    rig.Shake(0.12f);
                    Sfx.I.Play("power", 1.25f);
                }
            }
            else if (meters >= lastMilestone + MilestoneEvery)
            {
                lastMilestone = Mathf.FloorToInt(meters / MilestoneEvery) * (int)MilestoneEvery;
                if (lastMilestone % (int)ZoneLength != 0)
                {
                    ui.Toast(lastMilestone + " m", Color.white);
                    Sfx.I.Play("milestone", 1.2f, 0.6f);
                }
            }
            if (!newBestAnnounced && bestAtStart > 0 && meters > bestAtStart)
            {
                newBestAnnounced = true;
                ui.Toast("NEW BEST!", UIColors.Gold, true);
                Sfx.I.Play("best");
                fx.Explode(player.pos + Vector2.up, UIColors.Gold, Color.white, 30);
            }
            if (meters - lastReportedMeters >= 10)
            {
                lastReportedMeters = meters;
                Missions.Report(MissionType.RunDistance, meters);
            }
            ApplyTheme(player.pos.x, false);
        }

        /// <summary>Speed lines: a burst on every boost, plus a light constant stream once you're going fast.</summary>
        void SpeedLines(float dt, float speed, float mult)
        {
            speedFx = Mathf.MoveTowards(speedFx, 0f, dt / 2.5f);
            float rate = speedFx * 45f + Mathf.Max(0f, mult - 1.2f) * 12f;
            if (rate <= 0.01f) return;
            streakTimer -= dt;
            while (streakTimer <= 0f)
            {
                streakTimer += 1f / rate;
                float h = rig.cam.orthographicSize;
                var at = new Vector2(rig.Right + Random.Range(0f, 2f), rig.cam.transform.position.y + Random.Range(-h * 0.85f, h * 0.9f));
                fx.Streak(at, Random.Range(1.5f, 4f), -speed * Random.Range(2.5f, 3.5f), 0.18f + speedFx * 0.3f);
            }
        }

        void TickDead(float dt)
        {
            if (deadTimer > 0)
            {
                deadTimer -= dt;
                if (deadTimer <= 0)
                {
                    bool canAd = AdsManager.Instance != null && AdsManager.Instance.RewardedReady;
                    bool canCoins = SaveData.Coins + coinsRun >= ReviveCost;
                    int meters = Mathf.FloorToInt(player.pos.x);
                    if (!revived && meters > 60 && (canAd || canCoins))
                    {
                        reviveOffered = true;
                        reviveTimer = 5f;
                        ui.ShowRevive(meters, ReviveCost, canAd, canCoins);
                    }
                    else EndRun();
                }
                return;
            }
            if (reviveOffered && !(AdsManager.Instance != null && AdsManager.Instance.ShowingFullscreen))
            {
                reviveTimer -= Time.unscaledDeltaTime;
                ui.UpdateReviveTimer(reviveTimer / 5f, reviveTimer);
                if (reviveTimer <= 0) DeclineRevive();
            }
        }

        void ReadInput(out bool pressed, out bool held)
        {
            pressed = held = false;
            var es = EventSystem.current;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began)
                    {
                        if (es != null && es.IsPointerOverGameObject(t.fingerId)) uiFingers.Add(t.fingerId);
                        else pressed = true;
                    }
                    bool active = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                    if (!active) uiFingers.Remove(t.fingerId);
                    else if (!uiFingers.Contains(t.fingerId)) held = true;
                }
            }
            else
            {
                uiFingers.Clear();
                if (Input.GetMouseButtonDown(0))
                {
                    if (es != null && es.IsPointerOverGameObject()) uiFingers.Add(-1);
                    else pressed = true;
                }
                if (!Input.GetMouseButton(0)) uiFingers.Remove(-1);
                held = Input.GetMouseButton(0) && !uiFingers.Contains(-1);
            }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) pressed = true;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow)) held = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || TD_AUTOTEST
            if (DebugAutoJump != null && state == State.Playing) pressed = DebugAutoJump();
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || TD_AUTOTEST
        /// <summary>Test hook: an autopilot can drive jumps in development builds.</summary>
        public static System.Func<bool> DebugAutoJump;
#endif

        void OnBack()
        {
            if (state == State.Playing || state == State.Countdown)
            {
                Pause();
                return;
            }
            if (ui.HandleBack()) return;
            if (state == State.Menu) Application.Quit();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && (state == State.Playing || state == State.Countdown)) Pause();
            if (paused) Missions.Flush();
        }

        void OnApplicationQuit() => Missions.Flush();

        // ------------------------------------------------------------------ theme

        void ApplyTheme(float x, bool force)
        {
            int zone = Mathf.FloorToInt(Mathf.Max(0, x) / ZoneLength);
            float into = Mathf.Max(0, x) - zone * ZoneLength;
            float blend = Mathf.Clamp01((into - (ZoneLength - ZoneBlend)) / ZoneBlend);
            blend = Mathf.SmoothStep(0, 1, blend);
            float key = zone + Mathf.Round(blend * 50f) / 50f;
            if (!force && Mathf.Approximately(key, lastThemeKey)) return;
            lastThemeKey = key;
            var a = Theme.All[zone % Theme.All.Length];
            var b = Theme.All[(zone + 1) % Theme.All.Length];
            var t = Theme.Lerp(a, b, blend);
            world.ApplyTheme(t);
            backdrop.Apply(t);
            rig.cam.backgroundColor = t.skyBottom;
        }

        // ------------------------------------------------------------------ events from the world

        public void OnCoin(bool perfectArc)
        {
            coinsRun++;
            Missions.Report(MissionType.RunCoins, coinsRun);
            Missions.Report(MissionType.TotalCoins, 1);
            Sfx.I.Play("coin", 1f + Mathf.Min(0.3f, (coinsRun % 10) * 0.03f), 0.7f);
            ui.PunchCoins();
            if (perfectArc)
            {
                coinsRun += 3;
                perfectArcs++;
                Missions.Report(MissionType.PerfectArcs, 1);
                ui.Toast("PERFECT  +3", UIColors.Gold);
            }
        }

        public void OnPickup(World.PickupType type)
        {
            powerupsRun++;
            Missions.Report(MissionType.Powerups, 1);
            Sfx.I.Play("power");
            if (type == World.PickupType.Shield)
            {
                player.hasShield = true;
                ui.Toast("SHIELD!", UIColors.Cyan);
            }
            else
            {
                world.MagnetTime = 10f;
                ui.Toast("MAGNET!", UIColors.Red);
            }
        }

        public void OnShieldBroken()
        {
            rig.Shake(0.25f);
            Vibrate();
        }

        public void OnPlayerDied(bool byObstacle)
        {
            state = State.Dead;
            deadTimer = 0.9f;
            reviveOffered = false;
            fx.Explode(player.pos, player.SkinColor, player.SkinAccent, 46);
            rig.Shake(0.5f);
            Sfx.I.Play("crash");
            Sfx.I.SetMusicIntensity(false);
            Vibrate();
            player.Hide();
            ui.SetCountdown("");
        }

        void OnMissionCompleted(Mission m)
        {
            ui.Toast($"MISSION COMPLETE  +{m.Reward}", UIColors.Green, state != State.Playing);
            Sfx.I.Play("reward");
        }

        static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (SaveData.Vibration) Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------------ revive / game over

        public void ReviveWithAd()
        {
            if (!reviveOffered) return;
            reviveOffered = false;
            AdsManager.Instance.ShowRewarded(ok =>
            {
                if (ok) Revive();
                else EndRun();
            });
        }

        public void ReviveWithCoins()
        {
            if (!reviveOffered) return;
            if (SaveData.Coins >= ReviveCost) SaveData.Coins -= ReviveCost;
            else if (SaveData.Coins + coinsRun >= ReviveCost)
            {
                coinsRun -= ReviveCost - SaveData.Coins;
                SaveData.Coins = 0;
            }
            else return;
            reviveOffered = false;
            Revive();
        }

        public void DeclineRevive()
        {
            if (state != State.Dead) return;
            reviveOffered = false;
            EndRun();
        }

        void Revive()
        {
            revived = true;
            ui.HideRevive();
            float x = player.pos.x;
            world.ClearForRevive(x);
            fx.Clear();
            player.ResetAt(new Vector2(x, Player.Half));
            player.invulnerable = 2.5f;
            Sfx.I.SetMusicIntensity(true);
            BeginCountdown(3);
        }

        void EndRun()
        {
            if (state != State.Dead) return;
            state = State.GameOver;
            ui.HideRevive();
            ui.HideHud();
            var info = FinishRunStats();
            ui.ShowGameOver(info);
            if (info.newBest) Sfx.I.Play("best");
        }

        /// <summary>Banks coins, saves the best distance and progresses missions.</summary>
        GameOverInfo FinishRunStats()
        {
            int meters = Mathf.Max(0, Mathf.FloorToInt(player.pos.x));
            bool newBest = meters > SaveData.Best;
            if (newBest) SaveData.Best = meters;
            SaveData.Coins += coinsRun;
            SaveData.RunsPlayed++;
            Missions.Report(MissionType.RunDistance, meters);
            Missions.Report(MissionType.TotalDistance, meters);
            Missions.Report(MissionType.Jumps, player.jumps);
            Missions.Report(MissionType.Runs, 1);
            Missions.Flush();
            bool canDouble = coinsRun > 0 && AdsManager.Instance != null && AdsManager.Instance.RewardedReady;
            return new GameOverInfo { distance = meters, best = SaveData.Best, coins = coinsRun, newBest = newBest && bestAtStart > 0, canDouble = canDouble };
        }

        public void DoubleCoinsWithAd()
        {
            if (state != State.GameOver || doubled || AdsManager.Instance == null) return;
            doubled = true;
            AdsManager.Instance.ShowRewarded(ok =>
            {
                if (!ok) return;
                SaveData.Coins += coinsRun;
                ui.SetGameOverCoins(coinsRun * 2, false);
                Sfx.I.Play("reward");
            });
        }

        // ------------------------------------------------------------------ shop / daily

        public void BuyOrSelectSkin(int idx)
        {
            if (!SaveData.Owns(idx))
            {
                int price = Skin.All[idx].price;
                if (SaveData.Coins < price)
                {
                    ui.Toast("NOT ENOUGH COINS", UIColors.Red);
                    return;
                }
                SaveData.Coins -= price;
                SaveData.Unlock(idx);
                Sfx.I.Play("best");
                ui.Toast("UNLOCKED " + Skin.All[idx].name + "!", UIColors.Gold, true);
            }
            SaveData.SelectedSkin = idx;
            player.ApplySkin();
        }

        public void ClaimDaily()
        {
            int reward = SaveData.ClaimDaily();
            if (reward <= 0) return;
            Sfx.I.Play("reward");
            ui.Toast($"+{reward} COINS", UIColors.Gold, true);
        }

        /// <summary>Watch a rewarded ad to get double the daily reward (normal reward if the ad fails).</summary>
        public void ClaimDailyDoubled()
        {
            if (!SaveData.DailyAvailable || AdsManager.Instance == null) return;
            AdsManager.Instance.ShowRewarded(ok =>
            {
                int reward = SaveData.ClaimDaily();
                if (reward <= 0) return;
                if (ok) SaveData.Coins += reward;
                Sfx.I.Play("reward");
                ui.Toast($"+{(ok ? reward * 2 : reward)} COINS", UIColors.Gold, true);
            });
        }

        /// <summary>Watch a rewarded ad in the shop for free coins (every few hours).</summary>
        public void FreeCoinsWithAd()
        {
            if (SaveData.FreeCoinsWait > System.TimeSpan.Zero || AdsManager.Instance == null) return;
            AdsManager.Instance.ShowRewarded(ok =>
            {
                if (!ok) return;
                SaveData.MarkFreeCoinsUsed();
                SaveData.Coins += SaveData.FreeCoinsAmount;
                Sfx.I.Play("reward");
                ui.Toast($"+{SaveData.FreeCoinsAmount} FREE COINS", UIColors.Gold, true);
            });
        }
    }
}
