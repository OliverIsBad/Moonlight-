using System;
using System.Collections.Generic;

namespace MoonLightGame.Core;

// Metadaten für einen Tile-Typ. Hier werden Darstellung und einfache Eigenschaften gehalten.
public class TileType
{
    public int Id { get; }
    public string Name { get; set; }

    // Index in das geschnittene Sprite-Array (SpriteLoader.Slice)
    public int FrameIndex { get; set; }

    // Einfache Physik-/Interaktionsflags
    public bool IsSolid { get; set; } = false;
    public bool BlocksLight { get; set; } = false;

    // Animation (optional): Indices in das Frame-Array
    public int[]? AnimationFrames { get; set; }
    public float AnimationSpeed { get; set; } = 0.1f; // Sekunden pro Frame

    public TileType(int id, string name, int frameIndex)
    {
        Id = id;
        Name = name;
        FrameIndex = frameIndex;
    }

    public bool IsAnimated => AnimationFrames != null && AnimationFrames.Length > 0;

    public int GetAnimatedFrame(float time)
    {
        if (!IsAnimated) return FrameIndex;
        int idx = (int)(time / AnimationSpeed) % AnimationFrames!.Length;
        return AnimationFrames[idx];
    }
}
