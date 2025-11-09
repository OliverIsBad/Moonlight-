using Raylib_CsLo;

namespace MoonLightGame.Core;

public abstract class Entity
{
    public float X { get; set; }
    public float Y { get; set; }

    public Entity(float x, float y)
    {
        X = x;
        Y = y;
    }

    public abstract void Update();
    // showMasks: when true, implementations should draw debug masks (collision boxes, hitboxes)
    public abstract void Draw(float scale, bool showMasks = false);
}