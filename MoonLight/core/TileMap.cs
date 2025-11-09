using System;
using System.Collections.Generic;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using System.Numerics;

namespace MoonLightGame.Core;

// A simple tile grid that stores indices to TileType IDs.
public class TileMap
{
    public int Width { get; }
    public int Height { get; }
    public int TileWidth { get; set; } = 32;
    public int TileHeight { get; set; } = 32;

    private int[,] _grid;
    private readonly Dictionary<int, TileType> _types = new();

    // Constructor: width/height in tiles
    public TileMap(int width, int height, int defaultTileId = -1)
    {
        Width = width;
        Height = height;
        _grid = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _grid[x, y] = defaultTileId;
    }

    public void RegisterTileType(TileType type)
    {
        _types[type.Id] = type;
    }

    public TileType? GetTileTypeById(int id)
    {
        return id >= 0 && _types.TryGetValue(id, out var t) ? t : null;
    }

    public void SetTile(int x, int y, int tileId)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        _grid[x, y] = tileId;
    }

    public int GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return -1;
        return _grid[x, y];
    }

    public bool IsSolidAtTile(int x, int y)
    {
        var t = GetTileTypeById(GetTile(x, y));
        return t?.IsSolid ?? false;
    }

    // world coordinates (pixels) -> tile coordinates
    // Convert world (pixel) coords to tile coords. If rendering uses a scale, pass the same scale so
    // conversions match drawn positions.
    public (int tx, int ty) WorldToTile(float worldX, float worldY, float scale = 1f)
    {
        int tx = (int)(worldX / (TileWidth * scale));
        int ty = (int)(worldY / (TileHeight * scale));
        return (tx, ty);
    }

    // Draw all tiles using the provided spritesheet and frame array.
    // scale: global pixel scale (e.g. 4f in your Game)
    public void Draw(Texture spriteSheet, Rectangle[] frames, float scale)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int id = _grid[x, y];
                if (id < 0) continue;

                var tt = GetTileTypeById(id);
                if (tt == null) continue;

                // Choose frame (animated or static)
                int frameIndex = tt.IsAnimated ? tt.GetAnimatedFrame((float)Raylib.GetTime()) : tt.FrameIndex;

                if (frameIndex < 0 || frameIndex >= frames.Length) continue;

                Rectangle src = frames[frameIndex];
                Rectangle dest = new Rectangle(x * TileWidth * scale, y * TileHeight * scale, src.width * scale, src.height * scale);
                DrawTexturePro(spriteSheet, src, dest, new Vector2(0, 0), 0f, WHITE);
            }
        }
    }

    // Debug draw: outlines of all solid tiles (scaled)
    public void DrawCollisionMasks(float scale)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                int id = _grid[x, y];
                if (id < 0) continue;
                var tt = GetTileTypeById(id);
                if (tt == null || !tt.IsSolid) continue;

                float rx = x * TileWidth * scale;
                float ry = y * TileHeight * scale;
                float rw = TileWidth * scale;
                float rh = TileHeight * scale;

                // Draw rectangle outline in red
                DrawRectangleLines((int)rx, (int)ry, (int)rw, (int)rh, RED);
            }
        }
    }

    // Liefert alle Kollisionsrechtecke (AABB) innerhalb eines Bereichs (pixel coords)
    public List<Rectangle> GetCollisionRectsInArea(Rectangle area, float scale = 1f)
    {
        var rects = new List<Rectangle>();
        var topLeft = WorldToTile(area.x, area.y, scale);
        var bottomRight = WorldToTile(area.x + area.width, area.y + area.height, scale);

        for (int tx = Math.Max(0, topLeft.tx); tx <= Math.Min(Width - 1, bottomRight.tx); tx++)
        {
            for (int ty = Math.Max(0, topLeft.ty); ty <= Math.Min(Height - 1, bottomRight.ty); ty++)
            {
                if (IsSolidAtTile(tx, ty))
                {
                    // Return collision rects in world pixel coordinates (scaled)
                    rects.Add(new Rectangle(tx * TileWidth * scale, ty * TileHeight * scale, TileWidth * scale, TileHeight * scale));
                }
            }
        }

        return rects;
    }
}
