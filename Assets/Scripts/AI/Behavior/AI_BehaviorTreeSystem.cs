using System;
using System.Collections.Generic;
using UnityEngine;
using NGameData.NAIData;

/// <summary>
/// AI轻量行为树系统
/// 节点评估+装饰器条件+序列/选择/并行组合节点
/// 每帧由AI_Controller驱动tick
/// </summary>
namespace NAI
{
    // === 节点枚举 ===
    public enum EBtResult { Running, Success, Failure }
    public enum EBtNode { Selector, Sequence, Parallel, Condition, Action }

    // === 节点基类 ===
    public abstract class BtNode
    {
        public EBtNode NodeType;
        public List<BtNode> Children = new();
        public Func<bool> Condition;
        public abstract EBtResult Execute(AI_Blackboard bb);
    }

    public class BtSelector : BtNode
    {
        public BtSelector() => NodeType = EBtNode.Selector;
        public override EBtResult Execute(AI_Blackboard bb)
        {
            foreach (var child in Children)
            {
                if (child.Condition != null && !child.Condition()) continue;
                var result = child.Execute(bb);
                if (result != EBtResult.Failure) return result;
            }
            return EBtResult.Failure;
        }
    }

    public class BtSequence : BtNode
    {
        public BtSequence() => NodeType = EBtNode.Sequence;
        public override EBtResult Execute(AI_Blackboard bb)
        {
            foreach (var child in Children)
            {
                if (child.Condition != null && !child.Condition()) continue;
                var result = child.Execute(bb);
                if (result != EBtResult.Running) return result;
            }
            return EBtResult.Success;
        }
    }

    public class BtParallel : BtNode
    {
        public BtParallel() => NodeType = EBtNode.Parallel;
        public override EBtResult Execute(AI_Blackboard bb)
        {
            bool anyRunning = false;
            foreach (var child in Children)
            {
                if (child.Condition != null && !child.Condition()) continue;
                var r = child.Execute(bb);
                if (r == EBtResult.Running) anyRunning = true;
            }
            return anyRunning ? EBtResult.Running : EBtResult.Success;
        }
    }

    public class BtAction : BtNode
    {
        public Action<AI_Blackboard> ActionCallback;
        public BtAction() => NodeType = EBtNode.Action;
        public override EBtResult Execute(AI_Blackboard bb)
        {
            ActionCallback?.Invoke(bb);
            return EBtResult.Success;
        }
    }

    /// <summary>
    /// AI行为树系统——编辑器可视化根节点Root
    /// 外部通过SetRoot动态构建行为树
    /// </summary>
    public class AI_BehaviorTreeSystem : MonoBehaviour
    {
        [SerializeField] private AI_Controller _aiController;
        private AI_Blackboard _blackboard;
        private BtNode _rootNode;

        private void Start()
        {
            if (_aiController == null) _aiController = GetComponent<AI_Controller>();
            _blackboard = _aiController?.Blackboard;
        }

        private void Update()
        {
            if (_rootNode == null || _blackboard == null) return;
            _rootNode.Execute(_blackboard);
        }

        public void SetRoot(BtNode root) => _rootNode = root;
    }
}
