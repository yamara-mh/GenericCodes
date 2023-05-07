using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Localization;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using UniRx;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace Yamara.TMPro
{
    [CreateAssetMenu(fileName = nameof(FallbackFontLoader), menuName = nameof(ScriptableObject) + "/Create " + nameof(FallbackFontLoader))]
    public class FallbackFontLoader : ScriptableObject
    {
        public const string LabelName = nameof(FallbackFontLoader);

        [SerializeField] public bool LoadOnStartup = true;
        [SerializeField] public AssetReferenceT<TMP_FontAsset> BaseRef;
        [SerializeField] public AssetReferenceT<TMP_FontAsset> DynamicRef;
        [SerializeField, Header("Set the font for the corresponding language.\nPlease add Locale Code to the end of Address.\nEnglish font address example : FontName en")]
        public AssetReferenceT<TMP_FontAsset>[] LanguageRefs;

        public static bool IsLoadedDefaultFonts { get; private set; }
        public bool IsLoaded { get; private set; }

        private TMP_FontAsset _font;
        private TMP_FontAsset _dynamicFont;

        [RuntimeInitializeOnLoadMethod()]
        static void EnterPlayMode()
        {
            LoadDefaultFonts();
            LocalizationSettings.SelectedLocaleChanged += ReloadDefaultFonts;
        }

        private static async void LoadDefaultFonts(Locale locale = null)
        {
            IsLoadedDefaultFonts = false;
            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            foreach (var locations in await Addressables.LoadResourceLocationsAsync(LabelName, typeof(FallbackFontLoader)).ToUniTask())
            {
                var loader = await Addressables.LoadAssetAsync<FallbackFontLoader>(locations).ToUniTask();
                if (!loader.LoadOnStartup) continue;
                await loader.LoadFontsAsync(locale);
            }
            IsLoadedDefaultFonts = true;
        }
        private static async void ReloadDefaultFonts(Locale locale = null)
        {
            IsLoadedDefaultFonts = false;
            foreach (var locations in await Addressables.LoadResourceLocationsAsync(LabelName, typeof(FallbackFontLoader)).ToUniTask())
            {
                var loader = await Addressables.LoadAssetAsync<FallbackFontLoader>(locations).ToUniTask();
                loader.UnloadFallbacks();
            }
            LoadDefaultFonts();
        }

        public async UniTask<TMP_FontAsset> LoadFontsAsync(Locale locale = null, CancellationToken cancellationToken = default)
        {
            IsLoaded = false;
            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            _font ??= await Addressables.LoadAssetAsync<TMP_FontAsset>(BaseRef.RuntimeKey);
            if (cancellationToken.IsCancellationRequested) return _font;
            _font.fallbackFontAssetTable.Clear();

            foreach (var languageRef in LanguageRefs)
            {
                var location = (await Addressables.LoadResourceLocationsAsync(languageRef.RuntimeKey)).FirstOrDefault();
                if (cancellationToken.IsCancellationRequested) return _font;
                if (location == null || !location.PrimaryKey.EndsWith(locale.Identifier.Code)) continue;

                var font = await Addressables.LoadAssetAsync<TMP_FontAsset>(languageRef.RuntimeKey);
                if (cancellationToken.IsCancellationRequested) return _font;
                _font.fallbackFontAssetTable.Add(font);
                break;
            }

            if (DynamicRef.RuntimeKeyIsValid())
            {
                _dynamicFont = await Addressables.LoadAssetAsync<TMP_FontAsset>(DynamicRef.RuntimeKey);
                if (cancellationToken.IsCancellationRequested) return _font;
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
                _font.fallbackFontAssetTable.Add(_dynamicFont);
            }
            IsLoaded = true;
            return _font;
        }

        public async void UpdateAllText()
        {
            var list = new List<TMP_FontAsset>();
            foreach (var locations in await Addressables.LoadResourceLocationsAsync(LabelName, typeof(FallbackFontLoader)).ToUniTask())
            {
                var loader = await Addressables.LoadAssetAsync<FallbackFontLoader>(locations).ToUniTask();
                list.Add(loader._font);
            }
            UpdateAllText(list.ToArray());
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
            if (_font == null) return;
            foreach (var fallback in _font.fallbackFontAssetTable)
            {
                if (string.IsNullOrEmpty(fallback.name)) continue;
                Resources.UnloadAsset(fallback);
                Addressables.Release(fallback);
            }
            _font.fallbackFontAssetTable.Clear();
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(FallbackFontLoader))]
    public class FallbackFontSettingsEditor : Editor
    {
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(target.name)) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings.GetLabels().Contains(FallbackFontLoader.LabelName)) settings.AddLabel(FallbackFontLoader.LabelName);

            var assetPath = AssetDatabase.GetAssetPath(target);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(assetGuid);

            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), settings.DefaultGroup);
                EditorUtility.SetDirty(target);
            }
            if (!entry.labels.TryGetValue(FallbackFontLoader.LabelName, out _))
            {
                entry.SetLabel(FallbackFontLoader.LabelName, true, true);
                EditorUtility.SetDirty(target);
            }
            AssetDatabase.SaveAssets();
        }

        public override void OnInspectorGUI()
        {
            var loader = target as FallbackFontLoader;

            if (GUILayout.Button("Reflected in Base Font"))
            {
                if (string.IsNullOrEmpty(loader.BaseRef.AssetGUID))
                {
                    Debug.LogError("Base Ref is missing.");
                    return;
                }
                ReflectedInBaseFont(loader);
            }

            base.OnInspectorGUI();
        }

        private void ReflectedInBaseFont(FallbackFontLoader loader)
        {
            var baseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(loader.BaseRef.AssetGUID).AssetPath);
            baseFont.fallbackFontAssetTable.Clear();

            foreach (var languageRefs in loader.LanguageRefs)
            {
                var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(languageRefs.AssetGUID);
                if (!entry.address.EndsWith(LocalizationSettings.ProjectLocale.Identifier.Code)) continue;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(entry.AssetPath);
                baseFont.fallbackFontAssetTable.Add(font);
            }

            FallbackFontLoader.UpdateAllText(baseFont);
            EditorUtility.SetDirty(baseFont);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}
