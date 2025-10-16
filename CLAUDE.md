# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 提供在此仓库中工作的指导。

## 项目概述 (Project Overview)

Dead Cells Test Framework - 一个受《死亡细胞》启发的 Unity 2D 动作平台游戏框架，同时支持传统代码驱动和基于 CastleDB 和 LDtk 的数据驱动开发方式。

**Unity 版本**: 2022.3.14f1 LTS (已锁定)

## 架构 (Architecture)

### 模块组织 (Module Organization)

项目采用分层的程序集定义架构，具有清晰的依赖层次结构：

```
DeadCells.Data (底层 - 无依赖)
    ↓
DeadCells.Core (核心系统 - 依赖 TextMeshPro, Newtonsoft.Json)
    ↓
DeadCells.Player (玩家系统 - 依赖 Core)
DeadCells.Combat (战斗系统 - 依赖 Core, Data)
DeadCells.Rooms (房间系统 - 依赖 Core, Player, Combat, Data, LDtkUnity.Runtime)
    ↓
DeadCells.Game (游戏层 - 依赖所有模块)
```

**核心原则**：底层模块不能依赖上层模块。添加新代码时始终遵守此依赖流向。

### 核心系统 (Core Systems)

#### 数据驱动架构 (Data-Driven Architecture) - CastleDB 集成

- **CastleDBManager** (`DeadCells.Core`)：单例管理器，从 CastleDB 加载 JSON 数据并提供原始 JSON 访问
  - 启动时将数据加载到分类字典中（weapons, enemies, items, rooms）
  - 使用 `GetRawJsonData(category, id)` 获取 JSON 字符串
  - 使用 `DeserializeData<T>()` 进行类型安全的反序列化
  - 访问数据前始终检查 `IsDataLoaded`

- **Data Classes** (`DeadCells.Data`)：纯数据容器（WeaponData, EnemyData, ItemData, RoomData）
  - 不依赖 Unity
  - 使用 `[Serializable]` 特性用于 JSON 反序列化
  - WeaponData 包含嵌套的 WeaponStats，具有向后兼容的属性别名

#### 武器配置系统 (Weapon Configuration System)

**重要**：本项目使用基于类型安全接口的武器配置系统，而非 Unity 的 SendMessage。

- **IWeaponConfigurable**：所有武器组件必须实现的基础接口
  - `ConfigureWeapon(WeaponData)`：配置武器基础属性
  - `SupportsWeaponType(string)`：检查组件是否支持某种武器类型

- **专用接口 (Specialized Interfaces)**：
  - `IMeleeWeaponConfigurable`：近战武器（攻击范围、击退）
  - `IRangedWeaponConfigurable`：远程武器（弹药、穿透）

- **WeaponConfigurationManager**：编排武器设置的静态类
  - 调用 `ConfigureWeapon(GameObject, WeaponData)` 配置所有武器组件
  - 返回 `List<WeaponConfigurationResult>` 用于错误跟踪
  - 使用 `GetConfigurationSummary()` 调试配置问题

**添加新武器组件时**：
1. 实现适当的接口（IWeaponConfigurable 或专用变体）
2. 添加到武器预制体 GameObject
3. 配置将通过 WeaponConfigurationManager 自动完成

#### 玩家状态机 (Player State Machine)

- **PlayerStateMachine** 管理玩家状态转换
- 状态 (States)：Idle, Move, Jump, Fall, Attack, Roll
- 每个状态实现：Enter(), Update(), FixedUpdate(), Exit()
- 使用 `ChangeState(newState)` 进行转换，永远不要直接设置状态

#### 战斗系统 (Combat System)

- **DamageInfo**：包含伤害数值、类型、暴击状态、击退的结构体
- **Health**：可受伤实体的组件
- **HitstunController**：管理受击反应和无敌帧
- 武器继承自基类 `Weapon`，并特化为 `MeleeWeapon` 或 `RangedWeapon`

#### 视觉效果 (Visual Effects)

- **EffectsManager** (`DeadCells.Core`)：集中式效果控制器
  - 屏幕震动 (Screen shake)
  - 屏幕闪烁 (Screen flash)
  - 粒子效果 (Particle effects)
  - 通过单例模式访问

### 关卡设计集成 (Level Design Integration)

- **LDtk**：通过 LDtk for Unity 包 (v6.11.2) 集成关卡编辑器
- 关卡文件：`Assets/Data/LDtkLevel/*.ldtk`
- **重要**：首次克隆后，每个开发者必须：
  1. 在 Unity 中选择 `.ldtk` 文件
  2. 在 LDtk Importer inspector 中点击 "Install / Auto-add command"
  3. 这将配置平台特定的导出命令

## 开发命令 (Development Commands)

### Unity Editor 操作

这是一个 Unity 项目 - 大部分开发在 Unity Editor 中进行：
- 在 Unity Hub 中打开项目（需要 2022.3.14f1 LTS）
- 构建：File → Build Settings → Build
- 播放模式：Unity Editor 播放按钮
- 测试：Window → General → Test Runner

### LDtk 关卡编辑

1. 直接在 LDtk 应用程序 (v1.5.3+) 中编辑 `.ldtk` 文件
2. 在 LDtk 中保存 - Unity 会自动重新生成 `.ldtkt` 文件
3. `.ldtk` 和 `.ldtkt` 文件都必须提交到版本控制

### LDtk 导出故障排除

如果自动导出失败：
```bash
# Windows：确保 Git Bash 可用
bash --version

# Unix/macOS：授予脚本执行权限
chmod +x Library/LDtkTilesetExporter/export_tileset_universal.sh
```

然后在 Unity Inspector 中重新运行 "Install / Auto-add command"。

### 包版本问题

如果发生包冲突：
```bash
# 强制重新导入所有包
rm -rf Library/
# 重新打开 Unity 项目

# 重置锁定的包版本
git checkout -- Packages/packages-lock.json
```

## 关键文件 (Key Files)

- `Assets/Scripts/DeadCells.*/`：按模块组织的源代码
- `Assets/Data/LDtkLevel/`：关卡数据文件
- `Packages/packages-lock.json`：锁定的包版本（请勿修改）
- `ProjectSettings/ProjectVersion.txt`：Unity 版本锁定

## 测试 (Testing)

- 测试文件使用后缀 `*Test.cs`（例如 `WeaponConfigurationTest.cs`）
- 与实现文件放在一起
- 使用 Unity Test Framework (Window → General → Test Runner)

## 重要注意事项 (Important Notes)

### 使用武器系统时 (When Working with Weapons)

1. 永远不要使用 `SendMessage()` 进行武器配置 - 使用 WeaponConfigurationManager
2. 始终在武器组件上实现 IWeaponConfigurable
3. 检查 WeaponConfigurationResult 获取配置错误
4. WeaponStats 使用大写属性名（Damage, AttackSpeed, Range），带有小写别名以保持向后兼容

### 使用数据系统时 (When Working with Data)

1. 数据加载在启动时通过 CastleDBManager 进行
2. 访问数据前始终检查 `CastleDBManager.Instance.IsDataLoaded`
3. DeadCells.Data 中的数据类应保持纯净（无 Unity 依赖）
4. 使用 WeaponData.stats 获取武器统计数据，而非直接属性

### 添加新系统时 (When Adding New Systems)

1. 遵守程序集依赖层次结构（Data → Core → Specialized → Game）
2. 仅在必要时在 `.asmdef` 文件中添加程序集引用
3. 优先使用依赖注入而非单例模式（管理器除外）
4. 使用与程序集名称匹配的命名空间（例如 `namespace DeadCells.Combat`）

### 版本控制 (Version Control)

- 提交 `.ldtk` 和生成的 `.ldtkt` 文件
- 永远不要手动修改 `packages-lock.json`
- Unity 版本已锁定 - 未经团队同意不要升级

## 玩家角色设置指南 (Player Character Setup Guide)

本节提供了在游戏中设置可玩角色的标准化工作流程。在将新角色精灵集成到玩家控制器系统时参考此指南。

### 前置条件 (Prerequisites)

- 角色精灵表（例如 `adventurer-idle-00.png`, `adventurer-run-00.png` 等）
- 精灵应在 Unity 中正确切片（Sprite Mode: Multiple）
- 项目使用 PlayerController 和状态机架构

### 阶段 1：基础 GameObject 配置

#### 1.1 GameObject 设置

**操作步骤**：

1. **打开场景并定位角色对象**
   - 在 Project 窗口打开 TestScene（或你的工作场景）
   - 在 Hierarchy 中找到已拖入的 adventurer 对象

2. **重命名为 Player**
   - 选中对象，按 `F2` 键
   - 输入 `Player`，按 Enter 确认

3. **检查 SpriteRenderer 组件**
   - 在 Inspector 中确认 Sprite Renderer 组件存在
   - **Sprite**: 应显示当前的角色精灵（如 `adventurer-idle-00`）
   - **Flip X/Y**: 都不勾选（确保角色面向右侧）

4. **配置渲染层级**
   - **Sorting Layer**: 选择 `Default` 或创建专用的 `Player` 层
   - **Order in Layer**: 设置为 `10`（确保在背景之上）

5. **设置初始位置**
   - Transform Position: `(0, 0, 0)` 或场景中合适的位置

#### 1.2 物理组件 (Physics Components)

**添加 Rigidbody2D**：

1. **添加组件**
   - 选中 Player 对象
   - Inspector 底部点击 `Add Component`
   - 搜索并添加 `Rigidbody2D`

2. **配置 Rigidbody2D 参数**

   | 参数 | 设置值 | 说明 |
   |---|---|---|
   | **Body Type** | Dynamic | 角色受物理影响（重力、碰撞） |
   | **Material** | None | 暂时不设置，后续可添加防滑材质 |
   | **Simulated** | ✅ 勾选 | 启用物理模拟 |
   | **Use Auto Mass** | ❌ 不勾选 | 使用固定质量 |
   | **Mass** | 1 | 默认质量 |
   | **Linear Drag** | 0 | 空中移动阻力（如需调整空中控制可设为 0.5-1） |
   | **Angular Drag** | 0.05 | 旋转阻力 |
   | **Gravity Scale** | 3-4 | **关键参数**：3=轻盈，5=沉重，建议从 3 开始 |
   | **Collision Detection** | Continuous | **必须**：防止高速穿透 |
   | **Sleeping Mode** | Start Awake | 启动时激活 |
   | **Interpolate** | Interpolate | 平滑移动 |

3. **配置 Constraints（非常重要）**
   - 展开 Constraints 折叠面板
   - **Freeze Position**: X 和 Y 都**不勾选**
   - **Freeze Rotation**: **必须勾选 Z**
   - ⚠️ 如果不勾选 Freeze Rotation Z，角色会在碰撞时旋转翻滚

**添加 CapsuleCollider2D（推荐）**：

1. **添加组件**
   - 选中 Player 对象
   - Inspector 底部 `Add Component`
   - 搜索并添加 `Capsule Collider 2D`

   **为什么选择 CapsuleCollider2D**：
   - 比 BoxCollider2D 更圆滑，不会卡在台阶边缘
   - 比 CircleCollider2D 更符合人形角色
   - 平台游戏角色的标准选择

2. **配置 CapsuleCollider2D**

   | 参数 | 设置值 | 说明 |
   |---|---|---|
   | **Direction** | Vertical | 垂直胶囊形状 |
   | **Size** | X: 0.5-0.7<br>Y: 1.0-1.2 | 根据角色大小调整，应紧密包裹身体 |
   | **Offset** | (0, 0) | 或微调 Y 值对齐中心 |
   | **Material** | None | 暂不设置 |
   | **Is Trigger** | ❌ 不勾选 | 需要物理碰撞 |

3. **可视化调整 Collider 大小**
   - 点击 `Edit Collider` 按钮或按 `E` 键
   - 在 Scene 视图中拖动绿色控制点
   - **目标**：胶囊应紧密包裹角色身体，底部对齐脚底
   - **注意**：不要太大（会卡通道），不要太小（不合理）

#### 1.3 地面检测设置 (Ground Detection Setup)

**为什么需要 GroundCheck**：
- PlayerController 使用 `Physics2D.OverlapCircle(groundCheck.position, radius, layerMask)` 检测地面
- 需要一个位于角色脚底的检测点

**创建 GroundCheck 子对象**：

1. **创建空子对象**
   - 在 Hierarchy 中**右键点击 Player 对象**
   - 选择 `Create Empty`
   - 自动在 Player 下创建子对象

2. **重命名为 GroundCheck**
   - 选中新创建的子对象
   - 按 `F2` 键，输入 `GroundCheck`

