#!/usr/bin/env python
"""迁移素材到 assets/ 后同步引用：更新所有 .tscn/.cs 中的 res://scenes/... 资产路径为 assets/"""
import glob
import os
import re

# 收集映射：资产文件名 -> assets 新路径
rules = []
for p in glob.glob('assets/texture/**/*.png', recursive=True):
    rules.append((os.path.basename(p), 'res://' + p.replace(os.sep, '/')))
for p in glob.glob('assets/sound/*.sfxr'):
    rules.append((os.path.basename(p), 'res://' + p.replace(os.sep, '/')))
for p in glob.glob('assets/shader/*.gdshader'):
    rules.append((os.path.basename(p), 'res://' + p.replace(os.sep, '/')))
for p in glob.glob('assets/fonts/*.ttf'):
    rules.append((os.path.basename(p), 'res://' + p.replace(os.sep, '/')))

print(f'规则数: {len(rules)}')

targets = []
for pat in ['scenes/**/*.tscn', 'global/**/*.tscn', 'scripts/**/*.cs']:
    targets.extend(glob.glob(pat, recursive=True))

changed = 0
for t in targets:
    s = open(t, encoding='utf-8').read()
    orig = s
    for name, newpath in rules:
        # 替换 res://scenes/.../name 引用为新路径
        s = re.sub(r'res://scenes/[^"\s)]*' + re.escape(name), newpath, s)
    if s != orig:
        open(t, 'w', encoding='utf-8', newline='\n').write(s)
        changed += 1
        print('updated:', t)

print(f'共更新 {changed} 个文件')
