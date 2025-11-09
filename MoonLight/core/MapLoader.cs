using System;
using System.IO;
using System.Collections.Generic;

namespace MoonLightGame.Core
{
    public static class MapLoader
    {
        // Loads a simple char-based map file. `charToTileId` maps file chars to TileType.Id values.
        // Lines starting with commentPrefix are ignored. Returns the created TileMap and spawn coords if present ('P').
        public static (TileMap map, bool hasSpawn, int spawnX, int spawnY) LoadFromCharFile(string path, Dictionary<char, int> charToTileId, char commentPrefix = ';')
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            var lines = new List<string>();
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.TrimEnd('\r', '\n');
                if (string.IsNullOrWhiteSpace(line)) { lines.Add(""); continue; }
                if (line.Length > 0 && line[0] == commentPrefix) continue;
                lines.Add(line);
            }

            if (lines.Count == 0) return (new TileMap(0,0), false, 0, 0);

            int height = lines.Count;
            int width = 0;
            foreach (var l in lines) if (l.Length > width) width = l.Length;

            var map = new TileMap(width, height, -1);

            bool hasSpawn = false;
            int spawnX = 0, spawnY = 0;

            for (int y = 0; y < height; y++)
            {
                var row = lines[y];
                for (int x = 0; x < width; x++)
                {
                    char c = x < row.Length ? row[x] : '.'; // default empty char

                    if (c == 'P')
                    {
                        hasSpawn = true;
                        spawnX = x;
                        spawnY = y;
                        map.SetTile(x, y, -1);
                    }
                    else if (charToTileId.TryGetValue(c, out int tileId))
                    {
                        map.SetTile(x, y, tileId);
                    }
                    else
                    {
                        map.SetTile(x, y, -1); // unknown -> empty
                    }
                }
            }

            return (map, hasSpawn, spawnX, spawnY);
        }
    }
}
