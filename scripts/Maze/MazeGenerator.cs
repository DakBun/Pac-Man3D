using Godot;
using PacManGame.Items;

namespace PacManGame.Maze;

/// <summary>
/// Tự động tạo mê cung 3D từ một Texture2D (ảnh bản đồ).
/// Các pixel có sắc xanh dương làm chủ đạo sẽ được chuyển thành tường (Wall) trong GridMap.
/// </summary>
public partial class MazeGenerator : GridMap
{
    /// <summary>
    /// Ảnh bản đồ mê cung đầu vào. Các vùng màu xanh dương đậm sẽ thành tường.
    /// </summary>
    [Export] public Texture2D? MazeTexture { get; set; }

    /// <summary>
    /// ID mesh tường tương ứng trong GridMap (cần được định nghĩa trong Inspector của GridMap).
    /// </summary>
    [Export] public int WallMeshId { get; set; } = 0;

    /// <summary>
    /// Node MazeGrid nhận ma trận logic của mê cung (dùng cho pathfinding).
    /// Phải gán trong Inspector.
    /// </summary>
    [Export] public MazeGrid? TargetGrid { get; set; }

    /// <summary>
    /// In ma trận mê cung ra Output panel để đối chiếu bằng mắt.
    /// </summary>
    [Export] public bool DebugPrintGrid { get; set; } = true;

    /// <summary>
    /// Tự động rải pellet vào mọi ô đường đi sau khi dựng mê cung.
    /// </summary>
    [Export] public bool SpawnPellets { get; set; } = true;

    // --- Hình học của ảnh bản đồ, đo trực tiếp trên assets/maze_map_arcade.png ---
    // Ảnh gồm các dải xen kẽ: tường dày 8 px, hành lang rộng 40 px, bước lặp 48 px.
    // 11 dải tường + 10 dải hành lang = 21 dải mỗi trục.
    // Kiểm chứng kích thước: 11 * 8 + 10 * 40 = 488 px.
    private const int WallThickness = 8;
    private const int CellPitch = 48;
    private const int MazeSize = 21;
    private const int ExpectedImageSize = 488;

    /// <summary>
    /// Toạ độ pixel tại TÂM của dải thứ i (0..MazeSize-1).
    /// Dải chẵn là tường, dải lẻ là hành lang. Vì hai loại dải rộng khác nhau
    /// (8 px và 40 px) nên KHÔNG thể dùng Image.Resize để lấy mẫu — mọi phép
    /// resize đều lệch nhịp và làm mất tường.
    /// </summary>
    private static int BandCenter(int i)
    {
        if (i % 2 == 0)
        {
            return CellPitch * (i / 2) + WallThickness / 2;
        }

        return CellPitch * ((i - 1) / 2) + WallThickness + (CellPitch - WallThickness) / 2;
    }

    public override void _Ready()
    {
        if (MazeTexture != null)
        {
            GenerateMazeFromImage();
        }
        else
        {
            GD.PrintErr("[MazeGenerator] MazeTexture chưa được gán trong Inspector - không tạo được mê cung.");
        }
    }

