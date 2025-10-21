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

> 目标：以 Unity Animator 与脚本状态机协同驱动玩家 Idle / Walk / Run / Jump / Fall / Crouch / CrouchWalk / Roll / Attack / Climb 等基础动作，同时为 Dash、Hurt 等扩展动作预留空间。

#### 2.1 资源与目录结构
- 【Unity 操作】确认动画剪辑已放置在 Assets/Animations/Player/MainCharacter/：
  - Player_Idle.anim
  - Player_Walk.anim（若暂无，可复制 Run 动画并调低速度）
  - Player_Run.anim
  - Player_Jump.anim
  - Player_Fall.anim
  - Player_Attack.anim
  - Player_Roll.anim
  - Player_Crouch.anim
  - Player_CrouchWalk.anim
  - Player_ClimbIdle.anim、Player_ClimbMove.anim（若资源未就绪，可先保留空剪辑）
- 【Unity 操作】Animator 控制器统一使用 Assets/Animations/Player/MainCharacter/Player.controller。

#### 2.2 创建 Animator 参数
- 【Unity 操作】打开 Animator（Window > Animation > Animator），选中 Player 控制器。
- 在 Parameters 面板中新增：
  1. Float Speed（默认 0，归一化水平速度：0=静止，≈0.4=行走，≈1=跑步）
  2. Float VerticalVelocity（默认 0，读取 Rigidbody2D.y）
  3. Bool IsGrounded（默认 true）
  4. Bool IsCrouching（默认 false）
  5. Bool IsClimbing（默认 false）
  6. Trigger Attack
  7. Trigger Roll
  8. （可选）Trigger Hurt、Death

#### 2.3 Grounded 子状态机（Idle / Walk / Run）
- 【Unity 操作】在 Base Layer 新建子状态机 Grounded，将 Entry 连接到该子状态机。
- Grounded 内创建 Blend Tree：右键 → Create State → From New Blend Tree，命名 Grounded_Locomotion。
  - 打开 Blend Tree 面板，设置 Blend Type = 1D，Parameter = Speed。
  - 添加 Motion：Player_Idle.anim（Threshold 0）、Player_Walk.anim（0.4）、Player_Run.anim（1）。
  - 若暂时无 Walk 动画，可仅保留 Idle(0) 与 Run(1)。
- 通过 Speed 参数即可在 Idle / Walk / Run 之间平滑过渡，无需额外状态。

#### 2.4 下蹲与下蹲行走
- 【Unity 操作】在 Grounded 内创建状态 Player_Crouch、Player_CrouchWalk，分别指定对应动画。
- 配置转换：
  - Grounded_Locomotion → Player_Crouch：Has Exit Time = false；条件：IsCrouching == true 且 Speed <= 0.1。
  - Player_Crouch → Grounded_Locomotion：条件：IsCrouching == false。
  - Player_Crouch → Player_CrouchWalk：条件：IsCrouching == true 且 Speed > 0.1。
  - Player_CrouchWalk → Player_Crouch：条件：Speed <= 0.1。
  - Player_CrouchWalk → Grounded_Locomotion：条件：IsCrouching == false。

#### 2.5 翻滚与攻击
- 【Unity 操作】在 Base Layer 添加 Any State → Player_Roll：Has Exit Time = false，条件触发器为 Roll。
- Player_Roll 状态设置为 Has Exit Time = true，Exit Time ≈ 0.95，Fixed Duration 关闭。
  - 退出分支：
    * → Grounded_Locomotion：IsGrounded == true 且 IsCrouching == false（可按 Speed 分流至 Idle / Run）。
    * → Player_Crouch：IsGrounded == true 且 IsCrouching == true。
    * → Airborne 子状态机：IsGrounded == false。
- Any State → Player_Attack 配置类似（Trigger = Attack）。
- 【Unity 操作】在 Player_Attack.anim、Player_Roll.anim 末帧添加动画事件：分别调用 OnAttackComplete、OnRollComplete（于阶段 3 中实现）。

#### 2.6 空中状态 (Airborne)
- 【Unity 操作】新建子状态机 Airborne，包含 Player_Jump（Entry）、Player_Fall。
- 转换配置：
  - Grounded → Airborne.Player_Jump：Has Exit Time = false，条件：IsGrounded == false 且 VerticalVelocity > 0.1。
  - Player_Jump → Player_Fall：条件：VerticalVelocity <= 0。
  - Airborne → Grounded_Locomotion：条件：IsGrounded == true 且 IsCrouching == false。
  - Airborne → Player_Crouch：条件：IsGrounded == true 且 IsCrouching == true。

