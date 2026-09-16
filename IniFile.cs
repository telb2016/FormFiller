using System.Text;

namespace FormFiller;

/// <summary>Minimal INI reader/writer focused on [SavedIds] sections.</summary>
public sealed class IniFile
{
    private readonly string _path;
    private readonly Dictionary<string, Dictionary<string, string>> _sections =
        new(StringComparer.OrdinalIgnoreCase);

    public IniFile(string path)
    {
        _path = path;
        if (File.Exists(path))
            Load();
    }

    public IReadOnlyDictionary<string, string> GetSection(string section)
    {
        if (_sections.TryGetValue(section, out var map))
            return map;
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void SetValue(string section, string key, string value)
    {
        if (!_sections.TryGetValue(section, out var map))
        {
            map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = map;
        }
        map[key] = value;
    }

    public string? GetValue(string section, string key)
    {
        if (_sections.TryGetValue(section, out var map) && map.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(_path));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        foreach (var (section, entries) in _sections)
        {
            sb.Append('[').Append(section).AppendLine("]");
            foreach (var (key, value) in entries)
                sb.Append(key).Append('=').AppendLine(value);
            sb.AppendLine();
        }
        File.WriteAllText(_path, sb.ToString());
    }

    private void Load()
    {
        string? current = null;
        foreach (var raw in File.ReadAllLines(_path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = line[1..^1].Trim();
                if (!_sections.ContainsKey(current))
                    _sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            if (current is null)
                continue;
            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            _sections[current][key] = value;
        }
    }
}
