using System.Collections.Generic;
using GameEnumDefines;
using Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GamePlay
{
    [RequireComponent(typeof(MainCharacter))]
    [DefaultExecutionOrder(120)]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Target Selection")]
        [SerializeField, Range(-1f, 1f)] private float _frontDotThreshold = 0.3f;
        [SerializeField, Min(0.01f)] private float _distanceBias = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _switchAdvantage = 0.2f;
        [SerializeField] private Collider2D _footCollider;

        private readonly List<InteractComponent> _nearby = new List<InteractComponent>();
        private MainCharacter _character;
        private InputManager _input;
        private InputAction _interact;
        private InteractComponent _focused;
        private InteractionFocusView _view;

        private void Awake()
        {
            _character = GetComponent<MainCharacter>();
            if (_footCollider == null)
            {
                TryGetComponent(out _footCollider);
            }
        }

        private void Start()
        {
            _input = FindFirstObjectByType<InputManager>();
            InputActionMap actions = _input?.GetActionMap(InputState.Default);
            _interact = actions?.FindAction("Interact");
            if (_interact == null)
            {
                return;
            }

            _view = gameObject.AddComponent<InteractionFocusView>();
            _interact.started += OnInteractPerformed;
        }

        private void OnDestroy()
        {
            if (_interact != null)
            {
                _interact.started -= OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            _focused = null;
            if (_view != null)
            {
                _view.Show(null);
            }
        }

        private void LateUpdate()
        {
            _focused = CanSelect() ? FindBestInteractable() : null;
            if (_view != null)
            {
                _view.Show(_focused);
            }
        }

        public void Register(InteractComponent interactable)
        {
            if (!_nearby.Contains(interactable))
            {
                _nearby.Add(interactable);
            }
        }

        public void Unregister(InteractComponent interactable)
        {
            _nearby.Remove(interactable);
            if (_focused == interactable)
            {
                _focused = null;
                if (_view != null)
                {
                    _view.Show(null);
                }
            }
        }

        private bool CanSelect() => isActiveAndEnabled && _interact != null &&
            (_input == null || _input.CurrentInputState == InputState.Default) && Time.timeScale > 0f;

        private void OnInteractPerformed(InputAction.CallbackContext _)
        {
            if (CanSelect() && _focused != null && _focused.CanInteract)
            {
                _focused.Interact();
            }
        }

        private InteractComponent FindBestInteractable()
        {
            Vector2 origin = _footCollider != null ? (Vector2)_footCollider.bounds.center : (Vector2)transform.position;
            Vector2 facing = _character.FacingDirection;
            InteractComponent best = null;
            float bestScore = 0f;
            bool bestInFront = false;
            for (int i = _nearby.Count - 1; i >= 0; i--)
            {
                InteractComponent candidate = _nearby[i];
                if (candidate == null)
                {
                    _nearby.RemoveAt(i);
                    continue;
                }
                if (!candidate.CanInteract)
                {
                    continue;
                }

                Vector2 offset = candidate.GetClosestPoint(origin) - origin;
                float distance = offset.magnitude;
                float dot = distance < 0.0001f ? 1f : Vector2.Dot(facing, offset / distance);
                bool inFront = dot >= _frontDotThreshold;
                float score = (inFront ? Mathf.Max(0.01f, dot) : 1f) / (distance + Mathf.Max(0.01f, _distanceBias));
                if (candidate == _focused)
                {
                    score *= 1f + _switchAdvantage;
                }

                if (best == null || (inFront && !bestInFront) || (inFront == bestInFront && score > bestScore))
                {
                    best = candidate;
                    bestScore = score;
                    bestInFront = inFront;
                }
            }

            return best;
        }
    }
}
