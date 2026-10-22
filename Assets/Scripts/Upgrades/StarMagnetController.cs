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
        // Chỉ cho nâng cấp khi controller đang hoạt động và còn cấp trống.
        public bool CanUpgrade => isActiveAndEnabled && _level < MaximumLevel;
        // Trả bán kính hút tương ứng cấp Star Magnet hiện tại.
        public float AttractionRadius => _level switch
        {
            1 => 3f,
            2 => 4f,
            3 => 5f,
            _ => 0f
        };

        // Đăng ký vòng đời run và tìm GameManager để theo dõi trạng thái chơi.
        private void OnEnable()
        {
            GameManager.RunStarted += ResetForNewRun;
            BindGameManager();
        }

        // Hủy đăng ký, ngắt liên kết và trả các Star về chuyển động tự nhiên.
        private void OnDisable()
        {
            GameManager.RunStarted -= ResetForNewRun;
            UnbindGameManager();
            ClearAttractionTargets();
        }

        // Duyệt danh sách Star đang hoạt động để cập nhật mục tiêu hút mà không tìm toàn scene.
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

        // Đặt lại cấp hút và xóa mọi mục tiêu Star của run trước.
        private void ResetForNewRun()
        {
            _level = 0;
            ClearAttractionTargets();
            BindGameManager();
        }

        // Dừng hút Star khi GameManager kết thúc run.
        private void HandleRunFinished(RunResult result)
        {
            ClearAttractionTargets();
        }

        // Tăng cấp Star Magnet nếu còn dưới giới hạn tối đa.
        public bool TryUpgrade()
        {
            if (!CanUpgrade)
                return false;

            _level++;
            return true;
        }

        // Đồng bộ đăng ký RunFinished với GameManager hiện hành.
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

        // Hủy đăng ký RunFinished và xóa tham chiếu GameManager.
        private void UnbindGameManager()
        {
            if (_gameManager != null)
                _gameManager.RunFinished -= HandleRunFinished;

            _gameManager = null;
        }

        // Xóa mục tiêu hút khỏi mọi Star đang hoạt động.
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
