
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
        SetTargetFPS(60);

        Game game = new Game();

        while (!WindowShouldClose())
        {
            game.Update();
            game.Draw();
        }

        CloseWindow();


    }
}