#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using Yamara;
using UnityEditorInternal;
using System.Text.RegularExpressions;

namespace Addressables
{
    [CreateAssetMenu(menuName = "Addressables/Create AddressDefineCreator", fileName = "AddressDefineCreator")]
    public class AddressDefineCreator : ScriptableObject
    {
        private static string DirectoryPath = "Assets/AddressableAssetsData/";
        private static string ClassName = "AddressDefine";

        public const int AddressWordFrequencyThreshold = 2;
        public const int AddressWordLengthThreshold = 3;
        private static readonly char[] AddressWordSeparators =
        {
            ' ', '!', '\"', '#', '$',
            '%', '&', '\'', '(', ')',
            '-', '=', '^',  '~', '\\',
            '|', '[', '{',  '@', '`',
            ']', '}', ':',  '*', ';',
            '+', '/', '?',  '.', '>',
            ',', '<',
        };

        public AddressableAssetSettings Settings;

        public List<string> IgnoreGroups = new();
        public List<string> IgnoreAddressWords = new();

        public List<string> MirroringToAddressablesGroups = new();
        public List<string> MirroringToAddressFrequentWords = new();

        [MenuItem("Tools/Addressables/Open Address Define Creator")]
        private static void OpenAddressDefineCreator()
        {
            var guid = AssetDatabase.FindAssets("t:" + nameof(AddressDefineCreator)).FirstOrDefault();
            AddressDefineCreator instance;
            if (guid == null)
            {
                var fullPath = EditorUtility.SaveFilePanel(string.Empty, DirectoryPath, nameof(AddressDefineCreator), "asset");
                instance = CreateInstance(typeof(AddressDefineCreator)) as AddressDefineCreator;
                AssetDatabase.CreateAsset(instance, FileUtil.GetProjectRelativePath(fullPath));
                guid = AssetDatabase.FindAssets("t:" + nameof(AddressDefineCreator)).First();
            }
            else instance = AssetDatabase.LoadAssetAtPath<AddressDefineCreator>(AssetDatabase.GUIDToAssetPath(guid));;
            Selection.activeObject = instance;
        }

        public AddressableAssetSettings FindAddressableAssetSettings()
        {
            var settingsGuid = AssetDatabase.FindAssets("t:" + nameof(AddressableAssetSettings)).FirstOrDefault();
            var settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuid);
            return AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(settingsPath);
        }

        public void UpdateAddressDefineFile()
        {
            var assets = new List<AddressableAssetEntry>();
            Settings.GetAllAssets(assets, false);

            var assetDict = new Dictionary<AddressableAssetGroup, List<AddressableAssetEntry>>();

            var ignoreGroups = this.IgnoreGroups.ToHashSet();
            foreach (var group in Settings.groups)
            {
                if (ignoreGroups.Contains(group.Name)) continue;
                assetDict.Add(group, new());
            }
            foreach (var asset in assets)
            {
                if (ignoreGroups.Contains(asset.parentGroup.Name)) continue;
                assetDict[asset.parentGroup].Add(asset);
            }

            CreateAddressDefineFile(assetDict);
        }

        private void CreateAddressDefineFile(Dictionary<AddressableAssetGroup, List<AddressableAssetEntry>> assetDict)
        {
            var builder = new ScriptBuilder().Namespace(nameof(Addressables)).OpenBrace();
            builder.Append("public static class ").AppendLine(ClassName).OpenBrace();

            foreach (var pair in assetDict)
            {
                builder.Append("public static class ").AppendLine(pair.Key.Name, true).OpenBrace();
                foreach (var asset in pair.Value)
                {
                    if (IgnoreAddressWords.Any(ignoreWord => asset.address.Contains(ignoreWord))) continue;

                    builder.Append("public const string ")
                        .Append(asset.address, true).Append(" = ").AppendLine($"\"{asset.address}\";");
                }
                builder.CloseBrace();
            }

            builder.CloseBrace().CloseBrace();
            builder.WriteToFile(DirectoryPath, ClassName);
        }

