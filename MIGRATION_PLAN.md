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
        ├─ 通过 AnimatorStateInfo.nameHash 识别当前状态
        └─ 调用 PlayerPhysicsController 执行状态特定的物理
            ├─ Grounded_Locomotion: 应用移动速度
            ├─ Crouch / CrouchWalk: 维持下蹲状态
            ├─ Jump: 控制空中移动
            ├─ Fall: 应用重力
            └─ 其他状态: 状态特定的物理逻辑

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
- ✅ **状态识别**：通过 `AnimatorStateInfo.nameHash` 识别当前状态，调度相应的物理处理
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

**PlayerLocomotionStateBehaviour.cs**（Grounded locomotion）
```csharp
public class PlayerLocomotionStateBehaviour : PlayerStateBehaviour
{
    // 无需在 SMB 中处理物理逻辑
    // 水平移动、翻转等由 PlayerController.FixedUpdate() 转发到 HandleLocomotionPhysics()
}
```

**PlayerCrouchStateBehaviour.cs**
```csharp
public class PlayerCrouchStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：调整碰撞体
        physicsController.ResizeCollider(
            config.CrouchColliderSize,
            config.CrouchColliderOffset);

        // ✅ 一次性清理：停止水平移动
        physicsController.SetHorizontalVelocity(0);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：恢复碰撞体
        physicsController.RestoreColliderSize();
    }

    // ⚠️ 注意：下蹲期间的实际移动速度控制在 PlayerController.HandleCrouchPhysics() 中
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
        // ✅ 一次性初始化：禁用重力
        physicsController.SetGravityScale(0);

        // ✅ 一次性初始化：锁定水平速度
        physicsController.SetHorizontalVelocity(0);
    }

    protected override void OnExit()
    {
        // ✅ 状态清理：恢复重力
        physicsController.SetGravityScale(config.GravityScale);
    }
}
```

**PlayerClimbMoveStateBehaviour.cs**
```csharp
public class PlayerClimbMoveStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // ✅ 一次性初始化：禁用重力
        physicsController.SetGravityScale(0);
    }

    // ⚠️ 注意：攀爬垂直移动由 PlayerController.HandleClimbPhysics() 每帧处理
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
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool facingRight = true;

    // 缓存 Animator 状态哈希值
    // ⚠️ 重要提醒：nameHash 只包含状态短名称，同层级内必须保证名称唯一
    // 如果存在重名状态（例如不同子状态机中都有 "Idle"），必须使用完整路径哈希
    // 例如：Animator.StringToHash("Base Layer.Grounded_Locomotion") 或 Animator.StringToHash("Base Layer.Climb.ClimbIdle")

    private static readonly int LOCOMOTION_HASH = Animator.StringToHash("Grounded_Locomotion");
    private static readonly int CROUCH_HASH = Animator.StringToHash("Crouch");
    private static readonly int CROUCH_WALK_HASH = Animator.StringToHash("CrouchWalk");
    private static readonly int JUMP_HASH = Animator.StringToHash("Jump");
    private static readonly int FALL_HASH = Animator.StringToHash("Fall");
    private static readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    private static readonly int ROLL_HASH = Animator.StringToHash("Roll");
    private static readonly int CLIMB_IDLE_HASH = Animator.StringToHash("ClimbIdle");
    private static readonly int CLIMB_MOVE_HASH = Animator.StringToHash("ClimbMove");

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
        int currentStateHash = currentState.nameHash;

        // 检查是否正在转换，并获取下一个状态
        int nextStateHash = currentStateHash;
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            // 过渡期间优先使用目标状态的物理处理，确保状态切换时不出现帧延迟
            nextStateHash = nextState.nameHash;
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
        else if (stateHashToUse == LOCOMOTION_HASH)
        {
            HandleLocomotionPhysics();
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
        // 下蹲时保持静止
        physicsController.SetHorizontalVelocity(0);
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
            float climbAxis = input.Vertical;
            physicsController.SetVerticalVelocity(climbAxis * movementConfig.ClimbSpeed);
        }
        // ClimbIdle 时垂直速度为 0
    }
}
```

