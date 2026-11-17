using System.Collections.Generic;
using UltimaCinta.Data;
using UltimaCinta.Ghost;
using UnityEngine;

namespace UltimaCinta.Audio
{
    public static class AudioCueLibrary
    {
        private const int SampleRate = 22050;
        private static readonly AudioClip[] Footsteps = new AudioClip[3];
        private static readonly Dictionary<GhostSoundProfile, AudioClip[]> GhostCues = new Dictionary<GhostSoundProfile, AudioClip[]>();
        private static AudioClip _heartbeat;
        private static AudioClip _roomTone;
        private static AudioClip _doorOpen;
        private static AudioClip _doorClose;
        private static AudioClip _pickup;
        private static AudioClip _uiClick;
        private static AudioClip _milestone;

        public static AudioClip Footstep(int variant)
        {
            var index = Mathf.Abs(variant % Footsteps.Length);
            return Footsteps[index] ??= CreateClip($"Pisada {index + 1}", 0.16f, progress =>
            {
                var noise = Noise(index * 173 + (int)(progress * 4096f));
                var decay = Mathf.Exp(-progress * 22f);
                var thud = Mathf.Sin(Mathf.PI * 2f * Mathf.Lerp(82f, 44f, progress) * progress) * decay * 0.55f;
                var grit = Mathf.Sin(progress * Mathf.PI * 2f * (510f + index * 65f)) * decay * 0.12f;
                return (thud + grit + noise * decay * 0.28f) * Fade(progress, 0.01f);
            });
        }

        public static AudioClip Heartbeat() => _heartbeat ??= CreateClip("Latido doble", 0.34f, progress =>
        {
            var first = Pulse(progress, 0.08f, 0.28f);
            var second = Pulse(progress, 0.39f, 0.66f) * 0.78f;
            var pulse = Mathf.Max(first, second);
            var tone = Mathf.Sin(progress * Mathf.PI * 2f * 54f) * pulse * 0.65f;
            var click = Noise((int)(progress * 4096f)) * pulse * 0.2f;
            return (tone + click) * Fade(progress, 0.012f);
        });

        public static AudioClip RoomTone() => _roomTone ??= CreateClip("Tono de habitación", 6f, progress =>
        {
            var sampleIndex = (int)(progress * 6f * SampleRate);
            var white = Noise(sampleIndex);
            var lowNoise = Noise(sampleIndex / 6) * 0.55f + Noise(sampleIndex / 17) * 0.3f;
            var swell = 0.82f + Mathf.Sin(progress * Mathf.PI * 2f) * 0.18f;
            return (lowNoise * 0.12f + white * 0.018f) * swell * Fade(progress, 0.01f);
        });

        public static AudioClip Door(bool opening)
        {
            if (opening) return _doorOpen ??= CreateDoorClip(true);
            return _doorClose ??= CreateDoorClip(false);
        }

        public static AudioClip Pickup() => _pickup ??= CreateClip("Confirmación de objeto", 0.11f, progress =>
        {
            var frequency = Mathf.Lerp(880f, 1320f, progress);
            var decay = Mathf.Exp(-progress * 9f);
            return Mathf.Sin(progress * Mathf.PI * 2f * frequency * 0.11f) * decay * Fade(progress, 0.005f) * 0.28f;
        });

        public static AudioClip UiClick() => _uiClick ??= CreateClip("Clic de interfaz", 0.025f, progress =>
            Noise((int)(progress * 7f * SampleRate)) * Mathf.Exp(-progress * 30f) * Fade(progress, 0.005f) * 0.16f);

        public static AudioClip Milestone() => _milestone ??= CreateClip("Acorde de hito", 0.48f, progress =>
        {
            var envelope = Mathf.Clamp01(progress * 18f) * Mathf.Clamp01((1f - progress) * 4f);
            var first = Mathf.Sin(progress * Mathf.PI * 2f * 523f) * 0.21f;
            var second = Mathf.Sin(progress * Mathf.PI * 2f * 659f) * 0.18f;
            var third = Mathf.Sin(progress * Mathf.PI * 2f * 784f) * 0.15f;
            return (first + second + third) * envelope * Fade(progress, 0.005f);
        });

