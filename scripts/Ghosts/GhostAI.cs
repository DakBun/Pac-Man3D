using Godot;
using Godot.Collections;
using PacManGame.Maze;

namespace PacManGame.Ghosts;

/// <summary>
/// AI cơ bản cho Ghost: FSM (Finite State Machine) với 4 trạng thái
/// (Scatter, Chase, Frightened, Eaten) và hệ thống đổi hướng tự động tại giao lộ.
/// </summary>
public partial class GhostAI : CharacterBody3D
{
    [Export] private float _moveSpeed = 4.0f;
    [Export] private float _frightenedSpeed = 2.0f;
    [Export] private float _eatenSpeed = 8.0f;

    [Export] private float _modeTimerDuration = 7.0f;

    private MazeGrid? _mazeGrid;
    private GameManager? _gameManager;

    private Vector2I _currentGridPos;
    private Vector2I _targetGridPos;
    private Vector3 _targetWorldPos;

    public GhostMode CurrentMode { get; private set; } = GhostMode.Scatter;

    // Điểm scatter riêng cho từng loại ghost (ví dụ: góc của mê cung)
    [Export] private Vector2I _scatterTarget;

    private float _modeTimer = 0f;
    private bool _isFrightenedExpired = false;

    public override void _Ready()
    {
        _mazeGrid = GetNodeOrNull<MazeGrid>("%MazeGrid");
        _gameManager = GetNodeOrNull<GameManager>("%GameManager");

        if (_mazeGrid == null)
        {
            GD.PrintErr("[GhostAI] Thiếu MazeGrid node.");
            return;
        }

        _currentGridPos = _mazeGrid.WorldToGrid(GlobalPosition);
        _targetGridPos = _currentGridPos;
        _targetWorldPos = _mazeGrid.GridToWorld(_targetGridPos);
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

        if (newMode == GhostMode.Frightened && _gameManager != null)
        {
            _gameManager.OnGameDataChanged += OnFrightenedExpired;
        }
        else
        {
            _gameManager?.OnGameDataChanged -= OnFrightenedExpired;
        }
    }

    private void OnFrightenedExpired() => _isFrightenedExpired = true;

    private void UpdateModeTimer(float delta)
    {
        if (CurrentMode != GhostMode.Frightened)
        {
            _modeTimer += delta;
            // Ví dụ: sau khoảng thời gian, đổi giữa Chase/Scatter
            if (_modeTimer >= _modeTimerDuration)
            {
                GhostMode next = CurrentMode == GhostMode.Scatter ? GhostMode.Chase : GhostMode.Scatter;
                SetMode(next);
            }
        }
        else if (_isFrightenedExpired)
        {
            _isFrightenedExpired = false;
            SetMode(GhostMode.Scatter);
        }
    }

    private void ChooseNextDirection()
    {
        // Lấy hướng ngược lại so với vừa đi đến để không quay đầu (trừ khi đi ngược tuyệt đối)
        Vector2I reverse = -(_targetGridPos - _currentGridPos);

        // Thuật toán đơn giản: tìm các hướng có thể đi, chọn hướng gần mục tiêu nhất
        Vector2I bestDir = Vector2I.Zero;
        float bestDist = float.MaxValue;
        Vector2I target = CurrentMode == GhostMode.Eaten ? _mazeGrid!.GridToWorld(Vector2I.Zero) : Vector2I.Zero;

        if (CurrentMode is GhostMode.Chase || CurrentMode is GhostMode.Scatter)
        {
            target = CurrentMode == GhostMode.Chase ? Vector2I.Zero : _scatterTarget;
        }

        Vector2I[] dirs = [Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right];
        foreach (Vector2I dir in dirs)
        {
            if (dir == reverse) continue;
            Vector2I next = _currentGridPos + dir;
            if (_mazeGrid!.IsWalkable(next))
            {
                float dist = next.DistanceTo(target);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestDir = dir;
                }
            }
        }

        if (bestDir != Vector2I.Zero)
        {
            _targetGridPos = _currentGridPos + bestDir;
        }
        else
        {
            // Không có lối thoát (hiếm), phải quay đầu
            _targetGridPos = _currentGridPos + reverse;
        }
    }
}
