using System;
using UnityEngine;

namespace TomorrowsCrime.Audio
{
    /// <summary>
    /// Generates high-fidelity procedural PCM AudioClips at runtime using mathematical waveform synthesis.
    /// Eliminates external asset dependencies and guarantees immediate, crisp audio feedback
    /// for footsteps, clock chimes, paper rustles, clue discoveries, door interactions, and case resolutions.
    /// </summary>
    public static class ProceduralAudioEngine
    {
        private const int SampleRate = 44100;

        /// <summary>
        /// Generates a muffled low-frequency footstep thud on floor.
        /// </summary>
        public static AudioClip CreateFootstepClip(bool isSprinting, bool isCarpet)
        {
            float duration = 0.12f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float baseFreq = isCarpet ? 65f : (isSprinting ? 95f : 80f);
            var rng = new System.Random();

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 35f); // Fast decay

                // Low thud frequency with slight pitch drop
                float freq = Mathf.Lerp(baseFreq * 1.3f, baseFreq * 0.7f, t / duration);
                float sinWave = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Subtle surface texture noise
                float noise = ((float)rng.NextDouble() * 2f - 1f) * (isCarpet ? 0.05f : 0.18f);

                samples[i] = (sinWave * 0.85f + noise * 0.15f) * env * 0.6f;
            }

