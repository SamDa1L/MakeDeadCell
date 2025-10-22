# 玩家状态机迁移计划：脚本驱动 → Animator 驱动

**目标**：将现有的脚本驱动的`PlayerStateMachine`架构迁移到**Animator 驱动**的架构，通过 Unity 动画状态机完全掌控玩家行为。

**迁移日期**: 2025-10-20
**当前状态**: 100% 脚本驱动状态机
**目标状态**: 100% Animator 驱动，带 StateMachineBehaviour 物理处理

---

## 一、当前架构分析

### 脚本驱动流程（现存）
```
PlayerInput.Update()
    ↓
PlayerController.Update()
    ├─ CheckGrounded()
    ├─ UpdateTimers()
    ├─ animatorBridge.UpdateContinuousParameters()
    └─ stateMachine.Update()
        └─ currentState.UpdateLogic()
            ├─ 检查输入条件（AttackPressed, RollPressed 等）
            └─ 调用 stateMachine.ChangeState(newStateId)
                └─ 旧状态.Exit() → 新状态.Enter()

FixedUpdate():
    └─ stateMachine.FixedUpdate()
        └─ currentState.UpdatePhysics()
            ├─ SetHorizontalVelocity()
            ├─ Flip()
            └─ ResizeCollider()（仅Crouch状态）
```

### 关键问题
1. **双重状态管理**：PlayerStateMachine 和 Animator Controller 分别维护状态
2. **物理分散**：物理逻辑分散在各个PlayerState的UpdatePhysics()中
3. **参数同步复杂**：需在多个地方设置Animator参数
4. **事件处理困难**：攻击/翻滚完成事件难以准确同步
5. **扩展性受限**：新增功能需修改PlayerState基类和所有子类

---

## 二、目标架构设计

### Animator 驱动流程（新）

**重要说明**：Unity 2022 的 StateMachineBehaviour 不提供 `OnStateFixedUpdate()` 回调。物理逻辑需放在 PlayerController.FixedUpdate() 中直接调用，或通过 OnStateMove/OnStateUpdate 回调中驱动。以下为推荐架构：

```
PlayerInput.Update()
    ↓
PlayerController.Update()
    ├─ UpdateAnimatorParameters()
    │   ├─ Speed (归一化水平速度)
    │   ├─ VerticalVelocity
    │   ├─ IsGrounded
    │   ├─ IsCrouching (由Crouch SMB 在 OnStateEnter 中设置)
    │   ├─ IsClimbing (由 Climb SMB 在 OnStateEnter 中设置)
    │   └─ Attack/Roll Triggers (来自输入)
    └─ CheckGrounded()

Animator State Transitions
    └─ 参数变化 → Animator 自动转换状态
        └─ 新状态的 StateMachineBehaviour 回调执行：
            ├─ OnStateEnter(): 初始化状态（设置参数、调整碰撞体等）
            ├─ OnStateUpdate(): 逻辑帧更新（可选的轻量级逻辑）
            └─ OnStateExit(): 清理状态

PlayerController.FixedUpdate()
    └─ 根据当前 Animator 状态，调用相应的物理更新
        ├─ 通过 AnimatorStateInfo.shortNameHash 识别当前状态（11 个独立状态）
        └─ 调用 PlayerPhysicsController 执行状态特定的物理
            ├─ Idle / Walk / Run: 应用水平移动速度
            ├─ Crouch / CrouchWalk: 维持下蹲状态，应用下蹲速度
            ├─ Jump: 控制空中移动（上升阶段）
            ├─ Fall: 应用重力（下降阶段）
            ├─ Attack / Roll: 状态特定的移动逻辑
            └─ ClimbIdle / ClimbMove: 攀爬物理（禁用重力，垂直移动）

Animation Events (在动画时间轴上触发):
    └─ AttackComplete / RollComplete (动画末帧)
        └─ MonoBehaviour 回调或 StateMachineBehaviour.OnStateExit()
            └─ 设置标志，PrepareReturnToGround()
```

**关键**：
- ❌ 不存在 `OnStateFixedUpdate()` 回调
- ✅ 使用 `OnStateEnter()` 初始化、`OnStateUpdate()` 处理轻量级逻辑
- ✅ 物理逻辑集中在 **PlayerController.FixedUpdate()** 中
- ✅ 通过 Animator state hash 识别当前状态，调度相应的物理处理

### 核心概念：状态定义权从脚本转移到Animator
- **Animator State** 成为唯一真实来源
- **StateMachineBehaviour** 处理状态特定的物理/行为
- **PlayerController** 只负责输入采集和参数更新
- **PlayerPhysicsController** 处理通用物理逻辑

---

## 二点五、关键修复说明

### Unity 2022 中的两个阻塞性问题（已在本文档中修复）

#### 问题 1：StateMachineBehaviour.OnStateFixedUpdate() 不存在

**错误认知**：原计划中在 SMB 的 `OnStateFixedUpdate()` 中处理物理逻辑。

**实际情况**：Unity 2022 的 StateMachineBehaviour 中**不存在** `OnStateFixedUpdate()` 回调。仅提供以下回调：
- `OnStateEnter()` - 状态进入时调用
- `OnStateUpdate()` - 逻辑帧调用
- `OnStateMove()` - 根动画相关（通常不使用）
- `OnStateExit()` - 状态退出时调用

**修复方案**：
- ✅ **SMB 职责简化**：仅在 `OnEnter()`/`OnExit()` 中处理状态初始化/清理
- ✅ **物理逻辑集中**：所有物理操作转移到 **PlayerController.FixedUpdate()**
- ✅ **状态识别**：通过 `AnimatorStateInfo.shortNameHash` 识别当前状态，调度相应的物理处理
- 见上方"Animator 驱动流程（新）"架构图和"3.2 保留关键功能"中的 `HandlePhysicsForCurrentState()` 实现

#### 问题 2：animator.CompareTag() 检查错误

**错误认知**：原计划中使用 `animator.CompareTag("Crouching")` 检查 Animator 状态的 Tag。

**实际情况**：`animator.CompareTag()` 检查的是 **GameObject 的 Tag**，不是 **Animator 状态的 Tag**。这会导致无法正确识别状态。

**修复方案**：
- ✅ 使用 `AnimatorStateInfo.IsTag("Crouching")` 检查状态 Tag
- ✅ 或使用缓存的 Tag 哈希值：`stateInfo.IsTag(CROUCH_TAG_HASH)`
- ✅ 在 Animator 中为相关状态添加 Tag（例如为 Crouch 和 CrouchWalk 都添加 "Crouching" Tag）
- 见"7.1 Combat 系统"中的"方法 3"实现

---

## 三、具体迁移步骤

### 阶段 1：创建新的物理控制器组件

#### 1.1 创建 `PlayerPhysicsController.cs`

位置：`Assets/Scripts/DeadCells.Player/PlayerPhysicsController.cs`

职责：
- 管理所有物理操作（速度、跳跃、重力）
- 提供所有状态通用的物理方法
- 不依赖PlayerStateMachine

关键方法：
```csharp
public class PlayerPhysicsController : MonoBehaviour
{
    // 速度控制
    public void SetHorizontalVelocity(float velocity);
    public void SetVerticalVelocity(float velocity);
    public void AddHorizontalVelocity(float delta);
    public void AddVerticalVelocity(float delta);

    // 特定操作
    public void ApplyJump(float jumpForce);
    public void SetGravityScale(float scale);
    public void ResizeCollider(Vector2 newSize, Vector2 newOffset);
    public void RestoreColliderSize();
    public bool HasHeadroom(Vector2 checkSize);
    public void Flip();

    // 状态查询
    public float GetHorizontalSpeed();
    public float GetVerticalSpeed();
    public bool IsGrounded { get; }
    public bool FacingRight { get; }
}
```

#### 1.2 在 PlayerController 中使用 PlayerPhysicsController 实例

**重要**：使用 `[RequireComponent]` 特性而非运行时 AddComponent，以避免预制体中的重复组件。

```csharp
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PlayerPhysicsController))]  // 强制要求
public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerPhysicsController physicsController;

    private void Awake()
    {
        // GetComponent 将获取已有的组件或报错（若未在 Inspector 中挂载）
        physicsController = GetComponent<PlayerPhysicsController>();

        if (physicsController == null)
        {
            Debug.LogError("PlayerPhysicsController 未找到！请确保已在 Inspector 中添加该组件。");
        }
    }
}
```

**在 Inspector 中配置**：
1. 选中 Player GameObject
2. Add Component → PlayerPhysicsController
3. 确保 PlayerController 的 `physicsController` 字段已自动赋值

---

### 阶段 2：创建 StateMachineBehaviour 脚本

为每个 Animator 状态创建对应的 SMB，负责物理初始化和清理。

#### 2.1 通用基类 `PlayerStateBehaviour.cs`

**重要**：Unity 2022 StateMachineBehaviour 不提供 `OnStateFixedUpdate()` 回调。仅使用以下可用的钩子：

```csharp
namespace DeadCells.Player.StateMachine
{
    public abstract class PlayerStateBehaviour : StateMachineBehaviour
    {
        protected PlayerController playerController;
        protected PlayerPhysicsController physicsController;
        protected PlayerInput input;
        protected PlayerMovementConfig config;
        protected Animator animator;

        public sealed override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 首次初始化缓存
            if (playerController == null)
            {
                this.animator = animator;
                playerController = animator.GetComponent<PlayerController>();
                physicsController = animator.GetComponent<PlayerPhysicsController>();
                input = playerController.Input;
                config = playerController.MovementConfig;
            }

            OnEnter();
        }

        public sealed override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // OnStateUpdate 在逻辑帧调用，用于轻量级状态逻辑
            OnUpdate();
        }

        public sealed override void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // ⚠️ OnStateMove 在 Animator 帧评估期间调用
            // 仅用于根运动（Root Motion）处理，此项目暂不使用
            // ❌ 不要在此处放置任何物理逻辑 - 物理由 PlayerController.FixedUpdate() 集中处理
            // 若需要根运动功能，可在此处覆写实现
        }

        public sealed override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            OnExit();
        }

        // 子类覆写这些方法
        protected virtual void OnEnter() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnPhysicsUpdate() { }  // ❌ 不使用 - 仅为根运动预留，此项目不需要
        protected virtual void OnExit() { }
    }
}
```

