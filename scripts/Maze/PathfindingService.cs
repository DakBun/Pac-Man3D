using System;
using Godot;
using System.Collections.Generic;
using PacManGame.Maze;

namespace PacManGame.Maze;

/// <summary>
/// Dịch vụ tìm đường sử dụng thuật toán A* (A-Star).
/// Trả về danh sách các ô lưới từ điểm xuất phát đến điểm đích.
/// </summary>
public static class PathfindingService
{
    /// <summary>
    /// Tìm đường đi ngắn nhất từ start đến end trên lưới.
    /// </summary>
    public static List<Vector2I>? FindPath(MazeGrid mazeGrid, Vector2I start, Vector2I end)
    {
        if (mazeGrid == null)
        {
            GD.PrintErr("[PathfindingService] MazeGrid không được khởi tạo.");
            return null;
        }

        if (!mazeGrid.IsWalkable(start) || !mazeGrid.IsWalkable(end))
        {
            return null;
        }

        int rows = mazeGrid.GetRows();
        int cols = mazeGrid.GetCols();

        // Các hướng có thể đi: Lên, Xuống, Trái, Phải
        Vector2I[] directions =
        [
            new Vector2I(0, -1),
            new Vector2I(0, 1),
            new Vector2I(-1, 0),
            new Vector2I(1, 0)
        ];

        // Node mở (OpenSet) và Node đóng (ClosedSet)
        var openSet = new PriorityQueue<PathNode, float>();
        var closedSet = new HashSet<Vector2I>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, float>();

        gScore[start] = 0f;

        var startNode = new PathNode(start, 0f, Heuristic(start, end));
        openSet.Enqueue(startNode, startNode.F);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();

            if (current.Position == end)
            {
                return ReconstructPath(cameFrom, current.Position);
            }

            closedSet.Add(current.Position);

            foreach (Vector2I dir in directions)
            {
                Vector2I neighbor = current.Position + dir;

                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                if (!mazeGrid.IsWalkable(neighbor))
                {
                    continue;
                }

                // Chi phí di chuyển giữa các ô láng giềng là 1 (grid đơn vị)
                float tentativeGScore = gScore[current.Position] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current.Position;
                    gScore[neighbor] = tentativeGScore;
                    float fScore = tentativeGScore + Heuristic(neighbor, end);

                    PathNode neighborNode = new PathNode(neighbor, tentativeGScore, fScore);
                    openSet.Enqueue(neighborNode, fScore);
                }
            }
        }

        // Không tìm thấy đường
        return null;
    }

    /// <summary>
    /// Hàm heuristic (Manhattan Distance) để ước lượng chi phí từ a đến b.
    /// Phù hợp với lưới chỉ cho phép di chuyển 4 hướng (không qua đường chéo).
    /// </summary>
    private static float Heuristic(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Tái tạo đường đi từ bảng cameFrom.
    /// </summary>
    private static List<Vector2I> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        var totalPath = new List<Vector2I> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }

    /// <summary>
    /// Lớp con trỏ lưu trạng thái của một ô trong thuật toán A*.
    /// </summary>
    private class PathNode(Vector2I position, float g, float f) : IComparable<PathNode>
    {
        public Vector2I Position { get; } = position;
        public float G { get; } = g; // Chi phí từ start đến node hiện tại
        public float F { get; } = f; // Chi phí tổng (G + H)

        public int CompareTo(PathNode? other)
        {
            if (other == null) return 1;
            return F.CompareTo(other.F);
        }
    }
}
