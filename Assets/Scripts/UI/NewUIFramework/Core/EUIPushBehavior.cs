namespace NNewUIFramework
{
    /// <summary>
    /// UI 压栈行为枚举
    /// Additive：新 UI 与当前栈顶共存
    /// Exclusive：新 UI 独占栈顶，挂起栈内所有现有 UI
    /// </summary>
    public enum EUIPushBehavior
    {
        Additive,
        Exclusive
    }
}
