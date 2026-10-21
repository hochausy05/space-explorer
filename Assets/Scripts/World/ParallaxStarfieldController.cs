using UnityEngine;

namespace SpaceExplorer.World
{
    public sealed class ParallaxStarfieldController : MonoBehaviour
    {
        private struct StarVisual
        {
            public Transform Transform;
            public float HalfSize;

            public StarVisual(Transform transform, float halfSize)
            {
                Transform = transform;
                HalfSize = halfSize;
            }
        }

        [Header("Camera and Rendering")]
        [SerializeField] private Camera _gameplayCamera;
        [SerializeField] private float _worldZ;
        [SerializeField] private string _sortingLayerName = "Default";
        [SerializeField] private int _farSortingOrder = -60;
        [SerializeField] private int _nearSortingOrder = -50;

        [Header("FarStars")]
        [SerializeField, Min(0)] private int _farStarCount = 45;
        [SerializeField, Min(0f)] private float _farSpeed = 0.2f;
        [SerializeField, Min(0f)] private float _farMinimumSize = 0.035f;
        [SerializeField, Min(0f)] private float _farMaximumSize = 0.065f;
        [SerializeField, Range(0f, 1f)] private float _farMinimumOpacity = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _farMaximumOpacity = 0.45f;
        [SerializeField] private Color _farColor = new Color(0.78f, 0.94f, 1f, 1f);

        [Header("NearStars")]
        [SerializeField, Min(0)] private int _nearStarCount = 20;
        [SerializeField, Min(0f)] private float _nearSpeed = 0.6f;
        [SerializeField, Min(0f)] private float _nearMinimumSize = 0.07f;
        [SerializeField, Min(0f)] private float _nearMaximumSize = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _nearMinimumOpacity = 0.45f;
        [SerializeField, Range(0f, 1f)] private float _nearMaximumOpacity = 0.7f;
        [SerializeField] private Color _nearColor = new Color(0.9f, 0.98f, 1f, 1f);

        [Header("Procedural Sprite")]
        [SerializeField, Range(8, 64)] private int _spriteResolution = 32;
        [SerializeField, Min(8f)] private float _spritePixelsPerUnit = 32f;

        private StarVisual[] _farStars;
        private StarVisual[] _nearStars;
        private Texture2D _softStarTexture;
        private Sprite _softStarSprite;
        private float _softStarWorldDiameter;
        private Bounds _previousCameraBounds;
        private bool _hasPreviousCameraBounds;
        private bool _configurationErrorReported;

        private void Awake()
        {
            if (_gameplayCamera == null)
                _gameplayCamera = Camera.main;

            if (_gameplayCamera == null || !_gameplayCamera.orthographic)
            {
                FailConfiguration("ParallaxStarfieldController requires an orthographic Gameplay Camera.");
                return;
            }

            _softStarSprite = CreateSoftStarSprite();
            if (_softStarSprite == null)
            {
                FailConfiguration("ParallaxStarfieldController could not create its procedural star sprite.");
                return;
            }

            Bounds bounds = GetCameraBounds();
            _farStars = CreateLayer("FarStars", _farStarCount, _farMinimumSize, _farMaximumSize,
                _farMinimumOpacity, _farMaximumOpacity, _farColor, _farSortingOrder, bounds);
            _nearStars = CreateLayer("NearStars", _nearStarCount, _nearMinimumSize, _nearMaximumSize,
                _nearMinimumOpacity, _nearMaximumOpacity, _nearColor, _nearSortingOrder, bounds);
            _previousCameraBounds = bounds;
            _hasPreviousCameraBounds = true;
        }

        private void Update()
        {
            if (_gameplayCamera == null || !_gameplayCamera.orthographic)
                return;

            Bounds bounds = GetCameraBounds();
            if (_hasPreviousCameraBounds && CameraBoundsChanged(_previousCameraBounds, bounds))
                ReprojectStars(_previousCameraBounds, bounds);

            _previousCameraBounds = bounds;
            _hasPreviousCameraBounds = true;

            // Scaled time freezes both layers during Settings pause and level-up selection.
            float deltaTime = Time.deltaTime;
            MoveLayer(_farStars, _farSpeed, deltaTime, bounds);
            MoveLayer(_nearStars, _nearSpeed, deltaTime, bounds);
        }

        private StarVisual[] CreateLayer(string layerName, int count, float minimumSize, float maximumSize,
            float minimumOpacity, float maximumOpacity, Color color, int sortingOrder, Bounds bounds)
        {
            var layerObject = new GameObject(layerName);
            Transform layerTransform = layerObject.transform;
            layerTransform.SetParent(transform, false);
            layerTransform.localPosition = Vector3.zero;
            layerTransform.localRotation = Quaternion.identity;
            layerTransform.localScale = Vector3.one;

            int safeCount = Mathf.Max(0, count);
            var stars = new StarVisual[safeCount];
            float minSize = Mathf.Min(minimumSize, maximumSize);
            float maxSize = Mathf.Max(minimumSize, maximumSize);
            float minOpacity = Mathf.Clamp01(Mathf.Min(minimumOpacity, maximumOpacity));
            float maxOpacity = Mathf.Clamp01(Mathf.Max(minimumOpacity, maximumOpacity));

            for (int i = 0; i < stars.Length; i++)
            {
                var starObject = new GameObject($"{layerName}_{i + 1:00}");
                Transform starTransform = starObject.transform;
                starTransform.SetParent(layerTransform, false);

                float size = Random.Range(minSize, maxSize);
                float halfSize = size * 0.5f;
                starTransform.localScale = Vector3.one * (size / _softStarWorldDiameter);
                starTransform.position = RandomVisiblePosition(bounds, halfSize);

                SpriteRenderer renderer = starObject.AddComponent<SpriteRenderer>();
                renderer.sprite = _softStarSprite;
                renderer.color = new Color(color.r, color.g, color.b,
                    color.a * Random.Range(minOpacity, maxOpacity));
                renderer.sortingLayerName = _sortingLayerName;
                renderer.sortingOrder = sortingOrder;
                stars[i] = new StarVisual(starTransform, halfSize);
            }

            return stars;
        }

