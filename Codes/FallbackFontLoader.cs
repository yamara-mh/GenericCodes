using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Localization;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Localization.Settings;
using UniRx;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace TMPro
{
    [CreateAssetMenu(fileName = nameof(FallbackFontLoader), menuName = nameof(ScriptableObject) + "/Create " + nameof(FallbackFontLoader))]
    public class FallbackFontLoader : ScriptableObject
    {
        public const string LabelName = nameof(FallbackFontLoader);

        [SerializeField] public TMP_FontAsset BaseFont;
        [SerializeField] public AssetReferenceT<TMP_FontAsset> DynamicRef;
        [SerializeField, Header("Set the font for the corresponding language.\nPlease add Locale Code to the end of Address.\nEnglish font address example : FontName en")]
        public AssetReferenceT<TMP_FontAsset>[] LanguageRefs;

        public static bool IsLoadedDefaultFonts { get; private set; }
        public bool IsLoaded { get; private set; }

        private TMP_FontAsset _dynamicFont;

        public async UniTask<TMP_FontAsset> LoadFontsAsync(Locale locale = null, CancellationToken cancellationToken = default)
        {
            IsLoaded = false;
            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            BaseFont.fallbackFontAssetTable.Clear();

            foreach (var languageRef in LanguageRefs)
            {
                var location = (await Addressables.LoadResourceLocationsAsync(languageRef.RuntimeKey)).FirstOrDefault();
                if (cancellationToken.IsCancellationRequested) return BaseFont;
                if (location == null || !location.PrimaryKey.EndsWith(locale.Identifier.Code)) continue;

                var font = await Addressables.LoadAssetAsync<TMP_FontAsset>(languageRef.RuntimeKey);
                if (cancellationToken.IsCancellationRequested) return BaseFont;
                BaseFont.fallbackFontAssetTable.Add(font);
                break;
            }

            if (DynamicRef.RuntimeKeyIsValid())
            {
                _dynamicFont = await Addressables.LoadAssetAsync<TMP_FontAsset>(DynamicRef.RuntimeKey);
                if (cancellationToken.IsCancellationRequested) return BaseFont;
#if UNITY_EDITOR
                // MEMO : The Editor uses duplication to ensure that the contents of Dynamic fonts do not change
                var source = _dynamicFont;
                _dynamicFont = TMP_FontAsset.CreateFontAsset(
                    source.sourceFontFile,
                    source.creationSettings.pointSize,
                    source.atlasPadding,
                    GlyphRenderMode.SDFAA,
                    source.atlasWidth,
                    source.atlasHeight,
                    AtlasPopulationMode.Dynamic,
                    source.isMultiAtlasTexturesEnabled);
#endif
                BaseFont.fallbackFontAssetTable.Add(_dynamicFont);
            }
            IsLoaded = true;
            return BaseFont;
        }

        public static void UpdateAllText(params TMP_FontAsset[] fonts)
        {
            if (!Application.isPlaying) return;
            foreach (TMP_Text tmpText in FindObjectsOfType(typeof(TMP_Text)))
            {
                var fallback = fonts.FirstOrDefault(f => f.name == tmpText.font.name);
                if (fallback == null) continue;
                tmpText.font = fallback;
                tmpText.ForceMeshUpdate();
            }
        }

        public void UnloadFallbacks()
        {
            if (BaseFont == null) return;
            foreach (var fallback in BaseFont.fallbackFontAssetTable)
            {
                if (string.IsNullOrEmpty(fallback.name)) continue;
                Resources.UnloadAsset(fallback);
                Addressables.Release(fallback);
            }
            BaseFont.fallbackFontAssetTable.Clear();
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(FallbackFontLoader))]
    public class FallbackFontSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var loader = target as FallbackFontLoader;

            if (GUILayout.Button("Reflected in Base Font"))
            {
                if (loader.BaseFont == null)
                {
                    Debug.LogError("Base Font is missing.");
                    return;
                }
                ReflectedInBaseFont(loader);
            }

            base.OnInspectorGUI();
        }

        private void ReflectedInBaseFont(FallbackFontLoader loader)
        {
            loader.BaseFont.fallbackFontAssetTable.Clear();

            foreach (var languageRefs in loader.LanguageRefs)
            {
                var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(languageRefs.AssetGUID);
                if (!entry.address.EndsWith(LocalizationSettings.ProjectLocale.Identifier.Code)) continue;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(entry.AssetPath);
                loader.BaseFont.fallbackFontAssetTable.Add(font);
            }

            FallbackFontLoader.UpdateAllText(loader.BaseFont);
            EditorUtility.SetDirty(loader.BaseFont);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}