**使用场景**：
- `OnEnter()`: 状态初始化（调整碰撞体大小、设置重力、清空速度等）
- `OnUpdate()`: 轻量级逻辑判断（检查转换条件）
- `OnPhysicsUpdate()`: 物理操作（仅当需要根动画驱动时，通常不使用）
- `OnExit()`: 状态清理

**重要提醒**：
- ❌ 不使用 `OnStateFixedUpdate()`，它在 Unity 2022 中**不存在**
- ✅ 物理逻辑应在 **PlayerController.FixedUpdate()** 中集中处理
- ✅ 在 SMB 中仅处理状态初始化/清理，具体物理由 PlayerController 调度

#### 2.2 具体状态 SMB 脚本

**重要**：这些 SMB 脚本**仅处理状态初始化和清理**。物理逻辑由 PlayerController.FixedUpdate() 调度。

**关键区分**：
- ✅ **可在 OnEnter/OnExit 中的操作**（一次性初始化/清理）：
  - 调整碰撞体大小（进入下蹲时）
  - 改变重力缩放（进入攀爬时）
  - 清空速度（进入某些状态时）
  - 设置标志或缓存数据

- ❌ **不应在 OnEnter/OnExit 中的操作**（持续效果）：
  - 连续更新速度（应在 FixedUpdate 中）
  - 持续改变方向（应在 FixedUpdate 中）
  - 每帧物理计算（应在 FixedUpdate 中）

**PlayerIdleStateBehaviour.cs**、**PlayerWalkStateBehaviour.cs**、**PlayerRunStateBehaviour.cs**（地面移动状态）
```csharp
public class PlayerIdleStateBehaviour : PlayerStateBehaviour
{
    // Idle、Walk、Run 都是地面移动状态，无需特殊的 SMB 逻辑
    // 水平移动、翻转等由 PlayerController.FixedUpdate() 转发到 HandleLocomotionPhysics()
    // 这些状态之间的转换由 Animator 根据 Speed 参数自动完成
}

public class PlayerWalkStateBehaviour : PlayerStateBehaviour
{
    // 同上
}

public class PlayerRunStateBehaviour : PlayerStateBehaviour
{
    // 同上
}
```

**PlayerCrouchStateBehaviour.cs**

**⚠️ 下蹲状态工作流程说明**：
下蹲状态的转换由 **Trigger 参数驱动**（必须使用沿处理，避免每帧重复触发）：
1. **进入下蹲**：玩家**按下** C 键 → PlayerInput.CrouchPressed = true（GetKeyDown）→ PlayerController.UpdateAnimatorParameters() 调用 `animator.SetTrigger("TryCrouch")`（仅触发一次）→ Animator 触发转换到 Crouch 状态
2. **在下蹲中移动**：Crouch 状态 + Speed > 0.1 → 自动转换到 CrouchWalk 状态
3. **停止移动**：CrouchWalk 状态 + Speed < 0.05 → 自动转换回 Crouch 状态
4. **释放下蹲**：玩家**松开** C 键 → PlayerInput.CrouchReleased = true（GetKeyUp）→ PlayerController.UpdateAnimatorParameters() 调用 `animator.SetTrigger("ReleaseCrouch")`（仅触发一次）→ Animator 触发转换回地面状态（Idle/Walk/Run）
5. **OnEnter/OnExit 时的 SMB 职责**：Crouch/CrouchWalk 状态的 OnEnter() 设置 `IsCrouching = true`，OnExit() 设置 `IsCrouching = false`

**⚠️ 关键：沿处理防止抖动**：
- ✅ 使用 `input.CrouchPressed`（GetKeyDown）和 `input.CrouchReleased`（GetKeyUp）
- ❌ 不能用 `input.CrouchHeld`（GetKey），会导致每帧都调用 SetTrigger()，使 Animator 不停地进出 Crouch 状态，造成动画抖动

**代码示例**：
```csharp
public class PlayerCrouchStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：设置 IsCrouching 参数
        animator.SetBool("IsCrouching", true);

        // ✅ 一次性初始化：调整碰撞体
        physicsController.ResizeCollider(
            config.CrouchColliderSize,
            config.CrouchColliderOffset);

        // ✅ 一次性清理：停止水平移动
        physicsController.SetHorizontalVelocity(0);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：重置 IsCrouching 参数
        animator.SetBool("IsCrouching", false);

        // ✅ 状态清理：恢复碰撞体
        physicsController.RestoreColliderSize();
    }

    // ⚠️ 注意：下蹲期间的实际移动速度控制在 PlayerController.HandleCrouchPhysics() 中
}
```

**PlayerCrouchWalkStateBehaviour.cs**
```csharp
public class PlayerCrouchWalkStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：设置 IsCrouching 参数（维持为 true）
        animator.SetBool("IsCrouching", true);

        // ✅ 一次性初始化：确保碰撞体处于下蹲尺寸
        physicsController.ResizeCollider(
            config.CrouchColliderSize,
            config.CrouchColliderOffset);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：重置 IsCrouching 参数
        animator.SetBool("IsCrouching", false);

        // ✅ 状态清理：恢复碰撞体
        physicsController.RestoreColliderSize();
    }

    // ⚠️ 注意：下蹲行走期间的移动速度控制在 PlayerController.HandleCrouchWalkPhysics() 中
}
```

**PlayerJumpStateBehaviour.cs**
```csharp
public class PlayerJumpStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：应用跳跃力
        physicsController.ApplyJump(config.JumpForce);
    }

    // ⚠️ 注意：空中移动由 PlayerController.HandleJumpPhysics() 每帧处理
}
```

**PlayerFallStateBehaviour.cs**
```csharp
public class PlayerFallStateBehaviour : PlayerStateBehaviour
{
    // 下落物理由 PlayerController.HandleFallPhysics() 处理
}
```

**PlayerAttackStateBehaviour.cs**
```csharp
public class PlayerAttackStateBehaviour : PlayerStateBehaviour
{
    // 攻击物理由 PlayerController.HandleAttackPhysics() 处理
}
```

**PlayerRollStateBehaviour.cs**
```csharp
public class PlayerRollStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // 可选：在此处设置翻滚开始的特定处理
    }

    // ⚠️ 注意：翻滚速度维持由 PlayerController.HandleRollPhysics() 每帧处理
}
```

**PlayerClimbIdleStateBehaviour.cs**
```csharp
public class PlayerClimbIdleStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：禁用重力（攀爬时不受重力影响）
        physicsController.SetGravityScale(0);

        // ✅ 一次性初始化：锁定水平和竖直速度
        physicsController.SetHorizontalVelocity(0);
        physicsController.SetVerticalVelocity(0);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：恢复重力
        physicsController.SetGravityScale(config.GravityScale);
    }
    // ⚠️ 注意：IsClimbing 由 PlayerController.CheckClimbableNearby() 根据碰撞检测每帧维护
    // SMB 不负责设置/重置 IsClimbing 参数
}
```

**PlayerClimbMoveStateBehaviour.cs**
```csharp
public class PlayerClimbMoveStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：禁用重力（攀爬时不受重力影响）
        physicsController.SetGravityScale(0);

        // ✅ 一次性初始化：清零水平速度（攀爬时只能竖直移动）
        physicsController.SetHorizontalVelocity(0);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：恢复重力
        physicsController.SetGravityScale(config.GravityScale);
    }
    // ⚠️ 注意：IsClimbing 由 PlayerController.CheckClimbableNearby() 根据碰撞检测每帧维护
    // SMB 不负责设置/重置 IsClimbing 参数
}
```

---

### 阶段 3：重构 PlayerController

#### 3.1 移除状态机相关代码

**删除或注释**：
- `PlayerStateMachine stateMachine` 成员
- `Awake()` 中的状态机初始化
- `Start()` 中的 `stateMachine.Initialize()`
- `Update()` 中的 `stateMachine.Update()` 调用
- `FixedUpdate()` 中的 `stateMachine.FixedUpdate()` 调用

#### 3.2 保留关键功能

