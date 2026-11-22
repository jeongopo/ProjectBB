using System.Collections.Generic;
using System.IO;
using System.Linq;
using GamePlay;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 선택한 스프라이트(또는 시트)마다 가구 프리팹을 만든다.
/// 루트는 점유 영역 아래쪽 중앙, 스프라이트는 Visual 자식으로 두어 피벗과 상관없이 칸에 맞춘다.
///
/// 사용법: Project 창에서 스프라이트/시트 선택 → Tools / Furniture / Create Prefabs From Selection
/// </summary>
public static class FurniturePrefabBuilder
{
    private const string OutputFolder = "Assets/Objects/Furniture";
    private const string SortingLayerName = "Object";
    private const float SizeTolerance = 0.01f;

    [MenuItem("Tools/Furniture/Create Prefabs From Selection")]
    public static void Build()
    {
        List<Sprite> sprites = CollectSprites();
        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("Furniture", "Project 창에서 스프라이트나 스프라이트 시트를 먼저 선택하세요.", "확인");
            return;
        }

        EnsureFolder(OutputFolder);

        int created = 0;
        foreach (Sprite sprite in sprites)
        {
            string path = $"{OutputFolder}/Furn_{sprite.name.Replace(' ', '_')}.prefab";
            if (File.Exists(path))
            {
                Debug.LogWarning($"[Furniture] 이미 있어 건너뜀: {path}");
                continue;
            }

            CreatePrefab(sprite, path);
            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Furniture] 가구 프리팹 {created}개 생성 → {OutputFolder}");
    }

    private static List<Sprite> CollectSprites()
    {
        var result = new List<Sprite>();
        foreach (Object selected in Selection.objects)
        {
            if (selected is Sprite sprite)
            {
                result.Add(sprite);
            }
            else if (selected is Texture2D texture)
            {
                string path = AssetDatabase.GetAssetPath(texture);
                result.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>());
            }
        }

        return result.Distinct().ToList();
    }

    private static void CreatePrefab(Sprite sprite, string path)
    {
        var root = new GameObject("Furn_" + sprite.name);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        Bounds bounds = sprite.bounds;
        visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);

        var spriteRenderer = visual.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = SortingLayerName;

        var size = new Vector2Int(
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.x - SizeTolerance)),
            Mathf.Max(1, Mathf.CeilToInt(bounds.size.y - SizeTolerance)));

        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(size.x, size.y);
        collider.offset = new Vector2(0f, size.y * 0.5f);

        var furniture = root.AddComponent<Furniture>();
        furniture.Setup(sprite.name, size);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