#### 3.3 实现 UpdateAnimatorParameters()

```csharp
private void UpdateAnimatorParameters()
{
    // 连续参数
    float normalizedSpeed = Mathf.Abs(rb.velocity.x) / (movementConfig.RunSpeed > 0 ? movementConfig.RunSpeed : 1f);
    animator.SetFloat(AnimatorParams.Speed, normalizedSpeed);
    animator.SetFloat(AnimatorParams.VerticalVelocity, rb.velocity.y);
    animator.SetBool(AnimatorParams.IsGrounded, isGrounded);

    // 跳跃处理
    if (CanJump && input.JumpPressed)
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

    // 下蹲（通过SMB设置IsCrouching）
    // 攀爬（通过SMB设置IsClimbing）
}
```

---

### 阶段 4：更新 Animator Controller

在 Unity Editor 中对 Player.controller 进行以下修改。本阶段分为三个关键部分：参数创建、Blend Tree 配置、状态转换设置。

#### 4.1 创建和配置 Animator 参数

打开 Animator 窗口（Window > Animation > Animator），选中 Player.controller。在左侧 Parameters 面板中创建以下参数：

**连续参数（Float）**：
- `Speed`（默认值 0）
  - 用途：驱动 Blend Tree，表示归一化水平速度
  - 范围：0（静止） ~ 1（最高速）
  - 更新：每帧由 PlayerController.UpdateAnimatorParameters() 设置

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
  - 用途：标记角色是否进入攀爬状态
  - 设置方：由对应的 Climb SMB.OnStateEnter() 设置为 true
  - 重置方：由对应的 Climb SMB.OnStateExit() 设置为 false

**Trigger 参数**：
- `Jump`：触发跳跃动作，由 PlayerController.UpdateAnimatorParameters() 在检测到输入时设置
- `Attack`：触发攻击动作，由 PlayerController.UpdateAnimatorParameters() 在检测到输入时设置
- `Roll`：触发翻滚动作，由 PlayerController.UpdateAnimatorParameters() 在检测到输入时设置

#### 4.2 创建并配置 Grounded_Locomotion Blend Tree

**目的**：通过单一的 `Speed` 参数在 Idle、Walk、Run 三个动画之间平滑过渡，无需创建多个转换规则。

**操作步骤**：

1. **在 Base Layer 中创建子状态机**
   - 右键空白区域 → Create Sub-State Machine
   - 命名为 `Grounded_Locomotion`
   - 此子状态机将成为地面活动的集合

2. **配置 Entry 指向 Grounded_Locomotion**
   - 从 Entry 节点拖线到 Grounded_Locomotion
   - ⚠️ 注意：Entry → 子状态机的过渡不提供 Inspector 配置，保持默认即可

3. **在 Grounded_Locomotion 内创建 Blend Tree**
   - 进入 Grounded_Locomotion 子状态机（双击打开）
   - 右键空白区域 → Create State → From New Blend Tree
   - 命名为 `Locomotion_Blend`
   - 双击打开此 Blend Tree 编辑视图

4. **配置 Blend Tree 参数**
   - **Blend Type**: 选择 `1D`（一维混合）
   - **Parameter**: 选择 `Speed`（驱动混合的参数）
   - **Automate Threshold**: ⚠️ 只有当 Blend Tree 内至少存在**两个 Motion** 时才会出现此选项
     - 此选项用于在添加完所有 Motion 后，让 Unity 自动计算阈值
     - 完整的用法见下方步骤 5 的补充说明

