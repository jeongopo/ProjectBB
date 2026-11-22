using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 그리드에 설치되는 가구. 루트 Transform은 점유 영역의 아래쪽 중앙(발치)에 놓인다.
    /// </summary>
    public class Furniture : MonoBehaviour
    {
        private const int SortingOrderPerUnit = YSorter.OrderPerUnit;

        [Header("Data")]
        [SerializeField] private string _furnitureId;
        [SerializeField] private Vector2Int _size = Vector2Int.one;

        private SpriteRenderer[] _renderers;
        private int[] _baseOrders;
        private PlacementGrid _placementGrid;
        private bool _isRegistered;

        public string FurnitureId => _furnitureId;
        public Vector2Int Size => _size;
        public Vector3Int Origin { get; private set; }

        private void Awake()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            SyncCollider();
        }

        private void OnValidate()
        {
            _size = new Vector2Int(Mathf.Max(1, _size.x), Mathf.Max(1, _size.y));
            SyncCollider();
        }

        // 이동 차단 범위는 점유 칸(Size)과 항상 같다. 콜라이더를 직접 고쳐도 Size 값으로 덮어쓴다.
        private void SyncCollider()
        {
            if (TryGetComponent(out BoxCollider2D box))
            {
                box.size = new Vector2(_size.x, _size.y);
                box.offset = new Vector2(0f, _size.y * 0.5f);
            }
        }

        private void Start()
        {
            if (_isRegistered)
            {
                return;
            }

            // 에디터에서 미리 배치한 가구는 씬 시작 시 자기 위치로 점유 정보를 등록한다.
            PlacementGrid placementGrid = PlacementGrid.Instance;
            if (placementGrid == null)
            {
                return;
            }

            Vector3Int origin = placementGrid.PositionToOrigin(transform.position, _size);
            if (!PlaceAt(placementGrid, origin))
            {
                Debug.LogWarning($"[Furniture] {name}: 점유 칸이 겹치거나 설치 불가 영역입니다 ({origin}).", this);
            }
        }

        private void OnDestroy()
        {
            if (_placementGrid != null)
            {
                _placementGrid.Unregister(this);
            }
        }

        public bool PlaceAt(PlacementGrid placementGrid, Vector3Int origin)
        {
            if (!placementGrid.Register(this, origin))
            {
                return false;
            }

            _placementGrid = placementGrid;
            _isRegistered = true;
            Origin = origin;
            transform.position = placementGrid.OriginToPosition(origin, _size);
            ApplySorting();
            return true;
        }

        public void Setup(string furnitureId, Vector2Int size)
        {
            _furnitureId = furnitureId;
            _size = size;
        }

        private void ApplySorting()
        {
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<SpriteRenderer>();
            }

            if (_baseOrders == null)
            {
                _baseOrders = new int[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _baseOrders[i] = _renderers[i].sortingOrder;
                }
            }

            int order = -Mathf.RoundToInt(transform.position.y * SortingOrderPerUnit);
            for (int i = 0; i < _renderers.Length; i++)
            {
                _renderers[i].sortingOrder = order + _baseOrders[i];
            }
        }

        [ContextMenu("Snap To Grid")]
        public void SnapToGrid()
        {
            PlacementGrid placementGrid = FindFirstObjectByType<PlacementGrid>();
            if (placementGrid == null)
            {
                return;
            }

            Vector3Int origin = placementGrid.PositionToOrigin(transform.position, _size);
            transform.position = placementGrid.OriginToPosition(origin, _size);
        }

        private void OnDrawGizmosSelected()
        {
            PlacementGrid placementGrid = FindFirstObjectByType<PlacementGrid>();
            Grid grid = placementGrid != null ? placementGrid.Grid : null;
            Vector3 cellSize = grid != null ? grid.cellSize : Vector3.one;
            Vector3 footprint = new Vector3(_size.x * cellSize.x, _size.y * cellSize.y, 0f);
            Vector3 center = transform.position + new Vector3(0f, footprint.y * 0.5f, 0f);

            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.25f);
            Gizmos.DrawCube(center, footprint);
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 1f);
            Gizmos.DrawWireCube(center, footprint);
        }
    }
}
