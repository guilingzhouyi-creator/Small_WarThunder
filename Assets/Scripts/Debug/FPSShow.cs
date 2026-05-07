using UnityEngine;
using System.Text;

public class FPSConsoleLogger : MonoBehaviour
{
    [SerializeField, Tooltip("控制台输出的间隔（秒）")]
    private float outputInterval = 0.5f;

    [SerializeField, Tooltip("是否开启输出")]
    private bool enableOutput = true;

    // 统计变量
    private float accumTime;        // 累积的时间
    private int frameCount;         // 累积的帧数
    private float minFrameTime;     // 区间内最小单帧耗时
    private float maxFrameTime;     // 区间内最大单帧耗时

    private void Start()
    {
        ResetStats();
    }

    private void Update()
    {
        // ---- 按键切换输出 ----
        //采用新版输入系统的方式，按 F 键切换输出状态
        if (MIddleInputingController.Instance.IsDebugPressed())// 按 F 键切换输出状态
        {
            enableOutput = !enableOutput;
            Debug.Log($"<color=#00ff88>FPS 控制台输出: {(enableOutput ? "开启" : "关闭")}</color>");
        }

        if (!enableOutput)
            return;

        // 使用 unscaleDeltaTime 反映真实渲染速度，不受 Time.timeScale 影响
        float dt = Time.unscaledDeltaTime;

        accumTime += dt;
        frameCount++;

        if (dt < minFrameTime) minFrameTime = dt;
        if (dt > maxFrameTime) maxFrameTime = dt;

        // 累积时间达到设定间隔后，输出统计并重置
        if (accumTime >= outputInterval)
        {
            float avgFPS = frameCount / accumTime;
            float avgFrameTimeMs = (accumTime / frameCount) * 1000f;
            float minFPS = 1.0f / maxFrameTime;
            float maxFPS = 1.0f / minFrameTime;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=cyan>===== FPS 统计 (仅控制台) ====</color>");
            sb.AppendLine($"平均 FPS: {avgFPS:F1}  帧时间: {avgFrameTimeMs:F2} ms");
            sb.AppendLine($"最差 FPS: {minFPS:F1}  最佳 FPS: {maxFPS:F1}");
            sb.AppendLine($"统计帧数: {frameCount}  时间区间: {accumTime:F2}s");
            Debug.Log(sb.ToString());

            ResetStats();
        }
    }

    private void ResetStats()
    {
        accumTime = 0f;
        frameCount = 0;
        minFrameTime = float.MaxValue;
        maxFrameTime = float.MinValue;
    }
}