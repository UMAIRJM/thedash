using System;
using UnityEngine;

namespace TheDash
{
    public struct Skin
    {
        public string name;
        public Color body, accent;
        public int price;

        public Skin(string name, string body, string accent, int price)
        {
            this.name = name;
            ColorUtility.TryParseHtmlString(body, out this.body);
            ColorUtility.TryParseHtmlString(accent, out this.accent);
            this.price = price;
        }

        public static readonly Skin[] All =
        {
            new Skin("DASHER",  "#3DF2FF", "#FF4FD8", 0),
            new Skin("BUBBLE",  "#FF7AC8", "#FFE45E", 150),
            new Skin("LIME",    "#8CFF5A", "#2B9BFF", 300),
            new Skin("SOLAR",   "#FFC145", "#FF3D3D", 500),
            new Skin("VIOLET",  "#B57BFF", "#3DFFD0", 800),
            new Skin("FROST",   "#E8F6FF", "#5AA9FF", 1200),
            new Skin("INFERNO", "#FF4B2B", "#FFD000", 2000),
            new Skin("GOLDEN",  "#FFD700", "#FFFFFF", 3000),
        };
    }

    /// <summary>All persistent progress lives here (PlayerPrefs backed).</summary>
    public static class SaveData
    {
        const string KBest = "td_best", KCoins = "td_coins", KSkin = "td_skin", KOwned = "td_owned",
                     KMusic = "td_music", KSfx = "td_sfx", KVibe = "td_vibe", KRuns = "td_runs",
                     KDailyDay = "td_daily_day", KDailyStreak = "td_daily_streak", KTutorial = "td_tutorial";

        public static event Action Changed;

        public static int Best
        {
            get => PlayerPrefs.GetInt(KBest, LegacyBest());
            set { PlayerPrefs.SetInt(KBest, value); Save(); }
        }

        // The original version stored the high score under "score" - carry it over.
        static int LegacyBest() => PlayerPrefs.GetInt("score", 0);

        public static int Coins
        {
            get => PlayerPrefs.GetInt(KCoins, 0);
            set { PlayerPrefs.SetInt(KCoins, Mathf.Max(0, value)); Save(); }
        }

        public static int SelectedSkin
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(KSkin, 0), 0, Skin.All.Length - 1);
            set { PlayerPrefs.SetInt(KSkin, value); Save(); }
        }

        public static bool Owns(int skin) => skin == 0 || (PlayerPrefs.GetInt(KOwned, 1) & (1 << skin)) != 0;

        public static void Unlock(int skin)
        {
            PlayerPrefs.SetInt(KOwned, PlayerPrefs.GetInt(KOwned, 1) | (1 << skin));
            Save();
        }

        public static bool Music
        {
            get => PlayerPrefs.GetInt(KMusic, PlayerPrefs.GetInt("mute", 0) == 1 ? 0 : 1) == 1;
            set { PlayerPrefs.SetInt(KMusic, value ? 1 : 0); Save(); }
        }

        public static bool Sfx
        {
            get => PlayerPrefs.GetInt(KSfx, 1) == 1;
            set { PlayerPrefs.SetInt(KSfx, value ? 1 : 0); Save(); }
        }

        public static bool Vibration
        {
            get => PlayerPrefs.GetInt(KVibe, 1) == 1;
            set { PlayerPrefs.SetInt(KVibe, value ? 1 : 0); Save(); }
        }

        public static int RunsPlayed
        {
            get => PlayerPrefs.GetInt(KRuns, 0);
            set => PlayerPrefs.SetInt(KRuns, value);
        }

        public static bool TutorialDone
        {
            get => PlayerPrefs.GetInt(KTutorial, 0) == 1;
            set { PlayerPrefs.SetInt(KTutorial, value ? 1 : 0); Save(); }
        }

        // ---------- Daily reward ----------
        public static readonly int[] DailyRewards = { 25, 40, 60, 80, 100, 150, 300 };

        // ---------- Free coins (watch an ad in the shop, every few hours) ----------
        public const int FreeCoinsAmount = 50;
        public static readonly TimeSpan FreeCoinsCooldown = TimeSpan.FromHours(3);
        const string KFreeCoins = "td_free_coins_at";

        /// <summary>Time left until the free-coins ad can be watched again (zero = ready).</summary>
        public static TimeSpan FreeCoinsWait
        {
            get
            {
                if (!long.TryParse(PlayerPrefs.GetString(KFreeCoins, "0"), out long ticks)) return TimeSpan.Zero;
                var left = new DateTime(ticks, DateTimeKind.Utc) + FreeCoinsCooldown - DateTime.UtcNow;
                return left > TimeSpan.Zero && left <= FreeCoinsCooldown ? left : TimeSpan.Zero;
            }
        }

        public static void MarkFreeCoinsUsed()
        {
            PlayerPrefs.SetString(KFreeCoins, DateTime.UtcNow.Ticks.ToString());
            Save();
        }

        static int Today => (int)(DateTime.Now.Date - new DateTime(2024, 1, 1)).TotalDays;

        public static bool DailyAvailable => PlayerPrefs.GetInt(KDailyDay, -1) != Today;

        /// <summary>Index 0..6 of the reward that would be claimed today.</summary>
        public static int DailyIndex
        {
            get
            {
                int last = PlayerPrefs.GetInt(KDailyDay, -1);
                int streak = PlayerPrefs.GetInt(KDailyStreak, 0);
                if (last == Today) return Mathf.Max(0, streak - 1) % DailyRewards.Length;
                if (last == Today - 1) return streak % DailyRewards.Length;
                return 0; // streak broken (or first time)
            }
        }

        public static int ClaimDaily()
        {
            if (!DailyAvailable) return 0;
            int idx = DailyIndex;
            int reward = DailyRewards[idx];
            PlayerPrefs.SetInt(KDailyStreak, idx + 1);
            PlayerPrefs.SetInt(KDailyDay, Today);
            Coins += reward;
            return reward;
        }

        public static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
