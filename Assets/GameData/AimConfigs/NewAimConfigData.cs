using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// 瞄准系统配置数据，包含与瞄准相关的各种参数和设置。
/// 该数据通过ScriptableObject进行管理，便于在编辑器中进行配置和调整。
/// </summary>
/// 

//资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
[CreateAssetMenu(fileName = "AimConfigData", menuName = "DFCCCSystem/AimConfigData")]
public class NewAimConfigData : ScriptableObject
{
    public enum HudPreset
    {
        TacticalRing,
        ModernFireControl,
        Custom
    }

    public enum HudElementType
    {
        Crosshair,
        Ring,
        Graticule,
        CornerBracket,
        CenterDot,
        HorizontalScale,
        VerticalScale,
        RectangleFrame,
        ReadoutBox,
        CornerTick,
        TextSlot
    }

    public enum HudAnchor
    {
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        TopCenter,
        BottomCenter,
        LeftCenter,
        RightCenter
    }

    [System.Serializable]
    public class HudElementDefinition
    {
        public HudElementType ElementType = HudElementType.Crosshair;
        public HudAnchor Anchor = HudAnchor.Center;
        public Vector2 Offset = Vector2.zero;
        public Vector2 Size = new Vector2(24f, 24f);
        public float ScaleTotalLength = 24f;
        public float ScaleDecayPerStep = 2f;
        public float Thickness = 2f;
        public float Radius = 14f;
        public int RepeatCount = 1;
        public float RepeatSpacing = 12f;
        public Color Color = Color.white;
        public bool Enabled = true;
        public bool Filled = false;
        public string Text = string.Empty;
    }

    [Header("探测参数")]
    public float MaxDetectionRange = 4000f;  // 最大测距距离[cite: 11]
    public LayerMask AimLayerMask;           // 射线检测掩码[cite: 1]

    [Header("HUD 布局")]
    public HudPreset HudLayout = HudPreset.ModernFireControl;
    public List<HudElementDefinition> HudElements = new List<HudElementDefinition>();

    [Header("AIM 变焦")]
    public float[] ZoomFovLevels = { 60f, 20f, 10f }; // Existing line
    public float ZoomSmoothSpeed = 10f;               // Existing line
    public float BaseSensitivity = 6f;                 // New line
    public bool AutoResetZoom = true;                  // New line

    [Header("全屏蒙版 Vignette")]
    public bool EnableVignetteMask = true;
    /// <summary>
    /// 四角阴影沿屏幕长边向中心靠拢的范围。数值越大，左右两侧阴影越靠近中心。
    /// </summary>
    [FormerlySerializedAs("VignetteSmoothness")]
    [Range(0f, 1f)]
    public float CornerShadowLongSideRange = 0.55f;
    /// <summary>
    /// 四角阴影沿屏幕短边向中心靠拢的范围。数值越大，上下两侧阴影越靠近中心。
    /// </summary>
    [Range(0f, 1f)]
    public float CornerShadowShortSideRange = 0.55f;
    /// <summary>
    /// 中央观察区沿屏幕长边的尺寸。数值越大，中央清晰区左右更宽。
    /// </summary>
    [FormerlySerializedAs("VignetteRadius")]
    [Range(0f, 1f)]
    public float CenterViewLongSideSize = 0.72f;
    /// <summary>
    /// 中央观察区沿屏幕短边的尺寸。数值越大，中央清晰区上下更高。
    /// </summary>
    [Range(0f, 1f)]
    public float CenterViewShortSideSize = 0.62f;
    /// <summary>
    /// 周角阴影颜色，alpha 同时控制阴影强度。
    /// </summary>
    [FormerlySerializedAs("VignetteColor")]
    public Color ShadowColor = new Color(0f, 0f, 0f, 0.9f);

    [Header("HUD 样式")]
    public float HudScale = 1f;
    public float HudFovReference = 60f;
    public float HudFovScaleExponent = 0.35f;
    public float HudFovScaleMin = 0.85f;
    public float HudFovScaleMax = 1.9f;
    public float CrosshairLength = 15f;
    public float CrosshairGap = 3f;
    public float TpsRingRadius = 16f;
    public float TpsRingThickness = 2f;

    [Header("刻度线定义")]
    public Color HudThemeColor = Color.green;
    public float GraticuleSpacingMil = 18f;   // 默认密位间距
    public float GraticuleLineHalfWidth = 12f;
    public float GraticuleStartOffsetMil = 0f;
    public float LineThickness = 2.0f;
    public int GraticuleCount = 10;         // 刻度线数量

}
