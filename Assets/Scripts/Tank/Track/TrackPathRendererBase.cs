using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class TrackPathRendererBase : MonoBehaviour
{
    [Header("引用")]
    public Transform pathRoot;
    public GameObject trackPrefab;

    [Header("差速联动")]
    public bool autoDetectTrackSide = true;
    public bool isLeftTrack = true;

    [Header("参数")]
    public int linkCount = 120;
    public float scrollSpeed = 2f;
    public Vector3 rotationOffset = Vector3.zero;
    public float scrollSpeedLerp = 10f;

    protected readonly List<Transform> nodes = new List<Transform>();
    protected readonly List<GameObject> spawnedLinks = new List<GameObject>();
    protected TankTrackSideDrivePoint sideDrivePoint;
    protected float _visualScrollSpeed;
    protected float _trackOffset;

    public float VisualScrollSpeed => _visualScrollSpeed;
    public bool IsLeftTrack => GetTrackIsLeftSide();

    protected virtual void Start()
    {
        CacheRuntimeReferences();
        InitializeNodes();
        SpawnTracks();
        _visualScrollSpeed = ResolveScrollSpeed();
    }

    protected virtual void OnEnable()
    {
        CacheRuntimeReferences();
    }

    protected virtual void LateUpdate()
    {
        if (nodes.Count < 2 || spawnedLinks.Count == 0)
        {
            return;
        }

        float totalLength = GetTotalPathLength();
        if (totalLength <= 0.001f)
        {
            return;
        }

        float effectiveScrollSpeed = ResolveScrollSpeed();
        float lerpT = Mathf.Clamp01(scrollSpeedLerp * Time.deltaTime);
        _visualScrollSpeed = Mathf.Lerp(_visualScrollSpeed, effectiveScrollSpeed, lerpT);
        _trackOffset = Mathf.Repeat(_trackOffset + _visualScrollSpeed * Time.deltaTime, totalLength);

        for (int i = 0; i < spawnedLinks.Count; i++)
        {
            float distanceAlongTrack = Mathf.Repeat((float)i / spawnedLinks.Count * totalLength + _trackOffset, totalLength);
            UpdateLinkPosition(spawnedLinks[i].transform, distanceAlongTrack);
        }
    }

    protected virtual void CacheRuntimeReferences()
    {
        sideDrivePoint = GetComponentInParent<TankTrackSideDrivePoint>();
    }

    protected void InitializeNodes()
    {
        nodes.Clear();
        if (pathRoot == null)
        {
            return;
        }

        foreach (Transform child in pathRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child != pathRoot && child.name.Contains("Node"))
            {
                nodes.Add(child);
            }
        }

        nodes.Sort((left, right) => ExtractNodeIndex(left.name).CompareTo(ExtractNodeIndex(right.name)));
    }

    protected void SpawnTracks()
    {
        foreach (Transform child in transform.Cast<Transform>().ToList())
        {
            if (child == transform)
            {
                continue;
            }

            if (child.name.StartsWith("TrackLink_"))
            {
                Destroy(child.gameObject);
            }
        }

        spawnedLinks.Clear();
        if (trackPrefab == null || linkCount <= 0)
        {
            return;
        }

        for (int i = 0; i < linkCount; i++)
        {
            GameObject link = Instantiate(trackPrefab, transform);
            link.name = $"TrackLink_{i}";
            spawnedLinks.Add(link);
        }
    }

    protected void UpdateLinkPosition(Transform link, float distance)
    {
        float traveled = 0f;
        for (int i = 0; i < nodes.Count; i++)
        {
            int next = (i + 1) % nodes.Count;
            float segmentLength = Vector3.Distance(nodes[i].position, nodes[next].position);

            if (traveled + segmentLength >= distance)
            {
                float t = segmentLength > 0.001f ? (distance - traveled) / segmentLength : 0f;

                Vector3 position = Vector3.Lerp(nodes[i].position, nodes[next].position, t);
                Vector3 tangent = (nodes[next].position - nodes[i].position).normalized;
                Vector3 binormal = transform.right;
                Vector3 normal = Vector3.Cross(tangent, binormal).normalized;

                Quaternion rotation = Quaternion.LookRotation(tangent, normal)
                                      * Quaternion.Euler(rotationOffset);

                link.position = position;
                link.rotation = rotation;
                return;
            }

            traveled += segmentLength;
        }
    }

    protected float GetTotalPathLength()
    {
        if (nodes.Count < 2)
        {
            return 0f;
        }

        float length = 0f;
        for (int i = 0; i < nodes.Count; i++)
        {
            int next = (i + 1) % nodes.Count;
            length += Vector3.Distance(nodes[i].position, nodes[next].position);
        }

        return length;
    }

    protected float ResolveSideDriveVisualMultiplier()
    {
        return sideDrivePoint != null ? sideDrivePoint.CurrentVisualSpeedMultiplier : 1f;
    }

    protected bool GetTrackIsLeftSide()
    {
        if (!autoDetectTrackSide)
        {
            return isLeftTrack;
        }

        if (sideDrivePoint != null)
        {
            return sideDrivePoint.Side == TankTrackSideDrivePoint.TrackSide.Left;
        }

        return transform.localPosition.x < 0f;
    }

    public virtual void SetTankSuspensionManager(TankSuspensionManager manager)
    {
    }

    public virtual void SetAiSuspensionManager(AI_TankSuspensionManager manager)
    {
    }

    protected abstract float ResolveScrollSpeed();
    public abstract float GetWheelVisualSpeed();

    private static int ExtractNodeIndex(string nodeName)
    {
        System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(nodeName ?? string.Empty, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}