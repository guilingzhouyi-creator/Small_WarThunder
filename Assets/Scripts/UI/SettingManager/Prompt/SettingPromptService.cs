using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace NSettingSystem
{
    /// <summary>
    /// 设置提示服务：运行时在现有 UI Toolkit 根节点上构建一个轻量提示层，
    /// 负责显示应用/重置结果提示。
    /// </summary>
    public sealed class SettingPromptService : MonoBehaviour
    {
        [SerializeField] private int _defaultSortingOrder = 2000;
        [SerializeField] private int _sortingOrderOffsetAboveSettingCanvas = 50;
        [SerializeField] private UIDocument _uiDocument;
        [SerializeField] private bool _enableDebugLogs = true;

        private VisualElement _rootVisual;
        private VisualElement _overlayRoot;
        private VisualElement _promptCard;
        private Label _messageLabel;
        private Coroutine _hideCoroutine;

        public void Show(SettingPromptData promptData)
        {
            LogDebug($"Show called text='{promptData.MessageText}', type={promptData.PromptType}, autoClose={promptData.AutoClose}, duration={promptData.DurationSeconds}");
            EnsurePromptVisuals();
            if (_overlayRoot == null || _promptCard == null || _messageLabel == null)
            {
                Debug.LogWarning("[SettingPromptService] 未找到可用的 UI Toolkit 宿主，提示显示失败。", this);
                return;
            }

            _messageLabel.text = promptData.MessageText;
            ApplyPromptColors(promptData.PromptType);

            _overlayRoot.style.display = DisplayStyle.Flex;
            _overlayRoot.style.visibility = Visibility.Visible;
            _promptCard.style.display = DisplayStyle.Flex;
            _promptCard.style.visibility = Visibility.Visible;
            _promptCard.style.opacity = 1f;
            _messageLabel.style.visibility = Visibility.Visible;
            _rootVisual?.MarkDirtyRepaint();
            _overlayRoot.MarkDirtyRepaint();
            _promptCard.MarkDirtyRepaint();
            LogDebug($"Prompt displayed on host='{_uiDocument?.gameObject.name}', sortingOrder={_uiDocument?.sortingOrder}, panelReady={_overlayRoot.panel != null}");

            SchedulePostLayoutDebug();

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            if (promptData.AutoClose)
            {
                _hideCoroutine = StartCoroutine(HideAfterDelay(promptData.DurationSeconds));
            }
        }

        public void HideImmediate()
        {
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }

            if (_promptCard != null)
            {
                _promptCard.style.display = DisplayStyle.None;
                _promptCard.style.opacity = 0f;
            }

            if (_overlayRoot != null)
            {
                _overlayRoot.style.display = DisplayStyle.None;
            }
        }

        private IEnumerator HideAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, delaySeconds));
            HideImmediate();
        }

        private void EnsurePromptVisuals()
        {
            if (_overlayRoot != null && _overlayRoot.panel != null && _promptCard != null && _messageLabel != null)
            {
                LogDebug("EnsurePromptVisuals reused existing overlay visuals");
                return;
            }

            ResolveUIDocument();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                LogDebug("EnsurePromptVisuals failed because UIDocument or rootVisualElement is null");
                return;
            }

            ApplyPromptSortingOrder(_uiDocument);

            _rootVisual = _uiDocument.rootVisualElement;
            LogDebug($"Root before reset childCount={_rootVisual.childCount}, display={_rootVisual.style.display}, resolvedWidth={_rootVisual.resolvedStyle.width}, resolvedHeight={_rootVisual.resolvedStyle.height}");

            _rootVisual.style.display = DisplayStyle.Flex;
            _rootVisual.style.visibility = Visibility.Visible;
            _rootVisual.style.opacity = 1f;
            _rootVisual.style.flexGrow = 1f;
            _rootVisual.style.width = Length.Percent(100f);
            _rootVisual.style.height = Length.Percent(100f);
            _rootVisual.style.position = Position.Relative;
            LogDebug($"Resolved host root on '{_uiDocument.gameObject.name}', childCount={_rootVisual.childCount}, sortingOrder={_uiDocument.sortingOrder}");

            _overlayRoot = _rootVisual.Q<VisualElement>(UIStyleClassNames.SettingPromptOverlay);
            _promptCard = _rootVisual.Q<VisualElement>(UIStyleClassNames.SettingPromptCard);
            _messageLabel = _rootVisual.Q<Label>(UIStyleClassNames.SettingPromptText);

            if (_overlayRoot != null && _promptCard != null && _messageLabel != null)
            {
                ConfigureOverlayLayout(_overlayRoot);
                ConfigurePromptCardLayout(_promptCard);
                ConfigureMessageLabelLayout(_messageLabel);
                HideImmediate();
                LogDebug($"EnsurePromptVisuals rebound existing prompt tree on '{_uiDocument.gameObject.name}'");
                return;
            }

            if (_overlayRoot != null)
            {
                _overlayRoot.RemoveFromHierarchy();
            }

            _overlayRoot = new VisualElement { name = UIStyleClassNames.SettingPromptOverlay };
            ConfigureOverlayLayout(_overlayRoot);

            _promptCard = new VisualElement { name = UIStyleClassNames.SettingPromptCard };
            ConfigurePromptCardLayout(_promptCard);

            _messageLabel = new Label { name = UIStyleClassNames.SettingPromptText };
            ConfigureMessageLabelLayout(_messageLabel);

            _promptCard.Add(_messageLabel);
            _overlayRoot.Add(_promptCard);
            _rootVisual.Add(_overlayRoot);
            _overlayRoot.BringToFront();
            _promptCard.BringToFront();
            LogDebug($"Prompt visuals created and attached to '{_uiDocument.gameObject.name}'");
        }

        private void ResolveUIDocument()
        {
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                LogDebug($"ResolveUIDocument reused serialized reference '{_uiDocument.gameObject.name}'");
                return;
            }

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = GetComponentInChildren<UIDocument>(true);
            }

            if (_uiDocument == null)
            {
                _uiDocument = GetComponentInParent<UIDocument>(true);
            }

            LogDebug(_uiDocument != null
                ? $"ResolveUIDocument found '{_uiDocument.gameObject.name}' with sourceAsset='{_uiDocument.visualTreeAsset?.name}'"
                : "ResolveUIDocument could not find any UIDocument reference");
        }

        private void ConfigureOverlayLayout(VisualElement overlayRoot)
        {
            if (overlayRoot == null)
            {
                return;
            }

            overlayRoot.style.position = Position.Absolute;
            overlayRoot.style.left = 0f;
            overlayRoot.style.right = 0f;
            overlayRoot.style.top = 0f;
            overlayRoot.style.bottom = 0f;
            overlayRoot.style.display = DisplayStyle.None;
            overlayRoot.style.visibility = Visibility.Visible;
            overlayRoot.style.justifyContent = Justify.FlexEnd;
            overlayRoot.style.alignItems = Align.Center;
            overlayRoot.style.paddingBottom = 10f;
            overlayRoot.pickingMode = PickingMode.Ignore;
        }

        private void ConfigurePromptCardLayout(VisualElement promptCard)
        {
            if (promptCard == null)
            {
                return;
            }

            promptCard.style.display = DisplayStyle.None;
            promptCard.style.visibility = Visibility.Visible;
            promptCard.style.opacity = 0f;
            promptCard.style.minWidth = 220f;
            promptCard.style.maxWidth = 560f;
            promptCard.style.minHeight = 32f;
            promptCard.style.paddingTop = 6f;
            promptCard.style.paddingBottom = 6f;
            promptCard.style.paddingLeft = 16f;
            promptCard.style.paddingRight = 16f;
            promptCard.style.borderBottomLeftRadius = 4f;
            promptCard.style.borderBottomRightRadius = 4f;
            promptCard.style.borderTopLeftRadius = 4f;
            promptCard.style.borderTopRightRadius = 4f;
            promptCard.style.borderLeftWidth = 1f;
            promptCard.style.borderRightWidth = 1f;
            promptCard.style.borderTopWidth = 1f;
            promptCard.style.borderBottomWidth = 1f;
            promptCard.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        private void ConfigureMessageLabelLayout(Label messageLabel)
        {
            if (messageLabel == null)
            {
                return;
            }

            messageLabel.style.visibility = Visibility.Visible;
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.fontSize = 18f;
            messageLabel.style.color = new StyleColor(Color.white);
        }

        private void SchedulePostLayoutDebug()
        {
            if (_overlayRoot == null)
            {
                return;
            }

            _overlayRoot.schedule.Execute(() =>
            {
                if (_overlayRoot == null || _promptCard == null)
                {
                    return;
                }

                LogDebug($"PostLayout overlayDisplay={_overlayRoot.resolvedStyle.display}, overlayOpacity={_overlayRoot.resolvedStyle.opacity}, overlayBound={_overlayRoot.worldBound}, cardDisplay={_promptCard.resolvedStyle.display}, cardOpacity={_promptCard.resolvedStyle.opacity}, cardBound={_promptCard.worldBound}, text='{_messageLabel?.text}'");
            });
        }

        private void ApplyPromptSortingOrder(UIDocument document)
        {
            if (document == null)
            {
                return;
            }

            int targetSortingOrder = _defaultSortingOrder;
            int highestCanvasSortingOrder = ResolveHighestCanvasSortingOrder(out string sortingSource);
            if (!string.IsNullOrEmpty(sortingSource))
            {
                targetSortingOrder = Mathf.Max(targetSortingOrder, highestCanvasSortingOrder + _sortingOrderOffsetAboveSettingCanvas);
            }

            document.sortingOrder = targetSortingOrder;
            LogDebug($"ApplyPromptSortingOrder host='{document.gameObject.name}', highestCanvas={highestCanvasSortingOrder}, source='{sortingSource}', target={targetSortingOrder}, actual={document.sortingOrder}");
        }

        private int ResolveHighestCanvasSortingOrder(out string sortingSource)
        {
            int highestSortingOrder = int.MinValue;
            string resolvedSortingSource = string.Empty;

            void ConsiderCanvas(Canvas canvas, string source)
            {
                if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                {
                    return;
                }

                if (canvas.sortingOrder <= highestSortingOrder)
                {
                    return;
                }

                highestSortingOrder = canvas.sortingOrder;
                resolvedSortingSource = $"{source}:{canvas.gameObject.name}";
            }

            ConsiderCanvas(GetComponentInParent<Canvas>(true), "service-parent");

            if (SettingManager.Instance != null)
            {
                ConsiderCanvas(SettingManager.Instance.GetComponentInParent<Canvas>(true), "setting-manager");
            }

            Canvas[] sceneCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneCanvases.Length; i++)
            {
                ConsiderCanvas(sceneCanvases[i], "scene");
            }

            if (highestSortingOrder == int.MinValue)
            {
                sortingSource = string.Empty;
                return 0;
            }

            sortingSource = resolvedSortingSource;
            return highestSortingOrder;
        }

        private void ApplyPromptColors(SettingPromptType promptType)
        {
            Color backgroundColor;
            Color borderColor;

            switch (promptType)
            {
                case SettingPromptType.Error:
                    backgroundColor = new Color(0.34f, 0.08f, 0.08f, 0.92f);
                    borderColor = new Color(0.83f, 0.34f, 0.34f, 0.95f);
                    break;
                case SettingPromptType.Info:
                    backgroundColor = new Color(0.10f, 0.16f, 0.30f, 0.92f);
                    borderColor = new Color(0.42f, 0.62f, 0.92f, 0.95f);
                    break;
                default:
                    backgroundColor = new Color(0.08f, 0.24f, 0.15f, 0.92f);
                    borderColor = new Color(0.42f, 0.88f, 0.60f, 0.95f);
                    break;
            }

            _promptCard.style.backgroundColor = new StyleColor(backgroundColor);
            _promptCard.style.borderLeftColor = new StyleColor(borderColor);
            _promptCard.style.borderRightColor = new StyleColor(borderColor);
            _promptCard.style.borderTopColor = new StyleColor(borderColor);
            _promptCard.style.borderBottomColor = new StyleColor(borderColor);
        }

        private void OnDisable()
        {
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.RemoveFromHierarchy();
                _overlayRoot = null;
            }

            _rootVisual = null;
        }

        private void LogDebug(string message)
        {
            if (!_enableDebugLogs)
            {
                return;
            }

            Debug.Log($"[SettingPromptFlow][Service] {message}", this);
        }
    }
}