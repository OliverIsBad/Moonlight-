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
    private float _speed = 4f;         // Movement speed
    private TileMap? _map;
    private System.Numerics.Vector2 _vel = new System.Numerics.Vector2(0,0);
    private float _gravity = 0.6f;
    private float _jumpSpeed = -15f;
    private bool _onGround = false;
    // Optional attack animation
    private Texture? _attackSheet;
    private Rectangle[]? _attackFrames;
    private bool _hasAttack = false;
    private bool _isAttacking = false;
    private int _attackFrameIndex = 0;
    private float _attackFrameTime = 0f;
    private float _attackFrameSpeed = 0.08f;
    private float _attackCooldown = 0.5f;
    private float _attackCooldownRemaining = 0f;
    // Attack hitbox (world coords) and whether it's active this frame
    private Rectangle _attackHitbox = new Rectangle(0,0,0,0);
    private bool _attackHitboxActive = false;
    // Optional custom bounding box (in world pixels). When enabled, GetBoundingBox() will return
    // this rectangle relative to the player's X/Y. Use SetCustomBoundingBox{World|Src} to configure.
    private bool _useCustomBBox = false;
    private Rectangle _customBBox = new Rectangle(0, 0, 0, 0);

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
    // Footstep sound timer
    private float _stepTimer = 0f;
    private float _stepInterval = 0.35f; // seconds between footstep sounds while walking

    public Player(float x, float y, TileMap? map = null) : base(x, y)
    {
        _map = map;
        // Load the sprite sheet
        _spriteSheet = SpriteLoader.Load("assets/playerSprites-Sheet.png");

        // Keep pixel art sharp
        SetTextureFilter(_spriteSheet, TextureFilter.TEXTURE_FILTER_POINT);

        // Slice the sprite sheet into frames (32x32 each)
        _frames = SpriteLoader.Slice(_spriteSheet, 32, 32);

        // Try to load an optional attack sprite map (64x32 frames)
        try
        {
            var atk = SpriteLoader.Load("assets/player_fighting.png");
            SetTextureFilter(atk, TextureFilter.TEXTURE_FILTER_POINT);
            var atkFrames = SpriteLoader.Slice(atk, 64, 32);
            if (atkFrames != null && atkFrames.Length > 0)
            {
                _attackSheet = atk;
                _attackFrames = atkFrames;
                _hasAttack = true;
            }
        }
        catch { _hasAttack = false; }
    }

    // Set a custom bounding box in world pixels relative to the player's X/Y.
    // Example: SetCustomBoundingBox(2f, 4f, 16f, 28f);
    public void SetCustomBoundingBox(float offsetX, float offsetY, float width, float height)
    {
        _customBBox = new Rectangle(offsetX, offsetY, width, height);
        _useCustomBBox = true;
    }

    // Set a custom bounding box using source sprite pixels (will be multiplied by _scale).
    // Example: SetCustomBoundingBoxSrc(2, 4, 16, 28);
    public void SetCustomBoundingBoxSrc(float srcOffsetX, float srcOffsetY, float srcWidth, float srcHeight)
    {
        SetCustomBoundingBox(srcOffsetX * _scale, srcOffsetY * _scale, srcWidth * _scale, srcHeight * _scale);
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
            // play jump sound (if loaded)
            AudioManager.PlaySound("jump");
        }

        // Horizontal movement and resolve collisions
        X += _vel.X;
        ResolveCollisions(horizontal: true);

        // Vertical movement and resolve collisions
        Y += _vel.Y;
        ResolveCollisions(horizontal: false);

        // Footstep sound: play periodically while moving and on ground
        if (moving && _onGround)
        {
            _stepTimer += GetFrameTime();
            if (_stepTimer >= _stepInterval)
            {
                _stepTimer = 0f;
                // always play rock step sound
                AudioManager.PlaySound("step_rock_02");
            }
        }
        else
        {
            // reset so the next step waits a full interval
            _stepTimer = _stepInterval;
        }

        // --- Attack input & animation ---
            if (_hasAttack)
        {
            // start attack on left mouse press
            if (IsMouseButtonPressed(0) && _onGround && _attackCooldownRemaining <= 0f)
            {
                _isAttacking = true;
                    _attackFrameIndex = 0; // index within direction strip
                _attackFrameTime = 0f;
                _attackCooldownRemaining = _attackCooldown;
                try { AudioManager.PlaySound("attack03"); } catch { }
            }

            if (_isAttacking)
            {
                _attackFrameTime += GetFrameTime();
                if (_attackFrameTime >= _attackFrameSpeed)
                {
                    _attackFrameIndex++;
                    _attackFrameTime = 0f;
                    // determine frames per direction
                    int framesPerDir = (_attackFrames != null && _attackFrames.Length >= 2) ? (_attackFrames.Length / 2) : (_attackFrames?.Length ?? 0);
                    if (framesPerDir <= 0) framesPerDir = _attackFrames?.Length ?? 0;
                    if (_attackFrameIndex >= framesPerDir)
                    {
                        _isAttacking = false;
                        _attackFrameIndex = 0;
                        _attackHitboxActive = false;
                    }
                }
            }

            // Compute attack hitbox when attacking: active during the middle frames
            if (_isAttacking && _attackFrames != null && _attackFrames.Length > 0)
            {
                int startActive = Math.Max(0, _attackFrames.Length / 3);
                int endActive = Math.Max(startActive + 1, 2 * _attackFrames.Length / 3);
                bool active = (_attackFrameIndex >= startActive && _attackFrameIndex < endActive);
                if (active)
                {
                    // derive hitbox from current attack frame size
                    int aidx = Math.Clamp(_attackFrameIndex, 0, _attackFrames.Length - 1);
                    var srcA = _attackFrames[aidx];
                    float drawWA = srcA.width * _scale;
                    float drawHA = srcA.height * _scale;
                    bool facingLeft = (_state == AnimState.WalkLeft || _state == AnimState.IdleLeft || _state == AnimState.JumpLeft);

                    // make hitbox much smaller and place it close to the player's body
                    float hbW = drawWA * 0.18f; // smaller width
                    float hbH = drawHA * 0.18f; // smaller height
                    // move hitbox slightly lower so it aligns with torso/waist
                    float hbY = Y + drawHA * 0.45f;
                    // position relative to player's body center so we can mirror left/right
                    var psrc = _frames[Math.Clamp(_currentFrame, 0, _frames.Length - 1)];
                    float playerDrawW = psrc.width * _scale;
                    float playerCenterX = X + playerDrawW * 0.5f;
                    float sideOffset = playerDrawW * 0.25f;
                    float hbX = facingLeft ? (playerCenterX - sideOffset - hbW) : (playerCenterX + sideOffset);

                    _attackHitbox = new Rectangle(hbX, hbY, hbW, hbH);
                    _attackHitboxActive = true;
                }
                else _attackHitboxActive = false;
            }

            if (_attackCooldownRemaining > 0f) _attackCooldownRemaining -= GetFrameTime();
            if (_attackCooldownRemaining < 0f) _attackCooldownRemaining = 0f;
        }

        // Update animation frame
        // If player is in the air and in a jump state, lock to the last jump frame
        if (!_onGround && (_state == AnimState.JumpRight || _state == AnimState.JumpLeft))
        {
            _currentFrame = (_state == AnimState.JumpRight) ? 9 : 11;
            // keep frameTime as-is (no cycling)
        }
        else
        {
            _frameTime += GetFrameTime();
            if (_frameTime >= _frameSpeed)
            {
                _currentFrame = GetNextFrame();
                _frameTime = 0;
            }
        }

    }
    private void ResolveCollisions(bool horizontal)
    {
        if (_map == null) return;

        // Determine player's bounding box (uses custom box when configured)
        Rectangle playerRect = GetBoundingBox();
        // compute bbox offset relative to player.X/Y so we can adjust X/Y correctly
        float bboxOffsetX = playerRect.x - X;
        float bboxOffsetY = playerRect.y - Y;

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
                // set Y so that player's bbox bottom matches tile top
                Y = r.y - playerRect.height - bboxOffsetY;
                _vel.Y = 0;
                // play landing sound only when we were previously not on ground
                if (!_onGround) AudioManager.PlaySound("land");
                _onGround = true;
            }
            else if (min == bottom)
            {
                // player hit head -> place bbox top at tile bottom
                Y = r.y + r.height - bboxOffsetY;
                _vel.Y = 0;
            }
            else if (min == left)
            {
                // hit from left -> place bbox right at tile left
                X = r.x - playerRect.width - bboxOffsetX;
                _vel.X = 0;
            }
            else if (min == right)
            {
                // hit from right -> place bbox left at tile right
                X = r.x + r.width - bboxOffsetX;
                _vel.X = 0;
            }

            // update playerRect for subsequent collisions
            playerRect.x = X + bboxOffsetX;
            playerRect.y = Y + bboxOffsetY;
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
        // Draw the current animation frame (clamped index)
        if (_isAttacking && _hasAttack && _attackFrames != null && _attackSheet.HasValue)
        {
                // compute frames per direction and base index
                int framesPerDir = _attackFrames.Length / 2;
                if (framesPerDir <= 0) framesPerDir = _attackFrames.Length;
                bool facingLeft = (_state == AnimState.WalkLeft || _state == AnimState.IdleLeft || _state == AnimState.JumpLeft);
                int baseIdx = facingLeft ? framesPerDir : 0;
                int aidx = Math.Clamp(baseIdx + _attackFrameIndex, 0, _attackFrames.Length - 1);
                Rectangle srcA = _attackFrames[aidx];
                float drawWA = srcA.width * _scale;
                float drawHA = srcA.height * _scale;
                // compute player's normal draw width (from regular frames)
                var psrc = _frames[Math.Clamp(_currentFrame, 0, _frames.Length - 1)];
                float playerDrawW = psrc.width * _scale;
                // position attack frame: for right-facing keep X, for left-facing shift left so player's body lines up on right side
                float destAX = facingLeft ? (X - (drawWA - playerDrawW)) : X;
                Rectangle destA = new Rectangle(destAX, Y, drawWA, drawHA);
                DrawTexturePro(_attackSheet.Value, srcA, destA, new Vector2(0, 0), 0f, WHITE);
            // draw attack hitbox for debug/visibility when active
            if (_attackHitboxActive)
            {
                var a = _attackHitbox;
                DrawRectangleLines((int)a.x, (int)a.y, (int)a.width, (int)a.height, RED);
            }
            return;
        }

        int idx = Math.Clamp(_currentFrame, 0, _frames.Length - 1);
        Rectangle src = _frames[idx];
        float drawW = src.width * _scale;
        float drawH = src.height * _scale;

        // Draw normally; left-facing frames are in the sprite sheet so no flipping is necessary
        Rectangle dest = new Rectangle(X, Y, drawW, drawH);
        DrawTexturePro(_spriteSheet, src, dest, new Vector2(0, 0), 0f, WHITE);

    }

    // Expose the player's collision rectangle (in world pixels)
    public Rectangle GetBoundingBox()
    {
        if (_useCustomBBox)
        {
            return new Rectangle(X + _customBBox.x, Y + _customBBox.y, _customBBox.width, _customBBox.height);
        }

        var src = _frames[Math.Clamp(_currentFrame, 0, _frames.Length - 1)];
        float w = src.width * _scale;
        float h = src.height * _scale;
        return new Rectangle(X, Y, w, h);
    }
}
