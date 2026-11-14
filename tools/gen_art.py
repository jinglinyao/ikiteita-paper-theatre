# -*- coding: utf-8 -*-
"""Paper pieces for the street set, generated in the print style of the scanned 1913 toy theatre they stand in
(chromolithograph scenery sheets). MiniMax image-01; the key comes from the environment and is never printed.
    python -P tools/gen_art.py <out_dir> [name ...]        (no names = everything not yet on disk)
"""
import os, sys, json, time, urllib.request
sys.stdout.reconfigure(encoding='utf-8')
out = sys.argv[1]; only = set(sys.argv[2:]); os.makedirs(out, exist_ok=True)
KEY = os.environ['MINIMAX_API_KEY']; H = {'Authorization': 'Bearer ' + KEY, 'Content-Type': 'application/json'}
STYLE = ('printed as an antique chromolithograph toy-theatre scenery sheet from around 1910, fine engraved line work and cross-hatching, '
         'slightly faded inks on aged cream paper with visible paper fibres and faint foxing, flat frontal orthographic view, even flat light with no cast shadows, '
         'no people, no lettering, isolated on a plain pure white background')
JOBS = {
 'facade_R': ('3:4', 'Front elevation of a narrow five-storey Japanese apartment block at night, muted indigo and slate-blue walls, a tiled roof edge with a water tank, a drainpipe, '
                     'small iron balconies, an awning over a shuttered shop on the ground floor, exactly six tall windows arranged in two columns and three rows, every window opening filled with solid pure black, ' + STYLE),
 'facade_L': ('3:4', 'Front elevation of an old four-storey Japanese town building at night, dusty blue-grey plaster walls, wooden eaves, a rusty exterior staircase on one side, an electricity meter box, '
                     'exactly four windows arranged in two columns and two rows, every window opening filled with solid pure black, ' + STYLE),
 'far_row': ('16:9', 'A long continuous row of distant low city buildings, rooftops, water tanks, television aerials and telegraph poles with sagging wires at night, pale grey-blue and lavender inks, '
                     'tiny dark windows, a wide horizontal strip, ' + STYLE),
 'lamp_post': ('9:16', 'A single tall old Japanese street lamp on a wooden utility pole with a round glass lamp, a transformer box and a few wires, dark green and iron-grey inks, the whole pole from base to top, ' + STYLE),
 'sky': ('16:9', 'A night sky backdrop cloth for a toy theatre: deep Prussian blue fading to a paler blue-grey at the horizon, a scatter of small stars, a few thin stylised engraved clouds, '
                 'printed as an antique chromolithograph on aged paper with fine horizontal hatching and faint creases, fills the whole frame edge to edge, no moon, no people, no lettering'),
 'panes': ('16:9', 'A sheet of eight separate square vignettes arranged in a tidy grid of four columns and two rows with wide white gaps between them, each vignette a hand-cut black paper silhouette '
                   'against warm amber backlit parchment: a desk lamp, an alarm clock with two bells, a notebook with a pencil, an umbrella, a bicycle, a school backpack, a pair of shoes, a lunch box with chopsticks; '
                   'shadow-puppet paper-cut style, slightly irregular scissor-cut edges, visible paper fibres, no lettering, plain white background'),
 **{'sil_' + k: ('3:4', 'A single hand-cut black paper silhouette of ' + v + ', standing on a thin black strip of table edge at the bottom, shadow-puppet paper-cut style with slightly irregular '
                            'scissor-cut edges and one or two small cut-out details, solid matte black paper, centred, large in the frame, on a plain pure white background, no lettering, no other objects')
    for k, v in {'lamp': 'an adjustable desk lamp with a conical shade', 'clock': 'an old twin-bell alarm clock', 'book': 'an open notebook with a pencil lying across it',
                 'umbrella': 'a closed umbrella leaning against a wall hook', 'bike': 'a bicycle seen from the side', 'bag': 'a Japanese school backpack (randoseru)'}.items()},
 'vulture': ('1:1', 'A theatrical mask of a bald vulture head with a long hooked beak and a ruff of torn paper feathers round the neck, painted in grey, bone white and dull pink, built from folded cardboard and papier-mache, angular paper-craft facets, hand-painted, visible seams and glue tabs, menacing narrow eyes, the whole head from the top to the chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
 'crow': ('1:1', 'A theatrical mask of a crow head with a heavy black beak and glossy folded black paper feathers, painted in black and blue-black, built from folded cardboard and papier-mache, angular paper-craft facets, hand-painted, visible seams and glue tabs, menacing narrow eyes, the whole head from the top to the chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
 'wolf': ('1:1', 'A theatrical mask of a snarling wolf head with pointed ears and torn paper teeth, painted in grey, charcoal and off-white, built from folded cardboard and papier-mache, angular paper-craft facets, hand-painted, visible seams and glue tabs, menacing narrow eyes, the whole head from the top to the chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
 'crocodile': ('1:1', 'A theatrical mask of a crocodile head with a long flat snout, rows of torn paper teeth and ridged scales, painted in olive green and mud brown, built from folded cardboard and papier-mache, angular paper-craft facets, hand-painted, visible seams and glue tabs, menacing narrow eyes, the whole head from the top to the chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
 'boar': ('1:1', 'A theatrical mask of a wild boar head with a broad snout, two curved paper tusks and bristles of cut paper, painted in dark brown and rust, built from folded cardboard and papier-mache, angular paper-craft facets, hand-painted, visible seams and glue tabs, menacing narrow eyes, the whole head from the top to the chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
 'hyena': ('1:1', 'A theatrical mask of a snarling hyena head built from folded cardboard and papier-mache, angular paper-craft facets, torn paper teeth, hand-painted in ochre, umber and black, '
                  'visible seams and glue tabs, menacing narrow eyes, the whole head from ears to chin, front three-quarter view, soft even studio light, plain pure white background, product photograph'),
}


def call(url, body=None, timeout=240):
    req = urllib.request.Request(url, data=json.dumps(body).encode() if body is not None else None, headers=H, method='POST' if body is not None else 'GET')
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r: return json.loads(r.read().decode('utf-8', 'replace'))
    except urllib.error.HTTPError as e: return {'http_error': e.code, 'body': e.read().decode('utf-8', 'replace')[:400]}


def remains():
    d = call('https://api.minimaxi.com/v1/token_plan/remains')
    return [{k: m.get(k) for k in ('model_name', 'current_weekly_usage_count', 'current_weekly_remaining_percent')} for m in d.get('model_remains', []) if 'image' in str(m.get('model_name', '')).lower()] or str(d)[:200]


print('quota before:', json.dumps(remains(), ensure_ascii=False))
for name, (ar, prompt) in JOBS.items():
    p = os.path.join(out, name + '.jpg')
    if (only and name not in only) or (not only and os.path.exists(p)): continue
    t0 = time.time()
    r = call('https://api.minimaxi.com/v1/image_generation', {'model': 'image-01', 'prompt': prompt, 'aspect_ratio': ar, 'response_format': 'url', 'n': 1, 'prompt_optimizer': False})
    urls = ((r.get('data') or {}).get('image_urls') or []) if isinstance(r, dict) else []
    if not urls: print(name, 'FAILED', json.dumps(r, ensure_ascii=False)[:300]); continue
    with urllib.request.urlopen(urllib.request.Request(urls[0], headers={'User-Agent': 'Mozilla/5.0'}), timeout=180) as f: open(p, 'wb').write(f.read())
    print(name, ar, 'ok', round(time.time() - t0, 1), 's', os.path.getsize(p) // 1024, 'KB')
print('quota after:', json.dumps(remains(), ensure_ascii=False))
