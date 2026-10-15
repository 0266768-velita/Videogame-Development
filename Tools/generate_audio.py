"""Procedural placeholder audio for Mictlan. Run: python3 generate_audio.py <output_dir>
Writes 22050 Hz mono 16-bit WAVs. Replace any file in Assets/Resources/Audio with your own (same name)."""
import sys, os, wave
import numpy as np
SR = 22050
rng = np.random.default_rng(7)

def t(d): return np.arange(int(SR*d))/SR
def env(n, a=0.005, r=0.1, curve=2.0):
    e = np.ones(n); na=min(n,max(1,int(a*SR))); nr=min(n,max(1,int(r*SR)))
    e[:na] = np.linspace(0,1,na); e[-nr:] *= np.linspace(1,0,nr)**curve
    return e
def sine(f,d): return np.sin(2*np.pi*np.cumsum(np.broadcast_to(f,(int(SR*d),)))/SR)
def square(f,d,duty=0.5): return np.where((np.cumsum(np.broadcast_to(f,(int(SR*d),)))/SR)%1<duty,1.,-1.)*0.6
def saw(f,d): return ((np.cumsum(np.broadcast_to(f,(int(SR*d),)))/SR)%1*2-1)*0.6
def tri(f,d): return 2*np.abs(2*((np.cumsum(np.broadcast_to(f,(int(SR*d),)))/SR)%1)-1)-1
def noise(d): return rng.uniform(-1,1,int(SR*d))
def lp(x,k=0.1):
    y=np.zeros_like(x); a=0
    for i,v in enumerate(x): a+=k*(v-a); y[i]=a
    return y
def sweep(f0,f1,d): return np.linspace(f0,f1,int(SR*d))
def norm(x,g=0.8): m=np.max(np.abs(x)); return x/m*g if m>0 else x
def midi(n): return 440*2**((n-69)/12)
def save(path,x):
    x=np.clip(x,-1,1); d=(x*32767).astype('<i2')
    with wave.open(path,'wb') as w: w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())

def tone(wave_fn,f,d,a=0.005,r=0.1): 
    x=wave_fn(f,d); return x*env(len(x),a,r)
def seq(notes, wave_fn=square, d=0.1, gap=0.0, r=0.06):
    return np.concatenate([tone(wave_fn,midi(n),d,r=r) if n else np.zeros(int(SR*d)) for n in notes])

SFX = {}
SFX['ui_move']   = lambda: tone(square, 660, 0.04, r=0.03)*0.5
SFX['ui_select'] = lambda: seq([72,79],square,0.06)*0.6
SFX['ui_back']   = lambda: seq([72,65],square,0.06)*0.6
SFX['tip']       = lambda: seq([76,83,88],tri,0.08)*0.8
SFX['swing']     = lambda: lp(noise(0.18),0.25)*env(int(SR*0.18),0.01,0.15)*1.5
def _hit(): 
    n=lp(noise(0.2),0.3)*env(int(SR*0.2),0.001,0.2,3); return n*1.5+tone(sine,sweep(180,60,0.2),0.2,0.001,0.18)
SFX['enemy_hit'] = _hit
SFX['hurt']      = lambda: tone(saw,sweep(300,90,0.28),0.28,0.002,0.25)*0.8+lp(noise(0.28),0.4)*env(int(SR*0.28),0.001,0.25)*0.6
SFX['telegraph'] = lambda: tone(saw,sweep(150,420,0.4),0.4,0.05,0.05)*0.5
SFX['alert']     = lambda: seq([79,79,86],square,0.07)*0.6
SFX['dodge']     = lambda: lp(noise(0.22),0.5)*env(int(SR*0.22),0.01,0.2)*np.sin(np.linspace(0,3.14,int(SR*0.22)))*2
SFX['perfect']   = lambda: seq([79,84,88,91],tri,0.07)*1.0+0.0
SFX['interrupt'] = lambda: tone(square,sweep(900,300,0.2),0.2,0.002,0.15)*0.7+tone(sine,200,0.2)*0.5
SFX['potion']    = lambda: seq([67,71,74,79],sine,0.09)*0.9
SFX['fury']      = lambda: tone(saw,sweep(60,240,1.0),1.0,0.05,0.4)*0.7+tone(saw,sweep(61,242,1.0),1.0,0.05,0.4)*0.5+lp(noise(1.0),0.1)*env(int(SR),0.2,0.6)*0.8
SFX['seismic']   = lambda: tone(sine,sweep(90,35,0.8),0.8,0.002,0.7)*1.2+lp(noise(0.8),0.08)*env(int(SR*0.8),0.002,0.7)*1.5
SFX['jump']      = lambda: tone(square,sweep(300,600,0.12),0.12,0.002,0.08)*0.5
SFX['land']      = lambda: lp(noise(0.08),0.3)*env(int(SR*0.08),0.001,0.08)*1.5
SFX['turn_start']= lambda: seq([72,76],tri,0.09)*0.7
SFX['error']     = lambda: tone(square,110,0.15,0.002,0.05)*0.6
SFX['enemy_die'] = lambda: tone(saw,sweep(400,40,0.7),0.7,0.002,0.5)*0.7+lp(noise(0.7),0.2)*env(int(SR*0.7),0.002,0.6)
SFX['victory']   = lambda: seq([72,76,79,84,0,79,84],square,0.14,r=0.1)*0.7
SFX['defeat']    = lambda: seq([69,66,62,57],saw,0.3,r=0.25)*0.6
SFX['logo']      = lambda: seq([60,67,72,79],sine,0.18,r=0.3)*0.9

