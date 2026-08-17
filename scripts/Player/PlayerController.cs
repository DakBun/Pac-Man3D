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

    // Khoảng cách world tính là chạm ghost. Ô lưới cạnh nhau cách đúng 1.0
    // đơn vị, nên ngưỡng phải nhỏ hơn 1.0 mà vẫn đủ rộng để bắt lúc hai bên
    // đi ngược chiều lướt qua nhau giữa hai ô.
    [Export] private float _ghostHitRadius = 0.9f;

    // Node tham chiếu đến MazeGrid để truy cập dữ liệu lưới
    private Maze.MazeGrid? _mazeGrid;

    // Ghost và nhãn thông báo, lấy qua Scene Unique Name
    private Node3D? _ghost;
    private Label? _gameOverLabel;

    // Vị trí lưới hiện tại và đích
    private Vector2I _currentGridPos;
    private Vector2I _targetGridPos;

    // Vector đích trong world space (để lerp)
    private Vector3 _targetWorldPos;

    // Hướng di chuyển tiếp theo mà người chơi nhấn (để đổi hướng tại giao lộ)
    private Vector2I _nextDirection = Vector2I.Zero;

    // Hướng Pac-Man đang đi. Đây là trạng thái RIÊNG của Player, không dùng
    // MazeGrid.ForwardDirection vì đó là state dùng chung cho cả ghost.
    private Vector2I _currentDirection = Vector2I.Zero;

    // Ván đã kết thúc hay chưa, để không kích hoạt game over nhiều lần.
    private bool _isGameOver = false;

    public override void _Ready()
    {
        // Lấy tham chiếu đến MazeGrid từ Scene Tree
        _mazeGrid = GetNodeOrNull<Maze.MazeGrid>("%MazeGrid");
        if (_mazeGrid == null)
        {
            GD.PrintErr("[PlayerController] Không tìm thấy MazeGrid. Hãy gán Scene Unique Node.");
            return;
        }

        _ghost = GetNodeOrNull<Node3D>("%Ghost");
        _gameOverLabel = GetNodeOrNull<Label>("%GameOverLabel");

        if (_ghost == null)
        {
            GD.PrintErr("[PlayerController] Không tìm thấy Ghost - sẽ không có va chạm kết thúc ván.");
        }

        // Khởi tạo vị trí ban đầu từ World Position của Node này
        _currentGridPos = _mazeGrid.WorldToGrid(GlobalPosition);
        _targetGridPos = _currentGridPos;
        _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
        GlobalPosition = _targetWorldPos;
    }

    /// <summary>
    /// Nhận phím mũi tên hoặc WASD và đặt hướng đi mong muốn.
    /// Hướng chỉ được áp dụng khi Pac-Man đến giữa ô kế tiếp (xem _PhysicsProcess).
    /// Phím R chơi lại sau khi thua.
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (key.PhysicalKeycode == Key.R)
        {
            RestartGame();
            return;
        }

        if (_isGameOver)
        {
            return;
        }

        // Trục Y của lưới ứng với trục Z của world, nên lên = (0, -1).
        Vector2I direction = key.PhysicalKeycode switch
        {
            Key.Up or Key.W => new Vector2I(0, -1),
            Key.Down or Key.S => new Vector2I(0, 1),
            Key.Left or Key.A => new Vector2I(-1, 0),
            Key.Right or Key.D => new Vector2I(1, 0),
            _ => Vector2I.Zero
        };

        if (direction != Vector2I.Zero)
        {
            SetNextDirection(direction);
        }
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
                _currentDirection = _nextDirection;
                _nextDirection = Vector2I.Zero;
            }

            // Đi thẳng theo hướng hiện tại nếu ô kế tiếp đi được.
            // Nếu bị tường chặn (hoặc chưa nhấn phím nào) thì đứng yên tại chỗ,
            // đích trùng vị trí hiện tại nên vòng lặp không bị kẹt.
            if (_currentDirection != Vector2I.Zero && _mazeGrid.IsWalkable(_currentGridPos + _currentDirection))
            {
                _targetGridPos = _currentGridPos + _currentDirection;
            }
            else
            {
                _targetGridPos = _currentGridPos;
            }

            _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
        }

        // Nội suy vị trí mượt mà từ vị trí hiện tại đến vị trí đích
        Vector3 desiredPosition = GlobalPosition.Lerp(_targetWorldPos, _interpolationSpeed * dt);
        Velocity = (desiredPosition - GlobalPosition) / Mathf.Max(dt, 0.001f);

        MoveAndSlide();

        CheckGhostCollision();
    }

    /// <summary>
    /// Kiểm tra khoảng cách tới ghost. Dùng khoảng cách thay vì tín hiệu va chạm
    /// vì mesh tường trong MeshLibrary không có CollisionShape, nên toàn bộ va
    /// chạm của trò chơi này đều xử lý theo lưới logic chứ không qua physics.
    /// </summary>
    private void CheckGhostCollision()
    {
        if (_isGameOver || _ghost == null)
        {
            return;
        }

        if (_mazeGrid == null)
        {
            return;
        }

        // Bắt theo hai điều kiện. Điều kiện ô lưới xử lý trường hợp hai bên
        // cùng đứng trên một ô; điều kiện khoảng cách xử lý trường hợp lướt
        // qua nhau giữa hai ô mà không ô nào trùng.
        Vector2I ghostCell = _mazeGrid.WorldToGrid(_ghost.GlobalPosition);
        Vector2I playerCell = _mazeGrid.WorldToGrid(GlobalPosition);

        if (ghostCell == playerCell || GlobalPosition.DistanceTo(_ghost.GlobalPosition) < _ghostHitRadius)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        _isGameOver = true;
        GD.Print("[PlayerController] Pac-Man đã bị ghost bắt - kết thúc ván.");

        if (_gameOverLabel != null)
        {
            _gameOverLabel.Visible = true;
        }

        // Dừng toàn bộ trò chơi. Node Player đặt process_mode = Always trong
        // scene nên vẫn nhận được phím R để chơi lại.
        GetTree().Paused = true;
    }

    private void RestartGame()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    /// <summary>
    /// Được gọi từ Input Handler để nhận hướng đi mới từ người chơi.
    /// </summary>
    public void SetNextDirection(Vector2I direction)
    {
        _nextDirection = direction;
    }
}
