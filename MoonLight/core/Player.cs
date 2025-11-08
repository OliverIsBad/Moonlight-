using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using MoonLightGame.Graphics;

namespace MoonLightGame.Core;

public class Player : Entity
{
    private Texture _spriteSheet;
    private Rectangle[] _frames;
    private int _currentFrame;
    private float _frameTime;
    private float _frameSpeed = 0.15f;

    public Player(float x, float y) : base(x, y)
    {
        _spriteSheet = SpriteLoader.Load("assets/playerSprites-Sheet.png");
        _frames = SpriteLoader.Slice(_spriteSheet, 32, 32); // Beispiel: 32x32 Frames
    }

    public override void Update()
    {
        // Bewegung
        if (IsKeyDown(KeyboardKey.KEY_RIGHT)) X += 2;
        if (IsKeyDown(KeyboardKey.KEY_LEFT)) X -= 2;

        // Animation
        _frameTime += GetFrameTime();
        if (_frameTime >= _frameSpeed)
        {
            _currentFrame = (_currentFrame + 1) % _frames.Length;
            _frameTime = 0;
        }
    }

    public override void Draw(float scale)
    {
        Rectangle src = _frames[_currentFrame];
        Rectangle dest = new Rectangle(X, Y, src.width * scale, src.height * scale);

        DrawTexturePro(_spriteSheet, src, dest, new System.Numerics.Vector2(0, 0), 0f, WHITE);
    }
}
