using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using PhilanthropicPhyllonycteris.Core;

namespace PhilanthropicPhyllonycteris;

/// <summary>
/// Owns the marker file and knows which map is loaded. Everything that shows the marker listens
/// to <see cref="Changed"/> rather than to the click that caused it.
/// </summary>
internal sealed class MarkerStore
{
    private readonly string _path;
    private readonly MarkerFile _file;
    private readonly bool _writable = true;
    private string? _fingerprint;

    public static MarkerStore Instance { get; private set; } = null!;

    /// <summary>Raised when the current map's marker is placed, moved or removed, and when another map is loaded.</summary>
    public event Action? Changed;

    /// <summary>The loaded map's marker, or null when it has none or no map is loaded.</summary>
    public ZeePoint? Current => _fingerprint == null ? null : _file.Get(_fingerprint)?.Position;

    private MarkerStore(string path)
    {
        _path = path;
        _file = new MarkerFile();
        if (!File.Exists(path)) return;
        try
        {
            var problems = new List<string>();
            _file = MarkerFile.Parse(File.ReadAllText(path), problems);
            if (problems.Count > 0)
            {
                // Keep the original before the cleaned copy replaces it on the next change.
                File.Copy(path, path + ".damaged", overwrite: true);
                foreach (var problem in problems) Plugin.Log.LogWarning($"Marker file: {problem}");
            }
        }
        catch (Exception e)
        {
            // Unreadable is not the same as empty: never overwrite what could not be read.
            _writable = false;
            Plugin.Log.LogError($"Could not read {path}; markers will not be saved this session: {e.Message}");
        }
    }

    public static void Initialise() =>
        Instance = new MarkerStore(Path.Combine(Paths.ConfigPath, Plugin.PluginName + ".markers.txt"));

    /// <summary>Called when the zee has been built for a map.</summary>
    public void MapLoaded(string fingerprint)
    {
        _fingerprint = fingerprint;
        Changed?.Invoke();
    }

    public void Set(ZeePoint position)
    {
        if (_fingerprint == null) return;
        _file.Set(_fingerprint, new MarkerEntry(position, DateTime.UtcNow));
        Save();
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_fingerprint == null || !_file.Remove(_fingerprint)) return;
        Save();
        Changed?.Invoke();
    }

    /// <summary>A freshly generated map starts unmarked, even if it happens to match one a past captain marked.</summary>
    public void ForgetFreshMap(string fingerprint)
    {
        if (!_file.Remove(fingerprint)) return;
        Plugin.Log.LogInfo("A newly generated zee matched a marked one; its old marker was dropped.");
        Save();
        if (fingerprint == _fingerprint) Changed?.Invoke();
    }

    // Changes are a click apart at most, so every one is written at once: to a sibling, then swapped in.
    private void Save()
    {
        if (!_writable) return;
        try
        {
            var temp = _path + ".tmp";
            File.WriteAllText(temp, _file.Write());
            if (File.Exists(_path)) File.Replace(temp, _path, null);
            else File.Move(temp, _path);
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"Could not write {_path}: {e.Message}");
        }
    }
}
