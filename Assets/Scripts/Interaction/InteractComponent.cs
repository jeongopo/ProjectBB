using UnityEngine;
using GamePlay;
using UnityEngine.InputSystem;

namespace Interaction
{
    public abstract class InteractComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private Vector2 interactSize = Vector2.one;
        protected virtual void Start()
        {
        }

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

        internal void HandleTriggerEnter(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            other.GetComponentInParent<PlayerInteractor>()?.Register(this);
            OnPlayerEnter();
        }

        internal void HandleTriggerExit(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            other.GetComponentInParent<PlayerInteractor>()?.Unregister(this);
            OnPlayerExit();
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
