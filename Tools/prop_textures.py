"""
Рисует текстуры реквизита в Assets/Art/Props/Textures.

Запуск из корня проекта:  python Tools/prop_textures.py   (нужен только Pillow)
Текст на языке игрока сюда не попадает — он на предметах в TextMeshPro и переводится через Localization.
"""
import math
import os
import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Props", "Textures")
FONTS = os.path.join(ROOT, "Assets", "UI", "Fonts")


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + ".png"))
    print("  ", name)


def rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


# ---------------------------------------------------------------- шум

def value_noise(size, cells, seed, tile=True):
    """Сглаженный шум: маленькая случайная картинка, растянутая бикубически. Возвращает L."""
    rnd = random.Random(seed)
    w, h = size
    small = Image.new("L", (cells + (1 if not tile else 0), cells))
    small.putdata([rnd.randint(0, 255) for _ in range(small.width * small.height)])
    if tile:
        # Для бесшовности растягиваем картинку 3x3 и вырезаем центр.
        big = Image.new("L", (cells * 3, cells * 3))
        for i in range(3):
            for j in range(3):
                big.paste(small, (i * cells, j * cells))
        big = big.resize((w * 3, h * 3), Image.BICUBIC)
        return big.crop((w, h, 2 * w, 2 * h))
    return small.resize((w, h), Image.BICUBIC)


def fbm(size, seed, octaves=(4, 8, 16, 32, 64), weights=(0.4, 0.25, 0.15, 0.12, 0.08), tile=True):
    acc = Image.new("L", size, 0)
    total = 0.0
    for k, (c, wgt) in enumerate(zip(octaves, weights)):
        layer = value_noise(size, c, seed * 31 + k, tile)
        acc = Image.blend(acc, layer, wgt / (total + wgt))
        total += wgt
    return acc


def tint(gray, dark, light):
    """Раскрасить градации серого между двумя цветами."""
    lut_r = [int(dark[0] + (light[0] - dark[0]) * i / 255) for i in range(256)]
    lut_g = [int(dark[1] + (light[1] - dark[1]) * i / 255) for i in range(256)]
    lut_b = [int(dark[2] + (light[2] - dark[2]) * i / 255) for i in range(256)]
    return Image.merge("RGB", (gray.point(lut_r), gray.point(lut_g), gray.point(lut_b)))


def contrast(gray, lo, hi):
    return gray.point(lambda v: max(0, min(255, int((v - lo) * 255 / max(1, hi - lo)))))


def multiply(img, gray, strength):
    """Затемнить картинку по маске (0 — без изменений)."""
    shade = gray.point(lambda v: int(255 - v * strength))
    return ImageChops.multiply(img, Image.merge("RGB", (shade, shade, shade)))


# ---------------------------------------------------------------- материалы-плитки

def wood(name, dark, light, seed, streaks=520, size=1024, knots=2):
    rnd = random.Random(seed)
    base = fbm((size, size), seed, octaves=(2, 4, 8), weights=(0.5, 0.3, 0.2))
    img = tint(contrast(base, 30, 230), mix(dark, light, 0.3), mix(dark, light, 0.75))
    d = ImageDraw.Draw(img, "RGBA")
    # Волокна: волнистые продольные линии разной яркости.
    for _ in range(streaks):
        x0 = rnd.uniform(-20, size + 20)
        amp = rnd.uniform(2, 14)
        freq = rnd.uniform(0.004, 0.012)
        phase = rnd.uniform(0, 6.28)
        width = rnd.choice([1, 1, 1, 2, 2, 3])
        col = mix(dark, light, rnd.choice([rnd.uniform(-0.3, 0.1), rnd.uniform(0.8, 1.1)]))
        alpha = rnd.randint(70, 170)
        pts = [(x0 + amp * math.sin(y * freq + phase) + 6 * math.sin(y * 0.03 + phase * 2), y) for y in range(-10, size + 12, 8)]
        for dx in (-size, 0, size):
            d.line([(x + dx, y) for x, y in pts], fill=col + (alpha,), width=width)
    # Сучки.
    for _ in range(knots):
        cx, cy = rnd.uniform(0, size), rnd.uniform(0, size)
        for r in range(28, 2, -3):
            col = mix(dark, light, 0.15 + 0.5 * (r % 6) / 6)
            d.ellipse((cx - r * 0.6, cy - r * 1.6, cx + r * 0.6, cy + r * 1.6), outline=col + (150,), width=2)
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    grain = value_noise((size, size), 256, seed + 5)
    img = multiply(img, contrast(grain, 100, 255), 0.12)
    save(img, name)


def metal_wear():
    size = 1024
    rnd = random.Random(7)
    base = fbm((size, size), 7, octaves=(4, 8, 32), weights=(0.5, 0.3, 0.2))
    img = tint(contrast(base, 40, 220), (205, 205, 205), (255, 255, 255))
    d = ImageDraw.Draw(img, "RGBA")
    for _ in range(900):
        x, y = rnd.uniform(0, size), rnd.uniform(0, size)
        a = rnd.uniform(0, 6.28)
        l = rnd.uniform(4, 60)
        v = rnd.choice([150, 170, 255])
        d.line((x, y, x + math.cos(a) * l, y + math.sin(a) * l), fill=(v, v, v, rnd.randint(40, 110)), width=1)
    img = img.filter(ImageFilter.GaussianBlur(0.4))
    save(img, "metal_wear")


def leather(name, dark, light, seed):
    size = 512
    rnd = random.Random(seed)
    img = tint(contrast(fbm((size, size), seed, octaves=(4, 16), weights=(0.6, 0.4)), 40, 220), dark, light)
    d = ImageDraw.Draw(img, "RGBA")
    for _ in range(9000):
        x, y = rnd.uniform(0, size), rnd.uniform(0, size)
        r = rnd.uniform(1.5, 4)
        col = mix(dark, light, rnd.uniform(0.0, 1.0))
        d.ellipse((x - r, y - r * 0.8, x + r, y + r * 0.8), fill=col + (70,))
    for _ in range(40):
        x, y = rnd.uniform(0, size), rnd.uniform(0, size)
        pts = [(x + i * 6, y + rnd.uniform(-2, 2)) for i in range(rnd.randint(4, 14))]
        d.line(pts, fill=dark + (90,), width=1)
    save(img.filter(ImageFilter.GaussianBlur(0.6)), name)


