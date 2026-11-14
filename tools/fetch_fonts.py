# -*- coding: utf-8 -*-
"""Download the open fonts the captions, title and covers use (all SIL Open Font License 1.1) from the Google Fonts repository into fonts/.
    python -P tools/fetch_fonts.py
The Windows fonts the finish also uses (Microsoft YaHei, Segoe UI) come with Windows and are not downloaded."""
import os, sys, urllib.request
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); DST = os.path.join(ROOT, 'fonts'); os.makedirs(DST, exist_ok=True)
BASE = 'https://raw.githubusercontent.com/google/fonts/main/ofl/'
FILES = {
    'DelaGothicOne-Regular.ttf': 'delagothicone/DelaGothicOne-Regular.ttf',
    'ShipporiMinchoB1-ExtraBold.ttf': 'shipporiminchob1/ShipporiMinchoB1-ExtraBold.ttf',
    'ShipporiMinchoB1-Bold.ttf': 'shipporiminchob1/ShipporiMinchoB1-Bold.ttf',
    'ZenOldMincho-Bold.ttf': 'zenoldmincho/ZenOldMincho-Bold.ttf',
    'ZenOldMincho-Black.ttf': 'zenoldmincho/ZenOldMincho-Black.ttf',
    'ZenKakuGothicNew-Medium.ttf': 'zenkakugothicnew/ZenKakuGothicNew-Medium.ttf',
    'KleeOne-SemiBold.ttf': 'kleeone/KleeOne-SemiBold.ttf',
    'NotoSerifSC-VF.ttf': 'notoserifsc/NotoSerifSC%5Bwght%5D.ttf',
    'NotoSansSC-VF.ttf': 'notosanssc/NotoSansSC%5Bwght%5D.ttf',
    'OFL_shipporiminchob1.txt': 'shipporiminchob1/OFL.txt', 'OFL_zenoldmincho.txt': 'zenoldmincho/OFL.txt', 'OFL_zenkakugothicnew.txt': 'zenkakugothicnew/OFL.txt',
    'OFL_delagothicone.txt': 'delagothicone/OFL.txt', 'OFL_kleeone.txt': 'kleeone/OFL.txt', 'OFL_notoserifsc.txt': 'notoserifsc/OFL.txt', 'OFL_notosanssc.txt': 'notosanssc/OFL.txt',
}
bad = 0
for name, path in FILES.items():
    out = os.path.join(DST, name)
    if os.path.exists(out) and os.path.getsize(out) > 1000: print('have', name); continue
    try:
        urllib.request.urlretrieve(BASE + path, out); print('got ', name, os.path.getsize(out) // 1024, 'KB')
    except Exception as e:
        bad += 1; print('FAILED', name, e)
print('fonts in', DST, '- failed:', bad); sys.exit(1 if bad else 0)
