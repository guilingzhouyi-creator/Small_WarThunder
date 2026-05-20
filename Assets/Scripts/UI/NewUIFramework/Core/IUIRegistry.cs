using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNewUIFramework
{
    /// <summary>
    /// UI 控制器注册条目，封装一个已注册面板的全部元数据
    /// </summary>
    internal struct UIRegistryEntry
    {
        public EUIIdentity identity;
        public EUIContextType contextType;
        public object controller;
        public IUIViewAdapter viewAdapter;
    }

    /// <summary>
    /// UI 注册中心接口 —— Controller / ViewAdapter 的唯一注册入口
    /// 采用注册表模式解耦框架与业务，支持热插拔
    /// </summary>
    public interface IUIRegistry
    {
        /// <summary>注册一个 UI 面板及其控制器和视图适配器</summary>
        /// <typeparam name="TData">该面板 Open 时接收的数据类型</typeparam>
        /// <param name="identity">面板唯一标识</param>
        /// <param name="contextType">所属嵌套栈层级</param>
        /// <param name="controller">实现了 IUIController 的业务控制器</param>
        /// <param name="viewAdapter">视图适配器实例</param>
        void Register<TData>(EUIIdentity identity, EUIContextType contextType, IUIController<TData> controller, IUIViewAdapter viewAdapter);

        /// <summary>注销一个 UI 面板</summary>
        void Unregister(EUIIdentity identity);

        /// <summary>通过标识查找控制器（返回泛型接口）</summary>
        IUIController<TData> GetController<TData>(EUIIdentity identity);

        /// <summary>通过标识查找视图适配器</summary>
        IUIViewAdapter GetViewAdapter(EUIIdentity identity);

        /// <summary>获取指定层级的所有已注册面板标识</summary>
        IReadOnlyList<EUIIdentity> GetIdentitiesByContext(EUIContextType contextType);

        /// <summary>检查面板是否已注册</summary>
        bool IsRegistered(EUIIdentity identity);

        /// <summary>获取面板所属的上下文层级</summary>
        EUIContextType GetContextType(EUIIdentity identity);

        /// <summary>显式清理已销毁的场景 UI 条目（用于场景切换边界）。</summary>
        void PruneDestroyedEntries();
    }

    /// <summary>
    /// UI 注册中心默认实现
    /// 线程安全：仅主线程调用（Unity API 依赖），不引入锁开销
    /// </summary>
    internal class UIRegistry : IUIRegistry
    {
        private readonly Dictionary<EUIIdentity, UIRegistryEntry> _entries = new Dictionary<EUIIdentity, UIRegistryEntry>();
        private readonly Dictionary<EUIContextType, List<EUIIdentity>> _contextIndex = new Dictionary<EUIContextType, List<EUIIdentity>>();

        public void Register<TData>(EUIIdentity identity, EUIContextType contextType, IUIController<TData> controller, IUIViewAdapter viewAdapter)
        {
            if (_entries.ContainsKey(identity))
            {
                Debug.LogWarning($"[UIRegistry.Register] 面板 {identity} 已注册，将覆盖旧条目。");
                Unregister(identity);
            }

            _entries[identity] = new UIRegistryEntry
            {
                identity = identity,
                contextType = contextType,
                controller = controller,
                viewAdapter = viewAdapter
            };

            if (!_contextIndex.TryGetValue(contextType, out var list))
            {
                list = new List<EUIIdentity>();
                _contextIndex[contextType] = list;
            }
            list.Add(identity);

            Debug.Log($"[UIRegistry.Register] 面板 {identity} 注册成功，层级: {contextType}");
        }

        public void Unregister(EUIIdentity identity)
        {
            if (!_entries.TryGetValue(identity, out var entry))
            {
                Debug.LogWarning($"[UIRegistry.Unregister] 面板 {identity} 未注册，无需注销。");
                return;
            }

            _entries.Remove(identity);

            if (_contextIndex.TryGetValue(entry.contextType, out var list))
            {
                list.Remove(identity);
                if (list.Count == 0)
                {
                    _contextIndex.Remove(entry.contextType);
                }
            }

            Debug.Log($"[UIRegistry.Unregister] 面板 {identity} 已注销。");
        }

        public IUIController<TData> GetController<TData>(EUIIdentity identity)
        {
            if (_entries.TryGetValue(identity, out var entry))
            {
                return entry.controller as IUIController<TData>;
            }
            Debug.LogError($"[UIRegistry.GetController] 面板 {identity} 未注册。");
            return null;
        }

        public IUIViewAdapter GetViewAdapter(EUIIdentity identity)
        {
            if (_entries.TryGetValue(identity, out var entry))
            {
                return entry.viewAdapter;
            }
            Debug.LogError($"[UIRegistry.GetViewAdapter] 面板 {identity} 未注册。");
            return null;
        }

        public IReadOnlyList<EUIIdentity> GetIdentitiesByContext(EUIContextType contextType)
        {
            if (_contextIndex.TryGetValue(contextType, out var list))
            {
                return list;
            }
            return Array.Empty<EUIIdentity>();
        }

        public bool IsRegistered(EUIIdentity identity)
        {
            return _entries.ContainsKey(identity);
        }

        public EUIContextType GetContextType(EUIIdentity identity)
        {
            if (_entries.TryGetValue(identity, out var entry))
            {
                return entry.contextType;
            }
            Debug.LogError($"[UIRegistry.GetContextType] 面板 {identity} 未注册。");
            return EUIContextType.Permanent;
        }

        public void PruneDestroyedEntries()
        {
            if (_entries.Count == 0)
            {
                return;
            }

            List<EUIIdentity> deadEntries = null;
            foreach (var pair in _entries)
            {
                if (!IsEntryAlive(pair.Value))
                {
                    deadEntries ??= new List<EUIIdentity>();
                    deadEntries.Add(pair.Key);
                }
            }

            if (deadEntries == null)
            {
                return;
            }

            for (int index = 0; index < deadEntries.Count; index++)
            {
                EUIIdentity identity = deadEntries[index];
                Debug.LogWarning($"[UIRegistry.PruneDestroyedEntries] 移除已销毁的 UI 条目: {identity}");
                Unregister(identity);
            }
        }

        private static bool IsEntryAlive(UIRegistryEntry entry)
        {
            return IsAlive(entry.controller) && IsAlive(entry.viewAdapter);
        }

        private static bool IsAlive(object candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate is UnityEngine.Object unityObject)
            {
                return unityObject != null;
            }

            return true;
        }
    }
}
