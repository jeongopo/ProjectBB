using System;
using System.Collections.Generic;
using GamePlay;
using Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TavernLayoutSetup
{
    private const string ScenePath = "Assets/Maps/TestScene.unity";
    private const string LayoutName = "주점 목업 (2안)";
    private const string ArtPath = "Assets/Resources/Art/Map/enviroment.png";
    private const string SupplyShelfName = "SupplyShelf";
    private const string LegacySupplyShelfName = "창고 선반";

    private static readonly Dictionary<string, string> LegacyNames = new Dictionary<string, string>
    {
        { "01 주방 · 보급 → 조리 → 담기", "Kitchen" },
        { "02 카운터 · 서빙", "ServingCounter" },
        { "03 홀 · 입장과 단골석", "DiningHall" },
        { LegacySupplyShelfName, SupplyShelfName },
        { "장작더미 · 임시", "FirewoodStack" },
        { "화로", "Hearth" },
        { "Obj_Cook_Cutting", "CuttingStation" },
        { "Obj_Cook_Boiling", "BoilingStation" },
        { "Obj_Cook_Plating", "PlatingStation" },
        { "Furn_enviroment_217", "SideHearth" },
        { "Obj_Hunting", "HuntingStation" },
        { "카운터 조각 1", "CounterSection_01" },
        { "카운터 조각 4", "CounterSection_02" },
        { "카운터 조각 7", "CounterSection_03" },
        { "Furn_enviroment_10", "CounterStool_01" },
        { "카운터 의자 2", "CounterStool_02" },
        { "화로 앞 특별석 · 임시", "HearthStool" },
        { "단골 테이블", "DiningTable" },
        { "단골 의자 1", "DiningChair_01" },
        { "단골 의자 2", "DiningChair_02" },
        { "단골 의자 3", "DiningChair_03" },
        { "단골 의자 4", "DiningChair_04" },
        { "출입문 · 아래쪽", "EntranceDoor" },
    };

    [MenuItem("Tools/Tavern/Place Layout 2 In TestScene")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (GameObject.Find(SupplyShelfName) == null && GameObject.Find(LegacySupplyShelfName) == null)
        {
            new Layout().Build();
        }
        RemoveLayoutWrapper();
        NormalizeLayoutNames();
        ConnectSupplies(scene);
        Debug.Log("[Tavern] 2안 목업과 주문·서빙 연결을 TestScene에 저장했습니다.");
    }

    private static void NormalizeLayoutNames()
    {
        Grid grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            throw new InvalidOperationException("주점 가구가 배치된 Grid가 없습니다.");
        }

        foreach (Transform child in grid.GetComponentsInChildren<Transform>(true))
        {
            if (LegacyNames.TryGetValue(child.name, out string name))
            {
                Rename(child.gameObject, name);
            }
        }
    }

    private static void Rename(GameObject obj, string name)
    {
        Undo.RecordObject(obj, "Rename Tavern Object");
        obj.name = name;
        PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
    }

    private static void RemoveLayoutWrapper()
    {
        GameObject wrapper = GameObject.Find(LayoutName);
        if (wrapper == null)
        {
            return;
        }

        Grid grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
        if (grid == null)
        {
            throw new InvalidOperationException("주점 가구를 옮길 Grid가 없습니다.");
        }
        while (wrapper.transform.childCount > 0)
        {
            Undo.SetTransformParent(wrapper.transform.GetChild(0), grid.transform, "Organize Tavern Furniture");
        }
        Undo.DestroyObjectImmediate(wrapper);
    }

    private static void ConnectSupplies(Scene scene)
    {
        GameObject shelf = GameObject.Find(SupplyShelfName);
        if (shelf == null)
        {
            throw new InvalidOperationException("창고 선반이 없습니다.");
        }
        if (!shelf.TryGetComponent<SupplyShelf>(out _))
        {
            Undo.AddComponent<SupplyShelf>(shelf);
        }

        PlatingComponent plating = UnityEngine.Object.FindFirstObjectByType<PlatingComponent>(FindObjectsInactive.Include);
        if (plating == null)
        {
            throw new InvalidOperationException("담기 컴포넌트가 없습니다.");
        }
        SerializedObject settings = new SerializedObject(plating);
        settings.FindProperty("grantTestIngredientsOnOpen").boolValue = false;
        settings.ApplyModifiedProperties();
        TavernDaySummarySetup.Connect();

        if (scene.isDirty && !EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("TestScene을 저장할 수 없습니다.");
        }
        Debug.Log("[Tavern] 창고 보급을 연결했습니다.");
    }

    private sealed class Layout
    {
        private readonly Dictionary<int, Sprite> _art = new Dictionary<int, Sprite>();
        private readonly HashSet<Vector3Int> _used = new HashSet<Vector3Int>();
        private readonly Tilemap _floor;

        internal Layout()
        {
            GameObject floor = GameObject.Find("Floor");
            _floor = floor != null ? floor.GetComponent<Tilemap>() : null;
            if (_floor == null)
            {
                throw new InvalidOperationException("TestScene에 Floor 타일맵이 없습니다.");
            }
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ArtPath))
            {
                if (asset is Sprite sprite && sprite.name.StartsWith("enviroment_", StringComparison.Ordinal) &&
                    int.TryParse(sprite.name.Substring("enviroment_".Length), out int index))
                {
                    _art.Add(index, sprite);
                }
            }
        }

        internal void Build()
        {
            Furniture cutting = RequireFurniture("Obj_Cook_Cutting");
            Furniture boiling = RequireFurniture("Obj_Cook_Boiling");
            Furniture plating = RequireFurniture("Obj_Cook_Plating");
            Furniture hunting = RequireFurniture("Obj_Hunting");
            Furniture shelf = RequireFurniture("Furn_enviroment_217");
            CustomerSeat existingSeat = Require<CustomerSeat>();
            CustomerSpawner spawner = Require<CustomerSpawner>();
            MainCharacter player = Require<MainCharacter>();
            Transform entrance = spawner.transform.Find("CustomerEntrance");
            if (entrance == null)
            {
                throw new InvalidOperationException("CustomerEntrance가 없습니다.");
            }

            Transform root = Require<Grid>().transform;
            Transform kitchen = NewObject("Kitchen", root);
            Transform counter = NewObject("ServingCounter", root);
            Transform hall = NewObject("DiningHall", root);

            MakeFurniture(SupplyShelfName, kitchen, -7, 3, 2, 1, 20, 2.2f).gameObject.AddComponent<SupplyShelf>();
            MakeFurniture("FirewoodStack", kitchen, -7, 2, 1, 1, 101, 1f);
            MoveFurniture(cutting, kitchen, -5, 3);
            MoveFurniture(boiling, kitchen, -1, 3);
            MoveFurniture(plating, kitchen, 2, 3);
            MoveFurniture(shelf, kitchen, 5, 4);
            Transform hearth = NewObject("Hearth", kitchen);
            hearth.position = boiling.transform.position + Vector3.up * 0.75f;
            AddVisual(hearth, 215, 2.6f, -450);
            MoveFurniture(hunting, hall, -7, -4);

            for (int x = -5; x <= 1; x += 3)
            {
                MakeFurniture($"CounterSection_{(x + 5) / 3 + 1:00}", counter, x, 0, 3, 1, 37, 3f);
            }

            Furniture chair = existingSeat.GetComponent<Furniture>();
            if (chair == null)
            {
                throw new InvalidOperationException("기존 의자에 Furniture가 없습니다.");
            }
            SetFootprint(chair, Vector2Int.one);
            MoveFurniture(chair, counter, -4, -1);
            ConfigureSeat(existingSeat);
            MakeSeat("CounterStool_02", counter, -1, -1, 16);
            MakeSeat("HearthStool", counter, 2, -1, 17);

            MakeFurniture("DiningTable", hall, 6, -3, 4, 2, 37, 4f);
            MakeSeat("DiningChair_01", hall, 6, -4, 16);
            MakeSeat("DiningChair_02", hall, 8, -4, 16);
            MakeSeat("DiningChair_03", hall, 6, -1, 17);
            MakeSeat("DiningChair_04", hall, 8, -1, 17);

            entrance.position = _floor.GetCellCenterWorld(new Vector3Int(0, -6, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(entrance);
            Transform door = NewObject("EntranceDoor", hall);
            door.position = entrance.position + Vector3.down * 1.9f;
            AddVisual(door, 0, 1.15f, -1000);

            player.transform.position = _floor.GetCellCenterWorld(new Vector3Int(0, 2, 0));
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            for (int x = -6; x <= 4; x++)
            {
                RequireAisle(x, 1);
                if (x >= -5)
                {
                    RequireAisle(x, 2);
                }
            }
            RequireAisle(0, -6);

            if (UnityEngine.Object.FindFirstObjectByType<CurrencyWallet>() == null)
            {
                Require<InventoryManager>().gameObject.AddComponent<CurrencyWallet>();
            }
            CustomerSystemSetup.CreateCustomerPrefab();
            CustomerSystemSetup.SetupSpawner();
        }

        private static T Require<T>() where T : Component
        {
            T component = UnityEngine.Object.FindFirstObjectByType<T>();
            return component != null ? component : throw new InvalidOperationException($"{typeof(T).Name}이 없습니다.");
        }

        private static Furniture RequireFurniture(string name)
        {
            GameObject obj = GameObject.Find(name);
            Furniture furniture = obj != null ? obj.GetComponent<Furniture>() : null;
            return furniture != null ? furniture : throw new InvalidOperationException($"{name} 가구가 없습니다.");
        }

        private void MakeSeat(string name, Transform parent, int x, int y, int sprite)
        {
            Furniture chair = MakeFurniture(name, parent, x, y, 1, 1, sprite, 1f);
            ConfigureSeat(chair.gameObject.AddComponent<CustomerSeat>());
        }

        private static void ConfigureSeat(CustomerSeat seat)
        {
            Transform point = seat.transform.Find("SitPoint");
            if (point == null)
            {
                point = NewObject("SitPoint", seat.transform);
            }
            point.localPosition = Vector3.up * 0.5f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(point);
            SerializedObject serialized = new SerializedObject(seat);
            serialized.FindProperty("_sitPoint").objectReferenceValue = point;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private Furniture MakeFurniture(string name, Transform parent, int x, int y, int width, int depth, int sprite, float visualWidth)
        {
            Transform root = NewObject(name, parent);
            root.gameObject.AddComponent<BoxCollider2D>();
            Furniture furniture = root.gameObject.AddComponent<Furniture>();
            SetFootprint(furniture, new Vector2Int(width, depth));
            MoveFurniture(furniture, parent, x, y);
            AddVisual(root, sprite, visualWidth, 0);
            return furniture;
        }

        private static void SetFootprint(Furniture furniture, Vector2Int size)
        {
            SerializedObject serialized = new SerializedObject(furniture);
            serialized.FindProperty("_size").vector2IntValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BoxCollider2D collider = furniture.GetComponent<BoxCollider2D>();
            collider.size = size;
            collider.offset = new Vector2(0f, size.y * 0.5f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
        }

        private void MoveFurniture(Furniture furniture, Transform parent, int x, int y)
        {
            Vector3Int origin = new Vector3Int(x, y, 0);
            for (int dx = 0; dx < furniture.Size.x; dx++)
            {
                for (int dy = 0; dy < furniture.Size.y; dy++)
                {
                    Vector3Int cell = origin + new Vector3Int(dx, dy, 0);
                    if (!_floor.HasTile(cell) || !_used.Add(cell))
                    {
                        throw new InvalidOperationException($"{furniture.name}: 배치가 겹치거나 바닥이 없습니다 {cell}.");
                    }
                }
            }
            Undo.SetTransformParent(furniture.transform, parent, "Move Tavern Furniture");
            furniture.transform.position = _floor.CellToWorld(origin) + Vector3.right * (furniture.Size.x * 0.5f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(furniture.transform);
        }

        private void RequireAisle(int x, int y)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);
            if (!_floor.HasTile(cell) || _used.Contains(cell))
            {
                throw new InvalidOperationException($"통로가 막혔습니다 {cell}.");
            }
        }

        private void AddVisual(Transform parent, int id, float width, int sortingOrder)
        {
            Sprite sprite = _art[id];
            Transform visual = NewObject("Visual", parent);
            float scale = width / sprite.bounds.size.x;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = new Vector3(-sprite.bounds.center.x, -sprite.bounds.min.y, 0f) * scale;
            SpriteRenderer renderer = visual.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Object";
            renderer.sortingOrder = sortingOrder;
        }

        private static Transform NewObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create Tavern Mockup");
            obj.transform.SetParent(parent, false);
            return obj.transform;
        }
    }
}
