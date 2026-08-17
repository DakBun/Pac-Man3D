using Godot;

namespace PacManGame.Camera;

/// <summary>
/// Camera bám theo Player sử dụng Lerp/Slerp để tạo chuyển động mượt mà.
/// Thêm điều khiển chuột: giữ phải rê để quay quanh Player, lăn chuột để phóng to/thu nhỏ.
/// </summary>
public partial class CameraController : Camera3D
{
    [Export] private Node3D? _followTarget;

    /// <summary>
    /// Tâm quay của camera. Mặc định là tâm mê cung 21x21, tức (10.5, 0, 10.5).
    /// </summary>
    [Export] private Vector3 _orbitCenter = new Vector3(10.5f, 0f, 10.5f);

    /// <summary>
    /// Bật thì camera lấy Player làm tâm quay, tắt thì lấy _orbitCenter.
    /// Để tắt vì khi bám Player, tâm quay chạy theo nhân vật nên rê chuột
    /// giống như xoay quanh Pac-Man chứ không phải xoay mê cung.
    /// </summary>
    [Export] private bool _followPlayer = false;

    // Khoảng cách offset mong muốn so với Player
    [Export] private Vector3 _offset = new Vector3(0f, 6f, -8f);

    // Tốc độ Lerp cho vị trí (đơn vị / giây)
    [Export] private float _positionLerpSpeed = 5.0f;

    // Tốc độ Slerp cho rotation
    [Export] private float _rotationLerpSpeed = 5.0f;

    // --- Biến điều khiển bằng chuột ---
    // Góc quay quanh trục Y (radian)
    private float _orbitYaw;

    // Góc ngẩng (radian)
    private float _orbitPitch;

    // Khoảng cách tới Player
    private float _orbitDistance;

    // Cờ đang quay bằng chuột
    private bool _isOrbiting;

    // Độ nhạy chuột khi rê để quay
    [Export] private float _mouseSensitivity = 0.005f;

    // Khoảng cách thay đổi mỗi nấc lăn chuột
    [Export] private float _zoomStep = 2.0f;

    // Giới hạn phóng to / thu nhỏ
    [Export] private float _minDistance = 6.0f;
    [Export] private float _maxDistance = 60.0f;

    // Giới hạn góc ngẩng để camera không lật ngược hoặc chui xuống sàn
    private const float MinPitch = 0.15f;
    private const float MaxPitch = 1.5f;

    public override void _Ready()
    {
        // Khởi tạo 3 biến quỹ đạo từ giá trị _offset hiện có
        // để hành vi ban đầu giống hệt hành vi cũ.
        _orbitDistance = _offset.Length();

        if (Mathf.IsEqualApprox(_orbitDistance, 0f))
        {
            // Nếu _offset gần 0 thì dùng giá trị mặc định
            _orbitDistance = 24f;
            _orbitPitch = 1.0f;
            _orbitYaw = 0f;
        }
        else
        {
            // Asin(_offset.Y / distance) cho góc ngẩng
            _orbitPitch = Mathf.Asin(_offset.Y / _orbitDistance);
            // Atan2(X, Z) cho góc quay quanh trục Y
            _orbitYaw = Mathf.Atan2(_offset.X, _offset.Z);
        }

        _orbitPitch = Mathf.Clamp(_orbitPitch, MinPitch, MaxPitch);
        _orbitDistance = Mathf.Clamp(_orbitDistance, _minDistance, _maxDistance);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                // Bật/tắt cờ quay chuột
                _isOrbiting = mouseButton.Pressed;
                return;
            }

            // Sự kiện nút chuột bắn hai lần mỗi nấc lăn (nhấn và nhả).
            // Không lọc Pressed thì mỗi nấc sẽ zoom gấp đôi _zoomStep.
            if (!mouseButton.Pressed)
            {
                return;
            }

            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                // Lăn lên: lại gần
                _orbitDistance = Mathf.Clamp(_orbitDistance - _zoomStep, _minDistance, _maxDistance);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                // Lăn xuống: ra xa
                _orbitDistance = Mathf.Clamp(_orbitDistance + _zoomStep, _minDistance, _maxDistance);
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && _isOrbiting)
        {
            // Rê chuột khi giữ phải: cập nhật góc quay
            _orbitYaw -= mouseMotion.Relative.X * _mouseSensitivity;
            _orbitPitch += mouseMotion.Relative.Y * _mouseSensitivity;

            _orbitPitch = Mathf.Clamp(_orbitPitch, MinPitch, MaxPitch);
        }
    }

    /// <summary>
    /// Điểm mà camera quay quanh và luôn nhìn vào.
    /// </summary>
    private Vector3 GetPivot()
    {
        if (_followPlayer && _followTarget != null)
        {
            return _followTarget.GlobalPosition;
        }

        return _orbitCenter;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector3 pivot = GetPivot();

        // Đổi toạ độ cầu (yaw, pitch, distance) sang offset Descartes.
        // h là chiều dài hình chiếu của offset lên mặt phẳng XZ.
        float h = _orbitDistance * Mathf.Cos(_orbitPitch);
        Vector3 offset = new Vector3(
            h * Mathf.Sin(_orbitYaw),
            _orbitDistance * Mathf.Sin(_orbitPitch),
            h * Mathf.Cos(_orbitYaw)
        );

        // Cộng offset trong hệ world, không nhân với Basis của Player.
        // Nếu nhân, góc quỹ đạo sẽ tính theo hướng Pac-Man đang quay mặt và
        // camera sẽ xoay theo mỗi lần Pac-Man đổi hướng.
        Vector3 targetPosition = pivot + offset;

        // Lerp vị trí để camera di chuyển mượt theo Player
        GlobalPosition = GlobalPosition.Lerp(targetPosition, _positionLerpSpeed * dt);

        // Slerp rotation để nhìn về phía Player mượt mà
        Basis targetBasis = Basis.LookingAt(pivot - GlobalPosition, Vector3.Up);
        GlobalBasis = GlobalBasis.Slerp(targetBasis, _rotationLerpSpeed * dt);
    }
}