3. **定位 GroundCheck（关键步骤）**

   在 Inspector 的 Transform 中设置：

   | 参数 | 推荐值 | 说明 |
   |---|---|---|
   | **Position X** | 0 | 在角色水平中心 |
   | **Position Y** | -0.6 到 -0.7 | **关键**：在脚底略下方 |
   | **Position Z** | 0 | 保持 0 |

   **如何确定正确的 Y 值**：

   方法 1 - 可视化调整：
   - 在 Scene 视图中选中 GroundCheck
   - 按 `W` 键启用移动工具
   - 拖动到角色脚底略下方位置
   - 应该在 CapsuleCollider2D 底部边缘下方一点点

   方法 2 - 根据 Collider 计算：
   ```
   GroundCheck.Y = -(CapsuleCollider2D.Size.Y / 2) - 0.05
   例如：Size.Y = 1.2, Offset.Y = 0
   则：GroundCheck.Y = -(1.2/2) - 0.05 = -0.65
   ```

4. **验证设置**
   - 在 Scene 视图中选中 Player
   - 应该看到：
     - 角色的 sprite
     - 绿色的 CapsuleCollider2D 轮廓
     - GroundCheck 的小蓝色圆圈图标（在脚底位置）

**阶段 1 完成检查清单**：

```
✅ Player GameObject
   ├─ 已重命名为 "Player"
   ├─ Sprite Renderer 配置正确
   │  └─ Order in Layer = 10
   │
   ├─ Rigidbody2D
   │  ├─ Body Type: Dynamic
   │  ├─ Gravity Scale: 3-4
   │  ├─ Collision Detection: Continuous
   │  └─ Constraints: Freeze Rotation Z ✅
   │
   ├─ Capsule Collider 2D
   │  ├─ Direction: Vertical
   │  ├─ Size: 约 (0.6, 1.2)
   │  └─ Is Trigger: ❌
   │
   └─ GroundCheck (子对象) ✅
      └─ Position: (0, -0.6~-0.7, 0)
```

**可视化验证**：
- Scene 视图：能看到角色 sprite、绿色 collider 轮廓、GroundCheck 图标
- Game 视图：角色静态站立（如运行游戏会因重力下落）

**常见错误和解决**：
- ❌ 忘记 Freeze Rotation Z → 角色会旋转翻滚
- ❌ Collider 太大 → 角色会"飘"在地面上或卡在通道
- ❌ GroundCheck 位置太高 → 空中也会被判定为在地面
- ❌ 忘记创建 GroundCheck → 后续添加 PlayerController 时会报错

### 阶段 2：动画系统 (Animation System)

#### 2.1 创建 Animator Controller 资源

**在 Unity Editor 中的操作流程**：

1. **创建文件夹结构**
   - 在 Project 窗口右键 `Assets/` → Create → Folder
   - 命名为 `Animations`
   - 在 `Animations` 文件夹下创建 `Player` 文件夹
   - 在 `Player` 文件夹下创建角色专用文件夹（如 `MainCharacter`）
   - 最终路径：`Assets/Animations/Player/MainCharacter/`

2. **创建 Animator Controller**
   - 右键 `Adventurer` 文件夹
   - Create → Animator Controller
   - 命名为 `PlayerController`
   - 这会生成一个 `.controller` 文件（空的状态机资源）

**产出物**：
- `PlayerController.controller` - 空状态机，后续在阶段 2.4 配置

---

#### 2.2 创建动画剪辑 (Create Animation Clips)

这是最耗时的部分，需要从精灵表逐帧创建动画。以下是**完整的详细操作流程**。

##### **前置准备：切分精灵表**

1. **导入精灵表**
   - 将 `adventurer-Sheet.png` 拖入 Project 的 `Assets/Art/Character/` 文件夹

2. **配置精灵导入设置**
   - 选中 `adventurer-Sheet.png`
   - Inspector 中设置：
     - **Texture Type**: Sprite (2D and UI)
     - **Sprite Mode**: Multiple
     - **Pixels Per Unit**: 16（根据你的像素艺术分辨率调整）
     - **Filter Mode**: Point (no filter)（像素艺术风格）
     - **Compression**: None
   - 点击 Apply

3. **切分精灵（Sprite Editor）**
   - 点击 Inspector 中的 `Sprite Editor` 按钮
   - 在 Sprite Editor 窗口：
     - 点击左上角 `Slice` → `Automatic` 或 `Grid By Cell Size`
     - 如果是规则网格：输入每帧的像素尺寸（如 50x37）
     - 点击 `Slice` 按钮
   - 验证：每一帧动画都被正确框选
   - 点击右上角 `Apply` 保存

4. **验证切分结果**
   - 在 Project 窗口点击 `adventurer-Sheet.png` 左侧的展开箭头 ▶
   - 应该看到所有切好的精灵：
     - `adventurer-Sheet_0`, `adventurer-Sheet_1`, `adventurer-Sheet_2` ...
     - 或者如果手动命名：`adventurer-idle-00`, `adventurer-idle-01` ...

##### **创建动画剪辑：以 Idle 动画为例**

**方法：使用 Animation 窗口（推荐）**

1. **准备 Player GameObject**
   - 在 Hierarchy 中选中 `Player` GameObject
   - **必须确保 Player 上有 SpriteRenderer 组件**（阶段 1 已添加）

2. **打开 Animation 窗口**
   - 菜单栏：Window → Animation → Animation
   - 停靠到 Unity 窗口底部（与 Console 同层）

3. **创建新动画剪辑**
   - Animation 窗口会显示 "Create New Clip..." 按钮（如果 Player 还没有动画）
   - 点击 `Create`
   - 保存对话框：
     - 导航到 `Assets/Animations/Player/MainCharacter/`
     - 命名为 `Player_Idle.anim`
     - 点击保存

4. **添加精灵帧序列**
   - **重要**：在 Project 窗口展开 `adventurer-Sheet.png`
   - 找到 Idle 动画的精灵（假设命名为 `adventurer-idle-00` 到 `adventurer-idle-03`）
   - **全选这 4 个精灵**：
     - 按住 Ctrl（Windows）或 Cmd（Mac）多选
     - 或者点击第一个，按住 Shift 点击最后一个
   - **拖拽到 Animation 窗口的时间轴上**
   - Unity 会自动按顺序排列这些帧

5. **配置动画属性**

   **设置采样率（Sample Rate）**：
   - Animation 窗口左上角找到采样率数值（默认 60）
   - 点击数字，输入 `12` 或 `15`
   - 12 FPS = 复古像素风格，15 FPS = 更流畅

   **设置循环播放（Loop Time）**：
   - 选中 `Player_Idle.anim` 文件（在 Project 窗口）
   - Inspector 中勾选 ✅ `Loop Time`
   - 点击 Apply

6. **预览动画**
   - Animation 窗口点击播放按钮 ▶
   - 观察 Scene 视图中的 Player 是否流畅播放待机动画
   - 如果速度不对，调整 Sample Rate

##### **创建所有核心动画**

**重复上述流程创建以下动画**：

| 动画文件名 | 精灵范围 | Sample Rate | Loop Time |
|---|---|---|---|
| `Player_Idle.anim` | `idle-00` ~ `idle-03` | 12 | ✅ 勾选 |
| `Player_Run.anim` | `run-00` ~ `run-05` | 15 | ✅ 勾选 |
| `Player_Jump.anim` | `jump-00` ~ `jump-03` | 12 | ❌ 不勾选 |
| `Player_Fall.anim` | `fall-00` ~ `fall-01` | 12 | ✅ 勾选 |
| `Player_Attack.anim` | `attack1-00` ~ `attack1-04` | 15 | ❌ 不勾选 |
| `Player_Roll.anim` | `smrslt-00` ~ `smrslt-03` | 15 | ❌ 不勾选 |

**注意事项**：
- **Loop Time 勾选规则**：
  - ✅ 循环动画（Idle, Run, Fall）：动画会一直重复播放
  - ❌ 一次性动画（Jump, Attack, Roll）：播放一次后停在最后一帧
- **Sample Rate 选择**：
  - 12 FPS：适合慢动作（Idle, Jump, Fall）
  - 15 FPS：适合快动作（Run, Attack, Roll）

**产出物**：
- 6 个 `.anim` 文件，每个包含帧序列和播放设置

---

#### 2.3 添加并配置 Animator 组件

**⚠️ 重要：必须先在 Player GameObject 上添加 Animator 组件**

1. **添加 Animator 组件到 Player**
   - 选中 Player GameObject
   - Inspector 底部点击 `Add Component`
   - 搜索并添加 `Animator`

2. **分配 Animator Controller**
   - 在 Animator 组件的 `Controller` 字段
   - 拖入前面创建的 `PlayerController`（或你的角色控制器）
   - **如果不分配 Controller，后续代码中 `GetComponent<Animator>()` 会返回有效引用，但动画不会播放**

3. **配置 Animator 组件参数**

   | 参数 | 设置值 | 说明 |
   |---|---|---|
   | **Controller** | PlayerController | 必须分配，否则无动画 |
   | **Avatar** | None | 2D 项目不需要 |
   | **Apply Root Motion** | ❌ 不勾选 | 使用脚本控制移动 |
   | **Update Mode** | Normal | 正常更新 |
   | **Culling Mode** | Always Animate | 即使不可见也播放动画 |

#### 2.4 配置 Animator Controller 状态机

这是最复杂的部分，需要在 Animator 窗口中构建完整的状态机。

**打开 Animator 窗口**：
- Window → Animation → Animator
- 双击 `PlayerController.controller` 文件
- 停靠到 Unity 窗口中部（便于查看状态机网格）

##### **步骤 1：创建参数 (Parameters)**

在 Animator 窗口左侧 Parameters 面板：

1. **添加 isGrounded（Bool）**
   - 点击 Parameters 面板的 `+` 按钮
   - 选择 `Bool`
   - 命名为 `isGrounded`（区分大小写）
   - 默认值保持 `false`

2. **添加 isMoving（Bool）**
   - 点击 `+` → `Bool`
   - 命名为 `isMoving`

3. **添加 velocityY（Float）**
   - 点击 `+` → `Float`
   - 命名为 `velocityY`
   - 默认值 `0`

4. **添加 jump（Trigger）**
   - 点击 `+` → `Trigger`
   - 命名为 `jump`

5. **添加 attack（Trigger）**
   - 点击 `+` → `Trigger`
   - 命名为 `attack`

6. **添加 roll（Trigger）**
   - 点击 `+` → `Trigger`
   - 命名为 `roll`

**验证**：Parameters 面板应显示这 6 个参数

---

##### **步骤 2：创建动画状态 (States)**

在 Animator 窗口的网格区域右键操作：

**① 创建 Idle 状态（默认状态）**

1. 右键空白处 → `Create State` → `Empty`
2. 命名为 `Idle`
3. 选中 Idle 状态节点（橙色方块）
4. Inspector 中 `Motion` 字段：
   - 从 Project 拖入 `Player_Idle.anim`
5. **设为默认状态**：
   - 右键 Idle 状态 → `Set as Layer Default State`
   - Idle 节点变为橙色
   - Entry 节点（绿色）会自动连接到 Idle

**② 创建其他核心状态**

重复上述步骤创建（每个都分配对应的 .anim 文件）：

| 状态名称 | Motion（动画剪辑） |
|---|---|
| `Move` | `Player_Run.anim` |
| `Jump` | `Player_Jump.anim` |
| `Fall` | `Player_Fall.anim` |
| `Attack` | `Player_Attack.anim` |
| `Roll` | `Player_Roll.anim` |

**布局建议**（便于维护和可视化）：
```
基础流程（修正后）:
           [Entry] → [Idle] ←→ [Move]
                       ↓          ↓
                 (用户输入跳跃)    (用户输入跳跃)
                       ↓          ↓
                     [Jump] ←-----┘
                       ↓
                   (velocityY < -0.1)
                       ↓
                     [Fall]
                    ↙      ↘
           (isGrounded +   (isGrounded +
            !isMoving)      isMoving)
                ↓              ↓
              [Idle]        [Move]

自由落体触发（新增）:
[Idle]/[Move] 被动离地 → [Fall]
 (条件: !isGrounded && velocityY < -0.1)

特殊动作（任意状态可触发）:
[Any State] → [Attack] (attack trigger) → 固定返回 [Idle]
[Any State] → [Roll] (roll trigger) → 固定返回 [Idle]
```

---

##### **步骤 3：创建状态转换 (Transitions)**

**核心转换关系图**：

```
Idle ↔ Move（双向，基于 isMoving）
Idle/Move → Jump（基于 Jump输入 && isGrounded）
Idle/Move → Fall（基于 !isGrounded && velocityY < -0.1，自由落体）
Jump → Fall（基于 velocityY < -0.1）
Fall → Idle（基于 isGrounded && !isMoving）
Fall → Move（基于 isGrounded && isMoving）
Any State → Attack（基于 attack trigger）
Any State → Roll（基于 roll trigger）
```

**详细配置步骤**：

---

**转换① Idle ↔ Move（双向）**

