# BreakOut 架构方案（重写蓝图）

> 状态：方案定稿（2026-09）| 来源：[game_juice_breakout_4](https://github.com/GeWuYou/game_juice_breakout_4)（GDScript juice 教学，1478 行/22 脚本）
> 目标：基于模板仓库的**架构规范化示范项目**——domain 纯 C# 可单测 + presentation Godot 薄壳 + 内容数据驱动 + 新增砖种/能力演示拓展

## 设计原则（区别于 Twenty-four 的教训）

1. **domain 承载全部规则**：bump 判定/分数/砖种/关卡生成/反弹数学 = 纯 C# 可单测
2. **presentation 只做表现**：实体薄壳（碰撞转发+动画/粒子）+ juice 直写
3. **CQRS 事件仅在规则级状态变化**：球死/砖毁/bump 结果/道具/过关——不硬套每帧物理（每帧物理是表现层即时反馈，事件化=空转发）
4. **内容数据驱动**：砖种/板能力注册表化——加玩法不改核心代码，用新增功能验证可拓展性

## 目录结构

```
scripts/
├── domain/                          # 纯 C# 游戏规则内核（零 Godot 依赖，可单测）
│   ├── brick/                       # 砖种规则
│   │   ├── BrickType.cs             #   砖种类型枚举（数据键）
│   │   ├── BrickSpec.cs             #   砖种规格：血量/爆炸半径/权重/可连锁/分值系数
│   │   ├── BrickCatalog.cs          #   砖种注册表（Type→Spec 映射，含默认集）
│   │   └── BrickLogic.cs            #   受击判定/爆炸连锁/能量砖结算（纯逻辑）
│   ├── bump/                        # 击球时机判定
│   │   ├── BumpGrade.cs             #   Perfect/Late/Early/TooFar 枚举
│   │   └── BumpJudge.cs             #   距离+帧距 → 判定 + 增益系数（原版写在 ball.gd）
│   ├── scoring/                     # 分数规则
│   │   ├── ScoreRule.cs             #   连击 × 系数计分（原版散在 game.gd）
│   │   └── StageResult.cs           #   结算公式（原版在 stage_clear.gd 内联）
│   ├── level/                       # 关卡生成
│   │   ├── LevelLayout.cs           #   砖布局位置（spawn 点位数据）
│   │   ├── LevelGenerator.cs        #   概率表→砖种布局（原版 layout_bricks 内联）
│   │   └── LevelConfig.cs           #   关卡参数（概率/能量砖比例/节奏）
│   ├── ball/                        # 球物理（纯向量数学）
│   │   └── BallMotion.cs            #   速度衰减/限长/反弹角/吸收板速度（可单测）
│   ├── ability/                     # 板能力规则（新增拓展点）
│   │   ├── AbilityType.cs           #   Laser/Dash/Attract/Magnet 等枚举
│   │   ├── AbilitySpec.cs           #   能力规格：能量消耗/持续/强度
│   │   └── AbilityCatalog.cs        #   能力注册表
│   └── run/                         # 对局状态
│       ├── RunState.cs              #   生命/能量/分数/当前关/是否开始（纯状态机）
│       └── EnergyLogic.cs           #   能量增减钳制/满能量事件
│
├── cqrs/                            # CQRS（按域，命令/事件/查询）
│   ├── run/event/                   #   RunStartedEvent/PlayerDiedEvent/EnergyFullEvent/LevelClearedEvent/GameOverEvent
│   ├── scoring/event/               #   ScoreChangedEvent/ComboChangedEvent
│   ├── brick/event/                 #   BrickDestroyedEvent（含位置/类型，触发表现）
│   ├── bump/event/                  #   BumpJudgedEvent（Perfect 触发粒子+音效）
│   └── ability/command/             #   TriggerAbilityCommand（输入→能力）
│
├── presentation/                    # Godot 表现层
│   ├── game/GameRoot.cs             #   对局根：组装场景/订阅事件→驱动 domain
│   ├── paddle/PaddleView.cs         #   薄壳：输入→命令，碰撞→转发
│   ├── ball/BallView.cs             #   薄壳：move_and_collide + 表现
│   ├── brick/BrickView.cs           #   薄壳：视觉/动画，受击→domain
│   ├── effect/                      #   juice 直写（不 CQRS）
│   │   ├── CameraShake.cs           #   屏幕震动（原版 camera_final.gd 数学）
│   │   ├── HitStop.cs               #   定帧暂停（原版散在 ball/paddle）
│   │   ├── PatternPulse.cs          #   全屏脉冲 shader 驱动
│   │   └── FloatingText.cs          #   bump 判定飘字
│   └── ui/                          #   HUD/结算/连击（事件驱动刷新）
│       ├── HudView.cs               #   生命/能量条/分数（订阅事件）
│       ├── ComboView.cs             #   连击显示
│       └── StageClearView.cs        #   结算（StageResult 展示）
│
├── core/                            # （模板原有，保留）框架核心
├── framework/                       # （模板原有，保留）GFramework 扩展
├── module/                          # （模板原有，保留）DI 装配——新增 RunModule 注册 domain 单例
├── component/                       # （模板原有，保留）通用组件
├── enums/constants/                 # （模板原有，保留）
└── data/                            # （模板原有，保留）
```

## 原版 → 新架构迁移对照表

| 原版文件 | 迁移去向 | 迁移方式 |
|---|---|---|
| `ball.gd`（291 行） | bump 判定 → `domain/bump/BumpJudge.cs`；反弹数学 → `domain/ball/BallMotion.cs`；粒子/动画/飘字 → `presentation/ball/BallView.cs` | 拆 3 份 |
| `brick.gd`（193 行） | 类型/血量/连锁 → `domain/brick/*`；视觉/动画 → `presentation/brick/BrickView.cs` | 拆 2 份 |
| `paddle.gd`（137 行） | 能力开关 → `domain/ability/*`；弹簧振荡/hitstop → `presentation/paddle/PaddleView.cs` + effect | 拆 2 份 |
| `game.gd`（288 行 上帝脚本） | 关卡生成 → `domain/level/*`；分数/能量/生命 → `domain/run/*`；相机序列/UI 弹层 → `presentation/game/GameRoot.cs` | 最大拆分 |
| `camera_final.gd` | `presentation/effect/CameraShake.cs` | 数学直译 |
| `stage_clear.gd` | 结算公式 → `domain/scoring/StageResult.cs`；动画 → `presentation/ui/StageClearView.cs` | 拆 2 份 |
| `globals.gd` | 统计 → `domain/run/RunState.cs`；BUMP 枚举 → `domain/bump/BumpGrade.cs` | 消除全局单例 |
| `energy_bar/health/score.gd` | `presentation/ui/HudView.cs` | 合并 + 事件驱动 |
| `pattern.gd`/`bump_timing.gd`/`ghost.gd` | `presentation/effect/*` | 直译保留 |

## 新增拓展示范（验证可拓展性）

1. **新砖种 `MetalBrick`**（需 2 次+特殊能力才能破）——验证 BrickCatalog 加配置不改逻辑
2. **新砖种 `RainbowBrick`**（每次受击变型，连锁后高分）——验证类型驱动的表现分派
3. **新能力 `Magnet`**（按住吸引弹道修正，替代 Attract 的能量版）——验证 AbilityCatalog 注册

## 测试清单（tests/BreakOut.Tests/）

- `BumpJudgeTests`：距离/帧距边界 → Perfect/Late/Early/TooFar + 增益系数
- `BrickLogicTests`：受击扣血/爆炸连锁半径/能量砖结算/MetalBrick 特殊规则
- `ScoreRuleTests`：连击累加/砖毁分×combo/结算公式（对照 stage_clear 原公式）
- `BallMotionTests`：速度衰减收敛/限长/板速吸收/最大反弹角
- `EnergyLogicTests`：增减钳制/满 100 触发事件
- `LevelGeneratorTests`：概率分布/可解性前提（全部砖种生成合法）

## 执行阶段

| 阶段 | 内容 | 产出 |
|---|---|---|
| 0 ✅ | 基于模板建仓库 + 改名 + CI | BreakOut 仓库（d75ead1） |
| 1 | 本方案文档 + 资产迁移（原作 png/ogg/shader/addon→新仓库 assets/） | 可运行空壳 |
| 2 | domain 实现 + 单测全绿 | 规则内核 |
| 3 | presentation 薄壳 + 事件桥（可玩 1:1） | 玩法复刻 |
| 4 | 新增砖种/能力 + 数据驱动验证 | 拓展示范 |
| 5 | juice 还原 + 手感对照 | 完成 |
| 6 | 模式沉淀回模板（docs/guides/breakout-architecture.md） | 双向互补 |

## 验收

- [ ] domain 零 Godot 引用，全部可单测
- [ ] 原版玩法 1:1（手感参数逐项对照）
- [ ] 新增砖种/能力**不加核心代码**（仅配置+视图分派）
- [ ] CQRS 事件只出现在规则级变化，无每帧事件
- [ ] 全绿测试 + Godot headless 可启动
