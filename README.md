# Stand Up Buddy (起身啦)

Stand Up Buddy is a lightweight desktop break reminder for Windows and macOS. Instead of showing a stiff notification box, it brings large animated characters onto the screen, each with a distinct entrance, message, and short nonverbal sound.

![Web Ranger reminder preview](docs/web-ranger-preview.png)

## Why this project exists

Long vibe-coding sessions with Codex can make it surprisingly easy to lose track of time and stay seated for hours, often leaving your lower back feeling stiff or sore. This project is built for people who spend long stretches at a computer. Its goal is simple: offer a friendly, hard-to-miss reminder to stand up, stretch, and move around for a moment.

## Platforms

| Platform | Implementation | Current status |
| --- | --- | --- |
| Windows 10/11 | Native C# and WinForms | Source included; portable `v1.3.0` executable included |
| macOS 13+ | Native Swift, AppKit, and Core Animation | Source and universal build script included; final build must be produced and tested on a Mac |

The project intentionally avoids Electron, WebView, and always-running high-frame-rate loops. While idle, it performs only lightweight timer checks. Animation work stops after a character settles on screen.

## Features

- Adjustable 1–240 minute reminder interval
- Adjustable 5–120 second reminder duration
- Five large characters with unique entrance animations
- Character artwork sized to occupy roughly one-third of the screen
- One short character sound per appearance, with no speech or text-to-speech
- Random character rotation or manual character selection
- Five-minute snooze
- Windows system tray and macOS menu bar operation
- Optional launch at sign-in
- Offline operation with no analytics or telemetry
- Reduced-motion behavior on macOS

The five characters are an orange cat, a corgi, a red panda, a mech guardian, and the original open-source character **Web Ranger**. Web Ranger was created specifically for this project and does not use Marvel characters, movie costumes, logos, or third-party superhero assets.

## Windows

### Run the portable build

Download [`bin/起身啦.exe`](bin/起身啦.exe) and run it. The app uses the .NET Framework included with Windows 10 and Windows 11 and does not require an installer.

The executable is not commercially code-signed, so Windows may show a first-run security prompt. Verify it against [`SHA256SUMS.txt`](SHA256SUMS.txt) before opening it.

### Build from source

```powershell
.\build.cmd
.\run.cmd
```

The output is written to `bin\起身啦.exe`.

## macOS

The macOS version must be compiled on a Mac because AppKit, the macOS SDK, code signing, and notarization are not available on Windows.

```bash
cd macos
chmod +x build-macos.sh
./build-macos.sh
```

The default script creates a universal Apple Silicon and Intel build at `macos/dist/起身啦.app`. See the [macOS build and validation guide](macos/README.md) for signing, Gatekeeper, and real-device testing requirements.

## Controls

- Left-click the reminder, or press `Return`, `Space`, or `Esc`, to dismiss it.
- Right-click the reminder, or press `S`, to snooze for five minutes.
- Sound can be disabled in settings.

## Project layout

```text
assets/      Shared character, icon, and sound assets
bin/         Portable Windows build
macos/       Native Swift/AppKit implementation
tests/       Windows animation harness
tools/       Asset preparation helper
*.cs         Native Windows implementation
```

## Asset and sound credits

- Cat recording: freemaster2, CC0 1.0
- Dog recording: Kriplozoik at English Wikipedia, CC BY-SA 3.0; edited and redistributed under the same license
- Red panda recording: Mizunoryu, public domain
- Mech and Web Ranger effects: original synthesized sounds created for this project
- Web Ranger artwork: original AI-assisted character artwork created for this open-source release

Full source links and modification notes are available in [`assets/audio/README.md`](assets/audio/README.md). No movie audio or spoken dialogue is included.

## License

The source code and original project assets are available under the [MIT License](LICENSE). Third-party recordings retain their original licenses: the dog recording and its edited derivative are CC BY-SA 3.0, while the cat recording is CC0 and the red panda recording is in the public domain. See the audio credits for details.

## Health note

This app provides general break reminders. It is not medical software and does not diagnose, prevent, or treat any condition. Persistent back, neck, or joint pain should be discussed with a qualified healthcare professional.