**Idle → Move**：
1. 右键 `Idle` 状态 → `Make Transition`
2. 点击 `Move` 状态（出现白色箭头）
3. 选中白色箭头线，Inspector 中配置：
   - **Has Exit Time**: ❌ 取消勾选（立即响应输入）
   - **Transition Duration**: `0.1`（秒，平滑过渡）
   - **Conditions**（条件）：
     - 点击 `+` 添加条件
     - 选择 `isMoving` → `true`

**Move → Idle**（反向）：
1. 右键 `Move` → `Make Transition` → 点击 `Idle`
2. Inspector 配置：
   - **Has Exit Time**: ❌ 取消勾选
   - **Transition Duration**: `0.1`
   - **Conditions**: `isMoving` → `false`

---

**转换② 地面状态 → Jump（输入驱动）**

需要创建两条转换：`Idle → Jump` 和 `Move → Jump`

**⚠️ 重要：跳跃是由用户输入触发的，而不是物理状态**

实际的跳跃转换应该在 **PlayerStateMachine 脚本逻辑** 中处理，而不是在 Animator 中配置。Animator 中的转换仅作为视觉反馈的备份机制。

**脚本中的正确实现**：

```csharp
// 在 IdleState.Update() 和 MoveState.Update() 中：
if (Input.GetButtonDown("Jump") && player.IsGrounded)
{
    stateMachine.ChangeState(stateMachine.JumpState);
}
```

**Animator 中的备份转换**（可选配置）：

如果希望在 Animator 中也配置转换作为备份：

**Idle → Jump**：
1. `Idle` → `Make Transition` → `Jump`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`（快速响应）
   - **Conditions**：
     - `jump`（Trigger 触发）
     - `isGrounded` → `true`（确认在地面上）

**Move → Jump**：
1. `Move` → `Make Transition` → `Jump`
2. 同样的配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`
   - **Conditions**:
     - `jump`（Trigger 触发）
     - `isGrounded` → `true`（确认在地面上）

**⚠️ 重要说明**：
- 这些 Animator 转换仅在脚本逻辑失效时提供备份
- **主要的跳跃逻辑应该在状态类的 Update() 中通过检测 Input.GetButtonDown("Jump") 实现**
- Animator 参数会在 JumpState.Enter() 中更新，触发动画播放

---

**转换②-新增 地面状态 → Fall（自由落体，物理驱动）**

这是新增的转换，用于处理玩家被动离开地面的情况（如走出悬崖边）。

需要创建两条转换：`Idle → Fall` 和 `Move → Fall`

**Idle → Fall**：
1. `Idle` → `Make Transition` → `Fall`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**（必须同时满足）：
     - `isGrounded` → `false`（离开地面）
     - `velocityY` → `Less` → `-0.1`（向下速度，确认是下落而非跳跃）

**Move → Fall**：
1. `Move` → `Make Transition` → `Fall`
2. 同样的配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**:
     - `isGrounded` = `false`
     - `velocityY` Less `-0.1`

**⚠️ 为什么需要两个条件**：
- 只用 `!isGrounded` 会在跳跃开始瞬间误触发（跳跃时也会离开地面）
- 只用 `velocityY < -0.1` 可能在地面上因物理抖动误触发
- **两个条件同时满足才确认是真正的自由落体**（非跳跃导致的下落）

---

**转换③ Jump → Fall**

1. `Jump` → `Make Transition` → `Fall`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**: `velocityY` → `Less` → `-0.1`

**⚠️ 为什么用 -0.1 而非 0**：
- 避免在跳跃顶点（velocityY ≈ 0）时抖动
- 明确的下落阈值使转换更稳定

---

**转换④ Fall → 着陆（两种情况）**

**Fall → Idle**（着陆后静止）：
1. `Fall` → `Make Transition` → `Idle`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.15`（着陆缓冲）
   - **Conditions**（同时满足）：
     - `isGrounded` → `true`
     - `isMoving` → `false`

**Fall → Move**（着陆后继续移动）：
1. `Fall` → `Make Transition` → `Move`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.15`
   - **Conditions**：
     - `isGrounded` → `true`
     - `isMoving` → `true`

**⚠️ 为什么分两条**：
- 着陆时可能玩家正在按移动键（直接进 Move）
- 也可能松开键（进 Idle）
- 这样避免 Fall → Idle → Move 的额外跳转

---

**转换⑤ Any State → 特殊动作**

**Any State → Attack**：
1. 右键 `Any State`（灰色节点） → `Make Transition` → `Attack`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`
   - **Conditions**: `attack`（Trigger 类型）
   - **Interruption Source**: `Current State`（允许中断当前动作）

**Attack → 回归逻辑（方案 A - 简单固定返回）**：
1. `Attack` → `Make Transition` → `Idle`
2. Inspector 配置：
   - **Has Exit Time**: ✅ **勾选**（播放完整个攻击动画）
   - **Exit Time**: `0.95`（接近结束时，范围 0-1）
   - **Transition Duration**: `0.1`
   - **Conditions**: 无（仅依靠 Exit Time）

**Any State → Roll**：
1. `Any State` → `Make Transition` → `Roll`
2. 配置同 Attack：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`
   - **Conditions**: `roll`
   - **Interruption Source**: `Current State`

**Roll → Idle**：
1. `Roll` → `Make Transition` → `Idle`
2. 配置同 Attack 退出：
   - **Has Exit Time**: ✅ 勾选
   - **Exit Time**: `0.95`

---

##### **步骤 4：调整转换优先级**

选中一个状态（如 Idle），Inspector 中的 `Transitions` 列表显示所有从该状态出发的转换。

**优先级顺序**（从上到下，上面的优先检查）：
1. 特殊动作（Jump, Attack, Roll）
2. 垂直运动（Fall）
3. 水平运动（Move）

**调整方法**：
- 直接拖动 Transitions 列表中的条目重新排序

**示例（Idle 状态的转换优先级）**：
```
1. Idle → Jump（最优先）
2. Idle → Move
```

---

##### **步骤 5：设置转换配置的通用原则**

| 参数 | 响应式转换 | 动画驱动转换 |
|---|---|---|
| **Has Exit Time** | ❌ 不勾选 | ✅ 勾选 |
| **适用场景** | Idle↔Move, Jump, Fall | Attack→Idle, Roll→Idle |
| **Transition Duration** | 0.05-0.15s | 0.1-0.2s |
| **Interruption Source** | None | Current State（特殊动作） |

**产出物**：
- 完整的状态机，包含 6 个状态和约 10-12 条转换
- 所有转换都有明确的条件和配置

---

#### 2.5 实现中的关键决策点

在配置动画系统时，以下几个决策点会影响最终的游戏手感和维护成本。

##### **决策 1：Sample Rate（动画采样率）配置**

**Sample Rate 的选择**：

| Sample Rate | 特点 | 适用场景 |
|---|---|---|
| **12 FPS** | 更复古的像素风格，动作略显顿挫 | 慢节奏动作（Idle, Jump, Fall） |
| **15 FPS** | 更流畅，适合快节奏动作游戏 | 快节奏动作（Run, Attack, Roll） |

**建议**：
- 初始全部使用 12 FPS
- 如果感觉移动或攻击不流畅，再针对性调整到 15 FPS
- 保持同类动画使用相同 Sample Rate（如所有攻击动画都用 15）

**Transition Duration 的权衡**：

| Duration | 效果 | 优点 | 缺点 |
|---|---|---|---|
| **0** | 硬切 | 响应极快 | 可能产生闪现 |
| **0.05-0.1s** | 快速混合 | 响应快且平滑 | 适合像素艺术 |
| **0.1-0.2s** | 柔和混合 | 非常平滑 | 略有延迟感 |

**建议**：
- 响应式转换（Idle ↔ Move, Jump, Fall）：0.05-0.1s
- 动画驱动转换（Attack → Idle）：0.1-0.2s
- 像素艺术游戏倾向短时间，高清游戏倾向长时间

---

##### **决策 2：Attack/Roll 退出策略**

配置 Attack 和 Roll 动画结束后如何返回的方式有三种方案：

**方案 A：固定返回 Idle**（**当前推荐使用**）

```
Attack/Roll → Idle（固定）
```

- **优点**：
  - 配置简单，只需一条转换
  - 不会出错
  - 易于调试
- **缺点**：
  - 攻击后总是站立，破坏动作连续性
  - 玩家需要重新按移动键

**实现**：
- Has Exit Time: ✅ 勾选
- Exit Time: 0.95
- Transition Duration: 0.1
- Conditions: 无

**当前建议**：**先暂时使用方案 A（固定返回 Idle）**，确保基础系统稳定运行。

---

**方案 B：智能返回**（**预留用于后续优化**）

```
Attack → Idle（条件：isGrounded=true, isMoving=false）
Attack → Move（条件：isGrounded=true, isMoving=true）
Attack → Fall（条件：isGrounded=false）
```

- **优点**：
  - 攻击后保持移动状态，手感连贯
  - 空中攻击后自然进入下落
- **缺点**：
  - 需要 3 条转换
  - 调试复杂（需确保条件互斥）
  - 转换优先级需仔细调整

**实现**：
- 每条转换都设置 Has Exit Time: ✅
- Exit Time: 0.95
- 添加对应的 Conditions
- 按优先级排序：Fall > Move > Idle

---

**方案 C：使用动画事件**（**预留用于高级功能扩展**）

```csharp
// 在 Attack 动画的最后一帧添加事件
// 调用 PlayerController.OnAttackComplete()
// 脚本中根据当前状态智能切换

public void OnAttackComplete()
{
    if (!isGrounded)
    {
        stateMachine.ChangeState(fallState);
    }
    else if (isMoving)
    {
        stateMachine.ChangeState(moveState);
    }
    else
    {
        stateMachine.ChangeState(idleState);
    }
}
```

- **优点**：
  - Animator 配置简单（Attack → Idle 即可）
  - 脚本逻辑完全控制，灵活性最高
  - 易于扩展（如连招系统）
- **缺点**：
  - 需要在动画文件中手动添加事件
  - 脚本与动画耦合

**迭代路径**：
1. **当前阶段（初期）**：使用方案 A，快速验证核心玩法
2. **后续优化（中期）**：当动作系统稳定后，可切换到方案 B 提升手感
3. **高级扩展（后期）**：如果需要连招或复杂战斗系统，可采用方案 C

---

##### **决策 3：参数更新频率**

**连续参数（isGrounded, velocityY）**：

- **必须**在所有状态的 `Update()` 中每帧更新
- **原因**：Animator 的转换条件每帧检查这些值
- **如果不更新**：
  - 跳跃转换会延迟或失效
  - 着陆判定不准确
  - 动画与状态不同步

**瞬时参数（attack, roll）** - **推荐使用 Trigger 类型**：

- **只在**状态 `Enter()` 触发一次
- **原因**：Trigger 类型会自动重置，无需手动清除
- **如果在 Update() 重复触发**：
  - 动画每帧重新开始
  - 产生抖动和卡顿

**⚠️ 性能警告：使用 Trigger 参数时的注意事项**

虽然 Trigger 参数推荐用于瞬时动作（attack, roll），但必须注意性能消耗：

- **问题**：每次调用 `SetTrigger()` 会导致 Animator 重新评估所有转换条件
- **影响**：在复杂状态机中（10+ 状态，20+ 转换），频繁触发可能造成性能开销
- **最佳实践**：
  - **只在状态 Enter() 中调用一次**，永远不要在 Update() 中重复调用
  - 如果同一帧需要触发多个 Trigger，考虑合并为一个复合动作
  - 使用 Profiler（Window → Analysis → Profiler）监控 Animator.Update 的耗时
  - 如果发现性能瓶颈，可考虑使用 Bool 参数代替 Trigger（需手动重置）

**性能对比**：

| 参数类型 | 调用开销 | 适用场景 | 注意事项 |
|---|---|---|---|
| **Trigger** | 中等（触发转换评估） | 瞬时动作（攻击、跳跃） | 只在 Enter() 触发，避免重复调用 |
| **Bool** | 低（仅设置值） | 持续状态（移动、地面检测） | 需手动重置为 false |
| **Float** | 低（仅设置值） | 连续变化值（速度、高度） | 每帧更新，但开销可控 |

---

##### **决策 4：Animator 与 PlayerStateMachine 同步策略**

**问题**：两个状态机使用不同判定条件时会不同步

**解决方案 1：参数驱动**（**推荐使用**）

- PlayerStateMachine 只负责逻辑
- Animator 完全依赖参数自动转换
- 两者通过参数保持同步

**优点**：解耦，Animator 可独立调试

**解决方案 2：脚本驱动**

- PlayerStateMachine 直接调用 `animator.Play("StateName")`
- 强制 Animator 播放指定动画
- Animator 的转换条件仅作为备份

**缺点**：耦合紧密，难以调整

**建议**：**使用方案 1（参数驱动）**，更符合 Unity 的设计理念，便于在 Animator 窗口中可视化调试和调整

---

#### 2.6 验证和调试流程

