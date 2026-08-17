using System.Collections.Generic;
using Godot;

namespace PacMan.Core;

/// <summary>
/// Cầu nối duy nhất giữa toạ độ lưới (Vector2I) và toạ độ thế giới (Vector3).
///
/// Quy ước trục: cột x của lưới -> trục X, hàng y của lưới -> trục Z. Sàn tại Y = 0.
/// Mê cung được căn giữa gốc toạ độ.
///
/// KHÔNG nơi nào khác trong dự án được tự nhân/chia CellSize. Mọi quy đổi đi qua đây.
/// </summary>
public class GridSystem
{
    /// <summary>Cạnh một ô, đơn vị thế giới. Hằng số quy đổi duy nhất của dự án.</summary>
    public const float CellSize = 2.0f;

    private readonly MazeData _maze;
    private readonly float _offsetX;
    private readonly float _offsetZ;

    public int Width => _maze.Width;
    public int Height => _maze.Height;
    public MazeData Maze => _maze;

    public GridSystem(MazeData maze)
    {
        _maze = maze;
        _offsetX = (maze.Width - 1) * 0.5f;
        _offsetZ = (maze.Height - 1) * 0.5f;
    }

    /// <summary>Tâm ô trong toạ độ thế giới, Y = 0.</summary>
    public Vector3 CellToWorld(Vector2I cell) => new(
        (cell.X - _offsetX) * CellSize,
        0f,
        (cell.Y - _offsetZ) * CellSize);

    public Vector3 CellToWorld(int x, int y) => CellToWorld(new Vector2I(x, y));

    /// <summary>Nghịch đảo của CellToWorld. Bỏ qua thành phần Y.</summary>
    public Vector2I WorldToCell(Vector3 world) => new(
        Mathf.RoundToInt(world.X / CellSize + _offsetX),
        Mathf.RoundToInt(world.Z / CellSize + _offsetZ));

    /// <summary>Pac-Man đi được: không phải tường, không phải cửa, không phải nhà ma.</summary>
    public bool IsWalkable(Vector2I cell)
    {
        if (_maze.IsWall(cell)) return false;
        return _maze.GetCell(cell) != CellType.GhostHouse;
    }

    public bool IsWalkable(int x, int y) => IsWalkable(new Vector2I(x, y));

    /// <summary>Ghost đi được: mọi ô trừ tường. Bao gồm cửa và trong nhà ma.</summary>
    public bool IsWalkableForGhost(Vector2I cell) => !_maze.IsWallForGhost(cell);

    private static readonly Vector2I[] Directions =
    {
        Vector2I.Up,    // (0, -1)
        Vector2I.Down,  // (0,  1)
        Vector2I.Left,  // (-1, 0)
        Vector2I.Right, // ( 1, 0)
    };

    /// <summary>4 hướng trực giao. Không bao giờ đường chéo — Pac-Man chỉ đi 4 hướng.</summary>
    public Vector2I[] GetWalkableNeighbors(Vector2I cell)
    {
        var result = new List<Vector2I>(4);
        foreach (Vector2I d in Directions)
        {
            Vector2I n = cell + d;
            if (IsWalkable(n)) result.Add(n);
        }
        return result.ToArray();
    }

    public Vector2I[] GetWalkableNeighborsForGhost(Vector2I cell)
    {
        var result = new List<Vector2I>(4);
        foreach (Vector2I d in Directions)
        {
            Vector2I n = cell + d;
            if (IsWalkableForGhost(n)) result.Add(n);
        }
        return result.ToArray();
    }

    // ---- Công thức ID dùng chung cho AStar3D (ngày 2). Không được đổi. ----

    public int CellToId(Vector2I cell) => cell.Y * Width + cell.X;

    public Vector2I IdToCell(int id) => new(id % Width, id / Width);

    /// <summary>
    /// Tự kiểm tra tính nhất quán của các phép quy đổi.
    /// Gọi một lần lúc khởi động, xem output trong Godot Output panel.
    /// </summary>
    public void SelfTest()
    {
        bool ok = true;

        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            var c = new Vector2I(x, y);

            Vector2I back = WorldToCell(CellToWorld(c));
            if (back != c)
            {
                GD.PrintErr($"[GridSystem] FAIL round-trip world: {c} -> {back}");
                ok = false;
            }

            Vector2I byId = IdToCell(CellToId(c));
            if (byId != c)
            {
                GD.PrintErr($"[GridSystem] FAIL round-trip id: {c} -> {byId}");
                ok = false;
            }
        }

        foreach (Vector2I n in GetWalkableNeighbors(_maze.PacManSpawn))
        {
            int d = Mathf.Abs(n.X - _maze.PacManSpawn.X) + Mathf.Abs(n.Y - _maze.PacManSpawn.Y);
            if (d != 1)
            {
                GD.PrintErr($"[GridSystem] FAIL neighbor không kề: {n}");
                ok = false;
            }
        }

        GD.Print(ok
            ? $"[GridSystem] SelfTest PASS — {Width}x{Height}, CellSize={CellSize}"
            : "[GridSystem] SelfTest FAIL — xem lỗi phía trên.");
    }
}
