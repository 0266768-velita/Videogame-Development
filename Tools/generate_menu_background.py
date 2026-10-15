"""Pixel-art menu background (night jungle + stepped pyramid + moon). Run: python3 generate_menu_background.py <out.png>
Replace Assets/Resources/UI/MenuBackground.png with your own art (any 16:9 size) at any time."""
import sys, random, math
from PIL import Image, ImageDraw
W,H=480,270; random.seed(4)
img=Image.new('RGB',(W,H)); d=ImageDraw.Draw(img)
top=(12,6,28); mid=(70,20,50); low=(190,70,50)
for y in range(H):
    k=y/(H*0.7); k=min(1,k)
    c=tuple(int(top[i]+(mid[i]-top[i])*min(1,k*1.4)) for i in range(3)) if k<0.7 else tuple(int(mid[i]+(low[i]-mid[i])*((k-0.7)/0.3)) for i in range(3))
    d.line([(0,y),(W,y)],fill=c)
for _ in range(110):
    x,y=random.randrange(W),random.randrange(int(H*0.55)); b=random.choice([150,200,255])
    d.point((x,y),fill=(b,b,min(255,b+20)))
mx,my=350,60
for r,c in ((26,(90,50,90)),(21,(160,120,150)),(18,(235,225,200))): d.ellipse([mx-r,my-r,mx+r,my+r],fill=c)
for cx,cy,r in ((mx-6,my-4,4),(mx+6,my+7,3),(mx+3,my-9,2)): d.ellipse([cx-r,cy-r,cx+r,cy+r],fill=(205,195,170))
def mountains(base,amp,col,seed):
    rnd=random.Random(seed); pts=[(0,H)]; y=base
    for x in range(0,W+8,8):
        y=max(base-amp,min(base+amp,y+rnd.randint(-7,7))); pts.append((x,y))
    pts.append((W,H)); d.polygon(pts,fill=col)
mountains(175,22,(44,18,48),1); mountains(195,16,(30,12,36),2)
# pyramid
px,py=170,200; steps=7
for i in range(steps):
    w=(steps-i)*30; y1=py-i*14
    d.rectangle([px-w//2,y1-14,px+w//2,y1],fill=(26+i*3,14+i*2,34+i*2))
    d.line([px-w//2,y1-14,px+w//2,y1-14],fill=(70,36,70))
d.rectangle([px-14,py-steps*14-16,px+14,py-steps*14],fill=(34,18,40))
for sx in (px-8,px+5): d.rectangle([sx,py-steps*14-10,sx+3,py-steps*14-3],fill=(255,170,60))   # lit temple windows
for i in range(steps):   # stairs glow
    d.line([px,py-i*14,px,py-i*14-14],fill=(255,150,60))
mountains(225,12,(14,8,22),3)
# jungle trees
def tree(x,base,h,col):
    d.rectangle([x-2,base-h,x+2,base],fill=(10,6,14))
    for k in range(3): 
        r=14-k*3; d.ellipse([x-r,base-h-r+k*7,x+r,base-h+r+k*7],fill=col)
for x in range(-10,W+20,34): tree(x+random.randint(-6,6),H,random.randint(50,80),(14,32,30))
for x in range(5,W,50): tree(x+random.randint(-8,8),H,random.randint(28,50),(8,20,22))
# mist
mist=Image.new('RGBA',(W,H),(0,0,0,0)); md=ImageDraw.Draw(mist)
for _ in range(14):
    x,y=random.randrange(W),random.randrange(180,250); w=random.randint(60,140)
    md.ellipse([x-w,y-6,x+w,y+6],fill=(200,170,200,28))
img=Image.alpha_composite(img.convert('RGBA'),mist).convert('RGB')
# fireflies / embers
dd=ImageDraw.Draw(img)
for _ in range(40):
    x,y=random.randrange(W),random.randrange(120,260); dd.point((x,y),fill=(255,200,90))
img=img.resize((1920,1080),Image.NEAREST); img.save(sys.argv[1])
