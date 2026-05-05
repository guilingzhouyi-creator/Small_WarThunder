using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局鼠标锁定引擎 — 纯静态类，无需挂载 GameObject。
/// 提供 Lock / Unlock / SetLocked 三个核心接口，
/// 任何时机任何系统都可直接调用，统一管理 Cursor.lockState / visible。
/// </summary>
public static class CursorEngine
{
    public static bool IsLocked { get; private set; } = true;

    public static event Action<bool> OnLockStateChanged;

    /// <summary>
    /// 锁定光标（隐藏并锁定在屏幕中央）
    /// </summary>
    public static void Lock()
    {
        IsLocked = true;
        Apply();
        OnLockStateChanged?.Invoke(true);
    }

    /// <summary>
    /// 解锁光标（显示并自由移动）
    /// </summary>
    public static void Unlock()
    {
        IsLocked = false;
        Apply();
        OnLockStateChanged?.Invoke(false);
    }

    /// <summary>
    /// 设置光标锁定状态
    /// </summary>
    public static void SetLocked(bool locked)
    {
        if (locked)
            Lock();
        else
            Unlock();
    }

    private static void Apply()
    {
        Cursor.lockState = IsLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !IsLocked;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // MainMenuScene 自动解锁光标（防御性保底）
        if (SceneLoader.IsScene(scene, SceneLoader.Scene.MainMenuScene))
        {
            Unlock();
        }
    }
}
