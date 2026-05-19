using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SceneCatalogEntry
{
    public SceneLoader.Scene SceneId;
    [SerializeField, HideInInspector] private int _buildIndex = -1;
    [SerializeField, HideInInspector] private string _sceneName = string.Empty;

#if UNITY_EDITOR
    [SerializeField] private SceneAsset _sceneAsset;

    public void SyncSceneDataFromAsset()
    {
        if (_sceneAsset == null)
        {
            _buildIndex = -1;
            _sceneName = string.Empty;
            return;
        }

        _sceneName = _sceneAsset.name;
        _buildIndex = ResolveBuildIndex(AssetDatabase.GetAssetPath(_sceneAsset));
    }

    private static int ResolveBuildIndex(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return -1;
        }

        var scenes = EditorBuildSettings.scenes;
        for (int index = 0; index < scenes.Length; index++)
        {
            if (scenes[index].enabled && string.Equals(scenes[index].path, scenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }
#endif

    public bool TryGetRuntimeSceneInfo(out int buildIndex, out string sceneName)
    {
        buildIndex = _buildIndex;
        sceneName = _sceneName;
        return buildIndex >= 0 || !string.IsNullOrWhiteSpace(sceneName);
    }
}

[CreateAssetMenu(fileName = "SceneCatalog", menuName = "SmallWarThunder/核心/场景/场景目录")]
public sealed class SceneCatalog : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] private SceneCatalogEntry[] _entries;

    private readonly Dictionary<SceneLoader.Scene, int> _sceneBuildIndices = new Dictionary<SceneLoader.Scene, int>();
    private readonly Dictionary<SceneLoader.Scene, string> _sceneNames = new Dictionary<SceneLoader.Scene, string>();

    private void OnEnable()
    {
        RebuildLookup();
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        SyncEditorScenePaths();
#endif
    }

    public void OnAfterDeserialize()
    {
        RebuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncEditorScenePaths();
        RebuildLookup();
    }
#endif

    public bool TryGetSceneBuildIndex(SceneLoader.Scene sceneId, out int buildIndex)
    {
        EnsureLookup();
        return _sceneBuildIndices.TryGetValue(sceneId, out buildIndex);
    }

    public bool TryGetSceneName(SceneLoader.Scene sceneId, out string sceneName)
    {
        EnsureLookup();
        return _sceneNames.TryGetValue(sceneId, out sceneName);
    }

    private void EnsureLookup()
    {
        if (_sceneBuildIndices.Count == 0 && _sceneNames.Count == 0 && _entries != null && _entries.Length > 0)
        {
            RebuildLookup();
        }
    }

    private void RebuildLookup()
    {
        _sceneBuildIndices.Clear();
        _sceneNames.Clear();

        if (_entries == null)
        {
            return;
        }

        for (int index = 0; index < _entries.Length; index++)
        {
            SceneCatalogEntry entry = _entries[index];
            if (entry == null || _sceneBuildIndices.ContainsKey(entry.SceneId))
            {
                continue;
            }

            if (!entry.TryGetRuntimeSceneInfo(out int buildIndex, out string sceneName))
            {
                continue;
            }

            if (buildIndex >= 0)
            {
                _sceneBuildIndices.Add(entry.SceneId, buildIndex);
            }

            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                _sceneNames[entry.SceneId] = sceneName.Trim();
            }
        }
    }

#if UNITY_EDITOR
    private void SyncEditorScenePaths()
    {
        if (_entries == null)
        {
            return;
        }

        for (int index = 0; index < _entries.Length; index++)
        {
            SceneCatalogEntry entry = _entries[index];
            if (entry == null)
            {
                continue;
            }

            entry.SyncSceneDataFromAsset();
        }
    }
#endif
}