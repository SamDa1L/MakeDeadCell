# Dead Cells Test Framework - API参考文档

## 📖 核心API参考

### GameManager API

#### 静态访问
```csharp
// 获取GameManager单例实例
GameManager.Instance
```

#### 系统获取方法
```csharp
// 获取玩家控制器
PlayerController GetPlayer()

// 获取战斗管理器  
CombatManager GetCombatManager()

// 获取房间管理器 (传统模式)
RoomManager GetRoomManager() 

// 获取LDtk房间管理器 (数据驱动模式)
LDtkRoomManager GetLDtkRoomManager()

// 获取CastleDB数据管理器
CastleDBManager GetCastleDBManager()

// 获取武器工厂
WeaponFactory GetWeaponFactory()

// 获取LDtk关卡管理器
LDtkManager GetLDtkManager()
```

#### 模式控制方法
```csharp
// 检查是否使用数据驱动系统
bool IsUsingDataDrivenSystems()

// 切换数据驱动模式
// enable: true=启用数据驱动，false=传统模式
void SwitchToDataDrivenMode(bool enable)
```

---

### PlayerController API

#### 属性访问
```csharp
bool IsGrounded        // 是否在地面上
float MoveSpeed        // 移动速度
float JumpForce        // 跳跃力度  
bool FacingRight       // 是否面向右侧
Rigidbody2D Rigidbody  // 刚体组件引用
PlayerInput Input      // 输入处理器引用
```

#### 移动控制方法
```csharp
// 检查是否可以跳跃 (考虑Coyote Time和Jump Buffer)
bool CanJump()

// 执行跳跃
void Jump()

// 水平移动
// direction: 移动方向 (-1=左, 1=右, 0=停止)
void Move(float direction)
```

---

### CombatManager API

#### 战斗处理方法
```csharp
// 处理伤害
// attacker: 攻击者GameObject
// target: 目标GameObject  
// damageInfo: 伤害信息
void DealDamage(GameObject attacker, GameObject target, DamageInfo damageInfo)

// 应用击退效果
// target: 目标GameObject
// force: 击退力向量
void ApplyKnockback(GameObject target, Vector2 force)

// 应用硬直状态
// target: 目标GameObject
// duration: 硬直持续时间
void ApplyHitstun(GameObject target, float duration)
```

---

### Health API

#### 属性访问
```csharp
float MaxHealth      // 最大生命值
float CurrentHealth  // 当前生命值
bool IsAlive         // 是否存活
bool IsInvulnerable  // 是否无敌状态
```

#### 生命值操作方法
```csharp
// 受到伤害
void TakeDamage(DamageInfo damageInfo)

// 治疗
// amount: 治疗数量
void Heal(float amount)

// 设置最大生命值
void SetMaxHealth(float newMaxHealth)

// 设置无敌状态  
void SetInvulnerable(bool invulnerable)
```

#### 事件订阅
```csharp
// 生命值变化事件 (当前血量, 最大血量)
event Action<float, float> OnHealthChanged

// 受伤事件 (伤害信息)
event Action<DamageInfo> OnDamageTaken

// 死亡事件
event Action OnDeath
```

---

### Weapon API

#### 基础属性 (所有武器通用)
```csharp
string WeaponName     // 武器名称
WeaponType Type       // 武器类型 (Melee/Ranged/Magic/Shield)  
float BaseDamage      // 基础伤害
float AttackSpeed     // 攻击速度
float Range           // 攻击范围
```

#### 攻击方法
```csharp
// 尝试攻击 (考虑攻击冷却时间)
// attackDirection: 攻击方向向量
// 返回: true=攻击成功, false=攻击失败
bool TryAttack(Vector2 attackDirection)
```

---

### WeaponFactory API

#### 武器创建方法
```csharp
// 通过武器ID创建武器
// weaponId: CastleDB中的武器ID
// parent: 父级Transform (可选)
// 返回: 创建的武器GameObject
GameObject CreateWeapon(string weaponId, Transform parent = null)

// 通过武器数据创建武器
// weaponData: 武器数据结构
// parent: 父级Transform (可选)
GameObject CreateWeapon(WeaponData weaponData, Transform parent = null)
```

