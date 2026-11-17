using UltimaCinta.Data;
using UnityEngine;

namespace UltimaCinta.UI
{
    internal static class GameSettingsView
    {
        public static void Draw(GameConfig config, Rect area)
        {
            var scale = GameMenuStyles.Scale;
            var labelWidth = area.width * 0.42f;
            var sliderWidth = area.width - labelWidth - 100f * scale;
            var rowHeight = 56f * scale;
            var y = area.y;

            GameMenuStyles.DrawLabel(new Rect(area.x, y, labelWidth, rowHeight), "VOLUMEN GENERAL");
            var master = GameSettingsStore.MasterVolume(config);
            GUI.color = GamePalette.Lamp;
            var nextMaster = GUI.HorizontalSlider(new Rect(area.x + labelWidth, y + rowHeight * 0.5f, sliderWidth, 20f * scale), master, 0f, 1f);
            GUI.color = Color.white;
            if (!Mathf.Approximately(master, nextMaster)) GameSettingsStore.SetMasterVolume(nextMaster);
            GameMenuStyles.DrawBody(new Rect(area.xMax - 76f * scale, y, 76f * scale, rowHeight), $"{Mathf.RoundToInt(nextMaster * 100f)}%");
            y += rowHeight;

            GameMenuStyles.DrawLabel(new Rect(area.x, y, labelWidth, rowHeight), "SENSIBILIDAD DEL RATÓN");
            var sensitivity = GameSettingsStore.LookSensitivity(config);
            var minimum = config != null ? config.Audio.SensitivityMultiplierMinimum : 0.5f;
            var maximum = config != null ? config.Audio.SensitivityMultiplierMaximum : 2f;
            GUI.color = GamePalette.Lamp;
            var nextSensitivity = GUI.HorizontalSlider(new Rect(area.x + labelWidth, y + rowHeight * 0.5f, sliderWidth, 20f * scale), sensitivity, minimum, maximum);
            GUI.color = Color.white;
            if (!Mathf.Approximately(sensitivity, nextSensitivity)) GameSettingsStore.SetLookSensitivity(nextSensitivity, config);
            GameMenuStyles.DrawBody(new Rect(area.xMax - 76f * scale, y, 76f * scale, rowHeight), $"{nextSensitivity:0.00}×");
            y += rowHeight;

            GameMenuStyles.DrawLabel(new Rect(area.x, y, labelWidth, rowHeight), "SUBTÍTULOS DE AUDIO");
            var subtitles = GameSettingsStore.SubtitlesEnabled;
            if (GameMenuStyles.Button(new Rect(area.x + labelWidth, y + 7f * scale, sliderWidth, 40f * scale), subtitles ? "ACTIVADOS" : "DESACTIVADOS", true))
                GameSettingsStore.SetSubtitlesEnabled(!subtitles);
        }
    }
}
