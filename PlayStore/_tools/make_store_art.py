from PIL import Image, ImageDraw, ImageFont, ImageFilter
import os
SRC = 'D:/td_store'
FONT = 'D:/Umair/Unity Projects/thedash/thedash/Assets/Resources/Fonts/LilitaOne.ttf'
PINK, CYAN = (255, 79, 216), (61, 242, 255)

shots = [
    ('03_run_neoncity', 'TAP. JUMP. DASH FOREVER!'),
    ('08_run_ember',    'AN ENDLESS NEON PATH'),
    ('07_run_aurora',   '5 GLOWING WORLDS TO EXPLORE'),
    ('09_run_skyhigh',  'GRAB SHIELDS & MAGNETS'),
    ('04_shop',         'UNLOCK 8 COOL SKINS'),
    ('06_missions',     'COMPLETE MISSIONS, EARN COINS'),
    ('05_daily',        'DAILY REWARDS EVERY DAY'),
    ('10_gameover',     'BEAT YOUR BEST SCORE!'),
]

def glow_text(img, xy, text, font, fill, glow, blur):
    layer = Image.new('RGBA', img.size, (0,0,0,0)); d = ImageDraw.Draw(layer)
    d.text(xy, text, font=font, fill=glow + (255,), anchor='mm', stroke_width=max(2, blur//2), stroke_fill=glow + (255,))
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))
    d = ImageDraw.Draw(img)
    d.text((xy[0], xy[1] + blur//2), text, font=font, fill=(20, 6, 40, 200), anchor='mm')
    d.text(xy, text, font=font, fill=fill, anchor='mm', stroke_width=max(2, blur//3), stroke_fill=glow)

def background(W, H):
    bg = Image.new('RGBA', (W, H)); d = ImageDraw.Draw(bg)
    top, bot = (43, 16, 85), (12, 4, 30)
    for y in range(H):
        t = y / H
        d.line([(0, y), (W, y)], fill=tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)) + (255,))
    return bg

def framed(name, caption, W, H, out):
    bg = background(W, H)
    s = W / 1920
    font = ImageFont.truetype(FONT, int(86 * s))
    glow_text(bg, (W // 2, int(92 * s)), caption, font, (255, 255, 255), PINK, int(10 * s))
    shot = Image.open(f'{SRC}/{name}.png').convert('RGBA')
    sw, sh = int(1600 * s), int(900 * s)
    shot = shot.resize((sw, sh), Image.LANCZOS)
    x, y = (W - sw) // 2, int(165 * s)
    r = int(30 * s)
    glow = Image.new('RGBA', (W, H), (0,0,0,0)); gd = ImageDraw.Draw(glow)
    gd.rounded_rectangle([x - 8*s, y - 8*s, x + sw + 8*s, y + sh + 8*s], radius=r + 8*s, fill=PINK + (200,))
    bg.alpha_composite(glow.filter(ImageFilter.GaussianBlur(18 * s)))
    mask = Image.new('L', (sw, sh), 0); ImageDraw.Draw(mask).rounded_rectangle([0, 0, sw - 1, sh - 1], radius=r, fill=255)
    border = Image.new('RGBA', (W, H), (0,0,0,0))
    ImageDraw.Draw(border).rounded_rectangle([x - 5*s, y - 5*s, x + sw + 5*s, y + sh + 5*s], radius=r + 5*s, fill=PINK + (255,))
    bg.alpha_composite(border)
    bg.paste(shot, (x, y), mask)
    bg.convert('RGB').save(out, optimize=True)

for folder, (W, H) in {'screenshots/phone': (1920, 1080), 'screenshots/tablet-7inch': (1920, 1080), 'screenshots/tablet-10inch': (2560, 1440)}.items():
    os.makedirs(folder, exist_ok=True)
    for i, (n, c) in enumerate(shots, 1):
        framed(n, c, W, H, f'{folder}/{i:02d}_{n[3:]}.png')

os.makedirs('screenshots/raw-no-captions', exist_ok=True)
for n, _ in shots + [('01_menu', '')]:
    Image.open(f'{SRC}/{n}.png').convert('RGB').resize((1920, 1080), Image.LANCZOS).save(f'screenshots/raw-no-captions/{n}.png', optimize=True)

# ---------------- feature graphic 1024x500
fg = Image.open('D:/td_store_feature/feature_raw.png').convert('RGBA').resize((1024, 500), Image.LANCZOS)
shade = Image.new('RGBA', fg.size, (0,0,0,0)); sd = ImageDraw.Draw(shade)
for x in range(1024):
    a = int(150 * max(0, (x - 330) / 694) ** 0.8)
    sd.line([(x, 0), (x, 500)], fill=(20, 6, 45, a))
fg.alpha_composite(shade)
glow_text(fg, (690, 200), 'THE DASH', ImageFont.truetype(FONT, 132), (255, 255, 255), PINK, 9)
glow_text(fg, (690, 300), 'ENDLESS NEON RUNNER', ImageFont.truetype(FONT, 46), CYAN, (20, 6, 45), 4)
fg.convert('RGB').save('feature-graphic-1024x500.png', optimize=True)
print('done')
