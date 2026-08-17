using System.Collections.Generic;
using Godot;

namespace PacMan.Core;

/// <summary>
/// Dữ liệu mê cung Pac-Man. Class thuần, không kế thừa Node.
/// Kích thước 19 x 21, đối xứng qua trục dọc, 190 ô đi được, liên thông hoàn toàn.
///
/// Sơ đồ (đã kiểm chứng bằng BFS: 0 ô cô lập):
///
///      0123456789012345678
///   0  ###################
///   1  #........#........#
///   2  #o##.###.#.###.##o#
///   3  #.................#
///   4  #.##.#.#####.#.##.#
///   5  #....#...#...#....#
///   6  ####.###.#.###.####
///   7  #....#.......#....#
///   8  #.##.#.##-##.#.##.#
///   9  #....#.#HHH#.#....#
///  10  ####.#.#HHH#.#.####
///  11  #......#####......#
///  12  ####.#.......#.####
///  13  #....#.#####.#....#
///  14  #.##.#...#...#.##.#
///  15  #..#...#####...#..#
///  16  ##.#.#.......#.#.##
///  17  #o...#.#####.#...o#
///  18  #.#######.#######.#
///  19  #.................#
///  20  ###################
///
/// Ký hiệu: '#'=Wall  '.'=Dot  'o'=PowerPellet  ' '=Empty  'H'=GhostHouse  '-'=Door
/// Tổng: 186 dot + 4 power pellet. Spawn Pac-Man tại (9, 12).
/// </summary>
public class MazeData
{
    private static readonly string[] Layout =
    {
        "###################",
        "#........#........#",
        "#o##.###.#.###.##o#",
        "#.................#",
        "#.##.#.#####.#.##.#",
        "#....#...#...#....#",
        "####.###.#.###.####",
        "#....#.......#....#",
        "#.##.#.##-##.#.##.#",
        "#....#.#HHH#.#....#",
        "####.#.#HHH#.#.####",
        "#......#####......#",
        "####.#.......#.####",
        "#....#.#####.#....#",
        "#.##.#...#...#.##.#",
        "#..#...#####...#..#",
        "##.#.#.......#.#.##",
        "#o...#.#####.#...o#",
        "#.#######.#######.#",
        "#.................#",
        "###################",
    };

    private readonly CellType[,] _cells;

    public int Width { get; }
    public int Height { get; }

    /// <summary>Ô trống ngay dưới nhà ma — vị trí xuất phát của Pac-Man.</summary>
    public Vector2I PacManSpawn { get; }

    /// <summary>Tâm nhà ma — đích quay về của ghost ở trạng thái Eaten.</summary>
    public Vector2I GhostHouseCenter { get; }

    /// <summary>Ô cửa nhà ma. Ghost đi qua được, Pac-Man thì không.</summary>
    public Vector2I GhostHouseDoor { get; }

    /// <summary>4 góc scatter, thứ tự: trên-phải, trên-trái, dưới-phải, dưới-trái.</summary>
    public Vector2I[] GhostScatterCorners { get; }

    public MazeData()
    {
        Height = Layout.Length;
        Width = Layout[0].Length;
        _cells = new CellType[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            if (Layout[y].Length != Width)
            {
                GD.PrintErr($"[MazeData] Hàng {y} có độ dài {Layout[y].Length}, phải là {Width}.");
                continue;
            }

            for (int x = 0; x < Width; x++)
            {
                _cells[x, y] = Layout[y][x] switch
                {
                    '#' => CellType.Wall,
                    '.' => CellType.Dot,
                    'o' => CellType.PowerPellet,
                    'H' => CellType.GhostHouse,
                    '-' => CellType.Door,
                    ' ' => CellType.Empty,
                    var c => LogUnknown(c, x, y),
                };
            }
        }

        GhostHouseDoor = new Vector2I(9, 8);
        GhostHouseCenter = new Vector2I(9, 9);
        PacManSpawn = new Vector2I(9, 12);
        GhostScatterCorners = new[]
        {
            new Vector2I(17, 1),  // trên-phải  — Blinky
            new Vector2I(1, 1),   // trên-trái  — Pinky
            new Vector2I(17, 19), // dưới-phải  — Inky
            new Vector2I(1, 19),  // dưới-trái  — Clyde
        };
    }

    private static CellType LogUnknown(char c, int x, int y)
    {
        GD.PrintErr($"[MazeData] Ký tự lạ '{c}' tại ({x},{y}). Coi như tường.");
        return CellType.Wall;
    }

    /// <summary>Trả về Wall nếu toạ độ ra ngoài biên — không ném exception.</summary>
    public CellType GetCell(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return CellType.Wall;
        return _cells[x, y];
    }

    public CellType GetCell(Vector2I cell) => GetCell(cell.X, cell.Y);

    public void SetCell(int x, int y, CellType value)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _cells[x, y] = value;
    }

    public void SetCell(Vector2I cell, CellType value) => SetCell(cell.X, cell.Y, value);

    /// <summary>Tường theo góc nhìn Pac-Man: Wall, Door, hoặc ngoài biên.</summary>
    public bool IsWall(int x, int y)
    {
        CellType c = GetCell(x, y);
        return c == CellType.Wall || c == CellType.Door;
    }

    public bool IsWall(Vector2I cell) => IsWall(cell.X, cell.Y);

    /// <summary>Tường theo góc nhìn ghost: chỉ Wall. Ghost đi qua Door được.</summary>
    public bool IsWallForGhost(int x, int y) => GetCell(x, y) == CellType.Wall;

    public bool IsWallForGhost(Vector2I cell) => IsWallForGhost(cell.X, cell.Y);

    /// <summary>
    /// Kiểm chứng mê cung bằng BFS từ PacManSpawn.
    /// Bắt buộc gọi một lần lúc khởi tạo — mê cung hỏng thì A* ngày sau sẽ sai âm thầm.
    /// </summary>
    public bool Validate(out string error)
    {
        var walkable = new HashSet<Vector2I>();
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            if (!IsWall(x, y) && GetCell(x, y) != CellType.GhostHouse)
                walkable.Add(new Vector2I(x, y));

        if (!walkable.Contains(PacManSpawn))
        {
            error = $"PacManSpawn {PacManSpawn} không phải ô đi được.";
            return false;
        }

        var seen = new HashSet<Vector2I> { PacManSpawn };
        var queue = new Queue<Vector2I>();
        queue.Enqueue(PacManSpawn);

        Vector2I[] dirs = { Vector2I.Right, Vector2I.Left, Vector2I.Down, Vector2I.Up };

        while (queue.Count > 0)
        {
            Vector2I cur = queue.Dequeue();
            foreach (Vector2I d in dirs)
            {
                Vector2I next = cur + d;
                if (walkable.Contains(next) && seen.Add(next))
                    queue.Enqueue(next);
            }
        }

        if (seen.Count != walkable.Count)
        {
            foreach (Vector2I c in walkable)
                if (!seen.Contains(c))
                {
                    error = $"Ô {c} bị cô lập. Tổng đi được {walkable.Count}, tiếp cận được {seen.Count}.";
                    return false;
                }
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Đếm số pellet còn lại trên bản đồ.</summary>
    public int CountPellets()
    {
        int n = 0;
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            if (_cells[x, y] == CellType.Dot || _cells[x, y] == CellType.PowerPellet)
                n++;
        return n;
    }
}
