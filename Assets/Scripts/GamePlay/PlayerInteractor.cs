using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GameEnumDefines;
using Interaction;

namespace GamePlay
{
    [RequireComponent(typeof(MainCharacter))]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Target Selection")]
        [Tooltip("바라보는 방향과의 내적이 이 값 미만인 오브젝트는 정면 후보에서 제외한다. 0.3은 약 70도.")]
        [SerializeField] private float _frontDotThreshold = 0.3f;
        [Tooltip("거리 보정값. 클수록 거리보다 방향을 더 중시한다.")]
        [SerializeField] private float _distanceBias = 0.25f;
        [Tooltip("지정하면 이 콜라이더 중심을 플레이어 기준점으로 쓴다. 비우면 같은 오브젝트의 Collider2D.")]
        [SerializeField] private Collider2D _footCollider;

        private readonly List<InteractComponent> nearbyInteractables = new();
        private MainCharacter character;
        private InputManager inputManager;
        private InputAction interactAction;

        private void Awake()
        {
            character = GetComponent<MainCharacter>();
            if (_footCollider == null)
            {
                TryGetComponent(out _footCollider);
            }
        }

        private void Start()
        {
            inputManager = FindFirstObjectByType<InputManager>();
            var actionMap = inputManager?.GetCurrentActionMap();
            interactAction = actionMap?.FindAction("Interact");
            if (interactAction != null)
                interactAction.started += OnInteractPerformed;
        }

        private void OnDestroy()
        {
            if (interactAction != null)
                interactAction.started -= OnInteractPerformed;
        }

        public void Register(InteractComponent interactable)
        {
            if (!nearbyInteractables.Contains(interactable))
                nearbyInteractables.Add(interactable);
        }

        public void Unregister(InteractComponent interactable)
        {
            nearbyInteractables.Remove(interactable);
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (inputManager != null && inputManager.CurrentInputState != InputState.Default) return;

            FindBestInteractable()?.Interact();
        }

        private InteractComponent FindBestInteractable()
        {
            if (nearbyInteractables.Count == 0) return null;

            Vector2 origin = _footCollider != null ? (Vector2)_footCollider.bounds.center : (Vector2)transform.position;
            Vector2 facing = character.FacingDirection;

            InteractComponent best = null;
            float bestScore = float.MinValue;

            foreach (var interactable in nearbyInteractables)
            {
                if (interactable == null) continue;

                // 큰 오브젝트도 정면에 닿아 있으면 정면으로 보도록, 중심이 아니라 몸체에서 가장 가까운 점을 기준으로 한다.
                Vector2 toTarget = interactable.GetClosestPoint(origin) - origin;
                float distance = toTarget.magnitude;
                float dot = distance < 0.0001f ? 1f : Vector2.Dot(facing, toTarget / distance);

                if (dot < _frontDotThreshold) continue;

                float score = dot / (distance + _distanceBias);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }

            // 앞쪽에 없으면 가장 가까운 오브젝트로 폴백
            if (best == null)
            {
                float minDist = float.MaxValue;
                foreach (var interactable in nearbyInteractables)
                {
                    if (interactable == null) continue;
                    float dist = Vector2.Distance(origin, interactable.GetClosestPoint(origin));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        best = interactable;
                    }
                }
            }

            return best;
        }
    }
}
