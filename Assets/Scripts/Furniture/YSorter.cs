using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 발 위치(Y)를 기준으로 SpriteRenderer의 Sorting Order를 갱신한다. 가구와 같은 Sorting Layer에 있어야
    /// 캐릭터가 가구 뒤로 지나갈 때 가려진다.
    /// </summary>
    public class YSorter : MonoBehaviour
    {
        public const int OrderPerUnit = 100;

        [Header("Foot Position")]
        [Tooltip("지정하면 이 콜라이더의 아래쪽 끝을 발 위치로 쓴다. 비우면 Transform Y + Foot Offset Y.")]
        [SerializeField] private Collider2D _footCollider;
        [SerializeField] private float _footOffsetY;

        private SpriteRenderer[] _renderers;
        private int[] _baseOrders;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            _baseOrders = new int[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _baseOrders[i] = _renderers[i].sortingOrder;
            }
        }

        private void LateUpdate()
        {
            float footY = _footCollider != null
                ? _footCollider.bounds.min.y
                : transform.position.y + _footOffsetY;
            int order = -Mathf.RoundToInt(footY * OrderPerUnit);

            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sortingOrder = order + _baseOrders[i];
            }
        }
    }
}
