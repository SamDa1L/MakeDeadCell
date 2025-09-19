#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
FBX合并工具 - 将模型FBX和动画FBX合并为单个文件
作者: Claude Code Assistant
版本: 1.0.0
日期: 2025-01-15

功能：
- 导入模型FBX（网格+骨骼）
- 导入动画FBX（动画数据）
- 智能骨骼匹配和动画传输
- 导出合并后的FBX文件

使用方法：
1. 在Blender中打开此脚本
2. 修改配置部分的文件路径
3. 运行脚本
"""

import bpy
import bmesh
import os
import json
from mathutils import Vector, Matrix, Euler
import time
from collections import defaultdict

class FBXMerger:
    def __init__(self, config_file=None):
        """初始化FBX合并器"""
        # 默认配置
        self.model_path = r"F:\UnityTestProjects\ArtAssests\人物\测试3\Meshes\HumanM_Model.fbx"
        self.animation_path = r"F:\UnityTestProjects\ArtAssests\人物\测试3\Animations\Male\Idles\HumanM@Idle01-Idle02.fbx"
        self.output_path = r"F:\UnityTestProjects\MakeDeadCell\merged_output"
        self.output_filename = "HumanM_Merged.fbx"
        
        # 加载配置文件（如果提供）
        if config_file and os.path.exists(config_file):
            self.load_config(config_file)
        else:
            # 尝试加载同目录下的配置文件
            script_dir = os.path.dirname(__file__)
            default_config = os.path.join(script_dir, "fbx_merge_config.json")
            if os.path.exists(default_config):
                self.load_config(default_config)
        
        # 内部状态
        self.model_armature = None
        self.animation_armature = None
        self.temp_animation_armature = None  # 临时动画骨骼对象，用于骨骼映射后删除
        self.imported_actions = []
        self.bone_mapping = {}  # 骨骼名称映射
        
        print("🔧 FBX合并器已初始化")
        print(f"模型文件: {self.model_path}")
        print(f"动画文件: {self.animation_path}")
        print(f"输出路径: {os.path.join(self.output_path, self.output_filename)}")
    
    def load_config(self, config_file):
        """从JSON配置文件加载设置"""
        try:
            with open(config_file, 'r', encoding='utf-8') as f:
                config = json.load(f)
            
            # 更新路径配置
            self.model_path = config.get('model_path', self.model_path)
            self.animation_path = config.get('animation_path', self.animation_path) 
            self.output_path = config.get('output_path', self.output_path)
            self.output_filename = config.get('output_filename', self.output_filename)
            
            # 可以在这里添加更多配置项的加载
            print(f"✓ 已加载配置文件: {config_file}")
            
        except Exception as e:
            print(f"⚠ 无法加载配置文件 {config_file}: {e}")
            print("  使用默认配置")
    
    def clear_scene(self):
        """清理当前场景"""
        print("\n🧹 清理场景...")
        
        # 选择所有对象并删除
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete()
        
        # 清理未使用的数据块
        bpy.ops.outliner.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
        
        print("  ✓ 场景已清理")
    
    def import_model_fbx(self):
        """导入模型FBX文件"""
        print(f"\n📥 导入模型文件: {os.path.basename(self.model_path)}")
        
        if not os.path.exists(self.model_path):
            raise FileNotFoundError(f"模型文件不存在: {self.model_path}")
        
        # FBX导入设置 - 保留模型和骨骼
        bpy.ops.import_scene.fbx(
            filepath=self.model_path,
            use_manual_orientation=True,
            global_scale=1.0,
            bake_space_transform=False,
            use_custom_normals=True,
            use_image_search=True,
            use_alpha_decals=False,
            decal_offset=0.0,
            use_anim=True,  # 导入现有动画
            anim_offset=1.0,
            use_subsurf=False,
            use_custom_props=True,
            use_custom_props_enum_as_string=True,
            ignore_leaf_bones=False,
            force_connect_children=False,
            automatic_bone_orientation=False,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            use_prepost_rot=True
        )
        
        # 查找导入的骨骼对象
        for obj in bpy.context.scene.objects:
            if obj.type == 'ARMATURE':
                self.model_armature = obj
                break
        
        if self.model_armature:
            print(f"  ✓ 找到模型骨骼: {self.model_armature.name}")
            print(f"  ├─ 骨骼数量: {len(self.model_armature.data.bones)}")
            
            # 显示前几个骨骼名称
            bone_names = [bone.name for bone in self.model_armature.data.bones][:5]
            print(f"  ├─ 主要骨骼: {', '.join(bone_names)}...")
        else:
            raise RuntimeError("未找到模型骨骼对象")
        
        # 查找和显示网格对象
        mesh_objects = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
        print(f"  ├─ 网格数量: {len(mesh_objects)}")
        for mesh_obj in mesh_objects:
            print(f"  ├─ 网格: {mesh_obj.name}")
    
    def import_animation_fbx(self):
        """导入动画FBX文件并提取动画数据"""
        print(f"\n📥 导入动画文件: {os.path.basename(self.animation_path)}")
        
        if not os.path.exists(self.animation_path):
            raise FileNotFoundError(f"动画文件不存在: {self.animation_path}")
        
        # 记录导入前的动作名称和对象名称（使用名称集合更稳健）
        actions_before = {action.name for action in bpy.data.actions}
        objects_before = set(bpy.context.scene.objects)
        
        # FBX导入设置 - 只关注动画数据
        bpy.ops.import_scene.fbx(
            filepath=self.animation_path,
            use_manual_orientation=True,
            global_scale=1.0,
            bake_space_transform=False,
            use_custom_normals=False,
            use_image_search=False,
            use_alpha_decals=False,
            decal_offset=0.0,
            use_anim=True,  # 重点：导入动画
            anim_offset=1.0,
            use_subsurf=False,
            use_custom_props=False,
            use_custom_props_enum_as_string=True,
            ignore_leaf_bones=False,
            force_connect_children=False,
            automatic_bone_orientation=False,
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            use_prepost_rot=True
        )
        
        # 识别新导入的动作（通过名称比较，比ID对象集合差更稳健）
        actions_after = {action.name for action in bpy.data.actions}
        new_action_names = actions_after - actions_before
        
        # 获取对应的动作对象
        # 注：使用名称集合比较的优势：
        # 1. 避免Python ID对象生命周期问题
        # 2. 更稳定的集合操作，不受对象内存地址影响
        # 3. 便于调试和日志记录
        self.imported_actions = []
        for action in bpy.data.actions:
            if action.name in new_action_names:
                self.imported_actions.append(action)
        
        print(f"  ✓ 导入了 {len(self.imported_actions)} 个动画:")
        for action in self.imported_actions:
            frame_range = action.frame_range
            print(f"  ├─ {action.name}: {frame_range[0]:.0f}-{frame_range[1]:.0f} 帧")
        
        # 查找动画骨骼对象（用于骨骼匹配）
        objects_after = set(bpy.context.scene.objects)
        new_objects = objects_after - objects_before
        
        # 分类新导入的对象
        animation_armature_obj = None
        mesh_objects_to_remove = []
        other_objects_to_remove = []
        
        for obj in new_objects:
            if obj.type == 'ARMATURE':
                animation_armature_obj = obj
                self.animation_armature = obj
            elif obj.type == 'MESH':
                mesh_objects_to_remove.append(obj)
            else:
                other_objects_to_remove.append(obj)
        
        if self.animation_armature:
            print(f"  ├─ 动画骨骼: {self.animation_armature.name}")
            print(f"  ├─ 骨骼数量: {len(self.animation_armature.data.bones)}")
        
        # 只删除网格和其他非骨骼对象，保留动画骨骼用于后续的骨骼映射分析
        objects_to_remove_now = mesh_objects_to_remove + other_objects_to_remove
        
        if objects_to_remove_now:
            print(f"  ├─ 删除动画文件中的网格对象: {len(mesh_objects_to_remove)} 个")
            print(f"  ├─ 删除其他对象: {len(other_objects_to_remove)} 个")
            for obj in objects_to_remove_now:
                bpy.data.objects.remove(obj, do_unlink=True)
        
        # 保存动画骨骼对象的引用，稍后在骨骼映射完成后删除
        self.temp_animation_armature = animation_armature_obj
        
        print("  ✓ 清理了动画文件的网格对象，保留动画骨骼用于骨骼映射")
    
    def analyze_bone_mapping(self):
        """分析和创建骨骼映射"""
        print("\n🦴 分析骨骼映射...")
        
        if not self.model_armature:
            raise RuntimeError("模型骨骼未找到")
        
        model_bones = set(bone.name for bone in self.model_armature.data.bones)
        print(f"  ├─ 模型骨骼数量: {len(model_bones)}")
        
        if self.animation_armature:
            anim_bones = set(bone.name for bone in self.animation_armature.data.bones)
            print(f"  ├─ 动画骨骼数量: {len(anim_bones)}")
            
            # 直接匹配的骨骼
            direct_matches = model_bones.intersection(anim_bones)
            print(f"  ├─ 直接匹配: {len(direct_matches)} 个骨骼")
            
            # 创建映射（这里使用直接映射，可以扩展为更复杂的匹配算法）
            self.bone_mapping = {bone_name: bone_name for bone_name in direct_matches}
            
            # 显示未匹配的骨骼
            model_only = model_bones - anim_bones
            anim_only = anim_bones - model_bones
            
            if model_only:
                print(f"  ├─ 模型独有骨骼: {len(model_only)} 个")
                if len(model_only) <= 5:
                    print(f"  │   {', '.join(list(model_only)[:5])}")
                else:
                    print(f"  │   {', '.join(list(model_only)[:5])}...")
            
            if anim_only:
                print(f"  ├─ 动画独有骨骼: {len(anim_only)} 个")
                if len(anim_only) <= 5:
                    print(f"  │   {', '.join(list(anim_only)[:5])}")
                else:
                    print(f"  │   {', '.join(list(anim_only)[:5])}...")
        else:
            # 如果没有动画骨骼参考，假设所有动作都适用于模型骨骼
            print("  ├─ 无动画骨骼参考，假设动作适用于所有模型骨骼")
            self.bone_mapping = {bone_name: bone_name for bone_name in model_bones}
        
        print(f"  ✓ 骨骼映射完成: {len(self.bone_mapping)} 对映射")
    
    def cleanup_animation_objects(self):
        """清理动画导入时的临时对象"""
        print("\n🧹 清理动画导入的临时对象...")
        
        if hasattr(self, 'temp_animation_armature') and self.temp_animation_armature:
            try:
                # 确保对象仍然存在且有效
                if self.temp_animation_armature.name in bpy.data.objects:
                    print(f"  ├─ 删除临时动画骨骼: {self.temp_animation_armature.name}")
                    bpy.data.objects.remove(self.temp_animation_armature, do_unlink=True)
                    self.temp_animation_armature = None
                    print("  ✓ 临时动画骨骼已删除")
                else:
                    print("  ├─ 临时动画骨骼已不存在，跳过删除")
            except Exception as e:
                print(f"  ⚠ 删除临时动画骨骼时出错: {e}")
        
        # 清理对动画骨骼的引用，因为它已经不需要了
        self.animation_armature = None
    
    def transfer_animations_to_model(self):
        """将动画传输到模型骨骼并设置为活动动画或NLA轨道"""
        print("\n🎭 传输动画到模型骨骼...")
        
        if not self.imported_actions:
            print("  ⚠ 没有找到可传输的动画")
            return
        
        if not self.model_armature:
            raise RuntimeError("模型骨骼未找到")
        
        # 确保模型骨骼有动画数据
        if not self.model_armature.animation_data:
            self.model_armature.animation_data_create()
        
        transferred_actions = []
        transferred_count = 0
        
        for action in self.imported_actions:
            print(f"  ├─ 处理动画: {action.name}")
            
            # 创建动作的副本并重命名
            new_action_name = f"{action.name}_merged"
            new_action = action.copy()
            new_action.name = new_action_name
            
            # 检查动作是否包含模型骨骼的关键帧
            valid_fcurves = 0
            for fcurve in new_action.fcurves:
                if fcurve.data_path.startswith('pose.bones['):
                    # 提取骨骼名称
                    bone_name_start = fcurve.data_path.find('[\"') + 2
                    bone_name_end = fcurve.data_path.find('\"]', bone_name_start)
                    if bone_name_start > 1 and bone_name_end > bone_name_start:
                        bone_name = fcurve.data_path[bone_name_start:bone_name_end]
                        
                        # 检查这个骨骼是否存在于模型中
                        if bone_name in self.bone_mapping and bone_name in [b.name for b in self.model_armature.data.bones]:
                            valid_fcurves += 1
            
            if valid_fcurves > 0:
                print(f"  │   ✓ 有效关键帧通道: {valid_fcurves}")
                transferred_actions.append(new_action)
                transferred_count += 1
            else:
                print(f"  │   ⚠ 未找到匹配的骨骼关键帧")
                # 删除无效的动作副本
                bpy.data.actions.remove(new_action)
        
        print(f"  ✓ 成功传输 {transferred_count} 个动画")
        
        # 【关键修复】将动画挂载到模型骨骼
        if transferred_actions:
            self.setup_animations_on_model(transferred_actions)
        
        # 显示模型骨骼现在拥有的所有动作
        available_actions = [action for action in bpy.data.actions 
                           if action.name.endswith('_merged') or 
                           any(fcurve.data_path.startswith('pose.bones[') and 
                               any(bone.name in fcurve.data_path for bone in self.model_armature.data.bones)
                               for fcurve in action.fcurves)]
        
        print(f"  ├─ 模型骨骼可用动作: {len(available_actions)} 个")
        for action in available_actions[:3]:  # 只显示前3个
            print(f"  │   - {action.name}")
        if len(available_actions) > 3:
            print(f"  │   ... 还有 {len(available_actions) - 3} 个")
    
    def setup_animations_on_model(self, actions):
        """将动画正确设置到模型骨骼上"""
        print("\n🎪 设置动画到模型骨骼...")
        
        if not actions:
            print("  ⚠ 没有有效动画需要设置")
            return
        
        animation_data = self.model_armature.animation_data
        
        # 方案1: 设置第一个动画为Active Action（用于预览和简单导出）
        first_action = actions[0]
        animation_data.action = first_action
        print(f"  ├─ 设置Active Action: {first_action.name}")
        
        # 方案2: 同时创建NLA轨道（用于多动画导出）
        print("  ├─ 创建NLA轨道...")
        
        # 确保NLA轨道存在
        if not animation_data.nla_tracks:
            print("  │   创建新的NLA轨道集合")
        
        # 为每个动画创建独立的NLA轨道
        for i, action in enumerate(actions):
            # 创建新的NLA轨道
            track_name = f"Track_{action.name}"
            nla_track = animation_data.nla_tracks.new()
            nla_track.name = track_name
            
            # 获取动画的帧范围
            frame_start, frame_end = action.frame_range
            
            # 计算轨道的时间偏移（避免重叠）
            if i == 0:
                strip_start = frame_start
            else:
                # 后续动画放在前一个动画结束之后
                strip_start = frame_end + (frame_end - frame_start) * i
            
            # 创建NLA条带
            strip = nla_track.strips.new(action.name, int(strip_start), action)
            strip.action = action
            
            # 设置条带属性
            strip.use_auto_blend = True
            strip.blend_in = 5.0   # 5帧的融入
            strip.blend_out = 5.0  # 5帧的融出
            
            print(f"  │   ✓ 创建NLA条带: {action.name} ({strip_start:.0f}-{strip_start + (frame_end - frame_start):.0f})")
        
        # 设置动画数据的播放模式
        animation_data.use_nla = True  # 启用NLA
        
        print(f"  ✓ 完成动画设置: Active Action + {len(actions)} 个NLA轨道")
        
        # 验证设置
        self.verify_animation_setup()
    
    def verify_animation_setup(self):
        """验证动画设置是否正确"""
        print("\n🔍 验证动画设置...")
        
        animation_data = self.model_armature.animation_data
        
        # 检查Active Action
        if animation_data.action:
            print(f"  ├─ ✓ Active Action: {animation_data.action.name}")
        else:
            print("  ├─ ⚠ 没有Active Action")
        
        # 检查NLA轨道
        if animation_data.nla_tracks:
            print(f"  ├─ ✓ NLA轨道数量: {len(animation_data.nla_tracks)}")
            for track in animation_data.nla_tracks:
                print(f"  │   - {track.name}: {len(track.strips)} 个条带")
                for strip in track.strips:
                    print(f"  │     * {strip.name}: {strip.frame_start:.0f}-{strip.frame_end:.0f}")
        else:
            print("  ├─ ⚠ 没有NLA轨道")
        
        # 检查NLA模式
        print(f"  ├─ NLA模式: {'启用' if animation_data.use_nla else '禁用'}")
        
        # 检查总的动画帧范围
        if animation_data.action:
            action_range = animation_data.action.frame_range
            print(f"  └─ 活动动画帧范围: {action_range[0]:.0f}-{action_range[1]:.0f}")
    
    def setup_scene_animation_range(self):
        """设置场景动画范围以包含所有动画"""
        print("  ├─ 设置场景动画范围...")
        
        if not self.model_armature or not self.model_armature.animation_data:
            print("  │   ⚠ 没有动画数据，使用默认范围")
            return
        
        animation_data = self.model_armature.animation_data
        
        # 计算所有动画的总范围
        min_frame = float('inf')
        max_frame = float('-inf')
        
        # 检查Active Action
        if animation_data.action:
            action_range = animation_data.action.frame_range
            min_frame = min(min_frame, action_range[0])
            max_frame = max(max_frame, action_range[1])
        
        # 检查NLA轨道
        if animation_data.nla_tracks:
            for track in animation_data.nla_tracks:
                for strip in track.strips:
                    min_frame = min(min_frame, strip.frame_start)
                    max_frame = max(max_frame, strip.frame_end)
        
        # 设置场景帧范围
        if min_frame != float('inf') and max_frame != float('-inf'):
            bpy.context.scene.frame_start = int(min_frame)
            bpy.context.scene.frame_end = int(max_frame)
            bpy.context.scene.frame_current = int(min_frame)
            
            print(f"  │   ✓ 场景动画范围: {int(min_frame)}-{int(max_frame)} 帧")
        else:
            print("  │   ⚠ 无法确定动画范围，保持默认设置")
    
    def setup_output_directory(self):
        """创建输出目录"""
        if not os.path.exists(self.output_path):
            os.makedirs(self.output_path)
            print(f"✓ 创建输出目录: {self.output_path}")
    
    def export_merged_fbx(self):
        """导出合并后的FBX文件"""
        print(f"\n📤 导出合并的FBX文件...")
        
        self.setup_output_directory()
        output_file = os.path.join(self.output_path, self.output_filename)
        
        # 设置场景动画范围以包含所有动画
        self.setup_scene_animation_range()
        
        # 确保模型骨骼被选中和激活
        bpy.ops.object.select_all(action='DESELECT')
        self.model_armature.select_set(True)
        bpy.context.view_layer.objects.active = self.model_armature
        
        # 选择所有相关对象（网格 + 骨骼）
        for obj in bpy.context.scene.objects:
            if obj.type in ['MESH', 'ARMATURE']:
                obj.select_set(True)
        
        # FBX导出设置
        bpy.ops.export_scene.fbx(
            filepath=output_file,
            use_selection=True,  # 只导出选中的对象
            use_active_collection=False,
            global_scale=1.0,
            apply_unit_scale=True,
            apply_scale_options='FBX_SCALE_NONE',
            bake_space_transform=False,
            object_types={'ARMATURE', 'MESH'},
            use_mesh_modifiers=True,
            use_mesh_modifiers_render=True,
            mesh_smooth_type='OFF',
            use_subsurf=False,
            use_mesh_edges=False,
            use_tspace=False,
            use_custom_props=False,
            add_leaf_bones=False,             # 【优化】关闭叶子骨生成，避免Unity/UE中的多余骨骼
            primary_bone_axis='Y',
            secondary_bone_axis='X',
            use_armature_deform_only=False,
            armature_nodetype='NULL',
            bake_anim=True,  # 重要：烘焙动画
            bake_anim_use_all_bones=True,
            bake_anim_use_nla_strips=True,    # 使用NLA条带
            bake_anim_use_all_actions=True,   # 【关键】烘焙所有动作，确保完整导出
            bake_anim_force_startend_keying=True,
            bake_anim_step=1.0,
            bake_anim_simplify_factor=1.0,
            path_mode='AUTO',
            embed_textures=False,
            batch_mode='OFF',
            use_batch_own_dir=True,
            use_metadata=True
        )
        
        print(f"  ✓ FBX文件已导出: {output_file}")
        
        # 显示文件信息
        if os.path.exists(output_file):
            file_size = os.path.getsize(output_file)
            print(f"  ├─ 文件大小: {file_size / (1024*1024):.2f} MB")
        
        return output_file
    
    def generate_report(self, output_file):
        """生成合并报告"""
        print("\n📋 生成合并报告...")
        
        report = {
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "input_files": {
                "model": self.model_path,
                "animation": self.animation_path
            },
            "output_file": output_file,
            "statistics": {
                "model_armature": self.model_armature.name if self.model_armature else None,
                "model_bones": len(self.model_armature.data.bones) if self.model_armature else 0,
                "mesh_objects": len([obj for obj in bpy.context.scene.objects if obj.type == 'MESH']),
                "imported_animations": len(self.imported_actions),
                "bone_mappings": len(self.bone_mapping),
                "available_actions": len([action for action in bpy.data.actions])
            },
            "imported_animations": [action.name for action in self.imported_actions],
            "bone_mapping": self.bone_mapping
        }
        
        report_file = os.path.join(self.output_path, "merge_report.json")
        with open(report_file, 'w', encoding='utf-8') as f:
            json.dump(report, f, indent=2, ensure_ascii=False)
        
        print(f"  ✓ 报告已生成: {report_file}")
        
        # 控制台摘要
        stats = report["statistics"]
        print(f"\n📊 合并摘要:")
        print(f"  ├─ 模型骨骼: {stats['model_bones']} 个骨骼")
        print(f"  ├─ 网格对象: {stats['mesh_objects']} 个")
        print(f"  ├─ 导入动画: {stats['imported_animations']} 个")
        print(f"  ├─ 骨骼映射: {stats['bone_mappings']} 对")
        print(f"  └─ 可用动作: {stats['available_actions']} 个")
    
    def run(self):
        """执行完整的FBX合并流程"""
        print("🚀 开始FBX合并流程...")
        start_time = time.time()
        
        try:
            # 1. 清理场景
            self.clear_scene()
            
            # 2. 导入模型
            self.import_model_fbx()
            
            # 3. 导入动画
            self.import_animation_fbx()
            
            # 4. 分析骨骼映射
            self.analyze_bone_mapping()
            
            # 5. 清理动画导入的临时对象（骨骼映射完成后）
            self.cleanup_animation_objects()
            
            # 6. 传输动画
            self.transfer_animations_to_model()
            
            # 7. 导出合并的FBX
            output_file = self.export_merged_fbx()
            
            # 8. 生成报告
            self.generate_report(output_file)
            
            elapsed_time = time.time() - start_time
            print(f"\n✅ FBX合并完成！耗时: {elapsed_time:.2f} 秒")
            
        except Exception as e:
            print(f"\n❌ 合并过程中发生错误: {str(e)}")
            import traceback
            traceback.print_exc()
            raise

def main():
    """主函数 - 脚本入口点"""
    print("=" * 60)
    print("🔧 FBX合并工具 v1.0.0")
    print("=" * 60)
    
    # 创建合并器实例并运行
    merger = FBXMerger()
    merger.run()

# 脚本执行入口
if __name__ == "__main__":
    main()