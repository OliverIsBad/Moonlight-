using Raylib_CsLo;
using static Raylib_CsLo.Raylib;
using System.Numerics;
using System;

namespace MoonLightGame.Core;

// Very small particle used for light ambience
public class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float Size;
    public Color Color;

    public Particle(Vector2 pos, Vector2 vel, float life, float size, Color color)
    {
        Position = pos;
        Velocity = vel;
        Life = life;
        Size = size;
        Color = color;
    }

    public void Update(float dt)
    {
        Life -= dt;
        Position += Velocity * dt;
        // simple fade
        float t = Math.Max(0f, Math.Min(1f, Life));
        // reduce alpha based on life (assuming initial life <= ~2s)
        byte a = (byte)(Math.Clamp(t, 0f, 1f) * 255);
        Color.a = a;
    }

    public void Draw()
    {
        // draw small circle in world-space
        DrawCircleV(Position, Size, Color);
    }
}
