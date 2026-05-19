using Unity.Cinemachine;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraTransitionConfig", menuName = "SmallWarThunder/瞄准/相机/过渡规则")]
public class CameraTransitionConfig : ScriptableObject
{
    [Header("混合规则列表（From -> To 的过渡配置）")]
    public AimBlendRule[] BlendRules;

    [Header("优先级")]
    public int AimActivePriority = 15;
    public int AimInactivePriority = 0;
    public int ZoomActivePriority = 20;
    public int ZoomInactivePriority = 0;

    [Header("切换后行为")]
    public bool ResetZoomOnAim = true;

    private void OnValidate()
    {
        if (BlendRules == null || BlendRules.Length == 0)
        {
            return;
        }

        for (int i = 0; i < BlendRules.Length; i++)
        {
            for (int j = i + 1; j < BlendRules.Length; j++)
            {
                if (BlendRules[i].FromCamera == BlendRules[j].FromCamera &&
                    BlendRules[i].ToCamera == BlendRules[j].ToCamera)
                {
                    Debug.LogWarning(
                        $"[CameraTransitionConfig] Duplicate blend rule: {BlendRules[i].FromCamera} -> {BlendRules[i].ToCamera}",
                        this);
                }
            }
        }
    }
}


public enum CameraBlendTarget
{
    Any = 0,
    TPS = 1,
    Zoom = 2,
    Aim = 3,
    Map = 4
}

[System.Serializable]
public struct AimBlendRule
{
    [Tooltip("起始相机")]
    public CameraBlendTarget FromCamera;

    [Tooltip("目标相机")]
    public CameraBlendTarget ToCamera;

    [Tooltip("混合样式")]
    public CinemachineBlendDefinition.Styles BlendStyle;

    [Tooltip("混合时长（秒），Cut 样式时忽略此值")]
    [Range(0f, 2f)]
    public float BlendDuration;

    public CinemachineBlendDefinition ToBlendDefinition()
    {
        return new CinemachineBlendDefinition(BlendStyle, BlendDuration);
    }
}
