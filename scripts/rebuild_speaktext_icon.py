from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE_ICON = ROOT / "src" / "SpeakText.App" / "Assets" / "SpeakText.ico"
OUTPUT_ICON = ROOT / "src" / "SpeakText.App" / "Assets" / "SpeakTextApp.ico"
SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]


def main() -> None:
    OUTPUT_ICON.parent.mkdir(parents=True, exist_ok=True)

    with Image.open(SOURCE_ICON) as image:
        base_image = image.convert("RGBA")
        base_image.save(
            OUTPUT_ICON,
            format="ICO",
            bitmap_format="bmp",
            sizes=SIZES,
        )

    print(f"Rebuilt icon: {OUTPUT_ICON}")


if __name__ == "__main__":
    main()