        public static AudioClip GhostCue(GhostSoundProfile profile, GhostState state)
        {
            if (!GhostCues.TryGetValue(profile, out var cues))
            {
                cues = new AudioClip[4];
                GhostCues.Add(profile, cues);
            }

            var stateIndex = Mathf.Clamp((int)state, 0, cues.Length - 1);
            return cues[stateIndex] ??= CreateGhostClip(profile, state);
        }

        private static AudioClip CreateDoorClip(bool opening)
        {
            return CreateClip(opening ? "Quejido de puerta al abrir" : "Quejido de puerta al cerrar", 0.86f, progress =>
            {
                var envelope = Mathf.Sin(progress * Mathf.PI) * (opening ? 1f : 0.86f);
                var scrapeFrequency = Mathf.Lerp(opening ? 230f : 330f, opening ? 95f : 120f, progress);
                var scrape = Mathf.Sin(progress * Mathf.PI * 2f * scrapeFrequency) * 0.24f;
                var groan = Mathf.Sin(progress * Mathf.PI * 2f * (scrapeFrequency * 0.51f)) * 0.32f;
                var texture = Noise((int)(progress * 4f * SampleRate)) * 0.18f;
                return (scrape + groan + texture) * envelope * Fade(progress, 0.005f);
            });
        }

        private static AudioClip CreateGhostClip(GhostSoundProfile profile, GhostState state)
        {
            var profileIndex = (int)profile;
            var stateIndex = (int)state;
            var duration = state == GhostState.Hunting ? 1.35f : state == GhostState.Wandering ? 1.1f : 0.82f;
            return CreateClip($"Voz {profile} {state}", duration, progress =>
            {
                var baseFrequency = 48f + profileIndex * 17f;
                var statePitch = state == GhostState.Hunting ? 1.7f : state == GhostState.Activity ? 1.3f : state == GhostState.Resting ? 0.72f : 0.9f;
                var sweep = state == GhostState.Hunting ? Mathf.Lerp(1.5f, 0.78f, progress) : Mathf.Lerp(0.82f, 1.2f, progress);
                var vibrato = Mathf.Sin(progress * Mathf.PI * (4f + profileIndex)) * (0.035f + profileIndex * 0.006f);
                var phase = progress * Mathf.PI * 2f * baseFrequency * statePitch * sweep;
                var throat = Mathf.Sin(phase + Mathf.Sin(phase * 0.19f) * (0.5f + profileIndex * 0.12f));
                var formant = Mathf.Sin(phase * (1.9f + profileIndex * 0.07f) + vibrato) * 0.48f;
                var breath = Noise(profileIndex * 997 + stateIndex * 613 + (int)(progress * 16000f)) * 0.22f;
                var rasp = Mathf.Sin(phase * 3.07f + Mathf.Sin(phase * 0.08f)) * (state == GhostState.Hunting ? 0.31f : 0.15f);
                var envelope = Mathf.Pow(Mathf.Sin(progress * Mathf.PI), 0.8f);
                if (state == GhostState.Activity) envelope *= 0.7f + 0.3f * Mathf.Abs(Mathf.Sin(progress * Mathf.PI * 9f));
                return (throat * 0.34f + formant * 0.27f + rasp * 0.16f + breath) * envelope * Fade(progress, 0.006f) * 0.72f;
            });
        }

        private static AudioClip CreateClip(string name, float duration, System.Func<float, float> sample)
        {
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(duration * SampleRate));
            var samples = new float[sampleCount];
            for (var index = 0; index < sampleCount; index++)
                samples[index] = Mathf.Clamp(sample((float)index / sampleCount), -1f, 1f);
            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float Pulse(float progress, float start, float end)
        {
            if (progress < start || progress > end) return 0f;
            var normalized = (progress - start) / (end - start);
            return Mathf.Sin(normalized * Mathf.PI) * Mathf.Exp(-normalized * 3.4f);
        }

        private static float Fade(float progress, float fadeLength)
        {
            var fadeIn = Mathf.Clamp01(progress / fadeLength);
            var fadeOut = Mathf.Clamp01((1f - progress) / fadeLength);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private static float Noise(int sampleIndex)
        {
            var value = Mathf.Sin(sampleIndex * 12.9898f + 78.233f) * 43758.5453f;
            return (value - Mathf.Floor(value)) * 2f - 1f;
        }
    }
}
