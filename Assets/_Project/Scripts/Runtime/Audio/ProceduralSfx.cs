using System;
using System.Collections.Generic;
using UnityEngine;

namespace PhoneRepair.Audio
{
    /// <summary>
    /// Tiny synthesiser for placeholder sound effects, so feel can be judged before real audio
    /// exists. Deterministic (seeded noise) and generated once at startup (~a few ms).
    /// </summary>
    public static class ProceduralSfx
    {
        private const int SampleRate = 44100;

        public static Dictionary<SfxId, AudioClip> CreateAll()
        {
            var random = new System.Random(1234);
            return new Dictionary<SfxId, AudioClip>
            {
                [SfxId.Tick] = Make("sfx_tick", 0.03f, t =>
                    Noise(random) * Mathf.Exp(-t * 180f) * 0.45f + Mathf.Sin(Tau * 3200f * t) * Mathf.Exp(-t * 300f) * 0.3f),
                [SfxId.Pop] = Sweep("sfx_pop", 0.14f, 480f, 980f, t => Mathf.Exp(-t * 28f) * (1f - Mathf.Exp(-t * 900f)) * 0.55f),
                [SfxId.Thunk] = Make("sfx_thunk", 0.16f, PhaseSweep(140f, 85f, 0.16f, t => Mathf.Exp(-t * 30f) * 0.7f, random, 0.18f)),
                [SfxId.Click] = Make("sfx_click", 0.05f, t =>
                    Mathf.Sin(Tau * 2100f * t) * Mathf.Exp(-t * 140f) * 0.45f + Noise(random) * Mathf.Exp(-t * 400f) * 0.35f),
                [SfxId.Unplug] = Sweep("sfx_unplug", 0.09f, 720f, 360f, t => Mathf.Exp(-t * 45f) * 0.45f),
                [SfxId.Nope] = Make("sfx_nope", 0.24f, t =>
                {
                    float f = t < 0.11f ? 220f : 165f;
                    float env = Mathf.Exp(-(t % 0.12f) * 22f) * 0.28f;
                    return (float)Math.Tanh(3f * Mathf.Sin(Tau * f * t)) * env;
                }),
                [SfxId.Whoosh] = LowPassNoise("sfx_whoosh", 0.28f, random, t => Mathf.Pow(Mathf.Sin(Mathf.PI * t / 0.28f), 2f) * 0.22f),
                [SfxId.Select] = Make("sfx_select", 0.04f, t => Mathf.Sin(Tau * 1400f * t) * Mathf.Exp(-t * 160f) * 0.35f),
                [SfxId.Bell] = Make("sfx_bell", 0.9f, t => Bell(t, 1568f, 4.5f) * 0.4f),
                [SfxId.DoorChime] = Make("sfx_door_chime", 0.75f, t =>
                    Bell(t, 1319f, 6f) * 0.3f + (t > 0.18f ? Bell(t - 0.18f, 1047f, 5f) * 0.3f : 0f)),
                [SfxId.Coin] = Make("sfx_coin", 0.35f, t =>
                {
                    float f = t < 0.07f ? 1976f : 2637f;
                    float env = Mathf.Exp(-(t < 0.07f ? t : t - 0.07f) * 14f);
                    return Square(f * t) * env * 0.16f + Noise(random) * Mathf.Exp(-t * 90f) * 0.12f;
                }),
                [SfxId.Blip] = Make("sfx_blip", 0.05f, t => Square(520f * t) * Mathf.Exp(-t * 45f) * 0.14f),
                [SfxId.Swap] = Sweep("sfx_swap", 0.18f, 300f, 900f, t => Mathf.Exp(-t * 18f) * 0.4f),
                [SfxId.Paper] = LowPassNoise("sfx_paper", 0.07f, random, t => Mathf.Exp(-t * 60f) * 0.5f),
                [SfxId.Happy] = Make("sfx_happy", 0.42f, t => Arpeggio(t, new[] { 523f, 659f, 784f, 1047f }, 0.09f) * 0.22f),
                [SfxId.Sad] = Make("sfx_sad", 0.7f, t =>
                {
                    float f = (t < 0.3f ? 392f : 330f) * (1f + 0.02f * Mathf.Sin(Tau * 6f * t));
                    float local = t < 0.3f ? t : t - 0.3f;
                    return Square(f * t) * Mathf.Exp(-local * 3.5f) * 0.12f;
                }),
                [SfxId.Meow] = Make("sfx_meow", 0.38f, t =>
                {
                    float u = t / 0.38f;
                    float f = 620f + 420f * Mathf.Sin(Mathf.PI * u) - 120f * u;
                    return Mathf.Sin(Tau * f * t) * Mathf.Sin(Mathf.PI * u) * 0.3f + Mathf.Sin(Tau * 2f * f * t) * Mathf.Sin(Mathf.PI * u) * 0.08f;
                }),
            };
        }

        /// <summary>Inharmonic partials with a quick decay: counter bells and door chimes.</summary>
        private static float Bell(float t, float f, float decay) =>
            (Mathf.Sin(Tau * f * t) + 0.5f * Mathf.Sin(Tau * f * 2.76f * t) * Mathf.Exp(-t * 6f)) * Mathf.Exp(-t * decay);

        /// <summary>Soft square wave (cycles = frequency × time).</summary>
        private static float Square(float cycles) => (float)Math.Tanh(4f * Mathf.Sin(Tau * cycles));

        private static float Arpeggio(float t, float[] notes, float step)
        {
            int index = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t / step));
            float local = t - index * step;
            return Square(notes[index] * t) * Mathf.Exp(-local * 12f);
        }

        private const float Tau = Mathf.PI * 2f;

        private static float Noise(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);

        private static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(sample(i / (float)SampleRate), -1f, 1f);
            }

            ApplyFadeOut(data);
            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Sweep(string name, float seconds, float fromHz, float toHz, Func<float, float> envelope)
        {
            float phase = 0f;
            return Make(name, seconds, t =>
            {
                float f = Mathf.Lerp(fromHz, toHz, t / seconds);
                phase += Tau * f / SampleRate;
                return Mathf.Sin(phase) * envelope(t);
            });
        }

        private static Func<float, float> PhaseSweep(float fromHz, float toHz, float seconds, Func<float, float> envelope,
            System.Random random, float noiseAmount)
        {
            float phase = 0f;
            return t =>
            {
                float f = Mathf.Lerp(fromHz, toHz, t / seconds);
                phase += Tau * f / SampleRate;
                return Mathf.Sin(phase) * envelope(t) + Noise(random) * Mathf.Exp(-t * 120f) * noiseAmount;
            };
        }

        private static AudioClip LowPassNoise(string name, float seconds, System.Random random, Func<float, float> envelope)
        {
            float state = 0f;
            return Make(name, seconds, t =>
            {
                state += 0.08f * (Noise(random) - state);
                return state * 3f * envelope(t);
            });
        }

        private static void ApplyFadeOut(float[] data)
        {
            int fade = Mathf.Min(data.Length, 64); // avoid clicks at the end
            for (int i = 0; i < fade; i++)
            {
                data[data.Length - 1 - i] *= i / (float)fade;
            }
        }
    }
}
