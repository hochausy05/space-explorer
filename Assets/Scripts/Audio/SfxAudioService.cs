using UnityEngine;

namespace SpaceExplorer.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxAudioService : MonoBehaviour
    {
        private const string MutePreferenceKey = "SpaceExplorer.SfxMuted.v1";

        private enum Sound
        {
            Shoot,
            BulletHit,
            AsteroidBreak,
            ShieldHit,
            XPCollect,
            LevelUp,
            Click,
            GameOver,
            Count
        }

        private const int VoiceCount = 8;
        private const int GameOverVoiceIndex = VoiceCount - 1;
        private const int RegularVoiceCount = GameOverVoiceIndex;

        public static SfxAudioService Instance { get; private set; }
        // Đọc trạng thái mute hiện tại, kể cả khi service chưa được tạo.
        public static bool IsMuted => Instance != null
            ? Instance._isMuted
            : PlayerPrefs.GetInt(MutePreferenceKey, 0) != 0;

        [Header("Audio Output")]
        [SerializeField] private AudioSource _audioSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip _shootClip;
        [SerializeField] private AudioClip _bulletHitClip;
        [SerializeField] private AudioClip _asteroidBreakClip;
        [SerializeField] private AudioClip _shieldHitClip;
        [SerializeField] private AudioClip _xpCollectClip;
        [SerializeField] private AudioClip _levelUpClip;
        [SerializeField] private AudioClip _clickClip;
        [SerializeField] private AudioClip _gameOverClip;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _shootVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _bulletHitVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _asteroidBreakVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _shieldHitVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _xpCollectVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float _levelUpVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _clickVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _gameOverVolume = 1f;

        private readonly bool[] _hasPlayedRecently = new bool[(int)Sound.Count];
        private readonly double[] _lastPlayedAt = new double[(int)Sound.Count];
        private AudioSource[] _voices;
        private int _nextVoiceIndex;
        private long _pendingLevelUpSounds;
        private int _levelUpVoiceIndex = -1;
        private float _sourceVolume = 1f;
        private bool _sourceMuted;
        private bool _isMuted;
        private bool _missingClipWarningReported;
        private static bool _missingServiceWarningReported;

        // Phát tiếng bắn một lần cho sự kiện volley của Player.
        public static void PlayShoot() => PlayIfAvailable(Sound.Shoot);
        // Phát âm thanh khi đạn Player trúng mục tiêu.
        public static void PlayBulletHit() => PlayIfAvailable(Sound.BulletHit);
        // Phát âm thanh khi Asteroid bị phá hủy bởi đạn.
        public static void PlayAsteroidBreak() => PlayIfAvailable(Sound.AsteroidBreak);
        // Phát âm thanh khi Energy Shield hấp thụ va chạm.
        public static void PlayShieldHit() => PlayIfAvailable(Sound.ShieldHit);
        // Phát âm thanh khi Player nhặt được Star XP.
        public static void PlayXPCollect() => PlayIfAvailable(Sound.XPCollect);
        // Xếp tiếng Level Up để phát đủ âm thanh cho nhiều cấp liên tiếp.
        public static void PlayLevelUp()
        {
            if (Instance != null)
            {
                Instance.QueueLevelUp();
                return;
            }

            WarnMissingServiceOnce();
        }
        // Phát tiếng nhấn cho nút UI hoặc lựa chọn nâng cấp.
        public static void PlayClick() => PlayIfAvailable(Sound.Click);
        // Phát tiếng kết thúc run qua kênh ưu tiên riêng.
        public static void PlayGameOver() => PlayIfAvailable(Sound.GameOver);

        // Lưu lựa chọn mute và cập nhật các AudioSource đang phát.
        public static void SetMuted(bool muted)
        {
            PlayerPrefs.SetInt(MutePreferenceKey, muted ? 1 : 0);
            PlayerPrefs.Save();

            if (Instance != null)
                Instance.ApplyMute(muted);
        }

        // Khởi tạo singleton, các voice âm thanh và trạng thái mute đã lưu.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _missingServiceWarningReported = false;

            if (_audioSource == null || _audioSource.gameObject != gameObject)
                _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                Debug.LogError("SfxAudioService requires an AudioSource on the same GameObject.", this);
                enabled = false;
                Instance = null;
                return;
            }

            _sourceVolume = _audioSource.volume;
            _sourceMuted = _audioSource.mute;
            _isMuted = PlayerPrefs.GetInt(MutePreferenceKey, 0) != 0;
            _voices = new AudioSource[VoiceCount];
            _voices[0] = _audioSource;
            ConfigureVoice(_audioSource);
            for (int i = 1; i < _voices.Length; i++)
            {
                AudioSource voice = gameObject.AddComponent<AudioSource>();
                voice.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;
                voice.priority = _audioSource.priority;
                voice.mute = _audioSource.mute;
                voice.bypassEffects = _audioSource.bypassEffects;
                voice.bypassListenerEffects = _audioSource.bypassListenerEffects;
                voice.bypassReverbZones = _audioSource.bypassReverbZones;
                voice.ignoreListenerPause = _audioSource.ignoreListenerPause;
                voice.ignoreListenerVolume = _audioSource.ignoreListenerVolume;
                voice.pitch = _audioSource.pitch;
                voice.panStereo = _audioSource.panStereo;
                _voices[i] = ConfigureVoice(voice);
            }

            ApplyMute(_isMuted);

            DontDestroyOnLoad(gameObject);
            ReportMissingClipsOnce();
        }

        // Xóa singleton khi đúng instance hiện tại bị hủy.
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Phát tiếp tiếng Level Up đang chờ khi voice trước đã rảnh.
        private void Update()
        {
            if (_pendingLevelUpSounds > 0)
                TryPlayNextLevelUp();
        }

        // Chuyển yêu cầu phát âm thanh tới service nếu service đang tồn tại.
        private static void PlayIfAvailable(Sound sound)
        {
            if (Instance != null)
            {
                Instance.Play(sound);
                return;
            }

            WarnMissingServiceOnce();
        }

        // Chỉ cảnh báo một lần khi scene chưa có SfxAudioService.
        private static void WarnMissingServiceOnce()
        {
            if (_missingServiceWarningReported)
                return;

            Debug.LogWarning("SFX was requested but no SfxAudioService exists in the loaded scene.");
            _missingServiceWarningReported = true;
        }

        // Xếp âm thanh Level Up để nhiều cấp nhận từ một lần cộng XP không bị mất tiếng.
        private void QueueLevelUp()
        {
            if (_isMuted || _levelUpClip == null)
                return;

            _pendingLevelUpSounds++;
            TryPlayNextLevelUp();
        }

        // Phát âm thanh Level Up kế tiếp nếu có voice trống.
        private void TryPlayNextLevelUp()
        {
            if (_pendingLevelUpSounds <= 0 || _levelUpClip == null)
                return;

            if (_levelUpVoiceIndex >= 0 && _voices[_levelUpVoiceIndex] != null
                && _voices[_levelUpVoiceIndex].isPlaying)
                return;

            _levelUpVoiceIndex = -1;
            int voiceIndex = FindFreeRegularVoice();
            if (voiceIndex < 0)
                return;

            _pendingLevelUpSounds--;
            _levelUpVoiceIndex = voiceIndex;
            PlayOnVoice(Sound.LevelUp, voiceIndex);
        }

        // Chọn voice rảnh và giới hạn nhịp lặp để nhiều sự kiện không làm âm thanh dồn quá mức.
        private void Play(Sound sound)
        {
            if (_isMuted)
                return;

            AudioClip clip = GetClip(sound);
            if (clip == null)
                return;

            double now = Time.unscaledTimeAsDouble;
            int soundIndex = (int)sound;
            if (_hasPlayedRecently[soundIndex] && now - _lastPlayedAt[soundIndex] < GetRepeatInterval(sound))
                return;

            int voiceIndex = sound == Sound.GameOver
                ? FindFreeGameOverVoice()
                : FindFreeRegularVoice();
            if (voiceIndex < 0)
                return;

            PlayOnVoice(sound, voiceIndex);
            _hasPlayedRecently[soundIndex] = true;
            _lastPlayedAt[soundIndex] = now;
        }

        // Gán clip và âm lượng cho một voice rồi bắt đầu phát.
        private void PlayOnVoice(Sound sound, int voiceIndex)
        {
            AudioSource voice = _voices[voiceIndex];
            voice.volume = Mathf.Clamp01(_sourceVolume * _masterVolume * GetEffectVolume(sound));
            voice.clip = GetClip(sound);
            voice.Play();
        }

        // Cập nhật mute cho trạng thái service và toàn bộ voice.
        private void ApplyMute(bool muted)
        {
            _isMuted = muted;
            if (muted)
                _pendingLevelUpSounds = 0;

            if (_voices == null)
                return;

            for (int i = 0; i < _voices.Length; i++)
            {
                if (_voices[i] != null)
                    _voices[i].mute = _sourceMuted || muted;
            }
        }

        // Chỉ dùng voice Game Over riêng khi nó đang rảnh.
        private int FindFreeGameOverVoice()
        {
            return _voices[GameOverVoiceIndex] != null && !_voices[GameOverVoiceIndex].isPlaying
                ? GameOverVoiceIndex
                : -1;
        }

        // Tìm voice thường rảnh theo thứ tự vòng để âm ngắn có thể chồng nhau.
        private int FindFreeRegularVoice()
        {
            for (int offset = 0; offset < RegularVoiceCount; offset++)
            {
                int index = (_nextVoiceIndex + offset) % RegularVoiceCount;
                if (_voices[index] == null || _voices[index].isPlaying)
                    continue;

                _nextVoiceIndex = (index + 1) % RegularVoiceCount;
                return index;
            }

            return -1;
        }

        // Đặt AudioSource thành âm thanh 2D, không tự phát và không lặp.
        private AudioSource ConfigureVoice(AudioSource voice)
        {
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0f;
            voice.dopplerLevel = 0f;
            return voice;
        }

        // Lấy AudioClip tương ứng với loại âm thanh được yêu cầu.
        private AudioClip GetClip(Sound sound)
        {
            switch (sound)
            {
                case Sound.Shoot: return _shootClip;
                case Sound.BulletHit: return _bulletHitClip;
                case Sound.AsteroidBreak: return _asteroidBreakClip;
                case Sound.ShieldHit: return _shieldHitClip;
                case Sound.XPCollect: return _xpCollectClip;
                case Sound.LevelUp: return _levelUpClip;
                case Sound.Click: return _clickClip;
                case Sound.GameOver: return _gameOverClip;
                default: return null;
            }
        }

        // Lấy mức âm lượng riêng cho từng loại âm thanh.
        private float GetEffectVolume(Sound sound)
        {
            switch (sound)
            {
                case Sound.Shoot: return _shootVolume;
                case Sound.BulletHit: return _bulletHitVolume;
                case Sound.AsteroidBreak: return _asteroidBreakVolume;
                case Sound.ShieldHit: return _shieldHitVolume;
                case Sound.XPCollect: return _xpCollectVolume;
                case Sound.LevelUp: return _levelUpVolume;
                case Sound.Click: return _clickVolume;
                case Sound.GameOver: return _gameOverVolume;
                default: return 1f;
            }
        }

        // Giới hạn nhịp phát lặp để sự kiện dồn dập không gây âm quá nhiều.
        private double GetRepeatInterval(Sound sound)
        {
            switch (sound)
            {
                case Sound.Shoot: return 0.10d;
                case Sound.BulletHit: return 0.035d;
                case Sound.AsteroidBreak: return 0.05d;
                case Sound.ShieldHit: return 0.10d;
                case Sound.XPCollect: return 0.035d;
                case Sound.LevelUp: return 0d;
                case Sound.Click: return 0.05d;
                case Sound.GameOver: return 0.2d;
                default: return 0d;
            }
        }

        // Báo thiếu clip một lần để người dùng gán đủ âm thanh trong Inspector.
        private void ReportMissingClipsOnce()
        {
            if (_missingClipWarningReported)
                return;

            if (_shootClip != null && _bulletHitClip != null && _asteroidBreakClip != null
                && _shieldHitClip != null && _xpCollectClip != null && _levelUpClip != null
                && _clickClip != null && _gameOverClip != null)
                return;

            Debug.LogWarning("SfxAudioService has one or more unassigned AudioClip references. Assign all eight SFX clips in the Inspector.", this);
            _missingClipWarningReported = true;
        }
    }
}
