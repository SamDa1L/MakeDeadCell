# FBX合并工具使用指南

这个工具可以将分离的模型FBX文件和动画FBX文件合并为单个完整的FBX文件。

## 功能特点

- ✅ **智能导入** - 分别处理模型和动画FBX文件
- ✅ **骨骼映射** - 自动匹配模型和动画的骨骼
- ✅ **动画传输** - 将动画数据应用到模型骨骼
- ✅ **批量处理** - 支持多个动画的合并
- ✅ **报告生成** - 详细的合并过程报告
- ✅ **配置灵活** - JSON配置文件支持

## 快速开始

### 1. 准备文件

确保你有以下文件：
- **模型FBX**: 包含网格和骨骼的模型文件
- **动画FBX**: 包含动画数据的文件

```
示例文件结构:
F:\UnityTestProjects\ArtAssests\人物\测试3\
├── Meshes\
│   └── HumanM_Model.fbx          # 模型+骨骼
└── Animations\Male\Idles\
    └── HumanM@Idle01-Idle02.fbx  # 动画数据
```

### 2. 配置脚本

有两种方式配置文件路径：

#### 方法1: 直接编辑脚本
```python
# 在 fbx_merge_blender.py 中修改这些路径
self.model_path = r"F:\你的路径\模型文件.fbx"
self.animation_path = r"F:\你的路径\动画文件.fbx"
self.output_path = r"F:\你的路径\输出目录"
self.output_filename = "合并后的文件名.fbx"
```

#### 方法2: 使用配置文件
修改 `fbx_merge_config.json` 中的路径:
```json
{
    "model_path": "F:\\你的路径\\模型文件.fbx",
    "animation_path": "F:\\你的路径\\动画文件.fbx",
    "output_path": "F:\\你的路径\\输出目录",
    "output_filename": "合并后的文件名.fbx"
}
```

### 3. 在Blender中运行

1. **打开Blender**
2. **切换到Scripting工作区**
3. **点击"Open"按钮加载脚本**
   - 选择 `fbx_merge_blender.py` 文件
4. **点击"Run Script"按钮执行**

### 4. 查看结果

脚本完成后会生成：
- **合并的FBX文件** - 包含模型和动画的完整文件
- **合并报告** (`merge_report.json`) - 详细的处理信息

## 工作流程

### 📥 导入阶段
1. **清理场景** - 删除现有对象
2. **导入模型** - 加载网格和骨骼
3. **导入动画** - 提取动画数据

### 🔍 分析阶段  
4. **骨骼映射** - 匹配模型和动画的骨骼名称
5. **兼容性检查** - 验证动画是否适用于模型

### 🎭 合并阶段
6. **动画传输** - 将动画应用到模型骨骼
7. **数据清理** - 移除临时对象，保留必需数据

### 📤 导出阶段
8. **FBX导出** - 生成合并后的文件
9. **报告生成** - 创建处理摘要

## 配置选项详解

### 基本设置
```json
{
    "model_path": "模型FBX文件的完整路径",
    "animation_path": "动画FBX文件的完整路径", 
    "output_path": "输出目录路径",
    "output_filename": "输出文件名.fbx"
}
```

### 高级设置
```json
{
    "settings": {
        "clear_scene_before_import": true,     // 导入前清理场景
        "preserve_original_actions": false,   // 是否保留原始动作
        "auto_rename_merged_actions": true,   // 自动重命名合并的动作
        "bone_matching_mode": "exact",        // 骨骼匹配模式
        "export_scale": 1.0,                  // 导出缩放
        "bake_animations": true               // 烘焙动画
    }
}
```

### FBX导入/导出设置
```json
{
    "fbx_import_settings": {
        "global_scale": 1.0,
        "primary_bone_axis": "Y",
        "secondary_bone_axis": "X"
    },
    "fbx_export_settings": {
        "add_leaf_bones": false,              // 关闭叶子骨生成（Unity/UE推荐）
        "bake_anim": true,                    // 烘焙动画
        "bake_anim_use_all_bones": true,      // 烘焙所有骨骼
        "bake_anim_use_nla_strips": true,     // 烘焙NLA轨道
        "bake_anim_use_all_actions": true,    // 烘焙所有动作（确保完整导出）
        "bake_anim_step": 1.0                 // 动画采样步长
    }
}
```

## 输出文件说明

