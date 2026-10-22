using SpaceExplorer.Core.Pooling;
using UnityEngine;

namespace SpaceExplorer.Effects
{
    public sealed class PooledAsteroidExplosion : PooledObject
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private AsteroidExplosionPool _owner;
        private int _animationStateHash;
        private Vector3 _prefabScale;
        private bool _isPlaying;

        // Tìm Animator/SpriteRenderer và đặt animation dùng scaled time để pause cùng game.
        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            _prefabScale = transform.localScale;

            if (_animator == null || _animator.runtimeAnimatorController == null || _spriteRenderer == null)
                return;

            // Animator dùng scaled time nên animation cũng dừng khi Gameplay pause.
            _animator.updateMode = AnimatorUpdateMode.Normal;
        }

        // Kiểm tra prefab có Animator, SpriteRenderer và không có thành phần vật lý.
        internal bool TryValidate(out string error)
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                error = "The effect prefab needs its existing Animator and Animator Controller.";
                return false;
            }

            if (_spriteRenderer == null)
            {
                error = "The effect prefab needs its existing SpriteRenderer.";
                return false;
            }

            if (GetComponentInChildren<Collider2D>(true) != null || GetComponentInChildren<Rigidbody2D>(true) != null)
            {
                error = "The visual effect prefab must not contain a Collider2D or Rigidbody2D.";
                return false;
            }

            error = null;
            return true;
        }

        // Đặt lại scale và Animator trước mỗi lần dùng để hiệu ứng pool luôn chạy từ đầu.
        internal bool BeginPlayback(AsteroidExplosionPool owner, float scaleMultiplier)
        {
            if (owner == null || _animator == null || _spriteRenderer == null || !_animator.isActiveAndEnabled)
                return false;

            transform.localScale = _prefabScale * Mathf.Max(0f, scaleMultiplier);
            _spriteRenderer.enabled = true;
            if (!ResetAnimation())
                return false;

            _owner = owner;
            _isPlaying = true;
            return true;
        }

        // Theo dõi clip hiện tại và trả hiệu ứng khi animation chạy xong.
        private void Update()
        {
            if (!_isPlaying)
                return;

            if (_animator == null || !_animator.isActiveAndEnabled)
            {
                ReturnToPool();
                return;
            }

            if (_animator.IsInTransition(0))
                return;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == _animationStateHash && state.normalizedTime >= 1f)
                ReturnToPool();
        }

        // Trả hiệu ứng về pool nếu hệ thống khác tắt object khi nó đang chạy.
        private void OnDisable()
        {
            // Nếu hệ thống khác tắt hiệu ứng đang chạy, trả slot về pool để có thể dùng lại.
            if (_isPlaying)
                ReturnToPool();
        }

        // Xóa owner và trạng thái phát khi lấy object khỏi pool.
        protected override void OnTakenFromPool()
        {
            _owner = null;
            _isPlaying = false;
        }

        // Ẩn renderer và xóa trạng thái trước lần tái sử dụng tiếp theo.
        protected override void OnReturnedToPool()
        {
            _isPlaying = false;
            _owner = null;
            if (_spriteRenderer != null)
                _spriteRenderer.enabled = false;
        }

        // Đưa Animator về frame đầu và lưu mã state để theo dõi thời lượng phát.
        private bool ResetAnimation()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.layerCount == 0)
                return false;

            _animator.Rebind();
            _animator.Update(0f);
            _animationStateHash = _animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            if (_animationStateHash == 0)
                return false;

            _animator.Play(_animationStateHash, 0, 0f);
            _animator.Update(0f);
            return true;
        }

        // Trả hiệu ứng hiện tại về pool và xóa liên kết owner trước callback.
        private void ReturnToPool()
        {
            AsteroidExplosionPool owner = _owner;
            _isPlaying = false;
            _owner = null;
            owner?.Release(this);
        }

    }
}
