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
        [SerializeField, Range(0f, 1f)] private float _largeAsteroidProbability = 0.2f;
        [SerializeField, Min(0f)] private float _verticalInset = 0.8f;
        [SerializeField, Min(0f)] private float _rightEdgeOffset = 0.5f;

        private float _timeUntilNextSpawn;
        private bool _configurationErrorReported;

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
            {
                FailConfiguration("AsteroidSpawner interval must be greater than zero.");
                return;
            }
        }

        private void Start()
        {
            // Start runs after every scene object's Awake, including AsteroidPool.Awake.
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

            _timeUntilNextSpawn = _spawnInterval;
        }

        private void Update()
        {
            if (_pool == null || !IsValidCamera(_camera))
            {
                FailConfiguration("AsteroidSpawner lost its pool or camera reference.");
                return;
            }

            if (!IsRunActive())
                return;

            _timeUntilNextSpawn -= Time.deltaTime;
            if (_timeUntilNextSpawn > 0f)
                return;

            // Skip catch-up bursts after a hitch; pool capacity remains the active-object limit.
            _timeUntilNextSpawn = _spawnInterval;
            SpawnOne();
        }

        private bool IsRunActive()
        {
            GameManager manager = GameManager.Instance;
            return manager != null && manager.State == RunState.Playing;
        }

        private void SpawnOne()
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            float inset = Mathf.Min(_verticalInset, halfHeight * 0.9f);
            float minimumY = _camera.transform.position.y - halfHeight + inset;
            float maximumY = _camera.transform.position.y + halfHeight - inset;
            float spawnY = Random.Range(minimumY, maximumY);

            AsteroidSize size = Random.value < Mathf.Clamp01(_largeAsteroidProbability)
                ? AsteroidSize.Large
                : AsteroidSize.Small;

            Vector3 cameraPosition = _camera.transform.position;
            Vector3 spawnPosition = new Vector3(
                cameraPosition.x + halfWidth + _rightEdgeOffset,
                spawnY,
                _pool.transform.position.z);

            // A full pool is an expected limit; the next interval will retry without logging each time.
            _pool.TrySpawn(size, spawnPosition);
        }

        private static bool IsValidCamera(Camera camera)
        {
            if (camera == null || !camera.orthographic || camera.aspect <= 0f)
                return false;

            Vector3 angles = camera.transform.eulerAngles;
            return Mathf.Abs(Mathf.DeltaAngle(0f, angles.x)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.y)) < 0.01f
                && Mathf.Abs(Mathf.DeltaAngle(0f, angles.z)) < 0.01f;
        }

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
