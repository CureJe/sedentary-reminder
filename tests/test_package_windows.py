"""Check release contents and reject version or executable mismatches."""
import hashlib
import importlib.util
import json
import re
from pathlib import Path
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("package_windows", ROOT / "tools/package_windows.py")
packager = importlib.util.module_from_spec(spec)
spec.loader.exec_module(packager)
VERSION = re.search(r'AssemblyFileVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.0"\)',
                    (ROOT / "AssemblyInfo.cs").read_text(encoding="utf-8-sig"))[1]


class PackageTests(unittest.TestCase):
    def test_release_contains_binary_provenance_and_credits(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary)
            archive = packager.package(ROOT, output, VERSION)
            with zipfile.ZipFile(archive) as bundle:
                self.assertIsNone(bundle.testzip())
                self.assertEqual(bundle.read("StandUpBuddy.exe"), (ROOT / "bin/StandUpBuddy.exe").read_bytes())
                self.assertIn("assets/audio/source/dog-cc-by-sa.ogg", bundle.namelist())
                self.assertIn("LICENSE", bundle.namelist())
                self.assertIn("CC BY-SA", bundle.read("assets/audio/README.md").decode())
                self.assertRegex(json.loads(bundle.read("build-info.json"))["source_commit"], r"^[a-f0-9]{40}$")
                self.assertNotIn(".git/config", bundle.namelist())
            checksum, name = (output / "SHA256SUMS.txt").read_text().split()
            self.assertEqual(checksum, hashlib.sha256(archive.read_bytes()).hexdigest())
            self.assertEqual(name, archive.name)
            with self.assertRaises(FileExistsError):
                packager.package(ROOT, output, VERSION)

    def test_wrong_version_is_rejected_before_output(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "unused"
            for version in ("../escape", "99.99.99"):
                with self.assertRaises(ValueError):
                    packager.package(ROOT, output, version)
            self.assertFalse(output.exists())

    def test_changed_executable_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / "bin").mkdir()
            (root / "AssemblyInfo.cs").write_bytes((ROOT / "AssemblyInfo.cs").read_bytes())
            (root / "SHA256SUMS.txt").write_bytes((ROOT / "SHA256SUMS.txt").read_bytes())
            (root / "bin/StandUpBuddy.exe").write_bytes(b"changed executable")
            with self.assertRaisesRegex(ValueError, "checksum"):
                packager.package(root, root / "output", VERSION)


if __name__ == "__main__":
    unittest.main()
