using UnityEngine;

/// <summary>
/// AI 坦克履带纹理偏移动画，基于速度驱动履带滚动效果。
/// 借鉴 TrackController（Tank），不引用 Tank 文件夹。
/// 通过材质 _MainTex 的 offset 或 _BaseColorMap 的 offset 实现纹理偏移。
/// </summary>
namespace NAI
{
    public class AI_TrackVisualDriver : MonoBehaviour
    {
        [Header("履带渲染器")]
        [SerializeField] private Renderer[] _trackRenderers;
        [SerializeField] private int _materialIndex = 0;

        [Header("纹理偏移参数")]
        [SerializeField] private float _scrollSpeedFactor = 0.05f;
        [SerializeField] private string _texturePropertyName = "_BaseColorMap";

        [Header("数据源")]
        [SerializeField] private AI_MotionDriver _motionDriver;

        private int _texPropId;
        private float _accumulatedOffset;

        private void Awake()
        {
            _texPropId = Shader.PropertyToID(string.IsNullOrEmpty(_texturePropertyName) ? "_BaseColorMap" : _texturePropertyName);
            if (_motionDriver == null) _motionDriver = GetComponentInParent<AI_MotionDriver>();
            Debug.Log($"[AI_TrackVisualDriver] {name} 初始化，履带渲染器数量: {(_trackRenderers?.Length ?? 0)}");
        }

        private void Update()
        {
            if (_trackRenderers == null || _trackRenderers.Length == 0) return;
            if (_motionDriver == null) return;

            float speed = GetTrackSpeed();
            float scrollDelta = speed * _scrollSpeedFactor * Time.deltaTime;
            _accumulatedOffset += scrollDelta;

            Vector2 offset = new Vector2(0f, _accumulatedOffset);
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            foreach (Renderer renderer in _trackRenderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(block, _materialIndex);
                block.SetVector(_texPropId + "_ST", new Vector4(1f, 1f, 0f, _accumulatedOffset));
                renderer.SetPropertyBlock(block, _materialIndex);
            }
        }

        /// <summary>
        /// 从 AI_MotionDriver 获取当前车体速度 (m/s)，正值=前进，负值=后退
        /// </summary>
        private float GetTrackSpeed()
        {
            Rigidbody rb = _motionDriver.GetComponent<Rigidbody>();
            if (rb == null) return _motionDriver.ForwardInput * 10f; // 估算
            Vector3 localVel = _motionDriver.transform.InverseTransformDirection(rb.linearVelocity);
            return localVel.z;
        }
    }
}
