using System;
using UnityEngine;

namespace TheDash
{
    public enum MissionType { RunDistance, TotalDistance, RunCoins, TotalCoins, Jumps, Runs, Powerups, PerfectArcs }

    public class Mission
    {
        public int slot;
        public MissionType type;
        public int level;
        public int progress;

        public bool PerRun => type == MissionType.RunDistance || type == MissionType.RunCoins;

        public int Target
        {
            get
            {
                switch (type)
                {
                    case MissionType.RunDistance: return 250 + 250 * level;
                    case MissionType.TotalDistance: return 1000 + 1000 * level;
                    case MissionType.RunCoins: return 15 + 10 * level;
                    case MissionType.TotalCoins: return 75 + 75 * level;
                    case MissionType.Jumps: return 50 + 50 * level;
                    case MissionType.Runs: return 3 + 2 * level;
                    case MissionType.Powerups: return 3 + 2 * level;
                    default: return 3 + 2 * level;
                }
            }
        }

        public int Reward => 30 + 20 * Mathf.Min(level, 20);

        public string Description
        {
            get
            {
                int t = Target;
                switch (type)
                {
                    case MissionType.RunDistance: return $"Run {t} m in one go";
                    case MissionType.TotalDistance: return $"Run {t} m in total";
                    case MissionType.RunCoins: return $"Grab {t} coins in one run";
                    case MissionType.TotalCoins: return $"Collect {t} coins";
                    case MissionType.Jumps: return $"Jump {t} times";
                    case MissionType.Runs: return $"Play {t} runs";
                    case MissionType.Powerups: return $"Pick up {t} power-ups";
                    default: return $"Clear {t} perfect coin arcs";
                }
            }
        }

        public float Progress01 => Mathf.Clamp01(progress / (float)Target);
    }

    /// <summary>Three rotating objectives that reward coins - gives players a reason for "one more run".</summary>
    public static class Missions
    {
        public const int SlotCount = 3;
        public static readonly Mission[] Active = new Mission[SlotCount];
        public static event Action<Mission> Completed;

        static bool loaded;

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            for (int i = 0; i < SlotCount; i++)
            {
                var m = new Mission
                {
                    slot = i,
                    type = (MissionType)PlayerPrefs.GetInt($"td_m{i}_type", i * 2),
                    level = PlayerPrefs.GetInt($"td_m{i}_lvl", 0),
                    progress = PlayerPrefs.GetInt($"td_m{i}_prog", 0),
                };
                Active[i] = m;
            }
        }

        public static void BeginRun()
        {
            Load();
            foreach (var m in Active)
                if (m.PerRun) m.progress = 0;
        }

        /// <summary>Cumulative missions add <paramref name="value"/>; per-run missions take the run total.</summary>
        public static void Report(MissionType type, int value)
        {
            Load();
            for (int i = 0; i < SlotCount; i++)
            {
                var m = Active[i];
                if (m.type != type) continue;
                m.progress = m.PerRun ? Mathf.Max(m.progress, value) : m.progress + value;
                if (m.progress >= m.Target) Complete(m);
                else if (!m.PerRun) Persist(m);
            }
        }

        static void Complete(Mission m)
        {
            SaveData.Coins += m.Reward;
            Completed?.Invoke(m);

            // Replace with a new mission type not used by the other slots, one level harder.
            int count = Enum.GetValues(typeof(MissionType)).Length;
            MissionType next = m.type;
            for (int tries = 0; tries < 20; tries++)
            {
                next = (MissionType)UnityEngine.Random.Range(0, count);
                bool taken = false;
                foreach (var o in Active) if (o != m && o.type == next) taken = true;
                if (!taken && next != m.type) break;
            }
            Active[m.slot] = new Mission { slot = m.slot, type = next, level = m.level + 1, progress = 0 };
            Persist(Active[m.slot]);
        }

        static void Persist(Mission m)
        {
            PlayerPrefs.SetInt($"td_m{m.slot}_type", (int)m.type);
            PlayerPrefs.SetInt($"td_m{m.slot}_lvl", m.level);
            PlayerPrefs.SetInt($"td_m{m.slot}_prog", m.PerRun ? 0 : m.progress);
        }

        public static void Flush()
        {
            foreach (var m in Active) if (m != null) Persist(m);
            PlayerPrefs.Save();
        }
    }
}
