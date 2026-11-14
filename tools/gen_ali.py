# -*- coding: utf-8 -*-
"""Aliyun Token Plan generation (key in QWEN_API_KEY, never printed).
    python -P tools/gen_ali.py video <id> <prompt file>          submit, wait, download gen/video/<id>.mp4
    python -P tools/gen_ali.py image <id> <prompt file> [size]   gen/image/<id>.png
Every accepted call is written to gen/ledger.json (the user's budget: 5 videos, 15 images)."""
import sys, os, json, time, urllib.request, urllib.error
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); HOST = 'https://token-plan.maas.qianwenaiapi.com'; KEY = os.environ['QWEN_API_KEY']
LED = os.path.join(ROOT, 'gen', 'ledger.json')


def call(path, body=None, extra=None):
    h = {'Authorization': 'Bearer ' + KEY, 'Content-Type': 'application/json'}; h.update(extra or {})
    req = urllib.request.Request(HOST + path, data=json.dumps(body).encode() if body is not None else None, headers=h, method='POST' if body is not None else 'GET')
    try:
        with urllib.request.urlopen(req, timeout=180) as r: return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode() or '{}')


def ledger(kind, id_, note):
    d = json.load(open(LED, encoding='utf-8')) if os.path.exists(LED) else []
    d.append({'kind': kind, 'id': id_, 'note': note, 'time': time.strftime('%H:%M:%S')}); json.dump(d, open(LED, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)


kind, id_, pf = sys.argv[1], sys.argv[2], sys.argv[3]; prompt = open(pf, encoding='utf-8').read().strip()
if kind == 'video':
    body = {'model': 'wan3.0-video', 'input': {'prompt': prompt}, 'parameters': json.loads(os.environ.get('VPARAM', '{"resolution": "720P", "ratio": "4:3", "duration": 5, "prompt_extend": false, "watermark": false}'))}
    st, o = call('/api/v1/services/aigc/video-generation/video-synthesis', body, {'X-DashScope-Async': 'enable'})
    tid = (o.get('output') or {}).get('task_id')
    if not tid: print('REFUSED', st, json.dumps(o, ensure_ascii=False)[:400]); sys.exit(2)
    ledger('video', id_, tid); print('task', id_, 'accepted')
    for i in range(240):
        time.sleep(10); st, o = call('/api/v1/tasks/' + tid); s = (o.get('output') or {}).get('task_status')
        if s in ('SUCCEEDED', 'FAILED', 'CANCELED', 'UNKNOWN'): break
    if s != 'SUCCEEDED': print('FAILED', id_, json.dumps(o, ensure_ascii=False)[:400]); sys.exit(3)
    url = o['output'].get('video_url') or (o['output'].get('results') or [{}])[0].get('url'); urllib.request.urlretrieve(url, os.path.join(ROOT, 'gen', 'video', id_ + '.mp4')); print('ok', id_)
else:
    size = sys.argv[4] if len(sys.argv) > 4 else '1280*1280'
    body = {'model': os.environ.get('IMODEL', 'wan2.7-image'), 'input': {'messages': [{'role': 'user', 'content': [{'text': prompt}]}]}, 'parameters': {'size': size, 'n': 1, 'watermark': False}}
    st, o = call('/api/v1/services/aigc/multimodal-generation/generation', body)
    url = None
    try:
        for c in o['output']['choices'][0]['message']['content']:
            if 'image' in c: url = c['image']
    except Exception: pass
    if not url: print('REFUSED', id_, st, json.dumps(o, ensure_ascii=False)[:400]); sys.exit(2)
    ledger('image', id_, size); urllib.request.urlretrieve(url, os.path.join(ROOT, 'gen', 'image', id_ + '.png')); print('ok', id_)
