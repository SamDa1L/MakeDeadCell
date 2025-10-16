# CLAUDE.md 勘误表 (Errata for CLAUDE.md)

**生成日期**: 2025-10-10
**适用版本**: 当前 CLAUDE.md (已提交版本)

---

## ⚠️ 重大问题

### 问题 1：Input System 架构不匹配

**位置**: 阶段 3.2-3.5 节（约行 1920-2235）

**问题描述**:
- 文档描述基于新Unity Input System的MonoBehaviour `PlayerInput`
- 实际代码库使用旧Input API的纯C#类 `PlayerInput`
- 不存在 `PlayerInputActions.inputactions` 资源文件

**影响**:
- 按文档操作会导致代码冲突
- 需安装不必要的Input System包
- 编译错误（找不到 `UnityEngine.InputSystem` 命名空间）

**正确做法**:
- **跳过 3.2-3.5 节**的所有操作
- 使用当前 `Assets/Scripts/DeadCells.Player/PlayerInput.cs` 的实现（已存在）
- 该实现使用 `Input.GetAxisRaw()` 等旧API，无需额外包

**修正代码示例** (当前正确的PlayerInput):
```csharp
// 文件: Assets/Scripts/DeadCells.Player/PlayerInput.cs
namespace DeadCells.Player
{
    public class PlayerInput // 注意：不是MonoBehaviour
    {
        private float horizontalInput;
        private bool jumpPressed;
        // ...

        public void Update() // 注意：不是Unity生命周期方法
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetKeyDown(KeyCode.Space);
            // ...
        }
    }
}
```

---

### 问题 2：状态数量声明错误

**位置**:
- 阶段 3.1 行 1101 "实现9个玩家状态"
- 阶段 3.1 行 1888 "9个具体状态类"
- PlayerStateMachine.cs 行 343-345 (DoubleJump/WallSlide/WallJump属性)

**问题描述**:
- 文档多处声称需要实现9个状态（含DoubleJump、WallSlide、WallJump）
- 实际 `PlayerStates.cs` 仅包含6个基础状态（Idle/Move/Jump/Fall/Attack/Roll）
- 高级3状态的代码**不存在**

**影响**:
- 调用 `stateMachine.DoubleJumpState` 会返回null → NullReferenceException
- Animator配置步骤2（行1142-1158）添加的4个参数无用
- 步骤4-11（行1203-1860）的转换配置无法实现

**正确做法**:
1. **明确当前仅支持6个基础状态**
2. 如需高级动作，需：
   - 在 `PlayerStates.cs` 中新增 DoubleJumpState/WallSlideState/WallJumpState 类
   - 在 PlayerController 中添加墙壁检测代码
   - 在 PlayerStateMachine.Awake() 中注册新状态
3. **或者删除文档中步骤1-11**（行1120-1860），避免误导

**当前实际状态清单**:
```csharp
// Assets/Scripts/DeadCells.Player/PlayerStates.cs (实际文件)
public class PlayerIdleState : PlayerState { /* ... */ }   // ✅ 存在
public class PlayerMoveState : PlayerState { /* ... */ }   // ✅ 存在
public class PlayerJumpState : PlayerState { /* ... */ }   // ✅ 存在
public class PlayerFallState : PlayerState { /* ... */ }   // ✅ 存在
public class PlayerAttackState : PlayerState { /* ... */ }  // ✅ 存在
public class PlayerRollState : PlayerState { /* ... */ }   // ✅ 存在

// 以下状态不存在于当前代码库：
// public class DoubleJumpState : PlayerState { } // ❌ 不存在
// public class WallSlideState : PlayerState { }  // ❌ 不存在
// public class WallJumpState : PlayerState { }   // ❌ 不存在
```

---

### 问题 3：动画参数更新指导位置错误

**位置**:
- 阶段 2.5 决策3（行897-935）描述了正确做法
- 但阶段 2.4 步骤3（行596-736）示例代码中未体现

**问题描述**:
- 在步骤3"创建状态转换"的早期，文档应明确说明：
  - `isMoving` 只能在 MoveState 的 Enter/Exit 设置
  - `attack/roll` 只能在 AttackState/RollState 的 Enter() 触发一次
- 但实际这些规则被放在后面的"决策点"章节，容易遗漏

**影响**:
- 开发者按步骤3操作时，可能在每个状态的Update()中都调用SetBool("isMoving", ...)
- 导致动画抖动、参数冲突

**正确做法**:
- 将"规则1-3"（行1136-1244）移动到**步骤3之前**
- 或在步骤3的代码示例中加入注释警告

**修正后的流程**:
```
阶段 2.4 配置Animator Controller状态机
├─ 步骤1: 创建参数
├─ 步骤2: 创建状态
├─ **步骤2.5: 参数更新规则** ← 新增，放在这里
│   ├─ 规则1: Bool参数职责分离
│   ├─ 规则2: Trigger只在Enter()触发
│   └─ 规则3: Float由Controller集中更新
├─ 步骤3: 创建状态转换（现在可以安全参考规则）
├─ 步骤4: 调整转换优先级
└─ 步骤5: 设置转换配置通用原则
```

---

## 📋 快速修复检查清单

如果你已经按照CLAUDE.md操作过，请执行以下检查：

### 检查1: 删除错误安装的包
```bash
# 如果已安装 Unity Input System 包但不需要，可以卸载
# Unity Editor: Window → Package Manager → Input System → Remove
```

### 检查2: 验证PlayerInput实现
```bash
# 检查文件是否正确
cat Assets/Scripts/DeadCells.Player/PlayerInput.cs | grep "MonoBehaviour"

# 应该输出空（不继承MonoBehaviour）
# 如果输出有内容，说明实现错误
```

### 检查3: 验证状态数量
```bash
# 检查实际注册的状态
grep "RegisterState" Assets/Scripts/DeadCells.Player/*.cs

# 应该只看到6个RegisterState调用（Idle/Move/Jump/Fall/Attack/Roll）
# 如果看到DoubleJump/WallSlide/WallJump，说明文档误导了
```

### 检查4: 清理无用的Animator参数
如果已经添加了 `canDoubleJump`, `isTouchingWall`, `isWallSliding`, `wallJump` 参数：
1. 打开Animator窗口
2. 选择Parameters面板
3. 右键删除这4个参数（当前代码不使用）

---

## 🔧 推荐修复方案

### 方案 A：最小化修复（推荐用于快速上手）
1. 跳过阶段3.2-3.5节（安装Input System相关）
2. 删除阶段3.1中"步骤1-11"（高级动作系统配置）
3. 在阶段2.4步骤3前添加"参数更新规则"

**工作量**: 约30分钟（主要是文档标注）

---

### 方案 B：完整重构文档（推荐用于长期维护）
1. 创建新章节 `阶段3A: 基础6状态系统`
2. 创建新章节 `阶段3B: 高级3状态扩展（可选）`
3. 在3B中完整实现DoubleJump/WallSlide/WallJump的代码
4. 更新所有涉及状态数量的描述

**工作量**: 约2-3小时

---

## 📚 参考资料

- 正确的PlayerInput实现: `Assets/Scripts/DeadCells.Player/PlayerInput.cs:5`
- 当前6个状态实现: `Assets/Scripts/DeadCells.Player/PlayerStates.cs:22`
- Animator参数更新规则: `CLAUDE.md:897-935` (已正确描述)

---

**注意**: 本勘误表不修改原CLAUDE.md文件，而是提供补充说明。建议在使用CLAUDE.md时同时参考本文档。