```csharp
public class PlayerController : MonoBehaviour
{
    // 保留这些
    private PlayerInput input;
    private Rigidbody2D rb;
    private Animator animator;
    private PlayerMovementConfig movementConfig;
    private PlayerPhysicsController physicsController;

    // 状态追踪
    private bool isGrounded;
    private bool isClimbing;  // ✅ 由 CheckClimbableNearby() 每帧维护
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool facingRight = true;

    // 缓存 Animator 状态哈希值（平层架构，11 个独立状态）
    private static readonly int IDLE_HASH = Animator.StringToHash("Idle");
    private static readonly int WALK_HASH = Animator.StringToHash("Walk");
    private static readonly int RUN_HASH = Animator.StringToHash("Run");
    private static readonly int CROUCH_HASH = Animator.StringToHash("Crouch");
    private static readonly int CROUCH_WALK_HASH = Animator.StringToHash("CrouchWalk");
    private static readonly int JUMP_HASH = Animator.StringToHash("Jump");
    private static readonly int FALL_HASH = Animator.StringToHash("Fall");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int ROLL_HASH = Animator.StringToHash("Roll");
    private static readonly int CLIMB_IDLE_HASH = Animator.StringToHash("ClimbIdle");
    private static readonly int CLIMB_MOVE_HASH = Animator.StringToHash("ClimbMove");

    // 攀爬检测（CheckClimbableNearby() 需要）
    [SerializeField] private Transform climbCheckPoint;  // 角色正前方的检测点
    [SerializeField] private LayerMask climbWallLayerMask;  // 检测可攀爬墙壁的Layer

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        physicsController = GetComponent<PlayerPhysicsController>();
        input = new PlayerInput();
    }

    private void OnEnable()
    {
        // ⚠️ 重要：若使用 Unity Input System（新输入系统），需要在此启用输入
        // 注释掉以下行如果仍使用旧 Input.GetKey() 系统
        // input?.Enable();
    }

    private void OnDisable()
    {
        // ⚠️ 重要：若使用 Unity Input System，需要在此禁用输入
        // 注释掉以下行如果仍使用旧 Input.GetKey() 系统
        // input?.Disable();
    }

    private void Update()
    {
        input.Update();
        CheckGrounded();
        CheckClimbableNearby();  // ✅ 检测玩家是否接触可攀爬墙壁
        UpdateTimers();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        // 关键：在物理帧中根据当前 Animator 状态调度物理处理
        // 状态由 Animator 维护，SMB 只做初始化/清理
        HandlePhysicsForCurrentState();
    }

    private void HandlePhysicsForCurrentState()
    {
        // ⚠️ 重要：处理 Animator 状态转换期间的物理
        // 在淡入淡出(Blend)期间可能同时处于两个状态，需检查转换

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        int currentStateHash = currentState.shortNameHash;  // 平层架构下使用shortNameHash

        // 检查是否正在转换，并获取下一个状态
        int nextStateHash = currentStateHash;
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            // 过渡期间优先使用目标状态的物理处理，确保状态切换时不出现帧延迟
            nextStateHash = nextState.shortNameHash;
        }

        // 使用目标状态哈希进行转发（过渡期间用下一个状态，否则用当前状态）
        int stateHashToUse = animator.IsInTransition(0) ? nextStateHash : currentStateHash;

        // 优先级处理（高优先级状态先检查）
        if (stateHashToUse == JUMP_HASH)
        {
            HandleJumpPhysics();
        }
        else if (stateHashToUse == FALL_HASH)
        {
            HandleFallPhysics();
        }
        else if (stateHashToUse == ATTACK_HASH)
        {
            HandleAttackPhysics();
        }
        else if (stateHashToUse == ROLL_HASH)
        {
            HandleRollPhysics();
        }
        else if (stateHashToUse == CROUCH_HASH)
        {
            HandleCrouchPhysics();
        }
        else if (stateHashToUse == CROUCH_WALK_HASH)
        {
            HandleCrouchWalkPhysics();
        }
        else if (stateHashToUse == CLIMB_IDLE_HASH || stateHashToUse == CLIMB_MOVE_HASH)
        {
            HandleClimbPhysics(stateHashToUse == CLIMB_MOVE_HASH);
        }
        else if (stateHashToUse == IDLE_HASH || stateHashToUse == WALK_HASH || stateHashToUse == RUN_HASH)
        {
            HandleLocomotionPhysics();  // 处理所有地面移动状态
        }
    }

    private void HandleLocomotionPhysics()
    {
        float moveDirection = input.Horizontal;
        float targetSpeed = moveDirection != 0 ? movementConfig.RunSpeed : 0;
        physicsController.SetHorizontalVelocity(moveDirection * targetSpeed);

        // 处理翻转 - 保持 facingRight 与 physicsController 同步
        // ⚠️ 重要：本地的 facingRight 字段与 PlayerPhysicsController.FacingRight 必须始终一致
        // 推荐：由 physicsController 维护真实状态，PlayerController 定期同步读取
        if (moveDirection > 0 && !physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // 同步
        }
        else if (moveDirection < 0 && physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // 同步
        }
    }

    private void HandleCrouchPhysics()
    {
        float moveDirection = input.Horizontal;

        if (Mathf.Abs(moveDirection) < 0.01f)
        {
            // 没有水平输入：保持静止
            physicsController.SetHorizontalVelocity(0f);
        }
        else
        {
            // 有水平输入：应用下蹲移动速度
            // 这会让 Speed 参数升高，满足 Crouch → CrouchWalk 的转换条件（IsCrouching && Speed > 0.1）
            physicsController.SetHorizontalVelocity(
                moveDirection * movementConfig.CrouchSpeed);
        }
    }

    private void HandleCrouchWalkPhysics()
    {
        float moveDirection = input.Horizontal;
        physicsController.SetHorizontalVelocity(moveDirection * movementConfig.CrouchSpeed);

        // 处理翻转 - 必须在每次Flip后同步 facingRight，否则下一帧会反复翻转
        if (moveDirection > 0 && !physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
        else if (moveDirection < 0 && physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
    }

    private void HandleJumpPhysics()
    {
        // 允许空中移动控制
        float moveDirection = input.Horizontal;
        physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.8f);

        // 处理翻转 - 必须在每次Flip后同步 facingRight
        if (moveDirection > 0 && !physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
        else if (moveDirection < 0 && physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
    }

    private void HandleFallPhysics()
    {
        // 下落时允许水平移动
        float moveDirection = input.Horizontal;
        physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.8f);

        // 处理翻转 - 必须在每次Flip后同步 facingRight
        if (moveDirection > 0 && !physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
        else if (moveDirection < 0 && physicsController.FacingRight)
        {
            physicsController.Flip();
            facingRight = physicsController.FacingRight;  // ⚠️ 关键：立刻同步
        }
    }

    private void HandleAttackPhysics()
    {
        // 攻击期间允许少量空中控制
        if (!isGrounded)
        {
            float moveDirection = input.Horizontal;
            physicsController.SetHorizontalVelocity(moveDirection * movementConfig.RunSpeed * 0.5f);
        }
    }

    private void HandleRollPhysics()
    {
        // 维持翻滚速度
        float rollDirection = facingRight ? 1f : -1f;
        physicsController.SetHorizontalVelocity(rollDirection * movementConfig.RollSpeed);
    }

    private void HandleClimbPhysics(bool isMoving)
    {
        if (isMoving)
        {
            // ✅ 使用 ClimbAxis（PlayerInput 中定义的属性）
            float climbAxis = input.ClimbAxis;
            physicsController.SetVerticalVelocity(climbAxis * movementConfig.ClimbSpeed);
        }
        // ClimbIdle 时垂直速度为 0
    }
}
```

#### 3.3 实现 CheckClimbableNearby()

```csharp
/// <summary>
/// 检测玩家是否接触可攀爬墙壁
/// 只在玩家空中状态时，与climbWall碰撞时才能进入攀爬状态
/// </summary>
private void CheckClimbableNearby()
{
    // ⚠️ 关键：只在空中时才能触发攀爬
    // 玩家必须先离开地面，然后碰撞到墙壁才能攀爬
    if (!isGrounded)
    {
        // 在玩家正前方检测可攀爬墙壁
        // climbCheckPoint 应该放在角色身体前方（朝向方向）
        bool nearClimbWall = Physics2D.OverlapCircle(
            climbCheckPoint.position,
            movementConfig.ClimbCheckRadius,
            climbWallLayerMask);

        isClimbing = nearClimbWall;
    }
    else
    {
        // 在地面上不能攀爬
        isClimbing = false;
    }
}
```

**字段说明**：
- `climbCheckPoint`：Transform引用，应该是角色的子对象，位于角色正前方
- `climbWallLayerMask`：检测可攀爬墙壁的Layer（或使用Tag检测）
- `ClimbCheckRadius`：检测范围半径

**在 Inspector 中配置**：
1. 在 Player GameObject 下创建子对象 `ClimbCheckPoint`，位于角色身体前方
2. 在 PlayerController 中创建字段：
   - `[SerializeField] private Transform climbCheckPoint;`
   - `[SerializeField] private LayerMask climbWallLayerMask;`
3. 在 PlayerMovementConfig 中添加 `ClimbCheckRadius` 字段

---

#### 3.4 实现 UpdateAnimatorParameters()

```csharp
private void UpdateAnimatorParameters()
{
    // 连续参数
    float normalizedSpeed = Mathf.Abs(rb.velocity.x) / (movementConfig.RunSpeed > 0 ? movementConfig.RunSpeed : 1f);
    animator.SetFloat(AnimatorParams.Speed, normalizedSpeed);
    animator.SetFloat(AnimatorParams.VerticalVelocity, rb.velocity.y);
    animator.SetBool(AnimatorParams.IsGrounded, isGrounded);
    animator.SetBool(AnimatorParams.IsClimbing, isClimbing);  // ✅ 由 CheckClimbableNearby() 每帧维护

    // 跳跃处理
    bool canJump = coyoteTimeCounter > 0 && jumpBufferCounter > 0;
    if (canJump && input.JumpPressed)
    {
        animator.SetTrigger(AnimatorParams.Jump);
        coyoteTimeCounter = 0; // 消耗coyote time
        jumpBufferCounter = 0; // 消耗jump buffer
    }

    // 攻击
    if (input.AttackPressed)
    {
        animator.SetTrigger(AnimatorParams.Attack);
    }

    // 翻滚
    if (input.RollPressed)
    {
        animator.SetTrigger(AnimatorParams.Roll);
    }

    // ✅ 下蹲输入处理（新增）
    // 使用沿处理：仅在按下/松开时触发Trigger，而非持续检查CrouchHeld
    // ⚠️ 关键：必须用GetKeyDown/GetKeyUp，避免每帧重复SetTrigger造成状态反复进出
    if (input.CrouchPressed)  // GetKeyDown - 仅在按下瞬间为true
    {
        animator.SetTrigger(AnimatorParams.TryCrouch);  // 进入下蹲
    }

    if (input.CrouchReleased)  // GetKeyUp - 仅在松开瞬间为true
    {
        animator.SetTrigger(AnimatorParams.ReleaseCrouch);  // 释放下蹲
    }
    // ⚠️ 注意：不在此处改动 IsCrouching Bool 参数
    // IsCrouching 完全由 SMB 维护
}
```

