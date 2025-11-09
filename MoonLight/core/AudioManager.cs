using System;
using System.Collections.Generic;
using Raylib_CsLo;
using static Raylib_CsLo.Raylib;

namespace MoonLightGame.Core;

// Simple Audio manager wrapper around Raylib audio functionality.
public static class AudioManager
{
    private static readonly Dictionary<string, Sound> _sounds = new();
    private static readonly Dictionary<string, Music> _music = new();
    private static string? _currentMusicKey = null;
    private static float _musicVolume = 1f;
    private static float _sfxVolume = 1f;

    public static void Init()
    {
        try
        {
            if (!Raylib.IsAudioDeviceReady()) Raylib.InitAudioDevice();
        }
        catch { /* ignore on platforms without audio */ }
    }

    public static void Shutdown()
    {
        try
        {
            // unload sounds
            foreach (var kv in _sounds) Raylib.UnloadSound(kv.Value);
            _sounds.Clear();

            foreach (var kv in _music) Raylib.UnloadMusicStream(kv.Value);
            _music.Clear();

            if (Raylib.IsAudioDeviceReady()) Raylib.CloseAudioDevice();
        }
        catch { }
    }

    public static void LoadSound(string key, string path)
    {
        try
        {
            if (_sounds.ContainsKey(key)) return;
            var s = Raylib.LoadSound(path);
            Raylib.SetSoundVolume(s, _sfxVolume);
            _sounds[key] = s;
        }
        catch { /* file may be missing; fail silently */ }
    }

    public static void PlaySound(string key)
    {
        try
        {
            if (_sounds.TryGetValue(key, out var s)) Raylib.PlaySound(s);
        }
        catch { }
    }

    // Play a sound with a custom pitch and relative volume (0..1). Pitch <1.0 lowers pitch (muffled-ish), >1.0 raises it.
    public static void PlaySound(string key, float pitch, float relativeVolume = 1f)
    {
        try
        {
            if (!_sounds.TryGetValue(key, out var s)) return;
            // apply volume relative to master SFX volume
            Raylib.SetSoundVolume(s, Math.Clamp(_sfxVolume * relativeVolume, 0f, 1f));
            // set pitch if available in binding
            try { Raylib.SetSoundPitch(s, pitch); } catch { /* ignore if binding missing */ }
            Raylib.PlaySound(s);
        }
        catch { }
    }

    public static void UnloadSound(string key)
    {
        try
        {
            if (_sounds.TryGetValue(key, out var s)) { Raylib.UnloadSound(s); _sounds.Remove(key); }
        }
        catch { }
    }

    public static void SetSfxVolume(float volume)
    {
        _sfxVolume = Math.Clamp(volume, 0f, 1f);
        foreach (var s in _sounds.Values) Raylib.SetSoundVolume(s, _sfxVolume);
    }

    public static void LoadMusic(string key, string path)
    {
        try
        {
            if (_music.ContainsKey(key)) return;
            var m = Raylib.LoadMusicStream(path);
            Raylib.SetMusicVolume(m, _musicVolume);
            _music[key] = m;
        }
        catch { }
    }

    public static void PlayMusic(string key, bool loop = true)
    {
        try
        {
            if (!_music.TryGetValue(key, out var m)) return;
            if (_currentMusicKey != null && _currentMusicKey != key)
            {
                StopMusic();
            }
            _currentMusicKey = key;
            Raylib.PlayMusicStream(m);
            // Raylib's MusicStream doesn't have a loop flag here; manage externally if needed
        }
        catch { }
    }

    public static void StopMusic()
    {
        try
        {
            if (_currentMusicKey == null) return;
            if (_music.TryGetValue(_currentMusicKey, out var m)) Raylib.StopMusicStream(m);
            _currentMusicKey = null;
        }
        catch { }
    }

    public static void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
        if (_currentMusicKey != null && _music.TryGetValue(_currentMusicKey, out var m)) Raylib.SetMusicVolume(m, _musicVolume);
    }

    // Must be called every frame to stream music
    public static void Update()
    {
        try
        {
            if (_currentMusicKey != null && _music.TryGetValue(_currentMusicKey, out var m)) Raylib.UpdateMusicStream(m);
        }
        catch { }
    }
}
