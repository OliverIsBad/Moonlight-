using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using MoonLightGame.Graphics;
using System;
using MoonLightGame.Core;

namespace MoonLightGame.Core;

public class Game
{
    private Player player;

    private float _scale = 4.0f;
    private TileMap _map;
    private Texture _tileSheet;
    private Rectangle[] _tileFrames;
    private bool _showMasks = false;

    public Game()
    {
        // Load tile texture and create a small demo map
        _tileSheet = SpriteLoader.Load("assets/tiles/tile01.png");
        SetTextureFilter(_tileSheet, TextureFilter.TEXTURE_FILTER_POINT);
        _tileFrames = SpriteLoader.Slice(_tileSheet, 32, 32);

        // Determine map size in tiles based on screen size and render scale
        int mapW = (int)Math.Ceiling(GetScreenWidth() / (_scale * 32f));
        int mapH = (int)Math.Ceiling(GetScreenHeight() / (_scale * 32f));
        _map = new TileMap(mapW, mapH, -1);

        // Register a simple tile type (id 1) using the first frame
        var ground = new TileType(1, "Ground", 0) { IsSolid = true };
        _map.RegisterTileType(ground);

        // Fill bottom row as ground
        for (int x = 0; x < mapW; x++)
        {
            _map.SetTile(x, mapH - 1, 1);
        }

        // Create player after the map so we can pass the TileMap reference
        player = new Player(400, 300, _map);
    }

    public void Update()
    {
        // Toggle debug masks
        if (IsKeyPressed(KeyboardKey.KEY_M)) _showMasks = !_showMasks;

        player.Update();
    }

    public void Draw()
    {
        BeginDrawing();
        ClearBackground(RAYWHITE);
        // Draw tiles first, then player
        _map.Draw(_tileSheet, _tileFrames, _scale);
        player.Draw(_scale);

        // Debug: draw collision masks and player bbox
        if (_showMasks)
        {
            _map.DrawCollisionMasks(_scale);

            // Draw player bounding box in blue
            if (player is Player p)
            {
                var r = p.GetBoundingBox();
                DrawRectangleLines((int)r.x, (int)r.y, (int)r.width, (int)r.height, BLUE);
            }
        }
        EndDrawing();
    }
}