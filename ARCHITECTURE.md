# Dead Cells Test Framework - 架构文档

## 🏗️ 整体架构

### 系统层次结构

```
游戏架构层级:
┌─────────────────────────────────────────────────────────────┐
│                     GameManager                            │ ← 顶层管理器
│                   (核心协调者)                              │
└─────────────────────────────────────────────────────────────┘
┌─────────────────┬─────────────────┬─────────────────────────┐
│   核心系统       │   房间系统       │     数据系统           │
│                │                │                       │
│ PlayerController│  RoomManager    │  CastleDBManager      │
│ CombatManager   │  LDtkRoomManager│  WeaponFactory        │ 
│ EffectsManager  │  Room           │  LDtkManager          │
│                │  RoomDoor       │  DataDrivenWeapon     │
└─────────────────┴─────────────────┴─────────────────────────┘
┌─────────────────┬─────────────────┬─────────────────────────┐
│   玩家系统       │   战斗系统       │     武器系统           │
│                │                │                       │
│ PlayerInput     │  Health         │  Weapon (基类)        │
│ PlayerStates    │  DamageInfo     │  MeleeWeapon          │
│ StateMachine    │  HitstunControl │  RangedWeapon         │
│ AnimController  │  EnemyController│  Projectile           │
└─────────────────┴─────────────────┴─────────────────────────┘
```

## 📂 命名空间架构

### 命名空间设计原则
- 按功能模块划分，避免循环依赖
- 每个命名空间职责单一清晰
- 支持水平扩展和垂直深化

```csharp
DeadCellsTestFramework
├── Core                    // 核心管理，其他系统的协调者
│   └── GameManager         // 单例管理器，系统总入口
├── Player                  // 玩家相关，输入处理和状态管理
│   ├── PlayerController    // 玩家主控制器
│   ├── PlayerInput         // 输入封装
│   ├── PlayerStateMachine  // 状态机管理器
│   └── PlayerStates        // 具体状态实现
├── Combat                  // 战斗系统，伤害计算和生命管理
│   ├── CombatManager       // 战斗中央处理器
│   ├── Health              // 生命值系统
│   ├── DamageInfo          // 伤害信息封装
│   ├── HitstunController   // 硬直状态控制
│   └── EnemyController     // 敌人AI控制
├── Weapons                 // 武器系统，攻击行为和伤害输出
│   ├── Weapon              // 武器抽象基类
│   ├── MeleeWeapon         // 近战武器实现
│   ├── RangedWeapon        // 远程武器实现
│   └── Projectile          // 投射物控制
├── Rooms                   // 房间和关卡系统
│   ├── RoomManager         // 传统房间管理
│   ├── Room                // 房间数据和逻辑
│   ├── RoomDoor            // 房间连接门
│   └── LDtkRoomManager     // LDtk数据驱动房间管理
├── Data                    // 数据管理和工厂模式
│   ├── CastleDBManager     // 外部数据加载器
│   ├── GameData            // 游戏数据结构定义
│   ├── WeaponFactory       // 武器创建工厂
│   └── DataDrivenWeapon    // 数据驱动的武器实现
├── Level                   // 关卡加载和解析
│   ├── LDtkManager         // LDtk文件解析器
│   └── LDtkData            // LDtk数据结构映射
├── Animation               // 动画控制系统
│   └── AnimationController // 统一动画接口
├── Effects                 // 视觉效果和反馈
│   ├── EffectsManager      // 特效管理器
│   └── ScreenFlash         // 屏幕闪烁效果
└── Tools                   // 开发辅助工具
    └── GameSetupWizard     // 项目快速配置工具
```

## 🔄 数据流向图

### 玩家输入 → 动作执行流程

```
[玩家输入] 
    ↓
[PlayerInput.Update()] → 收集输入状态
    ↓
[PlayerStateMachine.Update()] → 状态转换判断
    ↓
[当前State.Update()] → 执行具体状态逻辑
    ↓                    ↓
[PlayerController]   [武器攻击]
 移动/跳跃处理          ↓
    ↓               [Weapon.TryAttack()]
[物理系统更新]          ↓
    ↓               [CombatManager.DealDamage()]
[AnimationController]   ↓
 播放对应动画        [Health.TakeDamage()]
                       ↓
                   [EffectsManager]
                    播放视觉效果
```

### 数据驱动系统流程

```
[CastleDB文件] → [CastleDBManager] → 解析游戏数据 → [内存数据字典]
                                                       ↓
[LDtk文件] → [LDtkManager] → 解析关卡数据 → [关卡实体生成]
    ↓                                        ↓
[瓦片地图生成] ← [LDtkRoomManager] ← [WeaponFactory]
    ↓                  ↓               创建数据驱动的武器
[玩家定位]         [敌人生成]              ↓
    ↓                  ↓           [DataDrivenWeapon]
[游戏开始]         [AI初始化]         应用数据属性
```

## 🎯 核心设计模式

### 1. 单例模式 (Singleton)
**使用场景**: 管理器类，确保全局唯一访问点

```csharp
// 应用类: GameManager, CombatManager, EffectsManager
public class GameManager : MonoBehaviour 
{
    public static GameManager Instance { get; private set; }
    
    private void Awake() 
    {
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else 
        {
            Destroy(gameObject);
        }
    }
}
```

