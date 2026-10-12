using UnityEngine;

namespace SpaceExplorer.Core.Pooling
{
    public abstract class PooledObject : MonoBehaviour
    {
        private object _poolOwner;
        private bool _isLeased;
        private Vector3 _prefabScale;

        protected virtual void OnTakenFromPool() { }
        protected virtual void OnReturnedToPool() { }

        internal void InitializePool(object owner)
        {
            _poolOwner = owner;
            _prefabScale = transform.localScale;
            _isLeased = false;
        }

        internal bool TryLease(object owner)
        {
            if (_poolOwner != owner || _isLeased)
                return false;

            _isLeased = true;
            OnTakenFromPool();
            return true;
        }

        internal bool IsLeasedTo(object owner) => _poolOwner == owner && _isLeased;

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
