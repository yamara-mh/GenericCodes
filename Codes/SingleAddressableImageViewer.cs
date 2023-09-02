using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace Yamara
{
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class SingleAddressableImageViewer : MonoBehaviour
    {
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] public Image Image;
        [SerializeField] public AssetReferenceSprite SpriteRef;

        private AsyncOperationHandle<Sprite> _handle = default;

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

            _handle = SpriteRef.LoadAssetAsync();
            await _handle;
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
        [SerializeField] private Sprite editorSprite = null;
        [SerializeField] private bool editorSaving = false;

        private bool HasImageSprite() => Image != null && Image.sprite != null && Image.sprite.name != null;
        private bool HasSpriteRef() => SpriteRef != null && SpriteRef.RuntimeKeyIsValid();

        private void StartEditor()
        {
            PrefabStage.prefabSaving += OnPrefabSaving;
            PrefabStage.prefabSaved += OnPrefabSaved;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;

            editorSaving = false;
            if (HasSpriteRef())
            {
                editorSprite = GetSpriteRefSprite();
                UpdateImageSprite();
            }
        }
        private void DestroyEditor()
        {
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
            TryReflrectToImage();
        }
        private void Update()
        {
            if (Application.isPlaying == false) TryReflectToSpriteRef();
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
            if (HasImageSprite() == false) return;

            editorSprite = Image.sprite;
            UpdateSpriteRef();
        }
        private void TryReflrectToImage()
        {
            if (Image == null || SpriteRef == null) return;

            var spriteRefSprite = GetSpriteRefSprite();
            if (spriteRefSprite == null || spriteRefSprite == Image.sprite) return;

            editorSprite = spriteRefSprite;
            UpdateImageSprite();
        }
        private void TryReflectToSpriteRef()
        {
            if (HasImageSprite() == false || Image.sprite == editorSprite) return;

            editorSprite = Image.sprite;
            UpdateSpriteRef();
        }


        private void UpdateSpriteRef()
        {
            if (SpriteRef == null) return;

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
        private void UpdateImageSprite()
        {
            if (Image == null) return;
            if (HasSpriteRef() == false) return;
            Image.sprite = editorSprite;
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
        private void OnSaving()
        {
            editorSaving = true;
            Image.sprite = null;
            editorSprite = null;
            EditorUtility.SetDirty(Image);
        }

        private void OnPrefabSaved(GameObject obj) => OnSaved();
        private void OnSceneSaved(Scene scene) => OnSaved();
        private void OnSaved()
        {
            editorSaving = false;
            var spriteRefSprite = GetSpriteRefSprite();
            if (Image != null || spriteRefSprite != null)
            {
                editorSprite = spriteRefSprite;
                UpdateImageSprite();
            }
        }
#endif
        #endregion
    }
}
