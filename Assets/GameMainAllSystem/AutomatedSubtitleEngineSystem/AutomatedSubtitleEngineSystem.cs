
namespace SmallWar.Data
{
    public enum MissionCategory { Training, Frontline, SpecialOps }

    [System.Serializable]

    //采用结构体数据单元标识任务文件，包含任务类别和子ID（同一类别下的不同任务）
    //轻量化地标识任务数据，方便在字幕系统中使用，避免直接引用 ScriptableObject 造成的耦合和资源管理问题以及GC压力
    //时间复杂度压缩为 O(1)，空间复杂度压缩为 O(1)，相比直接使用 ScriptableObject 作为键值，性能更优且更易于管理
    public struct MissionKey
    {
        public MissionCategory category;

        public int subID;

        // 复写Equals和GetHashCode方法，使得MissionKey可以作为Dictionary的键值使用，并且根据category和subID进行比较
        public override bool Equals(object obj) => obj is MissionKey other && category == other.category && subID == other.subID;

        // 简单的哈希函数，将category和subID组合成一个唯一的整数，完整版将采用更复杂的哈希算法以减少碰撞概率。
        // eg：AES加密算法的哈希函数，或者FNV-1a等高质量哈希算法
        public override int GetHashCode() => (int)category * 1000 + subID;

    }

}