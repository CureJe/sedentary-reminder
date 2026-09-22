# Desktop acceptance

Record the commit or release version, OS version, CPU architecture, display
resolution and scale, install type, date, steps, and actual results. A checklist
is a plan, not a claim that the tests have passed. Do not include personal data.

See the [2026-09-22 Windows validation record](validation-2026-09-22.md) for
completed automated checks and their limits.

## Windows

- Fresh launch: extract the release ZIP, verify the checksum, launch, and inspect
  settings and tray behavior. Verify no installer or extra runtime is required.
- Natural cycle: select one minute, five seconds, and sound off. Wait without
  pressing Preview. Confirm one reminder appears and disappears automatically,
  then confirm another appears after the next full interval.
- Snooze: press S during a scheduled reminder. Confirm the next reminder appears
  after five minutes, without an earlier popup. Escape should resume the normal
  configured interval.
- Pause: pause before the deadline and wait past it. Resume and check a fresh
  interval begins. Preview should not change the scheduled deadline.
- Sleep/wake: suspend before a reminder is due, resume after the deadline, and
  check that overdue reminders do not arrive as a burst. Record actual behavior.
- Startup: enable launch at sign-in, sign out/in, and confirm one tray instance.
  Disable it and repeat. Do not change unrelated startup entries.
- Upgrade: retain existing preferences, follow the README's startup migration
  steps, and confirm the old executable no longer launches at sign-in.
- Sound: test all characters with sound on/off. Check one sound per appearance.
- Displays: inspect settings and reminders at 100%, 125%, 150%, and 200%, plus
  mixed-scale monitors. Record clipping, focus, and off-screen controls.
- Accessibility: operate settings with Tab/Shift+Tab, Space, and arrow keys.
  Check labels with a screen reader and confirm reminder dismissal keys.

Do not suspend or sign out a machine running unrelated work solely to complete
this checklist. Schedule disruptive checks with its owner.

## macOS

Use the [macOS real-device checklist](../macos/README.md#real-device-validation-checklist).
Report Apple Silicon and Intel results separately. A universal binary does not
prove both hardware types were tested.

## Evidence boundaries

For a roughly seven-minute Windows timer integration check, run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/check-natural-cycle.ps1
```

This compiles the production forms with an in-memory settings substitute. It
uses real WinForms timers without advancing the clock or changing deadlines,
and checks two scheduled appearances, automatic dismissal, five-minute snooze,
Escape handling, and rescheduling. It does not read/write user preferences or
change startup registration. Sound is disabled and test windows are hidden where
possible. Programmatic key handling is not a physical-keyboard acceptance test.
This is a source integration check, not a full installed-release desktop test.

The existing English UI harness checks rendered controls and dismissal logic.
The macOS workflow checks compilation and bundle structure. Neither establishes
audible sound, real login startup, sleep/wake, or multi-display acceptance.
Only describe a behavior as verified when the corresponding evidence is recorded.

Use the **Desktop test report** issue template for a real test result, including
failed and untested items. Reports from the maintainer are not external adoption.
