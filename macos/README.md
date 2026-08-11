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

Output: `macos/dist/起身啦.app`

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

## Real-device validation checklist

1. Launch on both Apple Silicon and Intel Macs; confirm the menu bar icon appears and no persistent Dock icon remains.
2. Preview all five characters and confirm none are clipped by the menu bar, Dock, or display notch.
3. Confirm that every appearance plays no more than one sound and that disabling sound makes the reminder silent.
4. Dismiss while each animation is still moving to verify immediate interruption.
5. Enable **Reduce motion** in macOS accessibility settings and confirm that translation, rotation, and scaling are replaced by a short fade.
6. Enable launch at login, sign out, and sign back in; confirm that only one app instance starts.
7. Test on a regular desktop, multiple displays, and a full-screen app Space.

## Current validation boundary

The source was prepared in a Windows workspace. AppKit can only be compiled with the macOS/Xcode SDK, so the final app bundle, Developer ID signing, notarization, Gatekeeper behavior, and multi-display animation must be verified on a real Mac before publishing a macOS binary.
