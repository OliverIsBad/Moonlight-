using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using MoonLightGame.Graphics;
using System.Numerics;
using System;
using System.Collections.Generic;
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
    // Night / light settings
    private bool _isNight = true;
    private float _lightRadius = 140f; // in screen pixels
    private Color _nightColor = new Color(0, 0, 0, 200);
    private Camera2D _camera;
    // Simple particle system
    private readonly List<Particle> _particles = new List<Particle>();
    private float _particleSpawnTimer = 0f;
    private Random _rand = new Random();

    public Game()
    {
        // Load tile texture
        _tileSheet = SpriteLoader.Load("assets/tiles/tile01.png");
        SetTextureFilter(_tileSheet, TextureFilter.TEXTURE_FILTER_POINT);
        _tileFrames = SpriteLoader.Slice(_tileSheet, 32, 32);

    // Register a simple tile type (id 1) using the first frame
    var ground = new TileType(1, "Ground", 0) { IsSolid = true };

        // Load map from text file
        var mapping = new Dictionary<char, int> { {'.', -1}, {'#', 1} };
        var (map, hasSpawn, spawnX, spawnY) = MapLoader.LoadFromCharFile("assets/maps/level01.txt", mapping);
    _map = map;
    _map.RegisterTileType(ground);

    // Load example sounds/music (if present)
    AudioManager.LoadSound("jump", "assets/sounds/30_Jump_03.wav");
    AudioManager.LoadSound("land", "assets/sounds/45_Landing_01.wav");
    // footstep (rock) used when player walks
    AudioManager.LoadSound("step_rock_02", "assets/sounds/08_Step_rock_02.wav");
    AudioManager.LoadMusic("theme", "assets/music/theme.mp3");
    // start playing background music (if loaded)
    AudioManager.PlayMusic("theme");

    // Create player (temporary position). We'll compute a better spawn placement below.
    player = new Player(0, 0, _map);

        if (hasSpawn)
        {
            // Determine player's sprite size from its bounding box (uses current frame)
            var pb = player.GetBoundingBox();
            float playerW = pb.width;
            float playerH = pb.height;

            // Find the nearest solid tile at or below the spawn Y in the same column
            int groundY = -1;
            for (int ty = spawnY; ty < _map.Height; ty++)
            {
                if (_map.IsSolidAtTile(spawnX, ty))
                {
                    groundY = ty;
                    break;
                }
            }

            float px = spawnX * _map.TileWidth * _scale + (_map.TileWidth * _scale - playerW) / 2f;
            float py;
            if (groundY >= 0)
            {
                // place player so its feet rest on top of the ground tile
                py = groundY * _map.TileHeight * _scale - playerH;
            }
            else
            {
                // no ground found below spawn, just place at spawn tile y
                py = spawnY * _map.TileHeight * _scale;
            }

            player.X = px;
            player.Y = py;
        }
        else
        {
            // default position
            player.X = 400;
            player.Y = 300;
        }

        // Setup camera to follow player. offset is screen center so player stays centered.
        _camera = new Camera2D();
        _camera.offset = new Vector2(GetScreenWidth() / 2f, GetScreenHeight() / 2f);
        _camera.rotation = 0f;
        _camera.zoom = 1f;
    }

    public void Update()
    {
        // Toggle debug masks
        if (IsKeyPressed(KeyboardKey.KEY_M)) _showMasks = !_showMasks;
        // Toggle night/day
        if (IsKeyPressed(KeyboardKey.KEY_N)) _isNight = !_isNight;

        player.Update();

        // Update camera target to follow player center, clamped to map bounds
    if (_map != null)
        {
            var pb = player.GetBoundingBox();
            Vector2 center = new Vector2(pb.x + pb.width / 2f, pb.y + pb.height / 2f);

            float mapW = _map.Width * _map.TileWidth * _scale;
            float mapH = _map.Height * _map.TileHeight * _scale;

            float halfScreenW = GetScreenWidth() / 2f / _camera.zoom;
            float halfScreenH = GetScreenHeight() / 2f / _camera.zoom;

            float targetX;
            if (mapW <= GetScreenWidth()) targetX = mapW / 2f;
            else targetX = Math.Clamp(center.X, halfScreenW, Math.Max(halfScreenW, mapW - halfScreenW));

            float targetY;
            if (mapH <= GetScreenHeight()) targetY = mapH / 2f;
            else targetY = Math.Clamp(center.Y, halfScreenH, Math.Max(halfScreenH, mapH - halfScreenH));

            _camera.target = new Vector2(targetX, targetY);
        }
        // update audio streaming
        AudioManager.Update();

        // Update particles
        float dt = GetFrameTime();
        // spawn particles only when night is active
        if (_isNight)
        {
            _particleSpawnTimer += dt;
            float spawnInterval = 0.05f; // attempts to spawn every 0.05s
            if (_particleSpawnTimer >= spawnInterval)
            {
                _particleSpawnTimer = 0f;
                // spawn a small random number of particles
                int toSpawn = _rand.Next(0, 3);
                var pb = player.GetBoundingBox();
                Vector2 playerWorldCenter = new Vector2(pb.x + pb.width / 2f, pb.y + pb.height / 2f);
                // convert screen light radius to approximate world units (account for camera zoom)
                float worldRadius = _lightRadius / Math.Max(0.0001f, _camera.zoom);
                for (int i = 0; i < toSpawn; i++)
                {
                    // random angle and distance within radius
                    double ang = _rand.NextDouble() * Math.PI * 2.0;
                    double dist = Math.Sqrt(_rand.NextDouble()) * worldRadius * 0.6; // bias to center
                    Vector2 pos = new Vector2(playerWorldCenter.X + (float)(Math.Cos(ang) * dist), playerWorldCenter.Y + (float)(Math.Sin(ang) * dist));
                    // small random velocity
                    Vector2 vel = new Vector2((float)(_rand.NextDouble() - 0.5) * 10f, (float)(_rand.NextDouble() - 0.5) * 10f);
                    float life = (float)(_rand.NextDouble() * 1.2 + 0.6);
                    float size = (float)(_rand.NextDouble() * 2.0 + 1.0);
                    Color c = new Color((byte)255, (byte)230, (byte)180, (byte)_rand.Next(150, 255));
                    _particles.Add(new Particle(pos, vel, life, size, c));
                }
            }
        }

        // update existing particles and remove dead ones
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            _particles[i].Update(dt);
            if (_particles[i].Life <= 0f) _particles.RemoveAt(i);
        }
    }

    public void Draw()
    {
        BeginDrawing();
        ClearBackground(RAYWHITE);

        // Draw a fullscreen checkerboard background (purple tones) before world rendering
        int cellSize = (int)(_map.TileWidth * _scale);
        if (cellSize <= 0) cellSize = 32;
        int cols = (int)Math.Ceiling(GetScreenWidth() / (double)cellSize) + 1;
        int rows = (int)Math.Ceiling(GetScreenHeight() / (double)cellSize) + 1;
        // two purple tones
        Color darkPurple = new Color(48, 16, 64, 255);
        Color lightPurple = new Color(88, 40, 128, 255);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var c = ((x + y) % 2 == 0) ? darkPurple : lightPurple;
                DrawRectangle(x * cellSize, y * cellSize, cellSize, cellSize, c);
            }
        }

        // World-space rendering using camera: draw tiles first
        BeginMode2D(_camera);
        _map.Draw(_tileSheet, _tileFrames, _scale);
        // draw tile collision masks (debug) here so they get darkened by the overlay
        if (_showMasks) _map.DrawCollisionMasks(_scale);
        EndMode2D();

        // Night/time-of-day overlay and player-centered light (screen-space)
        if (_isNight)
        {
            // Fullscreen dark overlay
            DrawRectangle(0, 0, GetScreenWidth(), GetScreenHeight(), _nightColor);

            // Compute player center in world coords then convert to screen coords
            var pb = player.GetBoundingBox();
            Vector2 playerWorldCenter = new Vector2(pb.x + pb.width / 2f, pb.y + pb.height / 2f);
            Vector2 playerScreen = GetWorldToScreen2D(playerWorldCenter, _camera);

            // Draw a glowing light: bright-ish center -> transparent outer.
            Color inner = new Color(255, 240, 200, 200);
            Color outer = new Color(255, 240, 200, 0);
            DrawCircleGradient((int)playerScreen.X, (int)playerScreen.Y, _lightRadius, inner, outer);
        }

        // Draw particles and player in world-space on top of the overlay so the light appears behind them
        BeginMode2D(_camera);
        // draw particles
        foreach (var part in _particles)
        {
            part.Draw();
        }

        // draw player last so it appears above the light
        player.Draw(_scale);

        // Debug: draw player bounding box in blue if requested
        if (_showMasks)
        {
            var r = player.GetBoundingBox();
            DrawRectangleLines((int)r.x, (int)r.y, (int)r.width, (int)r.height, BLUE);
        }
        EndMode2D();

        EndDrawing();
    }
}