using System;
using System.Collections.Generic;
using GamePlay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

internal sealed class StoryEventWorkbench : EditorWindow
{
    private const string EventsFolder = "Assets/Resources/Events";
    private const string RequestPathKey = "ProjectBB.StoryEventWorkbench.Asset";
    private const string RequestStepKey = "ProjectBB.StoryEventWorkbench.Step";
    private const string StatusKey = "ProjectBB.StoryEventWorkbench.Status";

    [SerializeField] private EventDefinition _selected;
    [SerializeField] private string _search = string.Empty;
    private readonly List<EventDefinition> _events = new List<EventDefinition>();
    private StoryEventEditor _editor;
    private Vector2 _eventScroll;
    private Vector2 _stepScroll;
    private Vector2 _detailScroll;
    private double _requestDeadline;
    private EventRunner _runner;

    [MenuItem("Tools/ProjectBB/Event Workbench")]
    private static void Open()
    {
        StoryEventWorkbench window = GetWindow<StoryEventWorkbench>("Event Workbench");
        window.minSize = new Vector2(820f, 360f);
    }

    [InitializeOnLoadMethod]
    private static void RegisterSessionCleanup()
    {
        EditorApplication.playModeStateChanged -= ClearSession;
        EditorApplication.playModeStateChanged += ClearSession;
    }