def paper(name, base, seed, aged=False):
    size = 1024
    rnd = random.Random(seed)
    img = tint(contrast(fbm((size, size), seed, octaves=(8, 32, 128), weights=(0.4, 0.3, 0.3)), 60, 200),
               mix(base, (150, 130, 100), 0.12), base)
    if aged:
        stains = contrast(fbm((size, size), seed + 1, octaves=(3, 6), weights=(0.6, 0.4)), 150, 230)
        img = multiply(img, stains, 0.18)
        d = ImageDraw.Draw(img, "RGBA")
        for _ in range(60):
            x, y, r = rnd.uniform(0, size), rnd.uniform(0, size), rnd.uniform(1, 5)
            d.ellipse((x - r, y - r, x + r, y + r), fill=(140, 100, 60, rnd.randint(30, 80)))
    save(img, name)


def granite():
    size = 1024
    rnd = random.Random(11)
    img = tint(contrast(fbm((size, size), 11), 50, 210), (95, 95, 98), (150, 150, 152))
    d = ImageDraw.Draw(img, "RGBA")
    for _ in range(40000):
        x, y, r = rnd.uniform(0, size), rnd.uniform(0, size), rnd.uniform(0.5, 2.2)
        v = rnd.choice([(40, 40, 42), (200, 200, 200), (120, 110, 105), (70, 70, 75)])
        d.ellipse((x - r, y - r, x + r, y + r), fill=v + (rnd.randint(80, 200),))
    stains = contrast(fbm((size, size), 12, octaves=(2, 4, 8), weights=(0.5, 0.3, 0.2)), 140, 230)
    img = multiply(img, stains, 0.25)
    save(img.filter(ImageFilter.GaussianBlur(0.5)), "granite")


def speaker_mesh():
    size = 256
    img = Image.new("RGB", (size, size), (40, 40, 42))
    d = ImageDraw.Draw(img)
    step = 8
    for y in range(0, size, step):
        for x in range(0, size, step):
            d.ellipse((x + 2, y + 2, x + 6, y + 6), fill=(8, 8, 8))
    save(img, "speaker_mesh")


# ---------------------------------------------------------------- сейф

GOLD = (212, 170, 86)
GOLD_DARK = (150, 110, 50)
ENAMEL = (30, 58, 44)


def enamel_base(size, seed):
    g = contrast(fbm(size, seed, octaves=(4, 16, 64), weights=(0.5, 0.3, 0.2)), 60, 200)
    return tint(g, mix(ENAMEL, (0, 0, 0), 0.35), mix(ENAMEL, (80, 110, 90), 0.15))


def safe_enamel():
    img = enamel_base((512, 512), 21)
    d = ImageDraw.Draw(img, "RGBA")
    rnd = random.Random(21)
    for _ in range(120):
        x, y = rnd.uniform(0, 512), rnd.uniform(0, 512)
        a = rnd.uniform(0, 6.28)
        l = rnd.uniform(3, 25)
        d.line((x, y, x + math.cos(a) * l, y + math.sin(a) * l), fill=(160, 170, 160, 50), width=1)
    save(img, "safe_enamel")


def scroll(d, cx, cy, r, start, sweep, width, color, turns=1.6):
    """Спиральный завиток (орнамент)."""
    pts = []
    for i in range(60):
        t = i / 59
        a = math.radians(start + sweep * t * turns)
        rr = r * (1 - 0.75 * t)
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    d.line(pts, fill=color, width=width, joint="curve")


def safe_door():
    w, h = 800, 1040
    img = enamel_base((w, h), 22)
    d = ImageDraw.Draw(img, "RGBA")
    # Двойная золотая рамка с закруглёнными углами.
    d.rounded_rectangle((34, 34, w - 34, h - 34), 26, outline=GOLD, width=6)
    d.rounded_rectangle((52, 52, w - 52, h - 52), 18, outline=GOLD_DARK, width=2)
    # Угловые завитки.
    for (cx, cy, sx, sy) in ((70, 70, 1, 1), (w - 70, 70, -1, 1), (70, h - 70, 1, -1), (w - 70, h - 70, -1, -1)):
        for k in range(3):
            scroll(d, cx + sx * (30 + k * 26), cy + sy * 22, 22 - k * 5, 90 if sy > 0 else -90, 300 * sx, 3, GOLD)
        d.ellipse((cx - 7, cy - 7, cx + 7, cy + 7), fill=GOLD)
    # Овальная картина: лунный пейзаж с прудом (под ней — место для кодового замка и ручки).
    ox0, oy0, ox1, oy1 = 210, 300, w - 210, 590
    ow, oh = ox1 - ox0, oy1 - oy0
    scene = Image.new("RGB", (ow, oh))
    sd = ImageDraw.Draw(scene)
    for y in range(oh):
        sd.line((0, y, ow, y), fill=mix((24, 34, 58), (120, 96, 70), (y / oh) ** 1.6))
    mx, my = ow * 0.66, oh * 0.28
    sd.ellipse((mx - 26, my - 26, mx + 26, my + 26), fill=(236, 226, 190))
    sd.polygon([(0, oh * 0.72), (ow * 0.2, oh * 0.55), (ow * 0.38, oh * 0.66), (ow * 0.55, oh * 0.5),
                (ow * 0.75, oh * 0.64), (ow, oh * 0.52), (ow, oh), (0, oh)], fill=(28, 36, 30))
    sd.rectangle((0, oh * 0.8, ow, oh), fill=(38, 52, 70))
    sd.line((mx - 36, oh * 0.86, mx + 36, oh * 0.86), fill=(200, 190, 150), width=3)
    sd.line((mx - 22, oh * 0.92, mx + 22, oh * 0.92), fill=(170, 160, 130), width=2)
    for tx in (ow * 0.14, ow * 0.26, ow * 0.86):
        sd.polygon([(tx, oh * 0.38), (tx - 22, oh * 0.74), (tx + 22, oh * 0.74)], fill=(16, 24, 18))
    mask = Image.new("L", (ow, oh), 0)
    ImageDraw.Draw(mask).ellipse((0, 0, ow - 1, oh - 1), fill=255)
    img.paste(scene.filter(ImageFilter.GaussianBlur(1.0)), (ox0, oy0), mask)
    d.ellipse((ox0 - 8, oy0 - 8, ox1 + 8, oy1 + 8), outline=GOLD, width=7)
    d.ellipse((ox0 - 20, oy0 - 20, ox1 + 20, oy1 + 20), outline=GOLD_DARK, width=2)
    # Тонкая рамка под замок.
    d.rounded_rectangle((170, 640, w - 170, 760), 14, outline=GOLD_DARK, width=2)
    # Имя мастерской (часть обстановки, как вывески на улице).
    f = font("PTSerif-Bold.ttf", 44)
    title = "WARD & SONS"
    tw = d.textlength(title, font=f)
    d.text(((w - tw) / 2, 110), title, font=f, fill=GOLD)
    f2 = font("PTSerif-Bold.ttf", 26)
    sub = "HOLLOW CREEK · 1891"
    tw = d.textlength(sub, font=f2)
    d.text(((w - tw) / 2, 172), sub, font=f2, fill=GOLD_DARK)
    d.line((w / 2 - 160, 222, w / 2 + 160, 222), fill=GOLD, width=3)
    # Потёртости.
    wear = contrast(fbm((w, h), 23, octaves=(8, 32), weights=(0.6, 0.4), tile=False), 170, 240)
    img = multiply(img, wear, 0.35)
    save(img, "safe_door")