            var clip = AudioClip.Create(isCarpet ? "Footstep_Carpet" : "Footstep_Hardwood", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a rhythmic antique clock tick-tock sound.
        /// </summary>
        public static AudioClip CreateClockTickClip(bool isTock)
        {
            float duration = 0.045f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float freq = isTock ? 980f : 1250f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 90f); // Very snappy transient
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * (freq * 1.8f) * t) * 0.3f;
                samples[i] = wave * env * 0.35f;
            }

            var clip = AudioClip.Create(isTock ? "Clock_Tock" : "Clock_Tick", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a deep resonant grandfather clock bell chime (Midnight Chime).
        /// Fundamental 220Hz (A3) with harmonic overtones and long exponential decay.
        /// </summary>
        public static AudioClip CreateMidnightBellChime()
        {
            float duration = 3.2f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            float f0 = 220f; // A3

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 1.2f);
                float strike = Mathf.Exp(-t * 25f); // Initial hammer strike transient

                float h1 = Mathf.Sin(2f * Mathf.PI * f0 * t) * 0.50f;
                float h2 = Mathf.Sin(2f * Mathf.PI * (f0 * 2.02f) * t) * 0.25f;
                float h3 = Mathf.Sin(2f * Mathf.PI * (f0 * 3.01f) * t) * 0.15f;
                float h4 = Mathf.Sin(2f * Mathf.PI * (f0 * 4.25f) * t) * 0.10f;

                samples[i] = ((h1 + h2 + h3 + h4) * env + strike * 0.4f) * 0.65f;
            }

            var clip = AudioClip.Create("Bell_MidnightChime", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a tactile paper rustle sound when opening or turning notebook/dossier pages.
        /// </summary>
        public static AudioClip CreatePaperRustleClip()
        {
            float duration = 0.22f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            var rng = new System.Random();
            float lastVal = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration)); // Smooth bell envelope

                // Filtered brown noise for paper friction
                float white = (float)rng.NextDouble() * 2f - 1f;
                lastVal = (lastVal + (0.15f * white)) / 1.15f;

                samples[i] = lastVal * env * 0.45f;
            }

            var clip = AudioClip.Create("Paper_Rustle", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a shimmering crystal/brass evidence discovery chime (Ascending major triad).
        /// </summary>
        public static AudioClip CreateEvidenceDiscoveryChime()
        {
            float duration = 0.9f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            // C5 (523.25Hz), E5 (659.25Hz), G5 (783.99Hz), C6 (1046.5Hz)
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float noteDuration = 0.15f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float mixedSample = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float noteStart = n * noteDuration;
                    if (t >= noteStart)
                    {
                        float noteT = t - noteStart;
                        float env = Mathf.Exp(-noteT * 3.5f);
                        float vibrato = Mathf.Sin(2f * Mathf.PI * 6f * noteT) * 0.015f;
                        float freq = notes[n] * (1f + vibrato);

                        float wave = Mathf.Sin(2f * Mathf.PI * freq * noteT) * 0.7f
                                   + Mathf.Sin(2f * Mathf.PI * freq * 2f * noteT) * 0.25f
                                   + Mathf.Sin(2f * Mathf.PI * freq * 3f * noteT) * 0.05f;

                        mixedSample += wave * env * 0.25f;
                    }
                }

                samples[i] = Mathf.Clamp(mixedSample, -1f, 1f);
            }

            var clip = AudioClip.Create("Evidence_Discovery_Chime", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a mechanical door creak and latch unlock sound.
        /// </summary>
        public static AudioClip CreateDoorCreakClip()
        {
            float duration = 0.45f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Sin(Mathf.PI * (t / duration));

                // Wood friction squeak: frequency modulation
                float creakFreq = 340f + Mathf.Sin(2f * Mathf.PI * 18f * t) * 80f;
                float creak = Mathf.Sin(2f * Mathf.PI * creakFreq * t) * 0.4f;

                // Latch click at end
                float latchT = t - 0.38f;
                float latch = 0f;
                if (latchT > 0f)
                {
                    latch = Mathf.Sin(2f * Mathf.PI * 1200f * latchT) * Mathf.Exp(-latchT * 90f) * 0.5f;
                }

                samples[i] = (creak * env + latch) * 0.4f;
            }

            var clip = AudioClip.Create("Door_Creak", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a metallic keycard / lock mechanism click.
        /// </summary>
        public static AudioClip CreateLockClickClip()
        {
            float duration = 0.12f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float click1 = Mathf.Sin(2f * Mathf.PI * 1400f * t) * Mathf.Exp(-t * 80f);
                float click2 = 0f;
                if (t > 0.04f)
                {
                    float t2 = t - 0.04f;
                    click2 = Mathf.Sin(2f * Mathf.PI * 2100f * t2) * Mathf.Exp(-t2 * 100f);
                }

                samples[i] = (click1 * 0.5f + click2 * 0.5f) * 0.4f;
            }

            var clip = AudioClip.Create("Lock_Click", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a dramatic triumphant brass stinger for correctly solving the murder mystery!
        /// </summary>
        public static AudioClip CreateVictoryStinger()
        {
            float duration = 3.0f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            // Grand brass organ chord: C4 (261.63), G4 (392.00), C5 (523.25), E5 (659.25), G5 (783.99)
            float[] freqs = { 261.63f, 392.00f, 523.25f, 659.25f, 783.99f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Clamp01(t / 0.15f) * Mathf.Exp(-t * 0.75f); // Swell into sustained decay

                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float wave = Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * 0.4f
                               + Mathf.Sin(2f * Mathf.PI * freqs[f] * 2f * t) * 0.2f
                               + Mathf.Sin(2f * Mathf.PI * freqs[f] * 3f * t) * 0.08f;
                    sum += wave;
                }

                samples[i] = (sum / freqs.Length) * env * 0.75f;
            }

            var clip = AudioClip.Create("Music_Victory_Stinger", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a dissonant ominous minor chord stinger when an accusation is incorrect.
        /// </summary>
        public static AudioClip CreateDefeatStinger()
        {
            float duration = 2.4f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            // Low dissonant minor second cluster: 110Hz (A2), 116.54Hz (Bb2), 164.81Hz (E3)
            float[] freqs = { 110f, 116.54f, 164.81f };

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 1.1f);

                float sum = 0f;
                for (int f = 0; f < freqs.Length; f++)
                {
                    float wave = Mathf.Sin(2f * Mathf.PI * freqs[f] * t);
                    sum += wave;
                }

                samples[i] = (sum / freqs.Length) * env * 0.65f;
            }

            var clip = AudioClip.Create("Music_Defeat_Stinger", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Generates a whooshing reverse time-warp sweep when the 5-minute loop turns over.
        /// </summary>
        public static AudioClip CreateLoopRewindSweep()
        {
            float duration = 1.4f;
            int totalSamples = (int)(SampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SampleRate;
                float normT = t / duration;

                // Rising exponential sweep (reverse time perception)
                float freq = Mathf.Lerp(120f, 1600f, normT * normT);
                float env = Mathf.Sin(Mathf.PI * normT);

                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f
                           + Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;

                samples[i] = wave * env * 0.55f;
            }

            var clip = AudioClip.Create("Loop_Rewind_Sweep", totalSamples, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