完成配置后，需要系统性地验证动画系统是否正常工作。

##### **阶段 1：静态验证（不运行游戏）**

在 Animator 窗口中检查：

**检查清单**：

| 检查项 | 验证方法 | 预期结果 |
|---|---|---|
| ✅ 所有状态都有动画 | 逐个点击状态，查看 Motion 字段 | 每个状态显示对应的 .anim 文件 |
| ✅ 所有转换都有条件 | 点击转换箭头，查看 Conditions | 除 Attack/Roll 退出外，都有明确条件 |
| ✅ Entry 连接正确 | 查看绿色 Entry 节点 | 连接到 Idle 状态（橙色） |
| ✅ 参数命名正确 | 对照代码检查 Parameters 面板 | 大小写完全匹配 |
| ✅ 转换方向正确 | 检查箭头方向 | Idle ↔ Move 是双向箭头 |

**常见错误**：
- ❌ 状态的 Motion 为 None → 该状态不会播放动画
- ❌ 转换缺少条件 → 可能无法触发或错误触发
- ❌ 参数名大小写错误 → 代码 SetBool 无效

---

##### **阶段 2：运行时调试**

进入 Play Mode 后的调试步骤：

**步骤 1：打开 Animator 窗口**

- Unity 进入 Play Mode
- Window → Animation → Animator
- 双击 Player 的 Animator Controller
- 窗口会显示实时状态

**步骤 2：观察状态机行为**

| 观察内容 | 正常表现 | 异常表现 |
|---|---|---|
| **当前状态** | 蓝色高亮正确状态 | 一直停留在 Entry 或错误状态 |
| **转换触发** | 箭头白色闪烁 | 箭头从不闪烁 |
| **参数值** | Parameters 面板值实时变化 | 值一直为默认值 |

**步骤 3：手动测试转换**

在 Parameters 面板中**手动修改参数值**，观察状态是否切换：

```
测试 Idle → Move：
1. 当前状态应为 Idle（蓝色）
2. 点击 isMoving 参数，勾选为 true
3. 观察：状态应立即切换到 Move，角色播放跑步动画

测试 Jump：
1. 将 isGrounded 设为 false
2. 将 velocityY 设为 5
3. 观察：状态应切换到 Jump

测试 Attack：
1. 点击 attack 参数右侧的按钮（触发 Trigger）
2. 观察：从 Any State 进入 Attack，播放攻击动画
3. 动画结束后自动返回 Idle
```

**步骤 4：使用 Debug.Log 验证脚本**

在 PlayerController 和状态类中添加日志：

```csharp
// PlayerController.cs - Awake()
Debug.Log($"Animator组件: {(animator != null ? "已找到" : "未找到")}");

// IdleState.cs - Enter()
Debug.Log("进入 Idle 状态，设置 isMoving=false");
player.Animator.SetBool("isMoving", false);

// MoveState.cs - Update()
Debug.Log($"Move状态 - isGrounded: {player.IsGrounded}, velocityY: {player.Rigidbody.velocity.y}");
```

**查看 Console 输出**：
- 确认状态切换按预期发生
- 确认参数值正确传递给 Animator

---

##### **阶段 3：常见问题快速诊断**

| 症状 | 可能原因 | 诊断步骤 | 解决方案 |
|---|---|---|---|
| **角色没有动画** | Animator 组件缺失或 Controller 未分配 | 检查 Inspector 中的 Animator 组件 | 按阶段 2.3 添加组件并分配 Controller |
| **动画不切换** | 参数值没有变化 | Animator 窗口查看 Parameters 实时值 | 检查脚本是否调用 SetBool/SetFloat |
| **切换抖动** | 两条转换同时满足条件 | 检查转换优先级和条件互斥性 | 调整 Transitions 列表顺序或修改条件 |
| **跳跃动画在地面触发** | 跳跃逻辑应在脚本中实现，而非 Animator 转换 | 检查 IdleState/MoveState 的 Update() 方法 | 确保使用 Input.GetButtonDown("Jump") 检测跳跃输入 |
| **攻击动画循环播放** | Attack 状态的 Exit Time 未勾选 | 检查 Attack → Idle 的 Has Exit Time | 勾选 ✅ Has Exit Time |

---

##### **阶段 4：性能验证（可选）**

如果动画系统导致卡顿：

1. **Profiler 分析**：
   - Window → Analysis → Profiler
   - 运行游戏，查看 CPU Usage → Animation
   - 查找耗时的 Animator 计算

2. **优化建议**：
   - 减少转换数量（合并相似转换）
   - 降低 Animator 的 Update Mode 为 Animate Physics（如果角色是物理驱动）
   - 使用 Culling Mode: Cull Completely（角色不可见时停止动画）

---

##### **完成验证清单**

完成所有验证后，确认以下行为正常：

```
✅ Play Mode 中 Idle 动画自动播放
✅ 按 A/D 键，角色切换到 Run 动画
✅ 按空格键跳跃，播放 Jump → Fall 动画
✅ 着陆后根据是否按移动键，正确返回 Idle 或 Move
✅ 按鼠标左键，播放 Attack 动画并自动返回
✅ 所有转换平滑，无抖动或卡顿
✅ Console 无 NullReferenceException 或 MissingComponent 错误
```

**如果全部通过**：阶段 2 动画系统配置成功！可以进入阶段 3 脚本集成。

**如果有问题**：参考"常见问题和解决方案"章节（文档末尾）排查。

---

### 阶段 3：脚本集成 (Script Integration)

本阶段将创建完整的玩家控制器脚本系统，实现基础6状态（Idle, Move, Jump, Fall, Attack, Roll）的功能。

**阶段目标**：
- 创建核心脚本文件（PlayerInput, PlayerController, PlayerStates等）
- 实现6个玩家状态的完整逻辑
- 集成现有的动画系统
- 配置输入和物理检测

---

#### 3.1 现有代码架构概览 (Current Code Architecture Overview)

**前置确认**：

✅ 阶段 1-2 已完成（基础GameObject和动画系统配置完成）
✅ Animator Controller已配置6个基础状态（Idle, Move, Jump, Fall, Attack, Roll）
✅ Animator Parameters已创建（isGrounded, isMoving, velocityY, attack, roll）

**当前代码库已有的脚本**：

```
Assets/Scripts/DeadCells.Player/
├── PlayerInput.cs              - 轻量输入封装（使用旧Input API）
├── PlayerState.cs              - 状态抽象基类
├── PlayerStateMachine.cs       - 状态机（管理6个状态）
├── PlayerStates.cs             - 6个状态类实现
└── PlayerController.cs         - 核心控制器
```

**PlayerInput.cs 当前实现**（已存在，无需修改）：
- 使用 `Input.GetAxisRaw("Horizontal")` 获取移动输入
- 使用 `Input.GetKeyDown(KeyCode.Space)` 检测跳跃
- 纯C#类，不是MonoBehaviour
- PlayerController在其Update()中调用`playerInput.Update()`

#### 3.2 Animator参数更新最佳实践 (Animator Parameter Update Best Practices)

**⚠️ 关键规则**：防止动画抖动和参数冲突

**规则 1：Bool参数只能在特定状态中修改**

| 参数名 | 谁负责设置为true | 谁负责设置为false | 其他状态是否可以修改 |
|-------|-----------------|------------------|---------------------|
| `isMoving` | MoveState.Enter() | MoveState.Exit() | ❌ 禁止其他状态修改 |
| `isGrounded` | PlayerController.Update() | PlayerController.Update() | ❌ 禁止状态类修改 |

**错误示例**（会导致isMoving一直为true）：
```csharp
// ❌ 错误：在IdleState中设置isMoving
public class PlayerIdleState : PlayerState
{
    public override void Update()
    {
        player.Animator.SetBool("isMoving", false); // 错误！不要在Idle中设置

        if (Mathf.Abs(player.Input.Horizontal) > 0.1f)
        {
            stateMachine.ChangeState(stateMachine.MoveState);
        }
    }
}
```

**正确示例**：
```csharp
// ✅ 正确：只在MoveState的Enter/Exit中控制isMoving
public class PlayerMoveState : PlayerState
{
    public override void Enter()
    {
        player.Animator.SetBool("isMoving", true); // 进入时设为true
    }

    public override void Exit()
    {
        player.Animator.SetBool("isMoving", false); // 离开时设为false
    }

    public override void Update()
    {
        // Update中只检测条件，不要修改isMoving
        if (Mathf.Abs(player.Input.Horizontal) < 0.1f)
        {
            stateMachine.ChangeState(stateMachine.IdleState);
        }
    }
}
```

---

**规则 2：Trigger参数只在Enter()中触发一次**

**错误示例**（会导致动画每帧重新开始）：
```csharp
// ❌ 错误：在Update()中重复触发
public class PlayerAttackState : PlayerState
{
    public override void Update()
    {
        player.Animator.SetTrigger("attack"); // 错误！每帧都触发
        // ... 其他逻辑
    }
}
```

**正确示例**：
```csharp
// ✅ 正确：只在Enter()中触发一次
public class PlayerAttackState : PlayerState
{
    public override void Enter()
    {
        player.Animator.SetTrigger("attack"); // 正确：仅触发一次
    }

    public override void Update()
    {
        // Update中不要再次触发attack
        // 只处理状态转换逻辑
    }
}
```

---

**规则 3：Float参数由PlayerController集中更新**

**正确实现**（在PlayerController中）：
```csharp
void Update()
{
    // 物理检测
    CheckGrounded();

    // 集中更新Animator参数（每帧）
    animator.SetBool("isGrounded", IsGrounded);
    animator.SetFloat("velocityY", rb.velocity.y);

    // 状态机更新
    playerInput.Update();
    stateMachine.Update();
}
```

**注意**：状态类的Update()中**不要重复设置**这些参数，PlayerController已经处理。

---

#### 3.3 当前6个状态类的实现要点 (Current 6 States Implementation)

---

##### **步骤 3：添加新动画状态**

在 Animator 窗口的状态机网格中创建新状态：

**① 创建 DoubleJump 状态**

1. 右键空白处 → `Create State` → `Empty`
2. 命名为 `DoubleJump`
3. 选中状态，Inspector 中：
   - **Motion**: 拖入 `Adventurer_DoubleJump.anim`

**② 创建 WallSlide 状态**

1. 创建新状态，命名为 `WallSlide`
2. Inspector 中：
   - **Motion**: 拖入 `Adventurer_WallSlide.anim`

**③ 创建 WallJump 状态**

1. 创建新状态，命名为 `WallJump`
2. Inspector 中：
   - **Motion**: 拖入 `Adventurer_WallJump.anim`

**布局建议**（状态机可视化组织）：

```
基础流程（已有）:
[Entry] → [Idle] ↔ [Move] → [Jump] → [Fall] → 回到 Idle/Move
                                ↓
                          [DoubleJump]
                                ↓
                             [Fall]

墙壁交互（新增）:
[Fall] → [WallSlide] → [WallJump] → [Fall]
           ↓
        回到 Fall
```

---

##### **步骤 4：配置高级动作转换**

**转换组 1：二段跳 (Double Jump)**

**Fall → DoubleJump**（在下落时按跳跃键触发）：

1. 右键 `Fall` → `Make Transition` → `DoubleJump`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`（快速响应）
   - **Conditions**（必须同时满足）：
     - `canDoubleJump` → `true`
     - `velocityY` → `Greater` → `0.1`（表示二段跳已触发且向上加速）

**DoubleJump → Fall**（二段跳上升结束后下落）：

1. `DoubleJump` → `Make Transition` → `Fall`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**:
     - `velocityY` → `Less` → `-0.1`（开始下落）

---

**转换组 2：爬墙滑行 (Wall Slide)**

**Fall → WallSlide**（下落时接触墙壁触发）：

1. `Fall` → `Make Transition` → `WallSlide`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**（必须同时满足）：
     - `isTouchingWall` → `true`
     - `isWallSliding` → `true`
     - `isGrounded` → `false`（必须在空中）

**WallSlide → Fall**（离开墙壁回到下落）：

1. `WallSlide` → `Make Transition` → `Fall`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**（任一满足即可，多条转换）：
     - 方案 A：`isTouchingWall` → `false`
     - 方案 B：`isGrounded` → `true`（着陆）

**WallSlide → WallJump**（爬墙时按跳跃键触发蹬墙跳）：

1. `WallSlide` → `Make Transition` → `WallJump`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.05`
   - **Conditions**:
     - `wallJump`（Trigger 触发）

---

**转换组 3：蹬墙跳 (Wall Jump)**

**WallJump → Fall**（蹬墙跳动作结束后进入下落）：

1. `WallJump` → `Make Transition` → `Fall`
2. Inspector 配置：
   - **Has Exit Time**: ❌
   - **Transition Duration**: `0.1`
   - **Conditions**:
     - `velocityY` → `Less` → `-0.1`（开始下落）

---

**转换优先级调整**：

