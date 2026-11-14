using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 타일시트를 격자로 잘라 스프라이트를 만든다.
/// Sprite Editor의 "Grid By Cell Size"와 같은 동작이되,
/// 완전히 투명한 칸은 건너뛴다 (쓸모없는 빈 스프라이트가 안 생김).
///
/// 메뉴: Tools / Pixel Art
/// </summary>
public static class PixelArtSlicer
{
    // ---------------------------------------------------------------- 메뉴

    [MenuItem("Tools/Pixel Art/2. Slice PP Interiors Pack")]
    public static void SlicePack()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PixelArtImportSettings.Root });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[PixelArt] " + PixelArtImportSettings.Root + " 에서 텍스처를 찾지 못했습니다.");
            return;
        }

        int totalSprites = 0;
        int slicedFiles = 0;
        var skipped = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string file = Path.GetFileNameWithoutExtension(path);

                EditorUtility.DisplayProgressBar(
                    "Slicing sprite sheets", file, (float)i / guids.Length);

                // Torch 1/2 는 16x32 스프라이트 8프레임 애니메이션이다.
                // Torch Light 는 68x34 라 격자에 안 맞으므로 Single 로 둔다.
                int cellW = 16;
                int cellH = 16;
                if (file.StartsWith("Torch ") && !file.StartsWith("Torch Light"))
                    cellH = 32;

                int n = Slice(path, cellW, cellH);
                if (n > 0)
                {
                    totalSprites += n;
                    slicedFiles++;
                }
                else
                {
                    skipped.Add(file);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log(string.Format(
            "[PixelArt] 슬라이스 완료 — 파일 {0}개, 스프라이트 {1}개 생성. 건너뜀: {2}",
            slicedFiles, totalSprites,
            skipped.Count == 0 ? "없음" : string.Join(", ", skipped.ToArray())));
    }

    [MenuItem("Tools/Pixel Art/Slice Selected (16x16)")]
    public static void SliceSelected16() { SliceSelection(16, 16); }

    [MenuItem("Tools/Pixel Art/Slice Selected (16x32)")]
    public static void SliceSelected16x32() { SliceSelection(16, 32); }

    [MenuItem("Tools/Pixel Art/Slice Selected (32x32)")]
    public static void SliceSelected32() { SliceSelection(32, 32); }

    static void SliceSelection(int cw, int ch)
    {
        var textures = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);
        if (textures.Length == 0)
        {
            Debug.LogWarning("[PixelArt] Project 창에서 텍스처를 먼저 선택하세요.");
            return;
        }

        int total = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var tex in textures)
                total += Slice(AssetDatabase.GetAssetPath(tex), cw, ch);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        Debug.Log(string.Format("[PixelArt] 선택 항목 슬라이스 완료 — 스프라이트 {0}개 ({1}x{2})",
            total, cw, ch));
    }

    // ---------------------------------------------------------------- 핵심

    /// <summary>한 장을 cw x ch 격자로 자른다. 만들어진 스프라이트 수를 돌려준다.</summary>
    public static int Slice(string assetPath, int cw, int ch)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti == null) return 0;

        // 임포트 설정과 무관하게 픽셀을 읽기 위해 원본 파일을 직접 디코드한다.
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(assetPath)))
        {
            Object.DestroyImmediate(tex);
            return 0;
        }

        int W = tex.width;
        int H = tex.height;
        if (W % cw != 0 || H % ch != 0)
        {
            Debug.LogWarning(string.Format(
                "[PixelArt] 건너뜀 (격자에 안 맞음): {0} — {1}x{2}, 셀 {3}x{4}",
                Path.GetFileName(assetPath), W, H, cw, ch));
            Object.DestroyImmediate(tex);
            return 0;
        }

        var pixels = tex.GetPixels32();
        Object.DestroyImmediate(tex);

        string baseName = Path.GetFileNameWithoutExtension(assetPath).Replace(' ', '_');
        int cols = W / cw;
        int rows = H / ch;

        var rects = new List<SpriteRect>(cols * rows);

        // Unity 텍스처 좌표는 좌하단이 원점이다. 이름은 사람이 보기 편하게
        // 좌상단 기준 행 번호를 쓴다.
        for (int ry = 0; ry < rows; ry++)
        {
            for (int cx = 0; cx < cols; cx++)
            {
                int x0 = cx * cw;
                int y0 = ry * ch;

                if (IsEmpty(pixels, W, x0, y0, cw, ch)) continue;

                int rowFromTop = rows - 1 - ry;
                rects.Add(new SpriteRect
                {
                    name = string.Format("{0}_{1:D3}_{2:D3}", baseName, rowFromTop, cx),
                    rect = new Rect(x0, y0, cw, ch),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                    spriteID = GUID.Generate()
                });
            }
        }

        if (rects.Count == 0) return 0;

        ti.spriteImportMode = SpriteImportMode.Multiple;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(ti);
        provider.InitSpriteEditorDataProvider();
        provider.SetSpriteRects(rects.ToArray());

        var nameIdProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
        {
            var pairs = new List<SpriteNameFileIdPair>(rects.Count);
            foreach (var r in rects)
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            nameIdProvider.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        return rects.Count;
    }

    static bool IsEmpty(Color32[] pixels, int texWidth, int x0, int y0, int cw, int ch)
    {
        for (int y = y0; y < y0 + ch; y++)
        {
            int row = y * texWidth;
            for (int x = x0; x < x0 + cw; x++)
            {
                if (pixels[row + x].a != 0) return false;
            }
        }
        return true;
    }
}
