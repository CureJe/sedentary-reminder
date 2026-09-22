"""Check current tracked content, bundle paths, and the shipped Windows checksum."""
import hashlib
from pathlib import Path
import plistlib
import re
import subprocess
import unicodedata

ROOT = Path(__file__).resolve().parents[1]
BINARY_EXTENSIONS = {".exe", ".png", ".ico", ".wav", ".ogg"}


def contains_han(text):
    return any("CJK UNIFIED IDEOGRAPH" in unicodedata.name(char, "")
               or "CJK COMPATIBILITY IDEOGRAPH" in unicodedata.name(char, "")
               for char in text)


def main():
    files = subprocess.check_output(
        ["git", "ls-files", "-z"], cwd=ROOT
    ).decode("utf-8").rstrip("\0").split("\0")
    text_count = 0
    for name in files:
        assert not contains_han(name), f"Non-English filename: {name}"
        path = ROOT / name
        if path.suffix.lower() not in BINARY_EXTENSIONS:
            text = path.read_text(encoding="utf-8-sig")
            assert not contains_han(text), f"Non-English text: {name}"
            # Also inspect C#/Swift Unicode escapes, rather than just visible glyphs.
            for match in re.finditer(r"\\u(?:\{([0-9a-fA-F]+)\}|([0-9a-fA-F]{4}))", text):
                assert not contains_han(chr(int(match[1] or match[2], 16))), f"Escaped non-English text: {name}"
            text_count += 1

    with (ROOT / "macos/Info.plist").open("rb") as file:
        plist = plistlib.load(file)
    assert plist["CFBundleDevelopmentRegion"] == "en"
    assert plist["CFBundleDisplayName"] == "Stand Up Buddy"
    assert plist["CFBundleName"] == "Stand Up Buddy"
    assert plist["CFBundleExecutable"] == "StandUpBuddy"
    for name in ("cat", "corgi", "red-panda", "mech", "web-ranger"):
        assert (ROOT / f"assets/characters/{name}.png").is_file()
        assert (ROOT / f"assets/audio/{name}.wav").is_file()
    assert (ROOT / "assets/app/StandUpBuddy-icon.png").is_file()
    expected, executable = (ROOT / "SHA256SUMS.txt").read_text().split()
    assert executable == "bin/StandUpBuddy.exe"
    assert hashlib.sha256((ROOT / executable).read_bytes()).hexdigest() == expected.lower()
    print(f"PASS: {len(files)} tracked filenames, {text_count} text files, macOS metadata/resources, and Windows checksum")


if __name__ == "__main__":
    main()
