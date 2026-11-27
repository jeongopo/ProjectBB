using GamePlay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

internal static class CustomerSeatSetup
{
    private const string SitPointObjectName = "SitPoint";

    [MenuItem("Tools/Customer/Add Seat To Selected")]
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
}
