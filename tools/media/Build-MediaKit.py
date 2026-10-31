from __future__ import annotations

import json
import shutil
import subprocess
import zipfile
from datetime import date
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


ROOT = Path(__file__).resolve().parents[2]
KIT = ROOT / "media" / "press-kit"
CAPTURES = KIT / "captures"
STORE = KIT / "store"
COMPOSITIONS = KIT / "compositions"
WEB = KIT / "web"
ARCHIVE = ROOT / "media" / "RGAA-Studio-media-kit.zip"
ICON = ROOT / "src" / "RgaaStudio.Desktop" / "Assets" / "RgaaStudio.ico"
SIZE = (1920, 1080)
INK = "#171F3B"
BLUE = "#214B9A"
PAPER = "#EEF2FA"
WARM = "#FFFDF7"

CAPTURE_LABELS = {
    "01-mes-audits.png": "Mes audits",
    "02-menu-options.png": "Menu Options",
    "03-parametres.png": "Paramètres et données locales",
    "04-a-propos.png": "À propos et licence MIT",
    "05-tableau-de-bord.png": "Tableau de bord du dossier",
    "06-espace-audit-site.png": "Site numera11y dans l’espace d’audit",
    "07-tableau-de-bord.png": "Progression et indicateurs du dossier",
    "08-menu-vues-page.png": "Sélecteur des vues de page",
    "09-code-source.png": "Code source DOM",
    "10-site-sans-css.png": "Page sans CSS",
    "11-html5-semantique.png": "Structure HTML5 sémantique",
    "12-images-medias.png": "Repérage des images et médias",
    "13-vue-aria.png": "Aperçu de l’arbre ARIA",
    "14-technologies-seo.png": "Technologies et indicateurs SEO",
    "15-resultats-axe-core.png": "Suggestions axe-core à examiner",
    "16-page-offre-audit.png": "Page de l’offre d’audit d’accessibilité",
    "17-page-offres.png": "Page des offres numera11y",
    "18-page-realisations.png": "Page des réalisations numera11y",
    "19-page-rendez-vous.png": "Page de prise de rendez-vous",
    "20-page-ressources.png": "Page des ressources numera11y",
    "21-page-mentions-legales.png": "Mentions légales numera11y",
    "22-pipette-contraste.png": "Mesure de contraste sur le site",
    "23-espace-web-etendu.png": "Espace du navigateur élargi",
    "24-liste-pages-repliee.png": "Liste des pages repliée",
    "25-creation-audit.png": "Création d’un audit RGAA",
    "26-creation-wcag-niveau.png": "Création d’un audit WCAG cible AA",
}

COMPOSITION_BRIEFS = [
    (
        "01-espace-audit",
        "Le site, ses pages,\nson audit.",
        "Retrouvez les pages du périmètre et la page observée dans un même espace de travail.",
        "06-espace-audit-site.png",
        "Le site et son périmètre dans l’espace d’audit",
    ),
    (
        "02-suggestions-axe-core",
        "Des suggestions à examiner.\nPas un verdict.",
        "Les résultats automatisés guident l’examen ; la décision reste celle de l’auditeur.",
        "15-resultats-axe-core.png",
        "Résultats axe-core présentés comme pistes à vérifier",
    ),
    (
        "03-structure-accessible",
        "Explorer le contenu\net sa structure.",
        "Code source, structure HTML5 et aperçu ARIA : plusieurs angles pour la même page.",
        "11-html5-semantique.png",
        "Structure HTML5 et page du site affichées côte à côte",
    ),
    (
        "04-apercu-technique",
        "Les repères techniques,\nau fil de l’audit.",
        "Consultez les technologies détectées et les indicateurs SEO de la page sélectionnée.",
        "14-technologies-seo.png",
        "Résumé des technologies et indicateurs SEO",
    ),
]


def font(size: int, weight: str = "regular") -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    names = {
        "regular": ["segoeui.ttf", "arial.ttf"],
        "semibold": ["seguisb.ttf", "arialbd.ttf"],
        "bold": ["segoeuib.ttf", "arialbd.ttf"],
    }[weight]
    for name in names:
        candidate = Path("C:/Windows/Fonts") / name
        if candidate.exists():
            return ImageFont.truetype(str(candidate), size)
    return ImageFont.load_default()


