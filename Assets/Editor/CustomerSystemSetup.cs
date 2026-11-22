using System.IO;
using GamePlay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 손님 시스템에 필요한 프리팹과 씬 오브젝트를 만든다.
///
/// 사용법 (순서대로):
/// 1. Tools / Customer / 1. Create Customer Prefab
/// 2. 씬을 연 상태에서 Tools / Customer / 2. Setup Spawner In Scene
/// 3. 씬에서 의자 가구를 선택하고 Tools / Customer / 3. Add Seat To Selected
/// </summary>
public static class CustomerSystemSetup
{
    private const string CustomerPrefabPath = "Assets/Objects/Customer.prefab";
    private const string CharacterPrefabPath = "Assets/Prefabs/MainCharacter.prefab";
    private const string CharacterSheetPath = "Assets/Resources/Art/Character/Characters.png";
    private const string SortingLayerName = "Object";
    private const string SpawnerObjectName = "CustomerSystem";
    private const string EntranceObjectName = "CustomerEntrance";
    private const string SitPointObjectName = "SitPoint";

    // 플레이어와 같은 스프라이트를 임시로 쓰므로, 구분되도록 색만 살짝 다르게 한다.
    private static readonly Color CustomerTint = new Color(0.85f, 0.9f, 1f, 1f);

    [MenuItem("Tools/Customer/1. Create Customer Prefab")]
    public static void CreateCustomerPrefab()
    {
        if (File.Exists(CustomerPrefabPath))
        {
            Debug.LogWarning($"[Customer] 이미 있어 건너뜀: {CustomerPrefabPath}");
            return;
        }

        Sprite sprite = LoadCharacterSprite();
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Customer", "손님에게 쓸 캐릭터 스프라이트를 찾지 못했습니다.", "확인");
            return;
        }

        GameObject root = new GameObject("Customer");
        try
        {
            root.AddComponent<CustomerController>();
            root.AddComponent<YSorter>();

            // 루트가 발 위치가 되도록, 스프라이트의 아래쪽 끝을 루트에 맞춘다.
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y, 0f);

            SpriteRenderer spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = SortingLayerName;
            spriteRenderer.color = CustomerTint;

            PrefabUtility.SaveAsPrefabAsset(root, CustomerPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Customer] 손님 프리팹 생성 → {CustomerPrefabPath}");
    }

    [MenuItem("Tools/Customer/2. Setup Spawner In Scene")]
    public static void SetupSpawner()
    {
        CustomerController prefab = AssetDatabase.LoadAssetAtPath<CustomerController>(CustomerPrefabPath);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Customer", "손님 프리팹이 없습니다. 먼저 1번을 실행하세요.", "확인");
            return;
        }

        CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
        GameObject systemObject;
        if (spawner == null)
        {
            systemObject = new GameObject(SpawnerObjectName);
            Undo.RegisterCreatedObjectUndo(systemObject, "Create CustomerSystem");
            spawner = Undo.AddComponent<CustomerSpawner>(systemObject);
        }
        else
        {
            systemObject = spawner.gameObject;
        }

        Transform entrance = systemObject.transform.Find(EntranceObjectName);
        if (entrance == null)
        {
            GameObject entranceObject = new GameObject(EntranceObjectName);
            Undo.RegisterCreatedObjectUndo(entranceObject, "Create CustomerEntrance");
            entrance = entranceObject.transform;
            entrance.SetParent(systemObject.transform, false);

            SceneView sceneView = SceneView.lastActiveSceneView;
            Vector3 position = sceneView != null ? sceneView.pivot : Vector3.zero;
            entrance.position = new Vector3(position.x, position.y, 0f);
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("_customerPrefab").objectReferenceValue = prefab;
        serializedSpawner.FindProperty("_entrance").objectReferenceValue = entrance;

        GameClock clock = Object.FindFirstObjectByType<GameClock>();
        if (clock != null)
        {
            serializedSpawner.FindProperty("_clock").objectReferenceValue = clock;
        }

        serializedSpawner.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(systemObject.scene);
        Selection.activeGameObject = entrance.gameObject;

        if (clock == null)
        {
            Debug.LogWarning("[Customer] 씬에 GameClock이 없습니다. 스포너가 시간을 받으려면 필요합니다.");
        }

        if (Object.FindFirstObjectByType<DataManager>() == null)
        {
            Debug.LogWarning("[Customer] 씬에 DataManager가 없습니다. 손님 데이터를 불러오려면 필요합니다.");
        }

        Debug.Log("[Customer] 스포너 설정 완료. CustomerEntrance를 입구 위치로 옮기세요.");
    }

    [MenuItem("Tools/Customer/3. Add Seat To Selected")]
    public static void AddSeatToSelected()
    {
        int added = 0;
        foreach (GameObject target in Selection.gameObjects)
        {
            // 프리팹 에셋이 아니라 씬에 배치된 오브젝트에만 붙인다.
            if (!target.scene.IsValid() || target.TryGetComponent(out CustomerSeat _))
            {
                continue;
            }

            CustomerSeat seat = Undo.AddComponent<CustomerSeat>(target);

            GameObject sitPointObject = new GameObject(SitPointObjectName);
            Undo.RegisterCreatedObjectUndo(sitPointObject, "Create SitPoint");
            sitPointObject.transform.SetParent(target.transform, false);

            // 가구 루트는 점유 영역의 아래쪽 중앙이라, 앉는 위치는 영역의 가운데로 둔다.
            float height = target.TryGetComponent(out Furniture furniture) ? furniture.Size.y * 0.5f : 0f;
            sitPointObject.transform.localPosition = new Vector3(0f, height, 0f);

            SerializedObject serializedSeat = new SerializedObject(seat);
            serializedSeat.FindProperty("_sitPoint").objectReferenceValue = sitPointObject.transform;
            serializedSeat.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(target.scene);
            added++;
        }

        if (added == 0)
        {
            EditorUtility.DisplayDialog("Customer", "씬에서 의자 오브젝트를 먼저 선택하세요. 이미 CustomerSeat가 있으면 건너뜁니다.", "확인");
            return;
        }

        Debug.Log($"[Customer] CustomerSeat {added}개 추가. SitPoint를 의자에 앉는 위치로 조정하세요.");
    }

    private static Sprite LoadCharacterSprite()
    {
        GameObject character = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath);
        if (character != null)
        {
            SpriteRenderer characterRenderer = character.GetComponentInChildren<SpriteRenderer>();
            if (characterRenderer != null && characterRenderer.sprite != null)
            {
                return characterRenderer.sprite;
            }
        }

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CharacterSheetPath))
        {
            if (asset is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }
}
