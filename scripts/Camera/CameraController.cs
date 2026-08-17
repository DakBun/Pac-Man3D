using Godot;

namespace PacManGame.Camera;

/// <summary>
/// Camera bám theo Player sử dụng Lerp/Slerp để tạo chuyển động mượt mà.
/// </summary>
public partial class CameraController : Camera3D
{
    [Export] private Node3D? _followTarget;

    // Khoảng cách offset mong muốn so với Player
    [Export] private Vector3 _offset = new Vector3(0f, 6f, -8f);

    // Tốc độ Lerp cho vị trí (đơn vị / giây)
    [Export] private float _positionLerpSpeed = 5.0f;

    // Tốc độ Slerp cho rotation
    [Export] private float _rotationLerpSpeed = 5.0f;

    public override void _PhysicsProcess(double delta)
    {
        if (_followTarget == null)
        {
            GD.PrintErr("[CameraController] Thiếu node _followTarget.");
            return;
        }

        float dt = (float)delta;

        Vector3 targetPosition = _followTarget.GlobalPosition + _followTarget.GlobalTransform.Basis * _offset;

        // Lerp vị trí để camera di chuyển mượt theo Player
        GlobalPosition = GlobalPosition.Lerp(targetPosition, _positionLerpSpeed * dt);

        // Slerp rotation để nhìn về phía Player mượt mà
        Basis targetBasis = Basis.LookingAt(_followTarget.GlobalPosition - GlobalPosition, Vector3.Up);
        GlobalBasis = GlobalBasis.Slerp(targetBasis, _rotationLerpSpeed * dt);
    }
}
