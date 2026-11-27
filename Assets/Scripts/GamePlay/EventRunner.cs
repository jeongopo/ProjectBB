using System;
using System.Collections.Generic;
using DG.Tweening;
using GameEnumDefines;
using Interaction;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GamePlay
{
    public sealed class EventRunner : MonoBehaviour
    {
        private const string SuppliesReceived = "TavernSuppliesReceived";
        private const string OrderReceived = "TavernFirstOrderReceived";
        private readonly List<EventDefinition> _definitions = new List<EventDefinition>();
        private readonly Dictionary<StorySpeaker, StorySpeakerProfile> _speakerProfiles = new Dictionary<StorySpeaker, StorySpeakerProfile>();
        private readonly Dictionary<string, Transform> _targets = new Dictionary<string, Transform>();
        private readonly HashSet<string> _completed = new HashSet<string>();
        private readonly HashSet<string> _signals = new HashSet<string>();
        private readonly HashSet<string> _interrupted = new HashSet<string>();
        private readonly HashSet<string> _deferred = new HashSet<string>();
        private readonly List<EventChoiceResult> _choices = new List<EventChoiceResult>();
        private readonly Dictionary<string, string> _currentChoices = new Dictionary<string, string>();
        private readonly Dictionary<Transform, Vector3> _positions = new Dictionary<Transform, Vector3>();
        private DialoguePanel _panel;
        private InputManager _input;
        private GameSession _session;
        private GameClock _clock;
        private SupplyShelf _shelf;
        private OrderBoard _orders;
        private Transform _camera;
        private Vector3 _cameraHome;
        private bool _cameraOverridden;
        private Sprite _portrait;
        private StorySpeaker _speaker;
        private EventDefinition _active;
        private EventStep _waiting;
        private Tween _tween;
        private int _step;
        private bool _isPreview;
#if UNITY_EDITOR
        public const string WorkbenchPlaySessionKey = "ProjectBB.StoryEventWorkbench.PlaySession";
        public static bool IsWorkbenchPlaySession => UnityEditor.SessionState.GetBool(WorkbenchPlaySessionKey, false);
        public bool IsPreviewRunning => _isPreview && _active != null;
        public bool PreviewCompleted { get; private set; }
        public int PreviewStepIndex => _step;
        public string PreviewWaitingSignal => _waiting?.Signal;

        public bool StartWorkbenchPreview(EventDefinition definition, int startStep)
        {
            if (!IsWorkbenchPlaySession || !_definitions.Contains(definition) ||
                (!string.IsNullOrEmpty(definition.SceneName) && definition.SceneName != gameObject.scene.name) ||
                startStep < 0 || startStep >= definition.Steps.Length)
            {
                return false;
            }

            PreviewCompleted = false;
            return StartEvent(definition, startStep, true);
        }
#endif
        internal bool IsComplete(string id) => _completed.Contains(id);
        internal bool HasObserved(string signal) => _signals.Contains(signal);
        internal string GetChoice(string eventId, string choiceId) =>
            _choices.Find(choice => choice.EventId == eventId && choice.ChoiceId == choiceId).Value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHandler()
        {
            UnitySceneManager.sceneLoaded -= CreateForScene;
            UnitySceneManager.sceneLoaded += CreateForScene;
        }

        private static void CreateForScene(Scene scene, LoadSceneMode mode)
        {
            foreach (DialoguePanel panel in FindObjectsByType<DialoguePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (panel.gameObject.scene != scene)
                {
                    continue;
                }

                GameObject root = new GameObject("StoryEvents");
                UnitySceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent<EventRunner>().Configure(panel);
                return;
            }
        }

        private void Configure(DialoguePanel panel)
        {
            _panel = panel;
            _input = FindFirstObjectByType<InputManager>();
            _clock = FindFirstObjectByType<GameClock>();
            Camera camera = Camera.main;
            _camera = camera != null ? camera.transform : null;

            foreach (EventSceneAnchor anchor in FindObjectsByType<EventSceneAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (anchor.gameObject.scene == gameObject.scene)
                {
                    Bind(anchor.Id, anchor.transform);
                }
            }
            MainCharacter player = FindFirstObjectByType<MainCharacter>();
            if (player != null)
            {
                Bind("Player", player.transform);
            }

            _shelf = FindFirstObjectByType<SupplyShelf>();
            if (_shelf != null)
            {
                Bind("SupplyShelf", _shelf.transform);
                _shelf.SuppliesReceived += HandleSupplies;
            }
            _orders = FindFirstObjectByType<OrderBoard>();
            if (_orders != null)
            {
                _orders.OnOrderAdded += HandleOrder;
                if (_orders.Orders.Count > 0)
                {
                    Observe(OrderReceived);
                }
            }

            foreach (StorySpeakerProfile profile in Resources.LoadAll<StorySpeakerProfile>("StorySpeakers"))
            {
                if (profile.Speaker != StorySpeaker.Inherit && !_speakerProfiles.TryAdd(profile.Speaker, profile))
                {
                    Debug.LogWarning($"화자 프로필이 중복되었습니다: {profile.Speaker}", profile);
                }
            }

            foreach (EventDefinition definition in Resources.LoadAll<EventDefinition>("Events"))
            {
                Register(definition);
            }
        }

        private void Start()
        {
            _session = FindFirstObjectByType<GameSession>();
        }

        private void HandleSupplies() => Observe(SuppliesReceived);

        private void HandleOrder(PlatingOrder _) => Observe(OrderReceived);

        internal void Bind(string id, Transform target)
        {
            if (!string.IsNullOrWhiteSpace(id) && !_targets.TryAdd(id, target) && _targets[id] != target)
            {
                Debug.LogWarning($"이벤트 씬 대상 ID가 중복되었습니다: {id}", target);
            }
        }

        private Transform FindTarget(string id)
        {
            return !string.IsNullOrEmpty(id) && _targets.TryGetValue(id, out Transform target) ? target : null;
        }

        private void Register(EventDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || definition.Steps == null || definition.Steps.Length == 0 ||
                _definitions.Exists(existing => existing.Id == definition.Id))
            {
                Debug.LogWarning($"이벤트 에셋의 ID 또는 단계가 올바르지 않습니다: {definition.name}", definition);
                return;
            }
            _definitions.Add(definition);
            _definitions.Sort((first, second) =>
            {
                int followup = (!string.IsNullOrEmpty(second.Prerequisite)).CompareTo(!string.IsNullOrEmpty(first.Prerequisite));
                if (followup != 0)
                {
                    return followup;
                }
                int priority = second.Priority.CompareTo(first.Priority);
                return priority != 0 ? priority : string.CompareOrdinal(first.Id, second.Id);
            });
        }

        internal void Observe(string signal)
        {
            if (_signals.Add(signal))
            {
                _session?.MarkEventProgressChanged();
            }
        }

        internal void Restore(EventProgressData data, IEnumerable<string> legacyCompleted)
        {
            if (data?.CompletedIds != null)
            {
                _completed.UnionWith(data.CompletedIds);
            }
            if (legacyCompleted != null)
            {
                _completed.UnionWith(legacyCompleted);
            }
            if (data?.ObservedSignals != null)
            {
                _signals.UnionWith(data.ObservedSignals);
            }
            if (data?.InterruptedIds != null)
            {
                _interrupted.UnionWith(data.InterruptedIds);
                _interrupted.ExceptWith(_completed);
            }
            if (data?.Choices != null)
            {
                _choices.AddRange(data.Choices);
            }
        }

        internal EventProgressData Capture() => new EventProgressData
        {
            CompletedIds = new List<string>(_completed),
            ObservedSignals = new List<string>(_signals),
            InterruptedIds = new List<string>(_interrupted),
            Choices = new List<EventChoiceResult>(_choices),
        };

        private bool GameplayAvailable => _input == null || _input.CurrentInputState == InputState.Default;

        private void LateUpdate()
        {
            if (_active != null)
            {
                if (_waiting != null && GameplayAvailable && HasObserved(_waiting.Signal))
                {
                    _waiting = null;
                    _panel.ResumeEventDialogue();
                    Advance();
                }
                return;
            }

            if (!GameplayAvailable || Time.timeScale <= 0f || _panel == null || _panel.IsOpen)
            {
                return;
            }

#if UNITY_EDITOR
            if (IsWorkbenchPlaySession)
            {
                return;
            }
#endif

            foreach (EventDefinition definition in _definitions)
            {
                if ((definition.Automatic || _interrupted.Contains(definition.Id)) &&
                    !_deferred.Contains(definition.Id) && CanStart(definition) && StartEvent(definition))
                {
                    return;
                }
            }
        }

        internal bool TryResume(string speakerKey)
        {
            foreach (EventDefinition definition in _definitions)
            {
                if (GetSpeakerKey(definition) == speakerKey && _interrupted.Contains(definition.Id) &&
                    CanStart(definition) && StartEvent(definition))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool TryStart(string id)
        {
            foreach (EventDefinition definition in _definitions)
            {
                if (definition.Id == id)
                {
                    return CanStart(definition) && StartEvent(definition);
                }
            }
            return false;
        }

        internal bool CanTrigger(string id)
        {
            if (_active != null || _panel == null || _panel.IsOpen || !GameplayAvailable || string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (EventDefinition definition in _definitions)
            {
                if (definition.Id == id)
                {
                    return CanStart(definition);
                }
            }
            return false;
        }

        internal bool HasDefinition(string id) => _definitions.Exists(definition => definition.Id == id);

        private bool CanStart(EventDefinition definition)
        {
            if (_completed.Contains(definition.Id) ||
                (!string.IsNullOrEmpty(definition.Prerequisite) && !IsComplete(definition.Prerequisite)) ||
                (!string.IsNullOrEmpty(definition.SceneName) && definition.SceneName != gameObject.scene.name))
            {
                return false;
            }

            if (_clock != null && (_clock.IsDayEnded || _clock.Day < definition.EarliestDay ||
                _clock.TotalMinutes < definition.StartMinute || _clock.TotalMinutes >= definition.EndMinute))
            {
                return false;
            }

            if (definition.RequiredSignals != null)
            {
                foreach (string signal in definition.RequiredSignals)
                {
                    if (!string.IsNullOrEmpty(signal) && !HasObserved(signal))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool StartEvent(EventDefinition definition, int startStep = 0, bool preview = false)
        {
#if UNITY_EDITOR
            if (IsWorkbenchPlaySession && !preview)
            {
                return false;
            }
#endif
            if (_active != null || !GameplayAvailable || _panel == null || !_panel.Open(GetSpeakerKey(definition), SelectOrAdvance, Stop))
            {
                return false;
            }

            _active = definition;
            _isPreview = preview;
            if (!_isPreview)
            {
                _interrupted.Add(definition.Id);
                _deferred.Remove(definition.Id);
                _session?.MarkEventProgressChanged();
            }
            _step = startStep - 1;
            _speaker = definition.Speaker;
            _portrait = _speakerProfiles.TryGetValue(_speaker, out StorySpeakerProfile profile)
                ? profile.GetPortrait(StoryExpression.Neutral)
                : definition.Portrait;
            Advance();
            return true;
        }

        private void Advance()
        {
            if (_active == null || _tween != null || _waiting != null)
            {
                return;
            }

            while (++_step < _active.Steps.Length)
            {
                EventStep step = _active.Steps[_step];
                switch (step.Kind)
                {
                    case EventStepKind.Dialogue:
                        ApplySpeaker(step);
                        _panel.ShowLine(step.TextKey, _portrait, _step + 1 < _active.Steps.Length);
                        return;
                    case EventStepKind.Choice:
                        if (step.Options?.Length > 0)
                        {
                            _panel.ShowChoices(step.TextKey, _portrait, Array.ConvertAll(step.Options, option => option.LabelKey));
                            return;
                        }
                        break;
                    case EventStepKind.WaitForSignal:
                        if (!HasObserved(step.Signal))
                        {
                            _waiting = step;
                            _panel.WaitForGameplay(step.TextKey, _portrait);
                            return;
                        }
                        break;
                    case EventStepKind.Move:
                        Transform actor = FindTarget(step.TargetId);
                        Transform destination = FindTarget(step.DestinationId);
                        if (actor == null || (!string.IsNullOrEmpty(step.DestinationId) && destination == null))
                        {
                            CancelMissingTarget(step);
                            return;
                        }
                        _positions.TryAdd(actor, actor.position);
                        Vector3 position = destination != null ? destination.position : actor.position;
                        BeginTween(actor, position + step.Offset, Mathf.Max(0.01f, step.Duration), false);
                        return;
                    case EventStepKind.FocusCamera:
                        Transform focusTarget = FindTarget(step.TargetId);
                        if (_camera == null || (!step.UseWorldPosition && focusTarget == null))
                        {
                            CancelMissingTarget(step);
                            return;
                        }
                        if (!_cameraOverridden)
                        {
                            _cameraHome = _camera.localPosition;
                            _cameraOverridden = true;
                        }
                        Vector3 focus = step.UseWorldPosition ? (Vector3)step.WorldPosition : focusTarget.position + step.Offset;
                        focus.z = _camera.position.z;
                        BeginTween(_camera, focus, Mathf.Max(0.01f, step.Duration), false);
                        return;
                    case EventStepKind.RestoreCamera:
                        if (_camera != null && _cameraOverridden)
                        {
                            BeginTween(_camera, _cameraHome, Mathf.Max(0.01f, step.Duration), true);
                            return;
                        }
                        break;
                }
            }

            if (_isPreview)
            {
#if UNITY_EDITOR
                PreviewCompleted = true;
#endif
                _panel.Close();
                return;
            }

            _completed.Add(_active.Id);
            _interrupted.Remove(_active.Id);
            foreach (var choice in _currentChoices)
            {
                _choices.Add(new EventChoiceResult { EventId = _active.Id, ChoiceId = choice.Key, Value = choice.Value });
            }
            _session?.MarkEventProgressChanged();
            _panel.Close();
        }

        private string GetSpeakerKey(EventDefinition definition)
        {
            return _speakerProfiles.TryGetValue(definition.Speaker, out StorySpeakerProfile profile)
                ? profile.NameKey : definition.SpeakerKey;
        }

        private void ApplySpeaker(EventStep step)
        {
            bool changedSpeaker = step.Speaker != StorySpeaker.Inherit && step.Speaker != _speaker;
            if (changedSpeaker)
            {
                _speaker = step.Speaker;
                _speakerProfiles.TryGetValue(_speaker, out StorySpeakerProfile speakerProfile);
                _panel.SetSpeaker(speakerProfile != null ? speakerProfile.NameKey : string.Empty);
                _portrait = speakerProfile != null ? speakerProfile.GetPortrait(StoryExpression.Neutral) : null;
            }
            else if (step.Speaker == StorySpeaker.Inherit && !string.IsNullOrEmpty(step.SpeakerKey))
            {
                _speaker = StorySpeaker.Inherit;
                _panel.SetSpeaker(step.SpeakerKey);
            }

            if (step.Expression != StoryExpression.Inherit && _speakerProfiles.TryGetValue(_speaker, out StorySpeakerProfile profile))
            {
                _portrait = profile.GetPortrait(step.Expression);
            }
            else if (step.Speaker == StorySpeaker.Inherit && step.Portrait != null)
            {
                _portrait = step.Portrait;
            }
        }

        private void CancelMissingTarget(EventStep step)
        {
            Debug.LogWarning($"이벤트 {_active.Id}의 {_step} 단계에 필요한 씬 대상을 찾을 수 없습니다: {step.TargetId} / {step.DestinationId}", this);
            _panel.Close();
        }

        private void SelectOrAdvance(int index)
        {
            if (_active == null)
            {
                return;
            }

            if (index >= 0)
            {
                EventStep step = _active.Steps[_step];
                if (step.Kind != EventStepKind.Choice || index >= step.Options.Length)
                {
                    return;
                }

                EventChoice option = step.Options[index];
                if (!string.IsNullOrEmpty(step.ChoiceId))
                {
                    _currentChoices[step.ChoiceId] = option.Result;
                }
                _step = option.NextStep < 0 ? _active.Steps.Length - 1 : option.NextStep - 1;
            }

            Advance();
        }

        private void BeginTween(Transform target, Vector3 destination, float duration, bool local)
        {
            _panel.WaitForEvent();
            Tween tween;
            if (local)
            {
                tween = target.DOLocalMove(destination, duration);
            }
            else if (target.TryGetComponent(out Rigidbody2D body))
            {
                tween = body.DOMove(destination, duration).SetUpdate(UpdateType.Fixed);
            }
            else
            {
                tween = target.DOMove(destination, duration);
            }
            _tween = tween.SetEase(Ease.InOutSine).SetLink(target.gameObject).OnComplete(() =>
            {
                _tween = null;
                Advance();
            }).OnKill(() =>
            {
                if (_tween == tween)
                {
                    _tween = null;
                    _panel.Close();
                }
            });
        }

        private void Stop()
        {
            Tween tween = _tween;
            _tween = null;
            tween?.Kill();
            _waiting = null;
            _currentChoices.Clear();
            foreach (var entry in _positions)
            {
                if (entry.Key != null)
                {
                    entry.Key.position = entry.Value;
                    if (entry.Key.TryGetComponent(out Rigidbody2D body))
                    {
                        body.position = entry.Value;
                    }
                }
            }
            _positions.Clear();
            if (_cameraOverridden && _camera != null)
            {
                _camera.localPosition = _cameraHome;
            }
            _cameraOverridden = false;
            if (!_isPreview && _active != null && _interrupted.Contains(_active.Id))
            {
                _deferred.Add(_active.Id);
            }
            _isPreview = false;
            _active = null;
        }

        private void OnDisable()
        {
            if (_active != null && _panel != null && _panel.IsOpen)
            {
                _panel.Close();
            }
        }

        private void OnDestroy()
        {
            if (_shelf != null)
            {
                _shelf.SuppliesReceived -= HandleSupplies;
            }
            if (_orders != null)
            {
                _orders.OnOrderAdded -= HandleOrder;
            }
        }
    }
}
