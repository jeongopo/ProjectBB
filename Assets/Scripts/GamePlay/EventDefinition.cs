using System;
using UnityEngine;

namespace GamePlay
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "ProjectBB/Story Event")]
    public sealed class EventDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _prerequisite;
        [SerializeField] private int _priority;
        [SerializeField] private bool _automatic;
        [SerializeField] private string _sceneName;
        [SerializeField] private string[] _requiredSignals;
        [SerializeField, Min(1)] private int _earliestDay = 1;
        [SerializeField, Range(0, 1439)] private int _startMinute;
        [SerializeField, Range(1, 1440)] private int _endMinute = 1440;
        [SerializeField] private StorySpeaker _speaker;
        [SerializeField, HideInInspector] private string _speakerKey;
        [SerializeField, HideInInspector] private Sprite _portrait;
        [SerializeField] private EventStep[] _steps;

        internal string Id => _id;
        internal string Prerequisite => _prerequisite;
        internal int Priority => _priority;
        internal bool Automatic => _automatic;
        internal string SceneName => _sceneName;
        internal string[] RequiredSignals => _requiredSignals;
        internal int EarliestDay => _earliestDay;
        internal int StartMinute => _startMinute;
        internal int EndMinute => _endMinute;
        internal StorySpeaker Speaker => _speaker;
        internal string SpeakerKey => _speakerKey;
        internal Sprite Portrait => _portrait;
        internal EventStep[] Steps => _steps;
    }

    public enum EventStepKind
    {
        Dialogue,
        Move,
        FocusCamera,
        RestoreCamera,
        WaitForSignal,
        Choice,
    }

    [Serializable]
    internal struct EventChoice
    {
        [SerializeField] private string _labelKey;
        [SerializeField] private string _result;
        [SerializeField] private int _nextStep;

        internal string LabelKey => _labelKey;
        internal string Result => _result;
        internal int NextStep => _nextStep;
    }

    [Serializable]
    internal sealed class EventStep
    {
        [SerializeField] private EventStepKind _kind;
        [SerializeField] private string _textKey;
        [SerializeField] private string _signal;
        [SerializeField] private StorySpeaker _speaker;
        [SerializeField] private StoryExpression _expression;
        [SerializeField, HideInInspector] private Sprite _portrait;
        [SerializeField, HideInInspector] private string _speakerKey;
        [SerializeField] private string _choiceId;
        [SerializeField] private EventChoice[] _options;
        [SerializeField] private string _targetId;
        [SerializeField] private string _destinationId;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private bool _useWorldPosition;
        [SerializeField] private Vector2 _worldPosition;
        [SerializeField, Min(0.01f)] private float _duration = 0.65f;

        internal EventStepKind Kind => _kind;
        internal string TextKey => _textKey;
        internal string Signal => _signal;
        internal StorySpeaker Speaker => _speaker;
        internal StoryExpression Expression => _expression;
        internal Sprite Portrait => _portrait;
        internal string SpeakerKey => _speakerKey;
        internal string ChoiceId => _choiceId;
        internal EventChoice[] Options => _options;
        internal string TargetId => _targetId;
        internal string DestinationId => _destinationId;
        internal Vector3 Offset => _offset;
        internal bool UseWorldPosition => _useWorldPosition;
        internal Vector2 WorldPosition => _worldPosition;
        internal float Duration => _duration;
    }
}
