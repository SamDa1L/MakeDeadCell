# 死亡细胞风格角色生成器

这个Blender Python脚本可以快速生成具有完整骨骼系统的参数化人物模型，专为3D转2D的制作流程设计。

## 功能特点

- ✅ 标准人体形状模型生成（圆形头部、圆柱体四肢）
- ✅ T-pose标准姿态
- ✅ 完整的骨骼系统（包含IK约束支持）
- ✅ 自动权重绑定
- ✅ 参数化身体比例调整
- ✅ 真实人体几何形状
- ✅ 适合3D转2D渲染流程

## 使用方法

### 1. 在Blender中运行

1. 打开Blender
2. 打开文本编辑器 (Text Editor)
3. 点击 "Open" 加载 `character_generator.py`
4. 点击 "Run Script" 执行脚本

### 2. 参数调整

在脚本中找到 `create_dead_cells_character()` 函数，修改参数来调整角色：

```python
def create_dead_cells_character():
    params = CharacterParameters()
    
    # 调整这些参数来改变角色比例：
    params.total_height = 2.0        # 总高度
    params.torso_height = 0.6        # 躯干高度  
    params.upper_torso_height = 0.35 # 上半身高度
    params.lower_torso_height = 0.25 # 下半身高度
    params.leg_length = 0.8          # 腿长
    params.upper_leg_length = 0.4    # 大腿长度
    params.lower_leg_length = 0.4    # 小腿长度
    params.arm_length = 0.5          # 手臂长度
    params.upper_arm_length = 0.25   # 上臂长度
    params.forearm_length = 0.25     # 前臂长度
    params.shoulder_width = 0.3      # 肩宽
    params.torso_width = 0.25        # 躯干宽度
    params.hip_width = 0.22          # 臀宽
```

## 可调参数说明

### 身体高度
- `total_height`: 角色总高度
- `head_size`: 头部大小
- `torso_height`: 躯干总高度
- `upper_torso_height`: 上半身高度
- `lower_torso_height`: 下半身高度

### 四肢长度
- `leg_length`: 腿部总长度
- `upper_leg_length`: 大腿长度
- `lower_leg_length`: 小腿长度
- `arm_length`: 手臂总长度
- `upper_arm_length`: 上臂长度
- `forearm_length`: 前臂长度

### 身体宽度
- `shoulder_width`: 肩膀宽度
- `torso_width`: 躯干宽度
- `hip_width`: 臀部宽度
- `head_width`: 头部宽度

### 手脚细节
- `hand_size`: 手部大小
- `foot_length`: 脚长
- `foot_width`: 脚宽

## 骨骼系统

生成的模型包含完整的骨骼层次结构：

```
Hip (根骨骼)
├── Spine (脊椎)
│   ├── Head (头部)
│   ├── Left_Shoulder → Left_UpperArm → Left_Forearm → Left_Hand
│   └── Right_Shoulder → Right_UpperArm → Right_Forearm → Right_Hand
├── Left_UpperLeg → Left_LowerLeg → Left_Foot
└── Right_UpperLeg → Right_LowerLeg → Right_Foot
```

## 3D转2D工作流程建议

1. **模型准备**: 使用此脚本生成基础角色模型
2. **动画制作**: 在Blender中为角色添加动画
3. **渲染设置**: 
   - 使用正交相机 (Orthographic Camera)
   - 设置固定视角 (通常是侧视图)
   - 调整光照为平面光照
4. **材质设置**: 使用 Toon BSDF 或者 Emission 材质
5. **渲染输出**: 渲染为PNG序列帧
6. **后期处理**: 在图像编辑软件中进行像素化处理

## 模型特点

生成的人物模型具有以下特点：
- **头部**: 使用UV球体，更加真实的圆形头部
- **躯干**: 使用圆柱体，略微压扁模拟人体形状
- **四肢**: 使用圆柱体，符合人体比例
- **手掌**: 椭圆形球体，模拟手掌形状
- **脚部**: 椭圆形，适合行走动画
- **姿态**: 标准T-pose，便于动画制作

## 注意事项

- 脚本会清理场景中的所有现有对象
- 生成的模型自动进行权重绑定
- 建议在新的Blender文件中运行脚本
- 模型专为侧视角渲染优化

## 扩展功能

如需添加更多功能，可以扩展 `CharacterGenerator` 类：
- 添加面部特征
- 增加服装配件
- 自定义材质
- IK约束设置
- 动画模板