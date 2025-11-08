
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace MoonLightGame.Graphics;

public static class SpriteLoader
{
    public static Texture Load(string path)
    {
        return LoadTexture(path);
    }

    public static Rectangle[] Slice
    (Texture texture, int frameWidth, int frameHeight)
    {
        int columns = texture.width / frameWidth;
        int rows = texture.height / frameHeight;
        Rectangle[] frames = new Rectangle[columns * rows];

        int i = 0;
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
                frames[i++] = new Rectangle
                (x * frameWidth, y * frameHeight, frameWidth, frameHeight);

        return frames;
    }
}
