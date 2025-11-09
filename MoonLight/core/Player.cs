using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using System.Numerics;
using MoonLightGame.Graphics;

namespace MoonLightGame.Core;

public class Player : Entity
{
    private Texture _spriteSheet;
    private Rectangle[] _frames;
    private int _currentFrame;
    private float _frameTime;
    private float _frameSpeed = 0.15f; // Controls animation speed
    private float _scale = 4f;         // Pixel scaling
    private float _speed = 2f;         // Movement speed
    private TileMap? _map;
    private System.Numerics.Vector2 _vel = new System.Numerics.Vector2(0,0);
    private float _gravity = 0.6f;
    private float _jumpSpeed = -10f;
    private bool _onGround = false;

    // Enum representing all animation states
    private enum AnimState
    {
        IdleRight,
        IdleLeft,
        WalkRight,
        WalkLeft,
        JumpRight,
        JumpLeft
    }

    private AnimState _state = AnimState.IdleRight; // Default starting state

    public Player(float x, float y, TileMap? map = null) : base(x, y)
    {
        _map = map;
        // Load the sprite sheet
        _spriteSheet = SpriteLoader.Load("assets/playerSprites-Sheet.png");

        // Keep pixel art sharp
        SetTextureFilter(_spriteSheet, TextureFilter.TEXTURE_FILTER_POINT);

        // Slice the sprite sheet into frames (32x32 each)
        _frames = SpriteLoader.Slice(_spriteSheet, 32, 32);
    }

    public override void Update()
    {
        bool moving = false;

        // Move right
        if (IsKeyDown(KeyboardKey.KEY_D))
        {
            X += _speed;
            _state = AnimState.WalkRight;
            moving = true;
        }
        // Move left
        else if (IsKeyDown(KeyboardKey.KEY_A))
        {
            X -= _speed;
            _state = AnimState.WalkLeft;
            moving = true;
        }

        // Jump (example: space key)
        if (IsKeyPressed(KeyboardKey.KEY_SPACE))
        {
            if (_state == AnimState.WalkRight || _state == AnimState.IdleRight)
                _state = AnimState.JumpRight;
            else if (_state == AnimState.WalkLeft || _state == AnimState.IdleLeft)
                _state = AnimState.JumpLeft;

            moving = true;
        }

        // If no input, set idle based on last direction
        if (!moving)
        {
            if (_state == AnimState.WalkRight || _state == AnimState.JumpRight)
                _state = AnimState.IdleRight;
            else if (_state == AnimState.WalkLeft || _state == AnimState.JumpLeft)
                _state = AnimState.IdleLeft;
        }

        // Apply gravity
        _vel.Y += _gravity;

        // Jump
        if (IsKeyPressed(KeyboardKey.KEY_SPACE) && _onGround)
        {
            _vel.Y = _jumpSpeed;
            _onGround = false;
            if (_state == AnimState.WalkRight || _state == AnimState.IdleRight)
                _state = AnimState.JumpRight;
            else
                _state = AnimState.JumpLeft;
        }

        // Horizontal collision: move horizontally then resolve
        X += _vel.X;
        ResolveCollisions(horizontal: true);

        // Vertical movement and collision
        Y += _vel.Y;
        ResolveCollisions(horizontal: false);

        // Update animation frame
        _frameTime += GetFrameTime();
        if (_frameTime >= _frameSpeed)
        {
            _currentFrame = GetNextFrame();
            _frameTime = 0;
        }
    }

    private void ResolveCollisions(bool horizontal)
    {
        if (_map == null) return;

        // Determine player's bounding box
        var src = _frames[Math.Clamp(_currentFrame, 0, _frames.Length - 1)];
        Rectangle playerRect = new Rectangle(X, Y, src.width * _scale, src.height * _scale);

        var collRects = _map.GetCollisionRectsInArea(playerRect, _scale);
        foreach (var r in collRects)
        {
            if (!CheckCollisionRecs(playerRect, r)) continue;

            // Compute overlap
            float left = MathF.Abs((playerRect.x + playerRect.width) - r.x);
            float right = MathF.Abs((r.x + r.width) - playerRect.x);
            float top = MathF.Abs((playerRect.y + playerRect.height) - r.y);
            float bottom = MathF.Abs((r.y + r.height) - playerRect.y);

            float min = MathF.Min(MathF.Min(left, right), MathF.Min(top, bottom));

            if (min == top)
            {
                // player is above tile -> land on it
                Y = r.y - playerRect.height;
                _vel.Y = 0;
                _onGround = true;
            }
            else if (min == bottom)
            {
                // player hit head
                Y = r.y + r.height;
                _vel.Y = 0;
            }
            else if (min == left)
            {
                // hit from left
                X = r.x - playerRect.width;
                _vel.X = 0;
            }
            else if (min == right)
            {
                // hit from right
                X = r.x + r.width;
                _vel.X = 0;
            }

            // update playerRect for subsequent collisions
            playerRect.x = X;
            playerRect.y = Y;
        }
    }

    // Returns the correct frame index based on current animation state
    private int GetNextFrame()
    {
        switch (_state)
        {
            case AnimState.IdleRight:  return (_currentFrame < 0 || _currentFrame > 1) ? 0 : (_currentFrame + 1) % 2;
            case AnimState.IdleLeft:   return (_currentFrame < 2 || _currentFrame > 3) ? 2 : (_currentFrame + 1) % 2 + 2;
            case AnimState.WalkRight:  return (_currentFrame < 4 || _currentFrame > 5) ? 4 : ((_currentFrame + 1) == 6 ? 4 : _currentFrame + 1);
            case AnimState.WalkLeft:   return (_currentFrame < 6 || _currentFrame > 7) ? 6 : ((_currentFrame + 1) == 8 ? 6 : _currentFrame + 1);
            case AnimState.JumpRight:  return (_currentFrame < 8 || _currentFrame > 9) ? 8 : 9;
            case AnimState.JumpLeft:   return (_currentFrame < 10 || _currentFrame > 11) ? 10 : 11;
            default: return 0;
        }
    }

    public override void Draw(float scale)
    {
        // Draw the current animation frame
        Rectangle src = _frames[_currentFrame];
        Rectangle dest = new Rectangle(X, Y, src.width * _scale, src.height * _scale);
        DrawTexturePro(_spriteSheet, src, dest, new Vector2(0, 0), 0f, WHITE);
    }

    // Expose the player's collision rectangle (in world pixels)
    public Rectangle GetBoundingBox()
    {
        var src = _frames[Math.Clamp(_currentFrame, 0, _frames.Length - 1)];
        return new Rectangle(X, Y, src.width * _scale, src.height * _scale);
    }
}
