using System.Collections.Generic;
using UnityEngine;

namespace TheDash
{
    /// <summary>Music + synthesised sound effects (no audio files needed for SFX).</summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        AudioSource music, oneShots;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        float musicTarget = 0.55f;

        const int Rate = 44100;

        void Awake()
        {
            I = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0f;
            music.clip = Resources.Load<AudioClip>("Audio/music");
            oneShots = gameObject.AddComponent<AudioSource>();
            oneShots.playOnAwake = false;

            clips["jump"] = Synth(0.12f, t => Square(Mathf.Lerp(380, 760, t / 0.12f), t) * Env(t, 0.005f, 0.12f) * 0.35f);
            clips["coin"] = Synth(0.22f, t => Sine(t < 0.06f ? 1320 : 1760, t) * Env(t, 0.002f, 0.22f) * 0.45f);
            clips["power"] = Synth(0.45f, t =>
            {
                float[] notes = { 523, 659, 784, 1047 };
                int n = Mathf.Min(3, (int)(t / 0.09f));
                return Square(notes[n], t) * Env(t, 0.005f, 0.45f) * 0.25f;
            });
            clips["crash"] = Synth(0.6f, t => (Noise() * 0.7f + Square(Mathf.Lerp(220, 50, t / 0.6f), t) * 0.5f) * Env(t, 0.002f, 0.6f) * 0.55f);
            clips["shield"] = Synth(0.35f, t => (Noise() * 0.3f + Sine(Mathf.Lerp(900, 300, t / 0.35f), t)) * Env(t, 0.002f, 0.35f) * 0.4f);
            clips["click"] = Synth(0.05f, t => Sine(1000, t) * Env(t, 0.001f, 0.05f) * 0.35f);
            clips["milestone"] = Synth(0.5f, t => (Sine(880, t) + Sine(1320, t) * 0.5f) * Env(t, 0.005f, 0.5f) * 0.3f);
            clips["best"] = Synth(0.9f, t =>
            {
                float[] notes = { 523, 659, 784, 1047, 1319 };
                int n = Mathf.Min(4, (int)(t / 0.11f));
                return (Square(notes[n], t) * 0.5f + Sine(notes[n] * 2, t) * 0.5f) * Env(t, 0.005f, 0.9f) * 0.3f;
            });
            clips["reward"] = clips["best"];
            ApplySettings();
        }

        void Update()
        {
            music.pitch = Mathf.MoveTowards(music.pitch, pitchTarget, Time.unscaledDeltaTime * 0.1f);
            music.volume = Mathf.MoveTowards(music.volume, SaveData.Music ? musicTarget : 0f, Time.unscaledDeltaTime * 0.8f);
            if (music.volume <= 0.001f && music.isPlaying && !SaveData.Music) music.Pause();
        }

        public void ApplySettings()
        {
            if (SaveData.Music && music.clip != null && !music.isPlaying) music.Play();
        }

        float pitchTarget = 1f;

        /// <summary>Music speeds up slightly with each zone boost.</summary>
        public void SetMusicPitch(float pitch) => pitchTarget = pitch;

        /// <summary>Music ducks a little in menus, full volume while running.</summary>
        public void SetMusicIntensity(bool running) => musicTarget = running ? 0.6f : 0.35f;

        public void Play(string name, float pitch = 1f, float volume = 1f)
        {
            if (!SaveData.Sfx || !clips.TryGetValue(name, out var c)) return;
            oneShots.pitch = pitch;
            oneShots.PlayOneShot(c, volume);
        }

        // ---------------------------------------------------------------- synthesis helpers
        static AudioClip Synth(float seconds, System.Func<float, float> f)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float hz, float t) => Mathf.Sin(2 * Mathf.PI * hz * t);
        static float Square(float hz, float t) => Mathf.Sin(2 * Mathf.PI * hz * t) > 0 ? 0.6f : -0.6f;
        static readonly System.Random rng = new System.Random(1);
        static float Noise() => (float)(rng.NextDouble() * 2 - 1);

        static float Env(float t, float attack, float length)
        {
            if (t < attack) return t / attack;
            float k = 1f - (t - attack) / (length - attack);
            return Mathf.Clamp01(k) * Mathf.Clamp01(k);
        }
    }
}