def safe_wheels():
    """Полоса цифр 0–9 для колёс кодового замка: цифры стоят поперёк полосы (колесо крутится)."""
    w, h = 1200, 160
    img = Image.new("RGB", (w, h), (232, 222, 196))
    d = ImageDraw.Draw(img)
    f = font("PTSans-Bold.ttf", 96)
    cell = w / 10
    for i in range(10):
        glyph = Image.new("L", (int(cell), h), 0)
        gd = ImageDraw.Draw(glyph)
        s = str(i)
        tw = gd.textlength(s, font=f)
        gd.text(((cell - tw) / 2, 18), s, font=f, fill=255)
        glyph = glyph.rotate(90, expand=False)
        img.paste((30, 26, 22), (int(i * cell), 0), glyph)
        d.line((int(i * cell), 0, int(i * cell), h), fill=(170, 160, 140), width=3)
    save(img, "safe_wheels")


# ---------------------------------------------------------------- часы

def watch_dial():
    s = 512
    c = s / 2
    img = Image.new("RGB", (s, s), (234, 226, 204))
    g = contrast(fbm((s, s), 31, octaves=(8, 32), weights=(0.6, 0.4), tile=False), 80, 220)
    img = tint(g, (218, 206, 178), (244, 238, 222))
    d = ImageDraw.Draw(img)
    d.ellipse((6, 6, s - 6, s - 6), outline=(90, 80, 60), width=4)
    for i in range(60):
        a = math.radians(i * 6 - 90)
        r0 = c - 22 if i % 5 else c - 40
        r1 = c - 12
        wdt = 2 if i % 5 else 6
        d.line((c + math.cos(a) * r0, c + math.sin(a) * r0, c + math.cos(a) * r1, c + math.sin(a) * r1), fill=(40, 34, 28), width=wdt)
    f = font("PTSerif-Bold.ttf", 64)
    for n, ang in ((12, -90), (3, 0), (6, 90), (9, 180)):
        a = math.radians(ang)
        r = c - 88
        text = str(n)
        tw = d.textlength(text, font=f)
        d.text((c + math.cos(a) * r - tw / 2, c + math.sin(a) * r - 40), text, font=f, fill=(30, 26, 22))
    f2 = font("PTSerif-Bold.ttf", 22)
    brand = "CREEKSIDE"
    tw = d.textlength(brand, font=f2)
    d.text((c - tw / 2, c - 110), brand, font=f2, fill=(90, 70, 40))
    # Малый секундный циферблат.
    d.ellipse((c - 46, c + 58, c + 46, c + 150), outline=(80, 70, 55), width=2)
    for i in range(12):
        a = math.radians(i * 30 - 90)
        d.line((c + math.cos(a) * 38, c + 104 + math.sin(a) * 38, c + math.cos(a) * 44, c + 104 + math.sin(a) * 44), fill=(60, 52, 40), width=2)
    save(img, "watch_dial")


def watch_crack():
    s = 512
    rnd = random.Random(33)
    img = Image.new("RGBA", (s, s), (255, 255, 255, 18))
    d = ImageDraw.Draw(img)
    ix, iy = s * 0.62, s * 0.36
    for k in range(11):
        a = rnd.uniform(0, 6.28)
        x, y = ix, iy
        pts = [(x, y)]
        length = rnd.uniform(120, 330)
        steps = 12
        for _ in range(steps):
            a += rnd.uniform(-0.35, 0.35)
            x += math.cos(a) * length / steps
            y += math.sin(a) * length / steps
            pts.append((x, y))
        d.line(pts, fill=(255, 255, 255, 210), width=2)
        # Ответвления.
        for p in pts[3::4]:
            b = a + rnd.choice([-1, 1]) * rnd.uniform(0.6, 1.2)
            q = (p[0] + math.cos(b) * rnd.uniform(15, 50), p[1] + math.sin(b) * rnd.uniform(15, 50))
            d.line((p, q), fill=(255, 255, 255, 150), width=1)
    for r in (26, 48, 80):
        pts = []
        for i in range(24):
            a = i / 23 * 6.28
            rr = r + rnd.uniform(-6, 6)
            pts.append((ix + math.cos(a) * rr, iy + math.sin(a) * rr))
        d.line(pts, fill=(255, 255, 255, 120), width=1)
    d.ellipse((ix - 10, iy - 10, ix + 10, iy + 10), fill=(255, 255, 255, 160))
    circle = Image.new("L", (s, s), 0)
    ImageDraw.Draw(circle).ellipse((0, 0, s, s), fill=255)
    img.putalpha(ImageChops.multiply(img.getchannel("A"), circle))
    save(img, "watch_crack")


# ---------------------------------------------------------------- бумаги

def aged_sheet(w, h, seed, base=(236, 226, 200), stain=0.22):
    g = contrast(fbm((w, h), seed, octaves=(4, 16, 64), weights=(0.4, 0.3, 0.3), tile=False), 60, 200)
    img = tint(g, mix(base, (150, 130, 100), 0.10), base)
    st = contrast(fbm((w, h), seed + 1, octaves=(2, 5), weights=(0.6, 0.4), tile=False), 150, 235)
    img = multiply(img, st, stain)
    # Потемневшие края.
    edge = Image.new("L", (w, h), 0)
    ed = ImageDraw.Draw(edge)
    m = min(w, h)
    for i in range(24):
        v = int(90 * (1 - i / 24) ** 2)
        ed.rectangle((i * m / 200, i * m / 200, w - i * m / 200, h - i * m / 200), outline=v, width=int(m / 200) + 1)
    return multiply(img, edge.filter(ImageFilter.GaussianBlur(m / 60)), 1.0)


