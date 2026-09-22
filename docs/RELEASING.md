# Releasing Stand Up Buddy

## Windows

1. Complete the relevant [desktop acceptance checks](TESTING.md). Record the
   exact revision, OS, hardware, results, and untested behavior.
2. Update both versions in `AssemblyInfo.cs` and add a changelog entry. Rebuild
   the checked-in executable and its checksum with `build.cmd`; commit both.
3. Confirm Windows, macOS, and release-package PR checks pass. Merge the change.
4. Push a new tag named `vMAJOR.MINOR.PATCH` on the reviewed commit. Its version
   must match `AssemblyInfo.cs`. Do not move an existing release tag.
5. The **Windows release package** workflow builds from source, runs Windows UI
   and packaging checks, and creates a **draft** release with a ZIP and checksum.
6. Inspect the draft, download and check the package, replace the draft notes
   with the actual changes and known limitations, then publish it.

Pull requests and manual workflow dispatches only create a temporary Actions
artifact. They do not publish or change a release. The workflow will not replace
an existing release or asset. If creation fails after a partial result, inspect
the draft and its assets before retrying.

For a local package after building:

```powershell
python tools/package_windows.py --version 1.3.1
```

Use the version in the checkout. Outputs are in ignored `dist/releases/`.
Choose a fresh `--output` directory when packaging again. The ZIP contains the
executable, license, original sound recordings and attribution, a short guide,
and `build-info.json` with the source commit and executable checksum. Build from
a clean, committed revision; the source commit alone does not describe local edits.

ZIP checksums cover the actual uploaded bytes. Local and CI builds are not
claimed to be byte-for-byte reproducible. A checksum is not code signing.

## macOS

The macOS workflow validates a universal build, metadata, and ad hoc signature.
It does not publish a Mac binary. Complete the [Mac checklist](../macos/README.md),
then separately arrange Developer ID signing and notarization before adding a
public macOS distribution workflow. Do not label an untested build as verified.
