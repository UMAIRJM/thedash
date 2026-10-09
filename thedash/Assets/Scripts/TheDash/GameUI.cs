using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheDash
{
    public class GameOverInfo
    {
        public int distance, best, coins;
        public bool newBest, canDouble;
    }

    /// <summary>Builds and drives every screen of the game.</summary>
    public class GameUI : MonoBehaviour
    {
        Game game;
        RectTransform canvasRt, safe;
        Rect lastSafe;

        // menu
        GameObject menu, menuTap;
        readonly List<RectTransform> holders = new List<RectTransform>();
        Text menuBest, menuCoins, tapToPlay, title;
        GameObject dailyBadge, missionsBadge;

        // hud
        GameObject hud;
        Text hudDistance, hudBest, hudCoins, hudSpeed, countdown, tutorial;
        float shownSpeed = -1;
        RectTransform hudCoinsPill;
        GameObject shieldInd, magnetInd;
        Image magnetFill;

        // modals
        class Modal
        {
            public GameObject root;
            public CanvasGroup group;
            public RectTransform card;
            public Action onBack;
        }

        readonly List<Modal> open = new List<Modal>();
        Modal pause, revive, gameOver, shop, missions, daily, settings;

        // revive
        Image reviveRing;
        Text reviveCount, reviveInfo;
        UIKit.ButtonRefs reviveAd, reviveCoins;

        // game over
        Text goTitle, goDistance, goBest, goCoins, goMissionHead;
        readonly float[] goMissionRows = new float[Missions.SlotCount];
        UIKit.ButtonRefs goDouble;
        readonly Text[] goMissionText = new Text[Missions.SlotCount];
        readonly Image[] goMissionFill = new Image[Missions.SlotCount];

        // shop / missions / daily / settings
        Text shopCoins;
        UIKit.ButtonRefs shopFree, dailyDouble;
        float freeTimer;
        readonly List<Action> shopRefreshers = new List<Action>();
        readonly Text[] missionText = new Text[Missions.SlotCount], missionCount = new Text[Missions.SlotCount], missionReward = new Text[Missions.SlotCount];
        readonly Image[] missionFill = new Image[Missions.SlotCount];
        readonly List<Action> dailyRefreshers = new List<Action>();
        UIKit.ButtonRefs dailyClaim;
        Text dailyNote;
        readonly List<Action> settingsRefreshers = new List<Action>();
        GameObject privacyButton;

        // toasts
        RectTransform toastRoot;
        readonly Queue<(string text, Color color, bool big)> toastQueue = new Queue<(string, Color, bool)>();
        float toastCooldown;
        readonly bool[] toastSlots = new bool[4];

        public bool AnyModalOpen => open.Count > 0;

        // ================================================================== build

        public void Build(Game g)
        {
            game = g;
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = 5;
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasRt = (RectTransform)canvasGo.transform;

            // menu's tap-catcher lives outside the safe area so it covers the whole screen
            menuTap = UIKit.Image(canvasRt, Art.Pixel, new Color(0, 0, 0, 0), "TapToPlay").gameObject;
            ((RectTransform)menuTap.transform).Fill();
            menuTap.GetComponent<Image>().raycastTarget = true;
            menuTap.AddComponent<Button>().onClick.AddListener(() => game.StartRun());
            safe = UIKit.Node("SafeArea", canvasRt);
            ApplySafeArea();
            BuildMenu();
            BuildHud();
            toastRoot = UIKit.Node("Toasts", safe).At(0.5f, 1f, 0, -230, 1200, 400);
            pause = BuildPause();
            revive = BuildRevive();
            gameOver = BuildGameOver();
            shop = BuildShop();
            missions = BuildMissions();
            daily = BuildDaily();
            settings = BuildSettings();

            SaveData.Changed += RefreshAll;
        }

        void OnDestroy() => SaveData.Changed -= RefreshAll;

        void ApplySafeArea()
        {
            Rect sa = Screen.safeArea;
            lastSafe = sa;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safe.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safe.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            foreach (var h in holders)
            {
                h.anchorMin = safe.anchorMin;
                h.anchorMax = safe.anchorMax;
            }
        }

        // ------------------------------------------------------------------ menu

        void BuildMenu()
        {
            var content = UIKit.Node("Menu", safe).Fill();
            menu = content.gameObject;
            menu.AddComponent<CanvasGroup>();

            title = UIKit.Label(content, "THE DASH", 210, Color.white);
            title.rectTransform.At(0.5f, 0.5f, 0, 210, 1400, 240);
            title.Glow(UIColors.Pink, 7f);

            var sub = UIKit.Label(content, "ENDLESS  NEON  RUN", 46, UIColors.Cyan);
            sub.rectTransform.At(0.5f, 0.5f, 0, 80, 1000, 60);

            tapToPlay = UIKit.Label(content, "TAP ANYWHERE TO PLAY", 64, Color.white);
            tapToPlay.rectTransform.At(0.5f, 0.5f, 0, -60, 1200, 90);

            menuBest = UIKit.Pill(content, Art.Icon("trophy"), UIColors.Gold, "0 m", 400, out var bestRt);
            bestRt.At(0, 1, 40, -36, 400, 92);

            UIKit.Button(content, "", "gear", UIColors.Button, 110, 110, () => Open(settings)).rt.At(1, 1, -36, -30, 110, 110);
            menuCoins = UIKit.Pill(content, Art.Coin, Color.white, "0", 300, out var coinRt);
            coinRt.At(1, 1, -170, -36, 300, 92);

            // anchored to the right so the buttons never cover the player (who stands at ~27% of the width)
            var row = UIKit.Node("Buttons", content).At(1, 0, -40, 175, 960, 130);
            var skins = UIKit.Button(row, "SKINS", "shop", new Color(0.85f, 0.25f, 0.75f), 300, 130, () => Open(shop), 48);
            skins.rt.At(0.5f, 0.5f, -330, 0, 300, 130);
            var miss = UIKit.Button(row, "MISSIONS", "target", new Color(0.2f, 0.55f, 0.95f), 300, 130, () => Open(missions), 42);
            miss.rt.At(0.5f, 0.5f, 0, 0, 300, 130);
            missionsBadge = UIKit.Badge(miss.rt);
            var day = UIKit.Button(row, "DAILY", "gift", new Color(0.95f, 0.6f, 0.15f), 300, 130, () => Open(daily), 48);
            day.rt.At(0.5f, 0.5f, 330, 0, 300, 130);
            dailyBadge = UIKit.Badge(day.rt);
        }

        public void ShowMenu()
        {
            menu.SetActive(true);
            menuTap.SetActive(true);
            hud.SetActive(false);
            RefreshAll();
            var cg = menu.GetComponent<CanvasGroup>();
            Tweener.Run(menu, 0.35f, k => cg.alpha = k);
            Tweener.Run(title, 0.6f, k => title.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, UIKit.OutBack(k)));
        }

        public void HideMenu()
        {
            menu.SetActive(false);
            menuTap.SetActive(false);
        }

        // ------------------------------------------------------------------ HUD

        void BuildHud()
        {
            hud = UIKit.Node("HUD", safe).Fill().gameObject;

            hudDistance = UIKit.Label(hud.transform, "0 m", 104, Color.white, TextAnchor.UpperLeft);
            hudDistance.rectTransform.At(0, 1, 44, -18, 700, 120);
            hudDistance.Glow(new Color(0.1f, 0.02f, 0.25f, 0.7f), 4f);
            hudBest = UIKit.Label(hud.transform, "BEST 0 m", 40, UIColors.TextSoft, TextAnchor.UpperLeft);
            hudBest.rectTransform.At(0, 1, 50, -134, 600, 50);
            hudSpeed = UIKit.Label(hud.transform, "SPEED x1.00", 40, UIColors.Cyan, TextAnchor.UpperLeft);
            hudSpeed.rectTransform.At(0, 1, 50, -182, 600, 50);

            hudCoins = UIKit.Pill(hud.transform, Art.Coin, Color.white, "0", 250, out hudCoinsPill);
            hudCoinsPill.At(0.5f, 1, 0, -30, 250, 92);

            UIKit.Button(hud.transform, "", "pause", new Color(0.25f, 0.15f, 0.5f, 0.9f), 120, 120, () => game.Pause()).rt.At(1, 1, -36, -26, 120, 120);

            shieldInd = PowerIndicator("shield", new Color(0.3f, 0.9f, 1f), -70, out _);
            magnetInd = PowerIndicator("magnet", new Color(1f, 0.35f, 0.45f), 70, out magnetFill);

            tutorial = UIKit.Label(hud.transform, "TAP TO JUMP   •   HOLD TO KEEP JUMPING", 54, Color.white);
            tutorial.rectTransform.At(0.5f, 0, 0, 120, 1500, 80);
            tutorial.Glow(new Color(0.1f, 0.02f, 0.25f, 0.8f), 3f);

            countdown = UIKit.Label(hud.transform, "", 240, Color.white);
            countdown.rectTransform.At(0.5f, 0.5f, 0, 40, 600, 300);
            countdown.Glow(UIColors.Pink, 6f);
            hud.SetActive(false);
        }

        GameObject PowerIndicator(string icon, Color c, float x, out Image fill)
        {
            var root = UIKit.Image(hud.transform, Art.Circle, new Color(0.05f, 0.02f, 0.14f, 0.7f), icon);
            root.rectTransform.At(0.5f, 1, x, -140, 110, 110);
            fill = UIKit.Image(root.transform, Art.Circle, c, "Fill");
            fill.rectTransform.Fill(-6, -6, -6, -6);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.transform.SetAsFirstSibling();
            var inner = UIKit.Image(root.transform, Art.Circle, new Color(0.08f, 0.04f, 0.2f, 1f), "Inner");
            inner.rectTransform.Fill(6, 6, 6, 6);
            var ic = UIKit.Image(root.transform, Art.Icon(icon), c, "Icon");
            ic.rectTransform.Fill(22, 22, 22, 22);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        public void ShowHud(int best)
        {
            hud.SetActive(true);
            HideMenu();
            hudBest.text = best > 0 ? $"BEST {best} m" : "";
            tutorial.gameObject.SetActive(!SaveData.TutorialDone);
            countdown.text = "";
        }

        public void UpdateHud(int distance, int coins, bool shield, float magnet01, float speedMultiplier)
        {
            float rounded = Mathf.Round(speedMultiplier * 100f) / 100f;
            if (!Mathf.Approximately(rounded, shownSpeed))
            {
                shownSpeed = rounded;
                hudSpeed.text = $"SPEED x{rounded:0.00}";
            }
            hudDistance.text = distance + " m";
            hudCoins.text = coins.ToString();
            shieldInd.SetActive(shield);
            magnetInd.SetActive(magnet01 > 0);
            if (magnet01 > 0) magnetFill.fillAmount = magnet01;
        }

        public void PunchCoins()
        {
            Tweener.Run(hudCoinsPill, 0.25f, k => hudCoinsPill.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(k * Mathf.PI)));
        }

        public void PunchSpeed()
        {
            var rt = hudSpeed.rectTransform;
            Tweener.Run(rt, 0.6f, k => rt.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(k * Mathf.PI)));
        }

        public void HideTutorial()
        {
            if (!tutorial.gameObject.activeSelf) return;
            Tweener.Run(tutorial, 0.4f, k => tutorial.color = new Color(1, 1, 1, 1 - k), () => tutorial.gameObject.SetActive(false));
        }

        public void SetCountdown(string text)
        {
            if (countdown.text == text) return;
            countdown.text = text;
            if (text != "")
                Tweener.Run(countdown, 0.3f, k => countdown.rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1.6f, 1f, UIKit.OutBack(k)));
        }

        public void HideHud() => hud.SetActive(false);

        // ------------------------------------------------------------------ toasts

        public void Toast(string text, Color color, bool big = false) => toastQueue.Enqueue((text, color, big));

        void ShowToast(string text, Color color, bool big)
        {
            int slot = 0;
            while (slot < toastSlots.Length - 1 && toastSlots[slot]) slot++;
            toastSlots[slot] = true;
            float baseY = -slot * 125f;
            var t = UIKit.Label(toastRoot, text, big ? 92 : 60, color);
            t.rectTransform.At(0.5f, 1f, 0, baseY, 1400, big ? 120 : 80);
            t.Glow(new Color(0.08f, 0.02f, 0.2f, 0.85f), big ? 5f : 4f);
            var rt = t.rectTransform;
            float hold = big ? 1.6f : 1.1f;
            Tweener.Run(t, 0.35f + hold + 0.35f, k =>
            {
                float total = 0.35f + hold + 0.35f;
                float time = k * total;
                float a = time < 0.35f ? time / 0.35f : time > 0.35f + hold ? 1f - (time - 0.35f - hold) / 0.35f : 1f;
                float s = time < 0.35f ? Mathf.LerpUnclamped(0.6f, 1f, UIKit.OutBack(time / 0.35f)) : 1f;
                rt.localScale = Vector3.one * s;
                rt.anchoredPosition = new Vector2(0, baseY - time * 12f);
                var c = t.color;
                c.a = a;
                t.color = c;
            }, () =>
            {
                toastSlots[slot] = false;
                Destroy(t.gameObject);
            });
        }

        // ------------------------------------------------------------------ modals

        Modal MakeModal(string name, float w, float h, string titleText, bool closable, Action onBack)
        {
            var m = new Modal();
            m.root = UIKit.Node(name, canvasRt).Fill().gameObject;
            m.group = m.root.AddComponent<CanvasGroup>();
            UIKit.Dim(m.root.transform);
            var holder = UIKit.Node("Holder", m.root.transform);
            holder.anchorMin = safe.anchorMin;
            holder.anchorMax = safe.anchorMax;
            holder.offsetMin = holder.offsetMax = Vector2.zero;
            holders.Add(holder);

            var glow = UIKit.Panel(holder, new Color(UIColors.Pink.r, UIColors.Pink.g, UIColors.Pink.b, 0.55f), 1f, "Card");
            glow.rectTransform.At(0.5f, 0.5f, 0, 0, w, h);
            var card = UIKit.Panel(glow.transform, UIColors.Panel, 1f, "Inner");
            card.rectTransform.Fill(5, 5, 5, 5);
            card.raycastTarget = true;
            m.card = glow.rectTransform;

            if (!string.IsNullOrEmpty(titleText))
            {
                var t = UIKit.Label(card.transform, titleText, 84, Color.white);
                t.rectTransform.At(0.5f, 1, 0, -30, w - 200, 110);
                t.Glow(UIColors.Pink, 4f);
            }
            if (closable)
                UIKit.Button(card.transform, "", "close", UIColors.Red, 96, 96, () => Close(m)).rt.At(1, 1, -24, -24, 96, 96);
            m.onBack = onBack ?? (() => Close(m));
            m.root.SetActive(false);
            return m;
        }

        RectTransform CardContent(Modal m) => (RectTransform)m.card.GetChild(0);

        void Open(Modal m)
        {
            RefreshAll();
            if (open.Contains(m)) return;
            open.Add(m);
            m.root.SetActive(true);
            m.root.transform.SetAsLastSibling();
            Tweener.Run(m, 0.3f, k =>
            {
                m.group.alpha = Mathf.Clamp01(k * 2f);
                m.card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, UIKit.OutBack(k));
            });
        }

        void Close(Modal m)
        {
            if (!open.Contains(m)) return;
            open.Remove(m);
            Tweener.Run(m, 0.15f, k =>
            {
                m.group.alpha = 1f - k;
                m.card.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, k);
            }, () => m.root.SetActive(false));
        }

        public void CloseAll()
        {
            foreach (var m in open.ToArray()) Close(m);
        }

        /// <summary>Android back button. Returns true if the UI consumed it.</summary>
        public bool HandleBack()
        {
            if (open.Count == 0) return false;
            open[open.Count - 1].onBack?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ pause

        Modal BuildPause()
        {
            var m = MakeModal("Pause", 820, 560, "PAUSED", false, () => game.Resume());
            var c = CardContent(m);
            UIKit.Button(c, "RESUME", "play", new Color(0.2f, 0.75f, 0.45f), 500, 150, () => game.Resume(), 64).rt.At(0.5f, 0.5f, 0, 10, 500, 150);
            UIKit.Button(c, "", "home", UIColors.Button, 130, 130, () => game.GoHome()).rt.At(0.5f, 0, -170, 50, 130, 130);
            UIKit.Button(c, "", "gear", UIColors.Button, 130, 130, () => Open(settings)).rt.At(0.5f, 0, 0, 50, 130, 130);
            UIKit.Button(c, "", "retry", UIColors.Button, 130, 130, () => game.Restart()).rt.At(0.5f, 0, 170, 50, 130, 130);
            return m;
        }

        public void ShowPause() => Open(pause);
        public void HidePause() => Close(pause);

        // ------------------------------------------------------------------ revive

        Modal BuildRevive()
        {
            var m = MakeModal("Revive", 900, 760, "SO CLOSE!", false, () => game.DeclineRevive());
            var c = CardContent(m);
            reviveInfo = UIKit.Label(c, "", 46, UIColors.TextSoft);
            reviveInfo.rectTransform.At(0.5f, 1, 0, -140, 800, 60);

            var ringBg = UIKit.Image(c, Art.Circle, new Color(1, 1, 1, 0.08f), "RingBg");
            ringBg.rectTransform.At(0.5f, 1, 0, -215, 170, 170);
            reviveRing = UIKit.Image(ringBg.transform, Art.Circle, UIColors.Cyan, "Ring");
            reviveRing.rectTransform.Fill();
            reviveRing.type = Image.Type.Filled;
            reviveRing.fillMethod = Image.FillMethod.Radial360;
            reviveRing.fillOrigin = (int)Image.Origin360.Top;
            var hole = UIKit.Image(ringBg.transform, Art.Circle, UIColors.Panel, "Hole");
            hole.rectTransform.Fill(14, 14, 14, 14);
            reviveCount = UIKit.Label(ringBg.transform, "5", 90, Color.white);
            reviveCount.rectTransform.Fill();

            reviveAd = UIKit.Button(c, "CONTINUE", "video", new Color(0.2f, 0.75f, 0.45f), 560, 130, () => game.ReviveWithAd(), 58);
            reviveAd.rt.At(0.5f, 0, 0, 230, 560, 130);
            reviveCoins = UIKit.Button(c, "150", "coin", new Color(0.95f, 0.6f, 0.15f), 560, 130, () => game.ReviveWithCoins(), 58);
            reviveCoins.rt.At(0.5f, 0, 0, 230, 560, 130);
            var skip = UIKit.Button(c, "NO THANKS", "", new Color(0.3f, 0.25f, 0.45f), 360, 96, () => game.DeclineRevive(), 42);
            skip.rt.At(0.5f, 0, 0, 70, 360, 96);
            return m;
        }

        public void ShowRevive(int distance, int cost, bool canAd, bool canCoins)
        {
            reviveInfo.text = $"You ran {distance} m - keep going?";
            reviveAd.rt.gameObject.SetActive(canAd);
            reviveCoins.rt.gameObject.SetActive(canCoins);
            reviveCoins.label.text = cost.ToString();
            // if both are available, put them side by side
            if (canAd && canCoins)
            {
                reviveAd.rt.At(0.5f, 0, -150, 230, 380, 130);
                reviveCoins.rt.At(0.5f, 0, 210, 230, 300, 130);
                reviveAd.label.text = "FREE";
            }
            else
            {
                reviveAd.rt.At(0.5f, 0, 0, 230, 560, 130);
                reviveCoins.rt.At(0.5f, 0, 0, 230, 560, 130);
                reviveAd.label.text = "CONTINUE";
            }
            Open(revive);
        }

        public void UpdateReviveTimer(float remaining01, float seconds)
        {
            reviveRing.fillAmount = remaining01;
            reviveCount.text = Mathf.CeilToInt(seconds).ToString();
        }

        public void HideRevive() => Close(revive);

        // ------------------------------------------------------------------ game over

        Modal BuildGameOver()
        {
            var m = MakeModal("GameOver", 1150, 960, "", false, () => game.GoHome());
            var c = CardContent(m);
            goTitle = UIKit.Label(c, "GAME OVER", 88, Color.white);
            goTitle.rectTransform.At(0.5f, 1, 0, -26, 1000, 110);
            goTitle.Glow(UIColors.Pink, 4f);
            goDistance = UIKit.Label(c, "0 m", 150, Color.white);
            goDistance.rectTransform.At(0.5f, 1, 0, -130, 1000, 170);
            goDistance.Glow(UIColors.Cyan, 5f);

            goBest = UIKit.Label(c, "BEST 0 m", 48, UIColors.TextSoft);
            goBest.rectTransform.At(0.5f, 1, -200, -310, 440, 70);
            var coinIcon = UIKit.Image(c, Art.Coin, Color.white, "CoinIcon");
            coinIcon.rectTransform.At(0.5f, 1, 130, -306, 80, 80);
            goCoins = UIKit.Label(c, "+0", 56, UIColors.Gold, TextAnchor.MiddleLeft);
            goCoins.rectTransform.At(0.5f, 1, 280, -310, 200, 70);

            goDouble = UIKit.Button(c, "x2 COINS", "video", new Color(0.95f, 0.6f, 0.15f), 420, 96, () => game.DoubleCoinsWithAd(), 46);
            goDouble.rt.At(0.5f, 1, 0, -385, 420, 96);

            goMissionHead = UIKit.Label(c, "MISSIONS", 40, UIColors.Cyan);
            goMissionHead.rectTransform.At(0.5f, 1, 0, -495, 600, 50);
            for (int i = 0; i < Missions.SlotCount; i++)
            {
                float y = -550 - i * 62;
                goMissionRows[i] = y;
                goMissionText[i] = UIKit.Label(c, "", 36, Color.white, TextAnchor.MiddleLeft);
                goMissionText[i].rectTransform.At(0.5f, 1, -460, y, 620, 54);
                goMissionText[i].rectTransform.pivot = new Vector2(0, 1);
                var bar = UIKit.Bar(c, new Color(1, 1, 1, 0.1f), UIColors.Green, 30);
                bar.bg.rectTransform.At(0.5f, 1, 300, y - 12, 340, 30);
                goMissionFill[i] = bar.fill;
            }

            UIKit.Button(c, "", "home", UIColors.Button, 140, 140, () => game.GoHome()).rt.At(0.5f, 0, -360, 40, 140, 140);
            UIKit.Button(c, "RETRY", "retry", new Color(0.2f, 0.75f, 0.45f), 470, 140, () => game.Restart(), 66).rt.At(0.5f, 0, 0, 40, 470, 140);
            UIKit.Button(c, "", "shop", new Color(0.85f, 0.25f, 0.75f), 140, 140, () => Open(shop)).rt.At(0.5f, 0, 360, 40, 140, 140);
            return m;
        }

        public void ShowGameOver(GameOverInfo info)
        {
            goTitle.text = info.newBest ? "NEW BEST!" : "GAME OVER";
            goTitle.color = info.newBest ? UIColors.Gold : Color.white;
            goBest.text = $"BEST {info.best} m";
            goCoins.text = "+" + info.coins;
            goDouble.rt.gameObject.SetActive(info.canDouble);
            LayoutGameOver(info.canDouble);
            RefreshGameOverMissions();
            Open(gameOver);
            // count the distance up for a satisfying reveal
            Tweener.Run(goDistance, 0.8f, k => goDistance.text = Mathf.RoundToInt(info.distance * UIKit.OutCubic(k)) + " m", null, 0.15f);
            goDistance.text = "0 m";
        }

        public void SetGameOverCoins(int coins, bool canDouble)
        {
            goCoins.text = "+" + coins;
            goDouble.rt.gameObject.SetActive(canDouble);
            Tweener.Run(goCoins, 0.4f, k => goCoins.rectTransform.localScale = Vector3.one * (1f + 0.4f * Mathf.Sin(k * Mathf.PI)));
        }

        /// <summary>Without the x2 button, pull the missions up so there's no empty gap.</summary>
        void LayoutGameOver(bool withDouble)
        {
            float shift = withDouble ? 0 : 55;
            goMissionHead.rectTransform.anchoredPosition = new Vector2(0, -495 + shift);
            for (int i = 0; i < Missions.SlotCount; i++)
            {
                float y = goMissionRows[i] + shift;
                goMissionText[i].rectTransform.anchoredPosition = new Vector2(-460, y);
                ((RectTransform)goMissionFill[i].transform.parent).anchoredPosition = new Vector2(300, y - 12);
            }
        }

        void RefreshGameOverMissions()
        {
            for (int i = 0; i < Missions.SlotCount; i++)
            {
                var ms = Missions.Active[i];
                goMissionText[i].text = ms.Description;
                UIKit.SetBar(goMissionFill[i], ms.Progress01);
            }
        }

        public void HideGameOver() => Close(gameOver);

        // ------------------------------------------------------------------ shop

        Modal BuildShop()
        {
            var m = MakeModal("Shop", 1400, 900, "SKINS", true, null);
            var c = CardContent(m);
            shopCoins = UIKit.Pill(c, Art.Coin, Color.white, "0", 280, out var pill);
            pill.At(0, 1, 30, -38, 280, 92);
            shopFree = UIKit.Button(c, "+" + SaveData.FreeCoinsAmount + " FREE", "video", new Color(0.95f, 0.6f, 0.15f), 320, 92, () => game.FreeCoinsWithAd(), 40);
            shopFree.rt.At(1, 1, -140, -38, 320, 92);

            var grid = UIKit.Node("Grid", c).At(0.5f, 0.5f, 0, -60, 1300, 660);
            for (int i = 0; i < Skin.All.Length; i++)
            {
                int idx = i;
                var skin = Skin.All[i];
                float x = -487.5f + (i % 4) * 325f, y = i < 4 ? 165f : -165f;
                var card = UIKit.Panel(grid, UIColors.PanelLight, 1.4f, skin.name);
                card.rectTransform.At(0.5f, 0.5f, x, y, 300, 310);

                var glow = UIKit.Image(card.transform, Art.SoftDot, new Color(skin.body.r, skin.body.g, skin.body.b, 0.5f), "Glow");
                glow.rectTransform.At(0.5f, 1, 0, -6, 200, 160);
                var preview = UIKit.Image(card.transform, Art.Player(i), Color.white, "Preview");
                preview.rectTransform.At(0.5f, 1, 0, -30, 108, 108);
                var name = UIKit.Label(card.transform, skin.name, 38, Color.white);
                name.rectTransform.At(0.5f, 1, 0, -148, 280, 48);

                var btn = UIKit.Button(card.transform, "", "coin", UIColors.Button, 250, 84, () => game.BuyOrSelectSkin(idx), 40);
                btn.rt.At(0.5f, 0, 0, 16, 250, 84);
                var lbl = UIKit.Label(btn.icon.transform.parent, "", 40, Color.white);
                var lockIcon = UIKit.Image(card.transform, Art.Icon("lock"), new Color(1, 1, 1, 0.85f), "Lock");
                lockIcon.rectTransform.At(1, 1, -14, -14, 48, 48);

                shopRefreshers.Add(() =>
                {
                    bool owned = SaveData.Owns(idx), selected = SaveData.SelectedSkin == idx;
                    btn.icon.gameObject.SetActive(!owned);
                    lockIcon.gameObject.SetActive(!owned);
                    lbl.text = selected ? "EQUIPPED" : owned ? "SELECT" : skin.price.ToString();
                    bool afford = SaveData.Coins >= skin.price;
                    btn.bg.color = selected ? new Color(0.2f, 0.75f, 0.45f) : owned ? new Color(0.2f, 0.55f, 0.95f)
                        : afford ? new Color(0.95f, 0.6f, 0.15f) : new Color(0.35f, 0.3f, 0.45f);
                    btn.rt.Find("Lip").GetComponent<Image>().color = Color.Lerp(btn.bg.color, Color.black, 0.45f);
                    card.color = selected ? new Color(0.25f, 0.18f, 0.5f) : UIColors.PanelLight;
                });
            }
            return m;
        }

        public void PunchShopItem(int idx) => Sfx.I.Play("power");

        // ------------------------------------------------------------------ missions

        Modal BuildMissions()
        {
            var m = MakeModal("Missions", 1200, 760, "MISSIONS", true, null);
            var c = CardContent(m);
            for (int i = 0; i < Missions.SlotCount; i++)
            {
                var row = UIKit.Panel(c, UIColors.PanelLight, 1.4f, "Mission" + i);
                row.rectTransform.At(0.5f, 1, 0, -150 - i * 172, 1080, 155);
                missionText[i] = UIKit.Label(row.transform, "", 46, Color.white, TextAnchor.MiddleLeft);
                missionText[i].rectTransform.At(0, 1, 36, -14, 760, 64);
                var bar = UIKit.Bar(row.transform, new Color(1, 1, 1, 0.1f), UIColors.Green, 36);
                bar.bg.rectTransform.At(0, 0, 36, 28, 600, 36);
                missionFill[i] = bar.fill;
                missionCount[i] = UIKit.Label(row.transform, "", 38, UIColors.TextSoft, TextAnchor.MiddleLeft);
                missionCount[i].rectTransform.At(0, 0, 660, 22, 200, 50);
                var coin = UIKit.Image(row.transform, Art.Coin, Color.white, "Coin");
                coin.rectTransform.At(1, 0.5f, -150, 0, 90, 90);
                missionReward[i] = UIKit.Label(row.transform, "", 54, UIColors.Gold, TextAnchor.MiddleLeft);
                missionReward[i].rectTransform.At(1, 0.5f, -20, 0, 130, 80);
            }
            var note = UIKit.Label(c, "Complete missions to earn coins - new ones appear automatically!", 34, UIColors.TextSoft);
            note.rectTransform.At(0.5f, 0, 0, 22, 1100, 50);
            return m;
        }

        void RefreshMissions()
        {
            Missions.Load();
            for (int i = 0; i < Missions.SlotCount; i++)
            {
                var ms = Missions.Active[i];
                missionText[i].text = ms.Description;
                missionCount[i].text = ms.PerRun ? $"best: {ms.progress}" : $"{ms.progress} / {ms.Target}";
                missionReward[i].text = "+" + ms.Reward;
                UIKit.SetBar(missionFill[i], ms.Progress01);
            }
        }

        // ------------------------------------------------------------------ daily

        Modal BuildDaily()
        {
            var m = MakeModal("Daily", 1340, 700, "DAILY REWARD", true, null);
            var c = CardContent(m);
            int n = SaveData.DailyRewards.Length;
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                float x = -(n - 1) * 85f + i * 170f;
                var tile = UIKit.Panel(c, UIColors.PanelLight, 1.6f, "Day" + i);
                tile.rectTransform.At(0.5f, 0.5f, x, 40, 156, 240);
                var dayLbl = UIKit.Label(tile.transform, "DAY " + (i + 1), 34, UIColors.TextSoft);
                dayLbl.rectTransform.At(0.5f, 1, 0, -12, 150, 44);
                var coin = UIKit.Image(tile.transform, Art.Coin, Color.white, "Coin");
                coin.rectTransform.At(0.5f, 0.5f, 0, 6, i == n - 1 ? 110 : 90, i == n - 1 ? 110 : 90);
                var amt = UIKit.Label(tile.transform, SaveData.DailyRewards[i].ToString(), 44, UIColors.Gold);
                amt.rectTransform.At(0.5f, 0, 0, 14, 150, 54);
                var check = UIKit.Image(tile.transform, Art.Icon("check"), UIColors.Green, "Check");
                check.rectTransform.At(0.5f, 0.5f, 0, 6, 100, 100);
                dailyRefreshers.Add(() =>
                {
                    int today = SaveData.DailyIndex;
                    bool claimed = idx < today || (idx == today && !SaveData.DailyAvailable);
                    bool isToday = idx == today && SaveData.DailyAvailable;
                    check.gameObject.SetActive(claimed);
                    coin.color = claimed ? new Color(1, 1, 1, 0.3f) : Color.white;
                    tile.color = isToday ? new Color(0.45f, 0.25f, 0.75f) : UIColors.PanelLight;
                    tile.rectTransform.localScale = Vector3.one * (isToday ? 1.08f : 1f);
                });
            }
            dailyClaim = UIKit.Button(c, "CLAIM", "gift", new Color(0.2f, 0.75f, 0.45f), 460, 130, () => game.ClaimDaily(), 60);
            dailyClaim.rt.At(0.5f, 0, 0, 50, 460, 130);
            dailyDouble = UIKit.Button(c, "CLAIM x2", "video", new Color(0.95f, 0.6f, 0.15f), 420, 130, () => game.ClaimDailyDoubled(), 56);
            dailyDouble.rt.At(0.5f, 0, 230, 50, 420, 130);
            dailyNote = UIKit.Label(c, "Come back tomorrow for more!", 44, UIColors.TextSoft);
            dailyNote.rectTransform.At(0.5f, 0, 0, 90, 1000, 60);
            return m;
        }

        public void OpenDaily() => Open(daily);

        // ------------------------------------------------------------------ settings

        Modal BuildSettings()
        {
            var m = MakeModal("Settings", 900, 760, "SETTINGS", true, null);
            var c = CardContent(m);
            Toggle(c, "MUSIC", "music", -170, () => SaveData.Music, v => { SaveData.Music = v; Sfx.I.ApplySettings(); });
            Toggle(c, "SOUND", "sound", -300, () => SaveData.Sfx, v => SaveData.Sfx = v);
            Toggle(c, "VIBRATION", "vibrate", -430, () => SaveData.Vibration, v => SaveData.Vibration = v);

            var policy = UIKit.Button(c, "PRIVACY POLICY", "", UIColors.Button, 370, 90, () => Application.OpenURL(Game.PrivacyPolicyUrl), 36);
            policy.rt.At(0.5f, 0, -195, 60, 370, 90);
            policy.rt.gameObject.SetActive(!string.IsNullOrEmpty(Game.PrivacyPolicyUrl));
            var privacy = UIKit.Button(c, "AD PRIVACY", "", UIColors.Button, 370, 90, () => AdsManager.Instance?.ShowPrivacyOptions(), 36);
            privacy.rt.At(0.5f, 0, 195, 60, 370, 90);
            privacyButton = privacy.rt.gameObject;
            var ver = UIKit.Label(c, "v" + Application.version, 28, new Color(1, 1, 1, 0.35f));
            ver.rectTransform.At(0.5f, 0, 0, 14, 400, 36);
            return m;
        }

        void Toggle(RectTransform parent, string label, string icon, float y, Func<bool> get, Action<bool> set)
        {
            var row = UIKit.Panel(parent, UIColors.PanelLight, 1.4f, label);
            row.rectTransform.At(0.5f, 1, 0, y, 780, 110);
            var ic = UIKit.Image(row.transform, Art.Icon(icon), UIColors.Cyan, "Icon");
            ic.rectTransform.At(0, 0.5f, 26, 0, 70, 70);
            var t = UIKit.Label(row.transform, label, 50, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.At(0, 0.5f, 120, 0, 400, 80);
            UIKit.ButtonRefs btn = null;
            btn = UIKit.Button(row.transform, "ON", "", new Color(0.2f, 0.75f, 0.45f), 200, 86, () =>
            {
                set(!get());
                RefreshAll();
            }, 44);
            btn.rt.At(1, 0.5f, -18, 0, 200, 86);
            settingsRefreshers.Add(() =>
            {
                bool on = get();
                btn.label.text = on ? "ON" : "OFF";
                btn.bg.color = on ? new Color(0.2f, 0.75f, 0.45f) : new Color(0.4f, 0.32f, 0.5f);
                btn.rt.Find("Lip").GetComponent<Image>().color = Color.Lerp(btn.bg.color, Color.black, 0.45f);
            });
        }

        // ------------------------------------------------------------------ refresh / update

        public void RefreshAll()
        {
            if (menuBest == null) return;
            menuBest.text = SaveData.Best + " m";
            menuCoins.text = SaveData.Coins.ToString();
            shopCoins.text = SaveData.Coins.ToString();
            dailyBadge.SetActive(SaveData.DailyAvailable);
            Missions.Load();
            bool anyClose = false;
            foreach (var ms in Missions.Active) if (ms.Progress01 >= 0.75f) anyClose = true;
            missionsBadge.SetActive(anyClose);
            foreach (var r in shopRefreshers) r();
            foreach (var r in dailyRefreshers) r();
            foreach (var r in settingsRefreshers) r();
            RefreshMissions();
            bool adReady = AdsManager.Instance != null && AdsManager.Instance.RewardedReady;
            dailyClaim.rt.gameObject.SetActive(SaveData.DailyAvailable);
            dailyDouble.rt.gameObject.SetActive(SaveData.DailyAvailable && adReady);
            // with the x2 option available, put both buttons side by side
            dailyClaim.rt.anchoredPosition = new Vector2(dailyDouble.rt.gameObject.activeSelf ? -230 : 0, 50);
            dailyNote.gameObject.SetActive(!SaveData.DailyAvailable);
            RefreshFreeCoins();
            privacyButton.SetActive(AdsManager.Instance != null && AdsManager.Instance.PrivacyOptionsRequired);
        }

        /// <summary>Shop "free coins" button: shows a countdown while on cooldown, hidden if no ad is ready.</summary>
        void RefreshFreeCoins()
        {
            var wait = SaveData.FreeCoinsWait;
            bool adReady = AdsManager.Instance != null && AdsManager.Instance.RewardedReady;
            shopFree.rt.gameObject.SetActive(adReady || wait > System.TimeSpan.Zero);
            bool ready = wait <= System.TimeSpan.Zero;
            shopFree.button.interactable = ready;
            shopFree.label.text = ready ? "+" + SaveData.FreeCoinsAmount + " FREE" : $"{(int)wait.TotalHours}h {wait.Minutes:00}m";
            shopFree.icon.gameObject.SetActive(ready);
            shopFree.bg.color = ready ? new Color(0.95f, 0.6f, 0.15f) : new Color(0.35f, 0.3f, 0.45f);
            shopFree.rt.Find("Lip").GetComponent<Image>().color = Color.Lerp(shopFree.bg.color, Color.black, 0.45f);
        }

        void Update()
        {
            if (Screen.safeArea != lastSafe) ApplySafeArea();

            // keep the shop countdown and ad-dependent buttons fresh while a menu is open
            freeTimer -= Time.unscaledDeltaTime;
            if (freeTimer <= 0f && open.Count > 0)
            {
                freeTimer = 1f;
                RefreshFreeCoins();
                bool adReady = AdsManager.Instance != null && AdsManager.Instance.RewardedReady;
                dailyDouble.rt.gameObject.SetActive(SaveData.DailyAvailable && adReady);
                dailyClaim.rt.anchoredPosition = new Vector2(dailyDouble.rt.gameObject.activeSelf ? -230 : 0, 50);
            }

            if (menu.activeSelf && tapToPlay != null)
            {
                float p = Mathf.Sin(Time.unscaledTime * 4f);
                tapToPlay.color = new Color(1, 1, 1, 0.65f + 0.35f * p);
                tapToPlay.rectTransform.localScale = Vector3.one * (1f + 0.04f * p);
                title.rectTransform.anchoredPosition = new Vector2(0, 210 + Mathf.Sin(Time.unscaledTime * 1.6f) * 10f);
            }

            toastCooldown -= Time.unscaledDeltaTime;
            if (toastCooldown <= 0 && toastQueue.Count > 0)
            {
                var t = toastQueue.Dequeue();
                ShowToast(t.text, t.color, t.big);
                toastCooldown = 0.7f;
            }
        }
    }
}