def poster_art():
    w, h = 900, 1200
    img = aged_sheet(w, h, 41, base=(232, 200, 150), stain=0.3)
    d = ImageDraw.Draw(img, "RGBA")
    ink = (38, 22, 18)
    orange = (206, 96, 30)
    # Рамка.
    d.rectangle((30, 30, w - 30, h - 30), outline=ink, width=8)
    d.rectangle((48, 48, w - 48, h - 48), outline=orange, width=3)
    # Верх: место под заголовок (текст — в TextMeshPro), только декоративные звёзды.
    rnd = random.Random(41)
    for _ in range(26):
        x, y = rnd.uniform(80, w - 80), rnd.uniform(70, 560)
        r = rnd.uniform(3, 7)
        d.polygon([(x, y - r * 2), (x + r * 0.5, y - r * 0.5), (x + r * 2, y), (x + r * 0.5, y + r * 0.5),
                   (x, y + r * 2), (x - r * 0.5, y + r * 0.5), (x - r * 2, y), (x - r * 0.5, y - r * 0.5)], fill=orange + (150,))
    # Низ: полная луна, силуэт холма с мостом и шествие фонарей.
    moon = (w * 0.5, 820)
    d.ellipse((moon[0] - 170, moon[1] - 170, moon[0] + 170, moon[1] + 170), fill=(238, 170, 70))
    d.ellipse((moon[0] - 130, moon[1] - 150, moon[0] + 110, moon[1] + 60), fill=(245, 190, 95))
    hill = [(48, 1000), (200, 930), (330, 960), (470, 900), (620, 950), (760, 910), (w - 48, 960), (w - 48, h - 48), (48, h - 48)]
    d.polygon(hill, fill=ink)
    # Мост.
    d.arc((330, 880, 600, 1060), 180, 360, fill=ink, width=16)
    d.rectangle((330, 962, 600, 972), fill=ink)
    # Фонари вдоль дороги.
    for i in range(9):
        x = 110 + i * 85
        y = 1050 - 30 * math.sin(i / 8 * math.pi)
        d.line((x, y, x, y - 38), fill=ink, width=3)
        d.ellipse((x - 11, y - 60, x + 11, y - 34), fill=(250, 200, 90))
    # Маска над луной.
    mx, my = w * 0.5, 640
    d.ellipse((mx - 120, my - 60, mx + 120, my + 60), fill=ink)
    d.ellipse((mx - 80, my - 22, mx - 30, my + 18), fill=(245, 190, 95))
    d.ellipse((mx + 30, my - 22, mx + 80, my + 18), fill=(245, 190, 95))
    d.polygon([(mx - 120, my), (mx - 190, my - 40), (mx - 160, my + 10)], fill=ink)
    d.polygon([(mx + 120, my), (mx + 190, my - 40), (mx + 160, my + 10)], fill=ink)
    d.line((mx + 120, my - 10, mx + 150, my + 110), fill=orange, width=5)
    # Отсыревшие разводы поверх.
    damp = contrast(fbm((w, h), 42, octaves=(3, 7), weights=(0.6, 0.4), tile=False), 175, 240)
    img = multiply(img, damp, 0.45)
    save(img, "poster_art")


def postcard_back():
    w, h = 1200, 800
    img = aged_sheet(w, h, 51, base=(238, 228, 204), stain=0.18)
    d = ImageDraw.Draw(img, "RGBA")
    brown = (110, 76, 50)
    d.line((w * 0.56, 90, w * 0.56, h - 70), fill=brown, width=3)
    f = font("PTSerif-Bold.ttf", 44)
    t = "POST CARD"
    d.text(((w - d.textlength(t, font=f)) / 2, 22), t, font=f, fill=brown)
    for i in range(4):
        y = 420 + i * 90
        d.line((w * 0.60, y, w - 70, y), fill=brown, width=2)
    # Марка с перфорацией.
    sx0, sy0, sx1, sy1 = w - 250, 70, w - 70, 290
    d.rectangle((sx0, sy0, sx1, sy1), fill=(246, 240, 225))
    for x in range(int(sx0), int(sx1) + 1, 16):
        for y in (sy0, sy1):
            d.ellipse((x - 5, y - 5, x + 5, y + 5), fill=img.getpixel((10, 10)))
    for y in range(int(sy0), int(sy1) + 1, 16):
        for x in (sx0, sx1):
            d.ellipse((x - 5, y - 5, x + 5, y + 5), fill=img.getpixel((10, 10)))
    d.rectangle((sx0 + 16, sy0 + 16, sx1 - 16, sy1 - 16), fill=(52, 88, 120))
    d.ellipse((sx0 + 60, sy0 + 40, sx0 + 120, sy0 + 100), fill=(230, 214, 160))
    d.polygon([(sx0 + 16, sy1 - 16), (sx0 + 70, sy0 + 130), (sx0 + 110, sy0 + 160), (sx1 - 16, sy0 + 120), (sx1 - 16, sy1 - 16)], fill=(30, 50, 40))
    f3 = font("PTSans-Bold.ttf", 30)
    d.text((sx0 + 24, sy1 - 58), "3¢", font=f3, fill=(240, 230, 200))
    # Почтовый штемпель поверх марки.
    px, py = sx0 - 30, 170
    d.ellipse((px - 85, py - 85, px + 85, py + 85), outline=(40, 40, 60, 170), width=5)
    d.ellipse((px - 62, py - 62, px + 62, py + 62), outline=(40, 40, 60, 150), width=3)
    f4 = font("PTSans-Bold.ttf", 22)
    d.text((px - 52, py - 36), "HOLLOW", font=f4, fill=(40, 40, 60, 170))
    d.text((px - 42, py - 8), "CREEK", font=f4, fill=(40, 40, 60, 170))
    d.text((px - 42, py + 22), "OCT 31", font=f4, fill=(40, 40, 60, 170))
    for i in range(5):
        y = py - 50 + i * 25
        pts = [(px + 90 + x, y + 8 * math.sin(x / 18)) for x in range(0, 300, 6)]
        d.line(pts, fill=(40, 40, 60, 150), width=4)
    save(img, "postcard_back")