#### 数据查询方法
```csharp
// 获取所有武器数据
WeaponData[] GetAllWeapons()

// 根据类型获取武器数据
// weaponType: "Melee", "Ranged", "Magic"
WeaponData[] GetWeaponsByType(string weaponType)
```

---

### CastleDBManager API  

#### 数据加载方法
```csharp
// 加载数据库文件
void LoadDatabase()
```

#### 数据查询方法
```csharp
// 获取武器数据
// id: 武器ID
WeaponData GetWeapon(string id)

// 获取敌人数据  
// id: 敌人ID
EnemyData GetEnemy(string id)

// 获取物品数据
// id: 物品ID
ItemData GetItem(string id)

// 获取房间数据
// id: 房间ID  
RoomData GetRoom(string id)
```

#### 数据集合访问
```csharp
Dictionary<string, WeaponData> Weapons  // 所有武器数据
Dictionary<string, EnemyData> Enemies   // 所有敌人数据
Dictionary<string, ItemData> Items      // 所有物品数据
Dictionary<string, RoomData> Rooms      // 所有房间数据
```

---

### LDtkManager API

#### 项目和关卡加载
```csharp
// 加载LDtk项目文件
// projectPath: 项目文件相对路径
// 返回: true=加载成功, false=加载失败
bool LoadProject(string projectPath)

// 加载指定关卡
// levelIdentifier: 关卡标识符
// 返回: true=加载成功, false=加载失败
bool LoadLevel(string levelIdentifier)
```

#### 关卡信息查询
```csharp
// 获取当前关卡数据
LDtkLevel GetCurrentLevel()

// 获取玩家生成位置
Vector3 GetPlayerSpawnPosition()
```

---

### RoomManager API (传统模式)

#### 房间管理方法
```csharp
// 设置当前房间
// newRoom: 新房间对象
void SetCurrentRoom(Room newRoom)

// 移动到指定方向的房间
// direction: 移动方向 (Vector2.up/right/down/left)  
void MoveToRoom(Vector2 direction)

// 获取指定位置的房间
// gridPosition: 网格位置
Room GetRoomAt(Vector2 gridPosition)
```

#### 属性访问
```csharp
Room CurrentRoom               // 当前房间
List<Room> GeneratedRooms     // 已生成的房间列表
```

#### 事件订阅
```csharp
// 房间改变事件 (新房间, 之前的房间)
event RoomChangedDelegate OnRoomChanged
```

---

### LDtkRoomManager API (数据驱动模式)

#### 房间加载方法
```csharp
// 加载指定房间
// levelId: LDtk中的关卡ID
// 返回: true=加载成功, false=加载失败  
bool LoadRoom(string levelId)

// 切换到指定房间
// levelId: 目标关卡ID
void TransitionToRoom(string levelId)
```

#### 数据查询方法
```csharp
// 获取房间数据
// roomId: 房间ID
// 返回: 房间配置数据
RoomData GetRoomData(string roomId)
```

---

### EffectsManager API

#### 屏幕效果方法
```csharp
// 屏幕震动
// intensity: 震动强度 (默认1.0)
// duration: 持续时间 (默认0.1秒)
void ShakeScreen(float intensity = 1f, float duration = 0.1f)

// 屏幕闪烁
// color: 闪烁颜色
// duration: 持续时间
void FlashScreen(Color color, float duration = 0.1f)

// 受伤闪烁 (红色)
void FlashDamage()

// 治疗闪烁 (绿色)  
void FlashHeal()
```

#### 粒子效果方法
```csharp
// 播放击中效果
// position: 世界坐标位置
// normal: 法向量 (可选)
void PlayHitEffect(Vector3 position, Vector3 normal = default)

// 播放血液效果
// position: 世界坐标位置  
// direction: 方向向量 (可选)
void PlayBloodEffect(Vector3 position, Vector3 direction = default)

// 播放死亡效果
// position: 世界坐标位置
void PlayDeathEffect(Vector3 position)

// 播放跳跃尘土效果
// position: 世界坐标位置
void PlayJumpDustEffect(Vector3 position)
```