#### 2.7 攀爬子状态机
- 【Unity 操作】创建子状态机 Climb，包含 Player_ClimbIdle、Player_ClimbMove。
- 转换配置：
  - Any State → Climb.Player_ClimbIdle：Has Exit Time = false，条件：IsClimbing == true。
  - Player_ClimbIdle → Player_ClimbMove：条件：IsClimbing == true 且 Speed > 0.1（如需独立参数，可在阶段 3 中扩展 ClimbSpeed）。
  - Climb → Grounded_Locomotion：条件：IsClimbing == false 且 IsGrounded == true。
  - Climb → Airborne：条件：IsClimbing == false 且 IsGrounded == false。

#### 2.8 检查清单
- 【Unity 操作】进入 Play Mode，打开 Animator 窗口确认：
  1. 所有状态的 Motion 已指向正确动画。
  2. 转换均设置合理条件，只有必要状态启用 Has Exit Time。
  3. Any State 仅指向 Attack / Roll（以及 Climb 时的入口）。
  4. Speed 变化驱动 Idle → Walk → Run，IsCrouching 触发下蹲流程。
  5. IsGrounded、IsClimbing 在 Animator 面板中能随场景状态正确切换。

### 阶段 3：脚本集成 (Script Integration)

> 目标：实现可扩展的玩家状态机架构，统一管理动画参数更新、输入处理和物理逻辑，使动画切换完全由状态驱动。

#### 3.1 脚本目录规划
- 【Claude 实现】在 Assets/Scripts/DeadCells.Player/StateMachine/ 建议创建：
  - PlayerStateMachine.cs
  - PlayerState.cs（抽象基类）、PlayerStateId.cs（枚举）
  - States/Grounded/：PlayerLocomotionState.cs、PlayerCrouchState.cs、PlayerCrouchWalkState.cs、PlayerRollState.cs、PlayerAttackState.cs
  - States/Airborne/：PlayerJumpState.cs、PlayerFallState.cs
  - States/Climb/：PlayerClimbIdleState.cs、PlayerClimbMoveState.cs
  - 支撑类：PlayerAnimatorBridge.cs、PlayerContext.cs
- 【Claude 实现】所有脚本使用 
amespace DeadCells.Player。

#### 3.2 PlayerContext 与 Animator Bridge
- 【Claude 实现】PlayerContext 封装：PlayerController、Rigidbody2D、Collider2D、Animator、PlayerInput、PlayerMovementConfig、地面与攀爬检测组件。
  - 常用方法示例：
    `csharp
    void SetHorizontalVelocity(float value);
    void ApplyJump(float force);
    void ResizeCollider(Vector2 size, Vector2 offset, float smoothTime);
    bool HasHeadroom();
    void TriggerAttack();
    void TriggerRoll();
    `
- 【Claude 实现】PlayerAnimatorBridge 负责：
  - 缓存 Animator 参数哈希值（Speed、VerticalVelocity、IsGrounded、IsCrouching、IsClimbing、Attack、Roll）。
  - 在 UpdateContinuousParameters() 中每帧同步连续参数。
  - 提供触发器方法，内部先 ResetTrigger 再 SetTrigger，避免重复触发。

#### 3.3 PlayerStateMachine 设计
- 【Claude 实现】示例结构：
  `csharp
  public sealed class PlayerStateMachine
  {
      readonly Dictionary<PlayerStateId, PlayerState> states;
      public PlayerState CurrentState { get; private set; }

      public PlayerStateMachine(PlayerContext context)
      {
          states = new Dictionary<PlayerStateId, PlayerState>
          {
              { PlayerStateId.Locomotion, new PlayerLocomotionState(context, this) },
              // 其余状态依次注册
          };
      }

      public void Initialize(PlayerStateId initialState)
      {
          ChangeState(initialState);
      }

      public void ChangeState(PlayerStateId nextState)
      {
          if (CurrentState == states[nextState]) return;
          CurrentState?.Exit();
          CurrentState = states[nextState];
          CurrentState.Enter();
      }

      public void Update() => CurrentState?.UpdateLogic();
      public void FixedUpdate() => CurrentState?.UpdatePhysics();
  }
  `
- 状态切换优先级（建议）：Hurt / Death（预留） > Roll > Attack > Climb > Jump > Fall > Crouch / CrouchWalk > Locomotion。

#### 3.4 状态基类与接口
- 【Claude 实现】PlayerState 基类定义：
  `csharp
  public abstract class PlayerState
  {
      protected PlayerContext Context { get; }
      protected PlayerStateMachine Machine { get; }

      protected PlayerState(PlayerContext context, PlayerStateMachine machine)
      {
          Context = context;
          Machine = machine;
      }

      public virtual void Enter() {}
      public virtual void Exit() {}
      public virtual void UpdateLogic() {}
      public virtual void UpdatePhysics() {}
      public virtual bool IsCrouching => false;
      public virtual bool IsClimbing => false;
  }
  `
- 需要时可定义 ICrouchState / IClimbState 接口，方便 PlayerAnimatorBridge 判断。

