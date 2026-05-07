using UnityEngine;

/// <summary>
/// 指示对象数据类：表示一个具体的指示实例，包含类型、世界坐标、持续时间和优先级等信息。
/// 生命周期由 IndicatorManager 管理，渲染由 IndicatorRenderer 负责。
/// </summary>
public class IndicatorObject
{
    /// <summary>指示唯一标识</summary>
    public int id;

    /// <summary>指示类型</summary>
    public EIndicatorType type;

    /// <summary>世界空间目标位置</summary>
    public Vector3 worldPosition;

    /// <summary>持续时长 (秒)，0 表示持久</summary>
    public float duration;

    /// <summary>优先级 (值越小优先级越高)</summary>
    public int priority;

    /// <summary>创建时间</summary>
    public float createTime;

    /// <summary>是否已存活过期</summary>
    public bool isExpired;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">唯一标识</param>
    /// <param name="type">指示类型</param>
    /// <param name="worldPosition">世界坐标</param>
    /// <param name="duration">持续时长</param>
    /// <param name="priority">优先级</param>
    public IndicatorObject(int id, EIndicatorType type, Vector3 worldPosition, float duration, int priority)
    {
        this.id = id;
        this.type = type;
        this.worldPosition = worldPosition;
        this.duration = duration;
        this.priority = priority;
        this.createTime = Time.time;
        this.isExpired = false;
    }

    /// <summary>
    /// 检查是否已过期（基于持续时间和创建时间）
    /// </summary>
    /// <returns>true 表示已过期</returns>
    public bool CheckExpired()
    {
        if (isExpired)
        {
            return true;
        }

        if (duration > 0f && Time.time - createTime >= duration)
        {
            isExpired = true;
            return true;
        }

        return false;
    }
}
