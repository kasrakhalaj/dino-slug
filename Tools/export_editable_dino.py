"""Prepare native Aseprite face layers, then package the cleaned layers with the body."""
from pathlib import Path
import math
import struct
import sys

from PIL import Image
from export_dino_aseprite import ART, OUT, SIZE, OFFSET, chunk, string, cel

WORK = OUT / 'EditableDino'
NAMES = ['Skin', 'Mouth and gums', 'Teeth', 'Eye and nostril']


def layer(name, linked=False):
    return chunk(0x2004, struct.pack('<6HB3x', 19 if linked else 3, 0, 0, 0, 0, 0, 255) + string(name))


def linked_cel(index, x, y, frame=0):
    return chunk(0x2005, struct.pack('<HhhBHh5xH', index, x, y, 255, 1, 0, frame))


def save(path, width, height, layers, frames, tags=()):
    prefixes = [layer(name, len(frames) > 1 and i > 0) for i, name in enumerate(layers)]
    if tags:
        data = struct.pack('<H8x', len(tags))
        for name, first, last in tags:
            data += struct.pack('<HHBH6x3BB', first, last, 0, 0, 121, 191, 43, 0) + string(name)
        prefixes.append(chunk(0x2018, data))
    frames[0][1][:0] = prefixes
    data = b''
    for duration, chunks in frames:
        content = b''.join(chunks)
        data += struct.pack('<IHHH2xI', len(content) + 16, 0xF1FA, len(chunks), duration, 0) + content
    header = struct.pack('<I5HIHII B3xHBBhhHH84x', len(data) + 128, 0xA5E0,
                         len(frames), width, height, 32, 1, 83, 0, 0, 0, 0, 1, 1, 0, 0, 1, 1)
    path.write_bytes(header + data)


def prepare():
    WORK.mkdir(parents=True, exist_ok=True)
    head = Image.open(ART / 'dino_head.png').convert('RGBA')
    parts = [Image.new('RGBA', head.size) for _ in NAMES]
    for y in range(head.height):
        for x in range(head.width):
            r, g, b, a = pixel = head.getpixel((x, y))
            if not a:
                continue
            index = 0
            if ((11 <= x <= 13 and 9 <= y <= 11) or (25 <= x <= 28 and 14 <= y <= 17)) and g < 85 and r < 45 and b < 65:
                index = 3
            elif y >= 15 and x >= 10 and ((min(r, g, b) >= 100 and g >= r * .8) or (b > r * 1.3 and b > g * 1.2)):
                index = 2
            elif y >= 14 and x >= 10 and (r > g * 1.2 or (b > g * 1.1 and r > g)):
                index = 1
            parts[index].putpixel((x, y), pixel)
    original = Image.new('RGBA', head.size)
    for part in parts:
        original.alpha_composite(part)
    assert original.tobytes() == head.tobytes(), 'Separating the head must preserve every source pixel'
    save(WORK / 'Head-parts-source.aseprite', 32, 32, NAMES,
         [(100, [cel(i, part, 0, 0) for i, part in enumerate(parts)])])
    colors = ['000000', '02240d', '143c05', '276c39', '329874', '499e4c', '63b92b',
              '79bf2b', '84d52b', '99eb48', 'b0dd54', 'b9df76', 'b5f163',
              '19261c', '211a2b', '3d1e36', '66263b', '8f2432', 'b1262c', 'd3272b',
              'e04c47', 'fff8e6', 'd4d2c9', 'b7b6bb', '879dab', 'a3c7d3', '416477', '334753']
    lines = ['GIMP Palette', 'Name: Dino Slug Head', 'Columns: 7', '#']
    for value in colors:
        rgb = [int(value[i:i+2], 16) for i in (0, 2, 4)]
        lines.append(' '.join(str(v) for v in rgb) + ' #' + value)
    (WORK / 'Dino-Head.gpl').write_text('\n'.join(lines) + '\n', encoding='ascii')


def assemble():
    parts = [Image.open(WORK / f'{name}.png').convert('RGBA') for name in NAMES]
    head = Image.new('RGBA', (32, 32))
    for part in parts:
        assert part.size == (32, 32)
        assert set(part.getchannel('A').get_flattened_data()) <= {0, 255}
        head.alpha_composite(part)
    frames, tags, previews = [], [], []
    for state, count, duration in [('Run', 6, 83), ('Jump', 1, 1000), ('Hit', 2, 125), ('Death', 6, 167)]:
        tags.append((state, len(frames), len(frames) + count - 1))
        for i in range(count):
            body = Image.open(ART / ('dino_jump.png' if state == 'Jump' else f'dino_{state.lower()}_{i}.png')).convert('RGBA')
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
            chunks = [cel(0, body, *OFFSET)]
            preview = Image.new('RGBA', (SIZE, SIZE))
            preview.alpha_composite(body, OFFSET)
            for index, part in enumerate(parts, 1):
                face = part.rotate(angle, Image.Resampling.NEAREST, expand=True)
                x = round(OFFSET[0] + 32 + px * 32 - face.width / 2)
                y = round(OFFSET[1] + 57 - py * 32 - face.height / 2)
                chunks.append(linked_cel(index, x, y) if len(frames) > 0 and angle == 0 else cel(index, face, x, y))
                preview.alpha_composite(face, (x, y))
            frames.append((duration, chunks))
            previews.append(preview)
    save(WORK / 'Dino-Editable-source.aseprite', SIZE, SIZE,
         ['Body'] + ['Head - ' + name for name in NAMES], frames, tags)
    head.resize((384, 384), Image.Resampling.NEAREST).save(WORK / 'Head-preview.png')
    previews[0].crop((18, 16, 78, 80)).resize((360, 384), Image.Resampling.NEAREST).save(WORK / 'Dino-preview.png')
    print('Prepared 15 frames with 5 editable layers; standing head cels share their image.')


if __name__ == '__main__':
    prepare() if sys.argv[1] == 'prepare' else assemble()
