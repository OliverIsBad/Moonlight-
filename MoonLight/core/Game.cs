using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace MoonLightGame.Core;

public class Game
{
    private Player player;

    private float _scale = 4.0f;

    public Game()
    {
        player = new Player(400, 300);
    }

    public void Update()
    {
        player.Update();
    }

    public void Draw()
    {
        BeginDrawing();
        ClearBackground(RAYWHITE);
        player.Draw(_scale);
        EndDrawing();
    }
}