**关键说明**：
- 下蹲使用 Trigger 参数（TryCrouch/ReleaseCrouch），驱动状态转换
- ✅ **必须使用沿处理**（GetKeyDown/GetKeyUp）：
  - `input.CrouchPressed`（GetKeyDown）：仅在按下瞬间为 true
  - `input.CrouchReleased`（GetKeyUp）：仅在松开瞬间为 true
  - ❌ 不能用 `input.CrouchHeld`（GetKey），会导致每帧重复触发，造成状态反复进出和动画抖动
- IsCrouching Bool 由 PlayerCrouchStateBehaviour.OnEnter()/OnExit() 自动维护
- 在 PlayerInput.cs 中实现 CrouchPressed 和 CrouchReleased 属性，分别调用 GetKeyDown 和 GetKeyUp

---

### 阶段 4：更新 Animator Controller（平层架构）

在 Unity Editor 中对 PlayerController.controller 进行以下修改。新架构**移除所有子状态机**，所有状态直接放在 Base Layer，采用平层架构。

#### 4.1 创建和配置 Animator 参数

打开 Animator 窗口（Window > Animation > Animator），选中 PlayerController.controller。在左侧 Parameters 面板中创建以下参数：

**连续参数（Float）**：
- `Speed`（默认值 0）
  - 用途：表示归一化水平速度，用于 Idle/Walk/Run 之间的转换
  - 范围：0（静止） ~ 1（最高速）
  - 更新：每帧由 PlayerController.UpdateAnimatorParameters() 设置
  - **转换规则**：
    - Speed ≈ 0 → Idle
    - 0 < Speed < 0.5 → Walk
    - Speed ≥ 0.5 → Run

- `VerticalVelocity`（默认值 0）
  - 用途：表示角色竖直速度，用于 Jump/Fall 转换判断
  - 范围：正值（上升） ~ 负值（下降）
  - 更新：每帧由 PlayerController.UpdateAnimatorParameters() 设置

**Boolean 参数**：
- `IsGrounded`（默认值 true）
  - 用途：判断角色是否接触地面
  - 更新：每帧由 PlayerController.CheckGrounded() 更新

- `IsCrouching`（默认值 false）
  - 用途：标记角色是否进入下蹲状态
  - 设置方：由 PlayerCrouchStateBehaviour.OnStateEnter() 设置为 true
  - 重置方：由 PlayerCrouchStateBehaviour.OnStateExit() 设置为 false

- `IsClimbing`（默认值 false）
  - 用途：标记角色是否接触可攀爬墙壁
  - 设置方：由 PlayerController.CheckClimbableNearby() 每帧根据碰撞检测设置（玩家在空中且与climbWall碰撞时为true）
  - 重置方：由 PlayerController.CheckClimbableNearby() 在离开墙壁时自动设置为false

**Trigger 参数**：
- `Jump`：触发跳跃动作
- `Attack`：触发攻击动作
- `Roll`：触发翻滚动作
- `TryCrouch`（✅ 新增）：玩家按下 C 键时触发，驱动 Locomotion → Crouch 转换
- `ReleaseCrouch`（✅ 新增）：玩家释放 C 键时触发，驱动 Crouch/CrouchWalk → Locomotion 转换

**✅ 关键说明**：
- TryCrouch 和 ReleaseCrouch 是**输入驱动**参数，由 PlayerController.UpdateAnimatorParameters() 根据 `input.CrouchPressed/CrouchReleased` 设置
- ✅ **必须使用沿处理**：
  - 只在按下/松开**瞬间**触发（GetKeyDown/GetKeyUp）
  - ❌ 不能每帧重复触发，否则 Animator 会不停地进出 Crouch 状态，造成动画抖动
- **不要**使用 `IsCrouching` Bool 参数来驱动下蹲的进入/退出，它只用于细粒度的 Crouch/CrouchWalk 转换
- IsCrouching 由 StateMachineBehaviour.OnEnter()/OnExit() 自动维护，代码不需要手动改动

#### 4.2 创建基础状态（平层架构，11 个独立状态）

在 Base Layer 中**直接创建** 11 个独立状态（**完全不使用子状态机和 Blend Tree**）：

| 状态名 | 动画剪辑 | 说明 | 推荐 SMB |
|--------|---------|------|--------|
| Idle | Player_Idle.anim | 角色站立不动 | PlayerIdleStateBehaviour |
| Walk | Player_Walk.anim | 角色缓慢行走 | PlayerWalkStateBehaviour |
| Run | Player_Run.anim | 角色快速奔跑 | PlayerRunStateBehaviour |
| Crouch | Player_Crouch.anim | 角色下蹲不动 | PlayerCrouchStateBehaviour |
| CrouchWalk | Player_CrouchWalk.anim | 角色下蹲行走 | PlayerCrouchWalkStateBehaviour |
| Jump | Player_Jump.anim | 角色跳起 | PlayerJumpStateBehaviour |
| Fall | Player_Fall.anim | 角色下落 | PlayerFallStateBehaviour |
| Attack | Player_Attack.anim | 角色攻击 | PlayerAttackStateBehaviour |
| Roll | Player_Roll.anim | 角色翻滚 | PlayerRollStateBehaviour |
| ClimbIdle | Player_ClimbIdle.anim | 角色贴着墙不动 | PlayerClimbIdleStateBehaviour |
| ClimbMove | Player_ClimbMove.anim | 角色沿着墙上下移动 | PlayerClimbMoveStateBehaviour |

**创建方法**：
1. 右键 Base Layer 空白区域 → Create State → Empty
2. 输入状态名称（如 "Idle"）
3. 拖动对应的动画剪辑到该状态的 Motion 字段
4. 重复 11 次，直到所有状态创建完成

**重要提醒**：
- ❌ **不要**创建 Grounded_Locomotion、Airborne 或 Climb 子状态机
- ❌ **不要**创建 Blend Tree
- ✅ **所有 11 个状态都直接放在 Base Layer 层级**，互相通过转换箭头相连
- ✅ 每个状态可独立添加 StateMachineBehaviour（SMB）脚本

**设置默认状态**：
- 右键 Idle 状态 → Set as Layer Default Entry
- 或在 Entry 上创建一条转换到 Idle

#### 4.3 配置状态转换（平层架构）

**⚠️ 重要说明**：采用平层架构后，所有状态都是同级的 AnimatorStateTransition，都提供 "Has Exit Time" 和 "Transition Duration" 选项。

---

**地面移动组**：Idle ↔ Walk ↔ Run

**转换 1：Idle → Walk**
- **触发条件**：Speed greater than 0.1
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1（平滑过渡）
  - Conditions: Speed greater than 0.1

**转换 2：Walk → Idle**
- **触发条件**：Speed less than 0.05
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1
  - Conditions: Speed less than 0.05

**转换 3：Walk → Run**
- **触发条件**：Speed greater than 0.5
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1
  - Conditions: Speed greater than 0.5

**转换 4：Run → Walk**
- **触发条件**：Speed less than 0.4
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1
  - Conditions: Speed less than 0.4

---

**下蹲组**：Crouch ↔ CrouchWalk

**✅ 转换 5：Idle/Walk/Run → Crouch（修正版：使用 Trigger 而非 Bool）**
- **触发条件**：`TryCrouch` Trigger
- **配置**（从 Idle、Walk、Run 各创建一条）：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1
  - Conditions: TryCrouch (trigger)
- **说明**：
  - ✅ 由 PlayerController.UpdateAnimatorParameters() 中的 `animator.SetTrigger("TryCrouch")` 驱动（C 键按下时）
  - ❌ **不能**使用 `IsCrouching == true` Bool 条件，那是状态的结果而非原因
  - ❌ **不能**在 PlayerController 中手动改动 IsCrouching，完全由 SMB 维护

**转换 6：Crouch → CrouchWalk**
- **触发条件**：`IsCrouching == true && Speed > 0.1`
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.05
  - Conditions: IsCrouching equals true **AND** Speed greater than 0.1
- **说明**：
  - 玩家**继续按住 C 键**（IsCrouching 保持为 true）**且**有水平移动输入时触发
  - 确保仅在下蹲状态中有移动时才切换到下蹲行走

**转换 7：CrouchWalk → Crouch**
- **触发条件**：`IsCrouching == true && Speed < 0.05`
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.05
  - Conditions: IsCrouching equals true **AND** Speed less than 0.05
- **说明**：
  - 玩家**继续按住 C 键**（IsCrouching 保持为 true）**且**停止移动输入时触发
  - 从下蹲行走回到静止下蹲

**✅ 转换 8：Crouch → Idle/Walk/Run（修正版：使用 Trigger 而非 Bool）**
- **触发条件**：`ReleaseCrouch` Trigger
- **配置**（创建三条，根据释放时的 Speed 返回到不同状态）：
  - Crouch → Idle：
    - Has Exit Time: ❌ 不勾选
    - Conditions: ReleaseCrouch (trigger) && Speed less than 0.05
  - Crouch → Walk：
    - Has Exit Time: ❌ 不勾选
    - Conditions: ReleaseCrouch (trigger) && Speed between 0.05-0.5
  - Crouch → Run：
    - Has Exit Time: ❌ 不勾选
    - Conditions: ReleaseCrouch (trigger) && Speed greater than 0.5
- **说明**：
  - ✅ 由 PlayerController.UpdateAnimatorParameters() 中的 `animator.SetTrigger("ReleaseCrouch")` 驱动（C 键释放时）
  - ❌ **不能**使用 `IsCrouching == false` Bool 条件
  - ✅ 返回哪个状态取决于释放时的 Speed 值

**✅ 转换 9：CrouchWalk → Idle/Walk/Run（修正版：使用 Trigger 而非 Bool）**
- 同转换 8，从 CrouchWalk 返回到对应的地面状态
  - CrouchWalk → Idle：
    - Conditions: ReleaseCrouch (trigger) && Speed less than 0.05
  - CrouchWalk → Walk：
    - Conditions: ReleaseCrouch (trigger) && Speed between 0.05-0.5
  - CrouchWalk → Run：
    - Conditions: ReleaseCrouch (trigger) && Speed greater than 0.5