5. **添加运动到 Blend Tree**

   点击 Blend Tree 左侧的 `+` 按钮，添加以下动画：

   | Motion | Threshold | 说明 |
   |--------|-----------|------|
   | Player_Idle.anim | 0 | Speed=0 时播放（完全静止） |
   | Player_Walk.anim | 0.4 | Speed=0.4 时播放（行走速度） |
   | Player_Run.anim | 1 | Speed=1 时播放（奔跑速度） |

   **如果缺少 Player_Walk.anim**：
   - 暂时复制 Player_Run.anim 并重命名
   - 在 Animation 窗口中调整帧率使其显得较慢
   - 或将此步骤标记为 TODO，等待美术资源

   **⚠️ 关键：配置 Threshold 值的步骤**：
   1. 先将三条 Motion 添加到 Blend Tree 中（此时 Automate Threshold 选项会出现）
   2. 勾选一次 **Automate Threshold** ✅，让 Unity 自动填充初始阈值
   3. 立即取消勾选 Automate Threshold ❌
   4. 手动将各个 Motion 的 Threshold 值设置为表格中的值（0、0.4、1）
   5. 按回车或点击其他区域确认改动

6. **设置 Blend Tree 的 Entry**
   - 在 Grounded_Locomotion 内，从 Entry 连接到 Locomotion_Blend
   - ⚠️ 注意：Entry → 状态的过渡不提供 Inspector 配置，保持默认即可

7. **验证 Blend Tree（可选但推荐）**
   - 从 Grounded_Locomotion 子状态机返回 Base Layer（点击 Base Layer）
   - 进入 Play Mode
   - 在 Game 窗口中移动角色（A/D 或方向键）
   - 打开 Animator 窗口，观察 Speed 参数值：
     - 静止时：Speed ≈ 0，播放 Idle
     - 移动时：Speed ≈ 0.4-0.6，Blend Tree 混合 Walk/Run
     - 快速移动时：Speed ≈ 1，播放 Run 动画

#### 4.3 配置状态转换（Base Layer）

返回 Base Layer 层级（点击 Animator 窗口左上角的 "Base Layer"）。现在进行所有状态转换的配置。

**⚠️ 重要：理解两种过渡类型**

Unity Animator 中存在两种过渡类型，它们的 Inspector 界面不同：

1. **AnimatorStateTransition（状态 ↔ 状态）**
   - 例如：Jump → Fall 或 Crouch → CrouchWalk
   - Inspector 选项：Has Exit Time、Exit Time、Transition Duration、Conditions
   - 可以设置退出时间和过渡时间

2. **AnimatorTransition（子状态机 ↔ 状态 或 Any State ↔ 任何）**
   - 例如：Grounded_Locomotion → Jump 或 Attack → Grounded_Locomotion 或 Any State → Climb
   - Inspector 选项：Transitions（用于分别编辑每条条件）、Conditions
   - **不提供** "Has Exit Time"、"Exit Time" 或 "Transition Duration"（这些选项根本不存在）
   - 默认没有过渡时间，立即切换

**操作提示**：在配置时查看 Inspector，如果找不到某个选项，说明这是 AnimatorTransition 类型，直接配置 Conditions 即可。

**转换 1：Grounded_Locomotion → Jump**
- **触发条件**：Jump trigger 被触发
- **配置方法**：
  1. 从 Grounded_Locomotion 拖线到 Jump 状态
  2. 点击转换箭头，在 Inspector 中设置：
     - **Conditions**: Jump equals (none)（即 Trigger 被设置）
- **说明**：无需等待动画播放完毕，立即转换
  - ⚠️ 注意：Grounded_Locomotion 是子状态机，子状态机→状态的 AnimatorTransition 不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 2：Grounded_Locomotion → Crouch**
- **触发条件**：IsCrouching == true 且角色在地面
- **配置方法**：
  1. 从 Grounded_Locomotion 拖线到 Crouch 状态
  2. Inspector 设置：
     - **Conditions**: IsCrouching equals true
- **说明**：玩家按下下蹲键后立即转换
  - ⚠️ 注意：Grounded_Locomotion 是子状态机，子状态机→状态的 AnimatorTransition 不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 3：Grounded_Locomotion → Attack**
