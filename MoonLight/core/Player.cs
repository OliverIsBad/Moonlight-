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

    public Player(float x, float y) : base(x, y)
    {
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

        // Update animation frame
        _frameTime += GetFrameTime();
        if (_frameTime >= _frameSpeed)
        {
            _currentFrame = GetNextFrame();
            _frameTime = 0;
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
}
