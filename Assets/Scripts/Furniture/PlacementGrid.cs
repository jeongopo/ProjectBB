using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GamePlay
{
    /// <summary>
    /// 가구 점유 칸을 관리한다. 에디터에서 배치한 가구와 런타임에 설치한 가구가 같은 점유 정보를 공유한다.
    /// </summary>
    public class PlacementGrid : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Grid _grid;

        [Header("Placement Area")]
        [Tooltip("이 타일맵에 타일이 있는 칸에만 설치할 수 있다. 비워 두면 모든 칸이 허용된다.")]
        [SerializeField] private Tilemap _buildableTilemap;
        [Tooltip("타일이 있는 칸에는 설치할 수 없다 (벽 등).")]
        [SerializeField] private Tilemap[] _blockedTilemaps;

        private readonly Dictionary<Vector3Int, Furniture> _occupied = new Dictionary<Vector3Int, Furniture>();

        public static PlacementGrid Instance { get; private set; }

        public Grid Grid
        {
            get
            {
                if (_grid == null)
                {
                    _grid = FindFirstObjectByType<Grid>();
                }

                return _grid;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>가구 루트 위치(발치 중앙)를 점유 시작 칸(좌하단)으로 변환한다.</summary>
        public Vector3Int PositionToOrigin(Vector3 worldPosition, Vector2Int size)
        {
            Vector3 cellSize = Grid.cellSize;
            Vector3 bottomLeft = worldPosition - new Vector3(size.x * cellSize.x * 0.5f, 0f, 0f);
            return Grid.WorldToCell(bottomLeft + new Vector3(cellSize.x * 0.5f, cellSize.y * 0.5f, 0f));
        }

        /// <summary>점유 시작 칸을 가구 루트 위치(발치 중앙)로 변환한다.</summary>
        public Vector3 OriginToPosition(Vector3Int origin, Vector2Int size)
        {
            Vector3 cellSize = Grid.cellSize;
            Vector3 bottomLeft = Grid.CellToWorld(origin);
            return bottomLeft + new Vector3(size.x * cellSize.x * 0.5f, 0f, 0f);
        }

        public bool CanPlace(Vector3Int origin, Vector2Int size, Furniture ignore = null)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3Int cell = new Vector3Int(origin.x + x, origin.y + y, origin.z);
                    if (!IsBuildable(cell))
                    {
                        return false;
                    }

                    if (_occupied.TryGetValue(cell, out Furniture other) && other != ignore)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool Register(Furniture furniture, Vector3Int origin)
        {
            if (!CanPlace(origin, furniture.Size, furniture))
            {
                return false;
            }

            Unregister(furniture);
            for (int x = 0; x < furniture.Size.x; x++)
            {
                for (int y = 0; y < furniture.Size.y; y++)
                {
                    _occupied[new Vector3Int(origin.x + x, origin.y + y, origin.z)] = furniture;
                }
            }

            return true;
        }

        public void Unregister(Furniture furniture)
        {
            List<Vector3Int> cells = null;
            foreach (KeyValuePair<Vector3Int, Furniture> pair in _occupied)
            {
                if (pair.Value == furniture)
                {
                    cells ??= new List<Vector3Int>();
                    cells.Add(pair.Key);
                }
            }

            if (cells == null)
            {
                return;
            }

            foreach (Vector3Int cell in cells)
            {
                _occupied.Remove(cell);
            }
        }

        public Furniture GetAt(Vector3Int cell)
        {
            _occupied.TryGetValue(cell, out Furniture furniture);
            return furniture;
        }

        private bool IsBuildable(Vector3Int cell)
        {
            if (_buildableTilemap != null && !_buildableTilemap.HasTile(cell))
            {
                return false;
            }

            if (_blockedTilemaps != null)
            {
                foreach (Tilemap blocked in _blockedTilemaps)
                {
                    if (blocked != null && blocked.HasTile(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