- **触发条件**：Attack trigger 被触发
- **配置方法**：
  1. 从 Grounded_Locomotion 拖线到 Attack 状态
  2. Inspector 设置：
     - **Conditions**: Attack equals (none)
- **说明**：立即进入攻击状态
  - ⚠️ 注意：Grounded_Locomotion 是子状态机，子状态机→状态的 AnimatorTransition 不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 4：Grounded_Locomotion → Roll**
- **触发条件**：Roll trigger 被触发
- **配置方法**：
  1. 从 Grounded_Locomotion 拖线到 Roll 状态
  2. Inspector 设置：
     - **Conditions**: Roll equals (none)
- **说明**：立即进入翻滚状态
  - ⚠️ 注意：Grounded_Locomotion 是子状态机，子状态机→状态的 AnimatorTransition 不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 5：Crouch → Grounded_Locomotion**
- **触发条件**：IsCrouching == false（松开下蹲键）且头顶有足够空间
- **配置方法**：
  1. 从 Crouch 拖线到 Grounded_Locomotion
  2. Inspector 设置：
     - **Conditions**: IsCrouching equals false
- **说明**：由 PlayerCrouchStateBehaviour.OnUpdate() 检查 HasHeadroom()，若为真则复位 IsCrouching
  - ⚠️ 注意：状态→子状态机的过渡不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 6：Crouch → CrouchWalk**
- **触发条件**：IsCrouching == true 且 Speed > 0.1（玩家在下蹲状态下移动）
- **配置方法**：
  1. 从 Crouch 拖线到 CrouchWalk
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**: Speed greater than 0.1
- **说明**：需在 Animator 中添加额外的 Float 参数判断，或在 SMB 中动态控制

**转换 7：CrouchWalk → Crouch**
- **触发条件**：Speed <= 0.1（停止移动）
- **配置方法**：
  1. 从 CrouchWalk 拖线到 Crouch
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**: Speed less than 0.1

**转换 8：CrouchWalk → Grounded_Locomotion**
- **触发条件**：IsCrouching == false（松开下蹲键）
- **配置方法**：
  1. 从 CrouchWalk 拖线到 Grounded_Locomotion
  2. Inspector 设置：
     - **Conditions**: IsCrouching equals false
- **说明**：松开下蹲键后回到地面移动状态
  - ⚠️ 注意：状态→子状态机的过渡不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 9：Jump → Fall**
- **触发条件**：VerticalVelocity <= 0（竖直速度变为0或负值，表示上升结束）
- **配置方法**：
  1. 从 Jump 拖线到 Fall
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**: VerticalVelocity less than 0.1
- **说明**：不能用 <= 0，改用 < 0.1 以避免浮点精度问题

**转换 10：Fall → Grounded_Locomotion**
- **触发条件**：IsGrounded == true 且 IsCrouching == false
- **配置方法**：
  1. 从 Fall 拖线到 Grounded_Locomotion
  2. Inspector 设置：
     - **Conditions**:
       - IsGrounded equals true
       - IsCrouching equals false
- **说明**：着陆后（非下蹲状态）回到地面移动状态
  - ⚠️ 注意：状态→子状态机的过渡不提供 "Has Exit Time" 或 "Transition Duration" 选项

**转换 11：Fall → Crouch**
- **触发条件**：IsGrounded == true 且 IsCrouching == true（空中按下蹲，着陆时进入下蹲）
- **配置方法**：
  1. 从 Fall 拖线到 Crouch
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**:
       - IsGrounded equals true
       - IsCrouching equals true

**转换 12：Attack → Grounded_Locomotion**
- **触发条件**：IsGrounded == true 且动画播放完毕
- **配置方法**：
  1. 从 Attack 拖线到 Grounded_Locomotion
  2. Inspector 设置：
     - **Conditions**: IsGrounded equals true
- **说明**：攻击完毕后回到地面移动状态
  - ⚠️ 注意：状态→子状态机的过渡不提供 "Has Exit Time" 或 "Transition Duration" 选项
  - 💡 提示：通过在 Attack 动画末帧添加事件或使用 PlayerAttackStateBehaviour.OnStateExit() 来处理动画完毕逻辑

