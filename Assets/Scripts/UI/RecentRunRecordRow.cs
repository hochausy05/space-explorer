using System.Globalization;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class RecentRunRecordRow : MonoBehaviour
    {
        [SerializeField] private Text _matchNumberText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _survivalTimeText;
        [SerializeField] private Text _levelText;

        public bool IsConfigured => _matchNumberText != null && _scoreText != null
            && _survivalTimeText != null && _levelText != null;

        private void Awake()
        {
            Text sampleText = GetComponent<Text>();
            if (sampleText != null)
                sampleText.enabled = false;
        }

        public void SetRecord(int matchNumber, RecentRunRecord record)
        {
            if (!IsConfigured || record == null)
                return;

            _matchNumberText.text = matchNumber.ToString("00", CultureInfo.InvariantCulture);
            _scoreText.text = record.FinalScore.ToString("N0", CultureInfo.CurrentCulture);
            _survivalTimeText.text = FormatMinutesAndSeconds(record.SurvivalTimeSeconds);
            _levelText.text = record.PlayerLevel.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatMinutesAndSeconds(float seconds)
        {
            double safeSeconds = seconds;
            if (double.IsNaN(safeSeconds) || double.IsInfinity(safeSeconds) || safeSeconds < 0d)
                safeSeconds = 0d;

            double wholeSeconds = System.Math.Floor(safeSeconds);
            long totalSeconds = wholeSeconds >= long.MaxValue ? long.MaxValue : (long)wholeSeconds;
            long totalMinutes = totalSeconds / 60;
            long remainingSeconds = totalSeconds % 60;
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", totalMinutes, remainingSeconds);
        }
    }
}
