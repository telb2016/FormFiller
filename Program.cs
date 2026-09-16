using FormFiller;
using Microsoft.Playwright;
using System.CommandLine;

var iniOption = new Option<FileInfo>("--ini", "Path to the profile INI file") { IsRequired = true };
var smokeOption = new Option<bool>("--smoke", () => false, "INI round-trip only; no browser");

var root = new RootCommand("FormFiller — Playwright form helper with INI-saved IDs")
{
    iniOption,
    smokeOption
};

root.SetHandler(async (iniFile, smoke) =>
{
    Environment.ExitCode = await RunAsync(iniFile.FullName, smoke);
}, iniOption, smokeOption);

return await root.InvokeAsync(args);

static async Task<int> RunAsync(string iniPath, bool smoke)
{
    try
    {
        EnsureIniExists(iniPath);
        var ini = new IniFile(iniPath);

        if (smoke)
            return RunSmoke(ini, iniPath);

        return await RunPlaywrightAsync(ini, iniPath);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FormFiller failed: {ex.Message}");
        return 1;
    }
}

static void EnsureIniExists(string iniPath)
{
    if (File.Exists(iniPath))
        return;

    var dir = Path.GetDirectoryName(Path.GetFullPath(iniPath));
    if (!string.IsNullOrEmpty(dir))
        Directory.CreateDirectory(dir);

    File.WriteAllText(iniPath, """
        [Form]
        ; Happy-path form config (Linux native Playwright)
        Url=https://example.com/
        ; Optional: CSS selectors to fill before you take over. Leave blank to skip.
        ; Field.email=#email
        ; Field.name=#name
        ; Value.email=
        ; Value.name=

        [SavedIds]
        """);
    Console.WriteLine($"Created new profile: {iniPath}");
}

static int RunSmoke(IniFile ini, string iniPath)
{
    var key = "smoke_test";
    var value = Guid.NewGuid().ToString("N");
    ini.SetValue("SavedIds", key, value);
    ini.Save();

    var roundTrip = new IniFile(iniPath);
    var read = roundTrip.GetValue("SavedIds", key);
    if (!string.Equals(read, value, StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"Smoke FAILED: wrote '{value}' but read '{read ?? "<null>"}' from {iniPath}");
        return 1;
    }

    Console.WriteLine($"SMOKE OK: [SavedIds] {key}={value} written and read back from {iniPath}");
    return 0;
}

static async Task<int> RunPlaywrightAsync(IniFile ini, string iniPath)
{
    var url = ini.GetValue("Form", "Url");
    if (string.IsNullOrWhiteSpace(url))
    {
        Console.Error.WriteLine("Set [Form] Url= in the INI (site/form URL).");
        return 1;
    }

    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
    var page = await browser.NewPageAsync();
    await page.GotoAsync(url);
    Console.WriteLine($"Opened {url}");

    // Real-form hook: any Form Field.<name> + Value.<name> pair gets filled automatically.
    var form = ini.GetSection("Form");
    foreach (var (key, selector) in form)
    {
        if (!key.StartsWith("Field.", StringComparison.OrdinalIgnoreCase))
            continue;
        var name = key["Field.".Length..];
        if (string.IsNullOrWhiteSpace(selector))
            continue;
        if (!form.TryGetValue($"Value.{name}", out var value) || string.IsNullOrEmpty(value))
            continue;
        try
        {
            await page.FillAsync(selector, value);
            Console.WriteLine($"Filled {name} via {selector}");
        }
        catch (PlaywrightException ex)
        {
            Console.WriteLine($"Skip fill {name} ({selector}): {ex.Message}");
        }
    }

    Console.WriteLine("Browser open (headed). Finish the form, then return here.");
    Console.Write("Enter an id to save (or leave blank to skip): ");
    var id = Console.ReadLine()?.Trim();

    if (!string.IsNullOrEmpty(id))
    {
        Console.Write($"Save id '{id}' to [SavedIds] in {iniPath}? [y/N]: ");
        var answer = Console.ReadLine()?.Trim();
        if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
        {
            var key = $"id_{DateTime.UtcNow:yyyyMMddHHmmss}";
            ini.SetValue("SavedIds", key, id);
            ini.Save();
            Console.WriteLine($"Saved [SavedIds] {key}={id}");
        }
        else
        {
            Console.WriteLine("Skipped save.");
        }
    }
    else
    {
        Console.WriteLine("No id entered; nothing saved.");
    }

    var existing = ini.GetSection("SavedIds");
    if (existing.Count > 0)
    {
        Console.WriteLine("Known ids in this profile:");
        foreach (var (k, v) in existing)
            Console.WriteLine($"  {k}={v}");
    }

    return 0;
}
