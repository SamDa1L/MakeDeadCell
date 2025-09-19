# 死亡细胞角色渲染流水线

这个脚本可以自动将标准的3D角色模型转换为死亡细胞风格的2D精灵图。

## 功能特点

- ✅ 自动导入FBX角色模型
- ✅ 网格优化（减少面数适合2D渲染）
- ✅ 死亡细胞风格材质设置
- ✅ 正交相机设置（侧视角）
- ✅ 平面光照系统
- ✅ 批量动画渲染
- ✅ PNG序列帧输出（透明背景）
- ✅ 像素艺术适合的分辨率
- ✅ Unity精灵图集自动生成
- ✅ Unity Animator Controller生成
- ✅ Unity Player Prefab生成
- ✅ 自动导入到Unity项目

## 使用方法

### 1. 在Blender中运行

1. 打开Blender
2. 确保FBX文件路径正确
3. 打开文本编辑器加载 `character_DeadCellTest.py`
4. 点击 "Run Script" 执行

### 2. 自动化流程

脚本会自动执行以下步骤：
1. **清理场景** - 删除默认对象
2. **导入FBX** - 从指定路径导入角色模型
3. **优化网格** - 使用Decimate修改器减少面数
4. **设置材质** - 应用死亡细胞风格的平面着色
5. **配置相机** - 设置正交投影和侧视角
6. **设置光照** - 配置平面光照系统
7. **渲染设置** - 配置输出格式和分辨率
8. **分析动画** - 列出所有可用动画
9. **智能渲染** - 根据配置决定是否渲染（默认渲染全部动画）
10. **生成Unity资产** - 创建精灵图集、Animator Controller和Player Prefab（无论是否渲染）
11. **自动导入Unity** - 直接复制到Unity项目的Assets目录

## 配置参数

### 文件路径
```python
self.fbx_path = r"F:\UnityTestProjects\ArtAssests\人物\测试2\Animation Library[Standard]\Unity\AnimationLibrary_Unity_Standard.fbx"
self.output_path = r"F:\UnityTestProjects\MakeDeadCell\renders"
```

### 渲染设置
```python
self.render_resolution = (256, 256)  # 分辨率
self.frame_rate = 12                 # 帧率
```

### 自动化控制
```json
"automation": {
    "auto_render": true,        # 是否自动渲染
    "render_limit": 5,          # 渲染动画数量限制（-1表示全部，5表示前5个）
    "skip_confirmation": true   # 是否跳过交互确认
}
```

### 死亡细胞调色板
```python
self.dead_cells_colors = {
    'skin': (0.8, 0.6, 0.4, 1.0),    # 皮肤色
    'cloth': (0.2, 0.3, 0.6, 1.0),   # 布料色
    'metal': (0.5, 0.5, 0.5, 1.0),   # 金属色
    'accent': (0.8, 0.2, 0.2, 1.0)   # 强调色
}
```

## 输出结果

### 文件结构
```
F:\UnityTestProjects\MakeDeadCell\renders\
├── animation1\
│   ├── animation1_0001.png
│   ├── animation1_0002.png
│   └── ...
├── animation2\
│   ├── animation2_0001.png
│   └── ...
├── SpritesheetS\              # Unity资产
│   ├── animation1.png          # 精灵图集
│   ├── animation1.png.meta     # Unity元数据
│   ├── animation2.png
│   ├── animation2.png.meta
│   ├── PlayerAnimatorController.controller
│   └── PlayerCharacter.prefab
└── Assets\PlayerCharacter\    # 自动导入到Unity项目
    └── [相同的Unity资产文件]
```

### 输出规格
- **格式**: PNG (RGBA透明背景)
- **分辨率**: 256x256像素
- **帧率**: 12fps
- **视角**: 90度侧视图
- **投影**: 正交投影
- **默认行为**: 自动渲染全部动画并生成Unity资产

## 渲染设置详解

### 相机设置
- **类型**: 正交相机
- **位置**: (3, 0, 1)
- **旋转**: 90度侧视角
- **正交缩放**: 2.5

### 光照配置
- **主光源**: 太阳光，强度3.0，45度角
- **填充光**: 太阳光，强度1.0，-45度角
- **背景**: 深灰色，强度0.1

### 材质设置
- **着色器**: Emission材质
- **效果**: 平面着色，无阴影
- **颜色**: 基于死亡细胞调色板

## 使用流程建议

### 1. 预处理
- 确保FBX文件路径正确
- 检查输出目录权限
- 备份原始Blender文件

### 2. 渲染
- 运行脚本进行自动设置
- 检查预览效果
- 确认后开始批量渲染

