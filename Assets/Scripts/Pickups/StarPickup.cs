using System.Collections.Generic;
using SpaceExplorer.Audio;
using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Pickups
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class StarPickup : PooledObject
    {
        private static readonly List<StarPickup> ActiveStars = new List<StarPickup>(64);

        [Header("Pickup")]
        [SerializeField, Min(1)] private int _xpValue = 1;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _minimumSpeed = 0.8f;
        [SerializeField, Min(0f)] private float _maximumSpeed = 1.4f;
        [SerializeField] private float _minimumVerticalDrift = -0.35f;
        [SerializeField] private float _maximumVerticalDrift = 0.35f;

        [Header("Magnet Movement")]
        [SerializeField, Min(0.01f)] private float _magnetAcceleration = 18f;

        private Rigidbody2D _rigidbody;
        private Renderer _renderer;
        private StarPool _owner;
        private Camera _camera;
        private Vector2 _driftVelocity;
        private Transform _attractionTarget;
        private float _attractionSpeed;
        private bool _isInUse;
        private bool _isRegisteredActive;

        public static int ActiveCount => ActiveStars.Count;

        // Trả về Star đang hoạt động theo chỉ số để bộ hút không cần tìm toàn scene.
        public static StarPickup GetActiveAt(int index)
        {
            return index >= 0 && index < ActiveStars.Count ? ActiveStars[index] : null;
        }

        // Lấy component vật lý và cấu hình Star thành vật thể trôi, không bị trọng lực kéo.
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            CircleCollider2D pickupCollider = GetComponent<CircleCollider2D>();
            pickupCollider.isTrigger = true;
            _renderer = GetComponent<Renderer>();
            _rigidbody.gravityScale = 0f;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        // Khởi tạo hướng trôi ngẫu nhiên và lưu pool/camera để Star tự thu hồi đúng lúc.
        public void BeginDrift(StarPool owner, Camera camera)
        {
            if (!_isInUse || owner == null || camera == null || !camera.orthographic)
            {
                if (owner != null)
                    owner.Release(this);
                return;
            }

            _owner = owner;
            _camera = camera;
            float speed = Random.Range(_minimumSpeed, Mathf.Max(_minimumSpeed, _maximumSpeed));
            float verticalDrift = Random.Range(_minimumVerticalDrift, Mathf.Max(_minimumVerticalDrift, _maximumVerticalDrift));
            _rigidbody.position = transform.position;
            _driftVelocity = new Vector2(-speed, verticalDrift);
            _rigidbody.linearVelocity = _driftVelocity;
            _rigidbody.angularVelocity = 0f;
        }

        // Gán Player làm mục tiêu để Star Magnet kéo Star về phía người chơi.
        public void SetAttractionTarget(Transform target, float speed)
        {
            if (!_isInUse || target == null || speed <= 0f)
                return;

            _attractionTarget = target;
            _attractionSpeed = speed;
        }

        // Xóa mục tiêu hút để Star trở lại chuyển động trôi tự nhiên.
        public void ClearAttractionTarget()
        {
            _attractionTarget = null;
            _attractionSpeed = 0f;
        }

        // Đăng ký Star đang được pool sử dụng vào danh sách phục vụ Star Magnet.
        private void OnEnable()
        {
            if (!_isInUse || _isRegisteredActive)
                return;

            ActiveStars.Add(this);
            _isRegisteredActive = true;
        }

        // Bỏ mục tiêu và danh sách khi Star bị tắt hoặc trả về pool.
        private void OnDisable()
        {
            ClearAttractionTarget();
            UnregisterActiveStar();
        }

        // Đảm bảo Star bị hủy cũng được gỡ khỏi danh sách đang hoạt động.
        private void OnDestroy()
        {
            UnregisterActiveStar();
        }

        // Xóa trạng thái cũ và vận tốc khi lấy Star mới từ pool.
        protected override void OnTakenFromPool()
        {
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            _owner = null;
            _camera = null;
            ClearAttractionTarget();
            _driftVelocity = Vector2.zero;
            _isInUse = true;
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
        }

        // Dọn tham chiếu và vận tốc trước khi Star được tái sử dụng.
        protected override void OnReturnedToPool()
        {
            _isInUse = false;
            _owner = null;
            _camera = null;
            ClearAttractionTarget();
            _driftVelocity = Vector2.zero;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                _rigidbody.angularVelocity = 0f;
            }
        }

        // Thu hồi Star nếu cấu hình sai hoặc ra khỏi camera; nếu không thì cập nhật chuyển động.
        private void FixedUpdate()
        {
            if (!_isInUse)
                return;

            if (_owner == null || _camera == null || _renderer == null || IsOutsideLeftEdge())
            {
                ReturnToPool();
                return;
            }

            UpdateMovement();
        }

        // Chỉ khi Player chạm Star mới cộng XP; sau đó thu hồi object để tránh cộng lặp.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isInUse || _owner == null || other == null || !other.TryGetComponent(out StarXPCollector collector))
                return;

            StarPool owner = _owner;
            _isInUse = false;
            if (_xpValue > 0)
                SfxAudioService.PlayXPCollect();
            collector.AddXP(_xpValue);
            owner.Release(this);
        }

        // Kiểm tra Star đã trôi hoàn toàn qua mép trái camera hay chưa.
        private bool IsOutsideLeftEdge()
        {
            float cameraLeft = _camera.transform.position.x - _camera.orthographicSize * _camera.aspect;
            return _renderer.bounds.max.x < cameraLeft;
        }

        // Star Magnet đổi vận tốc dần về Player; khi không bị hút, Star tiếp tục drift ban đầu.
        private void UpdateMovement()
        {
            Vector2 desiredVelocity = _driftVelocity;
            float acceleration = _magnetAcceleration;

            if (_attractionTarget != null && _attractionSpeed > 0f)
            {
                Vector2 toTarget = (Vector2)_attractionTarget.position - _rigidbody.position;
                float distance = toTarget.magnitude;
                if (distance > 0.001f)
                {
                    desiredVelocity = toTarget / distance * _attractionSpeed;
                    float maximumStepSpeed = distance / Time.fixedDeltaTime;
                    if (desiredVelocity.magnitude > maximumStepSpeed)
                        desiredVelocity = desiredVelocity.normalized * maximumStepSpeed;
                }
                else
                {
                    desiredVelocity = Vector2.zero;
                }

                Vector2 nextVelocity = Vector2.MoveTowards(
                    _rigidbody.linearVelocity,
                    desiredVelocity,
                    acceleration * Time.fixedDeltaTime);

                // Giới hạn vận tốc bước vật lý kế tiếp để Star không vượt qua Player.
                float maximumSafeSpeed = distance / Time.fixedDeltaTime;
                if (distance > 0f && nextVelocity.sqrMagnitude > maximumSafeSpeed * maximumSafeSpeed)
                    nextVelocity = nextVelocity.normalized * maximumSafeSpeed;

                _rigidbody.linearVelocity = nextVelocity;
                return;
            }

            _rigidbody.linearVelocity = Vector2.MoveTowards(
                _rigidbody.linearVelocity,
                desiredVelocity,
                acceleration * Time.fixedDeltaTime);
        }

        // Gỡ Star khỏi danh sách dùng chung để bộ hút không giữ object đã ngừng hoạt động.
        private void UnregisterActiveStar()
        {
            if (!_isRegisteredActive)
                return;

            ActiveStars.Remove(this);
            _isRegisteredActive = false;
        }

        // Trả Star về pool tối đa một lần nếu pool vẫn sở hữu nó.
        private void ReturnToPool()
        {
            if (!_isInUse || _owner == null)
                return;

            _isInUse = false;
            _owner.Release(this);
        }
    }
}
