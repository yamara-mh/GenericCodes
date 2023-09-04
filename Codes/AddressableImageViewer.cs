using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Yamara
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class AddressableImageViewer : MonoBehaviour
    {
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] public Image Image;
        [SerializeField] public AssetReferenceSprite SpriteRef;

        private AsyncOperationHandle<Sprite> _handle = default;
        private bool _loading = false;

        private async void Start()
        {
            if (Application.isPlaying && loadOnStart) await Load();

#if UNITY_EDITOR
            if (Application.isPlaying == false) StartEditor();
#endif
        }
        private void OnDestroy()
        {
            if (Application.isPlaying) Release();

#if UNITY_EDITOR
            if (Application.isPlaying == false) DestroyEditor();
#endif
        }

        public async UniTask<bool> Load()
        {
            if (SpriteRef == null || SpriteRef.RuntimeKeyIsValid() == false) return false;
            if (_handle.IsValid())
            {
                if (_handle.Status == AsyncOperationStatus.Failed) return false;
                if (_handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Image.sprite = _handle.Result;
                    return true;
                }
            }

            _loading = true;
            if (_loading) _handle = SpriteRef.LoadAssetAsync();
            await _handle;
            _loading = false;

            if (_handle.Status != AsyncOperationStatus.Succeeded) return false;
            Image.sprite = _handle.Result;
            return true;
        }

        public void Release()
        {
            if (SpriteRef != null && SpriteRef.IsValid()) SpriteRef.ReleaseAsset();
        }

        #region Editor

#if UNITY_EDITOR

        private const string GroupName = null;
        private const string Label = null;

        [Header(nameof(Editor))]
        [SerializeField] private bool clearSpriteButton;
        [Header("Don't Touch")]
        [SerializeField] private Sprite editorSprite = null;
        [SerializeField] private bool editorSaving = false;

        private bool HasImageSprite => Image != null && Image.sprite != null && Image.sprite.name != null;
        private bool HasSpriteRef => SpriteRef != null && SpriteRef.RuntimeKeyIsValid();
        public bool IsClean => Image.sprite == null && editorSprite == null;

        private void StartEditor()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            PrefabStage.prefabSaving += OnPrefabSaving;
            PrefabStage.prefabSaved += OnPrefabSaved;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;

            if (editorSaving) OnSaved();
        }
        private void DestroyEditor()
        {
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaved -= OnPrefabSaving;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnValidate()
        {
            if (editorSaving) return;
            TryClearSprite();
            TryGetAndSetupImage();
            TryReflectToImage();
        }

        private void Update()
        {
            if (Application.isPlaying) return;
            if (editorSaving) OnSaved();
            TryReflectToSpriteRef();
        }

        private void OnHierarchyChanged()
        {
            if (Application.isPlaying) return;
            TryReflectToImage();
        }

        private void TryClearSprite()
        {
            if (clearSpriteButton == false) return;
            clearSpriteButton = false;
            if (Image != null) Image.sprite = null;
            SpriteRef = null;
            editorSprite = null;
        }

        private void TryGetAndSetupImage()
        {
            if (Image != null && Image.name != null) return;

            Image ??= GetComponent<Image>();
            loadOnStart = TryGetComponent<LocalizeSpriteEvent>(out var _) == false;

            if (HasImageSprite == false) return;

            editorSprite = Image.sprite;
            UpdateSpriteRef();
        }
        private void TryReflectToImage()
        {
            if (Image == null || SpriteRef == null) return;

            if (editorSprite == SpriteRef.editorAsset)
            {
                Image.sprite = editorSprite;
                return;
            }
            var spriteRefSprite = GetSpriteRefSprite();
            if (spriteRefSprite == null || spriteRefSprite == Image.sprite) return;

            editorSprite = spriteRefSprite;
            Image.sprite = editorSprite;
        }
        private void TryReflectToSpriteRef()
        {
            if (HasImageSprite == false || Image.sprite == editorSprite) return;

            editorSprite = Image.sprite;
            UpdateSpriteRef();
        }


        private void UpdateSpriteRef()
        {
            var path = AssetDatabase.GetAssetPath(Image.sprite);
            var guid = AssetDatabase.AssetPathToGUID(path);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                var group = settings.groups.FirstOrDefault(g => g.name == GroupName) ?? settings.DefaultGroup;
                entry = settings.CreateOrMoveEntry(guid, group);
                // Simplify Addressable Name
                // entry.SetAddress(Path.GetFileNameWithoutExtension(path));
                // Label
                // entry.SetLabel(Label, true, true);
            }

            SpriteRef = new AssetReferenceSprite(entry.guid);
            SpriteRef.SetEditorSubObject(editorSprite);
        }

        private Sprite GetSpriteRefSprite()
        {
            var hasSubSpriteRef = SpriteRef.SubObjectName != null && SpriteRef.SubObjectName.Length > 0;
            if (hasSubSpriteRef)
            {
                var path = AssetDatabase.GetAssetPath(SpriteRef.editorAsset);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
                return sprites.FirstOrDefault(s => s.name == SpriteRef.SubObjectName);
            }
            return SpriteRef.editorAsset as Sprite;
        }


        private void OnPrefabSaving(GameObject obj) => OnSaving();
        private void OnSceneSaving(Scene scene, string path) => OnSaving();
        public void OnSaving()
        {
            editorSaving = true;
            Image.sprite = null;
            editorSprite = null;
        }

        private void OnPrefabSaved(GameObject obj) => OnSaved();
        private void OnSceneSaved(Scene scene) => OnSaved();
        public void OnSaved()
        {
            editorSaving = false;
            var spriteRefSprite = GetSpriteRefSprite();
            if (Image != null || spriteRefSprite != null)
            {
                editorSprite = spriteRefSprite;
                Image.sprite = spriteRefSprite;
            }
        }
#endif
        #endregion
    }
#if UNITY_EDITOR
    public class AddressableImageViewerImportEditor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (Path.GetExtension(path) != ".prefab") return;
                var instance = PrefabUtility.LoadPrefabContents(path);

                var hasAddressableImageViewer = false;
                var viewers = instance.GetComponentsInChildren<AddressableImageViewer>(true);
                foreach (var viewer in viewers)
                {
                    if (viewer.IsClean) continue;
                    hasAddressableImageViewer = true;
                    viewer.OnSaving();
                }

                if (hasAddressableImageViewer == false) return;

                PrefabUtility.SaveAsPrefabAsset(instance, path);
                PrefabUtility.UnloadPrefabContents(instance);
            }
        }
    }
#endif
}
