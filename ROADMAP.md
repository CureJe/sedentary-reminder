# Roadmap and validation gaps

This is a list of work to validate and discuss, not a promise of dates or completed features.

## Current priorities

- **macOS desktop acceptance:** test on Apple Silicon and Intel where available. Cover English text at smaller display sizes, all five characters, keyboard dismissal, sound, login startup, Reduce Motion, and full-screen Spaces. Follow the macOS checklist and report the exact OS and hardware.
- **Windows natural-cycle testing:** check that reminders fire after the configured interval, snooze for five minutes, resume correctly after sleep, and do not duplicate after login. Check both a fresh install and an upgrade.
- **Accessibility:** verify keyboard-only settings navigation, screen-reader labels, and text at 125%, 150%, and 200% scaling before proposing changes.
- **Multi-display behavior:** record display arrangement and scaling when reporting clipping or unexpected placement.
- **Distribution:** evaluate Developer ID signing and notarization for macOS after interactive acceptance. Windows commercial signing is also not yet provided.

## How to help

Start with one reproducible observation, report expected and actual behavior, and propose a small fix or test if you can. Use [the contribution guide](CONTRIBUTING.md). User reports and external contributions are welcome, but no usage numbers, contributor counts, or compatibility claims will be invented to promote the project.