    /// <summary>
    /// Rải pellet vào mọi ô đường đi. Toàn bộ pellet dựng bằng code thay vì
    /// PackedScene để khỏi phải quản lý thêm một file .tscn, và để mesh/shape
    /// dùng chung một instance cho cả 237 viên.
    /// </summary>
    private void CreatePellets(int[,] gridData)
    {
        var pelletsRoot = new Node3D { Name = "Pellets" };
        AddChild(pelletsRoot);

        // Dùng chung tài nguyên cho mọi viên để không tạo 237 bản sao.
        var pelletMesh = new SphereMesh
        {
            Radius = 0.12f,
            Height = 0.24f,
            RadialSegments = 8,
            Rings = 4
        };
        var pelletMaterial = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.9f, 0.55f) };
        var pelletShape = new SphereShape3D { Radius = 0.3f };

        int pelletCount = 0;

        for (int z = 0; z < MazeSize; z++)
        {
            for (int x = 0; x < MazeSize; x++)
            {
                if (gridData[z, x] != 0)
                {
                    continue;
                }

                var pellet = new Pellet
                {
                    Name = $"Pellet_{x}_{z}",
                    // Khớp với MazeGrid.GridToWorld: tâm ô là (x + 0.5, z + 0.5).
                    Position = new Vector3(x + 0.5f, 0f, z + 0.5f)
                };

                pellet.AddChild(new MeshInstance3D
                {
                    Mesh = pelletMesh,
                    MaterialOverride = pelletMaterial
                });
                pellet.AddChild(new CollisionShape3D { Shape = pelletShape });

                pelletsRoot.AddChild(pellet);
                pelletCount++;
            }
        }

        GD.Print($"[MazeGenerator] Đã rải {pelletCount} pellet.");
    }

    /// <summary>
    /// Đọc ảnh bản đồ, lấy mẫu tại tâm từng dải và dựng tường GridMap.
    /// </summary>
    private void GenerateMazeFromImage()
    {
        if (MazeTexture == null)
        {
            return;
        }

        // Lưu ý: Texture2D.GetImage() có thể trả về null nếu texture chưa được load.
        Image? image = MazeTexture.GetImage();
        if (image == null)
        {
            GD.PrintErr("[MazeGenerator] MazeTexture không chứa Image hợp lệ.");
            return;
        }

        // Kiểm tra kích thước đầu vào. Các hằng số hình học ở trên chỉ đúng với
        // ảnh 488x488; ảnh khác kích thước sẽ cho mê cung sai âm thầm.
        int w = image.GetWidth();
        int h = image.GetHeight();
        if (w != ExpectedImageSize || h != ExpectedImageSize)
        {
            GD.PrintErr($"[MazeGenerator] Ảnh bản đồ là {w}x{h}, cần {ExpectedImageSize}x{ExpectedImageSize}. Huỷ tạo mê cung.");
            return;
        }

        // Xóa toàn bộ ô cũ trong GridMap trước khi tạo mới.
        Clear();

        // Ma trận logic song song với GridMap: 0 = đường đi, 1 = tường
        int[,] gridData = new int[MazeSize, MazeSize];
        int wallCount = 0;

        // Trong GridMap của Godot 4:
        //   - x: trục ngang (cột)
        //   - y: trục đứng (tầng) — để 0 vì mê cung nằm phẳng
        //   - z: trục sâu (hàng)
        for (int z = 0; z < MazeSize; z++)
        {
            int py = BandCenter(z);

            for (int x = 0; x < MazeSize; x++)
            {
                int px = BandCenter(x);
                Color pixelColor = image.GetPixel(px, py);

                // Lọc các pixel có sắc xanh dương làm chủ đạo.
                //   1. Kênh B (Blue) > 0.1 để loại bỏ nền đen.
                //   2. B > R và B > G để đảm bảo xanh chiếm ưu thế so với đỏ và lục.
                if (pixelColor.B > 0.1f && pixelColor.B > pixelColor.R && pixelColor.B > pixelColor.G)
                {
                    SetCellItem(new Vector3I(x, 0, z), WallMeshId);
                    gridData[z, x] = 1;
                    wallCount++;
                }
                else
                {
                    gridData[z, x] = 0;
                }
            }
        }

        if (TargetGrid != null)
        {
            TargetGrid.LoadGridData(gridData);
        }
        else
        {
            GD.PrintErr("[MazeGenerator] TargetGrid chưa được gán trong Inspector - MazeGrid sẽ rỗng, ghost không tìm đường được.");
        }

        if (SpawnPellets)
        {
            CreatePellets(gridData);
        }

        int freeCount = MazeSize * MazeSize - wallCount;
        GD.Print($"[MazeGenerator] Đã tạo mê cung {MazeSize}x{MazeSize} từ ảnh {w}x{h} - tường: {wallCount}, đường đi: {freeCount}.");

        if (DebugPrintGrid)
        {
            // ponytail: log tạm để đối chiếu mê cung bằng mắt.
            // Tắt bằng cách bỏ tick DebugPrintGrid trong Inspector sau khi chốt.
            for (int z = 0; z < MazeSize; z++)
            {
                var sb = new System.Text.StringBuilder(MazeSize);
                for (int x = 0; x < MazeSize; x++)
                {
                    sb.Append(gridData[z, x] == 1 ? '#' : '.');
                }
                GD.Print($"[MazeGrid] {sb}");
            }
        }
    }
}
