#!/usr/bin/env python
"""把 game_juice_breakout_4 的 .tscn 迁移为 BreakOut 可用布局：
1. 收集目标项目内所有资源 path -> uid 映射
2. 重写场景 ext_resource 的 uid 引用
3. 剥离 GDScript 玩法脚本引用（由 C# 挂载），保留 addon 脚本
用法: python tools/remap_tscn.py <tscn...>
"""
import glob
import os
import re
import sys

ROOT = os.getcwd()

# ---------- 1. 建立 path -> uid 索引 ----------
def collect_uids():
    """返回 {res_path: uid} 从 .import 与 .uid 文件中收集"""
    index = {}
    # .import 文件: source_file -> uid
    for imp in glob.glob(os.path.join(ROOT, "**", "*.import"), recursive=True):
        try:
            text = open(imp, encoding="utf-8").read()
        except OSError:
            continue
        m = re.search(r'^source_file="(res://[^"]+)"', text, re.M)
        uid_m = re.search(r'^uid="(uid://[^"]+)"', text, re.M)
        if m and uid_m:
            index[m.group(1)] = uid_m.group(1)
    # .uid 文件: 与同名文件配对（gdshader/gd/ttf 等无 .import 的）
    for uidf in glob.glob(os.path.join(ROOT, "**", "*.uid"), recursive=True):
        if ".godot" in uidf or "addons" in uidf:
            continue
        base = uidf[:-4]  # 去掉 .uid
        rel = os.path.relpath(base, ROOT).replace("\\", "/")
        res = "res://" + rel
        try:
            uid = open(uidf, encoding="utf-8").read().strip()
        except OSError:
            continue
        if uid.startswith("uid://"):
            index[res] = uid
    return index

INDEX = collect_uids()

# 打印已建索引数量做诊断
print(f"[index] 收集 {len(INDEX)} 个资源 uid 映射")

def resolve_uid(res_path):
    """给定 res:// 路径返回目标项目 uid（不存在返回 None）"""
    return INDEX.get(res_path)

# ---------- 2. 处理单个场景 ----------
GAMEPLAY_GD = {
    "res://scenes/ball/scripts/ball.gd",
    "res://scenes/paddle/scripts/paddle.gd",
    "res://scenes/brick/scripts/brick.gd",
    "res://scenes/game/scripts/game.gd",
    "res://scenes/game/scripts/camera_final.gd",
    "res://scenes/game/scripts/camera.gd",
    "res://scenes/game/scripts/pattern.gd",
    "res://scenes/ball/scripts/bounce_particles.gd",
    "res://scenes/ball/scripts/bump_particles.gd",
    "res://scenes/ball/scripts/ball_explode_particles.gd",
    "res://scenes/brick/scripts/brick_explode_particles.gd",
    "res://scenes/brick/scripts/bomb_explode_particles.gd",
    "res://scenes/game/scripts/lava_splash_particles.gd",
    "res://scenes/game/scripts/game_over.gd",
    "res://scenes/ui/stage_clear/stage_clear.gd",
    "res://scenes/paddle/scripts/ghost.gd",
    "res://scenes/paddle/scripts/ghost_spawner.gd",
    "res://scenes/paddle/scripts/laser.gd",
    "res://scenes/ui/energy_bar/scripts/energy_bar.gd",
    "res://scenes/ui/game_over/scripts/game_over.gd",
    "res://scenes/ui/stage_clear/scripts/stage_clear.gd",
    "res://scenes/ui/ultimate/scripts/ultimate_ready.gd",

    "res://scenes/ui/health/scripts/health.gd",
    "res://scenes/ui/score/scripts/score.gd",
    "res://scenes/ui/ultimate/scripts/ultimate_ready.gd",
}

def strip_script_refs(text):
    """移除玩法 GDScript 的 ext_resource 声明及 script = ExtResource 赋值"""
    lines = text.splitlines()
    removed_ids = set()
    out = []
    for line in lines:
        m = re.search(r'\[ext_resource[^\]]*path="(res://[^"]+\.gd)"', line)
        if m:
            p = m.group(1)
            if p in GAMEPLAY_GD:
                removed_ids.add(re.search(r'id="([^"]+)"', line).group(1))
                continue
        out.append(line)
    # 删除 script = ExtResource("id") 赋值行
    final = []
    for line in out:
        sm = re.search(r'^\s*script = ExtResource\("([^"]+)"\)', line)
        if sm and sm.group(1) in removed_ids:
            continue
        final.append(line)
    return "\n".join(final)

def remap_scene(path):
    text = open(path, encoding="utf-8").read()
    text = strip_script_refs(text)

    def repl(m):
        full = m.group(0)
        uid_m = re.search(r'uid="(uid://[^"]+)"', full)
        path_m = re.search(r'path="(res://[^"]+)"', full)
        if not path_m:
            return full
        res = path_m.group(1)
        new_uid = resolve_uid(res)
        if new_uid is None:
            # 子场景/未导入资源：检查是否指目标项目存在的 tscn
            return full
        if uid_m and uid_m.group(1) != new_uid:
            full = full.replace(uid_m.group(1), new_uid)
        return full

    text = re.sub(r'\[ext_resource[^\]]*\]', repl, text)
    return text

for tscn in sys.argv[1:]:
    new_text = remap_scene(tscn)
    open(tscn, "w", encoding="utf-8", newline="\n").write(new_text)
    print(f"[ok] {tscn}")
