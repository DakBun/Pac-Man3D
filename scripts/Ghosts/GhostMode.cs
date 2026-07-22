namespace PacManGame.Ghosts;

/// <summary>
/// Định nghĩa các trạng thái hoạt động của Ghost theo đúng bản gốc Pac-Man.
/// Mỗi mode có hành vi và sức mạnh khác nhau.
/// </summary>
public enum GhostMode
{
    /// <summary>
    /// Ghost rời nhà và di chuyển ngẫu nhiên hoặc đến vị trí scatter.
    /// </summary>
    Scatter = 0,

    /// <summary>
    /// Ghost đuổi theo Pac-Man với chiến lược đặc trưng của từng loại (Blinky, Pinky, Inky, Clyde).
    /// </summary>
    Chase = 1,

    /// <summary>
    /// Ghost bị ăn power pellet → chạy ngược hướng, chậm lại, và có thể bị ăn.
    /// </summary>
    Frightened = 2,

    /// <summary>
    /// Ghost đã bị ăn → chỉ còn đôi mắt trở về nhà.
    /// </summary>
    Eaten = 3
}
