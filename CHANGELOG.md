# Changelog

## Unreleased

- Add Windows release packaging with version and checksum checks, original sound
  notices, source commit information, and tag-triggered draft releases.
- Add a real-clock Windows timer integration harness and record its validation
  boundary separately from installed-release desktop acceptance.
- Add security and community policies, a desktop test report template, and
  release/acceptance guides.

## 1.3.1 - 2026-09-22

- Convert Windows and macOS UI, accessibility labels, errors, scripts, documentation, filenames, and preview text to English.
- Rename the Windows executable to `StandUpBuddy.exe` and the macOS bundle to `StandUpBuddy.app`.
- Preserve old Windows character selections and document how to refresh the login-startup entry after upgrading.
- Adjust Windows fonts and minimum settings-window bounds; allow macOS reminder subtitles to wrap.
- Rebuild the Windows portable executable and generate its SHA-256 checksum during builds.
- Add English content checks, Windows UI checks, macOS universal-build CI, contribution guidance, and issue templates.

macOS universal compilation and ad hoc signature verification have been checked in CI. Interactive macOS acceptance, Developer ID signing, and notarization are still pending. This release distributes a Windows package only.

## Initial public snapshot - 2026-08-11

- Publish the Windows v1.3.0 executable and native Windows/macOS source under MIT, with separate attribution for third-party audio.
