# FormFiller

C# / .NET 8 console app that fills web forms with Playwright and stores reusable IDs in an INI profile.

## CLI

```text
FormFiller.exe --ini "C:\path\to\profile.ini"
FormFiller.exe --ini "C:\path\to\profile.ini" --smoke
```

| Flag | Meaning |
|------|---------|
| `--ini` | **Required.** Profile INI path (created if missing). |
| `--smoke` | INI round-trip only — **no browser**. Safe for Wine smoke tests. |

## INI layout

```ini
[SavedIds]
smoke_test=abc123
id_20260916010101=order-42
```

AutoHotkey can `IniRead` the same file later to pick a known id.

## Build / run

```bash
dotnet build
dotnet run -- --ini /tmp/test.ini --smoke
```

Windows publish (for AHK / Wine):

```bash
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
```

Then AHK:

```text
FormFiller.exe --ini "…\profiles\work.ini"
FormFiller.exe --ini "…\profiles\work.ini" --smoke
```

## Platform notes

- **.NET + Playwright** run natively on Windows and Linux — no Wine required for the C# app.
- **`--smoke`**: launch + read/write `[SavedIds]`, exit 0. Use this under Wine for a Windows-built exe.
- **Real click-through**: headed Playwright on native Windows or Linux. Do **not** expect Chromium automation under Wine.
- **AHK hotkey launcher** on Linux needs Wine (or skip AHK and call `dotnet` directly).

## Playwright browsers (non-smoke)

After first build:

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install chromium
# or: dotnet exec …/playwright.dll install chromium
```

v1 opens `https://example.com/` as a stub — replace URL/selectors for your real form.
