using Godot;

namespace PacManGame.Player;

/// <summary>
/// Xử lý di chuyển theo lưới (grid-based) của Pac-Man.
/// Sử dụng hệ thống tọa độ 2D (Vector2I) cho lưới và chuyển đổi sang Vector3 trong world space.
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    // Tốc độ di chuyển đơn vị lưới / giây
    [Export] private float _gridMoveSpeed = 5.0f;

    // Tốc độ lerp để nội suy mượt mà giữa các ô lưới
    [Export] private float _interpolationSpeed = 10.0f;

    // Node tham chiếu đến MazeGrid để truy cập dữ liệu lưới
    private Maze.MazeGrid? _mazeGrid;

    // Vị trí lưới hiện tại và đích
    private Vector2I _currentGridPos;
    private Vector2I _targetGridPos;

    // Vector đích trong world space (để lerp)
    private Vector3 _targetWorldPos;

    // Hướng di chuyển tiếp theo mà người chơi nhấn (để đổi hướng tại giao lộ)
    private Vector2I _nextDirection = Vector2I.Zero;

    public override void _Ready()
    {
        // Lấy tham chiếu đến MazeGrid từ Scene Tree
        _mazeGrid = GetNodeOrNull<Maze.MazeGrid>("%MazeGrid");
        if (_mazeGrid == null)
        {
            GD.PrintErr("[PlayerController] Không tìm thấy MazeGrid. Hãy gán Scene Unique Node.");
            return;
        }

        // Khởi tạo vị trí ban đầu từ World Position của Node này
        _currentGridPos = _mazeGrid.WorldToGrid(GlobalPosition);
        _targetGridPos = _currentGridPos;
        _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
        GlobalPosition = _targetWorldPos;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_mazeGrid == null || _mazeGrid.IsReady == false)
        {
            return;
        }

        float dt = (float)delta;

        // Kiểm tra nếu đã đến gần vị trí đích → cho phép đổi hướng mới
        if (GlobalPosition.DistanceTo(_targetWorldPos) < 0.05f)
        {
            _currentGridPos = _targetGridPos;

            // Thử áp dụng hướng tiếp theo nếu hợp lệ
            if (_nextDirection != Vector2I.Zero && _mazeGrid.IsWalkable(_currentGridPos + _nextDirection))
            {
                _targetGridPos = _currentGridPos + _nextDirection;
                _nextDirection = Vector2I.Zero;
            }
            // Nếu không có hướng mới, tiếp tục đi thẳng nếu có thể
            else if (_mazeGrid.IsWalkable(_currentGridPos + _mazeGrid.ForwardDirection))
            {
                _targetGridPos = _currentGridPos + _mazeGrid.ForwardDirection;
            }

            _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
        }

        // Nội suy vị trí mượt mà từ vị trí hiện tại đến vị trí đích
        Vector3 desiredPosition = GlobalPosition.Lerp(_targetWorldPos, _interpolationSpeed * dt);
        Velocity = (desiredPosition - GlobalPosition) / Mathf.Max(dt, 0.001f);

        MoveAndSlide();
    }

    /// <summary>
    /// Được gọi từ Input Handler để nhận hướng đi mới từ người chơi.
    /// </summary>
    public void SetNextDirection(Vector2I direction)
    {
        _nextDirection = direction;
    }
}
