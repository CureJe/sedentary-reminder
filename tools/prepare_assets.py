from collections import deque
from pathlib import Path

from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
CHARACTERS = ROOT / "assets" / "characters"
SOURCE = ROOT / "assets" / "source"


def trim_and_resize(path: Path, max_edge: int) -> None:
    image = Image.open(path).convert("RGBA")
    bounds = image.getchannel("A").getbbox()
    if bounds is None:
        raise RuntimeError(f"No visible pixels in {path}")

    left, top, right, bottom = bounds
    padding = max(12, int(max(right - left, bottom - top) * 0.025))
    left = max(0, left - padding)
    top = max(0, top - padding)
    right = min(image.width, right + padding)
    bottom = min(image.height, bottom + padding)
    image = image.crop((left, top, right, bottom))

    scale = min(1.0, max_edge / float(max(image.size)))
    if scale < 1.0:
        size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
        image = image.resize(size, Image.Resampling.LANCZOS)
    image.save(path, optimize=True)


def remove_connected_flat_background(source: Path, output: Path) -> None:
    image = Image.open(source).convert("RGBA")
    width, height = image.size
    rgb = image.convert("RGB")
    pixels = rgb.load()
    key = pixels[0, 0]

    def is_background(x: int, y: int) -> bool:
        color = pixels[x, y]
        return max(abs(color[i] - key[i]) for i in range(3)) <= 3

    visited = bytearray(width * height)
    queue = deque()

    def seed(x: int, y: int) -> None:
        index = y * width + x
        if not visited[index] and is_background(x, y):
            visited[index] = 1
            queue.append((x, y))

    for x in range(width):
        seed(x, 0)
        seed(x, height - 1)
    for y in range(height):
        seed(0, y)
        seed(width - 1, y)

    while queue:
        x, y = queue.popleft()
        if x > 0:
            seed(x - 1, y)
        if x + 1 < width:
            seed(x + 1, y)
        if y > 0:
            seed(x, y - 1)
        if y + 1 < height:
            seed(x, y + 1)

    background = Image.frombytes("L", (width, height), bytes(255 if value else 0 for value in visited))
    edge_zone = background.filter(ImageFilter.MaxFilter(5))
    background_pixels = background.load()
    edge_pixels = edge_zone.load()
    alpha = Image.new("L", (width, height), 255)
    alpha_pixels = alpha.load()

    for y in range(height):
        for x in range(width):
            if background_pixels[x, y]:
                alpha_pixels[x, y] = 0
            elif edge_pixels[x, y]:
                color = pixels[x, y]
                distance = sum((color[i] - key[i]) ** 2 for i in range(3)) ** 0.5
                alpha_pixels[x, y] = max(0, min(255, round(distance / 42.0 * 255)))

    alpha = alpha.filter(ImageFilter.GaussianBlur(0.45))
    image.putalpha(alpha)
    output.parent.mkdir(parents=True, exist_ok=True)
    image.save(output, optimize=True)


def main() -> None:
    limits = {
        "cat.png": 1100,
        "corgi.png": 1200,
        "red-panda.png": 1100,
        "mech.png": 1100,
        "web-ranger.png": 1100,
    }
    for filename, max_edge in limits.items():
        trim_and_resize(CHARACTERS / filename, max_edge)


if __name__ == "__main__":
    main()