def postcard_front():
    w, h = 1200, 800
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line((0, y, w, y), fill=mix((40, 60, 96), (226, 150, 90), t ** 1.4))
    d.polygon([(0, 560), (200, 500), (420, 540), (700, 470), (900, 520), (w, 480), (w, h), (0, h)], fill=(40, 52, 40))
    for x, hh in ((150, 120), (260, 90), (820, 140), (940, 100)):
        d.rectangle((x, 560 - hh, x + 90, 560), fill=(60, 44, 40))
        d.polygon([(x - 10, 560 - hh), (x + 45, 500 - hh), (x + 100, 560 - hh)], fill=(90, 40, 36))
        d.rectangle((x + 30, 560 - hh + 30, x + 55, 560 - hh + 60), fill=(240, 200, 110))
    d.polygon([(560, 300), (600, 480), (520, 480)], fill=(70, 60, 60))
    d.arc((380, 560, 760, 760), 180, 360, fill=(120, 110, 100), width=18)
    d.rectangle((0, 660, w, h), fill=(50, 70, 90))
    img = img.filter(ImageFilter.GaussianBlur(1.5))
    border = Image.new("RGB", (w, h), (238, 230, 210))
    border.paste(img.resize((w - 60, h - 60)), (30, 30))
    save(border, "postcard_front")


def safe_note():
    w, h = 700, 520
    img = aged_sheet(w, h, 61, base=(246, 242, 226), stain=0.08)
    d = ImageDraw.Draw(img, "RGBA")
    for y in range(110, h, 52):
        d.line((0, y, w, y), fill=(120, 150, 200, 150), width=2)
    d.line((90, 0, 90, h), fill=(200, 80, 80, 170), width=2)
    # Перфорация сверху (лист вырван из блокнота).
    for x in range(10, w, 22):
        d.ellipse((x - 5, 18, x + 5, 28), fill=(180, 170, 150, 200))
    save(img, "safe_note")


def receipt():
    w, h = 720, 1000
    img = aged_sheet(w, h, 71, base=(242, 226, 226), stain=0.12)
    d = ImageDraw.Draw(img, "RGBA")
    ink = (120, 60, 90, 200)
    d.rectangle((40, 40, w - 40, 160), outline=ink, width=3)
    for i, y in enumerate(range(250, 900, 70)):
        d.line((60, y, w - 60, y), fill=(120, 60, 90, 110), width=2)
        if i % 3 == 0:
            d.line((w * 0.62, y - 60, w * 0.62, y), fill=(120, 60, 90, 90), width=2)
    f = font("PTSans-Bold.ttf", 34)
    d.text((w - 190, 60), "№ 0417", font=f, fill=ink)
    # Круглая печать.
    cx, cy = w * 0.72, 780
    d.ellipse((cx - 110, cy - 110, cx + 110, cy + 110), outline=(80, 60, 160, 150), width=6)
    d.ellipse((cx - 80, cy - 80, cx + 80, cy + 80), outline=(80, 60, 160, 120), width=3)
    d.line((cx - 60, cy, cx + 60, cy), fill=(80, 60, 160, 120), width=5)
    save(img, "receipt")


def outage_notice():
    w, h = 720, 960
    img = aged_sheet(w, h, 81, base=(246, 244, 236), stain=0.06)
    d = ImageDraw.Draw(img, "RGBA")
    d.rectangle((0, 0, w, 170), fill=(34, 34, 40))
    # Значок молнии.
    d.ellipse((34, 26, 150, 142), fill=(246, 196, 48))
    d.polygon([(102, 38), (62, 92), (90, 92), (74, 134), (122, 72), (94, 72), (112, 38)], fill=(34, 34, 40))
    d.rectangle((40, h - 90, w - 40, h - 86), fill=(34, 34, 40, 120))
    # Сова — логотип закусочной.
    ox, oy = w - 100, h - 50
    d.ellipse((ox - 30, oy - 34, ox + 30, oy + 30), outline=(34, 34, 40, 200), width=4)
    d.ellipse((ox - 20, oy - 18, ox - 4, oy - 2), outline=(34, 34, 40, 200), width=3)
    d.ellipse((ox + 4, oy - 18, ox + 20, oy - 2), outline=(34, 34, 40, 200), width=3)
    save(img, "outage_notice")


def typed_lines(d, x0, y0, x1, y1, rnd, line=34, color=(50, 50, 60)):
    y = y0
    while y < y1:
        x = x0
        indent = rnd.random() < 0.15
        if indent:
            x += 50
        end = x1 - (rnd.uniform(40, 300) if rnd.random() < 0.2 else 0)
        while x < end:
            wl = rnd.uniform(18, 90)
            if x + wl > end:
                break
            d.rectangle((x, y, x + wl, y + 9), fill=color + (rnd.randint(150, 220),))
            x += wl + rnd.uniform(9, 14)
        y += line
        if rnd.random() < 0.08:
            y += line


def papers_typed():
    w, h = 1024, 1400
    rnd = random.Random(91)
    img = aged_sheet(w, h, 91, base=(244, 240, 228), stain=0.08)
    d = ImageDraw.Draw(img, "RGBA")
    d.rectangle((90, 110, 520, 130), fill=(40, 40, 50, 220))
    typed_lines(d, 90, 200, w - 90, h - 250, rnd)
    pts = [(620 + i * 9, h - 170 + 22 * math.sin(i * 0.7) - i * 0.6) for i in range(36)]
    d.line(pts, fill=(30, 40, 120, 200), width=4)
    save(img, "papers_typed")


def papers_letterhead():
    w, h = 1024, 1400
    rnd = random.Random(92)
    img = aged_sheet(w, h, 92, base=(240, 236, 224), stain=0.1)
    d = ImageDraw.Draw(img, "RGBA")
    # Герб города.
    cx, cy = w / 2, 150
    d.ellipse((cx - 70, cy - 70, cx + 70, cy + 70), outline=(60, 50, 90), width=5)
    d.polygon([(cx - 36, cy - 40), (cx + 36, cy - 40), (cx + 36, cy + 10), (cx, cy + 46), (cx - 36, cy + 10)], outline=(60, 50, 90), width=4)
    d.line((cx - 36, cy - 5, cx + 36, cy - 5), fill=(60, 50, 90), width=3)
    d.line((90, 250, w - 90, 250), fill=(60, 50, 90), width=3)
    typed_lines(d, 90, 320, w - 90, h - 200, rnd)
    save(img, "papers_letterhead")


