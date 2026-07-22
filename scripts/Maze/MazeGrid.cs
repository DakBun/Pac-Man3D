using Godot;

namespace PacManGame.Maze;

/// <summary>
/// Quản lý ma trận lưới 2D biểu diễn mê cung.
/// Chịu trách nhiệm chuyển đổi giữa World Position (Vector3) và Grid Coordinates (Vector2I).
/// </summary>
public partial class MazeGrid : Node3D
{
    // Kích thước một ô lưới trong world space (đơn vị mét)
    [Export] private float _cellSize = 1.0f;

    // Mã hóa ma trận: 0 = đường đi, 1 = tường
    [Export] private int[,] _gridData = new int[0, 0];

    // Tọa độ gốc của lưới trong World Space (ô [0,0])
    [Export] private Vector3 _gridOrigin = Vector3.Zero;

    public bool IsReady { get; private set; } = false;

    /// <summary>
    /// Hướng di chuyển "thẳng" hiện tại của Player (được cập nhật khi đổi hướng).
    /// </summary>
    public Vector2I ForwardDirection { get; set; } = Vector2I.Right;

    public override void _Ready()
    {
        // Giả sử _gridData đã được gán từ editor.
        // Nếu chưa, có thể parse từ file .tres hoặc data ở đây.
        if (_gridData.GetLength(0) > 0 && _gridData.GetLength(1) > 0)
        {
            IsReady = true;
        }
    }

    /// <summary>
    /// Kiểm tra xem ô lưới có đi được không.
    /// </summary>
    public bool IsWalkable(Vector2I gridPosition)
    {
        int rows = _gridData.GetLength(0);
        int cols = _gridData.GetLength(1);

        if (gridPosition.X < 0 || gridPosition.X >= cols || gridPosition.Y < 0 || gridPosition.Y >= rows)
        {
            return false;
        }

        return _gridData[gridPosition.Y, gridPosition.X] == 0;
    }

    /// <summary>
    /// Chuyển World Position (Vector3) sang Grid Coordinates (Vector2I).
    /// </summary>
    public Vector2I WorldToGrid(Vector3 worldPosition)
    {
        // Vector3 localPosition = ToLocal(worldPosition); // Nếu cần offset theo node parent
        // Từ World Position, trừ đi gốc lưới rồi chia cho kích thước ô.
        Vector3 offset = worldPosition - _gridOrigin;
        int gridX = Mathf.FloorToInt(offset.X / _cellSize);
        int gridY = Mathf.FloorToInt(offset.Z / _cellSize); // Trong Godot 4, mặt phẳng lưới thường nằm trên XZ
        return new Vector2I(gridX, gridY);
    }

    /// <summary>
    /// Chuyển Grid Coordinates (Vector2I) sang World Position (Vector3).
    /// </summary>
    public Vector3 GridToWorld(Vector2I gridPosition)
    {
        // Cộng 0.5 để đặt vào tâm ô lưới
        float worldX = (gridPosition.X + 0.5f) * _cellSize + _gridOrigin.X;
        float worldZ = (gridPosition.Y + 0.5f) * _cellSize + _gridOrigin.Z;
        return new Vector3(worldX, _gridOrigin.Y, worldZ);
    }

    /// <summary>
    /// Lấy số hàng của ma trận lưới.
    /// </summary>
    public int GetRows() => _gridData.GetLength(0);

    /// <summary>
    /// Lấy số cột của ma trận lưới.
    /// </summary>
    public int GetCols() => _gridData.GetLength(1);

    /// <summary>
    /// Lấy giá trị ô lưới tại vị trí (row, col).
    /// </summary>
    public int GetCell(int row, int col)
    {
        if (row < 0 || row >= GetRows() || col < 0 || col >= GetCols())
        {
            return 1;
        }
        return _gridData[row, col];
    }
}
