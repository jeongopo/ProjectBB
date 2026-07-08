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
        private readonly List<InteractComponent> nearbyInteractables = new();
        private MainCharacter character;
        private InputManager inputManager;
        private InputAction interactAction;

        private void Awake()
        {
            character = GetComponent<MainCharacter>();
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

            Vector2 playerPos = transform.position;
            Vector2 facing = character.FacingDirection;

            InteractComponent best = null;
            float bestScore = float.MinValue;

            foreach (var interactable in nearbyInteractables)
            {
                if (interactable == null) continue;

                Vector2 toTarget = (Vector2)interactable.transform.position - playerPos;
                float distance = Mathf.Max(toTarget.magnitude, 0.001f);
                float dot = Vector2.Dot(facing, toTarget / distance);

                // 바라보는 방향 앞쪽(90도 이내)만 고려
                if (dot < 0f) continue;

                float score = dot / distance;
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
                    float dist = Vector2.Distance(playerPos, interactable.transform.position);
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