**转换 13：Attack → Fall**
- **触发条件**：IsGrounded == false（攻击中失去接触，进入空中）
- **配置方法**：
  1. 从 Attack 拖线到 Fall
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**: IsGrounded equals false
- **说明**：允许在空中攻击然后下落

**转换 14：Roll → Grounded_Locomotion**
- **触发条件**：IsGrounded == true 且动画播放完毕
- **配置方法**：
  1. 从 Roll 拖线到 Grounded_Locomotion
  2. Inspector 设置：
     - **Conditions**: IsGrounded equals true
- **说明**：翻滚完毕后回到地面移动状态
  - ⚠️ 注意：状态→子状态机的过渡不提供 "Has Exit Time" 或 "Transition Duration" 选项
  - 💡 提示：通过在 Roll 动画末帧添加事件或使用 PlayerRollStateBehaviour.OnStateExit() 来处理动画完毕逻辑

**转换 15：Roll → Fall**
- **触发条件**：IsGrounded == false（翻滚中失去接触）
- **配置方法**：
  1. 从 Roll 拖线到 Fall
  2. Inspector 设置：
     - **Has Exit Time**: ❌ 不勾选
     - **Conditions**: IsGrounded equals false

**可选：Any State → Climb（如果需要攀爬）**

**目的**：允许角色从**任何地面或空中状态**快速进入攀爬状态，只需接触可攀爬物体并按下对应的输入。

**前置条件**：
- 需要创建 Climb 子状态机，包含 ClimbIdle 和 ClimbMove 两个状态
- ClimbIdle：角色贴着可攀爬物体但不移动
- ClimbMove：角色沿着可攀爬物体上下移动

**配置步骤**：

1. **创建 Climb 子状态机**（如果尚未创建）
   - 在 Base Layer 中右键 → Create Sub-State Machine
   - 命名为 `Climb`
   - 在其内部创建两个状态：
     - Player_ClimbIdle.anim
     - Player_ClimbMove.anim
   - Entry 连接到 ClimbIdle（攀爬时默认进入空闲状态）

2. **配置 Climb 内部的状态转换**
   - ClimbIdle → ClimbMove：**Speed** greater than 0.1（开始移动）
   - ClimbMove → ClimbIdle：**Speed** less than 0.1（停止移动）
   - 两者都需要设置 "Has Exit Time: ❌ 不勾选"

3. **从 Any State 创建过渡到 Climb**
   - 右键 Any State → Make Transition
   - 拖线到 Climb 子状态机（或 ClimbIdle 状态）
   - Inspector 设置：
     - **Conditions**: IsClimbing equals true
   - **说明**：Any State → Climb 是 AnimatorTransition 类型，不提供 "Has Exit Time" 或 "Transition Duration" 选项

4. **从 ClimbIdle 创建过渡回到地面**
   - 从 ClimbIdle 拖线到 Grounded_Locomotion
   - Inspector 设置：
     - **Conditions**: IsClimbing equals false
   - **说明**：松开攀爬输入或离开可攀爬物体时回到地面活动
   - ⚠️ 注意：状态→子状态机的过渡，不提供 "Has Exit Time" 或 "Transition Duration" 选项

5. **从 ClimbMove 创建过渡回到地面**
   - 从 ClimbMove 拖线到 Grounded_Locomotion
   - Inspector 设置：
     - **Conditions**: IsClimbing equals false
   - **说明**：移动过程中松开攀爬时立即回到地面
   - ⚠️ 注意：状态→子状态机的过渡，不提供 "Has Exit Time" 或 "Transition Duration" 选项

6. **可选：添加从 Airborne 进入 Climb 的过渡**
   - 如果需要支持在空中接触可攀爬物体时直接进入攀爬
   - 从 Fall 拖线到 Climb
   - Inspector 设置：
     - **Conditions**: IsClimbing equals true
   - 这允许玩家在跳跃过程中接触墙壁并开始攀爬

