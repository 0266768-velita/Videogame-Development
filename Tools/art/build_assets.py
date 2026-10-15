"""Generates every sprite, the ground map and level.json. Run: python3 build_assets.py <Assets/Resources dir>"""
import sys, os, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chars_player, chars_enemies, props, level

def main(res):
    sp = os.path.join(res, 'Sprites'); lv = os.path.join(res, 'Level')
    os.makedirs(sp, exist_ok=True); os.makedirs(lv, exist_ok=True)
    chars_player.build().save(os.path.join(sp, 'tlahuelli.png'))
    for k, im in chars_enemies.build_all().items(): im.save(os.path.join(sp, k + '.png'))
    for k, c in props.build_all().items(): c.img.save(os.path.join(sp, k + '.png'))
    m, ground, wo, data = level.build()
    ground.save(os.path.join(sp, 'ground.png'))
    for i, f in enumerate(wo): f.save(os.path.join(sp, f'water_{i}.png'))
    with open(os.path.join(lv, 'level.json'), 'w') as f: json.dump(data, f, indent=1)
    print('assets written to', res)

if __name__ == '__main__': main(sys.argv[1])
