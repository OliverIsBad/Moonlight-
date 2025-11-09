using System;
using System.Collections.Generic;

namespace MoonLightGame.Core;

// Metadata for a tile type. Holds visual settings and simple properties.
public class TileType
{
    public int Id { get; }
    public string Name { get; set; }

    // Index into the sliced sprite array (SpriteLoader.Slice)
    public int FrameIndex { get; set; }

    // Simple physics / interaction flags
    public bool IsSolid { get; set; } = false;
    public bool BlocksLight { get; set; } = false;

    // Animation (optional): indices into the frame array
    public int[]? AnimationFrames { get; set; }
    public float AnimationSpeed { get; set; } = 0.1f; // seconds per frame

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
