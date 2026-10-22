using UnityEngine;

namespace SpaceExplorer.Core.Pooling
{
    public abstract class PooledObject : MonoBehaviour
    {
        private object _poolOwner;
        private bool _isLeased;
        private Vector3 _prefabScale;

        // Hook để lớp con reset dữ liệu khi object được lấy khỏi pool.
        protected virtual void OnTakenFromPool() { }
        // Hook để lớp con dọn dữ liệu trước khi object trở lại pool.
        protected virtual void OnReturnedToPool() { }

        // Lưu owner và scale gốc để object có thể trả về trạng thái prefab.
        internal void InitializePool(object owner)
        {
            _poolOwner = owner;
            _prefabScale = transform.localScale;
            _isLeased = false;
        }

        // Chỉ chủ pool đúng mới được mượn object, tránh một pool dùng nhầm object của pool khác.
        internal bool TryLease(object owner)
        {
            if (_poolOwner != owner || _isLeased)
                return false;

            _isLeased = true;
            OnTakenFromPool();
            return true;
        }

        // Kiểm tra object đang được lease bởi đúng pool.
        internal bool IsLeasedTo(object owner) => _poolOwner == owner && _isLeased;

        // Trả object về trạng thái prefab trước khi pool cho lần sử dụng tiếp theo.
        internal bool TryReturn(object owner)
        {
            if (_poolOwner != owner || !_isLeased)
                return false;

            _isLeased = false;
            OnReturnedToPool();
            gameObject.SetActive(false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = _prefabScale;
            return true;
        }
    }
}