---

## 📋 数据结构API

### DamageInfo 结构体
```csharp
public class DamageInfo
{
    float baseDamage;          // 基础伤害
    DamageType damageType;     // 伤害类型
    bool canCrit;              // 是否可暴击
    float critChance;          // 暴击几率 (0-1)
    float critMultiplier;      // 暴击倍数
    Vector2 knockback;         // 击退向量
    float hitstunDuration;     // 硬直持续时间
    StatusEffect[] statusEffects; // 状态效果数组
    GameObject source;         // 伤害来源
    Transform hitPoint;        // 击中点位置
    
    // 计算最终伤害 (包含暴击)
    float CalculateFinalDamage()
}
```

### WeaponData 结构体  
```csharp
public class WeaponData
{
    string id;              // 武器唯一ID
    string name;            // 武器显示名称
    string description;     // 武器描述
    string weaponType;      // 武器类型字符串
    float baseDamage;       // 基础伤害
    float attackSpeed;      // 攻击速度
    float range;            // 攻击范围
    float critChance;       // 暴击几率
    float critMultiplier;   // 暴击倍数
    string damageType;      // 伤害类型字符串  
    string iconPath;        // 图标资源路径
    string prefabPath;      // 预制体资源路径
    WeaponStats stats;      // 武器详细属性
}
```

### EnemyData 结构体
```csharp
public class EnemyData
{
    string id;                        // 敌人唯一ID
    string name;                      // 敌人名称
    string description;               // 敌人描述
    float maxHealth;                  // 最大生命值
    float moveSpeed;                  // 移动速度
    float attackDamage;               // 攻击伤害
    float detectionRange;             // 检测范围
    float attackRange;                // 攻击范围  
    float attackCooldown;             // 攻击冷却时间
    string aiType;                    // AI类型
    string prefabPath;                // 预制体路径
    EnemyRewards rewards;             // 奖励配置
    DamageResistanceData[] resistances; // 伤害抗性数组
}
```

---

## 🎯 使用示例

### 创建和使用武器
```csharp
// 通过WeaponFactory创建武器
var sword = WeaponFactory.Instance.CreateWeapon("basic_sword");

// 攻击目标
if (sword.GetComponent<Weapon>().TryAttack(Vector2.right))
{
    Debug.Log("攻击成功！");
}
```

### 处理伤害
```csharp
// 创建伤害信息
var damageInfo = new DamageInfo
{
    baseDamage = 25f,
    damageType = DamageType.Physical,  
    knockback = Vector2.right * 3f,
    hitstunDuration = 0.2f,
    source = attackerGameObject
};

// 造成伤害
CombatManager.Instance.DealDamage(attacker, target, damageInfo);
```

### 订阅生命值事件
```csharp
var health = GetComponent<Health>();

// 订阅事件
health.OnHealthChanged += (current, max) => 
{
    Debug.Log($"生命值: {current}/{max}");
};

health.OnDeath += () => 
{
    Debug.Log("角色死亡！");
};
```

### 切换房间
```csharp
// 数据驱动模式
LDtkRoomManager.Instance.LoadRoom("Level_1");

// 传统模式  
RoomManager.Instance.MoveToRoom(Vector2.up);
```

### 播放特效
```csharp
// 屏幕震动
EffectsManager.Instance.ShakeScreen(1.5f, 0.3f);

// 播放击中特效
EffectsManager.Instance.PlayHitEffect(hitPosition);

// 屏幕闪烁
EffectsManager.Instance.FlashScreen(Color.white, 0.1f);
```

---

## ⚠️ 注意事项

1. **单例访问**: 所有Manager类都使用单例模式，通过`.Instance`访问
2. **空引用检查**: 在调用API前建议检查Manager实例是否存在
3. **事件订阅**: 记得在适当时机取消事件订阅，避免内存泄漏
4. **数据驱动模式**: 确保CastleDB和LDtk文件正确放置在StreamingAssets文件夹
5. **坐标系统**: Unity使用左手坐标系，Y轴向上为正方向

---

这个API参考文档涵盖了框架的主要接口。如需了解更多细节，请参考源代码中的具体实现。