"""Package the game's separate head/body sprites as an editable Aseprite document."""
from pathlib import Path
import math
import struct
import zlib

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/Game/Art'
OUT = ROOT / 'Tools/SourceArt'
SIZE = 112
OFFSET = (16, 16)


def string(value):
    data = value.encode('utf-8')
    return struct.pack('<H', len(data)) + data


def chunk(kind, data):
    return struct.pack('<IH', len(data) + 6, kind) + data


def layer(name):
    return chunk(0x2004, struct.pack('<6HB3x', 3, 0, 0, 0, 0, 0, 255) + string(name))


def cel(index, image, x, y):
    data = struct.pack('<HhhBHh5xHH', index, x, y, 255, 2, 0, *image.size)
    return chunk(0x2005, data + zlib.compress(image.tobytes()))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    head = Image.open(ART / 'dino_head.png').convert('RGBA')
    states = [('Run', 6, 83), ('Jump', 1, 1000), ('Hit', 2, 125), ('Death', 6, 167)]
    tags = []
    frames = []
    composites = []
    for state, count, duration in states:
        tags.append((state, len(frames), len(frames) + count - 1))
        for i in range(count):
            filename = 'dino_jump.png' if state == 'Jump' else f'dino_{state.lower()}_{i}.png'
            body = Image.open(ART / filename).convert('RGBA')
            px, py, angle = .375, 1.125, 0
            if state == 'Run':
                py += math.sin(i * math.pi * 4 / 6) * .015625
            elif state == 'Hit':
                py += math.sin(i * 2) * .015625
            elif state == 'Death':
                angle = -min(85, i * 17)
                a = math.radians(angle)
                dx, dy = px, py - .140625
                px = math.cos(a) * dx - math.sin(a) * dy - i * .0625
                py = max(.35, .140625 + math.sin(a) * dx + math.cos(a) * dy)
            face = head.rotate(angle, Image.Resampling.NEAREST, expand=True)
            x = round(OFFSET[0] + 32 + px * 32 - face.width / 2)
            y = round(OFFSET[1] + 57 - py * 32 - face.height / 2)
            assert x >= 0 and y >= 0 and x + face.width <= SIZE and y + face.height <= SIZE
            frames.append((duration, [cel(0, body, *OFFSET), cel(1, face, x, y)]))
            composite = Image.new('RGBA', (SIZE, SIZE))
            composite.alpha_composite(body, OFFSET)
            composite.alpha_composite(face, (x, y))
            composites.append(composite)

    tag_data = struct.pack('<H8x', len(tags))
    for name, first, last in tags:
        tag_data += struct.pack('<HHBH6x3BB', first, last, 0, 0, 121, 191, 43, 0) + string(name)
    frames[0][1][:0] = [layer('Body'), layer('Head'), chunk(0x2018, tag_data)]
    frame_bytes = []
    for duration, chunks in frames:
        content = b''.join(chunks)
        frame_bytes.append(struct.pack('<IHHH2xI', len(content) + 16, 0xF1FA, len(chunks), duration, 0) + content)
    content = b''.join(frame_bytes)
    header = struct.pack('<I5HIHII B3xHBBhhHH84x', len(content) + 128, 0xA5E0,
                         len(frames), SIZE, SIZE, 32, 1, 83, 0, 0, 0, 0, 1, 1, 0, 0, 16, 16)
    assert len(header) == 128
    path = OUT / 'DinoSlug-Dino.aseprite'
    path.write_bytes(header + content)
    composites[0].resize((576, 576), Image.Resampling.NEAREST).save(OUT / 'DinoSlug-Dino-preview.png')
    print(f'Created {path}: {len(frames)} frames, Body + Head layers, {len(tags)} animation tags')


if __name__ == '__main__':
    main()