### 2. 状态机模式 (State Machine)
**使用场景**: 玩家行为控制，敌人AI

```csharp
// 玩家状态机结构
PlayerStateMachine
├── IdleState      // 待机状态
├── MoveState      // 移动状态  
├── JumpState      // 跳跃状态
├── FallState      // 下落状态
├── AttackState    // 攻击状态
└── RollState      // 翻滚状态

// 状态转换逻辑在每个状态的Update()方法中处理
```

### 3. 工厂模式 (Factory)
**使用场景**: 动态创建游戏对象

```csharp
// WeaponFactory - 根据数据创建武器
public GameObject CreateWeapon(string weaponId) 
{
    var weaponData = CastleDBManager.Instance.GetWeapon(weaponId);
    return ConfigureWeapon(InstantiatePrefab(weaponData), weaponData);
}
```

### 4. 组合模式 (Component)
**使用场景**: Unity的Component系统，模块化功能

```csharp
// 玩家GameObject组合示例
Player GameObject
├── PlayerController    (控制逻辑)
├── Rigidbody2D        (物理模拟)
├── Health             (生命系统)
├── HitstunController  (硬直控制)
├── AnimationController(动画管理)
└── Collider2D         (碰撞检测)
```

### 5. 观察者模式 (Observer)
**使用场景**: 事件通知系统

```csharp
// Health系统事件示例
public class Health : MonoBehaviour 
{
    public event Action<float, float> OnHealthChanged;  // 生命值变化
    public event Action<DamageInfo> OnDamageTaken;      // 受伤事件
    public event Action OnDeath;                        // 死亡事件
}
```

## 📊 系统交互图

### 战斗系统交互

```
[玩家攻击] → [Weapon.TryAttack()] → [检测目标] → [创建DamageInfo]
                                        ↓
[CombatManager.DealDamage()] ← [武器伤害计算] ← [应用武器数据]
            ↓
    [目标.Health.TakeDamage()]
            ↓
        [计算最终伤害] → [应用伤害抗性]
            ↓
        [更新生命值] → [触发受伤事件]
            ↓                ↓
    [检查死亡状态]    [视觉效果反馈]
            ↓                ↓
        [触发死亡]      [屏幕震动/粒子]
```

### 房间系统交互

```
[传统模式]                    [数据驱动模式]
     ↓                            ↓
[RoomManager]                [LDtkRoomManager]
     ↓                            ↓
[程序化生成房间]              [加载LDtk关卡文件]
     ↓                            ↓
[创建Room对象]               [LDtkManager解析]
     ↓                            ↓
[设置房间连接]               [生成瓦片地图+实体]
     ↓                            ↓
[生成敌人和物品]              [应用CastleDB数据]
     ↓                            ↓
[RoomDoor传送逻辑]           [RoomDoor传送逻辑]
```

## 🔧 扩展指南

### 添加新的玩家状态

1. **继承PlayerState基类**
```csharp
public class NewPlayerState : PlayerState 
{
    public override void Enter() { /* 进入状态时的初始化 */ }
    public override void Update() { /* 每帧更新逻辑和状态转换 */ }
    public override void Exit() { /* 离开状态时的清理 */ }
}
```

2. **在状态机中注册**
```csharp
// PlayerStateMachine构造函数中添加
NewState = new NewPlayerState(this, player);
```

### 添加新的武器类型

1. **继承Weapon基类或创建数据驱动武器**
```csharp
public class MagicWeapon : Weapon 
{
    public override bool TryAttack(Vector2 direction) 
    {
        // 实现魔法武器特有的攻击逻辑
        return true;
    }
}
```

2. **在WeaponFactory中添加支持**
```csharp
case "magic":
    return magicWeaponPrefab;
```

### 添加新的敌人AI行为

1. **扩展EnemyState枚举**
```csharp
public enum EnemyState 
{
    Idle, Patrol, Chase, Attack, Hurt, Death,
    NewBehavior  // 新AI行为
}
```

2. **在UpdateState()中实现逻辑**

### 集成新的外部工具

参考CastleDB和LDtk的集成方式：
1. 创建对应的Manager类
2. 定义数据结构映射
3. 实现文件解析逻辑  
4. 提供统一的访问接口

## ⚡ 性能考虑

### 对象池模式
- **投射物对象池**: 避免频繁创建销毁子弹
- **特效对象池**: 粒子效果复用
- **敌人对象池**: 大量敌人的内存管理

### 事件系统优化  
- 及时取消事件订阅，避免内存泄漏
- 使用弱引用处理长生命周期的事件订阅

### 数据加载策略
- CastleDB数据在游戏启动时一次性加载
- LDtk关卡数据按需加载，支持异步加载
- 资源预加载和延迟加载的平衡

## 🚀 未来扩展方向

1. **网络多人支持** - 添加网络同步层
2. **存档系统** - 玩家进度和游戏状态持久化  
3. **mod支持** - 提供更开放的数据接口
4. **性能分析工具** - 内置性能监控面板
5. **可视化编辑器** - Unity Editor扩展工具

---

这个架构文档描述了框架的核心设计思想和扩展方法。如果你需要了解特定系统的详细实现，请参考对应的源代码文件。