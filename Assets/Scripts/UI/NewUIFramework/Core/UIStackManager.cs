using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNewUIFramework
{
    /// <summary>
    /// 栈节点，记录压入栈的一个面板快照
    /// </summary>
    internal struct StackNode
    {
        public EUIIdentity identity;
        public EUIPushBehavior pushBehavior;
    }

    /// <summary>
    /// 嵌套栈管理器 —— 管理 Permanent/Gameplay/Overlay/System 四层栈的推入弹出
    /// 以及跨层自动遮挡/恢复逻辑
    /// 不涉及 Controller/ViewAdapter 操作，仅维护栈结构和调度回调
    /// </summary>
    internal class UIStackManager
    {
        /// <summary>各层栈：(contextType → Stack)</summary>
        private readonly Dictionary<EUIContextType, Stack<StackNode>> _stacks = new Dictionary<EUIContextType, Stack<StackNode>>();

        /// <summary>当前活跃面板集合（identity → 是否处于 Suspend 状态）</summary>
        private readonly Dictionary<EUIIdentity, bool> _activeStates = new Dictionary<EUIIdentity, bool>();

        private readonly IUIRegistry _registry;

        // 回调委托，由 UIManager 注入具体操作
        public Action<EUIIdentity> onOpen;
        public Action<EUIIdentity> onClose;
        public Action<EUIIdentity> onSuspend;
        public Action<EUIIdentity> onResume;
        public Action<EUIIdentity> onCover;   // 被高层遮挡
        public Action<EUIIdentity> onReveal;  // 遮挡解除

        /// <summary>最高有活跃面板的层级，用于遮挡判断</summary>
        private EUIContextType _highestActiveContext = (EUIContextType)(-1);

        public UIStackManager(IUIRegistry registry)
        {
            _registry = registry;
            foreach (EUIContextType ctx in Enum.GetValues(typeof(EUIContextType)))
            {
                _stacks[ctx] = new Stack<StackNode>();
            }
        }

        /// <summary>推入面板</summary>
        /// <param name="identity">面板标识</param>
        /// <param name="behavior">Additive / Exclusive</param>
        /// <param name="autoOpen">是否自动触发 Open 回调（框架层通常为 true）</param>
        public void Push(EUIIdentity identity, EUIPushBehavior behavior, bool autoOpen = true)
        {
            if (!_registry.IsRegistered(identity))
            {
                Debug.LogError($"[UIStackManager.Push] 面板 {identity} 未注册，无法推入栈。");
                return;
            }

            var contextType = _registry.GetContextType(identity);

            if (behavior == EUIPushBehavior.Exclusive)
            {
                // 清空当前栈内所有活跃面板（挂起它们）
                ClearContext(contextType);
            }

            var node = new StackNode { identity = identity, pushBehavior = behavior };
            _stacks[contextType].Push(node);

            if (autoOpen)
            {
                onOpen?.Invoke(identity);
                _activeStates[identity] = false; // 非 Suspend
            }

            UpdateHighestActiveContext();
            ApplyCrossContextCoverage();
        }

        /// <summary>弹出顶层面板</summary>
        public void Pop(EUIIdentity identity)
        {
            if (!_registry.IsRegistered(identity))
            {
                Debug.LogError($"[UIStackManager.Pop] 面板 {identity} 未注册。");
                return;
            }

            var contextType = _registry.GetContextType(identity);

            if (_stacks[contextType].Count == 0)
            {
                Debug.LogWarning($"[UIStackManager.Pop] 层级 {contextType} 栈为空，无法弹出 {identity}。");
                return;
            }

            var top = _stacks[contextType].Peek();
            if (top.identity != identity)
            {
                Debug.LogWarning($"[UIStackManager.Pop] 栈顶非 {identity}（当前: {top.identity}），跳过弹出。");
                return;
            }

            _stacks[contextType].Pop();
            onClose?.Invoke(identity);
            _activeStates.Remove(identity);

            UpdateHighestActiveContext();
            ApplyCrossContextCoverage();

            // 恢复上一层被 Exclusive 挂起的面板
            if (top.pushBehavior == EUIPushBehavior.Exclusive)
            {
                RestoreContext(contextType);
            }
        }

        /// <summary>清空指定层级的栈（挂起所有活跃面板）</summary>
        private void ClearContext(EUIContextType contextType)
        {
            var stack = _stacks[contextType];
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                onSuspend?.Invoke(node.identity);
                _activeStates[node.identity] = true; // Suspend 状态
            }
        }

        /// <summary>恢复指定层级（Exclusive 弹出后的反向操作）</summary>
        private void RestoreContext(EUIContextType contextType)
        {
            var stack = _stacks[contextType];
            if (stack.Count > 0)
            {
                var top = stack.Peek();
                if (_activeStates.TryGetValue(top.identity, out var suspended) && suspended)
                {
                    onResume?.Invoke(top.identity);
                    _activeStates[top.identity] = false;
                }
            }
        }

        /// <summary>更新最高活跃层级</summary>
        private void UpdateHighestActiveContext()
        {
            _highestActiveContext = (EUIContextType)(-1);
            foreach (EUIContextType ctx in new[] { EUIContextType.System, EUIContextType.Overlay, EUIContextType.Gameplay, EUIContextType.Permanent })
            {
                if (_stacks[ctx].Count > 0)
                {
                    _highestActiveContext = ctx;
                    return;
                }
            }
        }

        /// <summary>跨层遮挡：仅当最高活跃上下文存在 Exclusive 面板时才遮挡低层级 UI。
        /// Additive 面板（如小地图）不应隐藏 Gameplay HUD。</summary>
        private void ApplyCrossContextCoverage()
        {
            bool shouldCover = _highestActiveContext >= 0 && HighestContextHasExclusivePanel();

            foreach (EUIIdentity id in _activeStates.Keys)
            {
                if (!_registry.IsRegistered(id)) continue;
                var ctx = _registry.GetContextType(id);
                if (shouldCover && (int)ctx < (int)_highestActiveContext)
                {
                    onCover?.Invoke(id);
                }
                else if ((int)ctx == (int)_highestActiveContext)
                {
                    onReveal?.Invoke(id);
                }
            }
        }

        /// <summary>最高活跃上下文栈顶是否为 Exclusive 面板</summary>
        private bool HighestContextHasExclusivePanel()
        {
            var stack = _stacks[_highestActiveContext];
            if (stack.Count == 0) return false;
            return stack.Peek().pushBehavior == EUIPushBehavior.Exclusive;
        }

        /// <summary>获取指定层级的栈深度</summary>
        public int GetStackDepth(EUIContextType contextType)
        {
            return _stacks[contextType].Count;
        }

        /// <summary>获取指定层级的栈顶面板标识，栈空返回 null</summary>
        public EUIIdentity? PeekTop(EUIContextType contextType)
        {
            var stack = _stacks[contextType];
            if (stack.Count > 0)
                return stack.Peek().identity;
            return null;
        }

        /// <summary>面板是否存在于任意栈中</summary>
        public bool IsPanelInStack(EUIIdentity identity)
        {
            return _activeStates.ContainsKey(identity);
        }

        /// <summary>面板是否处于 Suspend 状态</summary>
        public bool IsSuspended(EUIIdentity identity)
        {
            return _activeStates.TryGetValue(identity, out var s) && s;
        }
    }
}