### 3. 后期处理
- 使用图像编辑软件进行像素化
- 调整颜色到项目调色板
- 添加描边效果
- 优化文件大小

## 自定义选项

### 修改动画选择
```python
# 在render_all_animations()函数中
for animation in animations[:5]:  # 修改数量
```

### 调整网格简化程度
```python
decimate.ratio = 0.3  # 调整保留面数比例
```

### 修改渲染分辨率
```python
self.render_resolution = (512, 512)  # 更高分辨率
```

## 故障排除

### 常见问题

1. **FBX导入失败**
   - 检查文件路径是否正确
   - 确认FBX文件完整性

2. **渲染输出为空**
   - 检查相机位置和角度
   - 确认角色在相机视野内

3. **材质显示异常**
   - 切换到Material Preview或Rendered视图
   - 检查材质节点连接

4. **动画不播放**
   - 确认动画数据已正确导入
   - 检查时间轴设置

### 性能优化

- 减少Decimate比例以降低面数
- 降低渲染分辨率以提高速度
- 限制同时渲染的动画数量

## 扩展功能

可以添加的功能：
- 多角度渲染（正面、背面、侧面）
- 批量材质变体生成
- 自动颜色替换
- 帧插值优化
- 批量后处理

## Unity集成功能

### 自动生成的Unity资产

脚本完成渲染后会自动生成以下Unity资产：

1. **精灵图集** (`.png` + `.meta`)
   - 将每个动画的所有帧合并到一张图片中
   - 自动计算最优的网格布局（接近正方形）
   - 生成Unity兼容的.meta文件，包含精灵切割信息

2. **Unity Editor脚本** (`.cs`)
   - 自动创建AnimationClip资产
   - 生成完整的AnimatorController
   - 配置Prefab的Controller引用
   - 一键式自动化流程

3. **基础Player Prefab** (`.prefab`)
   - 包含SpriteRenderer组件
   - 配置Rigidbody2D和BoxCollider2D
   - 预设PlayerController脚本引用
   - 包含GroundCheck子对象（由Editor脚本完善）

### 自动导入到Unity

- **智能路径检测** - 从输出路径向上搜索包含`Assets`和`ProjectSettings`的Unity项目根目录
- **自适应结构** - 支持各种项目目录结构，不限制脚本位置
- **安全搜索** - 最多向上搜索10级目录，防止无限循环
- **自动复制** - 将所有资产复制到 `Assets/PlayerCharacter/` 目录
- **刷新触发** - 检测Unity运行状态并触发资产数据库刷新

## 资产生成说明

### 📋 资产生成依赖

Unity资产生成**严格依赖**渲染输出：

1. **必需输入**: 渲染输出的PNG序列帧目录
   ```
   F:\UnityTestProjects\MakeDeadCell\renders\
   ├── animation1\          # 必需：包含PNG序列帧
   │   ├── frame_0001.png
   │   ├── frame_0002.png
   │   └── ...
   └── animation2\
       └── ...
   ```