def make_store_images(captures: list[Path]) -> list[Path]:
    STORE.mkdir(parents=True, exist_ok=True)
    out = []
    for source in captures:
        with Image.open(source) as original:
            image = original.convert("RGB")
        canvas = Image.new("RGB", SIZE, PAPER)
        x = (SIZE[0] - image.width) // 2
        y = (SIZE[1] - image.height) // 2
        canvas.paste(image, (x, y))
        target = STORE / source.name
        canvas.save(target, "PNG", optimize=True)
        out.append(target)
    return out


def rounded_screenshot(source: Path, target_width: int) -> Image.Image:
    with Image.open(source) as loaded:
        screenshot = loaded.convert("RGB")
    target_height = round(screenshot.height * target_width / screenshot.width)
    screenshot = screenshot.resize((target_width, target_height), Image.Resampling.LANCZOS)
    mask = Image.new("L", screenshot.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, screenshot.width - 1, screenshot.height - 1), radius=14, fill=255)
    screenshot.putalpha(mask)
    return screenshot


def make_composition(name: str, headline: str, subhead: str, screenshot_name: str, alt: str) -> Path:
    COMPOSITIONS.mkdir(parents=True, exist_ok=True)
    image = Image.new("RGB", SIZE, PAPER)
    draw = ImageDraw.Draw(image)

    # Soft brand wash with a restrained warm edge.
    wash = Image.new("RGB", SIZE, "#F3EEFF")
    wash_draw = ImageDraw.Draw(wash)
    wash_draw.ellipse((1080, -370, 2180, 610), fill="#FDEDE6")
    wash_draw.ellipse((-500, 670, 580, 1740), fill="#E4ECFB")
    image = Image.blend(image, wash, 0.44)
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, SIZE[0], 9), fill=BLUE)

    if ICON.exists():
        with Image.open(ICON) as icon_source:
            icon = icon_source.convert("RGBA")
            icon.thumbnail((46, 46), Image.Resampling.LANCZOS)
        image.paste(icon, (92, 42), icon)
    draw = ImageDraw.Draw(image)
    draw.text((154, 48), "RGAA STUDIO", font=font(21, "bold"), fill=INK)
    draw.text((154, 77), "NUMERA11Y  ·  ACCESSIBILITÉ NUMÉRIQUE", font=font(12, "semibold"), fill=BLUE)

    draw.multiline_text((92, 127), headline, font=font(54, "bold"), fill=INK, spacing=0, stroke_width=0)
    headline_box = draw.multiline_textbbox((92, 127), headline, font=font(54, "bold"), spacing=0)
    subhead_y = headline_box[3] + 12
    draw.multiline_text((96, subhead_y), subhead, font=font(20), fill="#45536D", spacing=4)

    source = CAPTURES / screenshot_name
    shot = rounded_screenshot(source, 1420)
    shot_x, shot_y = 250, 315
    shadow = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle(
        (shot_x + 8, shot_y + 14, shot_x + shot.width + 8, shot_y + shot.height + 14),
        radius=16,
        fill=(23, 31, 59, 50),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(18))
    image = Image.alpha_composite(image.convert("RGBA"), shadow)
    image.alpha_composite(shot, (shot_x, shot_y))
    draw = ImageDraw.Draw(image)
    path = COMPOSITIONS / f"{name}.png"
    image.convert("RGB").save(path, "PNG", optimize=True)
    return path


def make_contact_sheet(captures: list[Path]) -> Path:
    sheet_width = 1920
    margin, gap, columns, thumb_width = 28, 16, 5, 354
    thumb_height = round(thumb_width * 1032 / 1920)
    caption_height = 44
    rows = (len(captures) + columns - 1) // columns
    row_height = thumb_height + caption_height + 16
    sheet = Image.new("RGB", (sheet_width, margin * 2 + 72 + rows * row_height), WARM)
    draw = ImageDraw.Draw(sheet)
    draw.text((margin, 22), "RGAA Studio · planche des captures", font=font(30, "bold"), fill=INK)
    draw.text((margin, 66), "Interface Windows · www.numera11y.fr · captures du 30 octobre 2026", font=font(15), fill="#45536D")

    for index, source in enumerate(captures):
        col, row = index % columns, index // columns
        x = margin + col * (thumb_width + gap)
        y = margin + 84 + row * row_height
        with Image.open(source) as loaded:
            thumb = loaded.convert("RGB").resize((thumb_width, thumb_height), Image.Resampling.LANCZOS)
        sheet.paste(thumb, (x, y))
        label = CAPTURE_LABELS.get(source.name, source.stem)
        draw.text((x, y + thumb_height + 6), label[:46], font=font(12, "semibold"), fill=INK)

    path = KIT / "CONTACT-SHEET.png"
    sheet.save(path, "PNG", optimize=True)
    return path


