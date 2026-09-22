"""Package a built Windows executable with provenance and third-party notices."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def package(root, output, version):
    if not re.fullmatch(r"[0-9]+\.[0-9]+\.[0-9]+", version):
        raise ValueError("Version must be MAJOR.MINOR.PATCH")
    assembly = (root / "AssemblyInfo.cs").read_text(encoding="utf-8-sig")
    for field in ("AssemblyVersion", "AssemblyFileVersion"):
        match = re.search(field + r'\("([0-9.]+)"\)', assembly)
        if not match or match[1] != version + ".0":
            raise ValueError("Requested version does not match " + field)
    executable = root / "bin/StandUpBuddy.exe"
    digest = hashlib.sha256(executable.read_bytes()).hexdigest()
    expected, name = (root / "SHA256SUMS.txt").read_text().split()
    if name != "bin/StandUpBuddy.exe" or expected.lower() != digest:
        raise ValueError("Built executable checksum does not match SHA256SUMS.txt")
    commit = subprocess.check_output(
        ["git", "rev-parse", "HEAD"], cwd=root, text=True
    ).strip()
    files = {
        "StandUpBuddy.exe": executable,
        "LICENSE": root / "LICENSE",
        "assets/README.md": root / "assets/README.md",
        "assets/audio/README.md": root / "assets/audio/README.md",
    }
    for name in ("cat-cc0.wav", "dog-cc-by-sa.ogg", "red-panda-public-domain.ogg"):
        files["assets/audio/source/" + name] = root / "assets/audio/source" / name
    for path in files.values():
        if not path.is_file():
            raise ValueError("Missing release input: " + str(path))
    output.mkdir(parents=True, exist_ok=True)
    archive = output / ("StandUpBuddy-v" + version + "-windows.zip")
    if archive.exists() or (output / "SHA256SUMS.txt").exists():
        raise FileExistsError("Use a new output directory; release files are never overwritten")
    metadata = {"version": version, "source_commit": commit,
                "executable_sha256": digest, "platform": "Windows 10/11"}
    readme = f"""# Stand Up Buddy v{version} for Windows

Extract the entire ZIP, then run StandUpBuddy.exe on Windows 10 or 11.
The app uses the .NET Framework supplied by Windows. No installer is needed.
Closing settings keeps reminders running in the tray; use the tray menu to quit.

This executable is not commercially code-signed. Verify the ZIP against the
SHA256SUMS.txt attached to the same GitHub release before opening it.

Source, setup, upgrade notes, support, and validation limitations:
https://github.com/CureJe/sedentary-reminder

Code and original assets: MIT (LICENSE). Third-party sound licenses and source
links are in assets/audio/README.md, with original recordings in its source folder.
build-info.json identifies the source commit and executable SHA-256.
"""
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
        for name, path in sorted(files.items()):
            bundle.write(path, name)
        bundle.writestr("README.md", readme)
        bundle.writestr("build-info.json", json.dumps(metadata, indent=2) + "\n")
    with zipfile.ZipFile(archive) as bundle:
        if bundle.testzip() is not None:
            raise ValueError("Release ZIP integrity check failed")
        if hashlib.sha256(bundle.read("StandUpBuddy.exe")).hexdigest() != digest:
            raise ValueError("Packaged executable changed")
    archive_hash = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / "SHA256SUMS.txt").write_text(
        archive_hash + "  " + archive.name + "\n", encoding="ascii"
    )
    print("Packaged " + archive.name + " (sha256:" + archive_hash + ")")
    return archive


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--output", type=Path, default=ROOT / "dist/releases")
    args = parser.parse_args()
    package(ROOT, args.output, args.version)
