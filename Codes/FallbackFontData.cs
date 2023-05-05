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
    [CreateAssetMenu(fileName = nameof(FallbackFontData), menuName = nameof(ScriptableObject) + "/Create " + nameof(FallbackFontData))]
    public class FallbackFontData : ScriptableObject
    {
        public const string LabelName = nameof(FallbackFontData);

        [SerializeField] public bool LoadOnStartup = true;
        [SerializeField] public AssetReferenceT<TMP_FontAsset> BaseRef;
        [SerializeField] public AssetReferenceT<TMP_FontAsset> DynamicRef;
        [SerializeField, Header("Set the font for the corresponding language.\nPlease add Locale Code to the end of Address.\nEnglish font address example : FontName en")]
        public AssetReferenceT<TMP_FontAsset>[] LocalizationRefs;

        public static bool IsLoadedDefaultFonts { get; private set; }

        public bool IsLoaded { get; private set; }

        private TMP_FontAsset _font;
        private TMP_FontAsset _dynamicFont;

        [RuntimeInitializeOnLoadMethod()]
        static void EnterPlayMode()
        {
            LoadDefaultFonts();
            LocalizationSettings.SelectedLocaleChanged += LoadDefaultFonts;
        }

        private static async void LoadDefaultFonts(Locale locale = null)
        {
            IsLoadedDefaultFonts = false;
            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            var updateFonts = new List<TMP_FontAsset>();
            foreach (var locations in await Addressables.LoadResourceLocationsAsync(LabelName, typeof(FallbackFontData)).ToUniTask())
            {
                var settings = await Addressables.LoadAssetAsync<FallbackFontData>(locations).ToUniTask();
                if (!settings.LoadOnStartup) continue;
                await settings.LoadFontsAsync(locale);
            }
            IsLoadedDefaultFonts = true;
            if (updateFonts.Count > 0) UpdateAllText(updateFonts.ToArray());
        }

        public async UniTask<TMP_FontAsset> LoadFontsAsync(Locale locale = null, CancellationToken cancellationToken = default)
        {
            IsLoaded = false;
            locale ??= await LocalizationSettings.SelectedLocaleAsync;
            _font ??= await Addressables.LoadAssetAsync<TMP_FontAsset>(BaseRef.RuntimeKey);
            if (cancellationToken.IsCancellationRequested) return _font;
            _font.fallbackFontAssetTable.Clear();

            foreach (var localizationRef in LocalizationRefs)
            {
                var location = (await Addressables.LoadResourceLocationsAsync(localizationRef.RuntimeKey)).FirstOrDefault();
                if (cancellationToken.IsCancellationRequested) return _font;
                if (location == null || !location.PrimaryKey.EndsWith(locale.Identifier.Code)) continue;

                var font = await Addressables.LoadAssetAsync<TMP_FontAsset>(localizationRef.RuntimeKey);
                if (cancellationToken.IsCancellationRequested) return _font;
                _font.fallbackFontAssetTable.Add(font);
                break;
            }

            if (DynamicRef.RuntimeKeyIsValid())
            {
                if (_dynamicFont == null)
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
                }
                _font.fallbackFontAssetTable.Add(_dynamicFont);
            }
            IsLoaded = true;
            return _font;
        }

        public void UpdateAllText() => UpdateAllText(_font);
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
                Addressables.Release(fallback);
            }
            _font.fallbackFontAssetTable.Clear();
        }

        public async UniTask ReloadFallbacksAsync(Locale locale)
        {
            UnloadFallbacks();
            await LoadFontsAsync(locale);
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(FallbackFontData))]
    public class FallbackFontSettingsEditor : Editor
    {
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(target.name)) return;
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings.GetLabels().Contains(FallbackFontData.LabelName)) settings.AddLabel(FallbackFontData.LabelName);

            var assetPath = AssetDatabase.GetAssetPath(target);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(assetGuid);

            if (entry == null)
            {
                entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(assetPath), settings.DefaultGroup);
                EditorUtility.SetDirty(target);
            }
            if (!entry.labels.TryGetValue(FallbackFontData.LabelName, out _))
            {
                entry.SetLabel(FallbackFontData.LabelName, true, true);
                EditorUtility.SetDirty(target);
            }
            AssetDatabase.SaveAssets();
        }

        public override void OnInspectorGUI()
        {
            var settings = target as FallbackFontData;

            if (GUILayout.Button("Reflected in Base Font"))
            {
                if (string.IsNullOrEmpty(settings.BaseRef.AssetGUID))
                {
                    Debug.LogError("Base Ref is missing.");
                    return;
                }
                ReflectedInBaseFont(settings);
            }

            base.OnInspectorGUI();
        }

        private void ReflectedInBaseFont(FallbackFontData settings)
        {
            var baseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(settings.BaseRef.AssetGUID).AssetPath);
            baseFont.fallbackFontAssetTable.Clear();

            foreach (var localizationRef in settings.LocalizationRefs)
            {
                var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(localizationRef.AssetGUID);
                if (!entry.address.EndsWith(LocalizationSettings.ProjectLocale.Identifier.Code)) continue;
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(entry.AssetPath);
                baseFont.fallbackFontAssetTable.Add(font);
            }

            FallbackFontData.UpdateAllText(baseFont);
            EditorUtility.SetDirty(baseFont);
            AssetDatabase.SaveAssets();
        }
    }
#endif
}
