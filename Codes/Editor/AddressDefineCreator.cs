#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using Yamara;

namespace Addressables
{
    [CreateAssetMenu(menuName = "Addressables/Create AddressDefineCreator", fileName = "AddressDefineCreator")]
    public class AddressDefineCreatorSettings : ScriptableObject
    {
        private static string DirectoryPath = "Assets/AddressableAssetsData/";
        private static string ClassName = "AddressDefine";

        [SerializeField] AddressableAssetSettings settings;

        [Header("【Button】\nBuild Addressables & Update Address Define")]
        [SerializeField]
        bool BuildAndUpdateButton = false;

        [Header("【Button】\nUpdate Address Define")]
        [SerializeField]
        bool UpdateButton = false;

        [SerializeField] List<string> ignoreGroups = new();
        [SerializeField] List<string> MirroringToAddressablesGroups = new();

        public AddressableAssetSettings FindAddressableAssetSettings()
        {
            var settingsGuid = AssetDatabase.FindAssets("t:" + nameof(AddressableAssetSettings)).FirstOrDefault();
            var settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuid);
            return AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(settingsPath);
        }

        private void OnValidate()
        {
            if (BuildAndUpdateButton)
            {
                BuildAndUpdateButton = false;
                AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
                AddressableAssetSettings.BuildPlayerContent();
                UpdateAddressDefineFile();
            }
            if (UpdateButton)
            {
                UpdateButton = false;
                UpdateAddressDefineFile();
            }

            settings ??= FindAddressableAssetSettings();
            MirroringToAddressablesGroups = settings.groups.Select(g => g.Name).ToList();
        }

        public void UpdateAddressDefineFile()
        {
            var assets = new List<AddressableAssetEntry>();
            settings.GetAllAssets(assets, false);

            var assetDict = new Dictionary<AddressableAssetGroup, List<AddressableAssetEntry>>();

            var ignoreGroups = this.ignoreGroups.ToHashSet();
            foreach (var group in settings.groups)
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
                    builder.Append("public const string ")
                        .Append(asset.address, true).Append(" = ").AppendLine($"\"{asset.address}\";");
                }
                builder.CloseBrace();
            }

            builder.CloseBrace().CloseBrace();
            builder.WriteToFile(DirectoryPath, ClassName);
        }
    }
}
#endif
