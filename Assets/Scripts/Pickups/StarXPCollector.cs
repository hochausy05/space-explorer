using System;
using SpaceExplorer.Audio;
using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Pickups
{
    public sealed class StarXPCollector : MonoBehaviour
    {
        private static readonly int[] DefaultXPThresholds = { 8, 12, 16, 20, 24 };

        [Header("XP Progression")]
        [SerializeField, Min(1)] private int[] _xpThresholds = { 8, 12, 16, 20, 24 };

        public int CurrentLevel { get; private set; }
        public int TotalXP { get; private set; }
        public int XPIntoCurrentLevel { get; private set; }
        public int XPRequiredForNextLevel => GetThresholdForLevel(CurrentLevel);

        // Compatibility for existing systems that read the accumulated XP value.
        public int CurrentXP => TotalXP;

        public event Action<int> LevelUp;
        public event Action ProgressChanged;

        private void OnEnable()
        {
            GameManager.RunStarted += ResetProgression;
        }

        private void OnDisable()
        {
            GameManager.RunStarted -= ResetProgression;
        }

        public void AddXP(int amount)
        {
            if (amount <= 0)
                return;

            TotalXP += amount;
            XPIntoCurrentLevel += amount;

            while (XPIntoCurrentLevel >= XPRequiredForNextLevel)
            {
                XPIntoCurrentLevel -= XPRequiredForNextLevel;
                CurrentLevel++;
                SfxAudioService.PlayLevelUp();
                LevelUp?.Invoke(CurrentLevel);
            }

            ProgressChanged?.Invoke();
        }

        private void ResetProgression()
        {
            CurrentLevel = 0;
            TotalXP = 0;
            XPIntoCurrentLevel = 0;
            ProgressChanged?.Invoke();
        }

        private int GetThresholdForLevel(int level)
        {
            int[] thresholds = _xpThresholds != null && _xpThresholds.Length > 0
                ? _xpThresholds
                : DefaultXPThresholds;

            int thresholdIndex = Mathf.Min(level, thresholds.Length - 1);
            return Mathf.Max(1, thresholds[thresholdIndex]);
        }
    }
}
