using UnityEngine;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

/// <summary>
/// 暗号化に使用するキーを生成。メニューから更新
/// </summary>
public class GeneratedEncryptionKey
{
#if UNITY_EDITOR
    [MenuItem("Tools/" + nameof(UpdateEncryptionKey))]
    private static void UpdateEncryptionKey()
    {
        byte[] bytes = Enumerable.Range(0, 2)
            .SelectMany(_ => Guid.NewGuid().ToByteArray()).ToArray();

        var scriptGuids = AssetDatabase.FindAssets($"{nameof(GeneratedEncryptionKey)} t:Script");
        string scriptPath = null;
        foreach (var guid in scriptGuids)
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith($"{nameof(GeneratedEncryptionKey)}.cs", StringComparison.Ordinal))
            {
                scriptPath = p;
                break;
            }
        }

        if (string.IsNullOrEmpty(scriptPath))
        {
            Debug.LogError($"{nameof(GeneratedEncryptionKey)}.cs が見つかりませんでした。");
            return;
        }

        var literal = BuildByteArrayLiteral(bytes);
        var source = File.ReadAllText(scriptPath, Encoding.UTF8);
        var pattern = @"private static readonly byte\[\] KeyFactors\s*=\s*new byte\[\]\s*\{[^}]*\};";
        var replacement = $"private static readonly byte[] KeyFactors = new byte[] {{{literal}}};";
        var newSource = Regex.Replace(source, pattern, replacement, RegexOptions.Singleline);

        File.WriteAllText(scriptPath, newSource, new UTF8Encoding(false));
        AssetDatabase.ImportAsset(scriptPath);

        Debug.Log($"{nameof(GeneratedEncryptionKey)} のキーを更新しました。");
    }

    private static string BuildByteArrayLiteral(byte[] data)
    {
        var sb = new StringBuilder();
        const int perLine = 16;
        for (int i = 0; i < data.Length; i++)
        {
            if (i % perLine == 0)
            {
                if (i != 0) sb.AppendLine();
                sb.Append("\n        ");
            }
            sb.AppendFormat("0x{0:X2}", data[i]);
            if (i != data.Length - 1) sb.Append(", ");
        }
        sb.Append("\n    ");
        return sb.ToString();
    }
#endif

    private static readonly byte[] KeyFactors = new byte[] {
        0x8A, 0x92, 0xAC, 0x16, 0x1E, 0xDF, 0x95, 0x44, 0x8B, 0xEC, 0xA8, 0x92, 0x8F, 0x12, 0x26, 0x61, 

        0xDA, 0x77, 0x16, 0x59, 0x89, 0x58, 0xAB, 0x48, 0xB6, 0x4F, 0xAF, 0x71, 0x37, 0x6E, 0xBB, 0x82
    };

    /// <summary>暗号化/復号化用の鍵を KeyFactors の値から生成。簡素なXOR演算で難読化</summary>
    public static byte[] GetKey(int length = 32)
    {
        if (length <= 0) return Array.Empty<byte>();

        byte[] key = new byte[length];
        int factorsLen = KeyFactors.Length;

        for (int i = 0; i < length; i++)
        {
            byte factor = KeyFactors[i % factorsLen]; // 基本となるバイトを取得
            byte other = KeyFactors[(i + 7) % factorsLen]; // 別のインデックスのバイトを取得 (適当な素数7でずらす)
            key[i] = (byte)(factor ^ other ^ i ^ 0x5A); // インデックス値(i)を加味して全てXOR。適当な定数を噛ませi=0の時の単純化を防ぐ
        }

        return key;
    }
}