### 1. 合并的FBX文件
- **包含**: 完整的模型网格 + 骨骼 + 所有动画
- **格式**: 标准FBX格式，兼容Unity、UE等引擎
- **动画**: 烘焙后的动画数据，确保兼容性

### 2. 合并报告 (merge_report.json)
```json
{
    "timestamp": "2025-01-15 14:30:00",
    "input_files": {
        "model": "输入的模型文件路径",
        "animation": "输入的动画文件路径"
    },
    "output_file": "输出文件路径",
    "statistics": {
        "model_bones": 54,
        "mesh_objects": 1,
        "imported_animations": 2,
        "bone_mappings": 52
    },
    "imported_animations": ["Idle01", "Idle02"],
    "bone_mapping": {"Hips": "Hips", "Spine": "Spine", ...}
}
```

## 常见问题和解决方案

### ❓ 问题1: "模型文件不存在"
**解决方案:**
- 检查文件路径是否正确
- 确保使用反斜杠转义 (`\\`) 或原始字符串 (`r"..."`)
- 验证文件确实存在且可访问

### ❓ 问题2: "未找到骨骼对象"
**解决方案:**
- 确认模型FBX文件包含骨骼（Armature对象）
- 检查FBX导入设置，确保启用了动画导入
- 尝试在Blender中手动导入FBX查看内容

### ❓ 问题3: "没有找到可传输的动画"
**解决方案:**
- 确认动画FBX文件包含动画数据
- 检查动画文件路径和文件格式
- 验证动画文件不为空

### ❓ 问题4: "骨骼名称不匹配"
**解决方案:**
- 检查模型和动画的骨骼命名是否一致
- 使用3D软件查看两个文件的骨骼层次结构
- 考虑重新导出FBX时保持骨骼名称一致

### ❓ 问题5: "动画在Unity中不播放"
**解决方案:**
- 确保在Unity中正确设置Animation Controller
- 检查动画剪辑是否正确导入
- 验证动画循环设置

## 导出设置最佳实践

### 🎯 Unity/UE引擎优化建议

#### 骨骼设置优化
```python
add_leaf_bones=False  # 【推荐】关闭叶子骨生成
```
**原因**：
- Unity/UE不需要叶子骨骼，它们只是Blender中用于IK链结束的标记
- 多余的叶子骨会增加文件大小和导入复杂度
- 可能导致动画重定向时的问题

#### 动画烘焙设置
```python
bake_anim=True                    # 启用动画烘焙
bake_anim_use_all_bones=True      # 烘焙所有骨骼（确保完整性）
bake_anim_use_nla_strips=True     # 烘焙NLA轨道（多动画支持）
bake_anim_use_all_actions=True    # 烘焙所有动作（防止遗漏）
```
**原因**：
- `bake_anim_use_all_actions=True`：确保复制的动作被正确导出
- `bake_anim_use_nla_strips=True`：支持NLA轨道中的动画导出
- 双重保险确保无论动画在Active Action还是NLA中都能导出

#### 轴向设置
```python
primary_bone_axis='Y'     # Y轴向前（Blender标准）
secondary_bone_axis='X'   # X轴向右
```
**兼容性**：适配Unity/UE的标准骨骼轴向

### ⚙️ 导出参数对照表

| 参数 | 值 | 用途 | Unity影响 |
|------|----|----- |-----------|
| `add_leaf_bones` | `False` | 不生成叶子骨 | 减少无用骨骼 |
| `bake_anim_use_all_actions` | `True` | 烘焙所有动作 | 确保动画完整 |
| `bake_anim_use_nla_strips` | `True` | 烘焙NLA条带 | 支持多动画 |
| `bake_anim_force_startend_keying` | `True` | 强制首尾关键帧 | 动画循环正确 |
| `bake_anim_step` | `1.0` | 每帧采样 | 动画精度最高 |

### 🔧 常见导出问题和解决方案

#### 问题1: Unity中动画不播放
**可能原因**：
- `bake_anim_use_all_actions=False`
- 动作未挂载到骨骼的Active Action或NLA

**解决方案**：
- 确保`bake_anim_use_all_actions=True`
- 脚本已自动处理动作挂载

#### 问题2: 骨骼层次结构混乱
**可能原因**：
- `add_leaf_bones=True`生成了额外骨骼

**解决方案**：
- 设置`add_leaf_bones=False`
- 重新导出FBX

