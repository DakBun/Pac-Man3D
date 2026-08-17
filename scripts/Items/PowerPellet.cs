using PacManGame.Core;
using Godot;
using Godot.Collections;
using PacManGame.Ghosts;

namespace PacManGame.Items;

/// <summary>
/// Logic cho Power Pellet (điểm lớn).
/// Khi ăn: cộng điểm và chuyển tất cả Ghost sang chế độ Frightened.
/// </summary>
public partial class PowerPellet : Area3D
{
    [Export] private int _pointValue = 50;

    private GameManager? _gameManager;
    private Array<Node>? _ghosts;

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("%GameManager");
        _ghosts = GetTree().GetNodesInGroup("Ghosts");
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body.Name == "Player" || body.IsInGroup("Player"))
        {
            _gameManager?.AddScore(_pointValue);
            ActivateFrightenedMode();
            QueueFree();
        }
    }

    private void ActivateFrightenedMode()
    {
        if (_ghosts == null) return;

        foreach (Node node in _ghosts)
        {
            if (node is GhostAI ghost)
            {
                ghost.SetMode(GhostMode.Frightened);
            }
        }
    }
}