def page_edges():
    w, h = 512, 128
    rnd = random.Random(95)
    img = Image.new("RGB", (w, h), (236, 228, 208))
    d = ImageDraw.Draw(img)
    for y in range(0, h, 2):
        v = rnd.randint(200, 235)
        d.line((0, y, w, y), fill=(v, v - 8, v - 26))
    save(img, "page_edges")


def diary_pages():
    w, h = 800, 1000
    img = aged_sheet(w, h, 97, base=(246, 240, 222), stain=0.05)
    d = ImageDraw.Draw(img, "RGBA")
    for y in range(150, h - 40, 46):
        d.line((50, y, w - 50, y), fill=(150, 160, 190, 140), width=2)
    save(img, "diary_pages")


# ---------------------------------------------------------------- картинки

def sepia(img):
    g = img.convert("L")
    return tint(g, (46, 32, 22), (236, 214, 176))


def photo_cell(kind, w, h, seed):
    rnd = random.Random(seed)
    img = Image.new("RGB", (w, h), (170, 170, 170))
    d = ImageDraw.Draw(img)
    for y in range(h):
        d.line((0, y, w, y), fill=mix((210, 210, 210), (120, 120, 120), y / h))
    ground = int(h * 0.7)
    d.rectangle((0, ground, w, h), fill=(90, 90, 90))
    if kind == "school":
        d.rectangle((w * 0.2, h * 0.35, w * 0.8, ground), fill=(60, 60, 60))
        d.polygon([(w * 0.15, h * 0.35), (w * 0.5, h * 0.12), (w * 0.85, h * 0.35)], fill=(40, 40, 40))
        d.rectangle((w * 0.45, h * 0.05, w * 0.55, h * 0.2), fill=(50, 50, 50))
        for i in range(4):
            d.rectangle((w * (0.26 + i * 0.14), h * 0.42, w * (0.32 + i * 0.14), h * 0.52), fill=(200, 200, 200))
        d.rectangle((w * 0.46, h * 0.55, w * 0.54, ground), fill=(30, 30, 30))
    elif kind == "bridge":
        d.rectangle((0, h * 0.62, w, h), fill=(130, 130, 130))
        d.arc((w * 0.1, h * 0.35, w * 0.9, h * 1.0), 180, 360, fill=(40, 40, 40), width=int(h * 0.07))
        d.rectangle((w * 0.05, h * 0.5, w * 0.95, h * 0.55), fill=(40, 40, 40))
        for i in range(8):
            x = w * (0.1 + i * 0.11)
            d.line((x, h * 0.42, x, h * 0.5), fill=(40, 40, 40), width=3)
        d.line((w * 0.08, h * 0.42, w * 0.92, h * 0.42), fill=(40, 40, 40), width=3)
    elif kind == "cemetery":
        for i in range(5):
            x = w * (0.1 + i * 0.19)
            hh = rnd.uniform(0.18, 0.3) * h
            if i % 2:
                d.rectangle((x - 5, ground - hh, x + 5, ground), fill=(50, 50, 50))
                d.rectangle((x - 18, ground - hh * 0.75, x + 18, ground - hh * 0.62), fill=(50, 50, 50))
            else:
                d.rounded_rectangle((x - 20, ground - hh, x + 20, ground), 18, fill=(60, 60, 60))
        d.line((w * 0.8, ground, w * 0.86, h * 0.1), fill=(30, 30, 30), width=8)
        for k in range(6):
            a = -1.2 + k * 0.45
            d.line((w * 0.84, h * 0.3, w * 0.84 + math.cos(a) * 50, h * 0.3 + math.sin(a) * 40), fill=(30, 30, 30), width=3)
        d.ellipse((w * 0.47, ground - 12, w * 0.53, ground), fill=(240, 240, 240))
    elif kind == "house":
        d.rectangle((w * 0.25, h * 0.38, w * 0.75, ground), fill=(70, 70, 70))
        d.polygon([(w * 0.2, h * 0.38), (w * 0.5, h * 0.15), (w * 0.8, h * 0.38)], fill=(45, 45, 45))
        d.rectangle((w * 0.44, h * 0.5, w * 0.56, ground), fill=(35, 35, 35))
        d.rectangle((w * 0.3, h * 0.45, w * 0.4, h * 0.55), fill=(210, 210, 210))
        d.rectangle((w * 0.6, h * 0.45, w * 0.7, h * 0.55), fill=(210, 210, 210))
        d.rectangle((w * 0.1, ground - 30, w * 0.9, ground - 26), fill=(40, 40, 40))
    elif kind == "portrait":
        d.rectangle((0, 0, w, h), fill=(150, 150, 150))
        # Женщина и девочка.
        for (cx, top, s) in ((w * 0.38, h * 0.18, 1.0), (w * 0.66, h * 0.42, 0.7)):
            d.ellipse((cx - 30 * s, top, cx + 30 * s, top + 70 * s), fill=(215, 215, 215))
            d.ellipse((cx - 38 * s, top - 8 * s, cx + 38 * s, top + 36 * s), fill=(50, 50, 50))
            d.polygon([(cx - 70 * s, h), (cx - 50 * s, top + 90 * s), (cx + 50 * s, top + 90 * s), (cx + 70 * s, h)], fill=(60, 60, 60))
    img = img.filter(ImageFilter.GaussianBlur(1.1))
    grain = value_noise((w, h), 64, seed, tile=False)
    img = multiply(img, contrast(grain, 80, 255), 0.18)
    img = sepia(img)
    vign = Image.new("L", (w, h), 0)
    vd = ImageDraw.Draw(vign)
    for i in range(30):
        vd.rectangle((i * 3, i * 3, w - i * 3, h - i * 3), outline=int(160 * (1 - i / 30)), width=3)
    img = multiply(img, vign.filter(ImageFilter.GaussianBlur(12)), 1.0)
    framed = Image.new("RGB", (w + 24, h + 24), (232, 222, 200))
    framed.paste(img, (12, 12))
    return framed


def photo_tiles():
    kinds = ["school", "bridge", "cemetery", "house", "portrait"]
    cw, ch = 256, 256
    sheet = Image.new("RGB", (len(kinds) * cw, ch), (232, 222, 200))
    for i, k in enumerate(kinds):
        cell = photo_cell(k, cw - 24, ch - 24, 100 + i)
        sheet.paste(cell, (i * cw, 0))
    save(sheet, "photo_tiles")


