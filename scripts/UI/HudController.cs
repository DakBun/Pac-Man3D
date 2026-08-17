using PacManGame.Core;
using Godot;

namespace PacManGame.UI;

/// <summary>
/// Cập nhật điểm số và số mạng lên giao diện UI.
/// </summary>
public partial class HudController : CanvasLayer
{
    [Export] private Label? _scoreLabel;
    [Export] private Label? _livesLabel;
    [Export] private Label? _stateLabel;

    private GameManager? _gameManager;

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("%GameManager");

        if (_gameManager != null)
        {
            _gameManager.OnGameDataChanged += UpdateHud;
            UpdateHud();
        }
    }

    public override void _ExitTree()
    {
        if (_gameManager != null)
        {
            _gameManager.OnGameDataChanged -= UpdateHud;
        }
    }

    private void UpdateHud()
    {
        if (_gameManager == null) return;

        if (_scoreLabel != null)
        {
            _scoreLabel.Text = $"Score: {_gameManager.Score:D5}";
        }

        if (_livesLabel != null)
        {
            _livesLabel.Text = $"Lives: {_gameManager.Lives}";
        }

        if (_stateLabel != null)
        {
            _stateLabel.Text = $"State: {_gameManager.CurrentState}";
        }
    }
}
