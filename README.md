# Table Pet

A small always-on-top desktop pet for Windows. It walks around the work area, sits, lies down, and reminds you to drink water.

No Electron, no browser, no installer: a native WPF `.exe`.

## Requirements

- Windows 10 or 11, 64-bit

Friends who receive a published zip do **not** need Visual Studio or a separate .NET install.

Developers need the [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0).

## Run (published zip)

1. Unzip the folder.
2. Double-click `TablePet.exe`.
3. Look at the **center of the desktop**, or the **tray icon** near the clock.

Right-click the tray icon and choose **Exit** to quit. Closing or hiding the pet only hides the sprite; the process keeps running.

## Features

- Borderless, transparent, always-on-top window
- Drag the pet with the left mouse button
- Idle, walk, sit, and lie (random AI, or pick from the menu)
- Horizontal walking stays inside the current monitor work area
- Tray icon: show / hide, poses, settings, exit
- Water reminder with a speech bubble and an optional tray balloon
- Optional click-through when idle so clicks pass through to windows behind the pet
- Position, reminder interval, and click-through persist across restarts

Settings file: `%AppData%\TablePet\settings.json`

## Usage

### Tray menu

Right-click the tray icon:

| Item | Action |
|---|---|
| Show pet | Bring the pet back |
| Hide pet | Hide the sprite (app stays in the tray) |
| Sit / Lie down / Walk | Force a pose |
| Settings... | Reminder interval and click-through |
| Exit | Quit the app |

Double-click the tray icon to show the pet.

### Pet context menu

Right-click the pet: Idle, Walk, Sit, Lie down, Settings, Hide.

### Water reminder

Default interval is **45 minutes** (5–180). When due:

1. The pet sits and shows a bubble: `Time to drink water.`
2. The tray may show the same balloon.
3. Press **Confirm** to dismiss the bubble and restart the interval.

The timer pauses while Settings is open or a bubble is already visible. Next reminder uses the configured interval.

### Click-through

In Settings, enable **Click-through when idle**. Clicks pass through the pet except while you are dragging it. Turn the option off from Settings if you need to grab the pet again.

## Develop

```text
dotnet restore TablePet.sln
dotnet build TablePet.sln
dotnet test TablePet.sln
dotnet run --project src/TablePet/TablePet.csproj
```

Debug builds open a console so startup errors are visible.

### Publish a folder for friends

```text
dotnet publish src/TablePet/TablePet.csproj -c Release -r win-x64 --self-contained true
```

Zip everything under `src/TablePet/bin/Release/net7.0-windows/win-x64/publish/` and send that zip. Recipients double-click `TablePet.exe`. The folder is tens of MB because the .NET runtime is included; that is expected.

## Custom sprites

Place PNG frames next to the manifest:

```text
src/TablePet/Assets/Pet/default/
  manifest.json
  idle/001.png …
  walk/001.png …
  sit/001.png …
  lie/001.png …
```

`manifest.json` example:

```json
{
  "id": "default",
  "frameWidth": 128,
  "frameHeight": 128,
  "fps": 8,
  "clips": {
    "idle": { "fps": 6 },
    "walk": { "fps": 10 },
    "sit": { "fps": 4 },
    "lie": { "fps": 3 }
  }
}
```

Without PNG frames, the pet uses a built-in placeholder drawing. Missing `drag` frames fall back to `idle`. Facing left is a horizontal flip of the right-facing frames.

## Layout

```text
table_pet/
  TablePet.sln
  src/TablePet/          WPF app
    Config/              Defaults (pet, reminder, paths)
    Shell/               Transparent window, click-through, bounds
    Pet/                 State machine, walk, sprite atlas
    Reminder/            Water reminder (IClock for tests)
    Persistence/         settings.json
    Ui/                  Settings window
    Assets/
  tests/TablePet.Tests/  xUnit (no WPF window tests)
```

Constants live in `src/TablePet/Config/*.cs` and are imported by services. Constructors only take fakes needed in tests (`IClock`, `SettingsStore`, `Random`).

## Out of scope (for now)

Climbing other windows, hunger / affection, AI chat, walking across monitors, Steam Workshop, and an installer.

Windows only.

## License

Source in this repository is for this project. Replace bundled art with your own sprites if you redistribute; do not assume third-party pet art is cleared for reuse.
