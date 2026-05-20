using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家感知计算引擎。
/// 纯逻辑层，不持有运行时状态，不依赖MonoBehaviour。
/// 负责视锥判断、距离衰减、遮挡检测，将原始感知输入归一化为 visibility + strength。
/// </summary>
public static class PlayerPerceptionEngine
{
    private static readonly RaycastHit[] occlusionHits = new RaycastHit[PlayerPerceptionConstants.DefaultOcclusionSamples * 2];
    private static LayerMask _cachedOcclusionMask;

    /// <summary>
    /// 初始化遮挡层（建议由Manager在启动时调用一次）。
    /// </summary>
    /// <param name="excludeLayers">排除的层名集合</param>
    public static void InitializeOcclusionMask(HashSet<string> excludeLayers)
    {
        int mask = Physics.DefaultRaycastLayers;
        if (excludeLayers != null)
        {
            foreach (string layer in excludeLayers)
            {
                mask &= ~LayerMask.GetMask(layer);
            }
        }

        _cachedOcclusionMask = mask;
    }

    /// <summary>
    /// 对单个目标执行感知评估。
    /// </summary>
    /// <param name="cameraPos">相机世界位置</param>
    /// <param name="cameraFwd">相机前向</param>
    /// <param name="config">感知配置</param>
    /// <param name="targetPos">目标位置</param>
    /// <param name="occlusionSampleCount">遮挡采样数量（0或负数则跳过低开销遮挡检测）</param>
    /// <returns>评估结果：是否可见、感知强度(0~1)、是否被遮挡</returns>
    public static PerceptionResult EvaluateSingleUnit(
        Vector3 cameraPos,
        Vector3 cameraFwd,
        PlayerPerceptionConfigSO config,
        Vector3 targetPos,
        int occlusionSampleCount = PlayerPerceptionConstants.DefaultOcclusionSamples)
    {
        if (config == null)
        {
            Debug.LogWarning($"{PlayerPerceptionConstants.DebugTagEngine} EvaluateSingleUnit: config is null");
            return PerceptionResult.Invisible;
        }

        Vector3 toTarget = targetPos - cameraPos;
        float distance = toTarget.magnitude;
        if (distance < 0.001f)
        {
            return PerceptionResult.Invisible;
        }

        // ─── 1. 距离过滤 ───
        float maxDistance = Mathf.Max(config.maxPerceptionDistance, 0.001f);
        if (distance > maxDistance)
        {
            return PerceptionResult.Invisible;
        }

        if (_cachedOcclusionMask.value == 0 && config.occlusionLayerMask.value != 0)
        {
            _cachedOcclusionMask = config.occlusionLayerMask;
        }

        Vector3 dir = toTarget / distance;

        // ─── 2. 视锥角过滤 ───
        float angleDeg = Vector3.Angle(cameraFwd, dir);
        float halfAngle = Mathf.Clamp(config.visionConeHalfAngle, 0f, 180f);
        if (angleDeg > halfAngle)
        {
            return PerceptionResult.Invisible;
        }

        // ─── 3. 边缘衰减（视锥边缘弱化） ───
        float angleFactor = halfAngle <= 0.001f ? 1f : Mathf.InverseLerp(halfAngle, 0f, angleDeg);

        // ─── 4. 距离衰减 ───
        float fringeDistance = Mathf.Clamp(config.fringeDistanceThreshold, 0f, maxDistance);
        float distanceFactor = fringeDistance >= maxDistance
            ? 1f
            : Mathf.InverseLerp(maxDistance, fringeDistance, distance);

        // ─── 5. 基础强度（角度 × 距离 合成） ───
        float baseStrength = Mathf.Clamp01((angleFactor + distanceFactor) * 0.5f);

        // ─── 6. 遮挡检测 ───
        bool occluded = false;
        if (config.enableOcclusionCheck && occlusionSampleCount > 0)
        {
            occluded = EvaluateOcclusion(cameraPos, targetPos, distance, occlusionSampleCount);
        }

        float finalStrength = baseStrength;
        if (occluded)
        {
            // 有遮挡时强度衰减，但不直接归零（保留微弱线索）
            finalStrength *= 0.2f;
        }

        if (finalStrength <= 0.001f)
        {
            return PerceptionResult.Invisible;
        }

        return new PerceptionResult
        {
            isVisible = true,
            strength = Mathf.Clamp01(finalStrength),
            isOccluded = occluded,
            distance = distance,
            worldPosition = targetPos
        };
    }

    /// <summary>
    /// 评估遮挡状态（Raycast采样）。
    /// 使用质心采样策略：射线数≥3时在target周围取偏移采样点，减少误判。
    /// </summary>
    private static bool EvaluateOcclusion(Vector3 origin, Vector3 targetCenter, float maxDistance, int sampleCount)
    {
        if (_cachedOcclusionMask.value == 0)
        {
            _cachedOcclusionMask = Physics.DefaultRaycastLayers;
        }

        sampleCount = Mathf.Clamp(sampleCount, 1, occlusionHits.Length);
        int hitTotal = 0;

        if (sampleCount == 1)
        {
            // 单射线：精确指向目标中心
            Vector3 dir = (targetCenter - origin).normalized;
            if (Physics.Raycast(origin, dir, maxDistance, _cachedOcclusionMask, QueryTriggerInteraction.Ignore))
            {
                return true;
            }
        }
        else
        {
            // 多射线采样：质心 + 偏移点
            Vector3 baseDir = (targetCenter - origin).normalized;
            Vector3 right = Vector3.Cross(baseDir, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, baseDir).normalized;

            float offsetRadius = 0.3f;
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 offset = Vector3.zero;
                if (i == 0)
                {
                    // 中心点
                    offset = Vector3.zero;
                }
                else
                {
                    float angle = (i - 1) * (360f / (sampleCount - 1)) * Mathf.Deg2Rad;
                    offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * offsetRadius;
                }

                Vector3 sampleTarget = targetCenter + offset;
                Vector3 sampleDir = (sampleTarget - origin).normalized;
                float sampleDist = Vector3.Distance(origin, sampleTarget);

                if (Physics.Raycast(origin, sampleDir, sampleDist, _cachedOcclusionMask, QueryTriggerInteraction.Ignore))
                {
                    hitTotal++;
                }
            }

            // 过半射线命中即判定遮挡
            if (hitTotal > sampleCount / 2)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 感知评估单项结果。
    /// </summary>
    public struct PerceptionResult
    {
        /// <summary>是否在视锥范围内且未被完全遮挡</summary>
        public bool isVisible;

        /// <summary>感知强度（0~1）</summary>
        public float strength;

        /// <summary>是否存在遮挡</summary>
        public bool isOccluded;

        /// <summary>目标距离</summary>
        public float distance;

        /// <summary>目标世界位置</summary>
        public Vector3 worldPosition;

        /// <summary>不可见的结果常量</summary>
        public static PerceptionResult Invisible => new PerceptionResult
        {
            isVisible = false,
            strength = 0f,
            isOccluded = false,
            distance = 0f,
            worldPosition = Vector3.zero
        };
    }
}
