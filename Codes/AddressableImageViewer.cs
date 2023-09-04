#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Yamara
{
    /// <summary>
    /// This is a component that attaches an Addressable Sprite to an Image only while editing a Scene or Prefab
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Image))]
    public class AddressableImageViewerEditor : MonoBehaviour
    {
        [SerializeField] public Image Image;
        [SerializeField] public AssetReferenceSprite SpriteRef;
        [SerializeField] private bool clearSpriteButton;

        [Header("Don't Edit")]
        [SerializeField] private Sprite editorSprite = null;
        [SerializeField] private string editorGuid, editorSubName;
        [SerializeField] private bool editorIsNotReflected = false;


        public bool IsClean => Image.sprite == null && editorSprite == null;


        private void Start()
        {
            if (Application.isPlaying) return;

            PrefabStage.prefabSaving += OnPrefabSaving;
            PrefabStage.prefabSaved += OnPrefabSaved;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }
        private void OnDestroy()
        {
            if (Application.isPlaying) return;

            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaved -= OnPrefabSaved;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        private void OnValidate()
        {
            // MEMO : Preventing value from changing in OnValidate() during PrefabStage.prefabSaving.
            if (editorIsNotReflected) return;

            TryClearSprite();
            TryGetAndSetupImage();
        }

        private void Update()
        {
            if (Application.isPlaying) return;

            // MEMO : When create a Prefab from a GameObject on a Scene,
            // the original GameObject is deleted and a Prefab Instance is generated.
            // The Instance reflects the Prefab value after Start(), so it is attached using Update().
            if (editorIsNotReflected) OnSaved();

            TryReflectToImage();
        }

        private void TryClearSprite()
        {
            if (clearSpriteButton == false) return;
            clearSpriteButton = false;
            if (Image != null) Image.sprite = null;
            SpriteRef = null;
            editorSprite = null;
            editorGuid = null;
            editorSubName = null;
        }

        private void TryGetAndSetupImage()
        {
            if (Image != null && Image.name != null) return;

            Image ??= GetComponent<Image>();
            if (Image == null || Image.sprite == null || Image.sprite.name == null) return;

            CreateSpriteRef(Image.sprite);
            editorSprite = Image.sprite;
            editorGuid = SpriteRef.AssetGUID;
            editorSubName = SpriteRef.SubObjectName;
        }
        private void CreateSpriteRef(Sprite sprite)
        {
            var path = AssetDatabase.GetAssetPath(sprite);
            var guid = AssetDatabase.AssetPathToGUID(path);

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            var entry = settings.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                // Simplify Addressable Name
                // entry.SetAddress(Path.GetFileNameWithoutExtension(path));
                // Label
                // entry.SetLabel(nameof(Sprite), true, true);
            }

            SpriteRef = new AssetReferenceSprite(entry.guid);
            SpriteRef.SetEditorSubObject(sprite);
        }

        private void TryReflectToImage()
        {
            if (Image == null) return;

            if (SpriteRef == null || SpriteRef.AssetGUID == null)
            {
                Image.sprite = null;
                return;
            }
            if (editorGuid == SpriteRef.AssetGUID && editorSubName == SpriteRef.SubObjectName)
            {
                Image.sprite = editorSprite;
                return;
            }
            var spriteRefSprite = GetSpriteRefSprite();
            if (spriteRefSprite == null) return;

            editorSprite = spriteRefSprite;
            editorGuid = SpriteRef.AssetGUID;
            editorSubName = SpriteRef.SubObjectName;
            Image.sprite = editorSprite;
        }

        private Sprite GetSpriteRefSprite()
        {
            var hasSubSpriteRef = SpriteRef.SubObjectName != null && SpriteRef.SubObjectName.Length > 0;
            if (hasSubSpriteRef)
            {
                var path = AssetDatabase.GUIDToAssetPath(SpriteRef.AssetGUID);
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
                return sprites.FirstOrDefault(s => s.name == SpriteRef.SubObjectName);
            }
            var path2 = AssetDatabase.GUIDToAssetPath(SpriteRef.AssetGUID);
            return AssetDatabase.LoadAssetAtPath(path2, typeof(Sprite)) as Sprite;
        }


        private void OnPrefabSaving(GameObject obj) => OnSaving();
        private void OnSceneSaving(Scene scene, string path) => OnSaving();
        public void OnSaving()
        {
            Image.sprite = null;

            editorSprite = null;
            editorGuid = null;
            editorSubName = null;
            editorIsNotReflected = true;

            if (this != null && PrefabUtility.GetNearestPrefabInstanceRoot(this) == null) return;

            var serializedImage = new SerializedObject(Image);
            PrefabUtility.RevertPropertyOverride(serializedImage.FindProperty("m_Sprite"), InteractionMode.AutomatedAction);

            var serialized = new SerializedObject(this);
            PrefabUtility.RevertPropertyOverride(serialized.FindProperty(nameof(editorSprite)), InteractionMode.AutomatedAction);
            PrefabUtility.RevertPropertyOverride(serialized.FindProperty(nameof(editorGuid)), InteractionMode.AutomatedAction);
            PrefabUtility.RevertPropertyOverride(serialized.FindProperty(nameof(editorSubName)), InteractionMode.AutomatedAction);
            PrefabUtility.RevertPropertyOverride(serialized.FindProperty(nameof(editorIsNotReflected)), InteractionMode.AutomatedAction);
        }

        private void OnPrefabSaved(GameObject obj) => OnSaved();
        private void OnSceneSaved(Scene scene) => OnSaved();
        public void OnSaved()
        {
            editorIsNotReflected = false;
            var spriteRefSprite = GetSpriteRefSprite();
            if (Image != null || spriteRefSprite != null)
            {
                editorSprite = spriteRefSprite;
                editorGuid = SpriteRef.AssetGUID;
                editorSubName = SpriteRef.SubObjectName;
                Image.sprite = spriteRefSprite;
            }
        }
    }
    public class AddressableImageViewerImportEditor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
            {
                if (Path.GetExtension(path) != ".prefab") return;
                var instance = PrefabUtility.LoadPrefabContents(path);

                var hasAddressableImageViewer = false;
                var viewers = instance.GetComponentsInChildren<AddressableImageViewerEditor>(true);
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
}
#endif
