#!/usr/bin/env python
"""同步实体脚本 uid 到所有场景引用（git mv/partial 拆分后 uid 失效修复）"""
import glob
import os
import re

script_uid = {}
for uidf in glob.glob('scripts/entities/**/*.cs.uid', recursive=True):
    base = uidf[:-4]  # 去 .uid
    name = os.path.basename(base)
    # 跳过 partial 文件（含 .Dependencies 等点号，只有主文件有独立 uid）
    if name.count('.') > 1:
        continue
    rel = os.path.relpath(base, '.').replace(os.sep, '/')
    res = 'res://' + rel
    uid = open(uidf, encoding='utf-8').read().strip()
    script_uid[res] = uid

print('脚本 uid 映射:')
for k, v in script_uid.items():
    print(' ', k, '->', v)

count = 0
targets = glob.glob('scenes/**/*.tscn', recursive=True) + glob.glob('global/**/*.tscn', recursive=True)
for p in targets:
    s = open(p, encoding='utf-8').read()
    orig = s
    for res, uid in script_uid.items():
        s = re.sub(rf'(uid=")uid://[^"]*(" path="{re.escape(res)}")', rf'\g<1>{uid}\g<2>', s)
    if s != orig:
        open(p, 'w', encoding='utf-8', newline='\n').write(s)
        count += 1
        print('updated', p)
print(f'共更新 {count} 场景')
