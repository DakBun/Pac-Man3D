# Pac-Man3D

Trò chơi Pac-Man 3D được phát triển bằng Godot 4.7.1 + C# (.NET 8).

## Cấu trúc thư mục

Pac-Man3D/
├── project.godot                 Cấu hình engine
├── Pac_Man.csproj                Cấu hình biên dịch C#
├── maze_blocks.tres              MeshLibrary: khối tường + vật liệu viền
├── assets/
│   └── maze_map_arcade.png       Ảnh bản đồ 488 × 488
├── scenes/
│   └── main.tscn                 Cảnh chính, cây node
└── scripts/
    ├── Core/
    │   └── GameManager.cs        Singleton: Score, Lives, GameState
    ├── Maze/
    │   ├── MazeGenerator.cs      Đọc ảnh → GridMap → rải vật phẩm
    │   ├── MazeGrid.cs           int[,] + WorldToGrid / GridToWorld
    │   └── PathfindingService.cs A* tĩnh, thuần thuật toán
    ├── Player/
    │   └── PlayerController.cs   Di chuyển lưới, nhập phím, va chạm
    ├── Ghosts/
    │   ├── GhostAI.cs            FSM + gọi A*
    │   └── GhostMode.cs          Kiểu liệt kê 4 trạng thái
    ├── Items/
    │   ├── Pellet.cs             Vật phẩm thường, +10 điểm
    │   └── PowerPellet.cs        Vật phẩm đặc biệt, +50 điểm
    ├── Camera/
    │   └── CameraController.cs   Quỹ đạo cầu, điều khiển chuột
    └── UI/
        └── ScoreHud.cs           Hiển thị điểm số

## Tiến trình phát triển

- [x] Cấu trúc dự án (Godot 4 + C# .NET 8)
- [x] Maze Generator (GridMap) - Đọc ảnh → sinh tường + vật phẩm
- [x] Maze Grid & PathfindingService (A*)
- [x] Ghost AI (FSM)
- [x] Player Controller (di chuyển lưới)
- [x] Hệ thống Items (Pellet, PowerPellet)
- [x] Camera Controller (quỹ đạo cầu + điều khiển chuột)
- [x] HUD (điểm số)

## Cách chạy

```bash
dotnet build Pac_Man.csproj --no-incremental
```

Mở `project.godot` trong Godot Engine để chạy trò chơi.