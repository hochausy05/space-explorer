using SpaceExplorer.Core;
using SpaceExplorer.Pickups;
using UnityEngine;

namespace SpaceExplorer.Upgrades
{
    public sealed class StarMagnetController : MonoBehaviour
    {
        public const int MaximumLevel = 3;

        [Header("Attraction")]
        [SerializeField, Min(0.01f)] private float _attractionSpeed = 5f;

        private GameManager _gameManager;
        private int _level;

        public int Level => _level;
        public bool CanUpgrade => isActiveAndEnabled && _level < MaximumLevel;
        public float AttractionRadius => _level switch
        {
            1 => 3f,
            2 => 4f,
            3 => 5f,
            _ => 0f
        };

        private void OnEnable()
        {
            GameManager.RunStarted += ResetForNewRun;
            BindGameManager();
        }

        private void OnDisable()
        {
            GameManager.RunStarted -= ResetForNewRun;
            UnbindGameManager();
            ClearAttractionTargets();
        }

        private void FixedUpdate()
        {
            BindGameManager();
            if (_gameManager == null || _gameManager.State != RunState.Playing || _level <= 0)
            {
                ClearAttractionTargets();
                return;
            }

            float radius = AttractionRadius;
            float radiusSquared = radius * radius;
            Vector2 playerPosition = transform.position;

            for (int i = 0; i < StarPickup.ActiveCount; i++)
            {
                StarPickup star = StarPickup.GetActiveAt(i);
                if (star == null || !star.isActiveAndEnabled)
                    continue;

                Vector2 offset = (Vector2)star.transform.position - playerPosition;
                if (offset.sqrMagnitude <= radiusSquared)
                    star.SetAttractionTarget(transform, _attractionSpeed);
                else
                    star.ClearAttractionTarget();
            }
        }

        private void ResetForNewRun()
        {
            _level = 0;
            ClearAttractionTargets();
            BindGameManager();
        }

        private void HandleRunFinished(RunResult result)
        {
            ClearAttractionTargets();
        }

        public bool TryUpgrade()
        {
            if (!CanUpgrade)
                return false;

            _level++;
            return true;
        }

        private void BindGameManager()
        {
            GameManager current = GameManager.Instance;
            if (_gameManager == current)
                return;

            UnbindGameManager();
            _gameManager = current;
            if (_gameManager != null)
                _gameManager.RunFinished += HandleRunFinished;
        }

        private void UnbindGameManager()
        {
            if (_gameManager != null)
                _gameManager.RunFinished -= HandleRunFinished;

            _gameManager = null;
        }

        private void ClearAttractionTargets()
        {
            for (int i = 0; i < StarPickup.ActiveCount; i++)
            {
                StarPickup star = StarPickup.GetActiveAt(i);
                if (star != null)
                    star.ClearAttractionTarget();
            }
        }
    }
}
