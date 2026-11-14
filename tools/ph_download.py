# -*- coding: utf-8 -*-
"""Download Poly Haven assets (CC0) by id.
    python -I tools/ph_download.py model 2k id1 id2 ...      -> work/assets/polyhaven/<id>/<id>_2k.gltf + .bin + textures/
    python -I tools/ph_download.py texture 2k id1 id2 ...    -> work/assets/polyhaven/tex_<id>/ (diffuse, normal gl, roughness jpg)
    add --import to copy what was fetched into unity/Assets/Props/<id> or unity/Assets/Textures/tex_<id>
Nothing about this machine or project is sent: plain GETs to api.polyhaven.com and dl.polyhaven.org with a generic client name."""
import sys, os, json, shutil, urllib.request, time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'work', 'assets', 'polyhaven')
UA = {'User-Agent': 'asset-fetch/1.0'}


def get(url, tries=4):
    for k in range(tries):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=90) as r: return r.read()
        except Exception as e:
            if k == tries - 1: raise
            time.sleep(2 + 3 * k)


def save(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 0: return os.path.getsize(path)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    data = get(url)
    with open(path + '.part', 'wb') as f: f.write(data)
    os.replace(path + '.part', path)
    return len(data)


def model(aid, res):
    files = json.loads(get(f'https://api.polyhaven.com/files/{aid}'))
    g = files['gltf']; res = res if res in g else sorted(g)[0]
    main = g[res]['gltf']; d = os.path.join(OUT, aid.lower()); total = save(main['url'], os.path.join(d, os.path.basename(main['url'])))
    for rel, inc in main.get('include', {}).items(): total += save(inc['url'], os.path.join(d, rel.replace('/', os.sep)))
    return d, total, res


def texture(aid, res):
    files = json.loads(get(f'https://api.polyhaven.com/files/{aid}'))
    d = os.path.join(OUT, 'tex_' + aid.lower()); total = 0; got = []
    for key in ('Diffuse', 'nor_gl', 'Rough'):
        if key not in files: continue
        e = files[key]; r = res if res in e else sorted(e)[0]; u = e[r]['jpg']['url']
        total += save(u, os.path.join(d, os.path.basename(u))); got.append(key)
    return d, total, ','.join(got)


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if a != '--import']; do_import = '--import' in sys.argv
    kind, res, ids = args[0], args[1], args[2:]; log = []
    for aid in ids:
        try:
            d, n, info = (model if kind == 'model' else texture)(aid, res)
            dst = os.path.join(ROOT, 'unity', 'Assets', 'Props' if kind == 'model' else 'Textures', os.path.basename(d))
            if do_import: shutil.copytree(d, dst, dirs_exist_ok=True)
            print(f'ok   {aid:32s} {n / 1e6:6.1f} MB  {info}'); log.append({'id': aid, 'kind': kind, 'dir': d, 'bytes': n})
        except Exception as e:
            print(f'FAIL {aid:32s} {e}')
    p = os.path.join(OUT, 'downloads2.json'); old = json.load(open(p, encoding='utf-8')) if os.path.exists(p) else []
    json.dump(old + log, open(p, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