选中 `Fall` 状态，在 Inspector 的 Transitions 列表中调整顺序（从上到下优先级递减）：

```
Fall 状态的转换优先级（推荐顺序）：
1. Fall → WallSlide（墙壁交互优先）
2. Fall → DoubleJump（二段跳次之）
3. Fall → Idle（着陆）
4. Fall → Move（着陆后移动）
```

**为什么这样排序**：
- WallSlide 最优先：玩家贴墙时应立即进入滑墙状态
- DoubleJump 次之：空中跳跃是紧急动作
- 着陆转换最后：确保空中动作先执行

---

##### **步骤 5：脚本集成 - PlayerController 扩展**

**5.1 添加墙壁检测组件**

在 PlayerController.cs 中添加墙壁检测字段：

```csharp
[Header("墙壁检测 (Wall Detection)")]
public Transform wallCheckFront;      // 角色前方的墙壁检测点
public Transform wallCheckBack;       // 角色后方的墙壁检测点（可选）
public float wallCheckDistance = 0.3f; // 墙壁检测距离
public LayerMask wallLayer;            // 墙壁所在图层

private bool isTouchingWall;
```

**5.2 添加高级动作参数**

在 PlayerController.cs 中添加配置参数：

```csharp
[Header("二段跳设置 (Double Jump Settings)")]
public float doubleJumpForce = 16f;
public bool canChangeDirectionInAir = true; // 是否允许空中改变水平方向

[Header("蹬墙跳设置 (Wall Jump Settings)")]
public bool wallJumpConsumesDoubleJump = false; // 蹬墙跳是否消耗二段跳次数
public float wallJumpAngle = 45f;              // 蹬墙跳角度（度）
public float wallJumpForce = 15f;              // 蹬墙跳力度

[Header("爬墙设置 (Wall Slide Settings)")]
public float wallSlideSpeed = 2f;  // 爬墙滑行速度（正值，向下）
public float wallRunSpeed = 8f;    // 爬墙跑速度（预留，未实现）

// 运行时状态
private bool hasDoubleJump;      // 是否还有二段跳机会
private float originalGravityScale; // 原始重力缩放
```

**5.3 在 Awake() 中初始化**

```csharp
void Awake()
{
    animator = GetComponent<Animator>();
    rigidbody2D = GetComponent<Rigidbody2D>();

    originalGravityScale = rigidbody2D.gravityScale;
    hasDoubleJump = false; // 着陆后才能获得二段跳
}
```

**5.4 在 Update() 中更新检测**

```csharp
void Update()
{
    // ... 原有的地面检测代码

    // 墙壁检测
    UpdateWallDetection();

    // 更新动画参数
    animator.SetBool("isTouchingWall", isTouchingWall);
}

void UpdateWallDetection()
{
    Vector2 direction = transform.right; // 角色面向方向

    isTouchingWall = Physics2D.Raycast(
        wallCheckFront.position,
        direction,
        wallCheckDistance,
        wallLayer
    );
}
```

**5.5 创建墙壁检测点 GameObject**

在 Unity Editor 中：

1. 右键 Player → Create Empty，命名为 `WallCheckFront`
2. Transform Position 设置为：
   - X: `0.4`（角色身体右侧，根据 Collider 大小调整）
   - Y: `0`（角色垂直中心）
   - Z: `0`
3. 将 WallCheckFront 拖入 PlayerController 的 `wallCheckFront` 字段

**验证墙壁检测**：
- 在 Scene 视图中选中 WallCheckFront
- 应该看到一个小圆圈图标在角色身体侧面
- 运行游戏，角色贴墙时 Console 应输出检测到墙壁的日志（如添加调试日志）

---

##### **步骤 6：实现高级动作状态类**

**6.1 DoubleJumpState 实现**

在 PlayerStates.cs 中添加：

```csharp
public class DoubleJumpState : PlayerState
{
    public DoubleJumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 执行二段跳
        Vector2 velocity = player.Rigidbody.velocity;

        // 允许空中改变方向
        if (player.canChangeDirectionInAir)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            velocity.x = horizontalInput * player.moveSpeed;
        }

        // 向上施加力
        velocity.y = player.doubleJumpForce;
        player.Rigidbody.velocity = velocity;

        // 消耗二段跳机会
        player.hasDoubleJump = false;
        player.Animator.SetBool("canDoubleJump", false);

        Debug.Log("执行二段跳");
    }

    public override void Update()
    {
        // 持续更新动画参数
        player.Animator.SetFloat("velocityY", player.Rigidbody.velocity.y);
        player.Animator.SetBool("isGrounded", player.IsGrounded);

        // 当向下速度时转换到 Fall
        if (player.Rigidbody.velocity.y < -0.1f)
        {
            player.StateMachine.ChangeState(player.fallState);
        }
    }

    public override void FixedUpdate()
    {
        // 空中水平移动控制（如果允许）
        if (player.canChangeDirectionInAir)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            player.Rigidbody.velocity = new Vector2(
                horizontalInput * player.moveSpeed,
                player.Rigidbody.velocity.y
            );
        }
    }

    public override void Exit()
    {
        // 无需特殊清理
    }
}
```

---

**6.2 WallSlideState 实现**

```csharp
public class WallSlideState : PlayerState
{
    public WallSlideState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 取消重力，使用固定速度下滑
        player.Rigidbody.gravityScale = 0;

        player.Animator.SetBool("isWallSliding", true);

        Debug.Log("进入爬墙滑行状态");
    }

    public override void Update()
    {
        // 持续更新参数
        player.Animator.SetBool("isTouchingWall", player.isTouchingWall);
        player.Animator.SetBool("isGrounded", player.IsGrounded);

        // 检测离开墙壁
        if (!player.isTouchingWall)
        {
            player.StateMachine.ChangeState(player.fallState);
            return;
        }

        // 检测着陆
        if (player.IsGrounded)
        {
            player.StateMachine.ChangeState(player.idleState);
            return;
        }

        // 检测跳跃输入（蹬墙跳）
        if (Input.GetButtonDown("Jump"))
        {
            player.Animator.SetTrigger("wallJump");
            player.StateMachine.ChangeState(player.wallJumpState);
        }
    }

    public override void FixedUpdate()
    {
        // 固定速度下滑（不受重力加速度影响）
        player.Rigidbody.velocity = new Vector2(
            0, // 水平速度为 0（贴墙）
            -player.wallSlideSpeed // 固定向下速度
        );
    }

    public override void Exit()
    {
        // 恢复重力
        player.Rigidbody.gravityScale = player.originalGravityScale;
        player.Animator.SetBool("isWallSliding", false);
    }
}
```

---

**6.3 WallJumpState 实现**

```csharp
public class WallJumpState : PlayerState
{
    public WallJumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 计算蹬墙跳方向（与墙壁相反方向）
        Vector2 wallDirection = player.transform.right; // 角色面向方向
        Vector2 jumpDirection = -wallDirection; // 反方向

        // 应用角度（默认 45°）
        float angleRad = player.wallJumpAngle * Mathf.Deg2Rad;
        Vector2 force = new Vector2(
            jumpDirection.x * Mathf.Cos(angleRad) * player.wallJumpForce,
            Mathf.Sin(angleRad) * player.wallJumpForce
        );

        // 施加力
        player.Rigidbody.velocity = force;

        // 是否消耗二段跳
        if (player.wallJumpConsumesDoubleJump)
        {
            player.hasDoubleJump = false;
            player.Animator.SetBool("canDoubleJump", false);
        }

        Debug.Log($"蹬墙跳：方向={jumpDirection}, 力度={force}");
    }

    public override void Update()
    {
        // 持续更新参数
        player.Animator.SetFloat("velocityY", player.Rigidbody.velocity.y);
        player.Animator.SetBool("isGrounded", player.IsGrounded);

        // 开始下落时转换到 Fall
        if (player.Rigidbody.velocity.y < -0.1f)
        {
            player.StateMachine.ChangeState(player.fallState);
        }
    }

    public override void FixedUpdate()
    {
        // 蹬墙跳期间不允许额外控制（保持跳跃轨迹）
        // 如果需要空中控制，可以添加类似 DoubleJump 的逻辑
    }

    public override void Exit()
    {
        // 无需特殊清理
    }
}
```

---

**6.4 修改 FallState 以支持高级动作**

在现有 FallState 的 Update() 中添加：

```csharp
public override void Update()
{
    // ... 原有的着陆检测代码

    // 检测二段跳输入
    if (Input.GetButtonDown("Jump") && player.hasDoubleJump)
    {
        player.StateMachine.ChangeState(player.doubleJumpState);
        return;
    }

    // 检测墙壁接触（进入爬墙滑行）
    if (player.isTouchingWall && !player.IsGrounded)
    {
        player.StateMachine.ChangeState(player.wallSlideState);
        return;
    }

    // 持续更新动画参数
    player.Animator.SetBool("isGrounded", player.IsGrounded);
    player.Animator.SetFloat("velocityY", player.Rigidbody.velocity.y);
}
```

---

**6.5 修改 JumpState 以重置二段跳机会**

在 JumpState 的 Enter() 中添加：

```csharp
public override void Enter()
{
    // ... 原有的跳跃逻辑

    // 离开地面时获得二段跳机会
    player.hasDoubleJump = true;
    player.Animator.SetBool("canDoubleJump", true);
}
```

---

##### **步骤 7：Inspector 参数配置**

在 Unity Editor 中选中 Player GameObject，配置 PlayerController 组件的参数：

**二段跳设置**：

| 参数 | 推荐值 | 效果 |
|---|---|---|
| Double Jump Force | 16 | 与基础跳跃相同高度 |
| Can Change Direction In Air | ✅ 勾选 | 允许空中转向（更灵活） |

**蹬墙跳设置**：

| 参数 | 推荐值 | 效果 |
|---|---|---|
| Wall Jump Consumes Double Jump | ❌ 不勾选 | 蹬墙跳后仍可二段跳（推荐） |
| Wall Jump Angle | 45° | 标准 45° 角跳跃 |
| Wall Jump Force | 15 | 略小于普通跳跃（手感平衡） |

**爬墙设置**：

| 参数 | 推荐值 | 效果 |
|---|---|---|
| Wall Slide Speed | 2 | 缓慢下滑（玩家有时间反应） |

**墙壁检测**：

| 参数 | 推荐值 | 说明 |
|---|---|---|
| Wall Check Front | 拖入创建的 WallCheckFront 对象 | 必须分配 |
| Wall Check Distance | 0.3 | 根据角色大小调整 |
| Wall Layer | 选择地面图层 | 通常与 Ground Layer 相同 |

---

##### **步骤 8：参数调优建议**

**调优目标 1：二段跳手感**

| 参数 | 调整方向 | 效果 |
|---|---|---|
| doubleJumpForce | 增大（18-20） | 二段跳更高，适合高平台 |
| doubleJumpForce | 减小（12-14） | 二段跳更低，更像空中微调 |
| canChangeDirectionInAir | 开启 | 允许空中完全转向（推荐） |
| canChangeDirectionInAir | 关闭 | 保持原有水平速度（硬核风格） |

**调优目标 2：爬墙手感**

| 参数 | 调整方向 | 效果 |
|---|---|---|
| wallSlideSpeed | 更小（1-1.5） | 下滑极慢，像"粘"在墙上 |
| wallSlideSpeed | 更大（3-4） | 快速滑落，只是减速下落 |
| wallJumpAngle | 30° | 更水平，适合远距离跳跃 |
| wallJumpAngle | 60° | 更垂直，适合高墙跳跃 |

**调优目标 3：动作连续性**

| 设置组合 | 适用游戏风格 |
|---|---|
| wallJumpConsumesDoubleJump = false<br>canChangeDirectionInAir = true | **流畅动作游戏**（死亡细胞风格）<br>- 玩家可蹬墙跳后再二段跳<br>- 空中完全自由控制<br>- 容错率高 |
| wallJumpConsumesDoubleJump = true<br>canChangeDirectionInAir = false | **硬核平台游戏**<br>- 每次空中动作必须精确计算<br>- 一旦跳出无法改变轨迹<br>- 高难度挑战 |

---

##### **步骤 9：验证和测试**

**测试清单 1：二段跳**

```
✅ 单次跳跃后按空格键，触发二段跳动画
✅ 二段跳后角色继续上升
✅ 二段跳后再次按空格键，不会触发三段跳
✅ 着陆后再次跳跃，二段跳机会恢复
✅ 二段跳期间可以改变水平方向（如果启用）
```

**测试清单 2：爬墙滑行**

```
✅ 下落时贴近墙壁，自动进入 WallSlide 动画
✅ 爬墙滑行速度固定（不会越来越快）
✅ 离开墙壁后自动回到 Fall 状态
✅ 爬墙滑行时按空格键，触发蹬墙跳
✅ 爬墙期间角色面向墙壁方向
```

**测试清单 3：蹬墙跳**

