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
    // Parallax background layers (optional)
    private Texture _parallaxBg = new Texture();
    private Texture _parallaxFar = new Texture();
    private Texture _parallaxMid = new Texture();
    private Texture _parallaxClose = new Texture();
    private bool _hasParallax = false;
    // Simple particle system
    private readonly List<Particle> _particles = new List<Particle>();
    private float _particleSpawnTimer = 0f;
    // rarer particles that originate from blocks
    private float _blockParticleTimer = 0f;
    private Random _rand = new Random();

    public Game()
    {
        // Load tile texture
        _tileSheet = SpriteLoader.Load("assets/tiles/tile01.png");
        SetTextureFilter(_tileSheet, TextureFilter.TEXTURE_FILTER_POINT);
        _tileFrames = SpriteLoader.Slice(_tileSheet, 32, 32);

        // Try to load parallax pack (optional)
        try
        {
            // Load ParallaxBackground pack (folder: assets/ParallaxBackground)
            _parallaxBg = SpriteLoader.Load("assets/ParallaxBackground/Sky.png");
            _parallaxFar = SpriteLoader.Load("assets/ParallaxBackground/DownLayer.png");
            _parallaxMid = SpriteLoader.Load("assets/ParallaxBackground/MiddleLayer.png");
            _parallaxClose = SpriteLoader.Load("assets/ParallaxBackground/TopLayer.png");
            // use bilinear filter for smooth parallax
            SetTextureFilter(_parallaxBg, TextureFilter.TEXTURE_FILTER_BILINEAR);
            SetTextureFilter(_parallaxFar, TextureFilter.TEXTURE_FILTER_BILINEAR);
            SetTextureFilter(_parallaxMid, TextureFilter.TEXTURE_FILTER_BILINEAR);
            SetTextureFilter(_parallaxClose, TextureFilter.TEXTURE_FILTER_BILINEAR);
            _hasParallax = true;
        }
        catch { _hasParallax = false; }

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
    // alternate footstep (wood) - chosen randomly with rock for variety
    AudioManager.LoadSound("step_wood_03", "assets/sounds/12_Step_wood_03.wav");
    // attack sound (56_Attack_03.wav)
    AudioManager.LoadSound("attack03", "assets/sounds/56_Attack_03.wav");
    AudioManager.LoadMusic("theme", "assets/music/theme.mp3");
    // start playing background music (if loaded)
    AudioManager.PlayMusic("theme");

    // Create player (temporary position). We'll compute a better spawn placement below.
    player = new Player(0, 0, _map);
        // Configure a tighter collision box for the player (source pixels)
        // Using src coords: offsetX=6, offsetY=6, width=20, height=28 (will be multiplied by player._scale)
        try { player.SetCustomBoundingBoxSrc(6, 6, 20, 28); } catch { }

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

            // --- Block-originating particles (rarer than player but increased a bit) ---
            _blockParticleTimer += dt;
            float blockSpawnInterval = 0.35f; // attempt more often
            if (_blockParticleTimer >= blockSpawnInterval)
            {
                _blockParticleTimer = 0f;
                // increased chance to spawn a small burst from a block
                if (_map != null && _rand.NextDouble() < 0.65)
                {
                    // try a few times to find a random solid tile
                    for (int attempt = 0; attempt < 6; attempt++)
                    {
                        int tx = _rand.Next(0, Math.Max(1, _map.Width));
                        int ty = _rand.Next(0, Math.Max(1, _map.Height));
                        if (_map.IsSolidAtTile(tx, ty))
                        {
                            // spawn 1-2 small purple particles near the tile center
                            float tileW = _map.TileWidth * _scale;
                            float tileH = _map.TileHeight * _scale;
                            // base 1-2, then triple the count so blocks emit ~3x more particles
                            int spawnCount = (1 + _rand.Next(0, 2)) * 3; // 3 or 6
                            for (int s = 0; s < spawnCount; s++)
                            {
                                float px = tx * tileW + tileW * 0.5f + (float)(_rand.NextDouble() - 0.5) * 8f;
                                float py = ty * tileH + tileH * 0.5f + (float)(_rand.NextDouble() - 0.5) * 8f;
                                Vector2 pos = new Vector2(px, py);
                                // subtle upward/side velocity with more variety
                                Vector2 vel = new Vector2((float)(_rand.NextDouble() - 0.5) * 10f, (float)(_rand.NextDouble() - 0.8) * 8f);
                                float life = (float)(_rand.NextDouble() * 0.9 + 0.5);
                                float size = (float)(_rand.NextDouble() * 1.8 + 0.7);
                                Color c = new Color((byte)180, (byte)100, (byte)220, (byte)_rand.Next(120, 230));
                                _particles.Add(new Particle(pos, vel, life, size, c));
                            }
                            break;
                        }
                    }
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

        // Parallax background (if present) or fallback checkerboard
    if (_hasParallax)
        {
            // camera world target used for parallax offset
            Vector2 cam = _camera.target;
            int sw = GetScreenWidth();
            int sh = GetScreenHeight();

            // Draw a parallax layer scaled to base size (480x272) then scaled to fill the screen.
            // All layers will be bottom-aligned so they stack exactly on top of each other.
            void DrawLayer(Texture tex, float parallaxFactor)
            {
                if (tex.width == 0 || tex.height == 0) return;

                const float baseW = 480f;
                const float baseH = 272f;
                float scaleX = sw / baseW;
                float scaleY = sh / baseH;
                // scale so the base area fills the screen (no black bars)
                float s = Math.Max(scaleX, scaleY);

                float texDrawW = baseW * s; // target draw width
                float texDrawH = baseH * s; // target draw height

                // compute base x offset so layer scrolls slower than camera (in world units)
                float x = -((cam.X) * (1 - parallaxFactor)) % texDrawW;
                if (x > 0) x -= texDrawW;

                // bottom-align: place so bottom of layer matches bottom of screen
                float y = sh - texDrawH;

                // draw tiled across screen so movement never shows gaps
                for (float px = x - texDrawW; px < sw + texDrawW; px += texDrawW)
                {
                    // DrawTexturePro to scale the original texture into the target base area
                    Rectangle src = new Rectangle(0, 0, tex.width, tex.height);
                    Rectangle dest = new Rectangle(px, y, texDrawW, texDrawH);
                    DrawTexturePro(tex, src, dest, new Vector2(0, 0), 0f, WHITE);
                }
            }

            // background base (furthest)
            DrawLayer(_parallaxBg, 0.05f);
            // far trees
            DrawLayer(_parallaxFar, 0.15f);
            // mid trees
            DrawLayer(_parallaxMid, 0.35f);
            // close trees (slower now)
            DrawLayer(_parallaxClose, 0.4f);
        }
        else
        {
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

            // Draw a glowing light with a softer falloff by compositing two gradients:
            // a stronger inner glow and a wider, faint outer glow for a softer edge.
            Color inner = new Color((byte)255, (byte)240, (byte)200, (byte)200);
            Color outer = new Color((byte)255, (byte)240, (byte)200, (byte)0);
            DrawCircleGradient((int)playerScreen.X, (int)playerScreen.Y, _lightRadius, inner, outer);
            // faint outer halo to soften the transition
            Color mid = new Color((byte)255, (byte)240, (byte)200, (byte)60);
            Color midOuter = new Color((byte)255, (byte)240, (byte)200, (byte)0);
            DrawCircleGradient((int)playerScreen.X, (int)playerScreen.Y, _lightRadius * 1.8f, mid, midOuter);
        }

        // Draw particles and player in world-space on top of the overlay so the light appears behind them
        BeginMode2D(_camera);
        // draw particles
        foreach (var part in _particles)
        {
            part.Draw();
        }

    // draw player last so it appears above the light
    player.Draw(_scale, _showMasks);

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