def encode_webp(ffmpeg: str, source: Path, target: Path, width: int = 1600) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    command = [
        ffmpeg, "-y", "-loglevel", "error", "-i", str(source),
        "-vf", f"scale={width}:-2:flags=lanczos",
        "-frames:v", "1", "-c:v", "libwebp", "-quality", "88", "-compression_level", "6",
        str(target),
    ]
    subprocess.run(command, check=True)


def prune_generated(folder: Path, expected: set[str]) -> None:
    if not folder.exists():
        return
    for path in folder.iterdir():
        if path.is_file() and path.name not in expected:
            path.unlink()


def main() -> None:
    expected_capture_names = set(CAPTURE_LABELS)
    prune_generated(CAPTURES, expected_capture_names)
    captures = sorted(
        (path for path in CAPTURES.glob("*.png") if path.name in expected_capture_names),
        key=lambda path: int(path.name.split("-", 1)[0]),
    )
    if len(captures) < 20:
        raise SystemExit(f"Expected at least 20 application screenshots; found {len(captures)} in {CAPTURES}")
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        raise SystemExit("ffmpeg was not found on PATH; install it before building web derivatives.")

    STORE.mkdir(parents=True, exist_ok=True)
    COMPOSITIONS.mkdir(parents=True, exist_ok=True)
    WEB.mkdir(parents=True, exist_ok=True)

    prune_generated(STORE, expected_capture_names)
    prune_generated(COMPOSITIONS, {f"{brief[0]}.png" for brief in COMPOSITION_BRIEFS})
    prune_generated(WEB / "captures", {f"{Path(name).stem}.webp" for name in expected_capture_names})
    prune_generated(WEB / "compositions", {f"{brief[0]}.webp" for brief in COMPOSITION_BRIEFS})

    store_images = make_store_images(captures)
    compositions = [make_composition(*brief) for brief in COMPOSITION_BRIEFS]
    contact_sheet = make_contact_sheet(captures)

    for source in captures:
        encode_webp(ffmpeg, source, WEB / "captures" / f"{source.stem}.webp")
    for source in compositions:
        encode_webp(ffmpeg, source, WEB / "compositions" / f"{source.stem}.webp")

    records = []
    image_paths = [("capture", path) for path in captures]
    image_paths += [("store", path) for path in store_images]
    image_paths += [("composition", path) for path in compositions]
    image_paths.append(("contact-sheet", contact_sheet))
    image_paths += [("web-capture", path) for path in sorted((WEB / "captures").glob("*.webp"))]
    image_paths += [("web-composition", path) for path in sorted((WEB / "compositions").glob("*.webp"))]

    for category, path in image_paths:
        with Image.open(path) as loaded:
            width, height = loaded.size
        label = CAPTURE_LABELS.get(path.name)
        if category == "composition":
            for brief in COMPOSITION_BRIEFS:
                if path.stem == brief[0]:
                    label = brief[4]
                    break
        if not label:
            label = path.stem.replace("-", " ")
        records.append({
            "path": path.relative_to(KIT).as_posix(),
            "category": category,
            "alt": label,
            "width": width,
            "height": height,
            "bytes": path.stat().st_size,
        })

    manifest = {
        "product": "RGAA Studio",
        "publisher": "numera11y",
        "captureDate": date.today().isoformat(),
        "capturePlatform": "Windows desktop · application window at 1920×1032",
        "websiteShown": "https://www.numera11y.fr",
        "auditData": "isolated synthetic dossier; no previous local dossiers imported",
        "humanConformityDecisions": "none prefilled or recorded",
        "axeResults": "live automated suggestions from the public website at capture time; suggestions only",
        "records": records,
    }
    (KIT / "media-manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    with zipfile.ZipFile(ARCHIVE, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path in sorted(KIT.rglob("*")):
            if path.is_file() and path != ARCHIVE:
                archive.write(path, path.relative_to(ROOT).as_posix())
    print(f"Screenshots: {len(captures)}")
    print(f"Store-ready PNGs (1920x1080): {len(store_images)}")
    print(f"Editorial compositions: {len(compositions)}")
    print(f"WebP derivatives: {len(list(WEB.rglob('*.webp')))}")
    print(f"Contact sheet: {contact_sheet}")
    print(f"Manifest: {KIT / 'media-manifest.json'}")
    print(f"Shareable archive: {ARCHIVE} ({ARCHIVE.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
