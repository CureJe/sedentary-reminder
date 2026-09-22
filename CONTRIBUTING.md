# Contributing to Stand Up Buddy

Useful contributions include reproducible bug reports, accessibility feedback, real-device testing, documentation corrections, and focused fixes. You do not need to add a feature to help.

## Development setup

Clone this repository with its history. On Windows 10 or 11, run `build.cmd`; the script uses the .NET Framework C# compiler included with Windows. Start the app with `run.cmd`. Python 3 is needed only for the repository content check; Pillow is needed only if you work on asset preparation.

On macOS 13 or later, install Xcode Command Line Tools and run `bash macos/build-macos.sh`. See [the macOS guide](macos/README.md) for signing and test limitations.

## Before opening a pull request

1. Describe the problem and the smallest change that solves it. For a substantial feature, discuss its intended behavior in an issue first.
2. Keep UI text, documentation, filenames, and comments in English. Preserve third-party attribution and license notices.
3. Run `python3 tests/check_english.py` before rebuilding to verify the checked-in artifact and content. On Windows, `python` may be the available command instead of `python3`.
4. For Windows changes, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests/check-english.ps1`. It builds the app and generates ignored test renders under `tests/output/`. Inspect any affected screens. Building refreshes the executable checksum; do not include unrelated binary rebuilds in documentation-only PRs.
5. For macOS changes, build on a Mac if available. State clearly if you only have source or CI evidence. Follow the real-device checklist in the macOS guide for interactive changes.
6. Run `git diff --check`. Include the commands you ran, their results, and any untested behavior in the PR description.

Do not commit credentials, local settings, temporary test output, or private screenshots. Keep changes focused and preserve existing user preferences. Contributions made with AI assistance should receive the same review and testing as any other change; document known limitations honestly.

## Reporting issues

Use the issue templates and include the app version, operating system, display scaling, expected behavior, actual behavior, and reproduction steps. For sound, sleep/wake, multiple monitors, or login startup issues, describe the relevant settings. A minimal reproduction is more useful than a large unrelated log.

Check for an existing issue before opening a duplicate. Please be respectful and discuss the code and behavior rather than the person. Maintainer response times are not guaranteed.

## Licensing

Code contributions are submitted under the repository's MIT license. Only contribute work you have the right to share. Third-party assets require compatible licenses, source links, attribution, and notes describing modifications; do not assume every asset is covered by MIT.
