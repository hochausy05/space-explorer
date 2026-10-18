using System;
using System.Collections.Generic;
using SpaceExplorer.Upgrades;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SpaceExplorer.UI
{
    public sealed class UpgradeSelectionPanel : MonoBehaviour
    {
        [Header("Choice Buttons")]
        [SerializeField] private Button[] _buttons;
        [SerializeField] private Text[] _labels;
        [SerializeField] private Text[] _descriptions;
        [SerializeField] private Image[] _icons;

        public bool IsConfigured
        {
            get
            {
                if (!enabled || _buttons == null || _labels == null || _descriptions == null || _icons == null
                    || _buttons.Length == 0 || _buttons.Length != _labels.Length
                    || _buttons.Length != _descriptions.Length || _buttons.Length != _icons.Length)
                    return false;

                for (int i = 0; i < _buttons.Length; i++)
                {
                    if (_buttons[i] == null || _labels[i] == null || _descriptions[i] == null || _icons[i] == null)
                        return false;
                }

                return true;
            }
        }

        private Action<int, int> _onChoiceSelected;
        private UnityAction[] _runtimeCallbacks;

        private void Awake()
        {
            if (_onChoiceSelected == null)
                Hide();
        }

        public bool Show(IReadOnlyList<IUpgradeOption> choices, int selectionToken, Action<int, int> onChoiceSelected)
        {
            if (choices == null || choices.Count == 0 || onChoiceSelected == null || !IsConfigured)
                return false;

            int choiceCount = Mathf.Min(choices.Count, Mathf.Min(_buttons.Length, UpgradeManager.MaximumChoices));
            if (choiceCount == 0)
                return false;

            for (int i = 0; i < choiceCount; i++)
            {
                if (!_buttons[i].enabled || !_buttons[i].interactable
                    || !_labels[i].enabled || !_descriptions[i].enabled)
                    return false;
            }

            // Set the callback before activation: an initially inactive panel runs Awake on activation.
            _onChoiceSelected = onChoiceSelected;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            RemoveRuntimeListeners();

            for (int i = 0; i < _buttons.Length; i++)
            {
                bool isVisible = i < choiceCount;
                _buttons[i].gameObject.SetActive(isVisible);
                if (!isVisible)
                {
                    ClearPresentation(i);
                    continue;
                }

                _labels[i].text = choices[i].DisplayName;
                UpgradeDefinition definition = choices[i] as UpgradeDefinition;
                _descriptions[i].text = definition != null && !string.IsNullOrEmpty(definition.Description)
                    ? definition.Description
                    : string.Empty;
                SetIcon(i, definition != null ? definition.Icon : null);
                int capturedIndex = i;
                int capturedToken = selectionToken;
                UnityAction callback = () => _onChoiceSelected?.Invoke(capturedIndex, capturedToken);
                if (_runtimeCallbacks == null || _runtimeCallbacks.Length != _buttons.Length)
                    _runtimeCallbacks = new UnityAction[_buttons.Length];
                _runtimeCallbacks[i] = callback;
                _buttons[i].onClick.AddListener(callback);
            }

            gameObject.SetActive(true);
            return true;
        }

        public void Hide()
        {
            RemoveRuntimeListeners();
            _onChoiceSelected = null;
            if (_buttons != null)
            {
                for (int i = 0; i < _buttons.Length; i++)
                    ClearPresentation(i);
            }
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private void ClearPresentation(int index)
        {
            if (_labels != null && index < _labels.Length && _labels[index] != null)
                _labels[index].text = string.Empty;
            if (_descriptions != null && index < _descriptions.Length && _descriptions[index] != null)
                _descriptions[index].text = string.Empty;
            SetIcon(index, null);
        }

        private void SetIcon(int index, Sprite sprite)
        {
            if (_icons == null || index >= _icons.Length || _icons[index] == null)
                return;

            _icons[index].sprite = sprite;
            _icons[index].enabled = sprite != null;
        }

        private void RemoveRuntimeListeners()
        {
            if (_buttons == null || _runtimeCallbacks == null)
                return;

            int count = Mathf.Min(_buttons.Length, _runtimeCallbacks.Length);
            for (int i = 0; i < count; i++)
            {
                if (_buttons[i] != null && _runtimeCallbacks[i] != null)
                    _buttons[i].onClick.RemoveListener(_runtimeCallbacks[i]);
                _runtimeCallbacks[i] = null;
            }
        }
    }
}
