using Godot;

namespace PacManGame.Maze;

/// <summary>
/// T? d?ng t?o mê cung 3D t? m?t Texture2D (?nh b?n d?).
/// Các pixel có s?c xanh duong làm ch? d?o s? du?c chuy?n thành tu?ng (Wall) trong GridMap.
/// </summary>
public partial class MazeGenerator : GridMap
{
    /// <summary>
    /// ?nh b?n d? mê cung d?u vào. Các vùng màu xanh duong d?m s? thành tu?ng.
    /// </summary>
    [Export] public Texture2D MazeTexture { get; set; }

    /// <summary>
    /// ID mesh tu?ng tuong ?ng trong GridMap (c?n du?c d?nh nghia trong inspector c?a GridMap).
    /// </summary>
    [Export] public int WallMeshId { get; set; } = 0;

    /// <summary>
    /// Kích thu?c chu?n c?a ?nh b?n d? mê cung (chi?u r?ng x chi?u cao).
    /// </summary>
    private const int MazeWidth = 28;
    private const int MazeHeight = 36;

    public override void _Ready()
    {
        if (MazeTexture != null)
        {
            GenerateMazeFromImage();
        }
    }

    /// <summary>
    /// Ð?c ?nh b?n d?, resize v? chu?n 28x36 và d?ng tu?ng GridMap t?i các pixel màu xanh.
    /// </summary>
    private void GenerateMazeFromImage()
    {
        // L?y d?i tu?ng Image t? Texture2D.
        // Luu ý: Texture2D.GetImage() có th? tr? v? null n?u texture chua du?c load.
        Image image = MazeTexture.GetImage();
        if (image == null)
        {
            GD.PrintErr("[MazeGenerator] MazeTexture không ch?a Image h?p l?.");
            return;
        }

        // Resize ?nh v? kích thu?c chu?n c?a mê cung 28x36.
        // interpolation = Nearest gi? nguyên pixel art, không b? m?.
        image.Resize(MazeWidth, MazeHeight, Image.Interpolation.Nearest);

        // Xóa toàn b? ô cu trong GridMap tru?c khi t?o m?i.
        Clear();

        // Duy?t qua t?ng pixel theo chi?u r?ng (x) và chi?u cao (z).
        // Trong GridMap c?a Godot 4:
        //   - x: tr?c ngang (c?t)
        //   - y: tr?c d?ng (l?p) ? d? 0 vì mê cung n?m ph?ng
        //   - z: tr?c sâu (hàng)
        for (int z = 0; z < MazeHeight; z++)
        {
            for (int x = 0; x < MazeWidth; x++)
            {
                // Ð?c màu pixel t?i t?a d? (x, z).
                Color pixelColor = image.GetPixel(x, z);

                // L?c các pixel có s?c xanh duong làm ch? d?o.
                // Ði?u ki?n:
                //   1. Kênh B (Blue) > 0.1 d? lo?i b? n?n den/tr?ng hoàn toàn.
                //   2. B > R và B > G d? d?m b?o xanh chi?m uu th? so v?i d? và l?c.
                if (pixelColor.B > 0.1f && pixelColor.B > pixelColor.R && pixelColor.B > pixelColor.G)
                {
                    // Ð?t m?t ô tu?ng t?i v? trí lu?i (x, 0, z).
                    // Vector3i là ki?u t?a d? nguyên dùng cho GridMap trong Godot 4.
                    SetCellItem(new Vector3i(x, 0, z), WallMeshId);
                }
            }
        }

        GD.Print($"[MazeGenerator] Ðã t?o mê cung {MazeWidth}x{MazeHeight} t? Texture2D.");
    }
}
