using GamePlay;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scene 뷰에서 가구를 드래그하는 동안 그리드 칸 단위로 움직이게 한다.
/// 메뉴 Tools / Furniture / Auto Snap On Move 로 켜고 끈다.
/// </summary>
[InitializeOnLoad]
public static class FurnitureAutoSnap
{
    private const string MenuPath = "Tools/Furniture/Auto Snap On Move";
    private const string PrefKey = "ProjectBB.FurnitureAutoSnap";

    static FurnitureAutoSnap()
    {
        SceneView.duringSceneGui += OnSceneGui;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Enabled = !Enabled;
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        if (!Enabled || Application.isPlaying)
        {
            return;
        }

        // 이동 핸들이 이벤트를 소비하므로 rawType으로 확인한다. 드래그 중에도 맞춰서 한 칸씩 움직이게 한다.
        Event current = Event.current;
        EventType type = current.rawType;
        if ((type != EventType.MouseDrag && type != EventType.MouseUp) || current.button != 0)
        {
            return;
        }

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (!selected.TryGetComponent(out Furniture furniture))
            {
                continue;
            }

            Vector3 before = furniture.transform.position;
            Undo.RecordObject(furniture.transform, "Snap Furniture");
            furniture.SnapToGrid();
            if (furniture.transform.position != before)
            {
                sceneView.Repaint();
            }
        }
    }
}
