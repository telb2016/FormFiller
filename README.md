# FormFiller

Linux-native C# / .NET 8 + Playwright form helper. Profile INIs store form config and reusable IDs.

**Happy path: Linux native only** (`dotnet` + Playwright + `./formfiller.sh`). Wine is not part of the product path. AutoHotkey is optional for native Windows only.

## Quick start (Linux)

```bash
dotnet build -c Release
# once: install Chromium for Playwright
dotnet build -c Release
pwsh bin/Release/net8.0/playwright.ps1 install chromium   # or: npx playwright install chromium via playwright CLI

./formfiller.sh --smoke          # INI round-trip, exit 0, no browser
./formfiller.sh                  # pick/create profile → headed Playwright
./formfiller.sh work.ini         # use profiles/work.ini
```

## CLI

```text
dotnet run -- --ini path/to/profile.ini
dotnet run -- --ini path/to/profile.ini --smoke
```

| Flag | Meaning |
|------|---------|
| `--ini` | **Required.** Profile INI (created with a `[Form]` stub if missing). |
| `--smoke` | INI round-trip only — no browser. |

## INI layout (real-form hook)

```ini
[Form]
Url=https://your-site.example/form
Field.email=#email
Field.name=input[name="name"]
Value.email=you@example.com
Value.name=Brian

[SavedIds]
id_20260916010101=order-42
```

Any `Field.<name>` + `Value.<name>` pair is filled automatically after navigation. Leave values blank to click through yourself, then optionally save an id when prompted.

## Windows (optional)

```bash
dotnet publish -c Release -r win-x64 -o ./publish
```

AHK can launch `publish\FormFiller.exe --ini "…"` on **native Windows**. Do not expect that exe under Wine.
