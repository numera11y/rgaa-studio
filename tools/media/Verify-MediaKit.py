from __future__ import annotations

import json
import zipfile
from collections import Counter
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[2]
KIT = ROOT / "media" / "press-kit"
ARCHIVE = ROOT / "media" / "RGAA-Studio-media-kit.zip"


def main() -> None:
    manifest = json.loads((KIT / "media-manifest.json").read_text(encoding="utf-8"))
    records = manifest["records"]
    categories = Counter(record["category"] for record in records)
    for record in records:
        path = KIT / record["path"]
        if not path.is_file():
            raise SystemExit(f"Missing manifest asset: {record['path']}")
        with Image.open(path) as image:
            image.verify()
        with Image.open(path) as image:
            if image.size != (record["width"], record["height"]):
                raise SystemExit(f"Dimension mismatch: {record['path']} has {image.size}")
        if path.stat().st_size != record["bytes"]:
            raise SystemExit(f"File size changed after manifest creation: {record['path']}")

    expected = {
        "capture": 26,
        "store": 26,
        "composition": 4,
        "web-capture": 26,
        "web-composition": 4,
        "contact-sheet": 1,
    }
    if dict(categories) != expected:
        raise SystemExit(f"Unexpected media counts: {dict(categories)}")
    for path in (KIT / "ANIMATION-STORYBOARD.md", KIT / "README.md", KIT / "CONTACT-SHEET.png"):
        if not path.is_file():
            raise SystemExit(f"Required deliverable is missing: {path.relative_to(KIT)}")

    with zipfile.ZipFile(ARCHIVE) as archive:
        corrupt = archive.testzip()
        if corrupt:
            raise SystemExit(f"Corrupt archive member: {corrupt}")
        archive_names = set(archive.namelist())
        for relative in [
            "media/press-kit/README.md",
            "media/press-kit/ANIMATION-STORYBOARD.md",
            "media/press-kit/media-manifest.json",
            "media/press-kit/captures/06-espace-audit-site.png",
            "media/press-kit/store/06-espace-audit-site.png",
            "media/press-kit/compositions/01-espace-audit.png",
            "media/press-kit/web/compositions/01-espace-audit.webp",
        ]:
            if relative not in archive_names:
                raise SystemExit(f"Archive is missing {relative}")

    print(f"Manifest verified: {len(records)} image assets across {len(categories)} categories")
    print(f"Counts: {dict(categories)}")
    print(f"Archive verified: {ARCHIVE.stat().st_size:,} bytes")


if __name__ == "__main__":
    main()