def mix(*parts):
    n=max(len(p) for p in parts); out=np.zeros(n)
    for p in parts: out[:len(p)]+=p
    return out
def place(buf, snd, pos):
    i=int(pos*SR); j=min(len(buf),i+len(snd)); buf[i:j]+=snd[:j-i]

def music(bpm, bars, chords, bass_oct, lead_fn, arp=None, drums=None, lead_wave=tri, bass_wave=saw, pad=True, seed=1):
    r=np.random.default_rng(seed)
    beat=60/bpm; bar=beat*4; total=bars*bar; buf=np.zeros(int(SR*total)+SR)
    for b in range(bars):
        ch=chords[b%len(chords)]; t0=b*bar
        if pad:
            for n in ch: place(buf, tone(tri,midi(n),bar*0.98,0.2,0.3)*0.14, t0)
        for k in range(8):   # bass 8ths
            place(buf, tone(bass_wave,midi(ch[0]-12*bass_oct),beat*0.45,0.005,0.08)*0.28, t0+k*beat/2)
        if arp:
            for k in range(16):
                place(buf, tone(square,midi(ch[k%len(ch)]+12),beat*0.22,0.002,0.1)*0.09, t0+k*beat/4)
        for (pos,note,dur) in lead_fn(b,ch,r):
            place(buf, tone(lead_wave,midi(note),dur*beat,0.01,0.12)*0.3, t0+pos*beat)
        if drums:
            for k in range(8):
                if drums in ('full','soft') and k%4==0:
                    place(buf, tone(sine,sweep(120,45,0.15),0.15,0.001,0.12)*0.8, t0+k*beat/2)
                if drums=='full' and k%4==2:
                    place(buf, lp(noise(0.12),0.5)*env(int(SR*0.12),0.001,0.1)*0.4, t0+k*beat/2)
                if drums=='full':
                    place(buf, noise(0.03)*env(int(SR*0.03),0.001,0.03)*0.12, t0+k*beat/2+beat/4)
    n=int(SR*total)
    out=buf[:n].copy(); out[:len(buf)-n]+=buf[n:n+len(buf)-n][:len(out)] if len(buf)-n<=len(out) else 0  # wrap tails: seamless
    return norm(out,0.7)

def lead_menu(b,ch,r):
    sc=[0,2,3,5,7,8,10]; root=ch[0]+12; out=[]; pos=0
    while pos<4:
        d=r.choice([1,1,2]); out.append((pos,root+sc[r.integers(0,7)]+(12 if r.random()<.2 else 0),d*0.9)); pos+=d
    return out
def lead_explore(b,ch,r):
    return [(0,ch[2]+12,1.5),(2,ch[1]+12,1),(3,ch[2]+12+2,1)] if b%2==0 else [(0,ch[1]+12,2),(2,ch[0]+12+7,2)]
def lead_battle(b,ch,r):
    return [(i*0.5,ch[[0,2,1,2][i%4]]+12+(12 if i%8==7 else 0),0.45) for i in range(8)]
def lead_over(b,ch,r): return [(0,ch[2],3.5)]

MUSIC = {
 'music_menu':    lambda: music(72,16,[[57,60,64],[53,57,60],[55,59,62],[52,56,59]],2,lead_menu,drums='soft',seed=3),
 'music_explore': lambda: music(84,16,[[62,65,69],[58,62,65],[60,64,67],[57,60,64]],2,lead_explore,arp=True,seed=5),
 'music_battle':  lambda: music(138,16,[[57,60,64],[57,60,64],[53,57,60],[55,59,62]],2,lead_battle,arp=True,drums='full',lead_wave=square,seed=9),
 'music_gameover':lambda: music(52,6,[[57,60,64],[53,56,60],[52,55,59]],2,lead_over,seed=11,bass_wave=sine),
}

if __name__=='__main__':
    out=sys.argv[1]; os.makedirs(out,exist_ok=True)
    for k,f in SFX.items(): save(os.path.join(out,k+'.wav'), norm(f(),0.8))
    for k,f in MUSIC.items(): save(os.path.join(out,k+'.wav'), f())
    print(len(SFX)+len(MUSIC),'files')
