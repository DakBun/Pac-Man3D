using System;
using Godot;

namespace PacManGame.Core;

/// <summary>
/// Quản lý trạng thái toàn cục của game: điểm số, mạng, và các màn chơi.
/// Tuân theo mô hình Singleton để các script khác có thể truy cập dễ dàng.
/// </summary>
public partial class GameManager : Node
{
    private static GameManager? _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GD.PrintErr("[GameManager] Chưa có instance trong Scene Tree. Hãy đảm bảo GameManager là child của Root.");
            }
            return _instance;
        }
    }

    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// Điểm số hiện tại của người chơi.
    /// </summary>
    [Export] private int _score;

    /// <summary>
    /// Số mạng còn lại của người chơi.
    /// </summary>
    [Export] private int _lives;

    /// <summary>
    /// Trạng thái hiện tại của game.
    /// </summary>
    public GameState CurrentState { get; private set; } = GameState.Ready;

    public int Score
    {
        get => _score;
        private set
        {
            _score = Mathf.Max(0, value);
            OnGameDataChanged?.Invoke();
        }
    }

    public int Lives
    {
        get => _lives;
        private set
        {
            _lives = Mathf.Max(0, value);
            OnGameDataChanged?.Invoke();
        }
    }

    /// <summary>
    /// Sự kiện được gọi khi điểm số hoặc số mạng thay đổi.
    /// </summary>
    public event Action? OnGameDataChanged;

    public override void _Ready()
    {
        // Đảm bảo chỉ có một instance duy nhất
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;
    }

    public override void _Process(double delta)
    {
        // Xử lý input ở đây nếu cần (ví dụ: bắt đầu game khi nhấn phím)
    }

    /// <summary>
    /// Bắt đầu trạng thái chơi mới.
    /// </summary>
    public void StartGame()
    {
        Score = 0;
        Lives = 3;
        CurrentState = GameState.Playing;
        OnGameDataChanged?.Invoke();
    }

    /// <summary>
    /// Tạm dừng hoặc tiếp tục game.
    /// </summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            GetTree().Paused = true;
        }
        else if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            GetTree().Paused = false;
        }
    }

    /// <summary>
    /// Cộng điểm vào tổng điểm hiện tại.
    /// </summary>
    public void AddScore(int points)
    {
        Score += points;
    }

    /// <summary>
    /// Trừ mạng và kiểm tra Game Over.
    /// </summary>
    public void LoseLife()
    {
        Lives--;
        if (Lives <= 0)
        {
            CurrentState = GameState.GameOver;
            GD.Print("[GameManager] Game Over!");
        }
        OnGameDataChanged?.Invoke();
    }

    /// <summary>
    /// Kết thúc game và quay về trạng thái Ready.
    /// </summary>
    public void ResetGame()
    {
        Score = 0;
        Lives = 3;
        CurrentState = GameState.Ready;
        GetTree().Paused = false;
        OnGameDataChanged?.Invoke();
    }
}
