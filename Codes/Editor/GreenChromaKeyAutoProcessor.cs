using UnityEngine;
using UnityEditor;
using System.IO;

public class GreenChromaKeyAutoProcessor : AssetPostprocessor
{
    // --- 設定定数 ---
    private const float ColorThreshold = 0.4f;   // 色の許容範囲（小さいほど厳密）
    private const float Smoothness = 0.1f;       // 境界の滑らかさ
    private const float DespillFactor = 1.0f;    // デスピル（色被り除去）の強度
    private const int ErosionIterations = 1;     // 形態学処理（収縮）の回数。0で無効
    private const int FeatherRadius = 1;         // フェザリング（ぼかし）の半径

    private const string Keyword = "chromakey_green";

    // AssetPostprocessorのコールバック
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            // Keyword で終わるファイルのみ対象
            if (path.Contains(Keyword, System.StringComparison.OrdinalIgnoreCase))
            {
                // インポート直後に処理を行うとロックされる可能性があるため、遅延実行する
                string processingPath = path;
                EditorApplication.delayCall += () => ProcessImage(processingPath);
            }
        }
    }

    private static void ProcessImage(string filePath)
    {
        if (!File.Exists(filePath)) return;

        Debug.Log($"[ChromaKey] Processing: {filePath}");

        // 1. 画像データの読み込み (TextureImporterを経由せず生データを読む)
        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);

        // 生データをロード。これによりRead/Write Enabledの状態になる
        if (!texture.LoadImage(fileData))
        {
            Debug.LogError("Failed to load image data.");
            return;
        }

        int width = texture.width;
        int height = texture.height;
        Color[] pixels = texture.GetPixels();

        // 2. 画像処理パイプライン

        // Step A: クロマキー & デスピル
        ApplyChromaKeyAndDespill(pixels, width, height);

        // Step B: 形態学処理 (Erosion/収縮) - エッジのゴミ取り
        if (ErosionIterations > 0)
        {
            pixels = ApplyErosion(pixels, width, height, ErosionIterations);
        }

        // Step C: フェザリング (アルファチャンネルのぼかし)
        if (FeatherRadius > 0)
        {
            pixels = ApplyFeathering(pixels, width, height, FeatherRadius);
        }

        // 3. テクスチャに書き戻し
        Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        newTexture.SetPixels(pixels);
        newTexture.Apply();

        // 4. PNGとして保存
        byte[] pngBytes = newTexture.EncodeToPNG();
        string directory = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileName(filePath);

        // Keyword を取り除く
        string newFileName = fileName.Replace(Keyword, "");
        string newPath = Path.Combine(directory, newFileName);

        File.WriteAllBytes(newPath, pngBytes);
        Debug.Log($"[ChromaKey] Saved: {newPath}");

        // 5. メモリ解放
        Object.DestroyImmediate(texture);
        Object.DestroyImmediate(newTexture);

        // 6. 元ファイルの削除とAssetDatabaseの更新
        AssetDatabase.DeleteAsset(filePath); // 元ファイルを削除
        AssetDatabase.Refresh();             // 新しいファイルを認識させる
    }

    // --- 画像処理ロジック ---

    private static void ApplyChromaKeyAndDespill(Color[] pixels, int width, int height)
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];

            // 色距離の計算 (単純なRGB距離)
            float distance = Vector3.Distance(
                new Vector3(pixel.r, pixel.g, pixel.b),
                new Vector3(0f, 1f, 0f)
            );

            // アルファ値の決定
            float alpha = 1.0f;
            if (distance < ColorThreshold)
            {
                alpha = 0f;
            }
            else if (distance < ColorThreshold + Smoothness)
            {
                alpha = (distance - ColorThreshold) / Smoothness;
            }

            // デスピル処理 (緑色の成分を抑制する)
            // 緑色が強く、かつ半透明でない場合でも、反射している緑を抑える
            if (pixel.g > pixel.r && pixel.g > pixel.b)
            {
                // シンプルなデスピル: Gを RとBの平均または最大値に近づける
                float maxRB = Mathf.Max(pixel.r, pixel.b);
                float spillAmount = pixel.g - maxRB;
                if (spillAmount > 0)
                {
                    pixel.g -= spillAmount * DespillFactor;
                }
            }
            // (青バックの場合は同様に青成分を抑制するロジックが必要)

            pixel.a = alpha;
            pixels[i] = pixel;
        }
    }

    // 形態学処理：収縮 (Erosion)
    // 周囲に透明なピクセルがあれば、自分も透明にする
    private static Color[] ApplyErosion(Color[] sourcePixels, int width, int height, int iterations)
    {
        Color[] currentPixels = sourcePixels;
        Color[] bufferPixels = new Color[sourcePixels.Length];

        for (int k = 0; k < iterations; k++)
        {
            System.Array.Copy(currentPixels, bufferPixels, currentPixels.Length);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (currentPixels[index].a <= 0.01f) continue; // 既に透明なら無視

                    // 上下左右をチェック
                    bool shouldErode = false;
                    if (x > 0 && currentPixels[index - 1].a < 0.5f) shouldErode = true;
                    else if (x < width - 1 && currentPixels[index + 1].a < 0.5f) shouldErode = true;
                    else if (y > 0 && currentPixels[index - width].a < 0.5f) shouldErode = true;
                    else if (y < height - 1 && currentPixels[index + width].a < 0.5f) shouldErode = true;

                    if (shouldErode)
                    {
                        bufferPixels[index].a = 0f;
                    }
                }
            }
            // バッファを現在値として更新
            System.Array.Copy(bufferPixels, currentPixels, currentPixels.Length);
        }
        return currentPixels;
    }

    // フェザリング：アルファチャンネルのボックスブラー
    private static Color[] ApplyFeathering(Color[] sourcePixels, int width, int height, int radius)
    {
        Color[] resultPixels = new Color[sourcePixels.Length];
        System.Array.Copy(sourcePixels, resultPixels, sourcePixels.Length);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                // 透明領域の真ん中ならスキップして高速化
                if (sourcePixels[index].a <= 0f) continue;

                float alphaSum = 0f;
                int count = 0;

                // 近傍ピクセルの平均を取る
                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        int ny = y + ky;
                        int nx = x + kx;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            alphaSum += sourcePixels[ny * width + nx].a;
                            count++;
                        }
                    }
                }

                resultPixels[index].a = alphaSum / count;
            }
        }
        return resultPixels;
    }
}