#### 问题3: 动画重定向失败
**可能原因**：
- 骨骼轴向不匹配
- 叶子骨干扰

**解决方案**：
- 使用标准轴向设置：`primary_bone_axis='Y'`，`secondary_bone_axis='X'`
- 关闭叶子骨生成

## 高级用法

### 批量合并多个动画文件

如果你有多个动画文件需要合并到同一个模型，可以：

1. **修改脚本中的动画路径列表**
2. **循环调用动画导入函数**
3. **最后一次性导出所有动画**

示例修改：
```python
# 在 __init__ 方法中
self.animation_paths = [
    r"F:\path\to\animation1.fbx",
    r"F:\path\to\animation2.fbx", 
    r"F:\path\to\animation3.fbx"
]

# 在 run 方法中循环导入
for anim_path in self.animation_paths:
    self.animation_path = anim_path
    self.import_animation_fbx()
```

### 自定义骨骼映射

对于骨骼名称不完全匹配的情况，可以创建自定义映射：

```python
# 在 analyze_bone_mapping 方法中添加
custom_mapping = {
    "LeftArm": "L_Arm",
    "RightArm": "R_Arm",
    "LeftLeg": "L_Leg", 
    "RightLeg": "R_Leg"
}
self.bone_mapping.update(custom_mapping)
```

### 过滤特定动画

只合并特定名称的动画：
```python
# 在 transfer_animations_to_model 方法中添加过滤
animation_filter = ["Idle", "Walk", "Run"]
filtered_actions = [action for action in self.imported_actions 
                   if any(keyword in action.name for keyword in animation_filter)]
```

## 性能优化建议

### 🚀 加速导入
- 关闭不必要的FBX导入选项（如材质、纹理）
- 对于动画文件，禁用网格导入
- 使用较低的动画采样率（如果可接受）

### 💾 减少文件大小  
- 启用动画压缩
- 移除不必要的骨骼
- 优化关键帧数量

### 🔧 调试模式
在脚本开头添加调试标志：
```python
DEBUG = True

# 在关键位置添加调试输出
if DEBUG:
    print(f"调试: 当前处理 {action.name}")
```

## 版本历史

### v1.0.0 (2025-01-15)
- ✅ 初始版本发布
- ✅ 基本的模型和动画合并功能
- ✅ 骨骼映射和动画传输
- ✅ JSON报告生成

### 计划中的功能
- 🔄 批量处理多个文件对
- 🎯 智能骨骼名称匹配算法  
- 🔧 图形用户界面
- 📊 更详细的统计信息
- 🎨 动画预览功能

## 代码最佳实践说明

### 🔧 集合比较稳健性

工具采用了以下最佳实践确保稳定性：

#### 动作集合比较
```python
# ✅ 推荐：使用名称集合比较
actions_before = {action.name for action in bpy.data.actions}
actions_after = {action.name for action in bpy.data.actions}
new_action_names = actions_after - actions_before

# ❌ 避免：直接使用对象集合差（可能不稳健）
# actions_before = set(bpy.data.actions)  
# actions_after = set(bpy.data.actions)
# new_actions = actions_after - actions_before
```

**原因**：
- **ID对象生命周期问题**：Python对象ID可能在Blender内部操作中改变
- **内存地址不稳定**：集合差基于对象内存地址，可能导致意外结果
- **调试友好**：名称比较更容易跟踪和调试

#### 对象集合比较
```python
# ✅ 合适：场景对象集合差（对象引用稳定）
objects_before = set(bpy.context.scene.objects)
objects_after = set(bpy.context.scene.objects)
new_objects = objects_after - objects_before
```

**原因**：场景中的对象引用在导入操作中保持稳定，集合差是安全的。

### 📝 其他编码最佳实践

- **错误处理**：所有文件操作都有异常处理
- **路径安全**：使用`os.path.join()`构建路径
- **编码声明**：文件头部声明UTF-8编码
- **类型检查**：动态检查对象类型和属性存在性

## 技术支持

如果遇到问题：
1. 查看控制台输出的错误信息
2. 检查生成的 `merge_report.json` 文件
3. 确保Blender版本兼容（建议3.0+）
4. 验证FBX文件的完整性

---
**作者**: Claude Code Assistant  
**版本**: 1.0.1  
**更新**: 2025-01-15