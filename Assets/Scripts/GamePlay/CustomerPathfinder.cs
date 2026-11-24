using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// 가구 점유 칸을 피해 가는 격자 A* 경로 탐색. 칸 수가 작아서 열린 목록은 선형 탐색을 쓰고,
    /// 호출마다 같은 버퍼를 재사용해 할당을 줄인다.
    /// </summary>
    public static class CustomerPathfinder
    {
        private const float DiagonalCost = 1.41421356f;
        private const int SearchPadding = 8;

        private static readonly Vector3Int[] Directions =
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0),
            new Vector3Int(1, 1, 0), new Vector3Int(1, -1, 0), new Vector3Int(-1, 1, 0), new Vector3Int(-1, -1, 0),
        };

        private static readonly Dictionary<Vector3Int, float> GCosts = new Dictionary<Vector3Int, float>();
        private static readonly Dictionary<Vector3Int, Vector3Int> Parents = new Dictionary<Vector3Int, Vector3Int>();
        private static readonly List<Vector3Int> Open = new List<Vector3Int>();
        private static readonly HashSet<Vector3Int> Closed = new HashSet<Vector3Int>();
        private static readonly List<Vector3Int> CellPath = new List<Vector3Int>();

        /// <summary>
        /// from에서 to까지 칸 중심을 잇는 경로를 result에 채운다(시작 칸 제외, 마지막은 to 그대로).
        /// to가 있는 칸은 가구(의자)가 차지하고 있어도 도착지로 허용한다. extraBlocked는 임시로 막을 칸이다.
        /// </summary>
        public static bool TryFindPath(Vector3 from, Vector3 to, HashSet<Vector3Int> extraBlocked, List<Vector3> result)
        {
            result.Clear();

            PlacementGrid placement = PlacementGrid.Instance;
            if (placement == null || placement.Grid == null)
            {
                return false;
            }

            Grid grid = placement.Grid;
            Vector3Int start = ToCell(grid, from);
            Vector3Int goal = ToCell(grid, to);
            if (start == goal)
            {
                result.Add(to);
                return true;
            }

            Vector3Int min = new Vector3Int(Mathf.Min(start.x, goal.x) - SearchPadding, Mathf.Min(start.y, goal.y) - SearchPadding, 0);
            Vector3Int max = new Vector3Int(Mathf.Max(start.x, goal.x) + SearchPadding, Mathf.Max(start.y, goal.y) + SearchPadding, 0);

            GCosts.Clear();
            Parents.Clear();
            Open.Clear();
            Closed.Clear();

            GCosts[start] = 0f;
            Open.Add(start);

            while (Open.Count > 0)
            {
                int bestIndex = 0;
                float bestScore = float.MaxValue;
                for (int i = 0; i < Open.Count; i++)
                {
                    float score = GCosts[Open[i]] + Heuristic(Open[i], goal);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                Vector3Int current = Open[bestIndex];
                Open.RemoveAt(bestIndex);

                if (current == goal)
                {
                    BuildResult(grid, start, goal, from.z, to, result);
                    return true;
                }

                Closed.Add(current);
                ExpandNeighbors(placement, current, goal, min, max, extraBlocked);
            }

            return false;
        }

        private static void ExpandNeighbors(PlacementGrid placement, Vector3Int current, Vector3Int goal, Vector3Int min, Vector3Int max, HashSet<Vector3Int> extraBlocked)
        {
            foreach (Vector3Int direction in Directions)
            {
                Vector3Int next = current + direction;
                if (next.x < min.x || next.x > max.x || next.y < min.y || next.y > max.y || Closed.Contains(next))
                {
                    continue;
                }

                if (!IsPassable(placement, next, goal, extraBlocked))
                {
                    continue;
                }

                bool diagonal = direction.x != 0 && direction.y != 0;
                if (diagonal)
                {
                    // 모서리를 비스듬히 가로지르면 가구를 스치므로, 양옆 칸이 모두 비어 있을 때만 허용한다.
                    Vector3Int sideX = current + new Vector3Int(direction.x, 0, 0);
                    Vector3Int sideY = current + new Vector3Int(0, direction.y, 0);
                    if (!IsPassable(placement, sideX, goal, extraBlocked) || !IsPassable(placement, sideY, goal, extraBlocked))
                    {
                        continue;
                    }
                }

                float cost = GCosts[current] + (diagonal ? DiagonalCost : 1f);
                if (GCosts.TryGetValue(next, out float existing) && cost >= existing)
                {
                    continue;
                }

                GCosts[next] = cost;
                Parents[next] = current;
                if (!Open.Contains(next))
                {
                    Open.Add(next);
                }
            }
        }

        private static bool IsPassable(PlacementGrid placement, Vector3Int cell, Vector3Int goal, HashSet<Vector3Int> extraBlocked)
        {
            if (cell == goal)
            {
                return true;
            }

            return placement.IsWalkable(cell) && (extraBlocked == null || !extraBlocked.Contains(cell));
        }

        private static void BuildResult(Grid grid, Vector3Int start, Vector3Int goal, float z, Vector3 destination, List<Vector3> result)
        {
            CellPath.Clear();
            Vector3Int cell = goal;
            while (cell != start)
            {
                CellPath.Add(cell);
                cell = Parents[cell];
            }

            for (int i = CellPath.Count - 1; i >= 1; i--)
            {
                Vector3 center = grid.GetCellCenterWorld(CellPath[i]);
                result.Add(new Vector3(center.x, center.y, z));
            }

            result.Add(destination);
        }

        private static Vector3Int ToCell(Grid grid, Vector3 position)
        {
            Vector3Int cell = grid.WorldToCell(position);
            cell.z = 0;
            return cell;
        }

        private static float Heuristic(Vector3Int a, Vector3Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx + dy + (DiagonalCost - 2f) * Mathf.Min(dx, dy);
        }
    }
}
