using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;




/// <summary>
/// 优化清单：
/// 将Find方法替换为直接在Inspector中分配组件引用，减少运行时的性能开销。
/// 添加空引用检查，确保在使用组件之前已经正确分配，避免潜在的NullReferenceException错误。
/// 将组件引用的获取逻辑封装在一个单独的方法中，确保代码的清晰和可维护性。
/// </summary>

public class AudioCategoryVolumeItem : MonoBehaviour
{
    private const float RuntimeSliderWidth = 1550f;
    private const float RuntimeTextOffset = 135.5f;

    [SerializeField] private TMP_Text categoryNameText;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;

    private AudioVolumeCategory _category;
    private Action<AudioVolumeCategory, float> _onValueChanged;
    private float _lastDisplayedValue = -1f;

    public AudioVolumeCategory Category => _category;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindSliderListener();
    }

    public void Bind(AudioVolumeCategory category, float volume, Action<AudioVolumeCategory, float> onValueChanged)
    {
        ResolveReferences();
        ApplyRuntimeLayoutConstraints();
        _category = category;
        _onValueChanged = onValueChanged;
        _lastDisplayedValue = Mathf.Clamp01(volume);

        if (categoryNameText != null)
        {
            categoryNameText.text = FormatCategoryName(category);
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(_lastDisplayedValue);
            BindSliderListener();
        }

        UpdateValueLabel(_lastDisplayedValue);
    }

    public void SetVolumeWithoutNotify(float volume)
    {
        ResolveReferences();
        _lastDisplayedValue = Mathf.Clamp01(volume);

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(_lastDisplayedValue);
        }

        UpdateValueLabel(_lastDisplayedValue);
    }

    private void LateUpdate()
    {
        if (volumeSlider == null)
        {
            return;
        }

        float currentValue = Mathf.Clamp01(volumeSlider.value);
        if (Mathf.Abs(currentValue - _lastDisplayedValue) < 0.0001f)
        {
            return;
        }

        _lastDisplayedValue = currentValue;
        UpdateValueLabel(currentValue);
    }

    private void OnDestroy()
    {
        UnbindSliderListener();
    }

    private void HandleSliderValueChanged(float value)
    {
        _lastDisplayedValue = Mathf.Clamp01(value);
        UpdateValueLabel(value);
        _onValueChanged?.Invoke(_category, value);
    }

    private void UpdateValueLabel(float value)
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
        }
    }

    private void BindSliderListener()
    {
        if (volumeSlider == null || _onValueChanged == null)
        {
            return;
        }

        volumeSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        volumeSlider.onValueChanged.AddListener(HandleSliderValueChanged);
    }

    private void UnbindSliderListener()
    {
        if (volumeSlider == null)
        {
            return;
        }

        volumeSlider.onValueChanged.RemoveListener(HandleSliderValueChanged);
    }

    private void ResolveReferences()
    {
        if (volumeSlider == null)
        {
            volumeSlider = GetComponent<Slider>();
            if (volumeSlider == null)
            {
                volumeSlider = GetComponentInChildren<Slider>(true);
            }
        }

        if (categoryNameText == null)
        {
            categoryNameText = FindTextByName("ShowTextMesh");
            if (categoryNameText == null)
            {
                categoryNameText = FindFirstTextExcept(volumeValueText);
            }
        }

        if (volumeValueText == null)
        {
            volumeValueText = FindTextByName("ShowVolumeValue");
            if (volumeValueText == null)
            {
                volumeValueText = FindFirstTextExcept(categoryNameText);
            }
        }
    }

    private void ApplyRuntimeLayoutConstraints()
    {
        RectTransform rowRect = transform as RectTransform;
        if (rowRect != null)
        {
            rowRect.sizeDelta = new Vector2(RuntimeSliderWidth, rowRect.sizeDelta.y);

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = RuntimeSliderWidth;
        }

        ApplyTextAnchor(categoryNameText, isLeftText: true);
        ApplyTextAnchor(volumeValueText, isLeftText: false);
    }

    private static void ApplyTextAnchor(TMP_Text text, bool isLeftText)
    {
        if (text == null)
        {
            return;
        }

        RectTransform textRect = text.transform as RectTransform;
        if (textRect == null)
        {
            return;
        }

        if (isLeftText)
        {
            textRect.anchorMin = new Vector2(0f, 0.5f);
            textRect.anchorMax = new Vector2(0f, 0.5f);
            textRect.pivot = new Vector2(0f, 0.5f);
            textRect.anchoredPosition = new Vector2(-RuntimeTextOffset, 0f);
            text.alignment = TextAlignmentOptions.MidlineRight;
        }
        else
        {
            textRect.anchorMin = new Vector2(1f, 0.5f);
            textRect.anchorMax = new Vector2(1f, 0.5f);
            textRect.pivot = new Vector2(1f, 0.5f);
            textRect.anchoredPosition = new Vector2(RuntimeTextOffset, 0f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private TMP_Text FindTextByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Transform targetTransform = transform.Find(targetName);
        return targetTransform != null ? targetTransform.GetComponent<TMP_Text>() : null;
    }

    private TMP_Text FindFirstTextExcept(TMP_Text excludedText)
    {
        TMP_Text[] allTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < allTexts.Length; index++)
        {
            TMP_Text text = allTexts[index];
            if (text != null && text != excludedText)
            {
                return text;
            }
        }

        return null;
    }

    private static string FormatCategoryName(AudioVolumeCategory category)
    {
        return category switch
        {
            AudioVolumeCategory.Engine => "引擎",
            AudioVolumeCategory.Weapon => "武器",
            AudioVolumeCategory.Reload => "装填",
            AudioVolumeCategory.Impact => "碰撞",
            AudioVolumeCategory.Track => "履带",
            AudioVolumeCategory.UI => "界面",
            _ => "默认"
        };
    }
}
