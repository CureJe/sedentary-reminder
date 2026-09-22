# Windows validation record - 2026-09-22

Maintainer-run automated checks on Windows build 26200. This is not an external
user report. Application source is unchanged from
`c38e22a4c43f71b7ab19e7b018c492cbdddf10e0` (version 1.3.1).

## Commands and observed results

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/check-english.ps1`

- Build passed; 145 checks passed.
- Settings bounds, legacy character compatibility, five character renders and
  programmatic Escape dismissal were checked.

`powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/check-natural-cycle.ps1`

- Reminder 1 appeared at 61.8 seconds.
- Automatic dismissal was observed at 66.9 seconds.
- Reminder 2 appeared at 127.8 seconds; the test invoked S handling to snooze.
- Reminder 3 appeared at 428.9 seconds, after five real minutes.
- Escape completed the reminder and restored the configured one-minute interval.
- The run passed and exited after 429.0 seconds.

The timer test compiles the production forms against in-memory settings, with
sound disabled. It does not advance the clock or replace timer deadlines, read
or write personal preferences, or change startup registration. Key handlers are
invoked programmatically. It is not a test of the installed release or physical
keyboard input.

`python -m unittest discover -s tests -p test_package_windows.py`

- Three tests passed: archive contents and provenance (including refusing to
  overwrite an archive), invalid/mismatched versions, and a changed executable.
- ZIP integrity, executable bytes, license/credit inclusion, and archive checksum
  were verified.

## Still unverified

Real sound playback, sign-in startup, sleep/wake, mixed-DPI/multi-display layouts,
screen-reader operation, Windows 10 hardware, macOS interactive behavior, and
commercial signing/notarization need separate acceptance evidence. Automated
checks do not establish adoption, accessibility conformance, or a security audit.