        public void UpdateAddressablesFrequentWords()
        {
            var assets = new List<AddressableAssetEntry>();
            Settings.GetAllAssets(assets, false);
            var dict = new Dictionary<string, int>();
            var allWords = assets.Select(asset => asset.address.Split(AddressWordSeparators)).SelectMany(splittedWords => splittedWords);
            foreach (var word in allWords.Select(s => Regex.Replace(s, @"[0-9]", "")).Where(s => s.Length >= AddressWordLengthThreshold))
            {
                if (!dict.ContainsKey(word)) dict.Add(word, 0);
                dict[word]++;
            }
            MirroringToAddressFrequentWords = dict
                .Where(p => p.Value >= AddressWordFrequencyThreshold)
                .OrderByDescending(p => p.Value).Select(p => p.Key).ToList();
        }
    }

    [CustomEditor(typeof(AddressDefineCreator))]
    public class AddressDefineCreatorEditor : Editor
    {
        private ReorderableList _ignoreList, _ignoreAddressWordList, _mirrorGroupList, _frequentWordList, _separatorList;

        public void OnEnable()
        {
            var creator = target as AddressDefineCreator;
            creator.Settings ??= creator.FindAddressableAssetSettings();
            creator.MirroringToAddressablesGroups = creator.Settings.groups.Select(g => g.Name).ToList();

            _ignoreList = new ReorderableList(creator.IgnoreGroups, typeof(string));
            _ignoreList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, "Ignore Groups");
            _ignoreList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                creator.IgnoreGroups[index] = EditorGUI.TextField(rect, creator.IgnoreGroups[index]);
            };

            _ignoreAddressWordList = new ReorderableList(creator.IgnoreAddressWords, typeof(string));
            _ignoreAddressWordList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, "Ignore Address Words");
            _ignoreAddressWordList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                creator.IgnoreAddressWords[index] = EditorGUI.TextField(rect, creator.IgnoreAddressWords[index]);
            };

            _mirrorGroupList = new ReorderableList(creator.MirroringToAddressablesGroups, typeof(string), false, true, false, false);
            _mirrorGroupList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, "Addressables Groups");
            _mirrorGroupList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                EditorGUI.TextField(rect, creator.MirroringToAddressablesGroups[index]);
            };

            _frequentWordList = new ReorderableList(creator.MirroringToAddressFrequentWords, typeof(string), false, true, false, false);
            _frequentWordList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, "Address Frequent Words");
            _frequentWordList.drawElementCallback += (rect, index, isActive, isFocused) =>
            {
                EditorGUI.TextField(rect, creator.MirroringToAddressFrequentWords[index]);
            };
        }

        public override void OnInspectorGUI()
        {
            var creator = target as AddressDefineCreator;
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal(EditorStyles.whiteLargeLabel);
            GUILayout.Label("Addressable Asset Settings");
            creator.Settings = EditorGUILayout.ObjectField(creator.Settings, typeof(AddressableAssetSettings), true) as AddressableAssetSettings;
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Build Addressables\nAnd\nUpdate Address Define"))
            {
                AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
                AddressableAssetSettings.BuildPlayerContent();
                creator.UpdateAddressDefineFile();
            }
            if (GUILayout.Button("\nUpdate Address Define\n")) creator.UpdateAddressDefineFile();

            GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            GUILayout.Label("Ignore Settings", EditorStyles.whiteBoldLabel);

            _ignoreList.DoLayoutList();
            _ignoreAddressWordList.DoLayoutList();

            GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
            GUILayout.Label("Mirrored Informations", EditorStyles.whiteBoldLabel);

            _mirrorGroupList.DoLayoutList();

            if (GUILayout.Button("Update Address Frequent Words"))
            {
                creator.UpdateAddressablesFrequentWords();
                _frequentWordList = new ReorderableList(creator.MirroringToAddressFrequentWords, typeof(string), false, true, false, false);
                _frequentWordList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, "Address Frequent Words");
                _frequentWordList.drawElementCallback += (rect, index, isActive, isFocused) =>
                {
                    EditorGUI.TextField(rect, creator.MirroringToAddressFrequentWords[index]);
                };
            }

            _frequentWordList.DoLayoutList();

            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(creator);
        }
    }
}
#endif
