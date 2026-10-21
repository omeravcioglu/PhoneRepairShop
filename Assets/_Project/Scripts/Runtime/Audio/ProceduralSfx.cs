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
            };
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
