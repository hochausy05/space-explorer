using SpaceExplorer.Core;
using UnityEngine;

namespace SpaceExplorer.Asteroids
{
    public sealed class AsteroidSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AsteroidPool _pool;
        [SerializeField] private Camera _camera;

        [Header("Spawn Settings")]
        [SerializeField, Min(0.75f)] private float _spawnInterval = 1.8f;
        [SerializeField, Min(0.01f)] private float _initialAsteroidSpeedMultiplier = 0.8f;
        [SerializeField, Range(0.01f, 1f)] private float _initialSpawnFrequencyMultiplier = 0.75f;
        [SerializeField, Min(0.01f)] private float _difficultyStepSeconds = 30f;
        [SerializeField, Min(0f)] private float _speedMultiplierIncreasePerStep = 0.05f;
        [SerializeField, Min(0f)] private float _frequencyMultiplierIncreasePerStep = 0.08f;
        [SerializeField, Min(1f)] private float _maximumSpeedMultiplier = 1.6f;
        [SerializeField, Min(1f)] private float _maximumFrequencyMultiplier = 2f;
        [SerializeField, Range(0f, 1f)] private float _largeAsteroidProbability = 0.2f;
        [SerializeField, Min(0f)] private float _verticalInset = 0.8f;
        [SerializeField, Min(0f)] private float _rightEdgeOffset = 0.5f;

        [Header("Special Asteroid Spawning")]
        [SerializeField] private bool _automaticSpecialSpawningEnabled;
        [SerializeField, Min(1f)] private float _speedVariantMinimumMultiplier = 1.5f;
        [SerializeField, Min(1f)] private float _speedVariantMaximumMultiplier = 2f;

        private float _timeUntilNextSpawn;
        private double _activeGameplayElapsed;
        private bool _runTimerInitialized;
        private bool _runStartedSubscribed;
        private bool _configurationErrorReported;
        private bool _specialConfigurationWarningReported;
        private bool _speedPoolWarningReported;
        private bool _homingPoolWarningReported;
        private bool _erraticPoolWarningReported;
        private bool _splitterPoolWarningReported;

        // Lấy reference cần thiết và kiểm tra thiết lập spawn ban đầu.
        private void Awake()
        {
            if (_pool == null)
            {
                FailConfiguration("AsteroidSpawner needs an AsteroidPool reference.");
                return;
            }

            if (_camera == null)
                _camera = Camera.main;
            if (!IsValidCamera(_camera))
            {
                FailConfiguration("AsteroidSpawner needs an axis-aligned orthographic gameplay camera.");
                return;
            }

            if (_spawnInterval <= 0f)
                FailConfiguration("AsteroidSpawner interval must be greater than zero.");
        }

        // Khởi tạo lịch spawn nếu run đã bắt đầu trước khi component Start chạy.
        private void Start()
        {
            if (_pool != null && !_pool.IsReady)
            {
                FailConfiguration("AsteroidSpawner cannot run because AsteroidPool is not ready.");
                return;
            }

            if (GameManager.Instance == null)
            {
                FailConfiguration("AsteroidSpawner requires a GameManager in the Gameplay scene.");
                return;
            }

            if (_automaticSpecialSpawningEnabled && !_pool.AreAllSpecialPoolsReady)
                WarnSpecialConfigurationOnce();

            if (GameManager.Instance.State == RunState.Playing && !_runTimerInitialized)
                ResetDifficultyForNewRun();
        }

        // Đăng ký reset đồng hồ khi một run mới bắt đầu.
        private void OnEnable()
        {
            if (!_runStartedSubscribed)
            {
                GameManager.RunStarted += ResetDifficultyForNewRun;
                _runStartedSubscribed = true;
            }
        }

        // Hủy đăng ký sự kiện để tránh giữ callback sau khi tắt component.
        private void OnDisable()
        {
            if (_runStartedSubscribed)
            {
                GameManager.RunStarted -= ResetDifficultyForNewRun;
                _runStartedSubscribed = false;
            }
        }

        // Đồng hồ chỉ tăng khi GameManager đang Playing; cùng thời gian này điều khiển stage và cadence.
        private void Update()
        {
            if (_pool == null || !IsValidCamera(_camera))
            {
                FailConfiguration("AsteroidSpawner lost its pool or camera reference.");
                return;
            }

            if (!IsRunActive())
                return;

            _activeGameplayElapsed += Time.deltaTime;
            _timeUntilNextSpawn -= Time.deltaTime;
            if (_timeUntilNextSpawn > 0f)
                return;

            // Giữ cadence DIFF-01; không thêm giới hạn số lượng hay cooldown riêng cho Special.
            _timeUntilNextSpawn = GetCurrentSpawnInterval();
            SpawnOne();
        }

