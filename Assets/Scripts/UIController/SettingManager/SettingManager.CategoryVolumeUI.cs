using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class SettingManager : MonoBehaviour
{
    private static readonly AudioVolumeCategory[] VisibleAudioCategories =
    {
        AudioVolumeCategory.Engine,
        AudioVolumeCategory.Weapon,
        AudioVolumeCategory.Reload,
        AudioVolumeCategory.Impact,
        AudioVolumeCategory.Track,
        AudioVolumeCategory.UI
    };

    private void BuildCategoryVolumeUI()
    {
        RectTransform targetContainer = ResolveCategoryVolumeContainer();
        if (targetContainer == null || categoryVolumeItemPrefab == null)
        {
            return;
        }

        PrepareCategoryVolumeContainer(targetContainer);
        ClearCategoryVolumeUI();

        AudioCategoryVolumeSetting[] normalizedSettings = NormalizeCategoryVolumes(_currentAudioSettings.CategoryVolumes);
        _currentAudioSettings.CategoryVolumes = normalizedSettings;

        float itemSpacing = 16f;
        float itemHeight = 92f;
        float currentY = 0f;

        for (int index = 0; index < VisibleAudioCategories.Length; index++)
        {
            AudioVolumeCategory category = VisibleAudioCategories[index];
            float volume = GetCategoryVolumeFromSettings(normalizedSettings, category);
            GameObject itemObject = Instantiate(categoryVolumeItemPrefab, targetContainer, false);
            RectTransform itemRectTransform = itemObject.GetComponent<RectTransform>();
            if (itemRectTransform != null)
            {
                itemRectTransform.anchorMin = new Vector2(0f, 1f);
                itemRectTransform.anchorMax = new Vector2(1f, 1f);
                itemRectTransform.pivot = new Vector2(0.5f, 1f);
                itemRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);
                itemRectTransform.anchoredPosition = Vector2.zero;
                itemRectTransform.localScale = Vector3.one;
            }

            AudioCategoryVolumeItem item = itemObject.GetComponent<AudioCategoryVolumeItem>();

            if (item == null)
            {
                item = itemObject.AddComponent<AudioCategoryVolumeItem>();
            }

            if (item == null)
            {
                Debug.LogError("SettingManager: 分类音量项预制体缺少 AudioCategoryVolumeItem 组件。", this);
                Destroy(itemObject);
                continue;
            }

            LayoutElement layoutElement = itemObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = itemObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = itemHeight;
            layoutElement.preferredHeight = itemHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.flexibleWidth = 1f;

            item.Bind(category, volume, OnCategoryVolumeItemChanged);
            _categoryVolumeItems.Add(item);

            if (itemRectTransform != null)
            {
                itemRectTransform.anchoredPosition = new Vector2(0f, -currentY);
            }

            currentY += itemHeight + itemSpacing;
        }

        targetContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentY);
    }

    private void RefreshCategoryVolumeUI()
    {
        if (_categoryVolumeItems.Count == 0)
        {
            return;
        }

        for (int index = 0; index < _categoryVolumeItems.Count; index++)
        {
            AudioCategoryVolumeItem item = _categoryVolumeItems[index];
            if (item == null)
            {
                continue;
            }

            item.SetVolumeWithoutNotify(GetCategoryVolume(item.Category));
        }
    }

    private void ClearCategoryVolumeUI()
    {
        for (int index = 0; index < _categoryVolumeItems.Count; index++)
        {
            AudioCategoryVolumeItem item = _categoryVolumeItems[index];
            if (item == null)
            {
                continue;
            }

            Destroy(item.gameObject);
        }

        _categoryVolumeItems.Clear();
    }

    private void OnCategoryVolumeItemChanged(AudioVolumeCategory category, float value)
    {
        _currentAudioSettings.CategoryVolumes = SetCategoryVolumeInternal(_currentAudioSettings.CategoryVolumes, category, value);
        NotifySettingsChanged();
    }

    private RectTransform ResolveCategoryVolumeContainer()
    {
        if (categoryVolumeContent == null)
        {
            return null;
        }

        ScrollRect scrollRect = categoryVolumeContent.GetComponent<ScrollRect>();
        if (scrollRect != null && scrollRect.content != null)
        {
            return scrollRect.content;
        }

        Transform contentTransform = categoryVolumeContent.Find("Content");
        if (contentTransform is RectTransform contentRectTransform)
        {
            return contentRectTransform;
        }

        return categoryVolumeContent;
    }

    private void PrepareCategoryVolumeContainer(RectTransform targetContainer)
    {
        if (targetContainer == null)
        {
            return;
        }

        VerticalLayoutGroup verticalLayoutGroup = targetContainer.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.enabled = false;
        }

        ContentSizeFitter contentSizeFitter = targetContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }

        for (int index = targetContainer.childCount - 1; index >= 0; index--)
        {
            Transform child = targetContainer.GetChild(index);
            if (child == null)
            {
                continue;
            }

            Destroy(child.gameObject);
        }

        targetContainer.anchorMin = new Vector2(0f, 1f);
        targetContainer.anchorMax = new Vector2(1f, 1f);
        targetContainer.pivot = new Vector2(0.5f, 1f);
        targetContainer.anchoredPosition = Vector2.zero;
    }
}