**代码配合**：

在 PlayerController 中需要实现攀爬逻辑：

```csharp
private void UpdateAnimatorParameters()
{
    // ... 其他参数设置代码 ...

    // 检测是否在可攀爬物体附近，并处理攀爬输入
    bool isNearClimbable = CheckClimbableNearby(); // 需要实现此方法
    bool climbInput = input.ClimbHeld;             // 按住攀爬按键

    // 设置 IsClimbing 参数驱动状态转换
    animator.SetBool("IsClimbing", isNearClimbable && climbInput);
}

private bool CheckClimbableNearby()
{
    // 检查角色周围是否有可攀爬的物体
    // 例如使用 Physics2D.OverlapCircle 检测 "Climbable" 层
    // 这需要与关卡设计配合
    return false; // 示例，需要根据实际需求实现
}
```

在 PlayerPhysicsController 中支持攀爬物理：

```csharp
public void SetGravityScale(float scale)
{
    rb.gravityScale = scale;
}

public void SetVerticalVelocity(float velocity)
{
    rb.velocity = new Vector2(rb.velocity.x, velocity);
}
```

在 PlayerClimbIdleStateBehaviour 和 PlayerClimbMoveStateBehaviour 中：

```csharp
public class PlayerClimbIdleStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // 禁用重力，使角色贴在墙上
        physicsController.SetGravityScale(0);
        // 清零速度
        physicsController.SetHorizontalVelocity(0);
        physicsController.SetVerticalVelocity(0);
    }

    protected override void OnExit()
    {
        // 恢复重力
        physicsController.SetGravityScale(config.GravityScale);
    }
}

public class PlayerClimbMoveStateBehaviour : PlayerStateBehaviour
{
    protected override void OnEnter()
    {
        // 禁用重力
        physicsController.SetGravityScale(0);
    }

    protected override void OnExit()
    {
        // 恢复重力
        physicsController.SetGravityScale(config.GravityScale);
    }
}
```

在 PlayerController.FixedUpdate() 中的 HandlePhysicsForCurrentState()：

```csharp
else if (stateHashToUse == CLIMB_IDLE_HASH || stateHashToUse == CLIMB_MOVE_HASH)
{
    HandleClimbPhysics(stateHashToUse == CLIMB_MOVE_HASH);
}

private void HandleClimbPhysics(bool isMoving)
{
    if (isMoving)
    {
        // 在攀爬移动状态，读取竖直输入
        float climbAxis = input.Vertical;
        physicsController.SetVerticalVelocity(climbAxis * movementConfig.ClimbSpeed);
    }
    // ClimbIdle 时竖直速度为 0（由 SMB.OnEnter() 设置）
}
```

**集成建议**：

1. **优先级管理**：攀爬状态应该优先级较高，Any State 确保了从任何状态都能进入
2. **输入检测**：需要在关卡中标记可攀爬物体（使用特定的 Layer 或 Tag）
3. **过渡平滑性**：从攀爬回到地面时，考虑检查下方是否有地面，以避免角色悬空
4. **后续扩展**：可添加攀爬特效（粒子、声音）或受击时中断攀爬的逻辑

**调试建议**：

- 在 Scene 视图中可视化可攀爬物体的范围
- 在 Animator 窗口中观察 IsClimbing 参数的变化
- 使用 Debug.Log 跟踪 CheckClimbableNearby() 的返回值
- 测试从不同状态（Idle、Jump、Fall）进入攀爬的情况

#### 4.4 在 Animator 状态上添加 Tags（用于状态分类）

**为什么需要 Tags**：
- 便于在代码中通过 `stateInfo.IsTag("TagName")` 快速判断一类状态
- 例如：为 Crouch 和 CrouchWalk 都添加 "Crouching" Tag，在 Combat 系统中可一次性检查

**操作步骤**：

