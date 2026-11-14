using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 선택한 스프라이트 시트에서 Tile 에셋과 Tile Palette를 한 번에 만든다.
///
/// Tile Palette 창에 시트를 드래그하면 스프라이트 수만큼 .asset 파일이 쏟아지는데
/// (all walls = 4,346개 파일), 여기서는 시트 한 장당 .asset 파일 하나에
/// 모든 Tile을 서브에셋으로 넣는다. git 부담이 크게 줄어든다.
///
/// 팔레트 배치는 원본 시트의 격자 위치를 그대로 따라간다.
///
/// 사용법: Project 창에서 시트(PNG)를 선택 → Tools / Pixel Art / 3. Create Tiles + Palette
/// </summary>
public static class PixelArtTileBuilder
{
    const string TilesRoot = "Assets/Art/PP_Interiors/Tiles";
    const string PalettesRoot = "Assets/Art/PP_Interiors/Palettes";

    // PixelArtSlicer가 만든 이름 규칙: <시트이름>_<행>_<열>
    static readonly Regex NamePattern = new Regex(@"_(\d+)_(\d+)$", RegexOptions.Compiled);

    [MenuItem("Tools/Pixel Art/3. Create Tiles + Palette From Selection")]
    public static void Build()
    {
        var textures = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);
        if (textures.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Pixel Art",
                "Project 창에서 스프라이트 시트(PNG)를 먼저 선택하세요.\n\n" +
                "예: Assets/Art/PP_Interiors/Floors/all floors.png",
                "확인");
            return;
        }

        var made = new List<string>();

        try
        {
            for (int i = 0; i < textures.Length; i++)
            {
                EditorUtility.DisplayProgressBar(
                    "Creating tiles", textures[i].name, (float)i / textures.Length);

                string result = BuildOne(AssetDatabase.GetAssetPath(textures[i]));
                if (result != null) made.Add(result);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        if (made.Count == 0)
        {
            Debug.LogWarning("[PixelArt] 만들어진 팔레트가 없습니다. 시트가 슬라이스되어 있는지 확인하세요 " +
                             "(Tools > Pixel Art > 2. Slice PP Interiors Pack).");
            return;
        }

        Debug.Log("[PixelArt] 팔레트 생성 완료\n" + string.Join("\n", made.ToArray()));

        // 마지막으로 만든 팔레트를 Project 창에서 선택해 보여준다.
        var last = AssetDatabase.LoadAssetAtPath<GameObject>(
            made[made.Count - 1].Split('→').Last().Trim());
        if (last != null) EditorGUIUtility.PingObject(last);
    }

    static string BuildOne(string texturePath)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
                                   .OfType<Sprite>()
                                   .ToArray();
        if (sprites.Length == 0)
        {
            Debug.LogWarning("[PixelArt] 스프라이트가 없어 건너뜀: " + Path.GetFileName(texturePath) +
                             " — 먼저 슬라이스하세요.");
            return null;
        }

        string safeName = Path.GetFileNameWithoutExtension(texturePath).Replace(' ', '_');

        // ---------------------------------------------------------- Tile 에셋
        EnsureFolder(TilesRoot);
        string tilesPath = TilesRoot + "/" + safeName + "_Tiles.asset";
        AssetDatabase.DeleteAsset(tilesPath);

        var entries = new List<Entry>(sprites.Length);
        bool isFirst = true;

        foreach (var sprite in sprites.OrderBy(s => s.name, System.StringComparer.Ordinal))
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = sprite.name;
            tile.sprite = sprite;
            tile.color = Color.white;
            // 콜라이더가 필요한 벽/오브젝트는 나중에 Tile을 골라 Sprite로 바꾸면 된다.
            tile.colliderType = Tile.ColliderType.None;

            if (isFirst)
            {
                AssetDatabase.CreateAsset(tile, tilesPath);
                isFirst = false;
            }
            else
            {
                AssetDatabase.AddObjectToAsset(tile, tilesPath);
            }

            entries.Add(new Entry { tile = tile, cell = CellOf(sprite.name, entries.Count) });
        }

        AssetDatabase.SaveAssets();

        // ---------------------------------------------------------- 팔레트 프리팹
        EnsureFolder(PalettesRoot);
        string palettePath = PalettesRoot + "/" + safeName + "_Palette.prefab";
        AssetDatabase.DeleteAsset(palettePath);

        var root = new GameObject(safeName + "_Palette");
        var grid = root.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var layer = new GameObject("Layer1");
        layer.transform.SetParent(root.transform, false);
        var tilemap = layer.AddComponent<Tilemap>();
        layer.AddComponent<TilemapRenderer>();

        foreach (var e in entries)
            tilemap.SetTile(e.cell, e.tile);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, palettePath);
        Object.DestroyImmediate(root);

        if (prefab == null)
        {
            Debug.LogError("[PixelArt] 팔레트 프리팹 저장 실패: " + palettePath);
            return null;
        }

        // GridPalette 서브에셋이 있어야 Unity가 이 프리팹을 "타일 팔레트"로 인식한다.
        var settings = ScriptableObject.CreateInstance<GridPalette>();
        settings.name = "Palette Settings";
        settings.cellSizing = GridPalette.CellSizing.Manual;
        AssetDatabase.AddObjectToAsset(settings, prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(palettePath, ImportAssetOptions.ForceUpdate);

        return string.Format("  {0}: 타일 {1}개  →  {2}",
            Path.GetFileName(texturePath), entries.Count, palettePath);
    }

    struct Entry
    {
        public Tile tile;
        public Vector3Int cell;
    }

    /// <summary>스프라이트 이름에서 원본 시트의 격자 좌표를 복원한다.</summary>
    static Vector3Int CellOf(string spriteName, int fallbackIndex)
    {
        var m = NamePattern.Match(spriteName);
        if (m.Success)
        {
            int row = int.Parse(m.Groups[1].Value);
            int col = int.Parse(m.Groups[2].Value);
            // 이름의 행 번호는 위에서부터, 타일맵 Y는 아래에서부터 증가한다.
            return new Vector3Int(col, -row, 0);
        }

        // 이름 규칙이 다르면 한 줄로 늘어놓는다.
        return new Vector3Int(fallbackIndex, 0, 0);
    }

    static void EnsureFolder(string path)
    {
        path = path.Replace('\\', '/').TrimEnd('/');
        if (path == "Assets" || AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
