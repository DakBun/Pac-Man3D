using PacManGame.Core;
using Godot;
using System.Collections.Generic;
using PacManGame.Maze;

namespace PacManGame.Ghosts;

/// <summary>
/// AI cho Ghost: FSM (Finite State Machine) với 4 trạng thái
/// (Scatter, Chase, Frightened, Eaten). Việc chọn ô kế tiếp dùng A* qua
/// PathfindingService thay vì so khoảng cách tham lam một bước, nên ghost
/// không còn kẹt dao động ở ngõ cụt hay góc hẹp.
/// </summary>
public partial class GhostAI : CharacterBody3D
{
    [Export] private float _moveSpeed = 4.0f;
    [Export] private float _frightenedSpeed = 2.0f;
    [Export] private float _eatenSpeed = 8.0f;

    /// <summary>Thời gian bám đuổi Pac-Man trước khi chuyển sang Scatter.</summary>
    [Export] private float _chaseDuration = 12.0f;

    /// <summary>Thời gian rút về góc riêng trước khi quay lại Chase.</summary>
    [Export] private float _scatterDuration = 4.0f;

    /// <summary>Thời gian ma ở trạng thái Frightened sau khi Pac-Man ăn Power Pellet.</summary>
    [Export] private float _frightenedDuration = 6.0f;

    // Điểm scatter riêng cho từng loại ghost (ví dụ: góc của mê cung)
    [Export] private Vector2I _scatterTarget;

    private MazeGrid? _mazeGrid;
    private GameManager? _gameManager;

    // Pac-Man, dùng làm mục tiêu ở trạng thái Chase.
    private Node3D? _player;

    private Vector2I _currentGridPos;
    private Vector2I _targetGridPos;
    private Vector3 _targetWorldPos;

    // Ô xuất phát, dùng làm đích khi ở trạng thái Eaten.
    private Vector2I _spawnGridPos;

    public GhostMode CurrentMode { get; private set; } = GhostMode.Scatter;

    private float _modeTimer = 0f;

    // Đếm ngược thời gian còn lại của trạng thái Frightened.
    private float _frightenedTimer = 0f;

    public override void _Ready()
    {
        _mazeGrid = GetNodeOrNull<MazeGrid>("%MazeGrid");
        _gameManager = GetNodeOrNull<GameManager>("%GameManager");
        _player = GetNodeOrNull<Node3D>("%Player");

        if (_mazeGrid == null)
        {
            GD.PrintErr("[GhostAI] Thiếu MazeGrid node.");
            return;
        }

        if (_player == null)
        {
            GD.PrintErr("[GhostAI] Thiếu Player node - trạng thái Chase sẽ không bám được Pac-Man.");
        }

        _currentGridPos = _mazeGrid.WorldToGrid(GlobalPosition);
        _spawnGridPos = _currentGridPos;
        _targetGridPos = _currentGridPos;
        _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);

