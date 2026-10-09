from PIL import Image, ImageDraw, ImageFilter
import math, sys
S = 4  # supersample
def hexc(h, a=255): h=h.lstrip('#'); return (int(h[0:2],16), int(h[2:4],16), int(h[4:6],16), a)

def background(N):
    n = N*S
    im = Image.new('RGBA', (n, n))
    top, bot = hexc('#2B1055'), hexc('#FF7E5F')
    d = ImageDraw.Draw(im)
    for y in range(n):
        t = min(1, y / (n*0.72))
        t = t*t*(3-2*t)
        d.line([(0,y),(n,y)], fill=tuple(int(top[i]+(bot[i]-top[i])*t) for i in range(3))+(255,))
    # sun with glow + stripes
    cx, cy, r = n*0.5, n*0.50, n*0.30
    glow = Image.new('RGBA', (n,n), (0,0,0,0)); gd = ImageDraw.Draw(glow)
    gd.ellipse([cx-r*1.25, cy-r*1.25, cx+r*1.25, cy+r*1.25], fill=hexc('#FFD86F',140))
    glow = glow.filter(ImageFilter.GaussianBlur(n*0.05)); im.alpha_composite(glow)
    sun = Image.new('RGBA', (n,n), (0,0,0,0)); sd = ImageDraw.Draw(sun)
    a, b = hexc('#FFE27A'), hexc('#FF4FD8')
    for y in range(int(cy-r), int(cy+r)):
        t = (y-(cy-r))/(2*r); w = math.sqrt(max(0, r*r-(y-cy)**2))
        sd.line([(cx-w,y),(cx+w,y)], fill=tuple(int(a[i]+(b[i]-a[i])*t) for i in range(3))+(255,))
    k = 0
    y = cy + r*0.05
    while y < cy + r:
        gap = n*0.008 + (y-cy)/r*n*0.022
        sd.rectangle([0, y, n, y+gap], fill=(0,0,0,0)); y += n*0.055
    im.alpha_composite(sun)
    # neon grid floor
    hz = n*0.70
    fl = Image.new('RGBA', (n,n), (0,0,0,0)); fd = ImageDraw.Draw(fl)
    fd.rectangle([0,hz,n,n], fill=hexc('#1A0B2E'))
    pink = hexc('#FF4FD8')
    fd.line([(0,hz),(n,hz)], fill=pink, width=int(n*0.012))
    for i in range(-12, 13):
        fd.line([(n/2 + i*n*0.03, hz), (n/2 + i*n*0.22, n)], fill=hexc('#FF4FD8',150), width=int(n*0.004))
    yy, step = hz, n*0.02
    while yy < n:
        fd.line([(0,yy),(n,yy)], fill=hexc('#FF4FD8',150), width=int(n*0.004)); step *= 1.45; yy += step
    im.alpha_composite(fl)
    return im

def cube(n, size, cx, cy, angle):
    """The Dasher character, drawn like the in-game sprite."""
    c = Image.new('RGBA', (int(size*1.6), int(size*1.6)), (0,0,0,0)); d = ImageDraw.Draw(c)
    o = size*0.3; s = size
    body, accent, dark = hexc('#3DF2FF'), hexc('#FF4FD8'), hexc('#1B6C72')
    d.rounded_rectangle([o, o, o+s, o+s], radius=s*0.18, fill=dark)
    i = s*0.075
    d.rounded_rectangle([o+i, o+i, o+s-i, o+s-i], radius=s*0.13, fill=body)
    d.rectangle([o+i, o+s*0.66, o+s-i, o+s*0.80], fill=accent)
    d.rounded_rectangle([o+s*0.16, o+s*0.13, o+s*0.47, o+s*0.22], radius=s*0.05, fill=(255,255,255,120))
    pupil = hexc('#120C26')
    for ex in (0.52, 0.74):
        d.rounded_rectangle([o+s*(ex-0.07), o+s*0.33, o+s*(ex+0.07), o+s*0.57], radius=s*0.06, fill=(255,255,255,255))
        d.rounded_rectangle([o+s*(ex-0.01), o+s*0.38, o+s*(ex+0.07), o+s*0.54], radius=s*0.04, fill=pupil)
    d.line([(o+s*0.42, o+s*0.27), (o+s*0.58, o+s*0.31)], fill=dark, width=int(s*0.045))
    d.line([(o+s*0.66, o+s*0.31), (o+s*0.82, o+s*0.27)], fill=dark, width=int(s*0.045))
    c = c.rotate(angle, resample=Image.BICUBIC)
    layer = Image.new('RGBA', (n,n), (0,0,0,0))
    g = Image.new('RGBA', (n,n), (0,0,0,0)); gd = ImageDraw.Draw(g)
    gd.ellipse([cx-size*0.85, cy-size*0.85, cx+size*0.85, cy+size*0.85], fill=hexc('#3DF2FF',110))
    layer.alpha_composite(g.filter(ImageFilter.GaussianBlur(size*0.18)))
    # speed streaks
    sd = ImageDraw.Draw(layer)
    for k,(dy,ln) in enumerate([(-0.25,0.9),(0.05,1.25),(0.32,0.7)]):
        y = cy + dy*size; x1 = cx - size*0.62
        sd.rounded_rectangle([x1-ln*size, y-size*0.035, x1, y+size*0.035], radius=size*0.035, fill=(255,255,255,200 - k*40))
    layer.alpha_composite(c, (int(cx - c.width/2), int(cy - c.height/2)))
    return layer

def full_icon(N):
    im = background(N); n = N*S
    im.alpha_composite(cube(n, n*0.42, n*0.55, n*0.53, 12))
    return im.resize((N,N), Image.LANCZOS)

out = sys.argv[1]
full_icon(1024).convert('RGB').save(out + '/icon-1024.png')
full_icon(512).convert('RGB').save(out + '/icon-512.png')
# Android adaptive icon layers (432px, content in the central 66% safe zone)
A = 432
background(A).resize((A,A), Image.LANCZOS).convert('RGB').save(out + '/adaptive-background.png')
fg = cube(A*S, A*S*0.30, A*S*0.53, A*S*0.51, 12).resize((A,A), Image.LANCZOS); fg.save(out + '/adaptive-foreground.png')
print('ok')