**关键改进总结**：
| 方面 | 旧方式（❌错误） | 新方式（✅正确） |
|---|---|---|
| **进入下蹲触发** | `IsCrouching == true` | `TryCrouch` Trigger |
| **退出下蹲触发** | `IsCrouching == false` | `ReleaseCrouch` Trigger |
| **代码职责** | PlayerController 需要改动 IsCrouching | 完全由 SMB 维护，代码只触发 |
| **触发来源** | 状态结果判断 | 玩家输入（C 键）驱动 |
| **效果** | 容易造成循环判断或重复触发 | 清晰的输入→状态转换流程 |

---

**跳跃组**：Jump ↔ Fall

**转换 10：Idle/Walk/Run → Jump**
- **触发条件**：Jump trigger 被触发
- **配置**（从三个地面状态各创建一条）：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0
  - Conditions: Jump equals (none)

**转换 11：Jump → Fall**
- **触发条件**：VerticalVelocity less than 0.1
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.05
  - Conditions: VerticalVelocity less than 0.1

**转换 12：Fall → Idle/Walk/Run/Crouch**
- **触发条件**：IsGrounded == true
- **配置**（根据 IsCrouching 和 Speed 分配到不同状态）：
  - Fall → Idle：
    - Has Exit Time: ❌ 不勾选
    - Conditions: IsGrounded equals true 且 IsCrouching equals false 且 Speed less than 0.05
  - Fall → Walk：
    - Conditions: IsGrounded equals true 且 IsCrouching equals false 且 Speed between 0.05-0.5
  - Fall → Run：
    - Conditions: IsGrounded equals true 且 IsCrouching equals false 且 Speed greater than 0.5
  - Fall → Crouch：
    - Conditions: IsGrounded equals true 且 IsCrouching equals true

---

**攻击组**

**转换 13：Any State → Attack**
- **触发条件**：Attack trigger 被触发
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0
  - Conditions: Attack equals (none)

**转换 14：Attack → 返回地面状态**
- **触发条件**：IsGrounded == true 且动画播放完毕
- **配置**（创建三条过渡）：
  - Attack → Idle：
    - Has Exit Time: ✅ 勾选
    - Exit Time: 0.95
    - Transition Duration: 0.1
    - Conditions: IsGrounded equals true
  - Attack → Fall：
    - Has Exit Time: ❌ 不勾选
    - Transition Duration: 0
    - Conditions: IsGrounded equals false

---

**翻滚组**

**转换 15：Any State → Roll**
- **触发条件**：Roll trigger 被触发
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0
  - Conditions: Roll equals (none)

**转换 16：Roll → 返回地面状态**
- **触发条件**：IsGrounded == true 且动画播放完毕
- **配置**（类似 Attack）：
  - Roll → Idle：
    - Has Exit Time: ✅ 勾选
    - Exit Time: 0.95
    - Transition Duration: 0.05
    - Conditions: IsGrounded equals true
  - Roll → Fall：
    - Has Exit Time: ❌ 不勾选
    - Conditions: IsGrounded equals false

---

**攀爬组**

**⚠️ 前置准备：LDtk 中的 climbWall 实体定义**

1. **在 LDtk 中创建 climbWall 实体**：
   - 打开关卡编辑器（LDtk 应用程序）
   - 创建新的实体类型，命名为 `climbWall`
   - 设置为墙壁/平台的形状（矩形或其他）
   - 在关卡中放置 climbWall 实体（代表可攀爬的墙壁）

2. **在 Unity 中识别 climbWall**：
   - LDtk 导入后会自动生成对应的 GameObject
   - 确保 climbWall 对象有 Collider2D（用于 Physics2D.OverlapCircle 检测）
   - 将 climbWall 对象放在专用的 Layer（例如 "ClimbWall" Layer）
   - 或使用 Tag 标记为 "climbWall"

3. **在代码中配置检测**：
   - PlayerController 的 climbWallLayerMask 设置为检测 "ClimbWall" Layer
   - 或通过 Tag 检测（需要修改 CheckClimbableNearby 使用 Physics2D.OverlapCircle 配合 Tag 检查）

---

**转换 17：Any State → ClimbIdle**
- **触发条件**：IsClimbing == true
- **说明**：
  - ✅ IsClimbing 由 CheckClimbableNearby() 每帧维护
  - ✅ 仅当玩家**在空中**（!isGrounded）**且**碰撞到 climbWall 时，IsClimbing 才为 true
  - ❌ 玩家在地面上时，即使碰撞到墙壁也无法进入攀爬状态
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.1
  - Conditions: IsClimbing equals true

**转换 18：ClimbIdle → ClimbMove**
- **触发条件**：Speed greater than 0.1
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.05
  - Conditions: Speed greater than 0.1

**转换 19：ClimbMove → ClimbIdle**
- **触发条件**：Speed less than 0.05
- **配置**：
  - Has Exit Time: ❌ 不勾选
  - Transition Duration: 0.05
  - Conditions: Speed less than 0.05

**转换 20：ClimbIdle/ClimbMove → 返回地面状态**
- **触发条件**：IsClimbing == false
- **配置**（创建四条过渡）：
  - ClimbIdle → Idle：
    - Has Exit Time: ❌ 不勾选
    - Conditions: IsClimbing equals false 且 IsGrounded equals true 且 Speed less than 0.05
  - ClimbIdle → Walk/Run（类似）
  - ClimbMove → Idle/Walk/Run（类似）
  - ClimbIdle/ClimbMove → Fall：
    - Conditions: IsClimbing equals false 且 IsGrounded equals false

#### 4.4 在 Animator 状态上添加 Tags（用于状态分类）

**推荐的 Tags 分配**：

| States | Tag 名称 | 用途 |
|--------|---------|------|
| Crouch, CrouchWalk | `Crouching` | 判断角色是否处于下蹲状态 |
| Jump, Fall | `Airborne` | 判断角色是否在空中 |
| ClimbIdle, ClimbMove | `Climbing` | 判断角色是否在攀爬 |
| Attack | `Attacking` | 判断角色是否在攻击 |
| Roll | `Rolling` | 判断角色是否在翻滚 |

**添加方法**：
1. 选中状态
2. 在 Inspector 中找到 "Tags" 部分
3. 点击 `+` 添加 tag

#### 4.5 添加动画事件（可选但推荐）

在以下动画末帧（约 95% 处）添加事件：

- **Player_Attack.anim** → 函数 `OnAttackComplete`
- **Player_Roll.anim** → 函数 `OnRollComplete`

在 PlayerController 中实现回调：

```csharp
public void OnAttackComplete()
{
    // 攻击完毕，状态将由 SMB.OnStateExit() 处理
}

public void OnRollComplete()
{
    // 翻滚完毕，状态将由 SMB.OnStateExit() 处理
}
```

#### 4.6 验收清单

完成以下检查，确保 Animator 配置正确：

```
✅ Animator 参数创建
   ├─ Speed (Float, 0)
   ├─ VerticalVelocity (Float, 0)
   ├─ IsGrounded (Bool, true)
   ├─ IsCrouching (Bool, false, 由 SMB 维护)
   ├─ IsClimbing (Bool, false, 由 SMB 维护)
   ├─ Jump (Trigger)
   ├─ Attack (Trigger)
   ├─ Roll (Trigger)
   ├─ TryCrouch (Trigger, ✅ 新增)
   └─ ReleaseCrouch (Trigger, ✅ 新增)

✅ 11 个独立状态创建（平层架构）
   ├─ Idle → Player_Idle.anim
   ├─ Walk → Player_Walk.anim
   ├─ Run → Player_Run.anim
   ├─ Crouch → Player_Crouch.anim
   ├─ CrouchWalk → Player_CrouchWalk.anim
   ├─ Jump → Player_Jump.anim
   ├─ Fall → Player_Fall.anim
   ├─ Attack → Player_Attack.anim
   ├─ Roll → Player_Roll.anim
   ├─ ClimbIdle → Player_ClimbIdle.anim
   └─ ClimbMove → Player_ClimbMove.anim

✅ 状态转换（20+ 条）
   ├─ Entry → Idle（默认状态）
   ├─ Idle ↔ Walk ↔ Run（地面移动）
   ├─ Idle/Walk/Run → Crouch
   ├─ Crouch ↔ CrouchWalk
   ├─ Crouch → Idle/Walk/Run
   ├─ Idle/Walk/Run → Jump
   ├─ Jump → Fall
   ├─ Fall → Idle/Walk/Run/Crouch
   ├─ Any State → Attack
   ├─ Attack → Idle/Fall
   ├─ Any State → Roll
   ├─ Roll → Idle/Fall
   ├─ Any State → ClimbIdle
   ├─ ClimbIdle ↔ ClimbMove
   └─ ClimbIdle/ClimbMove → Idle/Walk/Run/Fall

✅ Tags 分配
   ├─ Crouch, CrouchWalk → "Crouching"
   ├─ Jump, Fall → "Airborne"
   ├─ ClimbIdle, ClimbMove → "Climbing"
   ├─ Attack → "Attacking"
   └─ Roll → "Rolling"

✅ 动画事件
   ├─ Player_Attack.anim - OnAttackComplete() @ 95%
   └─ Player_Roll.anim - OnRollComplete() @ 95% (可选)
```

#### 4.7 常见配置错误及解决方案

| 错误 | 症状 | 原因 | 解决方案 |
|------|------|------|--------|
| Speed 参数未更新 | Idle/Walk/Run 不切换 | PlayerController.UpdateAnimatorParameters() 未正确设置 Speed | 检查 `animator.SetFloat("Speed", normalizedSpeed)` 的实现 |
| 多条转换同时触发 | 角色卡在两个状态之间 | 转换条件重叠或优先级不清 | 检查条件逻辑，确保临界值明确（如 0.05、0.5） |
| IsCrouching 不更新 | 下蹲无反应 | SMB 中未设置/重置参数 | 检查 Crouch SMB 的 OnEnter()/OnExit() |
| IsGrounded 不同步 | 着陆卡住 | GroundCheck 位置或检测逻辑错误 | 检查 PlayerController.CheckGrounded() 的 Physics2D 设置 |
| IsClimbing 始终为 false | 无法进入攀爬 | CheckClimbableNearby() 逻辑错误或未检测到 climbWall | 检查：(1) 玩家是否在空中，(2) climbWall Layer/Tag 是否正确，(3) ClimbCheckPoint 位置是否在角色前方，(4) ClimbCheckRadius 是否足够大 |

