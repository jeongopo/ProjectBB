using System.Collections.Generic;
using GamePlay;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EventDefinition))]
internal sealed class StoryEventEditor : Editor
{
    private static readonly string[] StepLabels =
    {
        "대사", "이동", "카메라 이동", "카메라 복귀", "행동 대기", "선택지",
    };

    private SerializedProperty _steps;
    private int _selectedIndex;
    private SceneView _previewView;
    private Vector3 _savedPivot;
    private Quaternion _savedRotation;
    private float _savedSize;
    private bool _savedOrthographic;
    private Vector3 _previewCenter;
    private float _previewHalfHeight;
    private bool _showAdvancedSettings;
    private StorySpeakerProfile[] _speakerProfiles;
    private Color _inlineDialogueColor = Color.white;
    private bool _showDialogueColor;
    internal int SelectedIndex => _selectedIndex;
    internal int StepCount => _steps.arraySize;

    private void OnEnable()
    {
        _steps = serializedObject.FindProperty("_steps");
        _speakerProfiles = Resources.LoadAll<StorySpeakerProfile>("StorySpeakers");
        SceneView.duringSceneGui += DrawFrame;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawFrame;
        RestorePreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawEventSettings();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("이벤트 단계", EditorStyles.boldLabel);
        DrawAddButtons();

        if (_steps.arraySize > 0)
        {
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _steps.arraySize - 1);
            _selectedIndex = EditorGUILayout.Popup("편집할 단계", _selectedIndex, GetStepLabels());
            DrawStepActions();
            DrawSelectedStep();
        }
        serializedObject.ApplyModifiedProperties();
    }

    internal void DrawEventSettings()
    {
        DrawSetting("_id", "이벤트 ID");
        DrawSpeaker(serializedObject.FindProperty("_speaker"), "기본 화자");

        SerializedProperty automatic = serializedObject.FindProperty("_automatic");
        automatic.boolValue = EditorGUILayout.Popup("시작 방식", automatic.boolValue ? 1 : 0,
            new[] { "말 걸기 / 조사", "조건이 맞으면 자동" }) == 1;
        _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "고급 조건 · 선행 이벤트, 신호, 시간", true);
        if (!_showAdvancedSettings)
        {
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            DrawSetting("_prerequisite", "선행 이벤트 ID");
            DrawSetting("_requiredSignals", "필요한 행동 신호");
            DrawSetting("_sceneName", "실행할 씬");
            DrawSetting("_earliestDay", "첫 실행 가능 일차");
            DrawSetting("_startMinute", "시작 가능 시각 (분)");
            DrawSetting("_endMinute", "시작 마감 시각 (분)");
            DrawSetting("_priority", "우선순위");
        }
    }

    private void DrawSetting(string propertyName, string label)
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName), new GUIContent(label), true);
    }

    private static void DrawSpeaker(SerializedProperty speaker, string label)
    {
        EditorGUILayout.PropertyField(speaker, new GUIContent(label));
    }

    internal void DrawAddButtons()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("단계 추가"))
            {
                ShowStepMenu();
            }
            using (new EditorGUI.DisabledScope(SceneView.lastActiveSceneView == null))
            {
                if (GUILayout.Button("씬 뷰 구도 + 복귀 추가"))
                {
                    Vector3 center = SceneView.lastActiveSceneView.pivot;
                    AddCameraPair(center);
                }
            }
        }
    }

    internal string[] GetStepLabels()
    {
        string[] labels = new string[_steps.arraySize];
        for (int i = 0; i < labels.Length; i++)
        {
            SerializedProperty step = _steps.GetArrayElementAtIndex(i);
            int kind = step.FindPropertyRelative("_kind").enumValueIndex;
            string detail = step.FindPropertyRelative("_textKey").stringValue;
            if (string.IsNullOrEmpty(detail))
            {
                detail = step.FindPropertyRelative("_targetId").stringValue;
            }
            labels[i] = string.IsNullOrEmpty(detail) ? $"{i + 1}. {StepLabels[kind]}" : $"{i + 1}. {StepLabels[kind]} · {detail}";
        }
        return labels;
    }

    internal void SelectStep(int index)
    {
        _selectedIndex = index;
    }

    internal void DrawSelectedStep()
    {
        if (_steps.arraySize > 0)
        {
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _steps.arraySize - 1);
            DrawStep(_steps.GetArrayElementAtIndex(_selectedIndex));
        }
    }

    internal void DrawStepActions()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_selectedIndex == 0))
            {
                if (GUILayout.Button("↑"))
                {
                    MoveStep(-1);
                }
            }
            using (new EditorGUI.DisabledScope(_selectedIndex == _steps.arraySize - 1))
            {
                if (GUILayout.Button("↓"))
                {
                    MoveStep(1);
                }
            }
            if (GUILayout.Button("복제"))
            {
                DuplicateStep();
            }
            if (GUILayout.Button("단계 삭제"))
            {
                DeleteStep();
            }
        }
    }

    private void DrawStep(SerializedProperty step)
    {
        SerializedProperty kind = step.FindPropertyRelative("_kind");
        kind.enumValueIndex = EditorGUILayout.Popup("종류", kind.enumValueIndex, StepLabels);
        switch ((EventStepKind)kind.enumValueIndex)
        {
            case EventStepKind.Dialogue:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("_textKey"), new GUIContent("대사 키"));
                SerializedProperty speaker = step.FindPropertyRelative("_speaker");
                DrawSpeaker(speaker, "화자");
                DrawExpression(step.FindPropertyRelative("_expression"), (StorySpeaker)speaker.intValue);
                _showDialogueColor = EditorGUILayout.Foldout(_showDialogueColor, "대사 일부에 색상 넣기", true);
                if (_showDialogueColor)
                {
                    _inlineDialogueColor = EditorGUILayout.ColorField("글자색", _inlineDialogueColor);
                    if (GUILayout.Button("BBString용 색상 태그 복사"))
                    {
                        EditorGUIUtility.systemCopyBuffer = $"<color=#{ColorUtility.ToHtmlStringRGBA(_inlineDialogueColor)}>문구</color>";
                    }
                }
                break;
            case EventStepKind.Move:
                DrawSceneBinding(step.FindPropertyRelative("_targetId"), "이동 대상");
                DrawSceneBinding(step.FindPropertyRelative("_destinationId"), "목적지");
                DrawFields(step, "_offset", "_duration");
                break;
            case EventStepKind.FocusCamera:
                DrawCameraStep(step);
                break;
            case EventStepKind.RestoreCamera:
                DrawFields(step, "_duration");
                EditorGUILayout.HelpBox("카메라가 연출 전에 사용하던 위치로 돌아갑니다.", MessageType.None);
                break;
            case EventStepKind.WaitForSignal:
                DrawFields(step, "_signal", "_textKey");
                break;
            case EventStepKind.Choice:
                DrawFields(step, "_textKey", "_choiceId");
                DrawChoices(step.FindPropertyRelative("_options"));
                break;
        }
    }

    private void DrawExpression(SerializedProperty expression, StorySpeaker speaker)
    {
        string[] names = expression.enumDisplayNames;
        var labels = new List<GUIContent>();
        var values = new List<int>();
        for (int i = 0; i < names.Length; i++)
        {
            StoryExpression option = (StoryExpression)i;
            if (option == StoryExpression.Inherit || option == StoryExpression.Neutral || i == expression.intValue ||
                System.Array.Exists(_speakerProfiles, profile => profile.SupportsExpression(speaker, option)))
            {
                labels.Add(new GUIContent(names[i]));
                values.Add(i);
            }
        }
        expression.intValue = EditorGUILayout.IntPopup(new GUIContent("표정"), expression.intValue, labels.ToArray(), values.ToArray());
    }

    private void DrawChoices(SerializedProperty options)
    {
        string[] steps = GetStepLabels();
        string[] destinations = new string[steps.Length + 1];
        destinations[0] = "이벤트 완료";
        steps.CopyTo(destinations, 1);
        for (int i = 0; i < options.arraySize; i++)
        {
            SerializedProperty choice = options.GetArrayElementAtIndex(i);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawFields(choice, "_labelKey", "_result");
                SerializedProperty next = choice.FindPropertyRelative("_nextStep");
                int selected = next.intValue >= -1 && next.intValue < steps.Length ? next.intValue + 1 : -1;
                int chosen = EditorGUILayout.Popup("연결할 단계", selected, destinations);
                if (chosen != selected)
                {
                    next.intValue = chosen - 1;
                }
                if (GUILayout.Button("선택지 삭제"))
                {
                    options.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }
        if (GUILayout.Button("선택지 추가"))
        {
            int index = options.arraySize;
            options.arraySize++;
            SerializedProperty choice = options.GetArrayElementAtIndex(index);
            choice.FindPropertyRelative("_labelKey").stringValue = string.Empty;
            choice.FindPropertyRelative("_result").stringValue = string.Empty;
            choice.FindPropertyRelative("_nextStep").intValue = -1;
        }
    }

    private void DrawCameraStep(SerializedProperty step)
    {
        SerializedProperty worldMode = step.FindPropertyRelative("_useWorldPosition");
        SerializedProperty position = step.FindPropertyRelative("_worldPosition");
        worldMode.boolValue = EditorGUILayout.Popup("구도 기준", worldMode.boolValue ? 1 : 0,
            new[] { "씬 대상 중심", "맵에 고정된 구도" }) == 1;

        if (worldMode.boolValue)
        {
            EditorGUILayout.PropertyField(position);
        }
        else
        {
            DrawSceneBinding(step.FindPropertyRelative("_targetId"), "카메라 대상");
            DrawFields(step, "_offset");
        }
        DrawFields(step, "_duration");

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(SceneView.lastActiveSceneView == null))
            {
                if (GUILayout.Button("현재 씬 뷰 구도 사용"))
                {
                    worldMode.boolValue = true;
                    position.vector2Value = SceneView.lastActiveSceneView.pivot;
                }
            }
            if (GUILayout.Button("게임 구도 미리보기"))
            {
                PreviewShot(step);
            }
        }
        if (_previewView != null && GUILayout.Button("편집 뷰로 돌아가기"))
        {
            RestorePreview();
        }
    }

    private static void DrawSceneBinding(SerializedProperty id, string label)
    {
        EditorGUILayout.PropertyField(id, new GUIContent(label));
        GameObject selected = Selection.activeGameObject;
        bool canBind = selected != null && selected.scene.IsValid() && !EditorApplication.isPlayingOrWillChangePlaymode;
        using (new EditorGUI.DisabledScope(!canBind))
        {
            if (!GUILayout.Button($"씬 선택을 {label}에 연결"))
            {
                return;
            }

            if (!selected.TryGetComponent(out EventSceneAnchor anchor))
            {
                anchor = Undo.AddComponent<EventSceneAnchor>(selected);
            }

            var fields = new SerializedObject(anchor);
            SerializedProperty anchorId = fields.FindProperty("_id");
            if (string.IsNullOrEmpty(anchorId.stringValue))
            {
                Transform existing = FindSceneTarget(selected.name);
                anchorId.stringValue = existing == null || existing == selected.transform
                    ? selected.name
                    : selected.name + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
                fields.ApplyModifiedProperties();
            }
            id.stringValue = anchorId.stringValue;
        }
    }

    private void ShowStepMenu()
    {
        GenericMenu menu = new GenericMenu();
        for (int i = 0; i < StepLabels.Length; i++)
        {
            int kind = i;
            menu.AddItem(new GUIContent(StepLabels[i]), false, () =>
            {
                serializedObject.Update();
                AppendStep(kind);
                serializedObject.ApplyModifiedProperties();
            });
        }
        menu.ShowAsContext();
    }

    private void AddCameraPair(Vector3 center)
    {
        SerializedProperty focus = AppendStep((int)EventStepKind.FocusCamera);
        focus.FindPropertyRelative("_useWorldPosition").boolValue = true;
        focus.FindPropertyRelative("_worldPosition").vector2Value = center;
        AppendStep((int)EventStepKind.RestoreCamera);
        _selectedIndex--;
    }

    private SerializedProperty AppendStep(int kind)
    {
        _selectedIndex = _steps.arraySize;
        _steps.arraySize++;
        SerializedProperty step = _steps.GetArrayElementAtIndex(_selectedIndex);
        step.FindPropertyRelative("_kind").enumValueIndex = kind;
        foreach (string field in new[] { "_textKey", "_signal", "_speakerKey", "_choiceId", "_targetId", "_destinationId" })
        {
            step.FindPropertyRelative(field).stringValue = string.Empty;
        }
        step.FindPropertyRelative("_portrait").objectReferenceValue = null;
        step.FindPropertyRelative("_speaker").enumValueIndex = (int)StorySpeaker.Inherit;
        step.FindPropertyRelative("_expression").enumValueIndex = (int)StoryExpression.Inherit;
        step.FindPropertyRelative("_options").ClearArray();
        step.FindPropertyRelative("_offset").vector3Value = Vector3.zero;
        step.FindPropertyRelative("_useWorldPosition").boolValue = false;
        step.FindPropertyRelative("_worldPosition").vector2Value = Vector2.zero;
        step.FindPropertyRelative("_duration").floatValue = 0.65f;
        return step;
    }

    private void MoveStep(int direction)
    {
        int source = _selectedIndex;
        int destination = source + direction;
        _steps.MoveArrayElement(source, destination);
        foreach (SerializedProperty next in ChoiceDestinations())
        {
            if (next.intValue == source)
            {
                next.intValue = destination;
            }
            else if (next.intValue == destination)
            {
                next.intValue = source;
            }
        }
        _selectedIndex = destination;
    }

    private void DeleteStep()
    {
        foreach (SerializedProperty next in ChoiceDestinations())
        {
            if (next.intValue == _selectedIndex)
            {
                EditorUtility.DisplayDialog("이벤트 단계 삭제", "이 단계로 이어지는 선택지가 있습니다. 선택지의 연결을 바꾼 뒤 삭제하세요.", "확인");
                return;
            }
        }
        _steps.DeleteArrayElementAtIndex(_selectedIndex);
        foreach (SerializedProperty next in ChoiceDestinations())
        {
            if (next.intValue > _selectedIndex)
            {
                next.intValue--;
            }
        }
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _steps.arraySize - 1));
    }

    private void DuplicateStep()
    {
        int source = _selectedIndex;
        _steps.InsertArrayElementAtIndex(source);
        _steps.GetArrayElementAtIndex(source).boxedValue = _steps.GetArrayElementAtIndex(source + 1).boxedValue;
        foreach (SerializedProperty next in ChoiceDestinations())
        {
            if (next.intValue > source)
            {
                next.intValue++;
            }
        }
        _selectedIndex = source + 1;
    }

    private IEnumerable<SerializedProperty> ChoiceDestinations()
    {
        for (int i = 0; i < _steps.arraySize; i++)
        {
            SerializedProperty options = _steps.GetArrayElementAtIndex(i).FindPropertyRelative("_options");
            for (int j = 0; j < options.arraySize; j++)
            {
                yield return options.GetArrayElementAtIndex(j).FindPropertyRelative("_nextStep");
            }
        }
    }

    private static void DrawFields(SerializedProperty step, params string[] names)
    {
        foreach (string name in names)
        {
            EditorGUILayout.PropertyField(step.FindPropertyRelative(name), true);
        }
    }

    private void PreviewShot(SerializedProperty step)
    {
        SceneView view = SceneView.lastActiveSceneView;
        Camera camera = Camera.main;
        if (view == null || camera == null)
        {
            EditorUtility.DisplayDialog("카메라 미리보기", "씬 뷰와 MainCamera가 있는 씬을 열어 주세요.", "확인");
            return;
        }

        Vector3 center;
        if (step.FindPropertyRelative("_useWorldPosition").boolValue)
        {
            center = step.FindPropertyRelative("_worldPosition").vector2Value;
        }
        else
        {
            string id = step.FindPropertyRelative("_targetId").stringValue;
            Transform target = FindSceneTarget(id);
            if (target == null)
            {
                EditorUtility.DisplayDialog("카메라 미리보기", "씬에서 대상 ID에 해당하는 위치를 찾을 수 없습니다.", "확인");
                return;
            }
            center = target.position + step.FindPropertyRelative("_offset").vector3Value;
        }
        center.z = 0f;

        if (_previewView != view)
        {
            RestorePreview();
            _previewView = view;
            _savedPivot = view.pivot;
            _savedRotation = view.rotation;
            _savedSize = view.size;
            _savedOrthographic = view.orthographic;
        }
        _previewCenter = center;
        _previewHalfHeight = camera.orthographic
            ? camera.orthographicSize
            : Mathf.Abs(camera.transform.position.z - center.z) * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
        _previewHalfHeight = Mathf.Max(0.1f, _previewHalfHeight);
        view.orthographic = true;
        view.rotation = Quaternion.identity;
        view.pivot = center;
        view.size = _previewHalfHeight * 1.8f;
        view.Repaint();
    }

    private static Transform FindSceneTarget(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        foreach (EventSceneAnchor anchor in FindObjectsByType<EventSceneAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var fields = new SerializedObject(anchor);
            if (fields.FindProperty("_id").stringValue == id)
            {
                return anchor.transform;
            }
        }
        if (id == "Player")
        {
            MainCharacter player = FindFirstObjectByType<MainCharacter>();
            return player != null ? player.transform : null;
        }
        return GameObject.Find(id)?.transform;
    }

    private void DrawFrame(SceneView view)
    {
        if (view != _previewView)
        {
            return;
        }
        using (new Handles.DrawingScope(new Color(1f, 0.72f, 0.32f)))
        {
            Handles.DrawWireCube(_previewCenter, new Vector3(_previewHalfHeight * 2f * 800f / 450f, _previewHalfHeight * 2f, 0f));
        }
    }

    private void RestorePreview()
    {
        if (_previewView == null)
        {
            return;
        }
        _previewView.orthographic = _savedOrthographic;
        _previewView.rotation = _savedRotation;
        _previewView.pivot = _savedPivot;
        _previewView.size = _savedSize;
        _previewView.Repaint();
        _previewView = null;
    }
}