1. **选择状态** → 在 Animator 窗口中点击某个状态（如 Crouch）
2. **打开 Inspector** → 在状态的 Inspector 面板中找到 "Tags" 部分
3. **添加 Tag**：
   - 点击 `+` 按钮
   - 输入 tag 名称（如 "Crouching"）
   - 回车确认

**推荐的 Tags 分类**：

| States | Tag 名称 | 用途 |
|--------|---------|------|
| Crouch, CrouchWalk | `Crouching` | 判断角色是否处于下蹲状态 |
| Jump, Fall | `Airborne` | 判断角色是否在空中 |
| ClimbIdle, ClimbMove | `Climbing` | 判断角色是否在攀爬 |
| Attack | `Attacking` | 判断角色是否在攻击 |
| Roll | `Rolling` | 判断角色是否在翻滚 |

#### 4.5 添加动画事件（可选但推荐）

动画事件用于在动画播放的特定帧触发代码回调，例如在攻击动画末帧触发伤害检测。

**操作步骤**：

1. **打开 Animation 窗口** → Window > Animation > Animation
2. **选择需要添加事件的动画剪辑** → 例如 Assets/Animations/Player/MainCharacter/Player_Attack.anim
3. **定位到末帧** → 在 Timeline 上拖动到动画最后或接近最后（约 95% 处）
4. **添加事件**：
   - 右键此时间点 → Add Event
   - 在弹出的对话框中输入函数名：`OnAttackComplete`
   - 点击 OK
5. **在 PlayerController 中实现回调**：
   ```csharp
   public void OnAttackComplete()
   {
       // 可选：额外的攻击结束处理
       // 大部分逻辑由 PlayerAttackStateBehaviour.OnStateExit() 处理
   }
   ```

**常见动画事件**：
- `OnAttackComplete()`：攻击动画末帧
- `OnRollComplete()`：翻滚动画末帧
- `OnFootstep()`：脚步声特效（可选）

#### 4.6 验收清单

完成以下检查，确保 Animator 配置正确：