def portrait():
    w, h = 768, 1024
    rnd = random.Random(111)
    img = Image.new("RGB", (w, h))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line((0, y, w, y), fill=mix((58, 50, 36), (22, 22, 18), t))
    glow = Image.new("L", (w, h), 0)
    ImageDraw.Draw(glow).ellipse((w * 0.1, h * 0.05, w * 0.9, h * 0.7), fill=120)
    img = Image.composite(Image.new("RGB", (w, h), (96, 84, 58)), img, glow.filter(ImageFilter.GaussianBlur(120)))
    d = ImageDraw.Draw(img)
    cx = w * 0.5
    # Плечи и платье.
    d.polygon([(cx - 330, h), (cx - 250, 720), (cx - 110, 640), (cx + 110, 640), (cx + 250, 720), (cx + 330, h)], fill=(28, 34, 44))
    d.polygon([(cx - 120, 648), (cx, 760), (cx + 120, 648)], fill=(222, 214, 196))   # кружевной воротник
    d.polygon([(cx - 60, 650), (cx, 720), (cx + 60, 650)], fill=(210, 176, 150))     # шея в вырезе
    d.ellipse((cx - 16, 730, cx + 16, 762), fill=(170, 140, 70))                     # брошь
    # Шея.
    d.rectangle((cx - 52, 520, cx + 52, 660), fill=(206, 170, 144))
    # Волосы сзади.
    d.ellipse((cx - 170, 170, cx + 170, 560), fill=(46, 32, 24))
    # Лицо.
    d.ellipse((cx - 118, 250, cx + 118, 580), fill=(222, 186, 158))
    shade = Image.new("L", (w, h), 0)
    ImageDraw.Draw(shade).ellipse((cx + 10, 260, cx + 150, 590), fill=90)
    img = multiply(img, shade.filter(ImageFilter.GaussianBlur(40)), 0.6)
    d = ImageDraw.Draw(img)
    # Причёска: пучок и пробор.
    d.ellipse((cx - 150, 160, cx + 150, 330), fill=(52, 36, 26))
    d.ellipse((cx - 70, 110, cx + 70, 210), fill=(46, 32, 24))
    d.polygon([(cx - 130, 290), (cx - 118, 420), (cx - 150, 330)], fill=(52, 36, 26))
    d.polygon([(cx + 130, 290), (cx + 118, 420), (cx + 150, 330)], fill=(52, 36, 26))
    # Черты лица.
    for side in (-1, 1):
        ex = cx + side * 48
        d.ellipse((ex - 24, 390, ex + 24, 410), fill=(240, 232, 222))
        d.ellipse((ex - 10, 390, ex + 10, 410), fill=(60, 70, 60))
        d.ellipse((ex - 4, 396, ex + 4, 404), fill=(10, 10, 10))
        d.arc((ex - 30, 360, ex + 30, 392), 200, 340, fill=(70, 50, 40), width=6)
    d.line((cx, 410, cx - 8, 480), fill=(180, 138, 112), width=4)
    d.arc((cx - 22, 462, cx + 14, 492), 0, 180, fill=(170, 128, 104), width=3)
    d.ellipse((cx - 36, 512, cx + 36, 530), fill=(150, 80, 76))
    d.line((cx - 36, 521, cx + 36, 521), fill=(110, 56, 52), width=2)
    img = img.filter(ImageFilter.GaussianBlur(2.2))
    # Мазки и кракелюр.
    dd = ImageDraw.Draw(img, "RGBA")
    for _ in range(3000):
        x, y = rnd.uniform(0, w), rnd.uniform(0, h)
        a = rnd.uniform(-0.5, 0.5)
        l = rnd.uniform(6, 20)
        c = img.getpixel((int(x) % w, int(y) % h))
        c = tuple(max(0, min(255, v + rnd.randint(-14, 14))) for v in c)
        dd.line((x, y, x + math.cos(a) * l, y + math.sin(a) * l), fill=c + (120,), width=3)
    for _ in range(420):
        x, y = rnd.uniform(0, w), rnd.uniform(0, h)
        pts = [(x, y)]
        for _ in range(4):
            x += rnd.uniform(-14, 14)
            y += rnd.uniform(-14, 14)
            pts.append((x, y))
        dd.line(pts, fill=(20, 16, 10, 60), width=1)
    varnish = Image.new("RGB", (w, h), (255, 226, 160))
    img = ImageChops.multiply(img, varnish)
    save(img, "portrait")


def mirror_fog():
    s = 1024
    rnd = random.Random(121)
    base = contrast(fbm((s, s), 121, octaves=(4, 16, 64), weights=(0.4, 0.3, 0.3), tile=False), 40, 220)
    alpha = base.point(lambda v: int(60 + v * 0.45))
    img = Image.new("RGBA", (s, s), (235, 238, 240, 0))
    img.putalpha(alpha)
    d = ImageDraw.Draw(img)
    for _ in range(2500):
        x, y, r = rnd.uniform(0, s), rnd.uniform(0, s), rnd.uniform(1, 4)
        d.ellipse((x - r, y - r, x + r, y + r), fill=(250, 252, 255, rnd.randint(120, 220)))
    # Стекающие капли — прозрачные дорожки.
    for _ in range(26):
        x = rnd.uniform(40, s - 40)
        y = rnd.uniform(0, s * 0.6)
        length = rnd.uniform(80, 360)
        pts = [(x + 3 * math.sin(i * 0.2), y + i * length / 40) for i in range(40)]
        d.line(pts, fill=(235, 238, 240, 20), width=rnd.randint(4, 8))
        d.ellipse((pts[-1][0] - 6, pts[-1][1] - 6, pts[-1][0] + 6, pts[-1][1] + 8), fill=(255, 255, 255, 200))
    save(img, "mirror_fog")