        // Chỉ cho phép spawn khi GameManager xác nhận run đang Playing.
        private bool IsRunActive()
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.State == RunState.Playing;
        }

        // Thử Special theo xác suất hiện tại, nếu không spawn được thì giữ luồng Small/Large.
        private void SpawnOne()
        {
            if (_automaticSpecialSpawningEnabled)
            {
                if (_pool.AreAllSpecialPoolsReady)
                {
                    if (Random.value < AsteroidEvolution.GetSpecialSpawnChance(_activeGameplayElapsed))
                    {
                        AsteroidKind kind = ChooseSpecialKind();
                        if (TrySpawnSpecial(kind))
                            return;
                    }
                }
                else
                {
                    WarnSpecialConfigurationOnce();
                }
            }

            SpawnOrdinaryAsteroid();
        }

        // Sinh Asteroid thường sau khi lượt thử Special không được chọn hoặc thất bại.
        private void SpawnOrdinaryAsteroid()
        {
            AsteroidSize size = Random.value < Mathf.Clamp01(_largeAsteroidProbability)
                ? AsteroidSize.Large
                : AsteroidSize.Small;
            int health = AsteroidEvolution.GetMaximumHealth(size, _activeGameplayElapsed);
            _pool.TrySpawn(size, GetSpawnPosition(), GetCurrentAsteroidSpeedMultiplier(), health);
        }

        // Thử sinh Special bằng pool, áp dụng HP và tốc độ theo thời gian chơi.
        private bool TrySpawnSpecial(AsteroidKind kind)
        {
            Vector3 position = GetSpawnPosition();
            float difficultySpeed = GetCurrentAsteroidSpeedMultiplier();
            if (kind == AsteroidKind.Speed)
            {
                float minimum = Mathf.Clamp(Mathf.Min(_speedVariantMinimumMultiplier, _speedVariantMaximumMultiplier), 1.5f, 2f);
                float maximum = Mathf.Clamp(Mathf.Max(_speedVariantMinimumMultiplier, _speedVariantMaximumMultiplier), minimum, 2f);
                float variantMultiplier = Random.Range(minimum, maximum);
                int health = AsteroidEvolution.GetSpecialMaximumHealth(kind, _activeGameplayElapsed);
                if (_pool.TrySpawnSpeed(position, difficultySpeed, variantMultiplier, health) != null)
                    return true;
                WarnPoolFailureOnce(kind);
                return false;
            }

            int maximumHealth = AsteroidEvolution.GetSpecialMaximumHealth(kind, _activeGameplayElapsed);
            if (_pool.TrySpawnSpecial(kind, position, difficultySpeed, maximumHealth) != null)
                return true;
            WarnPoolFailureOnce(kind);
            return false;
        }

        // Chọn loại Special theo tỷ lệ phân phối đã cấu hình trong thiết kế.
        private AsteroidKind ChooseSpecialKind()
        {
            float roll = Random.value * 100f;
            if (roll < 35f)
                return AsteroidKind.Speed;
            if (roll < 65f)
                return AsteroidKind.Erratic;
            if (roll < 85f)
                return AsteroidKind.Homing;
            return AsteroidKind.Splitter;
        }

        // Tạo Speed Asteroid thủ công từ Context Menu khi đang Play Mode.
        [ContextMenu("Test/Spawn Speed Asteroid")]
        private void SpawnSpeedAsteroidForTesting() => SpawnSpecialForTesting(AsteroidKind.Speed);

        // Tạo Homing Asteroid thủ công từ Context Menu khi đang Play Mode.
        [ContextMenu("Test/Spawn Homing Asteroid")]
        private void SpawnHomingAsteroidForTesting() => SpawnSpecialForTesting(AsteroidKind.Homing);

        // Tạo Erratic Asteroid thủ công từ Context Menu khi đang Play Mode.
        [ContextMenu("Test/Spawn Erratic Asteroid")]
        private void SpawnErraticAsteroidForTesting() => SpawnSpecialForTesting(AsteroidKind.Erratic);

        // Tạo Splitter Asteroid thủ công từ Context Menu khi đang Play Mode.
        [ContextMenu("Test/Spawn Splitter Asteroid")]
        private void SpawnSplitterAsteroidForTesting() => SpawnSpecialForTesting(AsteroidKind.Splitter);

        // Chặn spawn thử ngoài Play Mode hoặc khi pool/camera/run chưa hợp lệ.
        private void SpawnSpecialForTesting(AsteroidKind kind)
        {
            if (!Application.isPlaying || !IsRunActive() || _pool == null || !IsValidCamera(_camera))
                return;

            if (!TrySpawnSpecial(kind))
                WarnPoolFailureOnce(kind);
        }

        // Chọn vị trí ngoài mép phải và giới hạn Y khỏi sát mép camera.
        private Vector3 GetSpawnPosition()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float inset = Mathf.Min(_verticalInset, halfHeight * 0.9f);
            float minimumY = _camera.transform.position.y - halfHeight + inset;
            float maximumY = _camera.transform.position.y + halfHeight - inset;
            float spawnY = Random.Range(minimumY, maximumY);
            Vector3 cameraPosition = _camera.transform.position;
            return new Vector3(cameraPosition.x + halfWidth + _rightEdgeOffset, spawnY, _pool.transform.position.z);
        }

        // Reset đồng hồ độ khó và thời gian chờ spawn cho run mới.
        private void ResetDifficultyForNewRun()
        {
            _activeGameplayElapsed = 0d;
            _timeUntilNextSpawn = GetCurrentSpawnInterval();
            _runTimerInitialized = true;
        }

        // Tính số bước DIFF-01 đã trôi qua, có giới hạn theo các hệ số tối đa.
        private int GetDifficultyStep()
        {
            if (_difficultyStepSeconds <= 0f)
                return 0;

            double steps = System.Math.Floor(_activeGameplayElapsed / _difficultyStepSeconds);
            double speedSteps = _speedMultiplierIncreasePerStep > 0f
                ? (_maximumSpeedMultiplier - 1f) / _speedMultiplierIncreasePerStep
                : 0d;
            double frequencySteps = _frequencyMultiplierIncreasePerStep > 0f
                ? (_maximumFrequencyMultiplier - 1f) / _frequencyMultiplierIncreasePerStep
                : 0d;
            double maximumSteps = System.Math.Ceiling(System.Math.Max(speedSteps, frequencySteps));
            return (int)System.Math.Min(System.Math.Min(steps, maximumSteps), int.MaxValue);
        }

        // Tính hệ số tốc độ áp dụng cho Asteroid mới được sinh.
        private float GetCurrentAsteroidSpeedMultiplier()
        {
            return _initialAsteroidSpeedMultiplier * Mathf.Min(
                1f + GetDifficultyStep() * _speedMultiplierIncreasePerStep, _maximumSpeedMultiplier);
        }

        // Tính khoảng chờ spawn mới theo hệ số tần suất hiện tại.
        private float GetCurrentSpawnInterval()
        {
            float frequencyMultiplier = _initialSpawnFrequencyMultiplier * Mathf.Min(
                1f + GetDifficultyStep() * _frequencyMultiplierIncreasePerStep, _maximumFrequencyMultiplier);
            return _spawnInterval / Mathf.Max(0.01f, frequencyMultiplier);
        }

        // Báo thiếu cấu hình Special một lần nhưng vẫn giữ spawn thường hoạt động.
        private void WarnSpecialConfigurationOnce()
        {
            if (_specialConfigurationWarningReported)
                return;

            Debug.LogError("Automatic Special Asteroid spawning is enabled but AsteroidPool is missing a Special/fragment prefab, StarPool, or Player target reference. Ordinary Small/Large spawning continues.", this);
            _specialConfigurationWarningReported = true;
        }

        // Báo lỗi pool theo từng loại Special, mỗi loại chỉ ghi một cảnh báo.
        private void WarnPoolFailureOnce(AsteroidKind kind)
        {
            if (kind == AsteroidKind.Speed)
            {
                if (_speedPoolWarningReported) return;
                _speedPoolWarningReported = true;
            }
            else if (kind == AsteroidKind.Homing)
            {
                if (_homingPoolWarningReported) return;
                _homingPoolWarningReported = true;
            }
            else if (kind == AsteroidKind.Erratic)
            {
                if (_erraticPoolWarningReported) return;
                _erraticPoolWarningReported = true;
            }
            else if (kind == AsteroidKind.Splitter)
            {
                if (_splitterPoolWarningReported) return;
                _splitterPoolWarningReported = true;
            }

            Debug.LogWarning($"{kind} Asteroid spawn failed; verify its prefab, pool, Player target, and capacity configuration.", this);
        }

        // Chỉ nhận camera orthographic, không nghiêng và có aspect hợp lệ.
        private static bool IsValidCamera(Camera camera)
        {
            if (camera == null || !camera.orthographic || camera.aspect <= 0f)
                return false;

            Vector3 angles = camera.transform.eulerAngles;
            return Mathf.Abs(Mathf.DeltaAngle(0f, angles.x)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.y)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.z)) < 0.01f;
        }

        // Dừng spawner sau lỗi cấu hình và chỉ ghi lỗi một lần.
        private void FailConfiguration(string message)
        {
            if (_configurationErrorReported)
                return;

            Debug.LogError(message, this);
            _configurationErrorReported = true;
            enabled = false;
        }
    }
}
