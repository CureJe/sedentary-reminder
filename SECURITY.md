# Security policy

Security fixes target the latest release and the `main` branch. Older releases
may require an upgrade. macOS builds remain under desktop validation; a passing
build does not establish that the app is ready for distribution.

## Report a vulnerability

Use [GitHub private vulnerability reporting](https://github.com/CureJe/sedentary-reminder/security/advisories/new)
for a suspected security issue. Include the affected version, operating system,
reproduction steps, expected impact, and a minimal demonstration when possible.
Do not include passwords, tokens, personal files, or unnecessary private data.

If the private reporting form is unavailable, open an issue requesting a private
contact channel **without including vulnerability details**. Ordinary crashes,
layout issues, and feature requests belong in the public issue tracker.

This is a volunteer-maintained project. Response and fix timelines are not
guaranteed. Reports will be evaluated and fixes coordinated before public
disclosure where possible.

## Distribution and privacy

- Download releases from this repository. Compare archive hashes with the
  `SHA256SUMS.txt` attached to the same release.
- Checksums detect a changed download; they are not a publisher signature.
- Windows releases are not commercially code-signed. macOS CI uses ad hoc
  signing; Developer ID signing and notarization remain separate requirements.
- The app runs offline and has no analytics or telemetry. Diagnostic reports
  are supplied voluntarily by users.
- Security controls, CI checks, or a clean build are not evidence of a full
  security audit.