def dust_ring():
    """Пыль на полке (1 м × 0,3 м): чистый круг под стоящим подсвечником (u=0.2) и там, где стоял второй (u=0.8)."""
    w, h = 1024, 308
    base = contrast(fbm((w, h), 131, octaves=(8, 32, 128), weights=(0.4, 0.3, 0.3), tile=False), 40, 230)
    alpha = base.point(lambda v: int(90 + v * 0.5))
    clean = Image.new("L", (w, h), 255)
    cd = ImageDraw.Draw(clean)
    r = 64
    cd.ellipse((w * 0.8 - r, h * 0.5 - r, w * 0.8 + r, h * 0.5 + r), fill=0)
    cd.ellipse((w * 0.2 - r, h * 0.5 - r, w * 0.2 + r, h * 0.5 + r), fill=40)
    clean = clean.filter(ImageFilter.GaussianBlur(3))
    rim = Image.new("L", (w, h), 0)
    ImageDraw.Draw(rim).ellipse((w * 0.8 - r - 6, h * 0.5 - r - 6, w * 0.8 + r + 6, h * 0.5 + r + 6), outline=120, width=6)
    alpha = ImageChops.add(ImageChops.multiply(alpha, clean), rim.filter(ImageFilter.GaussianBlur(2)))
    fade = Image.new("L", (w, h), 0)
    ImageDraw.Draw(fade).rounded_rectangle((12, 12, w - 12, h - 12), 30, fill=255)
    alpha = ImageChops.multiply(alpha, fade.filter(ImageFilter.GaussianBlur(14)))
    img = Image.new("RGBA", (w, h), (170, 160, 146, 0))
    img.putalpha(alpha)
    save(img, "dust_ring")


def register_keys():
    w, h = 1024, 768
    img = Image.new("RGB", (w, h), (60, 58, 56))
    d = ImageDraw.Draw(img)
    f = font("PTSans-Bold.ttf", 44)
    fs = font("PTSans-Bold.ttf", 26)
    cols, rows = 6, 5
    labels = [["7", "8", "9", "CL", "DEPT1", "VOID"],
              ["4", "5", "6", "X", "DEPT2", "PO"],
              ["1", "2", "3", "#", "DEPT3", "RA"],
              ["0", "00", ".", "-%", "DEPT4", "ST"],
              ["", "", "", "", "CHK", "CASH"]]
    kw, kh = w / cols, h / rows
    for r in range(rows):
        for c in range(cols):
            x0, y0 = c * kw + 8, r * kh + 8
            col = (226, 220, 206)
            if c >= 4:
                col = (190, 70, 60) if labels[r][c] in ("CASH", "VOID") else (86, 120, 150)
            if c == 3:
                col = (230, 180, 70)
            d.rounded_rectangle((x0, y0, x0 + kw - 16, y0 + kh - 16), 14, fill=col)
            d.rounded_rectangle((x0 + 6, y0 + 6, x0 + kw - 22, y0 + kh - 30), 10, fill=mix(col, (255, 255, 255), 0.18))
            t = labels[r][c]
            if t:
                ff = f if len(t) <= 2 else fs
                tw = d.textlength(t, font=ff)
                d.text((x0 + (kw - 16 - tw) / 2, y0 + (kh - 16) / 2 - (26 if ff == f else 16)), t, font=ff, fill=(30, 30, 30))
    save(img, "register_keys")


def lcd():
    w, h = 512, 128
    img = Image.new("RGB", (w, h), (20, 40, 20))
    d = ImageDraw.Draw(img)
    f = font("PTSans-Bold.ttf", 90)
    t = "0.00"
    d.text((w - d.textlength(t, font=f) - 30, 5), t, font=f, fill=(150, 255, 140))
    save(img.filter(ImageFilter.GaussianBlur(0.8)), "lcd")


def price_tags():
    w, h = 1024, 256
    img = Image.new("RGB", (w, h), (250, 248, 240))
    d = ImageDraw.Draw(img)
    f = font("PTSerif-Bold.ttf", 76)
    prices = ["$1.50", "$2.25", "$3.00", "$1.75"]
    for i, p in enumerate(prices):
        x0 = i * 256
        d.rectangle((x0 + 4, 4, x0 + 252, h - 4), outline=(180, 40, 40), width=6)
        tw = d.textlength(p, font=f)
        d.text((x0 + (256 - tw) / 2, 78), p, font=f, fill=(40, 30, 30))
    save(img, "price_tags")


def door_decal():
    s = 1024
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cream = (240, 228, 200, 235)
    c = s / 2
    oy = c - 110
    d.ellipse((c - 300, oy - 300, c + 300, oy + 300), outline=cream, width=14)
    d.ellipse((c - 268, oy - 268, c + 268, oy + 268), outline=cream, width=4)
    # Сова.
    d.ellipse((c - 130, oy - 150, c + 130, oy + 170), outline=cream, width=12)
    for side in (-1, 1):
        ex = c + side * 60
        d.ellipse((ex - 52, oy - 80, ex + 52, oy + 24), outline=cream, width=10)
        d.ellipse((ex - 18, oy - 46, ex + 18, oy - 10), fill=cream)
        d.polygon([(c + side * 100, oy - 130), (c + side * 150, oy - 210), (c + side * 50, oy - 145)], fill=cream)
    d.polygon([(c - 20, oy + 30), (c + 20, oy + 30), (c, oy + 76)], fill=cream)
    f = font("PTSerif-Bold.ttf", 110)
    f2 = font("PTSans-Bold.ttf", 60)
    for text, y, ff in (("NIGHT OWL", oy + 320, f), ("OPEN LATE", oy + 450, f2)):
        tw = d.textlength(text, font=ff)
        d.text((c - tw / 2, y), text, font=ff, fill=cream)
    save(img, "door_decal")


def main():
    print("Текстуры реквизита →", OUT)
    wood("wood_walnut", (58, 34, 22), (122, 78, 48), 1)
    wood("wood_oak", (120, 84, 50), (196, 150, 98), 2)
    wood("wood_door", (64, 30, 22), (128, 64, 42), 3, knots=0)
    wood("wood_pine", (170, 128, 80), (226, 190, 140), 4, knots=4)
    wood("wood_cedar", (96, 50, 30), (164, 96, 60), 5)
    metal_wear()
    leather("leather_brown", (60, 36, 22), (126, 84, 52), 6)
    leather("leather_red", (70, 16, 18), (140, 40, 38), 8)
    paper("paper_plain", (244, 240, 230), 9)
    paper("paper_aged", (232, 220, 190), 10, aged=True)
    granite()
    speaker_mesh()
    safe_enamel()
    safe_door()
    safe_wheels()
    watch_dial()
    watch_crack()
    poster_art()
    postcard_back()
    postcard_front()
    safe_note()
    receipt()
    outage_notice()
    papers_typed()
    papers_letterhead()
    page_edges()
    diary_pages()
    photo_tiles()
    portrait()
    mirror_fog()
    dust_ring()
    register_keys()
    lcd()
    price_tags()
    door_decal()


if __name__ == "__main__":
    main()