#### 4.8 PlayerController.cs 中的哈希值更新

由于采用平层架构，所有状态哈希值都是简单的状态名称：

```csharp
// 缓存 Animator 状态哈希值（平层架构）
private static readonly int IDLE_HASH = Animator.StringToHash("Idle");
private static readonly int WALK_HASH = Animator.StringToHash("Walk");
private static readonly int RUN_HASH = Animator.StringToHash("Run");
private static readonly int CROUCH_HASH = Animator.StringToHash("Crouch");
private static readonly int CROUCH_WALK_HASH = Animator.StringToHash("CrouchWalk");
private static readonly int JUMP_HASH = Animator.StringToHash("Jump");
private static readonly int FALL_HASH = Animator.StringToHash("Fall");
private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
private static readonly int ROLL_HASH = Animator.StringToHash("Roll");
private static readonly int CLIMB_IDLE_HASH = Animator.StringToHash("ClimbIdle");
private static readonly int CLIMB_MOVE_HASH = Animator.StringToHash("ClimbMove");
```

**在 HandlePhysicsForCurrentState() 中**：

```csharp
private void HandlePhysicsForCurrentState()
{
    AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
    int currentStateHash = currentState.shortNameHash;  // 使用 shortNameHash 而不是 fullPathHash

    // 按优先级检查状态
    if (currentStateHash == JUMP_HASH)
        HandleJumpPhysics();
    else if (currentStateHash == FALL_HASH)
        HandleFallPhysics();
    else if (currentStateHash == ATTACK_HASH)
        HandleAttackPhysics();
    else if (currentStateHash == ROLL_HASH)
        HandleRollPhysics();
    else if (currentStateHash == CROUCH_HASH)
        HandleCrouchPhysics();
    else if (currentStateHash == CROUCH_WALK_HASH)
        HandleCrouchWalkPhysics();
    else if (currentStateHash == CLIMB_IDLE_HASH || currentStateHash == CLIMB_MOVE_HASH)
        HandleClimbPhysics(currentStateHash == CLIMB_MOVE_HASH);
    else if (currentStateHash == IDLE_HASH || currentStateHash == WALK_HASH || currentStateHash == RUN_HASH)
        HandleLocomotionPhysics();  // 处理所有地面移动状态
}
```

#### 4.9 实施注意事项与最佳实践

##### 4.9.1 状态数量与过渡复杂度

**问题**：平层架构下有 11 个状态，相互之间铺设的过渡组合非常多。如果完全照表手动连线（Idle → Walk、Walk → Run、Idle → Crouch、Crouch → Idle、ClimbIdle → Idle 等），很容易漏掉过渡或条件写错。

**推荐做法**：
1. **按模块分组配置**（而不是全部一起配置）
   - 先配置 **Locomotion 模块**：Idle ↔ Walk ↔ Run
   - 再配置 **Crouch 模块**：Crouch ↔ CrouchWalk
   - 再配置 **Airborne 模块**：Jump → Fall
   - 再配置 **Climb 模块**：ClimbIdle ↔ ClimbMove
   - 最后配置 **特殊动作**：Attack、Roll

2. **完成每个模块后核对清单**
   - 参考第 4.6 节的验收清单
   - 确保每个模块内部的所有转换都已连线

3. **逐个测试模块**
   - 配置完 Locomotion 后进入 Play Mode 测试 A/D 移动
   - 配置完 Crouch 后测试 C 键下蹲
   - 这样能及早发现问题而不是最后全部配完再测试

---

##### 4.9.2 Speed 阈值与转换条件

**问题**：文档中给出的 Speed 阈值（0.1、0.4、0.5 等）是**估算值**。在实际游戏中，`Speed = Mathf.Abs(rb.velocity.x) / runSpeed` 的计算结果可能与预期不符，导致状态频繁抖动或卡在两个状态之间。

**示例场景**：
- 设定 Walk 阈值为 0.4，但实际运行时 Speed 在 0.35-0.45 之间频繁波动
- 角色在 Walk 和 Run 之间不停切换，动画闪烁

**调整步骤**：
1. **进入 Play Mode，打开 Animator 窗口**
2. **观察 Speed 参数的实时值**
   - 按 A/D 缓慢移动，记录 Speed 在慢走时的范围
   - 按 A/D 快速移动，记录 Speed 在奔跑时的范围
   - 停止移动，记录 Speed 快速降至 0 的过程

3. **根据观察调整阈值**
   - 如果慢走时 Speed 范围是 0.2-0.3，则改 Walk 阈值为 0.25（而不是 0.4）
   - 如果奔跑时 Speed 范围是 0.7-1.0，则改 Run 阈值为 0.6（而不是 0.5）
   - 使用 **小于**（less than）和 **大于**（greater than）的临界值要有足够空隙（至少 0.1），避免波动导致抖动

4. **测试并确认**
   - 播放→停止→播放，确保转换平滑
   - 原地转向（不移动），确保速度快速回到 0 而不卡住

---

##### 4.9.3 Crouch 组的条件处理

**关键**：Crouch 和 CrouchWalk 的转换需要同时考虑 **Speed 和 IsCrouching** 两个参数。

**错误示例**：
```
❌ Crouch → Walk 只检查 IsCrouching == false
   结果：松开 C 键时，无论什么速度都直接跳到 Walk，不会经过 Idle
```

**正确示例**：
```
✅ Crouch → Idle：IsCrouching == false && Speed < 0.1
✅ Crouch → Walk：IsCrouching == false && Speed between 0.1-0.5
✅ Crouch → Run：IsCrouching == false && Speed >= 0.5
```

**关键转换检查清单**：
- [ ] Crouch → CrouchWalk：**必须同时检查** `IsCrouching == true && Speed > 0.1`（继续按住 C 键且有水平移动）
- [ ] CrouchWalk → Crouch：**必须同时检查** `IsCrouching == true && Speed < 0.05`（继续按住 C 键但停止移动）
- [ ] Crouch → 地面状态：必须同时检查 `IsCrouching == false` 和 Speed（释放 C 键时的速度值）
- [ ] CrouchWalk → 地面状态：必须同时检查 `IsCrouching == false` 和 Speed（释放 C 键时的速度值）

**⚠️ 关键实现细节：HandleCrouchPhysics() 中的 Speed 控制**

**问题分析**：
- Crouch → CrouchWalk 的转换条件需要 `Speed > 0.1`
- 但如果 HandleCrouchPhysics() 每帧都强制将速度设为 0，Speed 参数永远为 0
- 结果就是永远无法转换到 CrouchWalk

**解决方案**：
在 Crouch 状态下，应该根据玩家输入动态调整速度：
- **没有水平输入**（按键松开）：速度为 0 → Speed ≈ 0
- **有水平输入**（按着方向键）：速度为 CrouchSpeed → Speed > 0.1 → Animator 自动转换到 CrouchWalk

**代码实现**（见上方 HandleCrouchPhysics）：
```csharp
float moveDirection = input.Horizontal;

if (Mathf.Abs(moveDirection) < 0.01f)
{
    // 没有水平输入：保持静止
    physicsController.SetHorizontalVelocity(0f);
}
else
{
    // 有水平输入：应用下蹲移动速度
    physicsController.SetHorizontalVelocity(moveDirection * movementConfig.CrouchSpeed);
}
```

**转换流程**：
1. 玩家按 C 进入 Crouch，Speed = 0，处于静止下蹲
2. 玩家按 A/D：moveDirection != 0 → Speed 变为 CrouchSpeed（例如 0.25） → Speed > 0.1 满足
3. Animator 自动转换 Crouch → CrouchWalk，播放下蹲行走动画
4. 玩家松开 A/D：moveDirection ≈ 0 → Speed 回到 0 → 自动转换回 Crouch
5. 玩家松开 C：ReleaseCrouch Trigger → 返回地面状态

---

##### 4.9.4 Climb 组的条件——IsClimbing 与 IsGrounded 的组合

**问题**：从 ClimbIdle/ClimbMove 回到地面时，需要同时处理 **IsClimbing 和 IsGrounded** 的状态，否则会卡住。

**场景分析**：
1. 玩家在攀爬（IsClimbing == true）→ 松开攀爬键（IsClimbing == false）且**仍在空中**（IsGrounded == false）
   - 应转向：Fall（继续下落）

2. 玩家在攀爬（IsClimbing == true）→ 松开攀爬键（IsClimbing == false）且**脚已接地**（IsGrounded == true）
   - 应转向：Idle/Walk/Run（根据 Speed 判断）

**正确的转换配置**：

**从 ClimbIdle 出发**：
```
ClimbIdle → Fall：
  IsClimbing equals false && IsGrounded equals false

ClimbIdle → Idle：
  IsClimbing equals false && IsGrounded equals true && Speed less than 0.05

ClimbIdle → Walk：
  IsClimbing equals false && IsGrounded equals true && Speed between 0.05-0.5

ClimbIdle → Run：
  IsClimbing equals false && IsGrounded equals true && Speed greater than 0.5
```

**从 ClimbMove 出发**：
```
同上，只是前置条件是从 ClimbMove（而不是 ClimbIdle）
```

**关键检查**：
- [ ] ClimbIdle/ClimbMove → Fall 条件中包含 `IsGrounded equals false`
- [ ] 不能让攀爬状态直接过渡到 Crouch（攀爬中无法蹲）
- [ ] 测试：从墙上跳下时，是否正确进入 Fall 而不是卡在 ClimbIdle

