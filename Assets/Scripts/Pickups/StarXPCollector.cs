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
        // Trả ngưỡng XP cần đạt để lên cấp kế tiếp.
        public int XPRequiredForNextLevel => GetThresholdForLevel(CurrentLevel);

        // Giữ tên cũ để các hệ thống đang đọc tổng XP vẫn tương thích.
        public int CurrentXP => TotalXP;

        public event Action<int> LevelUp;
        public event Action ProgressChanged;

        // Đăng ký nhận sự kiện bắt đầu run mới để reset tiến trình.
        private void OnEnable()
        {
            GameManager.RunStarted += ResetProgression;
        }

        // Hủy đăng ký sự kiện khi component không còn hoạt động.
        private void OnDisable()
        {
            GameManager.RunStarted -= ResetProgression;
        }

        // Giữ XP dư sau mỗi cấp và phát một sự kiện riêng cho từng lần lên Level.
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

        // Run mới bắt đầu lại Level và XP từ đầu, không reset khi component chỉ được tạo.
        private void ResetProgression()
        {
            CurrentLevel = 0;
            TotalXP = 0;
            XPIntoCurrentLevel = 0;
            ProgressChanged?.Invoke();
        }

        // Trả ngưỡng XP của cấp, dùng ngưỡng cuối cho mọi cấp cao hơn.
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
