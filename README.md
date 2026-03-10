# 🎮 Virtual Controller Emulator

> **A professional-grade Virtual Controller Emulator for Windows** — map your keyboard and mouse to virtual Xbox 360 and DualShock 4 controllers, powered by the [Nefarius ViGEmBus](https://github.com/nefarius/ViGEmBus/releases) driver SDK.

---

## 📸 Screenshots

> _Screenshots coming soon — see the Features section for UI details._

---

## ✨ Features

- 🎮 **Xbox 360 & DualShock 4 Emulation** — Create virtual controllers via ViGEmBus driver
- ⌨️ **Keyboard → Controller Mapping** — Map any key to any controller button
- 🖱️ **Mouse → Analog Stick** — Mouse movement mapped to joystick with configurable sensitivity
- 🔄 **Profile System** — Save/load/switch custom mapping profiles stored as JSON
- ⚡ **Turbo/Rapid Fire** — Configurable rapid-fire (5–30 Hz) for any button
- 🎯 **Deadzone & Sensitivity** — Per-axis deadzone and sensitivity curve settings
- 📋 **Per-Game Profiles** — Auto-switch profiles based on the active game process
- 🖥️ **Modern Dark UI** — Professional WPF dark-themed interface (reWASD / DS4Windows style)
- 📊 **Real-time Controller Visualizer** — Live visualization of all mapped inputs
- 🔔 **System Tray** — Minimize to tray with right-click quick actions
- 🏗️ **MVVM Architecture** — Clean, maintainable codebase

---

## 🛠️ Prerequisites

1. **Windows 10/11 (64-bit)**
2. **[ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases)** — Install before running the app
   - Download the latest `ViGEmBus_Setup_<version>.exe` from the releases page
   - Run the installer and reboot if prompted
3. **.NET 8 Runtime** — Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## 🚀 Build Instructions

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11 (WPF is Windows-only)
- Visual Studio 2022+ or VS Code with C# Dev Kit

### Clone & Build

```bash
git clone https://github.com/mrgold717/the-colonel4.git
cd the-colonel4

# Restore NuGet packages
dotnet restore VirtualControllerEmulator.sln

# Build (Debug)
dotnet build VirtualControllerEmulator.sln

# Build (Release)
dotnet build VirtualControllerEmulator.sln -c Release
```

### Run

```bash
dotnet run --project src/VirtualControllerEmulator/VirtualControllerEmulator.csproj
```

Or open `VirtualControllerEmulator.sln` in Visual Studio and press **F5**.

---

## 📖 Usage Guide

1. **Install the ViGEmBus driver** (see Prerequisites above)
2. Launch **Virtual Controller Emulator**
3. The app will connect to the virtual controller automatically on startup
4. A **default profile** is loaded with standard WASD + mouse layout (see below)
5. Use the **Mapping Editor** to reassign any button:
   - Click the button cell you want to remap
   - Press the desired key/mouse button
   - The mapping updates instantly
6. Use the **Profile Manager** to create, edit, duplicate, import, and export profiles
7. Link a profile to a game executable via **Settings → Per-Game Profile**

---

## 🗺️ Default Mapping Profile

| Keyboard / Mouse       | Controller Action      |
|------------------------|------------------------|
| **W**                  | Left Stick Up          |
| **A**                  | Left Stick Left        |
| **S**                  | Left Stick Down        |
| **D**                  | Left Stick Right       |
| **Arrow Up**           | Right Stick Up         |
| **Arrow Down**         | Right Stick Down       |
| **Arrow Left**         | Right Stick Left       |
| **Arrow Right**        | Right Stick Right      |
| **Space**              | A Button               |
| **Left Shift**         | B Button               |
| **E**                  | X Button               |
| **Q**                  | Y Button               |
| **Left Mouse Button**  | Right Trigger (RT)     |
| **Right Mouse Button** | Left Trigger (LT)      |
| **Mouse Wheel Up**     | Right Bumper (RB)      |
| **Mouse Wheel Down**   | Left Bumper (LB)       |
| **Tab**                | Back / Select          |
| **Enter**              | Start                  |
| **Mouse Movement**     | Right Stick (X/Y)      |
| **1**                  | D-Pad Up               |
| **2**                  | D-Pad Down             |
| **3**                  | D-Pad Left             |
| **4**                  | D-Pad Right            |
| **F**                  | Left Stick Click (L3)  |
| **R**                  | Right Stick Click (R3) |

---

## ⚙️ Configuration Guide

Profiles are stored as JSON files in:
```
%AppData%\VirtualControllerEmulator\Profiles\
```

### Profile JSON Structure

```json
{
  "Id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "Name": "Default",
  "ControllerType": "Xbox360",
  "KeyMappings": [
    {
      "InputKey": 87,
      "InputType": "Key",
      "ControllerButton": "LeftStickUp",
      "TurboEnabled": false,
      "TurboRate": 10
    }
  ],
  "MouseMapping": {
    "TargetStick": "RightStick",
    "Sensitivity": 1.5,
    "InvertX": false,
    "InvertY": false,
    "DeadZone": 0.05
  },
  "LinkedProcess": "game.exe",
  "IsDefault": false
}
```

### Import / Export Profiles

- **Export**: Profile Manager → select profile → **Export** → choose save location
- **Import**: Profile Manager → **Import** → select a `.json` profile file

---

## 🏗️ Project Structure

```
the-colonel4/
├── VirtualControllerEmulator.sln
├── src/
│   └── VirtualControllerEmulator/
│       ├── VirtualControllerEmulator.csproj
│       ├── App.xaml / App.xaml.cs
│       ├── MainWindow.xaml / MainWindow.xaml.cs
│       ├── Models/
│       │   ├── ControllerProfile.cs
│       │   ├── ControllerType.cs
│       │   ├── KeyMapping.cs
│       │   ├── MouseMapping.cs
│       │   └── StickSettings.cs
│       ├── Services/
│       │   ├── VirtualControllerService.cs   ← ViGEmBus integration
│       │   ├── InputCaptureService.cs         ← Low-level hooks
│       │   ├── InputMappingService.cs         ← Input → controller mapping
│       │   ├── ProfileService.cs              ← JSON profile management
│       │   ├── TurboService.cs                ← Turbo/rapid-fire
│       │   └── ProcessMonitorService.cs       ← Auto profile switching
│       ├── ViewModels/
│       ├── Views/
│       ├── Helpers/
│       ├── Converters/
│       └── Resources/
│           └── Styles.xaml                    ← Dark theme
```

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

---

## 🙏 Credits

- **[Nefarius ViGEmBus](https://github.com/nefarius/ViGEmBus)** — Virtual Gamepad Emulation Bus driver
- **[Nefarius.ViGEm.Client](https://github.com/nefarius/ViGEm.NET)** — .NET client library for ViGEmBus
- Inspired by [reWASD](https://www.rewasd.com/) and [DS4Windows](https://github.com/Ryochan7/DS4Windows)