        private void MoveLayer(StarVisual[] stars, float speed, float deltaTime, Bounds bounds)
        {
            if (stars == null || deltaTime <= 0f)
                return;

            float left = bounds.min.x;
            float right = bounds.max.x;
            float recycleStagger = Mathf.Max(0.15f, speed * 1.5f);

            for (int i = 0; i < stars.Length; i++)
            {
                StarVisual star = stars[i];
                if (star.Transform == null)
                    continue;

                Vector3 position = star.Transform.position;
                position.x -= Mathf.Max(0f, speed) * deltaTime;
                position.z = _worldZ;

                if (position.x + star.HalfSize < left)
                {
                    position.x = right + star.HalfSize + Random.Range(0f, recycleStagger);
                    position.y = RandomVisibleY(bounds, star.HalfSize);
                }

                star.Transform.position = position;
            }
        }

        private void ReprojectStars(Bounds oldBounds, Bounds newBounds)
        {
            ReprojectLayer(_farStars, oldBounds, newBounds);
            ReprojectLayer(_nearStars, oldBounds, newBounds);
        }

        private void ReprojectLayer(StarVisual[] stars, Bounds oldBounds, Bounds newBounds)
        {
            if (stars == null)
                return;

            float oldWidth = Mathf.Max(0.001f, oldBounds.size.x);
            float oldHeight = Mathf.Max(0.001f, oldBounds.size.y);

            for (int i = 0; i < stars.Length; i++)
            {
                StarVisual star = stars[i];
                if (star.Transform == null)
                    continue;

                Vector3 position = star.Transform.position;
                float normalizedX = (position.x - oldBounds.min.x) / oldWidth;
                float normalizedY = (position.y - oldBounds.min.y) / oldHeight;
                position.x = newBounds.min.x + normalizedX * newBounds.size.x;
                position.y = newBounds.min.y + normalizedY * newBounds.size.y;
                position.z = _worldZ;
                star.Transform.position = position;
            }
        }

        private Sprite CreateSoftStarSprite()
        {
            int resolution = Mathf.Clamp(_spriteResolution, 8, 64);
            var pixels = new Color32[resolution * resolution];
            float center = (resolution - 1) * 0.5f;
            float radius = resolution * 0.5f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01((1f - distance) / 0.28f);
                    byte alpha = (byte)Mathf.RoundToInt(edge * edge * 255f);
                    pixels[y * resolution + x] = new Color32(255, 255, 255, alpha);
                }
            }

            _softStarTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                name = "Runtime Soft Starfield Sprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            _softStarTexture.SetPixels32(pixels);
            _softStarTexture.Apply(false, true);

            Sprite sprite = Sprite.Create(_softStarTexture, new Rect(0f, 0f, resolution, resolution),
                new Vector2(0.5f, 0.5f), Mathf.Max(8f, _spritePixelsPerUnit), 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.DontSave;
            _softStarWorldDiameter = resolution / Mathf.Max(8f, _spritePixelsPerUnit);
            return sprite;
        }

        private Vector3 RandomVisiblePosition(Bounds bounds, float inset)
        {
            return new Vector3(
                RandomVisibleCoordinate(bounds.min.x, bounds.max.x, bounds.center.x, inset),
                RandomVisibleY(bounds, inset),
                _worldZ);
        }

        private static float RandomVisibleY(Bounds bounds, float inset)
        {
            return RandomVisibleCoordinate(bounds.min.y, bounds.max.y, bounds.center.y, inset);
        }

        private static float RandomVisibleCoordinate(float minimum, float maximum, float center, float inset)
        {
            float low = minimum + inset;
            float high = maximum - inset;
            return high < low ? center : Random.Range(low, high);
        }

        private Bounds GetCameraBounds()
        {
            float halfHeight = _gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * _gameplayCamera.aspect;
            Vector3 center = _gameplayCamera.transform.position;
            center.z = _worldZ;
            return new Bounds(center, new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
        }

        private static bool CameraBoundsChanged(Bounds previous, Bounds current)
        {
            const float tolerance = 0.0001f;
            return (previous.center - current.center).sqrMagnitude > tolerance * tolerance
                || (previous.size - current.size).sqrMagnitude > tolerance * tolerance;
        }

        private void OnDestroy()
        {
            if (_softStarSprite != null)
            {
                if (Application.isPlaying)
                    Destroy(_softStarSprite);
                else
                    DestroyImmediate(_softStarSprite);
            }

            if (_softStarTexture != null)
            {
                if (Application.isPlaying)
                    Destroy(_softStarTexture);
                else
                    DestroyImmediate(_softStarTexture);
            }
        }

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
