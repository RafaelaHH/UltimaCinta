using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.UI
{
    public static class GameSettingsStore
    {
        private const string MasterVolumeKey = "ultima-cinta.master-volume";
        private const string LookSensitivityKey = "ultima-cinta.look-sensitivity";
        private const string SubtitlesKey = "ultima-cinta.subtitles";

        public static float MasterVolume(GameConfig config) =>
            Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, config != null ? config.Audio.DefaultMasterVolume : 1f));

        public static float LookSensitivity(GameConfig config) =>
            Mathf.Clamp(PlayerPrefs.GetFloat(LookSensitivityKey, config != null ? config.Audio.DefaultSensitivityMultiplier : 1f),
                config != null ? config.Audio.SensitivityMultiplierMinimum : 0.5f,
                config != null ? config.Audio.SensitivityMultiplierMaximum : 2f);

        public static bool SubtitlesEnabled => PlayerPrefs.GetInt(SubtitlesKey, 1) != 0;

        public static void SetMasterVolume(float value)
        {
            var volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
            AudioListener.volume = volume;
        }

        public static void SetLookSensitivity(float value, GameConfig config)
        {
            var minimum = config != null ? config.Audio.SensitivityMultiplierMinimum : 0.5f;
            var maximum = config != null ? config.Audio.SensitivityMultiplierMaximum : 2f;
            PlayerPrefs.SetFloat(LookSensitivityKey, Mathf.Clamp(value, minimum, maximum));
        }

        public static void SetSubtitlesEnabled(bool enabled) => PlayerPrefs.SetInt(SubtitlesKey, enabled ? 1 : 0);

        public static void Apply(GameConfig config) => AudioListener.volume = MasterVolume(config);

        public static void Save() => PlayerPrefs.Save();
    }
}
