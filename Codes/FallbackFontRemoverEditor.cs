#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Yamara.FontLoader
{
    public class FallbackFontRemoverEditor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string SavePath = "Temp/" + nameof(FallbackFontRemoverEditor);
        int IOrderedCallback.callbackOrder => 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnterPlayMode() => RemoveFallbackFonts(false);

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report) => RemoveFallbackFonts(true);
        void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report) => AddFallbackFonts();

        private static void RemoveFallbackFonts(bool build)
        {
            var storage = new FallbackData();
            var releaseList = new List<TMP_FontAsset>();
            var removeFallbackList = new List<TMP_FontAsset>();
            var log = new StringBuilder();

            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(FallbackFontData)}"))
            {
                var settings = AssetDatabase.LoadAssetAtPath<FallbackFontData>(AssetDatabase.GUIDToAssetPath(guid));
                var entry = AddressableAssetSettingsDefaultObject.GetSettings(false).FindAssetEntry(settings.BaseRef.AssetGUID);
                removeFallbackList.Add(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(entry.AssetPath));
            }
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(TMP_FontAsset)}"))
            {
                var fontAssetPath = AssetDatabase.GUIDToAssetPath(guid);
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
                if (fontAsset.fallbackFontAssetTable.Count == 0) continue;

                if (removeFallbackList.Contains(fontAsset))
                {
                    releaseList.Add(fontAsset);
                    storage.Add(fontAssetPath, fontAsset);
                    fontAsset.fallbackFontAssetTable.Clear();
                    if (build) EditorUtility.SetDirty(fontAsset);
                }
                else Debug.LogError($"[{nameof(FallbackFontRemoverEditor)}] {nameof(TMP_FontAsset)} with a fallbackFontAssetTable needs to create {nameof(FallbackFontData)}.\nFont name : {fontAsset.name}");
            }

            if (build)
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(storage));
                AssetDatabase.SaveAssets();
                log.AppendLine($"Emptied the fallbackFontAssetTable of {storage.Paths.Count} TMP_FontAssets at build time.");
            }
            else
            {
                var disposer = new GameObject(nameof(FallbackFontRemoverEditor) + " Disposer");
                GameObject.DontDestroyOnLoad(disposer);
                disposer.OnDestroyAsObservable().Subscribe(_ =>
                {
                    foreach (var fontData in releaseList) Resources.UnloadAsset(fontData);
                });
                log.AppendLine($"Emptied the fallbackFontAssetTable of {storage.Paths.Count} TMP_FontAssets at play time.");
            }

            for (int i = 0; i < storage.Paths.Count; i++) log.AppendLine(storage.Paths[i]);
            Debug.Log($"[{nameof(FallbackFontRemoverEditor)}] {log}");
        }
        private static void AddFallbackFonts()
        {
            var storage = JsonUtility.FromJson<FallbackData>(File.ReadAllText(SavePath));
            for (int i = 0; i < storage.Paths.Count; i++)
            {
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(storage.Paths[i]);
                fontAsset.fallbackFontAssetTable = storage.GetFallbackList(i);
                EditorUtility.SetDirty(fontAsset);
            }
            AssetDatabase.SaveAssets();
            File.Delete(SavePath);
        }
    }

    [Serializable]
    class FallbackData
    {
        public List<string> Paths = new List<string>();
        public List<int> FallBacksCounts = new List<int>();
        public List<string> Fallbacks = new List<string>();
        public void Add(string fontAssetPath, TMP_FontAsset fontAsset)
        {
            Paths.Add(fontAssetPath);
            FallBacksCounts.Add(fontAsset.fallbackFontAssetTable.Count());
            Fallbacks.AddRange(fontAsset.fallbackFontAssetTable.Select(f => AssetDatabase.GetAssetPath(f)).ToList());
        }
        public List<TMP_FontAsset> GetFallbackList(int index)
            => Fallbacks
                .Skip(FallBacksCounts.Take(index).Sum())
                .Take(FallBacksCounts[index])
                .Select(path => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path))
                .ToList();
    }
}
#endif