    private static void ClearSession(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseBool(EventRunner.WorkbenchPlaySessionKey);
            SessionState.EraseString(RequestPathKey);
            SessionState.EraseInt(RequestStepKey);
            SessionState.EraseString(StatusKey);
            SessionState.EraseString(StatusKey + ".Asset");
        }
    }

    private void OnEnable()
    {
        RefreshEvents();
        SetSelected(_selected);
        EditorApplication.projectChanged += RefreshEvents;
        EditorApplication.update += RunPendingPreview;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= RefreshEvents;
        EditorApplication.update -= RunPendingPreview;
        if (_editor != null)
        {
            DestroyImmediate(_editor);
        }
    }

    private void RefreshEvents()
    {
        _events.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:EventDefinition", new[] { EventsFolder }))
        {
            EventDefinition definition = AssetDatabase.LoadAssetAtPath<EventDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if (definition != null)
            {
                _events.Add(definition);
            }
        }
        _events.Sort((first, second) => string.CompareOrdinal(first.name, second.name));
        Repaint();
    }

    private void SetSelected(EventDefinition definition)
    {
        if (_editor != null)
        {
            DestroyImmediate(_editor);
        }

        _selected = definition;
        _editor = _selected != null ? (StoryEventEditor)Editor.CreateEditor(_selected, typeof(StoryEventEditor)) : null;
        _stepScroll = Vector2.zero;
        _detailScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        DrawPreviewToolbar();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawEventList();
            if (_editor == null)
            {
                EditorGUILayout.HelpBox("이벤트를 선택하거나 새 이벤트를 만드세요.", MessageType.None);
                return;
            }

            _editor.serializedObject.Update();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawSteps();
                DrawDetails();
            }
            _editor.serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawPreviewToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using (new EditorGUI.DisabledScope(_editor == null || _editor.StepCount == 0 || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("처음부터 실행", EditorStyles.toolbarButton))
                {
                    StartPreview(0);
                }
                if (GUILayout.Button("선택한 단계부터 실행", EditorStyles.toolbarButton))
                {
                    StartPreview(_editor.SelectedIndex);
                }
            }

            if (EventRunner.IsWorkbenchPlaySession)
            {
                if (GUILayout.Button("테스트 종료", EditorStyles.toolbarButton))
                {
                    EditorApplication.isPlaying = false;
                }
                GUILayout.Label(GetPreviewStatus(), EditorStyles.miniLabel);
            }
            GUILayout.FlexibleSpace();
        }
    }

    private void DrawEventList()
    {
        float width = Mathf.Clamp(position.width * 0.22f, 165f, 260f);
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("이벤트", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("새 이벤트"))
                {
                    CreateEvent();
                    GUIUtility.ExitGUI();
                }
            }

            _eventScroll = EditorGUILayout.BeginScrollView(_eventScroll);
            foreach (EventDefinition definition in _events)
            {
                if (!string.IsNullOrWhiteSpace(_search) &&
                    definition.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0 &&
                    GetTextField(definition, "_id").IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (GUILayout.Toggle(definition == _selected, definition.name, "Button") && definition != _selected)
                {
                    SetSelected(definition);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawSteps()
    {
        float width = Mathf.Clamp(position.width * 0.32f, 245f, 410f);
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("단계", EditorStyles.boldLabel);
            _editor.DrawAddButtons();
            _stepScroll = EditorGUILayout.BeginScrollView(_stepScroll);
            string[] labels = _editor.GetStepLabels();
            for (int i = 0; i < labels.Length; i++)
            {
                bool playingStep = _runner != null && _runner.IsPreviewRunning &&
                    AssetDatabase.GetAssetPath(_selected) == SessionState.GetString(StatusKey + ".Asset", string.Empty) &&
                    _runner.PreviewStepIndex == i;
                string label = playingStep ? "▶ " + labels[i] : labels[i];
                if (GUILayout.Toggle(i == _editor.SelectedIndex, label, "Button"))
                {
                    _editor.SelectStep(i);
                }
            }
            EditorGUILayout.EndScrollView();
            if (_editor.StepCount > 0)
            {
                _editor.DrawStepActions();
            }
        }
    }

    private void DrawDetails()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            EditorGUILayout.LabelField("단계 설정", EditorStyles.boldLabel);
            _editor.DrawSelectedStep();
            EditorGUILayout.Space(18f);
            EditorGUILayout.LabelField("이벤트 설정", EditorStyles.boldLabel);
            _editor.DrawEventSettings();
            DrawRelatedEvents();
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawRelatedEvents()
    {
        string id = GetTextField(_selected, "_id");
        string prerequisite = GetTextField(_selected, "_prerequisite");
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        foreach (EventDefinition definition in _events)
        {
            string otherId = GetTextField(definition, "_id");
            if (otherId == prerequisite || GetTextField(definition, "_prerequisite") == id)
            {
                string relation = otherId == prerequisite ? "선행" : "후속";
                if (GUILayout.Button($"{relation} · {definition.name}"))
                {
                    _editor.serializedObject.ApplyModifiedProperties();
                    SetSelected(definition);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private void CreateEvent()
    {
        string path = EditorUtility.SaveFilePanelInProject("새 이벤트", "NewEvent", "asset", "이벤트 이름을 입력하세요.", EventsFolder);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!path.StartsWith(EventsFolder + "/", StringComparison.Ordinal))
        {
            EditorUtility.DisplayDialog("새 이벤트", "Assets/Resources/Events 아래에 저장하세요.", "확인");
            return;
        }

        path = AssetDatabase.GenerateUniqueAssetPath(path);
        EventDefinition definition = CreateInstance<EventDefinition>();
        AssetDatabase.CreateAsset(definition, path);
        var fields = new SerializedObject(definition);
        fields.FindProperty("_id").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
        fields.FindProperty("_sceneName").stringValue = UnitySceneManager.GetActiveScene().name;
        fields.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(definition);
        RefreshEvents();
        SetSelected(definition);
    }

    private static string GetTextField(EventDefinition definition, string field)
    {
        return new SerializedObject(definition).FindProperty(field).stringValue;
    }

    private void StartPreview(int startStep)
    {
        _editor.serializedObject.ApplyModifiedProperties();
        Scene scene = UnitySceneManager.GetActiveScene();
        string expectedScene = GetTextField(_selected, "_sceneName");
        if (!string.IsNullOrEmpty(expectedScene) && scene.name != expectedScene)
        {
            EditorUtility.DisplayDialog("이벤트 테스트", $"{expectedScene} 씬을 열고 다시 실행하세요.", "확인");
            return;
        }

        bool hasDialogue = Array.Exists(FindObjectsByType<DialoguePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None),
            panel => panel.gameObject.scene == scene);
        if (!hasDialogue)
        {
            EditorUtility.DisplayDialog("이벤트 테스트", "현재 씬에 대화 패널이 없습니다. 이벤트를 실행할 게임 씬을 열어 주세요.", "확인");
            return;
        }

        AssetDatabase.SaveAssetIfDirty(_selected);
        string path = AssetDatabase.GetAssetPath(_selected);
        SessionState.SetBool(EventRunner.WorkbenchPlaySessionKey, true);
        SessionState.SetString(RequestPathKey, path);
        SessionState.SetString(StatusKey + ".Asset", path);
        SessionState.SetInt(RequestStepKey, startStep);
        SessionState.SetString(StatusKey, "씬 로딩 중");
        _requestDeadline = 0;
        EditorApplication.EnterPlaymode();
    }

    private void RunPendingPreview()
    {
        if (!EditorApplication.isPlaying || !EventRunner.IsWorkbenchPlaySession)
        {
            return;
        }

        string path = SessionState.GetString(RequestPathKey, string.Empty);
        if (!string.IsNullOrEmpty(path))
        {
            if (_requestDeadline == 0d)
            {
                _requestDeadline = EditorApplication.timeSinceStartup + 10d;
            }

            _runner = FindFirstObjectByType<EventRunner>();
            EventDefinition definition = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (_runner != null && definition != null &&
                _runner.StartWorkbenchPreview(definition, SessionState.GetInt(RequestStepKey, 0)))
            {
                SessionState.EraseString(RequestPathKey);
                SessionState.SetString(StatusKey, "시작됨");
            }
            else if (EditorApplication.timeSinceStartup > _requestDeadline)
            {
                SessionState.EraseString(RequestPathKey);
                SessionState.SetString(StatusKey, "이벤트를 시작할 수 없습니다. 씬과 이벤트 ID를 확인하세요.");
            }
        }

        Repaint();
    }

    private string GetPreviewStatus()
    {
        string status = SessionState.GetString(StatusKey, string.Empty);
        if (_runner != null && _runner.IsPreviewRunning)
        {
            string waiting = _runner.PreviewWaitingSignal;
            return string.IsNullOrEmpty(waiting) ? $"{_runner.PreviewStepIndex + 1}단계 실행 중" : $"행동 대기 · {waiting}";
        }

        if (_runner != null && status == "시작됨")
        {
            return _runner.PreviewCompleted ? "테스트 완료" : "테스트 중단";
        }

        return status;
    }
}