        // Vào Chase ngay để người chơi thấy ghost đuổi từ đầu.
        SetMode(GhostMode.Chase);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_mazeGrid == null || !_mazeGrid.IsReady)
        {
            return;
        }

        float dt = (float)delta;
        UpdateModeTimer(dt);

        if (GlobalPosition.DistanceTo(_targetWorldPos) < 0.05f)
        {
            _currentGridPos = _targetGridPos;
            ChooseNextDirection();
            _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
        }

        float speed = _moveSpeed;
        if (CurrentMode == GhostMode.Frightened) speed = _frightenedSpeed;
        if (CurrentMode == GhostMode.Eaten) speed = _eatenSpeed;

        Vector3 desired = GlobalPosition.Lerp(_targetWorldPos, speed * dt);
        Velocity = (desired - GlobalPosition) / Mathf.Max(dt, 0.001f);
        MoveAndSlide();
    }

    /// <summary>
    /// Chuyển đổi trạng thái FSM.
    /// </summary>
    public void SetMode(GhostMode newMode)
    {
        CurrentMode = newMode;
        _modeTimer = 0f;

        // Frightened hết hạn bằng đồng hồ riêng, không dựa vào event điểm/mạng
        // của GameManager (event đó bắn mỗi lần đổi điểm, sẽ làm Frightened
        // kết thúc gần như tức thì khi Pac-Man ăn viên pellet kế tiếp).
        if (newMode == GhostMode.Frightened)
        {
            _frightenedTimer = _frightenedDuration;
        }
    }

    private void UpdateModeTimer(float delta)
    {
        if (CurrentMode == GhostMode.Frightened)
        {
            _frightenedTimer -= delta;
            if (_frightenedTimer <= 0f)
            {
                SetMode(GhostMode.Chase);
            }
            return;
        }

        if (CurrentMode == GhostMode.Eaten)
        {
            // Về tới ổ thì hồi sinh và đuổi tiếp.
            if (_currentGridPos == _spawnGridPos)
            {
                SetMode(GhostMode.Chase);
            }
            return;
        }

        _modeTimer += delta;

        // Chase dài hơn Scatter nhiều để ghost chủ yếu bám Pac-Man, thay vì
        // đứng lì ở góc riêng như khi hai khoảng thời gian bằng nhau.
        float limit = CurrentMode == GhostMode.Chase ? _chaseDuration : _scatterDuration;

        if (_modeTimer >= limit)
        {
            SetMode(CurrentMode == GhostMode.Chase ? GhostMode.Scatter : GhostMode.Chase);
        }
    }

    /// <summary>
    /// Ô lưới mà ghost đang hướng tới, tuỳ theo trạng thái FSM.
    /// Mọi giá trị trả về đều là ô đi được, vì A* từ chối tìm đường tới tường.
    /// </summary>
    private Vector2I GetTargetCell()
    {
        switch (CurrentMode)
        {
            case GhostMode.Chase:
                return GetPlayerCell();

            case GhostMode.Frightened:
                return GetFarthestCornerFromPlayer();

            case GhostMode.Eaten:
                return _spawnGridPos;

            case GhostMode.Scatter:
            default:
                return _scatterTarget;
        }
    }

    private Vector2I GetPlayerCell()
    {
        if (_player == null || _mazeGrid == null)
        {
            return _scatterTarget;
        }

        return _mazeGrid.WorldToGrid(_player.GlobalPosition);
    }

    /// <summary>
    /// Khi sợ, ghost chạy về góc xa Pac-Man nhất. Dùng góc cố định thay vì
    /// điểm đối xứng qua ghost, vì điểm đối xứng có thể rơi vào tường hoặc
    /// ra ngoài lưới, khiến A* trả về null.
    /// </summary>
    private Vector2I GetFarthestCornerFromPlayer()
    {
        if (_mazeGrid == null)
        {
            return _scatterTarget;
        }

        int lastRow = _mazeGrid.GetRows() - 2;
        int lastCol = _mazeGrid.GetCols() - 2;

        Vector2I[] corners =
        [
            new Vector2I(1, 1),
            new Vector2I(lastCol, 1),
            new Vector2I(1, lastRow),
            new Vector2I(lastCol, lastRow)
        ];

        Vector2I playerCell = GetPlayerCell();
        Vector2I best = _scatterTarget;
        float bestDist = -1f;

        foreach (Vector2I corner in corners)
        {
            if (!_mazeGrid.IsWalkable(corner))
            {
                continue;
            }

            float dist = corner.DistanceTo(playerCell);
            if (dist > bestDist)
            {
                bestDist = dist;
                best = corner;
            }
        }

        return best;
    }

    /// <summary>
    /// Chọn ô kế tiếp bằng A*. Đường đi được tính lại mỗi khi ghost tới giữa
    /// một ô, nên nó bám theo Pac-Man đang di chuyển. Lưới chỉ 21x21 = 441 ô
    /// nên chi phí không đáng kể.
    /// </summary>
    private void ChooseNextDirection()
    {
        if (_mazeGrid == null)
        {
            return;
        }

        Vector2I target = GetTargetCell();

        if (target != _currentGridPos)
        {
            List<Vector2I>? path = PathfindingService.FindPath(_mazeGrid, _currentGridPos, target);

            // path[0] là ô hiện tại, path[1] là ô kế tiếp cần đi tới.
            if (path != null && path.Count >= 2)
            {
                _targetGridPos = path[1];
                return;
            }
        }

        // Dự phòng khi A* không tìm được đường (đích trùng vị trí hiện tại,
        // hoặc mê cung bị chia cắt): đi tham lam về phía mục tiêu.
        ChooseNextDirectionGreedy(target);
    }

    private void ChooseNextDirectionGreedy(Vector2I target)
    {
        Vector2I[] dirs = [Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right];

        Vector2I bestDir = Vector2I.Zero;
        float bestDist = float.MaxValue;

        foreach (Vector2I dir in dirs)
        {
            Vector2I next = _currentGridPos + dir;
            if (!_mazeGrid!.IsWalkable(next))
            {
                continue;
            }

            float dist = next.DistanceTo(target);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDir = dir;
            }
        }

        _targetGridPos = bestDir == Vector2I.Zero
            ? _currentGridPos
            : _currentGridPos + bestDir;
    }
}
