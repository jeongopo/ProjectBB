using GamePlay;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 루트에 SpriteRenderer가 직접 붙은 기존 오브젝트 프리팹(Obj_Cook_* 등)을 가구 구조로 바꾼다.
/// 루트 = 점유 영역 발치 중앙, 스프라이트 = Visual 자식, Furniture 컴포넌트 추가.
///
/// 사용법: Project 창에서 프리팹 선택 → Tools / Furniture / Convert Selected Prefabs To Furniture
/// </summary>
public static class FurniturePrefabConverter
{
    private const float SizeTolerance = 0.01f;

    [MenuItem("Tools/Furniture/Convert Selected Prefabs To Furniture")]
    public static void ConvertSelected()
    {
        int converted = 0;
        foreach (Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (!path.EndsWith(".prefab") || !Convert(path))
            {
                continue;
            }

            converted++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Furniture] 프리팹 {converted}개를 가구 구조로 변환했습니다. 씬의 인스턴스는 Furniture 우클릭 메뉴 Snap To Grid로 칸에 맞추세요.");
    }

    private static bool Convert(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.GetComponent<Furniture>() != null)
            {
                Debug.LogWarning($"[Furniture] 이미 Furniture가 있어 건너뜀: {path}");
                return false;
            }

            SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
            if (rootRenderer == null || rootRenderer.sprite == null)
            {
                Debug.LogWarning($"[Furniture] 루트에 스프라이트가 없어 건너뜀: {path}");
                return false;
            }

            Bounds bounds = rootRenderer.sprite.bounds;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, 0f);

            var visualRenderer = visual.AddComponent<SpriteRenderer>();
            EditorUtility.CopySerialized(rootRenderer, visualRenderer);
            Object.DestroyImmediate(rootRenderer);

            // 기존 장식 스프라이트 자식은 원래 루트(스프라이트 피벗) 기준이었으므로 Visual 아래로 옮겨 상대 위치를 유지한다.
            var decorations = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in root.transform)
            {
                if (child != visual.transform && child.GetComponent<SpriteRenderer>() != null)
                {
                    decorations.Add(child);
                }
            }

            foreach (Transform decoration in decorations)
            {
                decoration.SetParent(visual.transform, false);
            }

            var size = new Vector2Int(Mathf.Max(1, Mathf.CeilToInt(bounds.size.x - SizeTolerance)), 1);
            if (root.GetComponent<BoxCollider2D>() == null)
            {
                root.AddComponent<BoxCollider2D>();
            }

            var furniture = root.AddComponent<Furniture>();
            furniture.Setup(root.name, size);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
