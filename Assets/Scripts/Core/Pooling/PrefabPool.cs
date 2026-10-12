using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpaceExplorer.Core.Pooling
{
    public sealed class PrefabPool<T> : IDisposable where T : PooledObject
    {
        private readonly T _prefab;
        private readonly Transform _root;
        private readonly Stack<T> _available;
        private readonly int _capacity;
        private int _totalCount;
        private int _activeCount;
        private bool _disposed;

        public int Capacity => _capacity;
        public int TotalCount => _totalCount;
        public int ActiveCount => _activeCount;
        public int AvailableCount => _available.Count;

        public PrefabPool(T prefab, int capacity, int prewarmCount = 0, Transform parent = null)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Pool capacity must be at least one.");
            if (prewarmCount < 0 || prewarmCount > capacity)
                throw new ArgumentOutOfRangeException(nameof(prewarmCount), "Prewarm count must be between zero and capacity.");

            _prefab = prefab;
            _capacity = capacity;
            _available = new Stack<T>(capacity);
            var rootObject = new GameObject($"{prefab.name} Pool");
            _root = rootObject.transform;
            _root.SetParent(parent, false);

            for (int i = 0; i < prewarmCount; i++)
                _available.Push(CreateInstance());
        }

        public T Get(Vector3 position, Quaternion rotation)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(PrefabPool<T>));

            T item;
            if (_available.Count > 0)
            {
                item = _available.Pop();
            }
            else
            {
                if (_totalCount >= _capacity)
                    return null;
                item = CreateInstance();
            }

            item.transform.SetPositionAndRotation(position, rotation);
            _activeCount++;
            if (!item.TryLease(this))
            {
                _activeCount--;
                throw new InvalidOperationException("Pool returned an object that was already leased or belongs to another pool.");
            }

            // A pool callback may release the object immediately; don't reactivate it in that case.
            if (!item.IsLeasedTo(this))
                return null;

            item.gameObject.SetActive(true);
            if (!item.IsLeasedTo(this))
                return null;
            return item;
        }

        public bool Release(T item)
        {
            if (_disposed || item == null || !item.TryReturn(this))
                return false;

            _activeCount--;
            _available.Push(item);
            return true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_root.gameObject);
            else
                UnityEngine.Object.DestroyImmediate(_root.gameObject);

            _available.Clear();
            _totalCount = 0;
            _activeCount = 0;
        }

        private T CreateInstance()
        {
            T item = UnityEngine.Object.Instantiate(_prefab, _root, false);
            item.gameObject.SetActive(false);
            item.InitializePool(this);
            _totalCount++;
            return item;
        }
    }
}
