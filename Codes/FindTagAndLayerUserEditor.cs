#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Yamara
{
    public class FindTagAndLayerUserEditor : EditorWindow
    {
        [MenuItem("Tools/Utility/Open " + nameof(FindTagAndLayerUserEditor))]
        private static void Open()
        {
            var window = (FindTagAndLayerUserEditor)GetWindow(typeof(FindTagAndLayerUserEditor));
            window.Show();
        }

        private FindType _findType;
        private enum FindType
        {
            Tag = 0,
            Layer = 1,
        }
        private string _targetName;
        private int _layerValue;
        private StringBuilder _pathes = new();
        private bool _savedCurrentScene;

        void OnGUI()
        {
            _findType = (FindType)EditorGUILayout.EnumPopup("Find Type", _findType);
            _targetName = EditorGUILayout.TextField("Target Name", _targetName);
            if (GUILayout.Button("Find")) FindAsset();
        }
        private void FindAsset()
        {
            if (_findType == FindType.Layer)
            {
                _layerValue = LayerMask.NameToLayer(_targetName);
                // Check existence of layer name
                if (string.IsNullOrEmpty(LayerMask.LayerToName(_layerValue))) return;
            }
            else
            {
                // Check existence of tag name
                GameObject.FindGameObjectWithTag(_targetName);
            }
            _pathes.Clear();
            _savedCurrentScene = false;
            var currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            var objectCount = 0;
            foreach (var obj in Selection.objects) objectCount = FindAsset(objectCount, obj);
            Log(objectCount);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != currentScenePath) EditorSceneManager.OpenScene(currentScenePath);
        }

        private int FindAsset(int count, Object obj)
        {
            if (obj == null) return 0;
            if (obj is GameObject) return FindInPrefab(obj);
            if (obj is SceneAsset) return FindInScene(obj);

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !File.GetAttributes(path).HasFlag(FileAttributes.Directory))
            {
                return count;
            }

            foreach (var childPath in AssetDatabase.FindAssets("t:prefab t:scene", new[] { path }).Select(AssetDatabase.GUIDToAssetPath))
            {
                count += FindAsset(count, AssetDatabase.LoadAssetAtPath<Object>(childPath));
            }
            return count;
        }

        private int FindInPrefab(Object obj)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
            var gameObject = PrefabUtility.LoadPrefabContents(path);

            var count = 0;
            foreach (var t in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (!CheckTagOrLayer(t.gameObject)) continue;

                if (count == 0) _pathes.AppendLine(path);
                count++;
                _pathes.Append("    ");
                _pathes.AppendLine(GetPath(t));
            }
            if (count > 0) PrefabUtility.UnloadPrefabContents(gameObject);
            return count;
        }
        private int FindInScene(Object obj)
        {
            if (!_savedCurrentScene) EditorSceneManager.SaveOpenScenes();
            var path = AssetDatabase.GetAssetPath(obj);
            EditorSceneManager.OpenScene(path);

            var count = 0;
            foreach (var sceneObj in FindObjectsOfType(typeof(GameObject)) as GameObject[])
            {
                if (sceneObj.activeInHierarchy || !CheckTagOrLayer(sceneObj)) continue;

                if (count == 0) _pathes.AppendLine(path);
                count++;
                _pathes.Append("    ");
                _pathes.AppendLine(GetPath(sceneObj.transform));
            }
            return count;
        }

        private bool CheckTagOrLayer(GameObject obj) => _findType == FindType.Tag
                    ? obj.CompareTag(_targetName)
                    : obj.layer == _layerValue;

        private void Log(int objectCount)
        {
            Debug.Log($"[{nameof(FindTagAndLayerUserEditor)}] {_targetName} {_findType} is used by {objectCount} in prefabs (and scenes).\n" + _pathes.ToString());
        }
        private string GetPath(Transform t)
        {
            var path = t.name;
            var parent = t.parent;
            while (parent)
            {
                path = $"{parent.name}/{path}";
                parent = parent.parent;
            }
            return path;
        }
    }
}
#endif
