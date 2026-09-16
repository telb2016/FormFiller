using FormFiller;
using Microsoft.Playwright;
using System.CommandLine;

var iniOption = new Option<FileInfo>("--ini", "Path to the profile INI file") { IsRequired = true };
var smokeOption = new Option<bool>("--smoke", () => false, "INI round-trip only; no browser (Wine-safe)");

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
    // v1 stub: headed browser + placeholder page. Replace URL/selectors with your real form.
    const string placeholderUrl = "https://example.com/";

    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
    var page = await browser.NewPageAsync();
    await page.GotoAsync(placeholderUrl);

    Console.WriteLine("Browser open (headed). Click through your form, then return here.");
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
