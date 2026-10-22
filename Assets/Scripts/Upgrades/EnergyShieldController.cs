using System;
using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public enum EnergyShieldState
    {
        Unavailable,
        Ready,
        Cooldown
    }

    public sealed class EnergyShieldController : MonoBehaviour
    {
        public const int MaximumLevel = 3;

        [Header("Cooldown by Upgrade Level")]
        [SerializeField, Min(0.01f)] private float _levelOneCooldown = 12f;
        [SerializeField, Min(0.01f)] private float _levelTwoCooldown = 9f;
        [SerializeField, Min(0.01f)] private float _levelThreeCooldown = 6f;

        private int _currentLevel;
        private float _cooldownEndsAt;

        public int CurrentLevel => _currentLevel;
        // Chỉ cho nâng cấp khi controller đang hoạt động và chưa đạt cấp tối đa.
        public bool CanUpgrade => isActiveAndEnabled && _currentLevel < MaximumLevel;
        public event Action StateChanged;
        // Trả thời gian cooldown còn lại và không cho kết quả âm.
        public float CooldownRemaining => Mathf.Max(0f, _cooldownEndsAt - Time.time);
        // Suy ra Shield đang chưa có, sẵn sàng hay còn trong cooldown.
        public EnergyShieldState State => _currentLevel == 0
            ? EnergyShieldState.Unavailable
            : CooldownRemaining > 0f ? EnergyShieldState.Cooldown : EnergyShieldState.Ready;

        // Đăng ký reset Shield khi bắt đầu một run mới.
        private void Awake()
        {
            GameManager.RunStarted += ResetForNewRun;
        }

        // Hủy đăng ký sự kiện run trước khi component bị hủy.
        private void OnDestroy()
        {
            GameManager.RunStarted -= ResetForNewRun;
        }

        // Tăng cấp Shield nếu chưa đạt giới hạn và báo HUD cập nhật.
        public bool TryUpgrade()
        {
            if (!CanUpgrade)
                return false;

            _currentLevel++;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>Dùng một lần chặn va chạm khi Shield sẵn sàng; cooldown của chu kỳ được chốt tại lúc trúng đòn.</summary>
        // Chỉ Shield Ready hấp thụ hit; cấp hiện tại quyết định thời lượng cooldown kế tiếp.
        public bool TryAbsorbAsteroidCollision()
        {
            if (State != EnergyShieldState.Ready)
                return false;

            _cooldownEndsAt = Time.time + GetCooldownForLevel(_currentLevel);
            StateChanged?.Invoke();
            return true;
        }

        // Lấy thời lượng cooldown theo cấp Shield hiện tại.
        private float GetCooldownForLevel(int level)
        {
            float configuredCooldown = level switch
            {
                1 => _levelOneCooldown,
                2 => _levelTwoCooldown,
                _ => _levelThreeCooldown
            };

            return Mathf.Max(0.01f, configuredCooldown);
        }

        // Xóa cấp và cooldown Shield để run mới bắt đầu từ trạng thái ban đầu.
        private void ResetForNewRun()
        {
            _currentLevel = 0;
            _cooldownEndsAt = 0f;
            StateChanged?.Invoke();
        }
    }
}
