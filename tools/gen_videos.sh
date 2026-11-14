#!/bin/bash
cd /e/sucai/ikiteita
for v in v2_phones v3_office v4_roof v5_red; do PYTHONUTF8=1 python -P tools/gen_ali.py video $v gen/video/$v.txt 2>&1 | tail -1 & sleep 3; done
PYTHONUTF8=1 python -P - <<'P'
import json,os,time,urllib.request
K=os.environ['QWEN_API_KEY']; tid=[x for x in json.load(open('gen/ledger.json',encoding='utf-8')) if x['id']=='v1_news'][0]['note']
if not os.path.exists('gen/video/v1_news.mp4'):
    for i in range(240):
        o=json.loads(urllib.request.urlopen(urllib.request.Request('https://token-plan.maas.qianwenaiapi.com/api/v1/tasks/'+tid,headers={'Authorization':'Bearer '+K}),timeout=60).read().decode()); s=o['output'].get('task_status')
        if s in('SUCCEEDED','FAILED','CANCELED','UNKNOWN'): break
        time.sleep(10)
    if s=='SUCCEEDED': urllib.request.urlretrieve(o['output'].get('video_url') or o['output']['results'][0]['url'],'gen/video/v1_news.mp4'); print('ok v1_news')
    else: print('FAILED v1',json.dumps(o,ensure_ascii=False)[:300])
P
wait; ls -la gen/video/*.mp4; echo videos-done