#### 3.5 各状态核心职责
- **Locomotion（Idle / Walk / Run）**
  - UpdateLogic()：根据输入检测 Attack、Roll、Crouch、Jump、Fall 的触发条件，并调用 Machine.ChangeState。
  - UpdatePhysics()：Context.SetHorizontalVelocity(input.Move.x * config.RunSpeed)。
- **Crouch / CrouchWalk**
  - Enter()：设置 IsCrouching，调整碰撞体尺寸（来自配置），将水平速度清零。
  - UpdateLogic()：松开下蹲且 Context.HasHeadroom() → 回到 Locomotion；保持下蹲并检测水平输入 → 切换 CrouchWalk。
  - UpdatePhysics()：应用 config.CrouchSpeed。
  - Exit()：恢复碰撞体尺寸。
- **Jump / Fall**
  - Jump Enter()：Context.ApplyJump(config.JumpForce)，重置缓冲计时。
  - 两者 UpdateLogic()：允许空中触发 Attack / Roll；落地后根据 IsCrouching 与 Speed 选择返回状态。
  - UpdatePhysics()：按空中移动速度比例更新水平速度。
- **Attack**
  - Enter()：调用 Context.TriggerAttack()，记录动画持续时间或等待动画事件。
  - UpdateLogic()：计时结束或收到事件 → 根据地面状态切换到 Locomotion / Crouch / Fall。
- **Roll**
  - Enter()：调用 Context.TriggerRoll()，记录持续时间。
  - UpdatePhysics()：保持朝向方向的固定速度（取自配置）。
  - UpdateLogic()：时间结束后依据是否下蹲、是否在地面决定返回状态。
- **ClimbIdle / ClimbMove**
  - Enter()：暂时将 igidbody.gravityScale 设为 0，锁定 X 位置。
  - UpdatePhysics()：根据 input.ClimbAxis 和 config.ClimbSpeed 设置垂直速度。
  - Exit()：恢复重力，判断落点决定回到 Grounded 或 Airborne。

#### 3.6 输入系统扩展
- 【Claude 实现】丰富 PlayerInput：
  - Vector2 Move
  - bool JumpPressed, bool JumpHeld
  - bool CrouchHeld
  - bool AttackPressed
  - bool RollPressed
  - float ClimbAxis, bool ClimbHeld
- 若未来迁移至 Unity Input System，可保持接口不变，仅替换内部实现。

#### 3.7 配置数据 (ScriptableObject)
- 【Claude 实现】新增 PlayerMovementConfig（ScriptableObject）：
  - 字段示例：walkSpeed, unSpeed, crouchSpeed, jumpForce, ollSpeed, climbSpeed, coyoteTime, jumpBufferTime, crouchColliderSize, crouchColliderOffset。
  - PlayerController 在 Inspector 中引用配置，状态通过 Context.Config 读取。

#### 3.8 PlayerController 调整
- 【Claude 实现】在 PlayerController 中新增：PlayerContext context、PlayerAnimatorBridge animatorBridge、PlayerStateMachine stateMachine。
- Awake()：实例化 PlayerInput、context、nimatorBridge、stateMachine，调用 stateMachine.Initialize(PlayerStateId.Locomotion)。
- Update()：依次 input.Update()、更新地面 / 攀爬检测、nimatorBridge.UpdateContinuousParameters()、stateMachine.Update()。
- FixedUpdate()：stateMachine.FixedUpdate()。
- 响应动画事件：实现 OnAttackComplete()、OnRollComplete() 调回适当状态。

#### 3.9 测试与验证
- 【Claude 实现】可在 Assets/Scripts/DeadCells.Player/Tests/ 编写 EditMode 测试：
  - 模拟输入切换，验证状态机从 Idle → Locomotion → Jump → Fall → Grounded 的流程。
  - 使用虚拟 Animator 或桥接类断言参数更新是否正确。
- 【Unity 操作】在场景中实测：
  - Idle → Walk → Run 时 Speed 连续变化，动画 Blend Tree 工作正常。
  - 按下下蹲键 IsCrouching 变为 true，动画切换为下蹲 / 下蹲移动。
  - 触发 Attack / Roll，动画播放完毕后能回到正确状态。
  - 进入可攀爬区域时 IsClimbing 为 true，退出后恢复原状态。

#### 3.10 后续扩展建议
- 新增 Dash、Hurt、Death 等动作时步骤：
  1. Animator 中新增 Trigger 与状态，配置 Any State → 新状态。
  2. 状态机注册新状态类，定义进入 / 退出逻辑。
  3. 在 PlayerMovementConfig 添加相关数值。
- 所有 Animator 参数字符串集中存放在 PlayerAnimatorParams 静态类，避免拼写错误。
- Trigger 参数只在状态 Enter() 中调用一次，防止重复触发造成动画抖动。### 阶段 4：场景设置 (Scene Setup)

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

