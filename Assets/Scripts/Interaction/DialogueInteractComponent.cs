using System;
using System.Collections.Generic;
using DG.Tweening;
using GamePlay;
using UnityEngine;

namespace Interaction
{
    public sealed class DialogueInteractComponent : InteractComponent
    {
        [Serializable]
        private sealed class Choice
        {
            [SerializeField] internal string _labelKey;
            [SerializeField] internal string _next;
        }

        [Serializable]
        private sealed class Node
        {
            [SerializeField] internal string _id;
            [SerializeField] internal string _textKey;
            [SerializeField] internal Sprite _portrait;
            [SerializeField] internal string _next;
            [SerializeField] internal Choice[] _choices;
            [SerializeField] internal Transform _moveTarget;
            [SerializeField] internal Vector3 _moveOffset;
            [SerializeField] internal float _moveDuration = 0.65f;
            [SerializeField] internal bool _completeIntroduction;
        }

        [SerializeField] private string _speakerKey;
        [SerializeField] private Sprite _portrait;
        [SerializeField] private string _introductionId;
        [SerializeField] private string _introductionStart;
        [SerializeField] private string _repeatStart;
        [SerializeField] private Node[] _nodes;

        private readonly Dictionary<string, Node> _nodesById = new Dictionary<string, Node>();
        private readonly Dictionary<Transform, Vector3> _originalPositions = new Dictionary<Transform, Vector3>();
        private DialoguePanel _panel;
        private GameSession _session;
        private EventRunner _eventRunner;
        private Tween _movement;
        private Node _current;
        private Sprite _currentPortrait;
        private bool _introCompleted;

        public override string PromptKey => "STR_Interact_Talk";
        public override bool CanInteract => base.CanInteract && _panel != null && !_panel.IsOpen && _nodesById.Count > 0;

        protected override void Start()
        {
            _panel = FindFirstObjectByType<DialoguePanel>(FindObjectsInactive.Include);
            _session = FindFirstObjectByType<GameSession>();
            if (_nodes == null)
            {
                return;
            }

            foreach (Node node in _nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node._id))
                {
                    _nodesById.Add(node._id, node);
                }
            }

            EventRunner runner = FindFirstObjectByType<EventRunner>();
            if (runner != null)
            {
                runner.Bind(gameObject.name, transform);
                if (runner.HasDefinition(_introductionId))
                {
                    _eventRunner = runner;
                }
            }
        }

        protected override void OnInteract()
        {
            bool completed = _introCompleted || _session?.HasCompletedDialogue(_introductionId) == true;
            if (_eventRunner != null)
            {
                if (_eventRunner.TryResume(_speakerKey))
                {
                    return;
                }

                completed = _eventRunner.IsComplete(_introductionId);
                if (!completed)
                {
                    _eventRunner.TryStart(_introductionId);
                    return;
                }
            }
            if (!_panel.Open(_speakerKey, Advance, EndDialogue))
            {
                return;
            }

            _currentPortrait = _portrait;
            Enter(completed || string.IsNullOrEmpty(_introductionStart) ? _repeatStart : _introductionStart);
        }

        private void Enter(string id)
        {
            if (string.IsNullOrEmpty(id) || !_nodesById.TryGetValue(id, out _current))
            {
                _panel.Close();
                return;
            }

            Node node = _current;
            if (node._completeIntroduction)
            {
                _introCompleted = true;
                if (!string.IsNullOrEmpty(_introductionId))
                {
                    _session?.CompleteDialogue(_introductionId);
                }
                _currentPortrait = _portrait;
                Enter(node._next);
                return;
            }

            if (node._moveTarget != null)
            {
                PlayMovement(node);
                return;
            }

            if (node._portrait != null)
            {
                _currentPortrait = node._portrait;
            }

            if (node._choices?.Length > 0)
            {
                string[] labels = Array.ConvertAll(node._choices, choice => choice._labelKey);
                _panel.ShowChoices(node._textKey, _currentPortrait, labels);
            }
            else
            {
                _panel.ShowLine(node._textKey, _currentPortrait, !string.IsNullOrEmpty(node._next));
            }
        }

        private void PlayMovement(Node node)
        {
            Transform target = node._moveTarget;
            _originalPositions.TryAdd(target, target.position);
            _panel.WaitForEvent();
            _movement = target.DOMove(target.position + node._moveOffset, Mathf.Max(0.01f, node._moveDuration))
                .SetEase(Ease.InOutSine)
                .SetLink(target.gameObject)
                .OnComplete(() =>
                {
                    _movement = null;
                    Enter(node._next);
                });
        }

        private void Advance(int choice)
        {
            if (_current == null || _movement != null)
            {
                return;
            }

            Enter(choice < 0 ? _current._next : _current._choices[choice]._next);
        }

        private void EndDialogue()
        {
            _movement?.Kill();
            _movement = null;
            foreach (var entry in _originalPositions)
            {
                if (entry.Key != null)
                {
                    entry.Key.position = entry.Value;
                }
            }
            _originalPositions.Clear();
            _current = null;
        }

        private void OnDisable()
        {
            if (_current != null && _panel != null)
            {
                _panel.Close();
            }
        }
    }
}
