using SpaceExplorer.Player;
using SpaceExplorer.Pickups;
using SpaceExplorer.Core;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class ProgressionHUD : MonoBehaviour
    {
        [Header("Progression Source")]
        [SerializeField] private StarXPCollector _collector;

        [Header("Player Health Source")]
        [SerializeField] private PlayerHealth _playerHealth;

        [Header("HUD Elements")]
        [SerializeField] private Text _levelLabel;
        [SerializeField] private Text _xpLabel;
        [SerializeField] private Text _scoreLabel;
        [SerializeField, FormerlySerializedAs("_progressFill")] private Image _hpBarFill;

        private bool _configurationErrorReported;
        private GameManager _gameManager;

        // Đăng ký nhận thay đổi XP/HP và hiển thị các giá trị ban đầu trên HUD.
        private void OnEnable()
        {
            if (_playerHealth == null && _collector != null)
                _playerHealth = _collector.GetComponent<PlayerHealth>();

            bool missingReference = _collector == null || _levelLabel == null ||
                                   _xpLabel == null || _scoreLabel == null ||
                                   _playerHealth == null || _hpBarFill == null;
            if (missingReference && !_configurationErrorReported)
            {
                Debug.LogError("Gameplay HUD needs the StarXPCollector, PlayerHealth, Level Text, XP Text, Score Text, and HP bar Fill references.", this);
                _configurationErrorReported = true;
            }

            if (_hpBarFill != null)
            {
                _hpBarFill.type = Image.Type.Filled;
                _hpBarFill.fillMethod = Image.FillMethod.Horizontal;
                _hpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            if (_collector != null)
            {
                _collector.ProgressChanged += RefreshProgression;
                RefreshProgression();
            }

            if (_playerHealth != null)
            {
                _playerHealth.HealthChanged += RefreshHealth;
                RefreshHealth(_playerHealth.CurrentHP, _playerHealth.MaxHP);
            }

            BindScoreSource();
        }

        // Thử liên kết lại Score sau khi mọi đối tượng trong scene đã khởi tạo.
        private void Start()
        {
            BindScoreSource();
        }

        // Hủy đăng ký XP, HP và Score khi HUD bị tắt.
        private void OnDisable()
        {
            if (_collector != null)
                _collector.ProgressChanged -= RefreshProgression;
            if (_playerHealth != null)
                _playerHealth.HealthChanged -= RefreshHealth;
            if (_gameManager != null)
                _gameManager.ScoreChanged -= RefreshScore;
            _gameManager = null;
        }

        // Liên kết với GameManager hiện tại và làm mới Score mà không giữ tham chiếu cũ.
        private void BindScoreSource()
        {
            GameManager manager = GameManager.Instance;
            if (_gameManager != manager)
            {
                if (_gameManager != null)
                    _gameManager.ScoreChanged -= RefreshScore;

                _gameManager = manager;
                if (_gameManager != null)
                    _gameManager.ScoreChanged += RefreshScore;
            }

            if (_gameManager != null)
                RefreshScore(_gameManager.CurrentScore);
        }

        // Hiển thị giá trị Score do GameManager cung cấp.
        private void RefreshScore(int score)
        {
            if (_scoreLabel != null)
                _scoreLabel.text = $"SCORE: {score}";
        }

        // Hiển thị Level và lượng XP hiện có trong cấp hiện tại.
        private void RefreshProgression()
        {
            if (_collector == null)
                return;

            if (_levelLabel != null)
                _levelLabel.text = $"Level {_collector.CurrentLevel}";

            if (_xpLabel != null)
            {
                int requiredXP = Mathf.Max(1, _collector.XPRequiredForNextLevel);
                _xpLabel.text = $"{_collector.XPIntoCurrentLevel} / {requiredXP} XP";
            }
        }

        // Cập nhật thanh HP theo tỷ lệ máu hiện tại trên mức tối đa.
        private void RefreshHealth(int currentHP, int maximumHP)
        {
            int safeMaximumHP = Mathf.Max(1, maximumHP);
            if (_hpBarFill != null)
                _hpBarFill.fillAmount = Mathf.Clamp01((float)Mathf.Clamp(currentHP, 0, safeMaximumHP) / safeMaximumHP);
        }
    }
}
