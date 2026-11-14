using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Art/PP_Interiors 아래로 들어오는 모든 텍스처에 픽셀아트용 임포트 설정을
/// 자동으로 적용한다. 수동으로 Inspector를 만질 필요가 없다.
///
/// 적용 항목:
///   - Texture Type      : Sprite (2D and UI)
///   - Pixels Per Unit   : 16   (16px 타일 1칸 = Grid 1셀)
///   - Filter Mode       : Point (no filter)   ← 도트가 뭉개지지 않게
///   - Compression       : None                ← 색 깨짐 방지
///   - Max Size          : 4096                ← 원본 축소 방지
///   - Mesh Type         : Full Rect           ← 타일 가장자리 잘림 방지
///   - Mip Maps          : off
///   - Sprite Mode       : 16의 배수 크기면 Multiple, 아니면 Single
/// </summary>
public class PixelArtImportSettings : AssetPostprocessor
{
    public const string Root = "Assets/Art/PP_Interiors";
    public const int PixelsPerUnit = 16;
    public const int Cell = 16;
    public const int MaxTextureSize = 4096;

    static bool InScope(string path)
    {
        return path.Replace('\\', '/').StartsWith(Root + "/");
    }

    void OnPreprocessTexture()
    {
        if (!InScope(assetPath)) return;

        var ti = (TextureImporter)assetImporter;

        ti.textureType = TextureImporterType.Sprite;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.maxTextureSize = MaxTextureSize;
        ti.sRGBTexture = true;

        // 가로세로가 모두 16의 배수이고 한 칸보다 크면 타일시트로 본다.
        bool isSheet = false;
        int w, h;
        if (TryGetPngSize(assetPath, out w, out h))
            isSheet = (w % Cell == 0) && (h % Cell == 0) && (w > Cell || h > Cell);

        ti.spriteImportMode = isSheet ? SpriteImportMode.Multiple : SpriteImportMode.Single;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spritePixelsPerUnit = PixelsPerUnit;
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteExtrude = 1;
        s.spriteGenerateFallbackPhysicsShape = false;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.filterMode = FilterMode.Point;
        s.mipmapEnabled = false;
        ti.SetTextureSettings(s);

        var platform = ti.GetDefaultPlatformTextureSettings();
        platform.maxTextureSize = MaxTextureSize;
        platform.textureCompression = TextureImporterCompression.Uncompressed;
        platform.format = TextureImporterFormat.Automatic;
        ti.SetPlatformTextureSettings(platform);
    }

    /// <summary>PNG 헤더(IHDR)에서 가로/세로를 읽는다. 텍스처를 로드하지 않아도 된다.</summary>
    public static bool TryGetPngSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            using (var fs = File.OpenRead(path))
            {
                var b = new byte[24];
                if (fs.Read(b, 0, 24) < 24) return false;
                if (b[0] != 0x89 || b[1] != 0x50 || b[2] != 0x4E || b[3] != 0x47) return false;
                width = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                height = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return width > 0 && height > 0;
            }
        }
        catch
        {
            return false;
        }
    }

    [MenuItem("Tools/Pixel Art/1. Reapply Import Settings")]
    public static void ReapplyAll()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (var g in guids)
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(g), ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        Debug.Log("[PixelArt] 임포트 설정 재적용 완료: 텍스처 " + guids.Length + "개");
    }
}
