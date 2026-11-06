#!/usr/bin/env python3
"""Generate PaperPlaneGlow.cur and PaperPlaneGlowClick.ani from the PNG frames."""

from __future__ import annotations

import struct
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CANVAS_SIZE = 48
HOTSPOT_X = 12
HOTSPOT_Y = 8
CLICK_SEQUENCE = (40, 42, 44, 46, 48, 48, 46, 44, 42, 40)

# Keep the final 40 px frame on screen long enough for the helper timer to restore
# the normal cursor without wrapping back into a second pulse.
ANIMATION_SEQUENCE = CLICK_SEQUENCE + (40,) * 6


def frame_canvas(size: int) -> Image.Image:
    source_path = ROOT / f"PaperPlaneGlow-Frame{size}.png"
    source = Image.open(source_path).convert("RGBA")
    if source.size != (size, size):
        raise ValueError(f"{source_path.name}: expected {size}x{size}, got {source.size}")

    source_hot_x = round(size * 12 / 48)
    source_hot_y = round(size * 8 / 48)
    canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
    canvas.alpha_composite(source, (HOTSPOT_X - source_hot_x, HOTSPOT_Y - source_hot_y))
    return canvas


def make_cur(image: Image.Image) -> bytes:
    width, height = image.size
    if width != CANVAS_SIZE or height != CANVAS_SIZE:
        raise ValueError("All cursor frames must use the fixed canvas size")

    bgra = image.tobytes("raw", "BGRA")
    rows = [bgra[row * width * 4 : (row + 1) * width * 4] for row in range(height)]
    xor_bitmap = b"".join(reversed(rows))
    mask_stride = ((width + 31) // 32) * 4
    and_mask = bytes(mask_stride * height)

    bitmap_header = struct.pack(
        "<IiiHHIIiiII",
        40,          # biSize
        width,       # biWidth
        height * 2,  # biHeight includes XOR + AND masks
        1,           # biPlanes
        32,          # biBitCount
        0,           # biCompression
        len(xor_bitmap),
        0,
        0,
        0,
        0,
    )
    image_data = bitmap_header + xor_bitmap + and_mask

    icon_dir = struct.pack("<HHH", 0, 2, 1)
    entry = struct.pack(
        "<BBBBHHII",
        width,
        height,
        0,
        0,
        HOTSPOT_X,
        HOTSPOT_Y,
        len(image_data),
        22,
    )
    return icon_dir + entry + image_data


def riff_chunk(tag: bytes, data: bytes) -> bytes:
    padding = b"\0" if len(data) % 2 else b""
    return tag + struct.pack("<I", len(data)) + data + padding


def build_ani(frames: list[bytes]) -> bytes:
    count = len(frames)
    anih = struct.pack(
        "<IIIIIIIII",
        36,              # cbSize
        count,           # cFrames
        count,           # cSteps
        CANVAS_SIZE,
        CANVAS_SIZE,
        32,
        1,
        1,               # iDispRate: one 1/60-second jiffy per frame
        1,               # AF_ICON
    )
    rates = struct.pack("<" + "I" * count, *([1] * count))
    sequence = struct.pack("<" + "I" * count, *range(count))
    frame_list = b"fram" + b"".join(riff_chunk(b"icon", frame) for frame in frames)
    body = (
        b"ACON"
        + riff_chunk(b"anih", anih)
        + riff_chunk(b"rate", rates)
        + riff_chunk(b"seq ", sequence)
        + riff_chunk(b"LIST", frame_list)
    )
    return b"RIFF" + struct.pack("<I", len(body)) + body


def main() -> None:
    normal = make_cur(frame_canvas(40))
    (ROOT / "PaperPlaneGlow.cur").write_bytes(normal)

    animation_frames = [make_cur(frame_canvas(size)) for size in ANIMATION_SEQUENCE]
    (ROOT / "PaperPlaneGlowClick.ani").write_bytes(build_ani(animation_frames))

    print(f"Generated PaperPlaneGlow.cur ({len(normal)} bytes)")
    print(f"Generated PaperPlaneGlowClick.ani ({len(animation_frames)} frames)")


if __name__ == "__main__":
    main()