2. **生成流程**: 序列帧 → 精灵图集 → Unity资产
3. **输出位置**: `renders\SpritesheetS\` 目录

### 🎯 不同场景下的行为

**场景1: 首次运行（默认配置）**
```
✓ 自动渲染前5个动画 → ✓ 生成Unity资产
📁 输出: renders\SpritesheetS\
```

**场景2: 已有渲染输出**
```
⚠ 跳过渲染阶段 → ✓ 检测到现有输出 → ✓ 生成Unity资产
```

**场景3: 无渲染输出**
```
⚠ 跳过渲染阶段 → ❌ 未找到PNG序列帧 → ❌ 无法生成Unity资产
💡 提示：请先设置 auto_render=true 或使用 DEADCELLS_AUTO_RENDER=true
```

### ⚡ 快速启用渲染

如果脚本没有渲染输出，可以通过以下方式启用：

1. **环境变量（推荐）**
   ```bash
   # Windows
   set DEADCELLS_AUTO_RENDER=true
   # 然后重新运行脚本
   
   # Linux/Mac
   export DEADCELLS_AUTO_RENDER=true
   ```

2. **修改配置文件**
   ```json
   // config.json
   "automation": {
       "auto_render": true,
       "render_limit": 5        // 或 -1 表示全部动画
   }
   ```

3. **删除配置文件重新生成**
   ```
   删除 config.json → 重新运行脚本 → 生成默认配置（已开启渲染）
   ```

### 在Unity中使用

1. **运行Editor脚本**
   ```
   1. 在Unity中打开: Window → Character Animation Setup
   2. 点击 "Create Animations and Controller" 按钮
   3. 自动创建所有AnimationClip和AnimatorController
   4. Prefab自动配置完成
   ```

2. **使用生成的资产**
   ```
   Assets/PlayerCharacter/
   ├── animation1.png           # 精灵图集
   ├── animation1.anim          # 动画剪辑（由脚本生成）
   ├── animation2.anim
   ├── PlayerCharacter.prefab   # 完整配置的Prefab
   ├── PlayerAnimatorController.controller # 完整的Controller
   └── CharacterAnimationSetup.cs # Editor脚本
   ```

3. **直接使用**
   - 拖拽PlayerCharacter.prefab到场景
   - 角色已完全配置，包含所有动画
   - 根据需要调整Animator状态转换

## 依赖管理

### 自动安装Pillow库

脚本在生成精灵图集时会自动检测并安装Pillow库：

1. **检测缺失** - 首次导入PIL时检测是否已安装
2. **智能安装** - 直接安装到Blender的site-packages目录
3. **立即可用** - 刷新模块缓存，无需重启Blender
4. **多重兜底** - 失败时尝试不同的安装策略

### 安装过程
```
⚠ Pillow库未安装，正在自动安装...
🔧 使用Python: /path/to/blender/python
🔧 目标安装路径: /blender/python/lib/site-packages
🔧 正在安装Pillow到Blender环境...
✓ Pillow安装到Blender环境成功
🔧 刷新模块缓存...
✓ Pillow安装成功并立即可用
```

## 文件名安全化

### Windows兼容性处理

脚本自动处理动画名中的非法字符，确保Windows文件系统兼容性：

**非法字符替换：**
```
原始动画名: Rig|Rig|Dance_Loop
安全文件名: Rig_Rig_Dance_Loop

非法字符: < > : " / \ | ? * 及ASCII控制字符
替换字符: _ (下划线)
```

**处理规则：**
- 替换所有Windows非法字符为下划线
- 移除文件名开头/结尾的点和空格
- 限制长度不超过100字符
- 确保文件名不为空

**影响范围：**
- 渲染输出目录名
- 精灵图集文件名
- Unity资产名称

### 故障排除

**常见问题1: Pillow模块缓存问题**
```
🔧 刷新模块缓存并重试...
⚠ 导入尝试 1/3 失败
✓ Pillow安装成功并立即可用  # 通常第2-3次重试成功
```

**解决方案：**
- **自动重试** - 脚本会自动重试3次导入，通常能解决缓存问题
- **如果仍失败** - 很少见，可能需要重启Blender或手动安装

**常见问题2: 自动安装失败**
1. 检查网络连接
2. 手动在Blender Python控制台执行：
   ```python
   import subprocess, sys
   subprocess.run([sys.executable, "-m", "pip", "install", "Pillow", "--user"])
   ```
3. 或在系统命令行：
   ```bash
   # Windows
   "C:\Program Files\Blender Foundation\Blender 4.0\4.0\python\bin\python.exe" -m pip install Pillow --user
   
   # Linux/Mac
   /path/to/blender/python -m pip install Pillow --user
   ```

## 路径检测机制

### Unity项目根目录自动发现

脚本使用智能路径搜索来找到Unity项目根目录：

```
当前输出路径: F:\UnityTestProjects\MakeDeadCell\renders
               ↓ 向上搜索
第0级: F:\UnityTestProjects\MakeDeadCell\renders (无Assets)
第1级: F:\UnityTestProjects\MakeDeadCell (检查Assets + ProjectSettings)
       ✓ 找到Unity项目根目录！
```

### 验证标准

一个有效的Unity项目根目录必须同时包含：
- `Assets/` 文件夹 - Unity资产目录
- `ProjectSettings/` 文件夹 - Unity项目设置

### 支持的项目结构

脚本适配各种目录结构：
```
Unity项目/
├── Assets/
├── ProjectSettings/
├── blender_scripts/          # 脚本可在这里
│   └── character_DeadCellTest.py
├── tools/blender/            # 或在这里
│   └── character_DeadCellTest.py
└── art_pipeline/3d_to_2d/    # 或任意深度的子目录
    └── character_DeadCellTest.py
```

### 搜索限制

- **最大深度**: 10级目录（防止无限搜索）
- **搜索方向**: 仅向上搜索父目录
- **失败处理**: 未找到时提供手动复制指引

## 注意事项

- 脚本会清理当前场景的所有对象
- 建议在新的Blender文件中运行
- 渲染大量动画可能需要较长时间
- 确保有足够的磁盘空间存储输出文件
- Unity项目路径自动检测，脚本可在项目内任意位置运行