```
✅ 从爬墙状态按空格键，触发蹬墙跳动画
✅ 蹬墙跳方向与墙壁相反（45° 角向上）
✅ 蹬墙跳后仍可执行二段跳（如果 wallJumpConsumesDoubleJump = false）
✅ 蹬墙跳后播放 WallJump 动画，然后自然过渡到 Fall
```

**测试清单 4：组合动作**

```
✅ 跳跃 → 二段跳 → 着陆（流畅无卡顿）
✅ 跳跃 → 贴墙 → 爬墙滑行 → 蹬墙跳 → 二段跳（完整连招）
✅ 爬墙滑行 → 离开墙壁 → 再次贴墙 → 再次爬墙（可重复）
✅ 所有动作在 Animator 窗口中转换正确，无抖动
```

---

##### **步骤 10：常见问题排查**

| 症状 | 可能原因 | 解决方案 |
|---|---|---|
| **二段跳无法触发** | canDoubleJump 参数未正确设置 | 检查 JumpState.Enter() 是否设置 `hasDoubleJump = true` |
| **爬墙滑行速度越来越快** | 重力未取消 | 确认 WallSlideState.Enter() 中设置 `gravityScale = 0` |
| **蹬墙跳方向错误（向墙跳）** | 方向计算错误 | 检查 WallJumpState 中的 `jumpDirection = -wallDirection` |
| **角色贴墙但不触发爬墙** | 墙壁检测失败 | 1. 检查 WallCheckFront 位置<br>2. 确认 Wall Layer 设置正确<br>3. 在 Scene 视图调试 Raycast |
| **蹬墙跳后无法二段跳** | wallJumpConsumesDoubleJump = true | 修改为 false，或在 WallJumpState 中保留二段跳机会 |
| **动画播放但动作无效** | 物理逻辑未执行 | 确认状态的 FixedUpdate() 被正确调用 |

---

##### **步骤 11：高级优化（可选）**

**优化 1：添加爬墙耐力限制**

在 PlayerController 中添加：

```csharp
[Header("爬墙耐力")]
public float wallSlideStamina = 3f; // 最大爬墙时间（秒）
private float currentStamina;

// 在 WallSlideState.Update() 中：
currentStamina -= Time.deltaTime;
if (currentStamina <= 0)
{
    // 强制离开墙壁
    player.StateMachine.ChangeState(player.fallState);
}

// 着陆时重置：
currentStamina = wallSlideStamina;
```

**优化 2：添加蹬墙跳冷却时间**

防止玩家在两面墙之间无限跳跃：

```csharp
public float wallJumpCooldown = 0.5f;
private float lastWallJumpTime;

// 在 WallJumpState.Enter() 中：
lastWallJumpTime = Time.time;

// 在 WallSlideState.Update() 中：
if (Input.GetButtonDown("Jump") && Time.time > lastWallJumpTime + player.wallJumpCooldown)
{
    // 执行蹬墙跳
}
```

**优化 3：添加粒子效果**

增强视觉反馈：

```csharp
// 在 WallSlideState.Enter() 中：
if (wallSlideParticles != null)
{
    wallSlideParticles.Play();
}

// 在 WallJumpState.Enter() 中：
if (wallJumpDustEffect != null)
{
    Instantiate(wallJumpDustEffect, wallCheckFront.position, Quaternion.identity);
}
```

---

##### **配置参数效果对比表**

| 配置方案 | 二段跳力度 | 蹬墙跳角度 | 爬墙速度 | 消耗二段跳 | 适用风格 |
|---|---|---|---|---|---|
| **方案 A（流畅）** | 16 | 45° | 2 | ❌ | 死亡细胞、空洞骑士<br>高自由度，适合探索 |
| **方案 B（平衡）** | 14 | 50° | 2.5 | ❌ | 蔚蓝、肉鸽游戏<br>需要技巧但不苛刻 |
| **方案 C（硬核）** | 12 | 40° | 3 | ✅ | 超级食肉男孩<br>精确操作，高难度 |

---

##### **完成检查清单**

完成所有配置后，确认以下系统正常：

```
✅ 阶段 2.7 高级动作系统
   ├─ 动画资源
   │  ├─ Adventurer_DoubleJump.anim 已创建
   │  ├─ Adventurer_WallSlide.anim 已创建
   │  └─ Adventurer_WallJump.anim 已创建
   │
   ├─ Animator 配置
   │  ├─ 9 个参数（5 个基础 + 4 个高级）
   │  ├─ 9 个状态（6 个基础 + 3 个高级）
   │  └─ 约 16-18 条转换（包含高级动作转换）
   │
   ├─ 脚本集成
   │  ├─ PlayerController 扩展（墙壁检测、参数配置）
   │  ├─ DoubleJumpState 实现
   │  ├─ WallSlideState 实现
   │  ├─ WallJumpState 实现
   │  └─ FallState 和 JumpState 修改
   │
   ├─ GameObject 设置
   │  ├─ WallCheckFront 子对象已创建并正确定位
   │  └─ PlayerController 的所有字段已分配
   │
   └─ 测试验证
      ├─ 二段跳功能正常 ✅
      ├─ 爬墙滑行功能正常 ✅
      ├─ 蹬墙跳功能正常 ✅
      └─ 组合动作流畅无 bug ✅
```

**如果全部通过**：高级动作系统配置成功！角色现在具备完整的平台游戏动作能力。

**如果有问题**：参考"步骤 10：常见问题排查"或本文档末尾的"常见问题和解决方案"章节。

---

### 阶段 3：脚本集成 (Script Integration)

本阶段将创建完整的玩家控制器脚本系统，实现基础6状态（Idle, Move, Jump, Fall, Attack, Roll）的功能。

**阶段目标**：
- 确认现有脚本文件（PlayerInput, PlayerState, PlayerStateMachine, PlayerStates, PlayerController）已创建
- 实现6个玩家状态的完整逻辑
- 使用Unity传统Input API（Input.GetAxisRaw等）
- 代码总量约800行，简洁实用

**重要提示**：本节假设你已经通过其他方式创建了基础脚本文件。如果尚未创建，请先完成PlayerController、PlayerStates等核心脚本的编写。

---

#### 3.1 脚本文件结构规划 (Script File Structure Planning)

**文件组织方案**：采用5个独立脚本文件的模块化架构。

**文件清单和职责**：

```
Assets/Scripts/DeadCells.Player/
├── PlayerInput.cs              (~60行)  - 输入抽象层，使用旧Input API
├── PlayerState.cs              (~60行)  - 状态基类，定义状态生命周期
├── PlayerStateMachine.cs       (~100行) - 状态机，管理状态注册和切换
├── PlayerStates.cs             (~400行) - 所有6个状态类的实现
└── PlayerController.cs         (~350行) - 核心控制器，物理逻辑和组件管理
```

**依赖关系图**：

```
PlayerInput (无依赖)
    ↓
PlayerController (依赖 Input, StateMachine, Rigidbody2D, Animator)
    ↓
PlayerStateMachine (依赖 PlayerState)
    ↓
PlayerState (抽象基类，依赖 PlayerController)
    ↓
PlayerStates (6个具体状态类，继承 PlayerState)
```

**命名空间约定**：

所有脚本使用统一的命名空间：`namespace DeadCells.Player`

**为什么使用5个独立文件**：

| 优势 | 说明 |
|-----|------|
| **职责清晰** | 每个文件负责单一功能模块 |
| **易于维护** | 修改某个状态不影响其他文件 |
| **版本控制友好** | 减少合并冲突，PR审查清晰 |
| **便于扩展** | 添加新状态时可以快速定位代码 |
| **IDE支持好** | 类搜索、跳转、重构更流畅 |

---

#### 3.6 实现PlayerState状态基类 (Implementing Base State Class)

打开 `PlayerState.cs` 文件,替换为以下代码：

```csharp
namespace DeadCells.Player
{
    /// <summary>
    /// 玩家状态抽象基类
    /// 所有具体状态类（Idle, Move, Jump等）都继承此类
    /// </summary>
    public abstract class PlayerState
    {
        // === 引用 ===

        /// <summary>玩家控制器引用（子类通过此访问玩家组件）</summary>
        protected PlayerController player;

        /// <summary>状态机引用（子类用于切换状态）</summary>
        protected PlayerStateMachine stateMachine;

        // === 构造函数 ===

        /// <summary>
        /// 构造函数 - 所有子类必须调用base(player, stateMachine)
        /// </summary>
        /// <param name="player">玩家控制器实例</param>
        /// <param name="stateMachine">状态机实例</param>
        protected PlayerState(PlayerController player, PlayerStateMachine stateMachine)
        {
            this.player = player;
            this.stateMachine = stateMachine;
        }

        // === 状态生命周期（子类必须实现） ===

        /// <summary>
        /// 进入状态时调用一次
        /// 用途：初始化状态参数、播放动画、重置计时器等
        /// </summary>
        public abstract void Enter();

        /// <summary>
        /// 状态运行时每帧调用（Update）
        /// 用途：检测输入、更新动画参数、状态转换逻辑
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// 状态运行时每物理帧调用（FixedUpdate）
        /// 用途：物理移动、施加力、碰撞检测等
        /// </summary>
        public abstract void FixedUpdate();

        /// <summary>
        /// 离开状态时调用一次
        /// 用途：清理资源、重置参数、停止效果等
        /// </summary>
        public abstract void Exit();
    }
}
```

**设计说明**：

- **抽象基类**：定义所有状态的公共接口
- **生命周期方法**：Enter → Update/FixedUpdate → Exit，清晰的状态执行流程
- **受保护引用**：子类可以直接访问`player`和`stateMachine`
- **强制实现**：abstract关键字确保子类必须实现所有方法

---

#### 3.7 实现PlayerStateMachine状态机 (Implementing State Machine)

打开 `PlayerStateMachine.cs` 文件,替换为以下代码：

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace DeadCells.Player
{
    /// <summary>
    /// 玩家状态机 - 管理状态注册、切换和更新
    /// 采用字典存储状态，支持通过名称快速查找
    /// </summary>
    public class PlayerStateMachine
    {
        // === 状态存储 ===

        /// <summary>当前活跃状态</summary>
        public PlayerState CurrentState { get; private set; }

        /// <summary>所有注册的状态（状态名 → 状态实例）</summary>
        private Dictionary<string, PlayerState> states = new Dictionary<string, PlayerState>();

        // === 状态快速访问属性（供PlayerController使用） ===

        public PlayerState IdleState => GetState("Idle");
        public PlayerState MoveState => GetState("Move");
        public PlayerState JumpState => GetState("Jump");
        public PlayerState FallState => GetState("Fall");
        public PlayerState AttackState => GetState("Attack");
        public PlayerState RollState => GetState("Roll");

        // === 初始化 ===

        /// <summary>
        /// 初始化状态机并设置初始状态
        /// </summary>
        /// <param name="startingState">初始状态（通常是IdleState）</param>
        public void Initialize(PlayerState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();

            Debug.Log($"[StateMachine] 初始化，进入状态: {GetStateName(CurrentState)}");
        }

        // === 状态注册 ===

        /// <summary>
        /// 注册状态到状态机
        /// </summary>
        /// <param name="stateName">状态名称（如"Idle"、"Jump"）</param>
        /// <param name="state">状态实例</param>
        public void RegisterState(string stateName, PlayerState state)
        {
            if (!states.ContainsKey(stateName))
            {
                states.Add(stateName, state);
            }
            else
            {
                Debug.LogWarning($"[StateMachine] 状态已存在: {stateName}，跳过注册");
            }
        }

        /// <summary>
        /// 通过名称获取状态
        /// </summary>
        private PlayerState GetState(string stateName)
        {
            if (states.TryGetValue(stateName, out PlayerState state))
            {
                return state;
            }

            Debug.LogError($"[StateMachine] 未找到状态: {stateName}");
            return null;
        }

        // === 状态切换 ===

        /// <summary>
        /// 切换到新状态（核心方法）
        /// </summary>
        /// <param name="newState">目标状态</param>
        public void ChangeState(PlayerState newState)
        {
            if (newState == null)
            {
                Debug.LogError("[StateMachine] 尝试切换到null状态！");
                return;
            }

            // 避免切换到相同状态
            if (CurrentState == newState)
            {
                return;
            }

            // 退出当前状态
            string oldStateName = GetStateName(CurrentState);
            CurrentState?.Exit();

            // 进入新状态
            CurrentState = newState;
            CurrentState.Enter();

            string newStateName = GetStateName(newState);
            Debug.Log($"[StateMachine] 状态切换: {oldStateName} → {newStateName}");
        }

        // === 更新循环 ===

        /// <summary>
        /// 每帧更新当前状态（由PlayerController的Update调用）
        /// </summary>
        public void Update()
        {
            CurrentState?.Update();
        }

        /// <summary>
        /// 每物理帧更新当前状态（由PlayerController的FixedUpdate调用）
        /// </summary>
        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }

        // === 辅助方法 ===

        /// <summary>
        /// 获取状态的名称（用于调试）
        /// </summary>
        private string GetStateName(PlayerState state)
        {
            if (state == null) return "None";

            foreach (var kvp in states)
            {
                if (kvp.Value == state)
                {
                    return kvp.Key;
                }
            }

            return state.GetType().Name;
        }
    }
}
```

**实现要点**：

- **字典存储**：使用`Dictionary<string, PlayerState>`快速查找状态
- **属性访问**：提供IdleState等属性，方便状态间切换
- **防重复切换**：避免切换到相同状态，减少不必要的Enter/Exit调用
- **调试支持**：所有状态切换都输出日志，便于排查问题

---

#### 3.8 实现PlayerController核心控制器 (Implementing Core Controller)

**文件**: `PlayerController.cs` (~450行)

**核心职责**:
- 管理所有Unity组件引用 (Rigidbody2D, Animator, PlayerInput)
- 初始化状态机和所有状态实例
- 执行物理检测 (地面检测、墙壁检测)
- 提供状态类使用的公共方法
- 在Unity生命周期中驱动状态机

**关键实现要点**:

```csharp
// 1. 组件引用管理
private Rigidbody2D rb;
private Animator animator;
private PlayerInput input;
private PlayerStateMachine stateMachine;

