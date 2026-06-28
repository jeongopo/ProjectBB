using UnityEngine;
using UnityEngine.InputSystem;
using GamePlay;
using GameEnumDefines;

namespace Interaction
{
    public abstract class InteractComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private Vector2 interactSize = Vector2.one;

        private InputManager inputManager;
        private InputAction interactAction;
        private bool playerInRange = false;

        protected virtual void Awake()
        {
            var child = new GameObject("InteractionRange");
            child.transform.SetParent(transform, false);
            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = interactSize;
            var forwarder = child.AddComponent<TriggerForwarder>();
            forwarder.Init(this);
        }

        protected virtual void Start()
        {
            inputManager = FindFirstObjectByType<InputManager>();

            var actionMap = inputManager?.GetCurrentActionMap();
            interactAction = actionMap?.FindAction("Interact");
            if (interactAction != null)
                interactAction.started += OnInteractPerformed;

            Debug.Log($"[InteractComponent] {name} | inputManager: {inputManager != null} | actionMap: {actionMap?.name} | interactAction: {interactAction != null}");
        }

        private void OnDestroy()
        {
            if (interactAction != null)
                interactAction.started -= OnInteractPerformed;
        }

        internal void HandleTriggerEnter(Collider2D other)
        {
            Debug.Log($"[InteractComponent] TriggerEnter: {other.name} tag={other.tag}");
            if (!other.CompareTag("Player")) return;
            playerInRange = true;
            OnPlayerEnter();
        }

        internal void HandleTriggerExit(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            playerInRange = false;
            OnPlayerExit();
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            Debug.Log($"[InteractComponent] OnInteractPerformed | playerInRange: {playerInRange} | state: {(inputManager != null ? inputManager.CurrentInputState.ToString() : "null")}");
            if (!playerInRange) return;
            if (inputManager != null && inputManager.CurrentInputState != InputState.Default) return;
            Interact();
        }

        public void Interact() => OnInteract();

        protected abstract void OnInteract();

        protected virtual void OnPlayerEnter() {}
        protected virtual void OnPlayerExit() {}
    }

    internal class TriggerForwarder : MonoBehaviour
    {
        private InteractComponent owner;

        internal void Init(InteractComponent owner) => this.owner = owner;

        private void OnTriggerEnter2D(Collider2D other) => owner?.HandleTriggerEnter(other);
        private void OnTriggerExit2D(Collider2D other) => owner?.HandleTriggerExit(other);
    }
}
