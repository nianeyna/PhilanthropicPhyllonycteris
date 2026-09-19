using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PhilanthropicPhyllonycteris.Core;

/// <summary>A marker and the UTC time it was placed.</summary>
public readonly struct MarkerEntry
{
    public ZeePoint Position { get; }
    public DateTime SetAtUtc { get; }

    public MarkerEntry(ZeePoint position, DateTime setAtUtc)
    {
        Position = position;
        SetAtUtc = setAtUtc;
    }
}

/// <summary>
/// Every map's marker, keyed by <see cref="MapFingerprint"/>, and its on-disk form: a header line
/// naming the format version, then one tab-separated line per map with the fingerprint, the
/// marker's world x and y, and the UTC time it was placed. Lines that cannot be understood are
/// skipped and reported rather than failing the load; a file in another version of the format is
/// refused whole.
/// </summary>
/// <example>
/// PhilanthropicPhyllonycteris markers	1
/// 9c1f06e2a4b7d350	4312.5	-208.25	2026-09-19T14:05:00Z
/// </example>
public sealed class MarkerFile
{
    public const string Header = "PhilanthropicPhyllonycteris markers";
    public const int Version = 1;

    private const char Tab = '\t';
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private readonly Dictionary<string, MarkerEntry> _entries = new Dictionary<string, MarkerEntry>(StringComparer.Ordinal);

    public int Count => _entries.Count;

    public MarkerEntry? Get(string fingerprint) =>
        _entries.TryGetValue(fingerprint, out var entry) ? entry : (MarkerEntry?)null;

    public void Set(string fingerprint, MarkerEntry entry)
    {
        if (!IsValidFingerprint(fingerprint)) throw new ArgumentException("A fingerprint is non-empty and has no tabs or line breaks.", nameof(fingerprint));
        _entries[fingerprint] = entry;
    }

    /// <summary>Forgets a map's marker. Returns whether there was one, so callers can skip the write when nothing changed.</summary>
    public bool Remove(string fingerprint) => _entries.Remove(fingerprint);

    #region Text format

    public string Write()
    {
        var text = new StringBuilder();
        text.Append(Header).Append(Tab).Append(Version).Append('\n');
        foreach (var pair in _entries.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            text.Append(pair.Key).Append(Tab)
                .Append(Number(pair.Value.Position.X)).Append(Tab)
                .Append(Number(pair.Value.Position.Y)).Append(Tab)
                .Append(pair.Value.SetAtUtc.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture)).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>
    /// Reads a file. Lines that cannot be understood are skipped and described in
    /// <paramref name="problems"/>, so a damaged file loses as little as possible. Empty text is
    /// an empty file; any other text must start with the header for this version, or nothing is
    /// read and the header is the one problem reported.
    /// </summary>
    public static MarkerFile Parse(string text, ICollection<string>? problems = null)
    {
        var file = new MarkerFile();
        var lineNumber = 0;
        var headerSeen = false;
        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;
            if (!headerSeen)
            {
                if (line != Header + Tab + Version)
                {
                    problems?.Add($"line {lineNumber}: expected the header \"{Header}\" version {Version}, found \"{line}\"");
                    return file;
                }
                headerSeen = true;
                continue;
            }
            var fields = line.Split(Tab);
            if (fields.Length == 4
                && IsValidFingerprint(fields[0])
                && TryNumber(fields[1], out var x)
                && TryNumber(fields[2], out var y)
                && DateTime.TryParseExact(fields[3], TimestampFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var setAt))
            {
                file._entries[fields[0]] = new MarkerEntry(new ZeePoint(x, y), setAt);
            }
            else
            {
                problems?.Add($"line {lineNumber}: not a marker: \"{line}\"");
            }
        }
        return file;
    }

    private static bool IsValidFingerprint(string fingerprint) =>
        !string.IsNullOrEmpty(fingerprint) && fingerprint.IndexOfAny(new[] { Tab, '\n', '\r' }) < 0;

    // "R" round-trips a float exactly; the invariant culture keeps the decimal point a point everywhere.
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static bool TryNumber(string text, out float value) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && !float.IsNaN(value) && !float.IsInfinity(value);

    #endregion
}
