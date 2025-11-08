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
    public abstract void Draw(float scale);
}