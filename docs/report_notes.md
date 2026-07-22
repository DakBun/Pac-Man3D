# Báo cáo đồ án - Đồ án Môn học Đồ họa Máy tính
## Game: 3D Pac-Man (Godot 4.7 + C# / .NET 8)

Tài liệu này lưu trữ bảng ánh xạ giữa thuật toán/lý thuyết cốt lõi
và các file mã nguồn chính trong dự án để phục vụ cho phần viết báo cáo.

---

## Bảng ánh xạ Thuật toán ↔ Mã nguồn ↔ Ghi chú lý thuyết

| # | Tính năng / Thuật toán | File mã nguồn chính | Ghi chú lý thuyết |
|---|-------------------------|---------------------|-------------------|
| 1 | **Grid-based Movement** | `scripts/Player/PlayerController.cs` | Di chuyển theo lưới (grid) tách biệt tọa độ lưới (Vector2I) và world position (Vector3). Pac-Man di chuyển giữa các ô, nhận hướng mới tại giao lộ, dùng `Mathf.Lerp` để nội suy vị trí mượt mà. |
| 2 | **Grid ↔ World Mapping** | `scripts/Maze/MazeGrid.cs` | Chuyển đổi hai chiều giữa `Vector2I` (lưới mê cung) và `Vector3` (world space) dựa trên tâm ô lưới + `_cellSize`. Đây là nền tảng cho mọi hệ thống không gian trong game. |
| 3 | **A* Pathfinding** | `scripts/Maze/PathfindingService.cs` | Thuật toán A-star với heuristic **Manhattan Distance** (chỉ cho phép đi 4 hướng). Sử dụng `PriorityQueue<T>` (C# 12/.NET 8) để mở rộng OpenSet. Có thể giải thích trong báo cáo về độ phức tạp O(E log V). |
| 4 | **Finite State Machine (FSM)** | `scripts/Ghosts/GhostMode.cs`, `scripts/Ghosts/GhostAI.cs` | Mô hình FSM gồm 4 trạng thái: `Scatter`, `Chase`, `Frightened`, `Eaten`. Mỗi trạng thái có chiến lược chọn hướng khác nhau (target riêng hoặc đuổi Player). Có thể dùng `switch` expression trong C# 12 mở rộng. |
| 5 | **Collision Detection (Area3D)** | `scripts/Items/Pellet.cs`, `scripts/Items/PowerPellet.cs` | Sử dụng `Area3D` + signal `BodyEntered` để phát hiện va chạm giữa Pac-Man và Pellet/PowerPellet. Đây là phương pháp phổ biến trong Godot 4 cho va chạm trigger. |
| 6 | **Smoothing Camera (Lerp/Slerp)** | `scripts/Camera/CameraController.cs` | Camera sử dụng `Vector3.Lerp()` vị trí và `Transform3D.Slerp()` rotation để bám theo target (Player) mượt mà, tránh hiện tượng giật. Giúp minh họa kỹ thuật nội suy tuyến tính và nội suy quaternion. |
| 7 | **Singleton Pattern** | `scripts/Core/GameManager.cs` | GameManager đóng vai trò Singleton, quản lý toàn cục trạng thái game (`Ready`, `Playing`, `Paused`, `GameOver`) và dữ liệu (`Score`, `Lives`). Có thể đề cập đến pattern trong phần kiến trúc phần mềm game. |
| 8 | **Signal vs Event** | `scripts/Items/PowerPellet.cs`, `scripts/UI/HudController.cs` | Giao tiếp giữa các component: HUD lắng nghe `OnGameDataChanged` để cập nhật UI; PowerPellet gửi tín hiệu thay đổi mode cho Ghosts. Có thể phân tích ưu/nhược điểm giữa Godot Signal và C# Event. |
| 9 | **Separation of Concerns** | Toàn bộ cấu trúc `scripts/` | Logic lưới (`MazeGrid`) tách khỏi lớp hiển thị (`Pellet`, `Camera`), giúp dễ bảo trì và mở rộng. Phù hợp với mô hình MVC-like trong đồ họa. |

---

## Ghi chú thêm cho báo cáo

1. **Hệ tọa độ**: Trong Godot 4, trục Y thường chỉ lên xuống (chiều cao), nên lưới mê cung 2D được bố trí trên mặt phẳng **XZ** (x: trục ngang, z: trục sâu). Điều này cần được giải thích rõ trong phần "Thiết kế hệ tọa độ".
2. **Tối ưu**: Caching node references trong `_Ready()` thay vì lặp lại `GetNode()` giúp giảm chi phí tính toán trước mỗi frame — điều này rất quan trọng trong một game di chuyển theo lưới với nhiều đối tượng.
3. **C# 12+**: Các record, `ArgumentNullException.ThrowIfNull()`, `switch` expressions là cú pháp hiện đại, giúp code ngắn gọn và an toàn kiểu hơn.
4. **Chiến lược mở rộng**: Sau boilerplate này, có thể thêm hệ thống animation (AnimationPlayer), âm thanh (AudioStreamPlayer3D), và hiệu ứng particle cho PowerPellet.

---

*Tài liệu này sẽ được cập nhật dần sau khi triển khai thêm các tính năng.*