---

##### 4.9.5 Animator Tags 与动画事件的手动配置

**问题**：Tags 和动画事件**不会自动出现**，必须手动挂上，否则后续管理器或 SMB 如果通过 Tag 判断会失效。

**需要配置的 Tags**（参见第 4.4 节）：

| 状态 | Tag | 用途 |
|------|-----|------|
| Crouch, CrouchWalk | `Crouching` | SMB 或 Combat 系统判断"是否蹲着" |
| Jump, Fall | `Airborne` | 用于判断"是否在空中" |
| ClimbIdle, ClimbMove | `Climbing` | 用于判断"是否在攀爬" |
| Attack | `Attacking` | 用于判断"是否在攻击" |
| Roll | `Rolling` | 用于判断"是否在翻滚" |

**配置步骤**：
1. 在 Animator 窗口中选中状态
2. Inspector 中找到 "Tags" 部分，点击 `+` 添加
3. 输入 tag 名称并确认

**需要配置的动画事件**（参见第 4.5 节）：

| 动画 | 事件名 | 触发时机 | 用途 |
|------|--------|---------|------|
| Player_Attack.anim | OnAttackComplete | 末帧（~95%） | 告知脚本攻击结束 |
| Player_Roll.anim | OnRollComplete | 末帧（~95%） | 告知脚本翻滚结束 |

**配置步骤**：
1. 打开 Animation 窗口（Window > Animation > Animation）
2. 选中动画剪辑
3. 定位到末帧（约 95%），右键 → Add Event
4. 输入函数名（如 `OnAttackComplete`）

**验证检查**：
- [ ] 所有需要的 Tag 都已添加到对应状态
- [ ] Attack 和 Roll 的末帧都有动画事件
- [ ] SMB 或代码中访问 Tag 时使用 `stateInfo.IsTag("TagName")` 而不是 `animator.CompareTag()`

---

##### 4.9.6 Trigger 消费与重复触发（沿处理的重要性）

**问题**：如果 Trigger 被每帧重复设置，会导致 Animator 不停地进出触发的状态，造成动画抖动。

**常见错误示例**：

```csharp
// ❌ 错误：每帧都重复触发（基于 CrouchHeld）
if (input.CrouchHeld)
{
    animator.SetTrigger("TryCrouch");  // 每帧调用！
}
else
{
    animator.SetTrigger("ReleaseCrouch");  // 每帧调用！
}
// 结果：Animator 不停进出 Crouch 状态，动画卡顿/抖动
```

**正确做法：沿处理**：

```csharp
// ✅ 正确：仅在沿处理时触发
if (input.CrouchPressed)  // GetKeyDown - 仅按下瞬间为 true
{
    animator.SetTrigger("TryCrouch");  // 只调用一次
}

if (input.CrouchReleased)  // GetKeyUp - 仅松开瞬间为 true
{
    animator.SetTrigger("ReleaseCrouch");  // 只调用一次
}
// 结果：平滑的状态转换，无抖动
```

**Trigger 设置的黄金规则**：
- ✅ 仅在**条件第一次满足时**设置一次（GetKeyDown/GetKeyUp）
- ✅ 使用**沿处理**而非**电平处理**
- ✅ 设置后**不要重复调用**（Animator 会自动在下一帧消费）
- ✅ 如果需要**持续效果**，改用 Bool 参数
- ❌ 不要在每帧检查状态后都调用 SetTrigger()
- ❌ 不要尝试手动重置 Trigger（Animator 会自动处理）

**对比示例**：

| 场景 | 错误做法 | 正确做法 | 结果 |
|---|---|---|---|
| 跳跃 | `if (input.JumpHeld) SetTrigger("Jump")` | `if (input.JumpPressed) SetTrigger("Jump")` | ✅ 跳一次 |
| 下蹲 | `if (input.CrouchHeld) SetTrigger("TryCrouch")` | `if (input.CrouchPressed) SetTrigger("TryCrouch")` | ✅ 进一次 |
| 下蹲释放 | `if (!input.CrouchHeld) SetTrigger("ReleaseCrouch")` | `if (input.CrouchReleased) SetTrigger("ReleaseCrouch")` | ✅ 出一次 |

---

在代码中的处理（PlayerController.UpdateAnimatorParameters）：

```csharp
private void UpdateAnimatorParameters()
{
    // ...其他参数...

    // ✅ 正确：检查条件后只设置一次 Trigger
    if (canJump && input.JumpPressed)
    {
        animator.SetTrigger("Jump");
        // Trigger 会在下一帧自动被 Animator 消费
        // 不要重复调用 SetTrigger()
    }

    // ❌ 错误：不要在每帧都重复设置
    // if (input.JumpHeld)  // 不要这样写
    //     animator.SetTrigger("Jump");

    // ✅ 如果需要持续跳跃效果，应该用 Bool 参数而不是 Trigger
    // animator.SetBool("IsJumping", input.JumpHeld);
}
```

**Trigger 设置的黄金规则**：
- ✅ 只在**条件第一次满足时**设置一次
- ✅ 设置后**不要重复调用**（Animator 会自动在下一帧消费）
- ✅ 如果需要**持续效果**，改用 Bool 参数
- ❌ 不要在 FixedUpdate 或 LateUpdate 中重复设置
- ❌ 不要尝试手动重置 Trigger（Animator 会自动处理）

---

##### 4.9.7 Animator 与代码的一致性检查清单

部署到 Unity 前，用此清单逐项确认：

```
✅ Animator 配置
   ├─ [ ] 所有 11 个状态已创建并绑定动画
   ├─ [ ] 所有状态转换已连线（参见 4.3）
   ├─ [ ] 所有 Tags 已添加（参见 4.4）
   ├─ [ ] 所有必要的动画事件已添加（参见 4.5）
   ├─ [ ] Idle 已设为 Entry 的默认状态
   └─ [ ] 无多余的子状态机或 Blend Tree 遗留

✅ 代码配置
   ├─ [ ] PlayerController.cs 中的 11 个 Hash 定义已更新
   ├─ [ ] HandlePhysicsForCurrentState() 中的状态判断已更新
   ├─ [ ] Speed 参数在 UpdateAnimatorParameters() 中正确计算
   ├─ [ ] IsCrouching 由 SMB.OnEnter/OnExit 设置（不在代码中硬写）
   ├─ [ ] IsClimbing 由 SMB 或 CheckClimbableNearby() 设置
   ├─ [ ] Jump/Attack/Roll Trigger 仅在条件满足时设置一次
   ├─ [ ] 没有对已删除 SMB 的调用
   └─ [ ] 编译无错误，运行无异常

✅ 参数阈值
   ├─ [ ] Speed 阈值已根据实际测试调整（不是直接用文档的估算值）
   ├─ [ ] Crouch/CrouchWalk 的 Speed 条件已验证
   ├─ [ ] Climb 的 IsClimbing/IsGrounded 组合已验证
   └─ [ ] 状态转换没有频繁抖动或卡住

✅ 运行测试
   ├─ [ ] 移动：A/D 能顺畅在 Idle → Walk → Run 切换
   ├─ [ ] 下蹲：C 能进入 Crouch，速度变化时转向 CrouchWalk
   ├─ [ ] 跳跃：Space 能触发 Jump，自动过渡到 Fall
   ├─ [ ] 攻击：Mouse0 能进入 Attack，完毕后回到地面状态
   ├─ [ ] 翻滚：Shift 能进入 Roll，完毕后回到地面状态
   ├─ [ ] 攀爬（如有）：接近墙时能进入 ClimbIdle，松开时正确掉落
   └─ [ ] 无明显卡顿、抖动或死锁状态
```

---

### 阶段 5：删除或禁用旧脚本

需要删除或禁用的文件：
```
Assets/Scripts/DeadCells.Player/StateMachine/
├─ PlayerStateMachine.cs (删除)
├─ PlayerState.cs (删除)
├─ PlayerStateId.cs (保留枚举定义，如果其他系统还在用)
├─ PlayerContext.cs (删除，功能分散到PlayerPhysicsController)
├─ PlayerAnimatorBridge.cs (功能集成到PlayerController)
└─ States/
   ├─ Grounded/ (全部删除)
   ├─ Airborne/ (全部删除)
   └─ Climb/ (全部删除)
```

保留的文件：
```
Assets/Scripts/DeadCells.Player/
├─ PlayerController.cs (大量重构)
├─ PlayerInput.cs (✅ 需扩展：新增 CrouchPressed/CrouchReleased 属性)
├─ PlayerMovementConfig.cs (✅ 需扩展：新增 ClimbCheckRadius 字段)
├─ PlayerPhysicsController.cs (新建)
└─ StateMachine/
   └─ PlayerStateBehaviour.cs (新建基类)
   └─ Behaviours/
      ├─ PlayerIdleStateBehaviour.cs (新建)
      ├─ PlayerWalkStateBehaviour.cs (新建)
      ├─ PlayerRunStateBehaviour.cs (新建)
      ├─ PlayerCrouchStateBehaviour.cs (新建)
      ├─ PlayerCrouchWalkStateBehaviour.cs (新建)
      ├─ PlayerJumpStateBehaviour.cs (新建)
      ├─ PlayerFallStateBehaviour.cs (新建)
      ├─ PlayerAttackStateBehaviour.cs (新建)
      ├─ PlayerRollStateBehaviour.cs (新建)
      ├─ PlayerClimbIdleStateBehaviour.cs (新建)
      └─ PlayerClimbMoveStateBehaviour.cs (新建)
```

---

### 阶段 6：将 SMB 脚本附加到 Animator 状态

在 Unity Editor 中：

1. 打开 Player.controller（Animator 窗口）

**地面移动状态**（可选添加 SMB，因为这些状态无需特殊初始化逻辑）：
2. 选择 Idle 状态 → Inspector → Add Behaviour → PlayerIdleStateBehaviour
3. 选择 Walk 状态 → Add Behaviour → PlayerWalkStateBehaviour
4. 选择 Run 状态 → Add Behaviour → PlayerRunStateBehaviour