```
✅ Animator 参数创建
   ├─ Speed (Float, 0)
   ├─ VerticalVelocity (Float, 0)
   ├─ IsGrounded (Bool, true)
   ├─ IsCrouching (Bool, false)
   ├─ IsClimbing (Bool, false)
   ├─ Jump (Trigger)
   ├─ Attack (Trigger)
   └─ Roll (Trigger)

✅ Grounded_Locomotion Blend Tree
   ├─ 子状态机已创建 "Grounded_Locomotion"
   ├─ 内部有 Blend Tree "Locomotion_Blend"
   ├─ Blend Type: 1D
   ├─ Parameter: Speed
   └─ Motion 列表:
       ├─ Player_Idle (Threshold: 0)
       ├─ Player_Walk (Threshold: 0.4)
       └─ Player_Run (Threshold: 1)

✅ 状态转换（15 条）
   ├─ Entry → Grounded_Locomotion
   ├─ Grounded_Locomotion → Jump
   ├─ Grounded_Locomotion → Crouch
   ├─ Grounded_Locomotion → Attack
   ├─ Grounded_Locomotion → Roll
   ├─ Crouch ↔ Grounded_Locomotion
   ├─ Crouch ↔ CrouchWalk
   ├─ Jump → Fall
   ├─ Fall → Grounded_Locomotion / Crouch
   ├─ Attack → Grounded_Locomotion / Fall
   ├─ Roll → Grounded_Locomotion / Fall
   └─ (可选) Any State → Climb / Climb → Ground

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
| Speed 参数绑定错误 | Idle/Walk/Run 不切换 | Blend Tree 中 Parameter 选择错误，或 Speed 参数在代码中未正确更新 | 检查 Blend Tree Parameter 是否为 "Speed"；检查 PlayerController.UpdateAnimatorParameters() 是否设置 Speed 参数 |
| 转换条件冲突 | 角色卡在两个状态之间 | 转换条件互相矛盾或重复 | 检查转换条件的逻辑，例如 Crouch 和 Grounded_Locomotion 不应同时满足条件 |
| IsCrouching 参数不更新 | 下蹲无反应 | PlayerCrouchStateBehaviour 中未正确设置/重置 IsCrouching | 检查 OnEnter() 中设置为 true，OnExit() 中设置为 false |
| IsGrounded 状态不同步 | 落地时卡住 | PlayerController.CheckGrounded() 未正确检测地面 | 检查 GroundCheck 位置是否正确，Ground Layer Mask 是否配置 |
| Blend Tree 无法使用 | 创建后 Blend Tree 消失 | 未连接 Entry 或状态连接错误 | 在 Grounded_Locomotion 内从 Entry 连接到 Locomotion_Blend |

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
├─ PlayerInput.cs (不变)
├─ PlayerMovementConfig.cs (不变)
├─ PlayerPhysicsController.cs (新建)
└─ StateMachine/
   └─ PlayerStateBehaviour.cs (新建基类)
   └─ Behaviours/
      ├─ PlayerLocomotionStateBehaviour.cs (新建)
      ├─ PlayerCrouchStateBehaviour.cs (新建)
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

1. 打开 Player.controller（Animator窗口）
2. 选择 Grounded_Locomotion 状态 → Inspector → Add Behaviour → PlayerLocomotionStateBehaviour
3. 选择 Crouch 状态 → Add Behaviour → PlayerCrouchStateBehaviour
4. 选择 Jump 状态 → Add Behaviour → PlayerJumpStateBehaviour
5. 选择 Fall 状态 → Add Behaviour → PlayerFallStateBehaviour
6. 选择 Attack 状态 → Add Behaviour → PlayerAttackStateBehaviour
7. 选择 Roll 状态 → Add Behaviour → PlayerRollStateBehaviour
8. 选择 ClimbIdle 状态 → Add Behaviour → PlayerClimbIdleStateBehaviour
9. 选择 ClimbMove 状态 → Add Behaviour → PlayerClimbMoveStateBehaviour

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

// 新方式 - 方法 1：使用缓存的状态哈希（推荐）
// ⚠️ 重要：nameHash 只比较状态短名称，同层级必须确保名称唯一
// 如果动画图中存在重名状态（如不同层的"Idle"），必须改用完整路径哈希
private static readonly int ATTACK_STATE_HASH = Animator.StringToHash("Attack");
private static readonly int CROUCH_STATE_HASH = Animator.StringToHash("Crouch");

void CheckPlayerState()
{
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.nameHash == ATTACK_STATE_HASH)
    {
        // 玩家正在攻击
    }
}

// 若存在重名风险，改用完整路径哈希：
private static readonly int ATTACK_STATE_FULL_HASH = Animator.StringToHash("Base Layer.Attack");
// 然后通过扩展方法或直接比较来检查
// if (stateInfo.fullPathHash == ATTACK_STATE_FULL_HASH) { ... }

// 新方式 - 方法 2：使用完整路径（适用于子状态机）
private static readonly int GROUNDED_LOCOMOTION_HASH =
    Animator.StringToHash("Base Layer.Grounded_Locomotion");

void CheckPlayerMoving()
{
    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

    if (stateInfo.nameHash == GROUNDED_LOCOMOTION_HASH)
    {
        // 玩家在地面移动
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
- ✅ 使用 `nameHash` 检查特定状态名称
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
- [ ] 移动：按 A/D，角色左右移动，Speed 参数连续变化
- [ ] 翻转：角色面向移动方向
- [ ] 跳跃：按 Space，触发 Jump trigger，播放跳跃动画，VerticalVelocity 变化
- [ ] 落地：IsGrounded 变为 true，回到 Locomotion
- [ ] 攻击：按 Mouse0，播放攻击动画，完成后回到 Locomotion
- [ ] 翻滚：按 Left Shift，播放翻滚动画，快速移动
- [ ] 下蹲：按 C（KeyCode.C），播放下蹲动画，碰撞体变小

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