// 2. 可配置参数 (通过Inspector暴露)
[Header("Movement")]
public float moveSpeed = 8f;
public float jumpForce = 16f;

[Header("Advanced Movement")]
public float doubleJumpForce = 16f;
public float wallSlideSpeed = 2f;
public Vector2 wallJumpForce = new Vector2(10f, 18f);

[Header("Ground Detection")]
public Transform groundCheck;
public LayerMask groundLayer;
public float groundCheckRadius = 0.2f;

[Header("Wall Detection")]
public Transform wallCheck;
public LayerMask wallLayer;
public float wallCheckDistance = 0.5f;

// 3. 运行时状态
public bool IsGrounded { get; private set; }
public bool IsTouchingWall { get; private set; }
public int FacingDirection { get; private set; } = 1;
public bool HasDoubleJump { get; set; }

// 4. 核心方法
void Awake()
{
    // 获取组件
    // 创建状态机
    // 初始化所有6个状态实例
    // 注册状态到状态机
}

void Start()
{
    // 进入初始状态 (Idle)
    stateMachine.Initialize(stateMachine.IdleState);
}

void Update()
{
    // 执行物理检测
    CheckGrounded();
    CheckWallContact();

    // 驱动状态机Update
    stateMachine.Update();

    // 更新Animator参数
    UpdateAnimatorParameters();
}

void FixedUpdate()
{
    // 驱动状态机FixedUpdate (物理更新)
    stateMachine.FixedUpdate();
}

// 5. 提供给状态类的公共方法
public void SetVelocityX(float x) { /* 设置水平速度 */ }
public void SetVelocityY(float y) { /* 设置垂直速度 */ }
public void Flip() { /* 翻转角色朝向 */ }
```

**关键设计决策**:
- 使用属性而非公共字段暴露运行时状态 (IsGrounded等)
- 物理检测在PlayerController中执行,状态类只读取结果
- 所有状态实例在Awake()中创建并注册
- 状态机驱动由PlayerController负责,状态类不直接调用Unity生命周期

---

#### 3.9 实现PlayerStates所有状态类 (Implementing All State Classes)

**文件**: `PlayerStates.cs` (~400行,包含6个状态类)

**状态清单**:

1. **IdleState** (~80行)
   - 检测移动输入 → 切换到Move
   - 检测跳跃输入 → 切换到Jump
   - 检测离地 → 切换到Fall
   - 检测攻击/翻滚 → 切换到对应状态

2. **MoveState** (~100行)
   - FixedUpdate: 施加水平速度
   - 检测停止移动 → 切换到Idle
   - 检测跳跃 → 切换到Jump
   - 检测离地 → 切换到Fall
   - 自动翻转角色朝向

3. **JumpState** (~90行)
   - Enter: 施加向上力
   - FixedUpdate: 可选空中水平控制
   - 检测velocityY < 0 → 切换到Fall
   - 检测攻击 → 切换到Attack (空中攻击)

4. **FallState** (~120行)
   - 检测着陆+静止 → 切换到Idle
   - 检测着陆+移动 → 切换到Move
   - 检测攻击 → 切换到Attack

5. **AttackState** (~100行)
   - Enter: 触发attack动画Trigger
   - 可选: 攻击期间锁定移动
   - 可选: 检测连招输入
   - 动画结束后自动返回Idle (通过Animator或脚本)

6. **RollState** (~90行)
   - Enter: 触发roll动画Trigger
   - FixedUpdate: 施加翻滚冲刺速度
   - 翻滚期间无敌 (可选,需集成HitstunController)
   - 动画结束后返回Idle

**实现模式统一性**:

```csharp
// 每个状态类都遵循此结构
public class XxxState : PlayerState
{
    // 构造函数
    public XxxState(PlayerController player, PlayerStateMachine stateMachine)
        : base(player, stateMachine) { }

    // Enter: 初始化状态,设置Animator参数
    public override void Enter()
    {
        // 设置动画参数
        // 重置状态变量
        // 执行一次性动作 (如施加跳跃力)
    }

    // Update: 检测输入,更新Animator,状态转换判定
    public override void Update()
    {
        // 读取player.Input的输入
        // 持续更新Animator的Bool/Float参数
        // 检测状态转换条件,调用stateMachine.ChangeState()
    }

    // FixedUpdate: 物理移动
    public override void FixedUpdate()
    {
        // 通过player.SetVelocityX/Y修改速度
        // 或直接访问player.Rigidbody.velocity
    }

    // Exit: 清理状态
    public override void Exit()
    {
        // 重置Animator参数
        // 恢复被修改的物理参数 (如重力)
    }
}
```

**关键设计决策**:
- 所有输入通过`player.Input`读取,不直接使用Input System
- 所有物理修改通过`player.Rigidbody`进行
- 所有动画通过`player.Animator`控制
- 状态转换使用`stateMachine.ChangeState(stateMachine.XxxState)`

---

#### 3.10 Unity Editor中配置脚本 (Linking Scripts in Unity Editor)

**步骤1: 添加组件到Player GameObject**

1. 选中Hierarchy中的Player GameObject
2. Add Component → 搜索`Player Input`
3. Add Component → 搜索`Player Controller`

**步骤2: 配置PlayerController Inspector参数**

| 参数组 | 参数名 | 推荐值 | 说明 |
|-------|--------|--------|------|
| **Movement** | Move Speed | 8 | 水平移动速度 |
| | Jump Force | 16 | 跳跃力度 |
| **Advanced Movement** | Double Jump Force | 16 | 二段跳力度 |
| | Wall Slide Speed | 2 | 滑墙速度 |
| | Wall Jump Force | (10, 18) | 蹬墙跳力度向量 |
| **Ground Detection** | Ground Check | 拖入GroundCheck子对象 | 必须分配 |
| | Ground Layer | 选择Ground层 | 必须配置 |
| | Ground Check Radius | 0.2 | 检测半径 |
| **Wall Detection** | Wall Check | 拖入WallCheck子对象 | 必须分配 |
| | Wall Layer | 选择Ground层 | 通常与地面相同 |
| | Wall Check Distance | 0.3 | 检测距离 |

**步骤3: 创建检测点子对象**

1. 右键Player → Create Empty → 命名为`WallCheck`
2. Transform Position设置为`(0.4, 0, 0)` (角色身体右侧)
3. 将WallCheck拖入PlayerController的Wall Check字段

**步骤4: 验证配置**

```
✅ Player GameObject
   ├─ Player Input (组件)
   ├─ Player Controller (组件)
   │  ├─ Ground Check: GroundCheck (已分配)
   │  ├─ Wall Check: WallCheck (已分配)
   │  ├─ Ground Layer: Ground (已选择)
   │  └─ Wall Layer: Ground (已选择)
   ├─ Rigidbody2D (阶段1已配置)
   ├─ Animator (阶段2已配置)
   ├─ Capsule Collider 2D (阶段1已配置)
   │
   ├─ GroundCheck (子对象)
   │  └─ Position: (0, -0.65, 0)
   │
   └─ WallCheck (子对象)
      └─ Position: (0.4, 0, 0)
```

**步骤5: 分配PlayerInputActions资源**

PlayerInput组件会自动创建PlayerInputActions实例,无需手动分配。

---

#### 3.11 测试验证和调优 (Testing and Verification)

**键盘测试清单**:

```
基础移动:
✅ 按A/D或方向键左右,角色移动
✅ 松开键,角色停止
✅ 角色朝向正确翻转

跳跃系统:
✅ 按Space,角色跳跃
✅ 空中再按Space,触发二段跳
✅ 二段跳后再按Space,无反应
✅ 着陆后二段跳机会恢复

墙壁交互:
✅ 空中贴墙,触发滑墙动画
✅ 滑墙时按Space,触发蹬墙跳
✅ 蹬墙跳方向正确 (离开墙壁)

战斗动作:
✅ 按鼠标左键,触发攻击动画
✅ 攻击动画播放完毕后返回Idle
✅ 按Left Shift,触发翻滚动画
✅ 翻滚期间角色向前冲刺
```

**手柄测试清单** (Xbox/PlayStation):

```
基础移动:
✅ 推动左摇杆,角色移动
✅ 摇杆死区正确 (轻微推动无反应)

按键映射:
✅ A键/✕键 - 跳跃和二段跳
✅ X键/□键 - 攻击
✅ B键/○键 - 翻滚

设备切换:
✅ 键盘输入时,PlayerInput.CurrentDevice = Keyboard
✅ 手柄输入时,PlayerInput.CurrentDevice = Gamepad
✅ 设备切换无延迟,立即生效
```

**常见问题排查**:

| 问题症状 | 可能原因 | 解决方案 |
|---------|---------|---------|
| 角色不响应输入 | PlayerInput组件未添加 | 检查组件是否存在 |
| 二段跳无法触发 | HasDoubleJump未在JumpState.Enter()中重置 | 检查状态代码 |
| 滑墙速度越来越快 | WallSlideState.Enter()中未取消重力 | 设置gravityScale=0 |
| 蹬墙跳方向错误 | 方向计算错误 | 检查jumpDirection=-wallDirection |
| 手柄无反应 | Input System包未安装或未配置 | 重新执行3.2步骤 |
| Console大量状态切换日志 | 正常现象 (调试日志) | 可注释Debug.Log语句 |

**性能验证**:

1. 打开Profiler (Window → Analysis → Profiler)
2. 运行游戏,观察CPU Usage
3. 检查PlayerStateMachine.Update()耗时应< 0.1ms
4. 检查Animator.Update耗时应< 0.5ms

---

#### 3.12 代码重构建议 (Code Refactoring Recommendations)

**重构方向1: 接口化状态系统**

**目的**: 提高状态的可组合性和可测试性

**当前问题**: 所有状态都继承抽象类PlayerState,缺乏灵活性

**重构方案**:
```csharp
// 定义核心接口
public interface IState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
}

// 定义能力接口
public interface IMovableState : IState
{
    float GetMoveSpeed();
}

public interface IAnimatedState : IState
{
    string GetAnimationName();
}

// 状态实现多个接口
public class MoveState : IState, IMovableState, IAnimatedState
{
    public float GetMoveSpeed() => 8f;
    public string GetAnimationName() => "Move";
    // ...
}
```

**优势**:
- 可以为不同状态组合不同能力
- 便于单元测试 (mock接口)
- 支持AI复用状态逻辑 (AI也可实现IMovableState)

---

**重构方向2: 事件驱动的动画系统**

**目的**: 解耦状态逻辑和动画逻辑

**当前问题**: 每个状态都直接调用Animator.SetTrigger(),耦合紧密

**重构方案**:
```csharp
// 在PlayerStateMachine中添加事件
public event Action<string> OnStateEntered;

public void ChangeState(PlayerState newState)
{
    CurrentState?.Exit();
    CurrentState = newState;
    CurrentState.Enter();

    // 发布事件
    OnStateEntered?.Invoke(GetStateName(newState));
}

// 独立的动画控制器订阅事件
public class PlayerAnimationController : MonoBehaviour
{
    void Start()
    {
        player.StateMachine.OnStateEntered += OnStateChanged;
    }

    void OnStateChanged(string stateName)
    {
        // 根据状态名称播放动画
        animator.Play(stateName);
    }
}
```

**优势**:
- 动画逻辑独立,易于调整
- 可以同时触发音效、粒子效果
- 支持动画编辑器可视化配置

---

**重构方向3: 命令模式输入系统**

**目的**: 支持输入录制、回放、重绑定

**当前问题**: 输入检测和动作执行耦合在状态类中

**重构方案**:
```csharp
// 定义命令接口
public interface ICommand
{
    bool CanExecute(PlayerController player);
    void Execute(PlayerController player);
}