**其他状态**（必须添加 SMB 以处理初始化/清理）：
5. 选择 Crouch 状态 → Add Behaviour → PlayerCrouchStateBehaviour
6. 选择 CrouchWalk 状态 → Add Behaviour → PlayerCrouchWalkStateBehaviour
7. 选择 Jump 状态 → Add Behaviour → PlayerJumpStateBehaviour
8. 选择 Fall 状态 → Add Behaviour → PlayerFallStateBehaviour
9. 选择 Attack 状态 → Add Behaviour → PlayerAttackStateBehaviour
10. 选择 Roll 状态 → Add Behaviour → PlayerRollStateBehaviour
11. 选择 ClimbIdle 状态 → Add Behaviour → PlayerClimbIdleStateBehaviour
12. 选择 ClimbMove 状态 → Add Behaviour → PlayerClimbMoveStateBehaviour

**验证步骤**：
- 确保每个状态都已分配对应的 SMB（除了 Idle/Walk/Run 可选外）
- 在 Inspector 中确认 SMB 脚本已正确显示

---

### 阶段 7：处理依赖系统

检查以下系统是否依赖 PlayerStateMachine 或 PlayerState：

#### 7.1 Combat 系统
- 搜索对 `PlayerStateMachine` 或 `PlayerState` 的引用
- 如果存在，改为通过 Animator 参数或状态名称查询状态
- **重要**：使用状态的完整路径哈希或缓存哈希值，以避免层级变化导致查询失效

示例：
```csharp
// 旧方式
if (playerStateMachine.CurrentState is PlayerAttackState) { ... }

// 新方式 - 方法 1：使用缓存的状态哈希（推荐 - 平层架构）
// ✅ 在平层架构下，所有状态都在 Base Layer 顶层，使用 shortNameHash 即可
// shortNameHash 只比较状态短名称，由于平层架构名称唯一，完全满足需求
private static readonly int ATTACK_STATE_HASH = Animator.StringToHash("Attack");
private static readonly int CROUCH_STATE_HASH = Animator.StringToHash("Crouch");

void CheckPlayerState()
{
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.shortNameHash == ATTACK_STATE_HASH)  // ✅ 使用 shortNameHash
    {
        // 玩家正在攻击
    }
}

// 新方式 - 方法 2：检查玩家是否在地面移动状态（平层架构）
private static readonly int IDLE_STATE_HASH = Animator.StringToHash("Idle");
private static readonly int WALK_STATE_HASH = Animator.StringToHash("Walk");
private static readonly int RUN_STATE_HASH = Animator.StringToHash("Run");

void CheckPlayerMoving()
{
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.shortNameHash == IDLE_STATE_HASH ||
        stateInfo.shortNameHash == WALK_STATE_HASH ||
        stateInfo.shortNameHash == RUN_STATE_HASH)
    {
        // 玩家正在地面移动（Idle、Walk 或 Run 中的任一状态）
    }
}

// 新方式 - 方法 3：通过 Animator State Tag 检查（推荐用于多状态分类）
void CheckPlayerCrouching()
{
    // ⚠️ 错误用法：animator.CompareTag() 检查的是 GameObject 的 Tag，不是 Animator 状态的 Tag
    // if (animator.CompareTag("Crouching")) { ... }  // ❌ 这不能用

    // ✅ 正确用法：使用 AnimatorStateInfo.IsTag() 检查状态 Tag
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.IsTag("Crouching"))
    {
        // 玩家正在下蹲或下蹲行走
    }
}

// 或者缓存 Tag 哈希值以提高效率
private static readonly int CROUCH_TAG_HASH = Animator.StringToHash("Crouching");

void CheckPlayerCrouchingOptimized()
{
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.IsTag(CROUCH_TAG_HASH))
    {
        // 玩家正在下蹲或下蹲行走
    }
}
```

**关键注意**：
- ❌ 不要使用 `animator.GetCurrentAnimatorStateInfo(0).fullPathHash`，因为它包含层级前缀
- ❌ 不要使用 `animator.CompareTag()`，这检查的是 GameObject 的 Tag，不是 Animator 状态的 Tag
- ✅ **平层架构下使用 `shortNameHash`** 检查特定状态名称
- ✅ 使用 `IsTag()` 或 `IsName()` 方法检查状态的 Tag 或名称
- ✅ 将常用哈希值作为静态常数缓存，避免每帧重复哈希计算
- ✅ **在 Animator 中为相关状态添加 Tag**（例如：为 Crouch 和 CrouchWalk 都添加 "Crouching" Tag）

#### 7.2 Rooms 系统
- 检查是否有敌人或机制依赖玩家状态
- 改为使用 `IsGrounded` 和 `VerticalVelocity` 等公开属性
- 或订阅动画事件

#### 7.3 其他系统
- 全局搜索 `PlayerStateMachine` 和 `PlayerState`
- 逐一修复引用

---

### 阶段 8：测试与验证

#### 8.1 编译和基础测试
1. 修复所有编译错误
2. 在 Unity 中启动场景
3. 验证角色能否加载和初始化

#### 8.2 功能测试

**输入键位说明**（确认与 PlayerInput.cs 一致）：
- 移动：A/D 或方向键左/右
- 跳跃：Space
- 攻击：Mouse 0（鼠标左键）
- 翻滚：Left Shift
- 下蹲：C（对应 KeyCode.C，参见 PlayerInput.cs）

**测试项目**：
- [ ] 移动：按 A/D，角色左右移动，Speed 参数连续变化，平滑过渡 Idle → Walk → Run
- [ ] 翻转：角色面向移动方向
- [ ] 跳跃：按 Space，触发 Jump trigger，播放跳跃动画，VerticalVelocity 变化
- [ ] 落地：IsGrounded 变为 true，自动回到对应的地面状态（Idle/Walk/Run）
- [ ] 攻击：按 Mouse0，播放攻击动画，完成后回到对应的地面状态或 Fall
- [ ] 翻滚：按 Left Shift，播放翻滚动画，快速移动，完成后回到对应状态
- [ ] 下蹲：按 C（KeyCode.C），播放下蹲动画，碰撞体变小，移动转向 CrouchWalk

#### 8.3 Animator 窗口验证
1. Play Mode 启动
2. 打开 Animator 窗口（Window > Animation > Animator）
3. 验证：
   - 状态转换正确
   - 参数实时更新
   - 动画无跳帧或不同步

#### 8.4 运行现有测试
```bash
Window > General > Test Runner
运行所有 Player 相关测试
```

---

## 四、潜在风险与缓解措施

| 风险 | 描述 | 缓解措施 |
|------|------|--------|
| **参数同步延迟** | Animator 参数更新和状态转换可能有延迟 | 在 FixedUpdate 的 HandlePhysicsForCurrentState() 中立刻处理关键物理；只在 OnStateEnter 中设置标志或缓存数据 |
| **转换条件复杂** | 某些转换条件可能难以在 Animator 中表达 | 使用 SMB 的 OnStateUpdate() 进行动态检查，必要时强制转换 |
| **依赖外部系统** | Combat 或 Rooms 可能依赖旧状态机 | 提前搜索和修复所有引用 |
| **动画事件丢失** | 关键事件（如攻击完成）可能无法准确触发 | 使用 OnStateExit() 作为主要转换触发点 |
| **性能回归** | StateMachineBehaviour 在某些情况下可能更低效 | 对比迁移前后性能数据 |

---

## 五、验收标准

迁移完成时应满足：

1. ✅ **所有编译错误已修复**
   - 无红色波浪线
   - 可成功构建

2. ✅ **功能完全相同**
   - 所有玩家操作（移动、跳跃、攻击等）行为一致
   - 动画播放正确
   - 物理效果相同

3. ✅ **代码质量**
   - StateMachineBehaviour 脚本清晰且可维护
   - PlayerController 简化至~150行
   - 物理逻辑集中在 PlayerPhysicsController

4. ✅ **测试通过**
   - 现有测试仍可通过
   - 新增 SMB 集成测试

5. ✅ **性能指标**
   - FPS 不低于迁移前
   - 内存占用不增加

6. ✅ **文档更新**
   - CLAUDE.md 中的状态机部分已更新
   - 新开发者能理解 Animator 驱动架构

---

## 六、时间估算

| 阶段 | 任务 | 预计时间 |
|------|------|--------|
| 1 | 创建 PlayerPhysicsController | 1-2 小时 |
| 2 | 创建 SMB 脚本 | 2-3 小时 |
| 3 | 重构 PlayerController | 1-2 小时 |
| 4 | 更新 Animator Controller | 1 小时 |
| 5 | 删除旧脚本，处理依赖 | 1-2 小时 |
| 6 | 附加 SMB 到状态 | 30 分钟 |
| 7 | 测试与调试 | 2-3 小时 |
| **总计** | | **9-13.5 小时** |

---

## 七、迁移检查清单

- [ ] PlayerPhysicsController 已创建并在 PlayerController 中实例化
- [ ] 所有 StateMachineBehaviour 脚本已创建
- [ ] PlayerController 已重构，移除状态机调用
- [ ] Animator Controller 参数和转换已配置
- [ ] SMB 脚本已附加到所有相应的 Animator 状态
- [ ] 旧的 StateMachine/* 脚本已删除
- [ ] 所有编译错误已修复
- [ ] 功能测试全部通过
- [ ] Animator 窗口中状态转换正确
- [ ] 依赖 PlayerStateMachine 的外部系统已修复
- [ ] 现有单元测试仍通过
- [ ] 性能指标符合要求
- [ ] CLAUDE.md 已更新

---

## 八、相关文档

- 当前实现：CLAUDE.md - 阶段 3：脚本集成
- 目标实现：本文档（MIGRATION_PLAN.md）
- 迁移后更新：CLAUDE.md 中 StateMachine 部分
