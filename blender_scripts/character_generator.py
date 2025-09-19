import bpy
import bmesh
from mathutils import Vector, Matrix
import math

class CharacterParameters:
    """参数化人物模型的配置类"""
    
    def __init__(self):
        # 身体比例参数 (Dead Cells风格)
        self.total_height = 2.0
        self.head_size = 0.15
        self.torso_height = 0.6
        self.upper_torso_height = 0.35
        self.lower_torso_height = 0.25
        self.leg_length = 0.8
        self.upper_leg_length = 0.4
        self.lower_leg_length = 0.4
        
        # 手臂参数
        self.arm_length = 0.5
        self.upper_arm_length = 0.25
        self.forearm_length = 0.25
        
        # 身体宽度参数
        self.shoulder_width = 0.3
        self.torso_width = 0.25
        self.hip_width = 0.22
        self.head_width = 0.12
        
        # 手脚参数
        self.hand_size = 0.08
        self.foot_length = 0.15
        self.foot_width = 0.06

class CharacterGenerator:
    """死亡细胞风格的人物模型生成器"""
    
    def __init__(self, params=None):
        self.params = params if params else CharacterParameters()
        self.character_name = "DeadCellsCharacter"
        
    def clear_scene(self):
        """清理场景中的所有对象"""
        # 确保在对象模式
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)
        
    def create_character(self):
        """创建完整的人物模型"""
        self.clear_scene()
        
        # 创建身体各部分
        body_parts = {
            'head': self.create_head(),
            'torso': self.create_torso(),
            'left_arm': self.create_arm(side='left'),
            'right_arm': self.create_arm(side='right'),
            'left_leg': self.create_leg(side='left'),
            'right_leg': self.create_leg(side='right')
        }
        
        # 合并所有身体部分
        character_mesh = self.merge_body_parts(body_parts)
        
        # 创建骨骼系统
        armature = self.create_armature()
        
        # 绑定网格到骨骼
        self.bind_mesh_to_armature(character_mesh, armature)
        
        return character_mesh, armature
    
    def create_head(self):
        """创建头部"""
        # 使用UV球体创建头部
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=self.params.head_size/2,
            location=(0, 0, self.params.total_height - self.params.head_size/2)
        )
        head = bpy.context.active_object
        head.name = "Head"
        
        return head
    
    def create_torso(self):
        """创建躯干"""
        # 使用圆柱体创建躯干
        bpy.ops.mesh.primitive_cylinder_add(
            radius=self.params.torso_width/2,
            depth=self.params.torso_height,
            location=(0, 0, self.params.total_height - self.params.head_size - self.params.torso_height/2)
        )
        torso = bpy.context.active_object
        torso.name = "Torso"
        
        # 稍微压扁躯干，使其更像人体
        torso.scale = Vector((1.0, 0.7, 1.0))
        
        return torso
    
    def create_arm(self, side='left'):
        """创建手臂 - T-pose姿态"""
        x_multiplier = 1 if side == 'left' else -1
        shoulder_z = self.params.total_height - self.params.head_size - 0.1
        
        # 上臂 - 水平向外延伸 (T-pose)
        upper_arm_x = self.params.shoulder_width/2 + self.params.upper_arm_length/2 * x_multiplier
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.04,
            depth=self.params.upper_arm_length,
            location=(upper_arm_x, 0, shoulder_z)
        )
        upper_arm = bpy.context.active_object
        upper_arm.name = f"{side.capitalize()}_UpperArm"
        # 旋转90度使其水平
        upper_arm.rotation_euler = (0, 0, math.radians(90 * x_multiplier))
        
        # 前臂 - 继续水平延伸
        forearm_x = self.params.shoulder_width/2 + self.params.upper_arm_length + self.params.forearm_length/2 * x_multiplier
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.03,
            depth=self.params.forearm_length,
            location=(forearm_x, 0, shoulder_z)
        )
        forearm = bpy.context.active_object
        forearm.name = f"{side.capitalize()}_Forearm"
        # 旋转90度使其水平
        forearm.rotation_euler = (0, 0, math.radians(90 * x_multiplier))
        
        # 手 - 球形手掌
        hand_x = self.params.shoulder_width/2 + self.params.upper_arm_length + self.params.forearm_length + self.params.hand_size/2 * x_multiplier
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=self.params.hand_size/2,
            location=(hand_x, 0, shoulder_z)
        )
        hand = bpy.context.active_object
        hand.name = f"{side.capitalize()}_Hand"
        hand.scale = Vector((1.2, 0.8, 0.6))  # 手掌形状
        
        # 合并手臂部分
        bpy.ops.object.select_all(action='DESELECT')
        upper_arm.select_set(True)
        forearm.select_set(True)
        hand.select_set(True)
        bpy.context.view_layer.objects.active = upper_arm
        bpy.ops.object.join()
        
        return upper_arm
    
    def create_leg(self, side='left'):
        """创建腿部"""
        x_offset = self.params.hip_width/2 * (1 if side == 'left' else -1)
        
        # 大腿
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.05,
            depth=self.params.upper_leg_length,
            location=(
                x_offset,
                0,
                self.params.total_height - self.params.head_size - self.params.torso_height - self.params.upper_leg_length/2
            )
        )
        upper_leg = bpy.context.active_object
        upper_leg.name = f"{side.capitalize()}_UpperLeg"
        
        # 小腿
        bpy.ops.mesh.primitive_cylinder_add(
            radius=0.04,
            depth=self.params.lower_leg_length,
            location=(
                x_offset,
                0,
                self.params.total_height - self.params.head_size - self.params.torso_height - self.params.upper_leg_length - self.params.lower_leg_length/2
            )
        )
        lower_leg = bpy.context.active_object
        lower_leg.name = f"{side.capitalize()}_LowerLeg"
        
        # 脚 - 椭圆形
        bpy.ops.mesh.primitive_uv_sphere_add(
            radius=0.05,
            location=(
                x_offset,
                self.params.foot_length/4,
                self.params.total_height - self.params.head_size - self.params.torso_height - self.params.leg_length - 0.03
            )
        )
        foot = bpy.context.active_object
        foot.name = f"{side.capitalize()}_Foot"
        foot.scale = Vector((self.params.foot_width, self.params.foot_length, 0.6))
        
        # 合并腿部部分
        bpy.ops.object.select_all(action='DESELECT')
        upper_leg.select_set(True)
        lower_leg.select_set(True)
        foot.select_set(True)
        bpy.context.view_layer.objects.active = upper_leg
        bpy.ops.object.join()
        
        return upper_leg
    
    def merge_body_parts(self, body_parts):
        """合并所有身体部分成一个网格"""
        # 确保在对象模式
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
            
        bpy.ops.object.select_all(action='DESELECT')
        
        # 选择所有身体部分
        for part in body_parts.values():
            if part is not None:  # 防止空对象
                part.select_set(True)
        
        # 设置活动对象为躯干
        bpy.context.view_layer.objects.active = body_parts['torso']
        
        # 合并
        bpy.ops.object.join()
        
        character_mesh = bpy.context.active_object
        character_mesh.name = self.character_name
        
        return character_mesh
    
    def create_armature(self):
        """创建骨骼系统"""
        bpy.ops.object.armature_add(location=(0, 0, 0))
        armature = bpy.context.active_object
        armature.name = f"{self.character_name}_Armature"
        
        # 进入编辑模式创建骨骼
        bpy.context.view_layer.objects.active = armature
        bpy.ops.object.mode_set(mode='EDIT')
        
        # 清除默认骨骼
        bpy.ops.armature.select_all(action='SELECT')
        bpy.ops.armature.delete()
        
        # 创建骨骼层次结构
        self.create_bone_hierarchy()
        
        bpy.ops.object.mode_set(mode='OBJECT')
        
        return armature
    
    def create_bone_hierarchy(self):
        """创建骨骼层次结构"""
        # 获取骨架对象的edit_bones
        edit_bones = bpy.context.active_object.data.edit_bones
        
        # 创建根骨骼 (Hip)
        hip_bone = edit_bones.new('Hip')
        hip_bone.head = Vector((0, 0, self.params.total_height - self.params.head_size - self.params.torso_height))
        hip_bone.tail = Vector((0, 0, self.params.total_height - self.params.head_size - self.params.torso_height/2))
        
        # 创建脊椎骨骼
        spine_bone = edit_bones.new('Spine')
        spine_bone.head = hip_bone.tail
        spine_bone.tail = Vector((0, 0, self.params.total_height - self.params.head_size - self.params.head_size/2))
        spine_bone.parent = hip_bone
        
        # 创建头部骨骼
        head_bone = edit_bones.new('Head')
        head_bone.head = spine_bone.tail
        head_bone.tail = Vector((0, 0, self.params.total_height))
        head_bone.parent = spine_bone
        
        # 创建左右手臂骨骼 (T-pose)
        for side in ['Left', 'Right']:
            x_multiplier = 1 if side == 'Left' else -1
            shoulder_z = spine_bone.tail.z
            
            # 肩膀
            shoulder_bone = edit_bones.new(f'{side}_Shoulder')
            shoulder_bone.head = Vector((0, 0, shoulder_z))
            shoulder_bone.tail = Vector((self.params.shoulder_width/2 * x_multiplier, 0, shoulder_z))
            shoulder_bone.parent = spine_bone
            
            # 上臂 - 水平向外延伸
            upper_arm_bone = edit_bones.new(f'{side}_UpperArm')
            upper_arm_bone.head = shoulder_bone.tail
            upper_arm_bone.tail = Vector((
                (self.params.shoulder_width/2 + self.params.upper_arm_length) * x_multiplier, 
                0, 
                shoulder_z
            ))
            upper_arm_bone.parent = shoulder_bone
            
            # 前臂 - 继续水平延伸
            forearm_bone = edit_bones.new(f'{side}_Forearm')
            forearm_bone.head = upper_arm_bone.tail
            forearm_bone.tail = Vector((
                (self.params.shoulder_width/2 + self.params.upper_arm_length + self.params.forearm_length) * x_multiplier,
                0,
                shoulder_z
            ))
            forearm_bone.parent = upper_arm_bone
            
            # 手
            hand_bone = edit_bones.new(f'{side}_Hand')
            hand_bone.head = forearm_bone.tail
            hand_bone.tail = Vector((
                (self.params.shoulder_width/2 + self.params.upper_arm_length + self.params.forearm_length + self.params.hand_size) * x_multiplier,
                0,
                shoulder_z
            ))
            hand_bone.parent = forearm_bone
        
        # 创建左右腿部骨骼
        for side in ['Left', 'Right']:
            x_offset = self.params.hip_width/2 * (1 if side == 'Left' else -1)
            
            # 大腿
            upper_leg_bone = edit_bones.new(f'{side}_UpperLeg')
            upper_leg_bone.head = hip_bone.head
            upper_leg_bone.tail = Vector((x_offset, 0, hip_bone.head.z - self.params.upper_leg_length))
            upper_leg_bone.parent = hip_bone
            
            # 小腿
            lower_leg_bone = edit_bones.new(f'{side}_LowerLeg')
            lower_leg_bone.head = upper_leg_bone.tail
            lower_leg_bone.tail = Vector((x_offset, 0, upper_leg_bone.tail.z - self.params.lower_leg_length))
            lower_leg_bone.parent = upper_leg_bone
            
            # 脚
            foot_bone = edit_bones.new(f'{side}_Foot')
            foot_bone.head = lower_leg_bone.tail
            foot_bone.tail = Vector((x_offset, self.params.foot_length/2, lower_leg_bone.tail.z))
            foot_bone.parent = lower_leg_bone
    
    def bind_mesh_to_armature(self, mesh, armature):
        """将网格绑定到骨骼"""
        # 确保在对象模式
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
            
        # 选择网格和骨骼
        bpy.ops.object.select_all(action='DESELECT')
        mesh.select_set(True)
        armature.select_set(True)
        bpy.context.view_layer.objects.active = armature
        
        # 创建父子关系并自动权重
        bpy.ops.object.parent_set(type='ARMATURE_AUTO')


def create_dead_cells_character():
    """主函数：创建死亡细胞风格的角色"""
    
    # 可调整的参数 - 在这里修改来调整角色比例
    params = CharacterParameters()
    
    # 示例：调整参数创建不同风格的角色
    # params.total_height = 2.2        # 总高度
    # params.torso_height = 0.7        # 躯干高度
    # params.leg_length = 0.9          # 腿长
    # params.arm_length = 0.6          # 手臂长度
    # params.shoulder_width = 0.35     # 肩宽
    
    # 创建角色生成器
    generator = CharacterGenerator(params)
    
    # 生成角色
    character_mesh, armature = generator.create_character()
    
    print(f"成功创建死亡细胞风格角色: {character_mesh.name}")
    print("可以通过修改 CharacterParameters 类中的参数来调整角色比例")
    
    return character_mesh, armature


# 运行脚本
if __name__ == "__main__":
    create_dead_cells_character()