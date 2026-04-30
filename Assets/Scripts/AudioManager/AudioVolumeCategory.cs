using UnityEngine;

public enum AudioVolumeCategory
{
    Default = 0,
    Engine = 1,
    Weapon = 2,
    Reload = 3,
    Impact = 4,
    Track = 5,
    UI = 6
}

[System.Serializable]
public struct AudioCategoryVolumeSetting
{
    public AudioVolumeCategory Category;
    [Range(0f, 1f)] public float Volume;
}