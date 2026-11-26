using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TD_Game.EditorTools
{
    /// <summary>
    /// Original sound effects made with code (no outside recordings, so there is no licence problem):
    /// softer enemy warnings and hits with several versions each, falls into water and ravines, breaking chains,
    /// puzzle chimes and the looping ambience of each region (forest, river, desert wind, inside a house).
    /// Also makes the merchant's voice (Audio/Voice): a synthesized gruff character voice, or a teammate's recordings made rougher.
    /// WAV files go to Audio/SFX/Generated. Menu: TD_Game > Audio > Generate Sounds.
    /// </summary>
    public static class SfxSynth
    {
        public const string Folder = "Assets/TD_Game/Audio/SFX/Generated";
        public const string VoiceOut = "Assets/TD_Game/Audio/Voice";
        const int Rate = 44100;

        static System.Random random = new System.Random(11);
        static float Rand() => (float)random.NextDouble() * 2f - 1f;

        [MenuItem("TD_Game/Audio/Generate Sounds")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Folder);
            for (int i = 1; i <= 3; i++)
            {
                random = new System.Random(100 + i);
                Save($"telegraph_{i}", Telegraph(i));
                Save($"hurt_{i}", Hurt(i));
                Save($"enemy_hit_{i}", EnemyHit(i));
                Save($"swing_{i}", Swing(i));
            }
            random = new System.Random(7);
            Save("splash", Splash());
            Save("fall_pit", FallPit());
            Save("chains_break", Chains());
            Save("puzzle_place", Chime(new[] { 880f, 1320f }, 0.7f));
            Save("puzzle_done", Arpeggio());
            Save("amb_forest", Loop(Forest, 12f));
            Save("amb_river", Loop(River, 10f));
            Save("amb_wind", Loop(Wind, 12f));
            Save("amb_interior", Loop(Interior, 10f));
            ProcessVoices();
            AssetDatabase.Refresh();
        }

        // ---------------- Building blocks ----------------

        static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * Rate)];

        /// <summary>One-pole low-pass filter state.</summary>
        class LowPass
        {
            float y;
            public float Run(float x, float cutoff)
            {
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / Rate);
                y += a * (x - y);
                return y;
            }
        }

        /// <summary>Band-pass: the difference of two low-passes.</summary>
        class BandPass
        {
            readonly LowPass high = new LowPass(), low = new LowPass();
            public float Run(float x, float center, float width) => high.Run(x, center + width) - low.Run(x, Mathf.Max(20f, center - width));
        }

        static void Normalize(float[] s, float peak)
        {
            float max = 0.0001f;
            foreach (float v in s) max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < s.Length; i++) s[i] = s[i] / max * peak;
        }

        /// <summary>Short fades at both ends so nothing clicks.</summary>
        static void Edges(float[] s, float fadeIn = 0.003f, float fadeOut = 0.02f)
        {
            int a = Mathf.Min(s.Length, (int)(fadeIn * Rate)), b = Mathf.Min(s.Length, (int)(fadeOut * Rate));
            for (int i = 0; i < a; i++) s[i] *= i / (float)a;
            for (int i = 0; i < b; i++) s[s.Length - 1 - i] *= i / (float)b;
        }

        static void Save(string name, float[] samples)
        {
            WriteWav($"{Folder}/{name}.wav", samples, Rate);
        }

        public static void WriteWav(string path, float[] samples, int rate)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = samples.Length * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                w.Write(System.Text.Encoding.ASCII.GetBytes("fmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (float v in samples) w.Write((short)Mathf.Clamp(Mathf.RoundToInt(v * 32767f), -32768, 32767));
            }
        }

        // ---------------- Combat ----------------

        /// <summary>The enemy is about to strike: a soft rising breath of air and a low, round drum. Not a beep.</summary>
        static float[] Telegraph(int variant)
        {
            float length = 0.42f + variant * 0.03f;
            var s = Buffer(length);
            var band = new BandPass();
            float drumAt = length - 0.2f;
            float drumPitch = 70f + variant * 9f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float rise = Mathf.Clamp01(t / drumAt);
                float air = band.Run(Rand(), 300f + 900f * rise, 250f) * rise * rise * (t < drumAt ? 1f : Mathf.Exp(-(t - drumAt) * 25f));
                float drum = 0f;
                if (t >= drumAt)
                {
                    float d = t - drumAt;
                    drum = Mathf.Sin(2f * Mathf.PI * drumPitch * d * (1f - d)) * Mathf.Exp(-d * 18f) * 1.4f;
                }
                s[i] = air * 0.9f + drum;
            }
            Normalize(s, 0.55f);
            Edges(s);
            return s;
        }

        /// <summary>Tlahuelli is hit: a heavy body blow (deep punch, flesh slap, a short room around it).</summary>
        static float[] Hurt(int variant) => Impact(variant, heavy: true);

        /// <summary>The Macuahuitl bites into an enemy: a sharp crack of obsidian, a meaty crunch and a deep punch.</summary>
        static float[] EnemyHit(int variant) => Impact(variant, heavy: false);

        /// <summary>
        /// A hit with weight, built in layers like the impacts of action games:
        ///   1. a 4 ms transient (the "snap" the ear locks on to),
        ///   2. a deep punch whose pitch drops fast (the weight you feel),
        ///   3. a crunchy band of noise (flesh, bone, obsidian),
        ///   4. soft saturation that glues the layers, and a tiny dark room so it is not dry.
        /// Short (under 0.4 s) so it never tires when it repeats.
        /// </summary>
        static float[] Impact(int variant, bool heavy)
        {
            float length = heavy ? 0.42f : 0.34f;
            var s = Buffer(length);
            var crunchBand = new BandPass();
            var snapBand = new BandPass();
            var tail = new LowPass();
            float punchFrom = (heavy ? 120f : 170f) + variant * 12f;
            float punchTo = heavy ? 42f : 55f;
            float crunchCentre = (heavy ? 650f : 1300f) + variant * 180f;
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                // 1. Transient.
                float snap = t < 0.004f ? snapBand.Run(Rand(), 3800f, 2200f) * (1f - t / 0.004f) * 2.2f : snapBand.Run(0f, 3800f, 2200f);
                // 2. Punch: pitch falls exponentially, the amplitude has a quick attack.
                float f = punchTo + (punchFrom - punchTo) * Mathf.Exp(-t * 28f);
                phase += 2f * Mathf.PI * f / Rate;
                float punch = Mathf.Sin(phase) * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * (heavy ? 11f : 15f)) * 1.5f;
                // 3. Crunch: grainy noise that dies fast; the light hit adds a bright crack of bone or glass.
                float crunch = crunchBand.Run(Rand(), crunchCentre, crunchCentre * 0.7f) * Mathf.Exp(-t * (heavy ? 32f : 40f)) * 1.6f;
                float crack = heavy ? 0f : (Rand() * Mathf.Exp(-t * 120f)) * 0.5f;
                float v = snap + punch + crunch + crack;
                // 4. Saturation and a little low rumble that carries the tail.
                v = (float)System.Math.Tanh(v * 1.8f);
                v += tail.Run(Rand(), 140f) * Mathf.Exp(-t * 9f) * (heavy ? 1.2f : 0.7f);
                s[i] = v;
            }
            // A small dark room: two early echoes.
            var wet = (float[])s.Clone();
            int d1 = Rate * 19 / 1000, d2 = Rate * 37 / 1000;
            for (int i = d2; i < s.Length; i++) wet[i] += s[i - d1] * 0.22f + s[i - d2] * 0.12f;
            Normalize(wet, heavy ? 0.9f : 0.85f);
            Edges(wet, 0.0005f, 0.04f);
            return wet;
        }

        /// <summary>A weapon cutting the air: a soft whoosh that rises and falls.</summary>
        static float[] Swing(int variant)
        {
            float length = 0.2f + variant * 0.03f;
            var s = Buffer(length);
            var band = new BandPass();
            for (int i = 0; i < s.Length; i++)
            {
                float k = i / (float)s.Length;
                float hump = Mathf.Sin(k * Mathf.PI);
                float center = 500f + 1700f * Mathf.Sin(k * Mathf.PI) + variant * 120f;
                s[i] = band.Run(Rand(), center, 350f) * hump * hump;
            }
            Normalize(s, 0.5f);
            Edges(s);
            return s;
        }

        // ---------------- Falls and the quest ----------------

        static float[] Splash()
        {
            var s = Buffer(1.1f);
            var lp = new LowPass();
            var bubbles = new List<(float at, float f)>();
            for (int b = 0; b < 9; b++) bubbles.Add((0.15f + (float)random.NextDouble() * 0.8f, 350f + (float)random.NextDouble() * 500f));
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float body = lp.Run(Rand(), Mathf.Lerp(4000f, 600f, Mathf.Clamp01(t / 0.5f))) * Mathf.Exp(-t * 5f) * (t < 0.01f ? t / 0.01f : 1f);
                float bub = 0f;
                foreach (var (at, f) in bubbles)
                {
                    float d = t - at;
                    if (d < 0f || d > 0.06f) continue;
                    bub += Mathf.Sin(2f * Mathf.PI * f * (1f + d * 8f) * d) * Mathf.Exp(-d * 60f) * 0.35f;
                }
                s[i] = body + bub;
            }
            Normalize(s, 0.7f);
            Edges(s);
            return s;
        }

        /// <summary>Falling into a ravine: a gust of air rushing past, pebbles clattering down and fading away.</summary>
        static float[] FallPit()
        {
            var s = Buffer(1.3f);
            var band = new BandPass();
            var clicks = new List<float>();
            for (int c = 0; c < 14; c++) clicks.Add(0.1f + (float)random.NextDouble() * 1.0f);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float k = t / 1.3f;
                float air = band.Run(Rand(), Mathf.Lerp(900f, 250f, k), 300f) * Mathf.Sin(Mathf.Clamp01(k * 1.4f) * Mathf.PI) * 1.2f;
                float clack = 0f;
                foreach (float at in clicks)
                {
                    float d = t - at;
                    if (d >= 0f && d < 0.02f) clack += Rand() * Mathf.Exp(-d * 300f) * (1f - k) * 0.8f;
                }
                s[i] = air + clack;
            }
            Normalize(s, 0.6f);
            Edges(s);
            return s;
        }

        /// <summary>Magic chains bursting: a crack and bright metal rings that die away.</summary>
        static float[] Chains()
        {
            var s = Buffer(1.6f);
            float[] partials = { 523f, 1347f, 2089f, 2950f, 787f };
            float[] decays = { 3f, 5f, 7f, 9f, 4f };
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float metal = 0f;
                for (int p = 0; p < partials.Length; p++)
                {
                    metal += Mathf.Sin(2f * Mathf.PI * partials[p] * t) * Mathf.Exp(-t * decays[p]) / (p + 1.5f);
                    float t2 = t - 0.11f;   // a second link breaking
                    if (t2 > 0f) metal += Mathf.Sin(2f * Mathf.PI * partials[p] * 1.07f * t2) * Mathf.Exp(-t2 * decays[p] * 1.3f) / (p + 2f);
                }
                float crack = Rand() * Mathf.Exp(-t * 60f);
                s[i] = metal + crack * 0.8f;
            }
            Normalize(s, 0.6f);
            Edges(s);
            return s;
        }

        static float[] Chime(float[] notes, float length)
        {
            var s = Buffer(length);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float v = 0f;
                foreach (float f in notes) v += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 5f);
                s[i] = v;
            }
            Normalize(s, 0.45f);
            Edges(s, 0.002f);
            return s;
        }

        static float[] Arpeggio()
        {
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            var s = Buffer(2.2f);
            for (int n = 0; n < notes.Length; n++)
            {
                int start = (int)(n * 0.13f * Rate);
                for (int i = start; i < s.Length; i++)
                {
                    float t = (i - start) / (float)Rate;
                    s[i] += (Mathf.Sin(2f * Mathf.PI * notes[n] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[n] * t)) * Mathf.Exp(-t * 2.5f);
                }
            }
            Normalize(s, 0.5f);
            Edges(s, 0.002f, 0.1f);
            return s;
        }

        // ---------------- Ambience (seamless loops) ----------------

        /// <summary>Makes a sound of the given length that loops without a seam (the tail fades into the head).</summary>
        static float[] Loop(System.Func<float, float[]> make, float seconds)
        {
            const float overlap = 1.5f;
            var raw = make(seconds + overlap);
            int n = Mathf.CeilToInt(seconds * Rate), o = raw.Length - n;
            var s = new float[n];
            for (int i = 0; i < n; i++) s[i] = raw[i];
            for (int i = 0; i < o && i < n; i++)
            {
                float k = i / (float)o;
                s[i] = raw[i] * k + raw[n + i] * (1f - k);
            }
            return s;
        }

        static float[] Forest(float seconds)
        {
            var s = Buffer(seconds);
            var leaves = new LowPass();
            var birds = new List<(float at, float f, int notes)>();
            for (float t = 0.5f; t < seconds; t += 0.9f + (float)random.NextDouble() * 2.2f)
                birds.Add((t, 2200f + (float)random.NextDouble() * 1800f, 2 + random.Next(4)));
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float sway = 0.6f + 0.4f * Mathf.Sin(t * 0.7f) * Mathf.Sin(t * 0.23f + 1f);
                float v = leaves.Run(Rand(), 700f) * sway * 0.9f;
                foreach (var (at, f, notes) in birds)
                {
                    float d = t - at;
                    if (d < 0f || d > notes * 0.11f) continue;
                    float local = d % 0.11f;
                    if (local > 0.07f) continue;
                    float pitch = f * (1f + 0.25f * Mathf.Sin(local / 0.07f * Mathf.PI));
                    v += Mathf.Sin(2f * Mathf.PI * pitch * local) * Mathf.Sin(local / 0.07f * Mathf.PI) * 0.09f;
                }
                s[i] = v;
            }
            Normalize(s, 0.35f);
            return s;
        }

        static float[] River(float seconds)
        {
            var s = Buffer(seconds);
            var deep = new LowPass();
            var band = new BandPass();
            float brown = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                brown = Mathf.Clamp(brown + Rand() * 0.04f, -1f, 1f) * 0.998f;
                float gurgle = band.Run(Rand(), 500f + 300f * Mathf.Sin(t * 3.1f) * Mathf.Sin(t * 1.3f), 200f) * (0.5f + 0.5f * Mathf.Sin(t * 5.7f));
                s[i] = deep.Run(brown, 500f) * 1.5f + gurgle * 0.8f;
            }
            Normalize(s, 0.4f);
            return s;
        }

        static float[] Wind(float seconds)
        {
            var s = Buffer(seconds);
            var band = new BandPass();
            var low = new LowPass();
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float gust = 0.45f + 0.55f * Mathf.Pow(Mathf.Sin(t * 0.45f) * 0.5f + 0.5f, 2f);
                float whistle = band.Run(Rand(), 450f + 250f * Mathf.Sin(t * 0.3f), 120f) * gust;
                s[i] = whistle * 1.4f + low.Run(Rand(), 180f) * gust * 0.8f;
            }
            Normalize(s, 0.4f);
            return s;
        }

        static float[] Interior(float seconds)
        {
            var s = Buffer(seconds);
            var low = new LowPass();
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)Rate;
                float drone = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 82.4f * t + Mathf.Sin(t * 0.5f)) * 0.3f;
                float breath = low.Run(Rand(), 250f) * (0.5f + 0.5f * Mathf.Sin(t * 0.8f));
                s[i] = drone * (0.6f + 0.2f * Mathf.Sin(t * 0.37f)) + breath * 1.5f;
            }
            Normalize(s, 0.3f);
            return s;
        }

        // ---------------- The merchant's voice ----------------

        /// <summary>Every line of the merchant (file name, subtitle). The builder reads this same table.</summary>
        public static readonly (string id, string text)[] MerchantLines =
        {
            ("merchant_greet_1", "What do you want, stranger?"), ("merchant_greet_2", "Ahh... a living soul. Come closer, stranger."),
            ("merchant_greet_3", "Got some fine things for sale, warrior. Take a look."), ("merchant_greet_4", "Welcome, welcome! Mind the bones on the floor."),
            ("merchant_buy_1", "Heh heh... a fine choice."), ("merchant_buy_2", "Pleasure doing business, stranger."),
            ("merchant_buy_3", "That will serve you well down there."), ("merchant_buy_4", "Sold! Try not to die before you use it."),
            ("merchant_win_1", "Ohh! The gods smile on you today, stranger!"), ("merchant_win_2", "Lucky bones... Take your gold."),
            ("merchant_win_3", "Bah! Fortune is a fickle thing. You win."),
            ("merchant_lose_1", "Heh heh heh... the bones have spoken."), ("merchant_lose_2", "The house of the dead always wins, stranger."),
            ("merchant_lose_3", "Better luck in your next life."),
            ("merchant_bye_1", "Come back anytime... if you're still breathing."), ("merchant_bye_2", "Watch your back out there, stranger."),
        };

        /// <summary>Real recordings (a teammate's voice) go here with the same names (merchant_greet_1.wav...). They win over
        /// the synthesized voice: run TD_Game > Audio > Generate Sounds (or the v0.9 upgrade) after adding them.</summary>
        public const string RecordingsFolder = "Assets/TD_Game/Audio/VoiceRecordings";

        /// <summary>
        /// The merchant's voice. If a teammate recorded the line, the recording is made deeper and rougher.
        /// If not, the line is "spoken" by a synthesized character voice: a low, raspy, mumbling voice that follows the
        /// vowels and rhythm of the subtitle (like the voices of many classic games), with a wheezing "heh heh" laugh.
        /// Robotic text-to-speech is no longer used.
        /// </summary>
        static void ProcessVoices()
        {
            Directory.CreateDirectory(VoiceOut);
            Directory.CreateDirectory(RecordingsFolder);
            foreach (var (id, text) in MerchantLines)
            {
                string recording = $"{RecordingsFolder}/{id}.wav";
                if (File.Exists(recording))
                {
                    var (samples, rate) = ReadWav(recording);
                    if (samples != null) { WriteWav($"{VoiceOut}/{id}.wav", Gravel(samples, rate, 0.9f), rate); continue; }
                }
                random = new System.Random(id.GetHashCode());
                WriteWav($"{VoiceOut}/{id}.wav", Mumble(text), Rate);
            }
        }

        /// <summary>A recorded voice made older and rougher: slower and deeper, a bit of grit, softer highs, a dark little room.</summary>
        static float[] Gravel(float[] samples, int rate, float pitch)
        {
            var slow = new float[Mathf.CeilToInt(samples.Length / pitch)];
            for (int i = 0; i < slow.Length; i++)
            {
                float src = i * pitch;
                int a = Mathf.Min(samples.Length - 1, (int)src);
                int b = Mathf.Min(samples.Length - 1, a + 1);
                slow[i] = Mathf.Lerp(samples[a], samples[b], src - a);
            }
            return Room(slow, rate);
        }

        // ---- The synthesized character voice ----

        /// <summary>Two-pole resonator (a formant of the voice).</summary>
        class Resonator
        {
            float y1, y2;
            public float Run(float x, float f, float bandwidth)
            {
                float r = Mathf.Exp(-Mathf.PI * bandwidth / Rate);
                float b1 = 2f * r * Mathf.Cos(2f * Mathf.PI * f / Rate), b2 = r * r;
                float y = (1f - r) * x + b1 * y1 - b2 * y2;
                y2 = y1; y1 = y;
                return y;
            }
        }

        /// <summary>Formants (F1, F2, F3) of each vowel of a deep male voice.</summary>
        static Vector3 Formants(char vowel) => vowel switch
        {
            'a' => new Vector3(700, 1100, 2450),
            'e' => new Vector3(500, 1750, 2450),
            'i' => new Vector3(320, 2100, 2800),
            'o' => new Vector3(520, 850, 2400),
            'u' => new Vector3(340, 800, 2300),
            _ => new Vector3(600, 1200, 2400),
        };

        /// <summary>
        /// Speaks a line as a gruff mumble: one syllable per vowel group of the text, consonants as short bursts of breath
        /// or clicks, a pitch that rises and falls with the phrase (and goes up on "?" and "!"), commas as pauses,
        /// and "heh" as a wheezing laugh. A rough, breathy throat (jittery pulses plus noise) makes it sound like an old trader.
        /// </summary>
        static float[] Mumble(string text)
        {
            var parts = new List<(char vowel, char onset, float length, float pitchBend, bool laugh, bool pause)>();
            string lower = text.ToLowerInvariant();
            int words = 0;
            for (int i = 0; i < lower.Length; i++)
            {
                char ch = lower[i];
                if (ch == ',' || ch == '.' || ch == '!' || ch == '?')
                {
                    if (ch == '.' && i + 1 < lower.Length && lower[i + 1] == '.') continue;   // "..." is one pause
                    parts.Add((' ', ' ', ch == ',' ? 0.12f : 0.22f, ch == '?' ? 1.25f : ch == '!' ? 1.15f : 0.9f, false, true));
                    continue;
                }
                if (ch == ' ') { words++; continue; }
                if (lower.Substring(i).StartsWith("heh"))
                {
                    parts.Add(('e', 'h', 0.13f, 1.1f, true, false));
                    i += 2;
                    continue;
                }
                if ("aeiouy".IndexOf(ch) < 0) continue;
                char onset = i > 0 ? lower[i - 1] : ' ';
                while (i + 1 < lower.Length && "aeiouy".IndexOf(lower[i + 1]) >= 0) i++;   // a vowel group = one syllable
                parts.Add((ch == 'y' ? 'i' : ch, onset, 0.085f + (float)random.NextDouble() * 0.05f, 1f, false, false));
            }

            float total = 0.25f;
            foreach (var p in parts) total += p.length + (p.pause ? 0f : 0.03f);
            var s = Buffer(total);
            var f1 = new Resonator(); var f2 = new Resonator(); var f3 = new Resonator();
            var breath = new BandPass();
            float basePitch = 92f + (float)random.NextDouble() * 10f;
            float phase = 0f, period = 0f;
            int pos = (int)(0.04f * Rate);
            Vector3 current = Formants('a');
            for (int pi = 0; pi < parts.Count; pi++)
            {
                var p = parts[pi];
                int n = (int)(p.length * Rate);
                if (p.pause) { pos += n; continue; }
                float progress = pi / (float)Mathf.Max(1, parts.Count - 1);
                // Phrase melody: up a little at the start, down at the end; the next punctuation bends the last syllable.
                float melody = 1.08f - 0.18f * progress;
                if (pi + 1 < parts.Count && parts[pi + 1].pause) melody *= parts[pi + 1].pitchBend;
                Vector3 target = Formants(p.vowel);
                bool hiss = "sfzcx".IndexOf(p.onset) >= 0, stop = "ptkbdgq".IndexOf(p.onset) >= 0, nasal = "mnl".IndexOf(p.onset) >= 0;
                int onsetLength = (int)((hiss ? 0.05f : stop ? 0.012f : 0f) * Rate);
                for (int k = 0; k < n + onsetLength && pos < s.Length; k++, pos++)
                {
                    float t = k / (float)(n + onsetLength);
                    float v;
                    if (k < onsetLength)
                    {
                        // Consonant: a hiss or a click of breath.
                        v = breath.Run(Rand(), hiss ? 4200f : 1800f, hiss ? 1800f : 1200f) * (hiss ? 0.5f : 0.9f);
                    }
                    else
                    {
                        current = Vector3.Lerp(current, target, 0.0025f);
                        float pitch = basePitch * melody * (p.laugh ? 1.25f - 0.25f * t : 1f);
                        // A rough throat: each pulse a little early or late (jitter), with breath noise.
                        period += pitch / Rate;
                        if (period >= 1f) { period -= 1f; phase = 1f + (float)(random.NextDouble() - 0.5) * 0.25f; }
                        phase *= 0.93f;
                        float source = phase + Rand() * (p.laugh ? 0.9f : 0.28f);
                        float vowel = f1.Run(source, current.x, 90f) * 1.0f + f2.Run(source, current.y, 120f) * 0.55f + f3.Run(source, current.z, 170f) * 0.25f;
                        if (nasal && t < 0.25f) vowel *= 0.5f;
                        float env = Mathf.Sin(Mathf.Clamp01((k - onsetLength) / (float)n) * Mathf.PI);
                        v = vowel * Mathf.Pow(env, 0.6f);
                    }
                    s[pos] += v;
                }
                pos += (int)(0.03f * Rate);
            }
            return Room(s, Rate);
        }

        /// <summary>Grit, softened highs and a small dark room.</summary>
        static float[] Room(float[] samples, int rate)
        {
            {
                Normalize(samples, 1f);
                var lp = new LowPass();
                var outS = new float[samples.Length + rate / 3];
                for (int i = 0; i < samples.Length; i++)
                {
                    float x = samples[i] * 1.8f;
                    x = (float)System.Math.Tanh(x);                  // warm grit
                    outS[i] = lp.Run(x, 3200f * 44100f / rate);       // softer highs
                }
                // A small room: three short echoes.
                int[] delays = { rate * 23 / 1000, rate * 41 / 1000, rate * 67 / 1000 };
                float[] gains = { 0.22f, 0.14f, 0.08f };
                var wet = (float[])outS.Clone();
                for (int d = 0; d < delays.Length; d++)
                    for (int i = delays[d]; i < outS.Length; i++) wet[i] += outS[i - delays[d]] * gains[d];
                Normalize(wet, 0.85f);
                Edges(wet, 0.005f, 0.05f);
                return wet;
            }
        }

        static (float[], int) ReadWav(string path)
        {
            var bytes = File.ReadAllBytes(path);
            int channels = System.BitConverter.ToInt16(bytes, 22);
            int rate = System.BitConverter.ToInt32(bytes, 24);
            int bits = System.BitConverter.ToInt16(bytes, 34);
            if (bits != 16) return (null, 0);
            int pos = 12;
            while (pos + 8 < bytes.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                int size = System.BitConverter.ToInt32(bytes, pos + 4);
                if (id == "data")
                {
                    int count = size / 2 / channels;
                    var s = new float[count];
                    for (int i = 0; i < count; i++) s[i] = System.BitConverter.ToInt16(bytes, pos + 8 + i * 2 * channels) / 32768f;
                    return (s, rate);
                }
                pos += 8 + size;
            }
            return (null, 0);
        }
    }
}
