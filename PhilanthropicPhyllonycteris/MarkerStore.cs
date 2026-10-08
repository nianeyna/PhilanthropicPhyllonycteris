using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using PhilanthropicPhyllonycteris.Core;

namespace PhilanthropicPhyllonycteris;

/// <summary>
/// Owns the marker file and knows which map is loaded. Everything that shows the path listens
/// to <see cref="Changed"/> rather than to the click that caused it.
/// </summary>
internal sealed class MarkerStore
{
    private static readonly ZeePoint[] NoPath = new ZeePoint[0];

    private readonly string _path;
    private readonly MarkerFile _file;
    private readonly bool _writable = true;
    private string? _fingerprint;
    private ZeePoint[] _current = NoPath;

    public static MarkerStore Instance { get; private set; } = null!;

    /// <summary>Raised when the current map's path changes in any way, and when another map is loaded.</summary>
    public event Action? Changed;

    /// <summary>The loaded map's path in order; empty when it has none or no map is loaded.</summary>
    public IReadOnlyList<ZeePoint> Path => _current;

    /// <summary>The marker the ship is heading for: the first of the path, or null when there is none.</summary>
    public ZeePoint? Next => _current.Length > 0 ? _current[0] : (ZeePoint?)null;

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
        Instance = new MarkerStore(System.IO.Path.Combine(Paths.ConfigPath, Plugin.PluginName + ".markers.txt"));

    /// <summary>Called when the zee has been built for a map.</summary>
    public void MapLoaded(string fingerprint)
    {
        _fingerprint = fingerprint;
        Refresh();
        Changed?.Invoke();
    }

    public void Append(ZeePoint position)
    {
        if (_fingerprint == null) return;
        _file.Append(_fingerprint, new MarkerEntry(position, DateTime.UtcNow));
        Saved();
    }

    public void RemoveAt(int index)
    {
        if (_fingerprint == null || !_file.RemoveAt(_fingerprint, index)) return;
        Saved();
    }

    /// <summary>The ship has reached the first marker, so the next one is up.</summary>
    public void Advance()
    {
        if (_fingerprint == null || !_file.RemoveAt(_fingerprint, 0)) return;
        Plugin.Log.LogInfo(_current.Length > 1 ? "Arrived; on to the next marker." : "Arrived at the last marker.");
        Saved();
    }

    public void Clear()
    {
        if (_fingerprint == null || !_file.Remove(_fingerprint)) return;
        Saved();
    }

    #region Dragging

    /// <summary>
    /// Moves one marker without writing the file, for every frame of a drag. Raises
    /// <see cref="Changed"/>, so the chart follows. An index the path no longer has (it advanced
    /// as the drag began) is ignored.
    /// </summary>
    public void Preview(int index, ZeePoint position)
    {
        if (_fingerprint == null || index < 0 || index >= _current.Length) return;
        _file.Replace(_fingerprint, index, new MarkerEntry(position, DateTime.UtcNow));
        Refresh();
        Changed?.Invoke();
    }

    /// <summary>The drag has ended: whatever the path now is gets written.</summary>
    public void Commit()
    {
        if (_fingerprint == null) return;
        Save();
    }

    #endregion

    /// <summary>A freshly generated map starts unmarked, even if it happens to match one a past captain marked.</summary>
    public void ForgetFreshMap(string fingerprint)
    {
        if (!_file.Remove(fingerprint)) return;
        Plugin.Log.LogInfo("A newly generated zee matched a marked one; its old path was dropped.");
        Save();
        if (fingerprint == _fingerprint)
        {
            Refresh();
            Changed?.Invoke();
        }
    }

    private void Saved()
    {
        Refresh();
        Save();
        Changed?.Invoke();
    }

    // A copy, so listeners iterating it cannot be upset by the file changing under them.
    private void Refresh()
    {
        if (_fingerprint == null)
        {
            _current = NoPath;
            return;
        }
        var entries = _file.Get(_fingerprint);
        var path = new ZeePoint[entries.Count];
        for (var i = 0; i < path.Length; i++) path[i] = entries[i].Position;
        _current = path;
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
