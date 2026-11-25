using UnityEngine;
using GamePlay;

namespace Interaction
{
    public abstract class InteractComponent : MonoBehaviour, IInteractable
    {
        [Tooltip("Furniture가 없는 오브젝트의 상호작용 범위 크기.")]
        [SerializeField] private Vector2 interactSize = Vector2.one;
        [Tooltip("Furniture가 있으면 점유 칸 바깥으로 이만큼 범위를 넓힌다.")]
        [SerializeField] private Vector2 interactMargin = new Vector2(0.5f, 0.5f);
        protected virtual void Start()
        {
        }

        private Collider2D _bodyCollider;
        public SpriteRenderer[] InteractionVisuals { get; private set; }
        public virtual bool CanInteract => isActiveAndEnabled;
        public virtual string PromptKey => "STR_Interact_Default";

        /// <summary>플레이어 기준으로 이 오브젝트 몸체에서 가장 가까운 점. 콜라이더가 없으면 위치를 쓴다.</summary>
        public Vector2 GetClosestPoint(Vector2 from)
        {
            return _bodyCollider != null ? _bodyCollider.ClosestPoint(from) : (Vector2)transform.position;
        }

        protected virtual void Awake()
        {
            TryGetComponent(out _bodyCollider);
            InteractionVisuals = GetComponentsInChildren<SpriteRenderer>(true);

            var child = new GameObject("InteractionRange");
            child.transform.SetParent(transform, false);
            var col = child.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            // 가구는 점유 칸에 여백을 더한 크기를 쓴다. 칸이 커져도 범위가 따라오고, 옆면에서도 닿는다.
            if (TryGetComponent(out Furniture furniture))
            {
                Vector2 footprint = new Vector2(furniture.Size.x, furniture.Size.y);
                col.size = footprint + interactMargin * 2f;
                col.offset = new Vector2(0f, footprint.y * 0.5f);
            }
            else
            {
                col.size = interactSize;
            }

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

        public void Interact()
        {
            if (CanInteract)
            {
                OnInteract();
            }
        }

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
