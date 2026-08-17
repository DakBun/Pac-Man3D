using PacManGame.Core;
using Godot;

namespace PacManGame.Items;

/// <summary>
/// Logic cho một viên Pellet thường.
/// Khi Pac-Man ăn vào → điểm cộng và biến mất khỏi scene.
/// </summary>
public partial class Pellet : Area3D
{
    [Export] private int _pointValue = 10;

    private GameManager? _gameManager;

    public override void _Ready()
    {
        // Cú pháp "%" chỉ phân giải được khi node có Owner. Pellet được sinh ra
        // lúc chạy nên không có Owner, phải lùi về singleton GameManager.Instance.
        _gameManager = GetNodeOrNull<GameManager>("%GameManager") ?? GameManager.Instance;

        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body.Name == "Player" || body.IsInGroup("Player"))
        {
            _gameManager?.AddScore(_pointValue);
            QueueFree();
        }
    }
}