// 具体命令
public class JumpCommand : ICommand
{
    public bool CanExecute(PlayerController player)
    {
        return player.IsGrounded || player.HasDoubleJump;
    }

    public void Execute(PlayerController player)
    {
        if (player.IsGrounded)
            player.StateMachine.ChangeState(player.StateMachine.JumpState);
        else
            player.StateMachine.ChangeState(player.StateMachine.DoubleJumpState);
    }
}

// 输入处理器
public class PlayerInputHandler : MonoBehaviour
{
    private Dictionary<string, ICommand> commands;

    void Update()
    {
        if (input.JumpPressed && commands["Jump"].CanExecute(player))
        {
            commands["Jump"].Execute(player);
        }
    }
}
```

**优势**:
- 支持输入录制和回放 (重播系统)
- 支持运行时按键重绑定
- 便于实现教程模式 (禁用特定命令)
- 网络同步只需传输命令ID

---

**重构方向4: 数据驱动的状态配置**

**目的**: 将硬编码参数移到ScriptableObject

**当前问题**: 移动速度、跳跃力度等参数散落在代码中

**重构方案**:
```csharp
// 创建ScriptableObject
[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player Config")]
public class PlayerConfigData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 16f;

    [Header("Advanced")]
    public float doubleJumpForce = 16f;
    public float wallSlideSpeed = 2f;
}

// PlayerController引用配置
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerConfigData config;

    void ApplyJumpForce()
    {
        rb.velocity = new Vector2(rb.velocity.x, config.jumpForce);
    }
}
```

**优势**:
- 参数集中管理,易于调优
- 支持多角色配置 (不同角色不同参数)
- 可在运行时热更新参数 (用于平衡性调整)

---

**重构优先级建议**:

| 重构方向 | 优先级 | 时机 | 复杂度 |
|---------|-------|------|--------|
| **数据驱动配置** | 高 | 立即 | 低 |
| **事件驱动动画** | 中 | 动画系统稳定后 | 中 |
| **命令模式输入** | 低 | 需要回放/网络时 | 高 |
| **接口化状态** | 低 | 需要AI/多角色时 | 中 |

**迭代建议**:
1. **第一阶段**: 使用当前架构完成核心玩法验证
2. **第二阶段**: 应用数据驱动配置,便于参数调优
3. **第三阶段**: 根据需求选择性应用其他重构方案

---

### 阶段 4：场景设置 (Scene Setup)

#### 4.1 地面层配置 (Ground Layer Configuration)
- 确保场景中有带碰撞器的地面对象
- 创建或使用现有的 `Ground` 层
- 将地面对象分配到此层
- 在 PlayerController 中设置 `Ground Layer Mask`

#### 4.2 摄像机设置 (Camera Setup)（可选）
- 配置 Main Camera 跟随 Player transform
- 使用 Cinemachine 或自定义跟随脚本
- 调整正交大小以获得合适的视图

### 阶段 5：测试和调优 (Testing and Tuning)

#### 5.1 基础功能测试 (Basic Function Tests)
测试所有输入控制：
- 左/右移动（A/D 或方向键）
- 跳跃（空格键）- 测试 coyote time 和 jump buffer
- 攻击（鼠标左键）
- 翻滚/闪避（左 Shift）
- 角色翻转（应面向移动方向）

#### 5.2 参数调优 (Parameter Tuning)
调整以获得最佳游戏手感：

**移动手感 (Movement Feel)**：
- Move Speed: 更高 = 更快的移动
- Jump Force: 更高 = 跳得更高
- Gravity Scale: 更高 = 更重的感觉，更快的下落

**跳跃机制 (Jump Mechanics)**：
- Coyote Time: 0.15-0.25s（平台游戏标准）
- Jump Buffer Time: 0.1-0.15s（输入宽容度）

**物理 (Physics)**：
- Collider size: 应紧密匹配视觉精灵
- Ground Check Radius: 应可靠地检测地面
- Friction: 如果发生滑动，调整 Physics Material

#### 5.3 动画打磨 (Animation Polish)
- 验证动画循环设置（循环 vs 一次性）
- 在 Animator 中调整动画速度
- 微调状态之间的转换时间
- 检查状态变化期间的动画抖动

### 阶段 6：可选增强 (Optional Enhancements)

#### 6.1 战斗系统集成 (Combat System Integration)

**⚠️ 重要：程序集依赖要求**

战斗系统组件（武器、生命值、受击控制）位于 `DeadCells.Combat` 程序集中。如果你的 PlayerController 脚本在 `DeadCells.Player` 程序集中，需要先添加程序集引用。

**方法 1：修改程序集定义（推荐用于脚本引用）**

1. **找到 Player 程序集定义文件**
   - 路径：`Assets/Scripts/DeadCells.Player/DeadCells.Player.asmdef`

2. **添加 Combat 程序集引用**
   - 在 Unity 中选中该文件
   - Inspector 中找到 `Assembly Definition References` 部分
   - 点击 `+` 添加引用
   - 选择 `DeadCells.Combat`
   - 点击 Apply

3. **验证依赖关系**
   ```
   DeadCells.Player 现在可以引用:
   ├─ DeadCells.Core ✅ (原有)
   └─ DeadCells.Combat ✅ (新添加)
   ```

**方法 2：直接在 Inspector 添加组件（推荐用于 GameObject 组件）**

如果你只是想在 Player GameObject 上添加 Combat 组件，而不需要在 Player 脚本中引用 Combat 类型：
- 无需修改 .asmdef
- 直接在 Inspector 中 Add Component 即可
- Unity 会自动处理跨程序集组件引用

---

**武器配置步骤（需要方法 1）**：

如果角色需要武器系统：

1. **创建武器子 GameObject**
   - 在 Hierarchy 中右键 Player → Create Empty
   - 重命名为 `Weapon`
   - 添加 SpriteRenderer 并分配武器精灵

2. **实现武器组件**
   - 创建继承自 `IWeaponConfigurable` 的脚本
   - 遵循武器配置指南（参见本文档"武器配置系统"部分）
   - 示例：
     ```csharp
     using DeadCells.Combat; // 需要 Combat 程序集引用
     using DeadCells.Data;

     public class PlayerMeleeWeapon : MonoBehaviour, IMeleeWeaponConfigurable
     {
         public void ConfigureWeapon(WeaponData data) { /* 实现 */ }
         public bool SupportsWeaponType(string type) { return type == "melee"; }
         public void ConfigureMeleeProperties(float range, float knockback) { /* 实现 */ }
     }
     ```

3. **使用 WeaponConfigurationManager 配置**
   ```csharp
   using DeadCells.Combat; // 需要 Combat 程序集引用
   using DeadCells.Data;

   // 在 PlayerController 或武器管理器中
   WeaponData weaponData = /* 从 CastleDB 加载 */;
   var results = WeaponConfigurationManager.ConfigureWeapon(weaponGameObject, weaponData);

   // 检查配置结果
   if (results.Any(r => !r.Success))
   {
       Debug.LogWarning(WeaponConfigurationManager.GetConfigurationSummary(results));
   }
   ```

#### 6.2 生命值系统 (Health System)

**⚠️ 程序集依赖要求：同 6.1**

添加伤害/生命值机制：

**方法 A：仅添加组件（无需修改 .asmdef）**

1. 选中 Player GameObject
2. Add Component → 搜索 `Health`（来自 DeadCells.Combat）
3. 配置 Health 组件：
   - Max Health: 100
   - Current Health: 100
   - Invincibility Frames: 0.5s

4. Add Component → 搜索 `HitstunController`
5. 配置受击反应：
   - Hitstun Duration: 0.3s
   - Knockback Multiplier: 1.0

**方法 B：在脚本中引用（需要修改 .asmdef）**

如果需要在 PlayerController 中访问 Health 组件：

```csharp
using DeadCells.Combat; // 需要按方法 1 添加程序集引用

public class PlayerController : MonoBehaviour
{
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    void HandleDeath()
    {
        // 处理玩家死亡
        stateMachine.ChangeState(deathState);
    }
}
```

**⚠️ 依赖关系总结**：

| 使用场景 | 是否需要修改 .asmdef | 说明 |
|---|---|---|
| 在 Inspector 添加 Combat 组件 | ❌ 不需要 | Unity 自动处理 |
| 在 Player 脚本中使用 `using DeadCells.Combat` | ✅ 需要 | 必须添加程序集引用 |
| 实现 `IWeaponConfigurable` 接口 | ✅ 需要 | 接口定义在 Combat 中 |
| 调用 `WeaponConfigurationManager` | ✅ 需要 | 静态类在 Combat 中 |

#### 6.3 视觉效果 (Visual Effects)
通过效果增强：
- 跳跃/着陆粒子（灰尘、冲击）
- 移动轨迹
- 通过 `EffectsManager` 的攻击效果
- 动作音效

### 常见问题和解决方案 (Common Issues and Solutions)

**角色不移动**：
- 检查 Rigidbody2D 是 Dynamic
- 验证输入系统是否工作（PlayerInput.cs）
- 检查是否有冲突的脚本修改速度

**穿过地面**：
- 验证地面有 Collider2D
- 检查碰撞矩阵（Edit → Project Settings → Physics 2D）
- 确保 Ground Layer Mask 正确设置
- 尝试 Continuous 碰撞检测

**动画不播放或 NullReferenceException**：
- **验证 Animator 组件存在**：
  - 检查 Player GameObject 上是否已添加 Animator 组件（阶段 2.3）
  - 确认 Animator 组件的 Controller 字段已分配 Controller 资源
- **检查参数名称**：
  - Animator 参数与代码完全匹配（区分大小写）
  - 在 Animator 窗口的 Parameters 标签中确认所有参数已创建
- **调试步骤**：
  - 在 Animator 窗口中运行游戏，观察参数实时变化
  - 添加空引用检查：`if (animator == null) Debug.LogError("Animator is null!");`
  - 确保动画剪辑已分配给 Animator Controller 的状态

**动画卡死或循环错误动画**：
- **问题 1：多个状态设置相同的 Bool 参数**
  - 症状：`isMoving` 恒为 true，角色一直播放移动动画
  - 解决：只在 MoveState 的 Enter()/Exit() 中控制 isMoving，其他状态不要修改
- **问题 2：在 Update() 中重复触发 Trigger**
  - 症状：攻击动画每帧重新开始，产生抖动
  - 解决：`SetTrigger("attack")` 只在 AttackState.Enter() 中调用一次
- **问题 3：Animator 转换条件不完整**
  - 症状：在地面上触发跳跃动画
  - 解决：跳跃应通过脚本输入检测实现，参见阶段 2.4 的输入驱动逻辑

**状态机与动画不同步**：
- **症状**：角色状态已切换到 Jump，但动画仍播放 Idle
- **原因**：PlayerStateMachine 和 Animator 使用不同的判定条件
- **解决**：
  - 确保所有状态的 Update() 都持续更新 `isGrounded` 和 `velocityY`
  - Animator 转换条件应与 PlayerStateMachine 状态切换逻辑一致
  - 示例：如果脚本通过 `velocity.y > 0` 判定跳跃，Animator 也应使用 `velocityY > 0.1`

**编译错误："找不到类型 IWeaponConfigurable"**：
- **原因**：PlayerController 脚本尝试引用 `DeadCells.Combat`，但程序集依赖未配置
- **解决**：
  - 按照阶段 6.1 的"方法 1"添加程序集引用
  - 或者将武器代码移到 DeadCells.Game 程序集（该程序集引用所有模块）

**跳跃感觉不对**：
- 调整 Gravity Scale（更高 = 更重）
- 调整 Jump Force（以 2 为增量测试）
- 修改 Rigidbody2D Linear Drag 以获得空中控制
- 考虑可变跳跃高度（按住跳跃键跳得更高）

**角色翻转不正确**：
- PlayerController 使用 Y 轴旋转（0° 或 180°）
- 确保精灵在原始艺术中面向右侧
- 检查子对象是否干扰旋转
- 如果需要，考虑使用 localScale.x 翻转

### 角色特定注意事项 (Character-Specific Considerations)

**精灵导入设置 (Sprite Import Settings)**：
- Sprite Mode: Multiple（用于精灵表）
- Pixels Per Unit: 匹配项目标准（像素艺术通常为 16 或 32）
- Filter Mode: Point（像素艺术）或 Bilinear（高清艺术）
- Compression: None（像素艺术质量）

**性能 (Performance)**：
- 保持动画剪辑数量合理
- 使用精灵图集以获得更好的批处理
- 避免在 Update() 中进行过多的物理计算

**艺术要求 (Art Requirements)**：
- 最少动画：Idle, Run, Jump, Fall
- 推荐添加：Attack, Hurt, Die, Roll
- 可选：Crouch, Climb, 特殊动作
- 确保动画之间精灵尺寸一致
