using SpaceExplorer.Audio;
using SpaceExplorer.Asteroids;
using SpaceExplorer.Combat;
using SpaceExplorer.Core;
using SpaceExplorer.Pickups;
using SpaceExplorer.Upgrades;
using UnityEngine;

namespace SpaceExplorer.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(PlayerHealth))]
    public sealed class PlayerAsteroidCollisionHandler : MonoBehaviour
    {
        [SerializeField] private StarXPCollector _progression;
        [SerializeField, Min(0)] private int _smallAsteroidDamage = 1;
        [SerializeField, Min(0)] private int _largeAsteroidDamage = 3;
        [SerializeField, Min(0)] private int _smallAsteroidScorePenalty = 5;
        [SerializeField, Min(0)] private int _largeAsteroidScorePenalty = 15;
        [SerializeField, Min(0)] private int _speedAsteroidDamage = 5;
        [SerializeField, Min(0)] private int _speedAsteroidScorePenalty = 25;
        [SerializeField, Min(0)] private int _homingAsteroidDamage = 5;
        [SerializeField, Min(0)] private int _homingAsteroidScorePenalty = 25;
        [SerializeField, Min(0)] private int _erraticAsteroidDamage = 5;
        [SerializeField, Min(0)] private int _erraticAsteroidScorePenalty = 25;
        [SerializeField, Min(0)] private int _splitterAsteroidDamage = 5;
        [SerializeField, Min(0)] private int _splitterAsteroidScorePenalty = 25;
        [SerializeField, Min(0)] private int _fragmentAsteroidDamage = 1;
        [SerializeField, Min(0)] private int _fragmentAsteroidScorePenalty = 5;
        [SerializeField, Min(0f)] private float _postHitProtectionDuration = 1f;

        [Header("Shield Impact Visual")]
        [SerializeField] private SpriteRenderer _shieldVisual;
        [SerializeField, Min(0.01f)] private float _shieldVisualFadeInDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float _shieldVisualFadeOutDuration = 0.1f;

        private Collider2D _playerCollider;
        private PlayerHealth _playerHealth;
        private PlayerController _playerController;
        private WeaponController _weaponController;
        private EnergyShieldController _energyShield;
        private GameManager _runManager;
        private double _runStartedAt;
        private double _postHitProtectionStartedAt;
        private double _postHitProtectionEndsAt;
        private double _lastCollisionFixedTime = double.NegativeInfinity;
        private EntityId _lastCollisionColliderId = EntityId.None;
        private bool _hasRunStartTime;
        private bool _gameOverStarted;
        private bool _configurationErrorReported;
        private Color _shieldVisualBaseColor;
        private bool _shieldVisualConfigured;
        private bool _shieldVisualVisible;
        private bool _shieldVisualConfigurationWarningReported;

        // Chọn handler duy nhất, lấy component Player và thiết lập liên kết run.
        private void Awake()
        {
            PlayerAsteroidCollisionHandler[] handlers = GetComponents<PlayerAsteroidCollisionHandler>();
            for (int i = 0; i < handlers.Length; i++)
            {
                PlayerAsteroidCollisionHandler handler = handlers[i];
                if (handler == null || !handler.enabled)
                    continue;

                if (handler != this)
                {
                    Debug.LogWarning("Multiple PlayerAsteroidCollisionHandlers are attached to this Player. Only the first enabled handler will run; remove duplicates in the Inspector.", this);
                    enabled = false;
                    return;
                }

                break;
            }

            _playerCollider = GetComponent<Collider2D>();
            _playerHealth = GetComponent<PlayerHealth>();
            _playerController = GetComponent<PlayerController>();
            _weaponController = GetComponent<WeaponController>();
            _energyShield = GetComponent<EnergyShieldController>();
            _shieldVisualConfigured = ConfigureShieldVisual();

            if (_progression == null)
                _progression = GetComponent<StarXPCollector>();

            if (_playerCollider == null || !_playerCollider.isTrigger)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's existing trigger Collider2D.");
                return;
            }

            if (_progression == null)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's StarXPCollector to capture RunResult.");
                return;
            }

            if (_playerHealth == null)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler requires the Player's PlayerHealth component.");
                return;
            }

            GameManager.RunStarted += HandleRunStarted;
            GameManager manager = GameManager.Instance;
            if (manager != null && (manager.State == RunState.Playing || manager.State == RunState.Paused))
                HandleRunStarted();
        }

        // Hủy đăng ký các sự kiện GameManager khi handler bị hủy.
        private void OnDestroy()
        {
            GameManager.RunStarted -= HandleRunStarted;
            if (_runManager != null)
                _runManager.RunFinished -= HandleRunFinished;
        }

        // Cập nhật thời lượng bảo vệ và hiệu ứng Shield theo thời gian gameplay.
        private void Update()
        {
            if (!_shieldVisualVisible)
                return;

            double now = Time.timeAsDouble;
            if (now >= _postHitProtectionEndsAt)
            {
                HideShieldVisual();
                return;
            }

            float duration = Mathf.Max(0.0001f, (float)(_postHitProtectionEndsAt - _postHitProtectionStartedAt));
            float progress = Mathf.Clamp01((float)((now - _postHitProtectionStartedAt) / duration));
            EvaluateShieldVisualAnimation(progress, _shieldVisualFadeInDuration / duration,
                _shieldVisualFadeOutDuration / duration, out float alpha, out float brightness);
            SetShieldVisualAppearance(alpha, brightness);
        }

        // Mỗi va chạm được xử lý theo thứ tự: bỏ qua bảo vệ, trừ điểm, thử Shield rồi mới trừ máu.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_gameOverStarted || !other.TryGetComponent(out Asteroid asteroid))
                return;

            GameManager manager = GameManager.Instance;
            if (manager == null || (manager.State != RunState.Playing && manager.State != RunState.Paused))
                return;

            EntityId colliderId = other.GetEntityId();
            double fixedTime = Time.fixedTimeAsDouble;
            if (_lastCollisionColliderId.Equals(colliderId) && _lastCollisionFixedTime == fixedTime)
                return;

            _lastCollisionColliderId = colliderId;
            _lastCollisionFixedTime = fixedTime;

            if (Time.timeAsDouble < _postHitProtectionEndsAt)
                return;

            int scorePenalty = GetAsteroidScorePenalty(asteroid);

            if (!manager.TryApplyScorePenalty(scorePenalty, out string penaltyError))
            {
                Debug.LogError($"Player-Asteroid collision could not apply the Score penalty: {penaltyError}", this);
                return;
            }

            if (_energyShield != null && _energyShield.TryAbsorbAsteroidCollision())
            {
                SfxAudioService.PlayShieldHit();
                BeginPostHitProtection(showShieldBubble: true);
                return;
            }

            int damage = GetAsteroidDamage(asteroid);

            int hpBeforeDamage = _playerHealth.CurrentHP;
            if (_playerHealth.ApplyDamage(damage))
            {
                FinishRun(manager);
                return;
            }

            if (_playerHealth.CurrentHP < hpBeforeDamage)
                BeginPostHitProtection(showShieldBubble: false);
        }

        // Trả sát thương HP theo loại Asteroid và giá trị đã cấu hình trong Inspector.
        private int GetAsteroidDamage(Asteroid asteroid)
        {
            switch (asteroid.Kind)
            {
                case AsteroidKind.Speed: return Mathf.Max(0, _speedAsteroidDamage);
                case AsteroidKind.Homing: return Mathf.Max(0, _homingAsteroidDamage);
                case AsteroidKind.Erratic: return Mathf.Max(0, _erraticAsteroidDamage);
                case AsteroidKind.Splitter: return Mathf.Max(0, _splitterAsteroidDamage);
                case AsteroidKind.Fragment: return Mathf.Max(0, _fragmentAsteroidDamage);
                default: return asteroid.Size == AsteroidSize.Small
                    ? Mathf.Max(0, _smallAsteroidDamage)
                    : Mathf.Max(0, _largeAsteroidDamage);
            }
        }

        // Trả mức phạt Score theo loại Asteroid mà Player vừa chạm phải.
        private int GetAsteroidScorePenalty(Asteroid asteroid)
        {
            switch (asteroid.Kind)
            {
                case AsteroidKind.Speed: return Mathf.Max(0, _speedAsteroidScorePenalty);
                case AsteroidKind.Homing: return Mathf.Max(0, _homingAsteroidScorePenalty);
                case AsteroidKind.Erratic: return Mathf.Max(0, _erraticAsteroidScorePenalty);
                case AsteroidKind.Splitter: return Mathf.Max(0, _splitterAsteroidScorePenalty);
                case AsteroidKind.Fragment: return Mathf.Max(0, _fragmentAsteroidScorePenalty);
                default: return asteroid.Size == AsteroidSize.Small
                    ? Mathf.Max(0, _smallAsteroidScorePenalty)
                    : Mathf.Max(0, _largeAsteroidScorePenalty);
            }
        }

        // Chụp kết quả cuối run một lần rồi chuyển sang EndGame qua SceneLoader.
        private void FinishRun(GameManager manager)
        {
            if (!_hasRunStartTime)
            {
                FailConfiguration("PlayerAsteroidCollisionHandler did not receive the new-run event needed to capture survival time.");
                return;
            }

            _gameOverStarted = true;
            ClearPostHitProtection();
            if (_playerController != null)
                _playerController.enabled = false;
            if (_weaponController != null)
                _weaponController.enabled = false;

            float survivalTime = Mathf.Max(0f, (float)(Time.timeAsDouble - _runStartedAt));
            var result = new RunResult(manager.CurrentScore, _progression.CurrentLevel, survivalTime);

            if (!manager.TryFinishRun(result, out string finishError))
            {
                Debug.LogError($"Player-Asteroid collision could not finish the run: {finishError}", this);
                return;
            }

            if (!SceneLoader.TryLoad(SceneLoader.EndGame, out string sceneError))
                Debug.LogError(sceneError, this);
        }

        // Đặt lại bộ khử va chạm và thời gian bảo vệ khi run mới bắt đầu.
        private void HandleRunStarted()
        {
            BindRunFinishedEvent();
            _gameOverStarted = false;
            _runStartedAt = Time.timeAsDouble;
            ClearPostHitProtection();
            _lastCollisionColliderId = EntityId.None;
            _lastCollisionFixedTime = double.NegativeInfinity;
            _hasRunStartTime = true;
        }

        // Theo dõi GameManager hiện tại để dọn hiệu ứng khi run kết thúc.
        private void BindRunFinishedEvent()
        {
            GameManager manager = GameManager.Instance;
            if (_runManager == manager)
                return;

            if (_runManager != null)
                _runManager.RunFinished -= HandleRunFinished;

            _runManager = manager;
            if (_runManager != null)
                _runManager.RunFinished += HandleRunFinished;
        }

        // Xóa trạng thái bảo vệ và ẩn hình Shield sau khi run kết thúc.
        private void HandleRunFinished(RunResult _)
        {
            ClearPostHitProtection();
        }

        // Bắt đầu cửa sổ miễn va chạm; chỉ hit được Shield hấp thụ mới hiện bubble.
        private void BeginPostHitProtection(bool showShieldBubble)
        {
            double now = Time.timeAsDouble;
            _postHitProtectionStartedAt = now;
            _postHitProtectionEndsAt = now + Mathf.Max(0f, _postHitProtectionDuration);
            _shieldVisualVisible = showShieldBubble && _postHitProtectionEndsAt > now && _shieldVisualConfigured;

            if (showShieldBubble && !_shieldVisualConfigured && !_shieldVisualConfigurationWarningReported)
            {
                Debug.LogWarning("Shield impact protection is active, but the ShieldVisual SpriteRenderer is missing or is not a child of the Player.", this);
                _shieldVisualConfigurationWarningReported = true;
            }

            if (_shieldVisualVisible)
            {
                _shieldVisual.enabled = true;
                SetShieldVisualAppearance(0f, 1.12f);
            }
            else
            {
                HideShieldVisual();
            }
        }

        // Xóa thời gian bảo vệ và khôi phục trạng thái hiển thị gốc của Shield.
        private void ClearPostHitProtection()
        {
            _postHitProtectionStartedAt = 0d;
            _postHitProtectionEndsAt = 0d;
            HideShieldVisual();
        }

        // Ẩn hình Shield và phục hồi màu gốc trước lần hiển thị tiếp theo.
        private void HideShieldVisual()
        {
            _shieldVisualVisible = false;
            if (_shieldVisual != null && _shieldVisualConfigured)
            {
                _shieldVisual.color = _shieldVisualBaseColor;
                _shieldVisual.enabled = false;
            }
        }

        // Tìm SpriteRenderer ShieldVisual dưới Player, lưu màu và ẩn lúc khởi tạo.
        private bool ConfigureShieldVisual()
        {
            if (_shieldVisual == null)
            {
                Transform shieldVisualTransform = transform.Find("ShieldVisual");
                if (shieldVisualTransform != null)
                    _shieldVisual = shieldVisualTransform.GetComponent<SpriteRenderer>();
            }

            if (_shieldVisual == null || !FollowsPlayer(_shieldVisual.transform))
                return false;

            _shieldVisualBaseColor = _shieldVisual.color;
            _shieldVisual.enabled = false;
            return true;
        }

        // Xác nhận hình Shield là con của Player nên sẽ đi theo tàu.
        private bool FollowsPlayer(Transform rendererTransform)
        {
            return rendererTransform.IsChildOf(transform);
        }

        // Tính fade, nhịp sáng và nhấp nháy theo tiến độ cửa sổ bảo vệ.
        private static void EvaluateShieldVisualAnimation(float progress, float fadeInFraction,
            float fadeOutFraction, out float alpha, out float brightness)
        {
            float fadeInEnd = Mathf.Clamp(fadeInFraction, 0.01f, 0.15f);
            const float pulseEnd = 0.65f;
            float blinkEnd = Mathf.Clamp(1f - fadeOutFraction, pulseEnd + 0.01f, 1f);

            if (progress < fadeInEnd)
            {
                float fade = Mathf.SmoothStep(0f, 1f, progress / fadeInEnd);
                alpha = fade;
                brightness = Mathf.Lerp(1f, 1.12f, fade);
                return;
            }

            if (progress < pulseEnd)
            {
                float pulseProgress = (progress - fadeInEnd) / (pulseEnd - fadeInEnd);
                float pulse = (Mathf.Sin(pulseProgress * Mathf.PI * 4f) + 1f) * 0.5f;
                alpha = 0.96f + pulse * 0.04f;
                brightness = 1f + pulse * 0.035f;
                return;
            }

            if (progress < blinkEnd)
            {
                float blinkProgress = (progress - pulseEnd) / (blinkEnd - pulseEnd);
                float blink = (Mathf.Cos(blinkProgress * Mathf.PI * 4f) + 1f) * 0.5f;
                alpha = Mathf.Lerp(0.25f, 1f, blink);
                brightness = 1f;
                return;
            }

            float fadeOut = Mathf.SmoothStep(1f, 0f, (progress - blinkEnd) / (1f - blinkEnd));
            alpha = fadeOut;
            brightness = 1f;
        }

        // Áp dụng độ sáng và alpha lên màu gốc của hình Shield.
        private void SetShieldVisualAppearance(float alpha, float brightness)
        {
            if (_shieldVisual == null)
                return;

            Color color = _shieldVisualBaseColor;
            color.r = Mathf.Clamp01(color.r * brightness);
            color.g = Mathf.Clamp01(color.g * brightness);
            color.b = Mathf.Clamp01(color.b * brightness);
            color.a *= alpha;
            _shieldVisual.color = color;
        }

        // Báo cấu hình sai một lần rồi tắt handler để tránh lỗi runtime lặp lại.
        private void FailConfiguration(string message)
        {
            if (!_configurationErrorReported)
            {
                Debug.LogError(message, this);
                _configurationErrorReported = true;
            }

            enabled = false;
        }
    }
}
