using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// シンプルで高速な byte[] 用のストリーム暗号（AES-CTR 実装）ユーティリティ。
/// AES のブロック暗号をカウンタモードで使い、ストリーム暗号として動作する。
/// </summary>
public static class AesCtrCipher
{
    private const int IvSize = 16;  // AES-CTR カウンタブロックのサイズ
    private const int MacSize = 32; // HMAC-SHA256 のサイズ（32 バイト）

    /// <summary>暗号化。保存のたびにランダムな IV を生成し、[IV(16)] + [暗号文] + [HMAC(32)] の形式で返す（Encrypt-then-MAC）</summary>
    public static byte[] Encrypt(byte[] data)
    {
        byte[] iv = GenerateRandomCounter();
        byte[] ciphertext = TransformAesCtr(data, iv);

        byte[] result = new byte[IvSize + ciphertext.Length + MacSize];
        Buffer.BlockCopy(iv, 0, result, 0, IvSize);
        Buffer.BlockCopy(ciphertext, 0, result, IvSize, ciphertext.Length);

        byte[] mac = ComputeHmac(result, 0, IvSize + ciphertext.Length);
        Buffer.BlockCopy(mac, 0, result, IvSize + ciphertext.Length, MacSize);

        return result;
    }

    /// <summary>復号化の試み。HMAC を検証してから復号化する（verify-then-decrypt）</summary>
    /// <remarks>データ形式: [IV(16)] + [暗号文] + [HMAC(32)]</remarks>
    public static bool TryDecrypt(byte[] data, out byte[] output)
    {
        output = null;
        if (data == null || data.Length < IvSize + MacSize) return false;

        int ciphertextLength = data.Length - IvSize - MacSize;
        byte[] expectedMac = ComputeHmac(data, 0, IvSize + ciphertextLength);

        byte[] actualMac = new byte[MacSize];
        Buffer.BlockCopy(data, IvSize + ciphertextLength, actualMac, 0, MacSize);

        if (!CryptographicOperations.FixedTimeEquals(expectedMac, actualMac)) // 定数時間比較で MAC を検証
        {
            Debug.LogError("データが破損しています");
            return false;
        }

        byte[] iv = new byte[IvSize];
        Buffer.BlockCopy(data, 0, iv, 0, IvSize);

        byte[] ciphertext = new byte[ciphertextLength];
        Buffer.BlockCopy(data, IvSize, ciphertext, 0, ciphertextLength);

        output = TransformAesCtr(ciphertext, iv);
        return true;
    }


    /// <summary>暗号文に対して HMAC-SHA256 を計算。認証キーは暗号化キーとは独立して派生させる。</summary>
    private static byte[] ComputeHmac(byte[] data, int offset, int count)
    {
        using (var hmac = new HMACSHA256(GetAuthKey()))
        {
            byte[] segment = new byte[count];
            Buffer.BlockCopy(data, offset, segment, 0, count);
            return hmac.ComputeHash(segment);
        }
    }
    /// <summary>暗号化キーから HMAC 用の認証キーを派生させる（鍵の使い回し防止）</summary>
    private static byte[] GetAuthKey()
    {
        using (var hmac = new HMACSHA256(GeneratedEncryptionKey.GetKey()))
            return hmac.ComputeHash(Encoding.UTF8.GetBytes("AesCtrCipher.AuthKey.v1"));
    }

    /// <summary>
    /// byte[] の暗号化 兼 複合化
    /// 指定したキーと 16 バイトのカウンタブロックで data を XOR ベースのストリーム暗号で変換する。
    /// </summary>
    /// <param name="data">暗号化または複合化対象</param>
    /// <param name="iv">初期カウンタブロック（16 バイト）</param>
    /// <returns>変換後のバイト配列（新しい配列）</returns>
    private static byte[] TransformAesCtr(byte[] data, byte[] iv)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (iv == null) throw new ArgumentNullException(nameof(iv));

        byte[] output = new byte[data.Length];

        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.ECB; // CTR の keystream 用にブロック暗号を直接使う
            aes.Padding = PaddingMode.None;
            aes.Key = GeneratedEncryptionKey.GetKey();

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] counter = new byte[16];
                Buffer.BlockCopy(iv, 0, counter, 0, 16);

                int blockCount = (data.Length + 15) / 16;
                byte[] keystreamBlock = new byte[16];

                for (int i = 0; i < blockCount; i++)
                {
                    // カウンタを暗号化してキーストリームを生成する
                    encryptor.TransformBlock(counter, 0, 16, keystreamBlock, 0);

                    int offset = i * 16;
                    int remaining = Math.Min(16, data.Length - offset);

                    for (int j = 0; j < remaining; j++)
                    {
                        output[offset + j] = (byte)(data[offset + j] ^ keystreamBlock[j]);
                    }

                    IncrementCounter(counter);
                }
            }
        }

        return output;
    }

    /// <summary>counter の下位 8 byte をインクリメント</summary>
    private static void IncrementCounter(byte[] counter)
    {
        for (int i = 15; i >= 8; i--) if (++counter[i] != 0) break;
    }
    /// <summary>新しいランダムな 16 バイトカウンタブロックを生成</summary>
    public static byte[] GenerateRandomCounter()
    {
        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(iv);
        return iv;
    }
    /// <summary>byte[] から一意の byte[] を生成</summary>
    public static byte[] GenerateHash(byte[] input, int count = 16)
    {
        using (var sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(input);
            byte[] counter = new byte[count];
            Buffer.BlockCopy(hash, 0, counter, 0, count);
            return counter;
        }
    }
}
