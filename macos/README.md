# Stand Up Buddy for macOS

This is the native macOS edition of Stand Up Buddy. It uses Swift, AppKit, and Core Animation, with no Electron, WebView, or third-party runtime. The minimum supported version is macOS 13.

## Included features

- Menu bar operation without a persistent Dock icon
- Five distinct entrance animations: climb, run, tumble, mech landing, and filament launch
- Transparent borderless reminder layer with a character occupying roughly one-third of the screen
- One nonverbal sound per appearance, with no spoken reminder
- Left-click or `Return` / `Space` to complete a break
- Right-click or `S` to snooze for five minutes
- Login-item registration through `SMAppService`
- Reduced-motion behavior that replaces large movement with a short fade
- Universal Apple Silicon and Intel build by default

## Build on a Mac

Install the Xcode Command Line Tools, then run:

```bash
cd macos
chmod +x build-macos.sh
./build-macos.sh
```

Output: `macos/dist/StandUpBuddy.app`

The app's display language is English. The existing bundle identifier is intentionally retained so macOS can continue using saved preferences and the existing app identity. Quit and replace the old bundle, then check launch-at-login registration after moving or renaming it.

To build only for the current Mac architecture:

```bash
ARCHS="$(uname -m)" ./build-macos.sh
```

The script uses an ad hoc signature by default for local testing. To distribute the app to other people, use an Apple Developer ID, hardened runtime, and Apple notarization:

```bash
SIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" ./build-macos.sh
```

The script applies the hardened runtime when a Developer ID is supplied, but notarization must still be completed with Xcode or `notarytool` before public distribution.

## First launch and login item

An ad hoc signed test build may require a manual Gatekeeper override on first launch. If login launch is awaiting approval, open **System Settings > General > Login Items** and allow Stand Up Buddy.

## Automated runtime checks without a personal Mac

The [macOS runtime workflow](https://github.com/CureJe/sedentary-reminder/actions/workflows/macos-runtime.yml)
runs on pull requests, pushes to main, and manual dispatch. It uses separate
standard `macos-15` (arm64) and `macos-15-intel` (x86_64) GitHub-hosted runners.
Each runner compiles the universal production app, then builds a native test
bundle from the same application sources with an isolated preferences suite.
No personal preferences or login-item registration are changed.

On a Mac with an active GUI session, reproduce the check with:

```bash
bash macos/build-macos.sh
bash macos/check-runtime.sh
```

The harness exercises launch, menu pause/resume, settings actions and bounds,
settings limits, character rotation, all five character windows, image/audio
decoding, dismissal keys, preview lifetime, and duplicate-window prevention.
It waits for two real one-minute reminders, a five-second automatic dismissal,
and a full five-minute snooze before checking normal rescheduling. It does not
advance the clock or shorten the production timer intervals.

Download `macos-runtime-macos-15` and `macos-runtime-macos-15-intel` from a
completed workflow run. Each contains `report.json`, `runtime.log`,
`settings.png`, and five `character-*.png` renders. The report records the
architecture, macOS version, tested Git commit, elapsed time, checks, and limits.
Artifacts expire after 30 days; retain a validation record for durable results.

## Real-device validation checklist

1. Launch on both Apple Silicon and Intel Macs; confirm the menu bar icon appears and no persistent Dock icon remains.
2. Preview all five characters and confirm none are clipped by the menu bar, Dock, or display notch.
3. Confirm that every appearance plays no more than one sound and that disabling sound makes the reminder silent.
4. Dismiss while each animation is still moving to verify immediate interruption.
5. Enable **Reduce motion** in macOS accessibility settings and confirm that translation, rotation, and scaling are replaced by a short fade.
6. Enable launch at login, sign out, and sign back in; confirm that only one app instance starts.
7. Test on a regular desktop, multiple displays, and a full-screen app Space.

## Current validation boundary

The macOS GitHub Actions workflow compiles both arm64 and x86_64 using the macOS/Xcode SDK, builds a universal app bundle, and verifies its ad hoc signature and English bundle metadata. It also checks repository text, resource paths, and the shipped Windows checksum with `python3 tests/check_english.py`.

The additional runtime workflow executes AppKit in hosted macOS VMs. It uses
programmatic control/key actions and captures its own views, not the desktop.
Audio files are decoded with sound disabled. Physical keyboard and audible
sound, login items, sleep/wake, display notches, multiple displays, full-screen
Spaces, and changing the system Reduce Motion preference remain unverified.
The deployment target is macOS 13, but runtime evidence covers only the macOS
version reported by CI. Developer ID signing, notarization, and Gatekeeper
distribution acceptance remain unverified; CI does not publish a macOS release
binary.
