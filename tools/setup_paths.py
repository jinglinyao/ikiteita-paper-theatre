# -*- coding: utf-8 -*-
"""Point the scripts at this clone. The scripts were written on the author's machine with the project at E:/sucai/ikiteita;
this rewrites that path (and its Git Bash form /e/sucai/ikiteita) to wherever this repository now is.
    python -P tools/setup_paths.py            rewrite, and list what changed
    python -P tools/setup_paths.py --check    only list the files that still name another location
Run it again after moving the clone: it remembers the path it last wrote in tools/.project_root."""
import os, sys, re
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))).replace('\\', '/')
STAMP = os.path.join(ROOT, 'tools', '.project_root')
old = open(STAMP, encoding='utf-8').read().strip() if os.path.exists(STAMP) else 'E:/sucai/ikiteita'


def bash_form(p):
    """E:/a/b -> /e/a/b (Git Bash)"""
    m = re.match(r'^([A-Za-z]):/(.*)$', p); return f'/{m.group(1).lower()}/{m.group(2)}' if m else p


pairs = [(old, ROOT), (bash_form(old), bash_form(ROOT)), (old.replace('/', '\\\\'), ROOT.replace('/', '\\\\'))]
check = '--check' in sys.argv; changed = []
for d in ('tools', os.path.join('unity', 'AgentScripts')):
    for dp, _, fs in os.walk(os.path.join(ROOT, d)):
        for f in fs:
            if not f.endswith(('.sh', '.py', '.cs', '.mjs', '.cjs', '.ps1')) or f == 'setup_paths.py': continue
            p = os.path.join(dp, f); s = open(p, encoding='utf-8', errors='surrogateescape').read(); t = s
            for a, b in pairs:
                if a != b: t = t.replace(a, b)
            if t != s:
                changed.append(os.path.relpath(p, ROOT))
                if not check: open(p, 'w', encoding='utf-8', errors='surrogateescape', newline='').write(t)
if not check: open(STAMP, 'w', encoding='utf-8').write(ROOT)
print(('would change' if check else 'changed'), len(changed), 'files:', ', '.join(changed) if changed else '-')
print('project root:', ROOT, '| unity project:', ROOT + '/unity')
