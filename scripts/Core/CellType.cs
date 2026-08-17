namespace PacMan.Core;

/// <summary>Loại ô trong mê cung.</summary>
public enum CellType : byte
{
    Empty = 0,
    Wall = 1,
    Dot = 2,
    PowerPellet = 3,
    GhostHouse = 4,

    /// <summary>Cửa nhà ma: ghost đi qua được, Pac-Man thì không.</summary>
    Door = 5,
}
