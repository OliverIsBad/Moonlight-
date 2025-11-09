
using System;
using MoonLightGame.Core;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace MoonLightGame;

public class Program
{

    public static void Main(string[] args)
    {

    InitWindow(800,600, "Raylib");
    // initialize audio device (via AudioManager wrapper)
    MoonLightGame.Core.AudioManager.Init();
        SetTargetFPS(60);

        Game game = new Game();

        while (!WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

    // shutdown audio before closing window
    MoonLightGame.Core.AudioManager.Shutdown();
    CloseWindow();


    }
}