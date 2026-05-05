using System;

namespace SmallWar.Data
{
    [Serializable]
    public struct TaskDefinition
    {
        public MissionKey key;
        public string description;
        public int requiredCount;
        public string unitTag;

        public TaskDefinition(MissionKey key, string description, int requiredCount, string unitTag)
        {
            this.key = key;
            this.description = description;
            this.requiredCount = requiredCount;
            this.unitTag = unitTag;
        }
    }

    [Serializable]
    public class TaskProgress
    {
        public TaskDefinition Definition;
        public int CurrentCount;
        public bool IsCompleted;

        public TaskProgress(TaskDefinition definition)
        {
            Definition = definition;
            CurrentCount = 0;
            IsCompleted = false;
        }

        public string FormattedText
        {
            get
            {
                string progressColor = IsCompleted ? "#0f0" : "#ff0";
                string checkMark = IsCompleted ? " ✓" : "";
                return $"{Definition.description} <color={progressColor}>{CurrentCount}</color>/{Definition.requiredCount}{checkMark}";
            }
        }
    }
}
