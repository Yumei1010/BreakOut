#!/usr/bin/env python
"""把 game_orig.tscn 转换为挂 GameRoot.cs 的正式主场景 game.tscn：
- 子场景 path: paddle/ball/brick.tscn -> *_layout.tscn（uid 已跨项目稳定）
- UI 场景保留原路径（energy_bar/health/score 已拷入）
- 剥离 game.gd/pattern.gd/camera_final.gd 玩法脚本
- 根节点挂 GameRoot.cs
"""
import re
import shutil

SRC = 'scenes/game/game_orig.tscn'
DST = 'scenes/game/game.tscn'
GAMEROOT_UID = 'uid://dehhmutvtlldg'

s = open(SRC, encoding='utf-8').read()

# 1) 头加 GameRoot script 引用
head = re.search(r'^\[gd_scene[^\]]*\]', s, re.M).group(0)
new_head = head + '\n\n[ext_resource type="Script" uid="%s" path="res://scripts/presentation/game/GameRoot.cs" id="gameroot"]' % GAMEROOT_UID
s = s.replace(head, new_head, 1)

# 2) 子场景 path 改 *_layout
s = re.sub(r'(path="res://scenes/paddle/paddle)\.tscn"', r'\1_layout.tscn"', s)
s = re.sub(r'(path="res://scenes/ball/ball)\.tscn"', r'\1_layout.tscn"', s)
s = re.sub(r'(path="res://scenes/brick/brick)\.tscn"', r'\1_layout.tscn"', s)

# 3) 剥离 gd 脚本 ext_resource 行
GDS = ['game.gd', 'pattern.gd', 'camera_final.gd', 'camera.gd']
removed = set()
out = []
for line in s.splitlines():
    m = re.search(r'\[ext_resource[^\]]*path="res://scenes/game/scripts/([^"]+\.gd)"', line)
    if m and m.group(1) in GDS:
        removed.add(re.search(r'id="([^"]+)"', line).group(1))
        continue
    out.append(line)
s = '\n'.join(out)
# 删 script 赋值行
s = '\n'.join(l for l in s.splitlines() if not (re.match(r'^\s*script = ExtResource\("([^"]+)"\)', l) and re.match(r'^\s*script = ExtResource\("([^"]+)"\)', l).group(1) in removed))

# 4) 根节点挂 GameRoot（原 Game 节点挂 game.gd，改为 gameroot）
s = s.replace('[node name="Game" type="Node2D"]', '[node name="Game" type="Node2D"]\nscript = ExtResource("gameroot")', 1)

open(DST, 'w', encoding='utf-8', newline='\n').write(s)
print('generated', DST)
print('removed gd ids:', removed)
