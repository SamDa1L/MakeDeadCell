import bpy
import os
import json
import sys
import time
from mathutils import Vector
import math

class DeadCellsRenderPipeline:
    """死亡细胞风格角色渲染流水线"""
    
    def __init__(self):
        # 获取脚本所在目录 - 在Blender中运行时需要特殊处理
        self.script_dir = self.detect_script_directory()
        print(f"检测到的脚本目录: {self.script_dir}")
        
        # 加载配置并设置路径
        self.load_config_and_setup()
        
    def detect_script_directory(self):
        """智能检测脚本所在目录"""
        # 方法1: 尝试使用__file__
        try:
            if '__file__' in globals() and __file__:
                detected_dir = os.path.dirname(os.path.abspath(__file__))
                print(f"方法1(__file__): {detected_dir}")
                # 验证这个目录是否合理
                if os.path.exists(detected_dir) and not detected_dir.endswith(('/', '\\')):
                    return detected_dir
        except (NameError, AttributeError):
            pass
        
        # 方法2: 使用Blender的blend文件目录
        try:
            if bpy.data.is_saved:
                blend_dir = os.path.dirname(bpy.data.filepath)
                blender_scripts_dir = os.path.join(blend_dir, "blender_scripts")
                print(f"方法2(blend文件): {blender_scripts_dir}")
                if os.path.exists(blender_scripts_dir):
                    return blender_scripts_dir
        except:
            pass
        
        # 方法3: 使用已知的脚本目录位置
        known_script_dir = r"F:\UnityTestProjects\MakeDeadCell\blender_scripts"
        print(f"方法3(已知路径): {known_script_dir}")
        if os.path.exists(known_script_dir):
            return known_script_dir
        
        # 方法4: 尝试通过当前工作目录推断
        cwd = os.getcwd()
        possible_dirs = [
            os.path.join(cwd, "blender_scripts"),
            os.path.join(os.path.dirname(cwd), "blender_scripts"),
            cwd  # 最后的备选
        ]
        
        for possible_dir in possible_dirs:
            print(f"方法4(尝试): {possible_dir}")
            if os.path.exists(possible_dir):
                return possible_dir
        
        # 如果都失败了，返回当前工作目录
        print(f"所有方法都失败，使用当前工作目录: {cwd}")
        return cwd
    
    def load_config_and_setup(self):
        """加载配置并设置路径"""
        config_path = os.path.join(self.script_dir, "config.json")
        
        # 加载配置
        self.config = self.load_config(config_path)
        
        # 设置路径（智能处理绝对路径和相对路径）
        fbx_config_path = self.config['fbx_path']
        output_config_path = self.config['output_path']
        
        # 如果是绝对路径，直接使用；如果是相对路径，基于脚本目录解析
        if os.path.isabs(fbx_config_path):
            self.fbx_path = os.path.normpath(fbx_config_path)
        else:
            self.fbx_path = os.path.normpath(os.path.abspath(
                os.path.join(self.script_dir, fbx_config_path)
            ))
            
        if os.path.isabs(output_config_path):
            self.output_path = os.path.normpath(output_config_path)
        else:
            self.output_path = os.path.normpath(os.path.abspath(
                os.path.join(self.script_dir, output_config_path)
            ))
        
        # 从配置文件读取其他设置
        self.character_name = self.config.get('character_name', 'DeadCellsCharacter')
        self.render_resolution = tuple(self.config['render_settings']['resolution'])
        self.frame_rate = self.config['render_settings']['frame_rate']
        
        # 死亡细胞调色板（从配置文件读取）
        colors_config = self.config['dead_cells_colors']
        self.dead_cells_colors = {
            key: tuple(value) for key, value in colors_config.items()
        }
        
        # 死亡细胞像素艺术色阶调色板
        self.dead_cells_palette = {
            'skin': {
                'shadow': (0.4, 0.2, 0.15, 1.0),    # 暗部
                'mid': (0.7, 0.5, 0.35, 1.0),       # 中间色
                'highlight': (0.9, 0.75, 0.55, 1.0), # 亮部
                'rim': (1.0, 0.9, 0.8, 1.0)         # 边缘光
            },
            'cloth': {
                'shadow': (0.1, 0.15, 0.3, 1.0),
                'mid': (0.2, 0.3, 0.6, 1.0),
                'highlight': (0.4, 0.5, 0.8, 1.0),
                'rim': (0.6, 0.7, 0.9, 1.0)
            },
            'metal': {
                'shadow': (0.2, 0.2, 0.2, 1.0),
                'mid': (0.5, 0.5, 0.5, 1.0),
                'highlight': (0.8, 0.8, 0.8, 1.0),
                'rim': (1.0, 1.0, 1.0, 1.0)
            }
        }
        
        # 存储骨骼对象引用
        self.armature = None
        self.original_mesh = None  # 原始高精度网格
        self.render_mesh = None    # 渲染用低精度网格
        self.smart_camera = None   # 智能相机对象
        
        # 超时控制
        self.start_time = time.time()
        self.timeout_seconds = 10 * 60  # 10分钟
        self.timeout_warned = False
        self.should_abort = False
        
        # 检测EEVEE版本（在配置加载完成后）
        self.eevee_engine = self.detect_eevee_engine()
    
    def detect_eevee_engine(self):
        """检测可用的EEVEE渲染引擎版本"""
        try:
            # 获取渲染引擎的可用选项
            render_prop = bpy.context.scene.render.bl_rna.properties['engine']
            available_engines = [item.identifier for item in render_prop.enum_items]
            print(f"可用渲染引擎: {available_engines}")
            
            # 优先使用新版本EEVEE
            if 'BLENDER_EEVEE_NEXT' in available_engines:
                print("✓ 检测到 BLENDER_EEVEE_NEXT")
                return 'BLENDER_EEVEE_NEXT'
            elif 'EEVEE' in available_engines:
                print("✓ 检测到 EEVEE")
                return 'EEVEE'
            elif 'CYCLES' in available_engines:
                print("✓ 检测到 CYCLES")
                return 'CYCLES'
            else:
                print("⚠ 未找到常见渲染引擎，使用默认")
                return available_engines[0] if available_engines else 'CYCLES'
                
        except Exception as e:
            print(f"⚠ 检测渲染引擎时出错: {e}")
            print("使用默认的CYCLES引擎")
            return 'CYCLES'
    
    def ensure_eevee_engine(self):
        """确保使用正确的EEVEE渲染引擎"""
        # 懒加载：如果还没有检测过引擎，现在检测
        if self.eevee_engine is None:
            self.eevee_engine = self.detect_eevee_engine()
            
        current_engine = bpy.context.scene.render.engine
        
        if current_engine != self.eevee_engine:
            print(f"⚠ 当前渲染引擎: {current_engine}, 正在切换到: {self.eevee_engine}")
            bpy.context.scene.render.engine = self.eevee_engine
        else:
            print(f"✓ 渲染引擎已正确设置: {self.eevee_engine}")
    
    def get_eevee_settings(self):
        """获取EEVEE设置对象（兼容新旧版本）"""
        scene = bpy.context.scene
        
        # 尝试新版本的EEVEE Next设置
        if hasattr(scene, 'eevee') and self.eevee_engine == 'BLENDER_EEVEE_NEXT':
            return scene.eevee
        # 尝试旧版本的EEVEE设置
        elif hasattr(scene, 'eevee') and self.eevee_engine == 'EEVEE':
            return scene.eevee
        # 如果都不可用，返回None（使用Cycles等其他引擎）
        else:
            print("⚠ EEVEE设置不可用，跳过EEVEE特定配置")
            return None
    
    def load_config(self, config_path):
        """加载JSON配置文件"""
        try:
            if not os.path.exists(config_path):
                print(f"警告: 配置文件不存在: {config_path}")
                print("正在创建默认配置文件...")
                self.create_default_config(config_path)
            
            with open(config_path, 'r', encoding='utf-8') as f:
                config = json.load(f)
            
            print(f"成功加载配置文件: {config_path}")
            return config
            
        except json.JSONDecodeError as e:
            print(f"错误: 配置文件格式错误: {e}")
            raise
        except Exception as e:
            print(f"错误: 无法读取配置文件: {e}")
            raise
    
    def create_default_config(self, config_path):
        """创建默认配置文件"""
        default_config = {
            "fbx_path": r"F:\UnityTestProjects\ArtAssests\人物\测试2\Animation Library[Standard]\Unity\AnimationLibrary_Unity_Standard.fbx",
            "output_path": r"F:\UnityTestProjects\MakeDeadCell\renders",
            "character_name": "DeadCellsCharacter",
            "render_settings": {
                "resolution": [256, 256],
                "frame_rate": 12
            },
            "dead_cells_colors": {
                "skin": [0.8, 0.6, 0.4, 1.0],
                "cloth": [0.2, 0.3, 0.6, 1.0],
                "metal": [0.5, 0.5, 0.5, 1.0],
                "accent": [0.8, 0.2, 0.2, 1.0]
            },
            "toon_material_settings": {
                "use_hard_shadows": True,
                "color_steps": 4,
                "rim_light_strength": 0.3,
                "shadow_strength": 0.7
            },
            "automation": {
                "auto_render": True,   # 默认自动渲染，提升开箱即用体验
                "render_limit": 5,     # 默认渲染前5个动画，平衡速度与测试需求
                "skip_confirmation": True  # 默认跳过确认，避免GUI环境阻塞
            },
            "camera_settings": {
                "auto_adjust": True,
                "margin_ratio": 0.15,
                "min_ortho_scale": 0.5,
                "max_ortho_scale": 15.0,
                "per_animation_adjustment": True,
                "clip_start": 0.01,
                "clip_end_multiplier": 10.0,
                "min_clip_end": 100.0
            }
        }
        
        try:
            with open(config_path, 'w', encoding='utf-8') as f:
                json.dump(default_config, f, indent=4, ensure_ascii=False)
            print(f"默认配置文件已创建: {config_path}")
        except Exception as e:
            print(f"错误: 无法创建配置文件: {e}")
            raise
    
    def validate_paths(self):
        """验证文件路径是否有效"""
        print(f"=== 路径验证 ===")
        print(f"脚本目录: {self.script_dir}")
        print(f"配置FBX路径: {self.config['fbx_path']}")
        print(f"解析后FBX路径: {self.fbx_path}")
        
        if not os.path.exists(self.fbx_path):
            print(f"❌ 错误: FBX文件不存在: {self.fbx_path}")
            
            # 提供诊断信息
            parent_dir = os.path.dirname(self.fbx_path)
            if os.path.exists(parent_dir):
                print(f"📁 父目录存在: {parent_dir}")
                print(f"📂 父目录内容:")
                try:
                    for item in os.listdir(parent_dir):
                        print(f"    - {item}")
                except Exception as e:
                    print(f"    无法列出目录内容: {e}")
            else:
                print(f"📁 父目录也不存在: {parent_dir}")
            
            return False
        
        # 创建输出目录（如果不存在）
        try:
            os.makedirs(self.output_path, exist_ok=True)
            print(f"✅ FBX路径验证成功: {self.fbx_path}")
            print(f"✅ 输出路径: {self.output_path}")
        except Exception as e:
            print(f"❌ 无法创建输出目录: {e}")
            return False
            
        return True
    
    def find_armature(self):
        """查找骨骼对象"""
        for obj in bpy.context.scene.objects:
            if obj.type == 'ARMATURE':
                return obj
        return None
    
    def find_main_mesh_by_armature(self):
        """通过骨骼对象找到主要绑定的网格"""
        armature = self.find_armature()
        if not armature:
            return None
        
        # 查找被此骨骼驱动的网格
        bound_meshes = []
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH':
                for modifier in obj.modifiers:
                    if modifier.type == 'ARMATURE' and modifier.object == armature:
                        bound_meshes.append(obj)
                        break
        
        if not bound_meshes:
            return None
        
        # 选择面数最多的网格（通常是主体）
        main_mesh = max(bound_meshes, key=lambda mesh: len(mesh.data.polygons))
        print(f"通过骨骼关系找到 {len(bound_meshes)} 个绑定网格，选择主网格: {main_mesh.name} ({len(main_mesh.data.polygons)} 面)")
        
        return main_mesh
    
    def find_main_mesh_by_collection(self):
        """按集合或层级筛选找主网格"""
        # 优先查找特定集合中的网格
        character_collections = ['Character', 'Body', 'Main', 'Root', 'Mannequin']
        
        for collection_name in character_collections:
            if collection_name in bpy.data.collections:
                collection = bpy.data.collections[collection_name]
                meshes = [obj for obj in collection.objects if obj.type == 'MESH']
                if meshes:
                    # 返回顶点数最多的
                    main_mesh = max(meshes, key=lambda mesh: len(mesh.data.vertices))
                    print(f"在集合 '{collection_name}' 中找到主网格: {main_mesh.name}")
                    return main_mesh
        
        return None
    
    def find_main_mesh_by_priority(self):
        """按优先级和顶点数找到主网格"""
        meshes = [obj for obj in bpy.context.scene.objects if obj.type == 'MESH']
        
        if not meshes:
            return None
        
        # 优先级规则
        def mesh_priority(mesh):
            name_lower = mesh.name.lower()
            vertex_count = len(mesh.data.vertices)
            
            # 名称优先级评分
            if any(keyword in name_lower for keyword in ['body', 'character', 'mannequin', 'main', 'base']):
                name_score = 10000
            elif any(keyword in name_lower for keyword in ['torso', 'chest', 'trunk']):
                name_score = 5000
            elif any(keyword in name_lower for keyword in ['weapon', 'sword', 'gun', 'tool', 'blade', 'rifle']):
                name_score = -10000  # 负分，避免选中武器
            elif any(keyword in name_lower for keyword in ['accessory', 'hat', 'helmet', 'glove', 'boot', 'shoe']):
                name_score = -5000   # 配件优先级低
            elif any(keyword in name_lower for keyword in ['hair', 'eye', 'teeth', 'tongue']):
                name_score = -3000   # 面部细节优先级低
            else:
                name_score = 0
            
            # 综合评分：名称优先级 + 顶点数权重
            total_score = name_score + vertex_count
            print(f"网格 '{mesh.name}': 顶点数={vertex_count}, 名称评分={name_score}, 总分={total_score}")
            
            return total_score
        
        main_mesh = max(meshes, key=mesh_priority)
        print(f"通过优先级算法选择主网格: {main_mesh.name}")
        return main_mesh
    
    def find_character_mesh(self):
        """使用多种策略找到角色主网格"""
        print("开始查找角色主网格...")
        
        # 策略1：通过骨骼关系（最可靠）
        print("\n策略1: 通过骨骼关系查找...")
        mesh = self.find_main_mesh_by_armature()
        if mesh:
            print(f"✓ 通过骨骼关系找到主网格: {mesh.name}")
            return mesh
        else:
            print("✗ 骨骼关系查找失败")
        
        # 策略2：按集合筛选
        print("\n策略2: 通过集合筛选查找...")
        mesh = self.find_main_mesh_by_collection()
        if mesh:
            print(f"✓ 通过集合找到主网格: {mesh.name}")
            return mesh
        else:
            print("✗ 集合筛选查找失败")
        
        # 策略3：按优先级和顶点数（备选方案）
        print("\n策略3: 通过优先级算法查找...")
        mesh = self.find_main_mesh_by_priority()
        if mesh:
            print(f"✓ 通过优先级算法找到主网格: {mesh.name}")
            return mesh
        else:
            print("✗ 所有策略均失败")
        
        print("警告: 未找到合适的角色主网格")
        self.debug_scene_objects()  # 显示调试信息
        return None
    
    def debug_scene_objects(self):
        """调试：显示场景中所有对象的信息"""
        print("\n=== 场景对象调试信息 ===")
        
        meshes = []
        armatures = []
        others = []
        
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH':
                vertex_count = len(obj.data.vertices)
                face_count = len(obj.data.polygons)
                meshes.append(f"  🔸 {obj.name} (顶点:{vertex_count}, 面:{face_count})")
            elif obj.type == 'ARMATURE':
                bone_count = len(obj.data.bones)
                armatures.append(f"  🦴 {obj.name} (骨骼:{bone_count})")
            else:
                others.append(f"  ⚪ {obj.name} ({obj.type})")
        
        print(f"网格对象 ({len(meshes)}):")
        for mesh_info in meshes:
            print(mesh_info)
        
        print(f"\n骨骼对象 ({len(armatures)}):")
        for armature_info in armatures:
            print(armature_info)
        
        if others:
            print(f"\n其他对象 ({len(others)}):")
            for other_info in others[:5]:  # 只显示前5个
                print(other_info)
            if len(others) > 5:
                print(f"  ... 还有 {len(others)-5} 个其他对象")
        
        print("========================\n")
    
    def apply_animation_to_armature(self, action):
        """将动画应用到骨骼对象并设置正确的帧范围"""
        if not self.armature:
            self.armature = self.find_armature()
        
        if not self.armature:
            print("错误: 找不到骨骼对象")
            return False
        
        # 【检查点2】确认骨骼对象就是你随后绑定Action的那个self.armature
        print(f"🎯 动画绑定检查：将动画 {action.name} 绑定到骨骼 {self.armature.name}")
        
        # 验证这个骨骼对象是否被网格的Armature修改器引用
        mesh_references = []
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH':
                for mod in obj.modifiers:
                    if mod.type == 'ARMATURE' and mod.object == self.armature:
                        mesh_references.append(obj.name)
        
        if mesh_references:
            print(f"  ├─ ✓ 骨骼被以下网格引用: {', '.join(mesh_references)}")
        else:
            print(f"  ├─ ⚠ 警告：没有网格引用此骨骼，动画可能无法生效")
        
        # 确保骨骼对象有动画数据
        if not self.armature.animation_data:
            self.armature.animation_data_create()
        
        # 处理NLA轨道 - 静音所有轨道，只激活当前动画
        self.setup_nla_for_action(self.armature, action)
        
        # 应用动画到action
        self.armature.animation_data.action = action
        
        # 设置场景帧范围为当前动画的范围
        scene = bpy.context.scene
        start_frame, end_frame = [int(round(x)) for x in action.frame_range]
        scene.frame_start = start_frame
        scene.frame_end = end_frame
        scene.frame_set(start_frame)  # 跳转到开始帧
        
        # 更新视图层以确保变更生效
        bpy.context.view_layer.update()
        
        print(f"✓ 已激活动画 '{action.name}' (帧 {start_frame}-{end_frame}) 到骨骼 '{self.armature.name}'")
        return True
    
    def setup_nla_for_action(self, armature, target_action):
        """设置NLA轨道，确保只播放目标动画"""
        if not armature.animation_data or not armature.animation_data.nla_tracks:
            # 没有NLA轨道，直接返回
            return
        
        print(f"  ├─ 处理NLA轨道以激活动画: {target_action.name}")
        
        # 静音所有轨道
        for track in armature.animation_data.nla_tracks:
            track.mute = True
            if hasattr(track, 'is_solo'):
                track.is_solo = False
        
        # 寻找包含目标动画的轨道和条带
        target_track = None
        target_strip = None
        
        for track in armature.animation_data.nla_tracks:
            for strip in track.strips:
                if strip.action and strip.action.name == target_action.name:
                    target_track = track
                    target_strip = strip
                    break
            if target_track:
                break
        
        # 如果找到了对应的轨道，启用它
        if target_track:
            target_track.mute = False
            if hasattr(target_track, 'is_solo'):
                target_track.is_solo = True
            print(f"  ├─ ✓ 已启用NLA轨道: {target_track.name}")
            
            # 确保条带处于活动状态
            if target_strip:
                target_strip.mute = False
                print(f"  ├─ ✓ 已启用NLA条带: {target_strip.name}")
        else:
            print(f"  ├─ ⚠ 未在NLA轨道中找到动画: {target_action.name}")
    
    def find_armature_for(self, character_obj):
        """为指定角色对象找到对应的骨骼"""
        if character_obj and character_obj.type == 'ARMATURE':
            return character_obj
        
        # 如果传入的是网格，查找其关联的骨骼
        if character_obj and character_obj.type == 'MESH':
            for modifier in character_obj.modifiers:
                if modifier.type == 'ARMATURE' and modifier.object:
                    return modifier.object
        
        # 备用方案：查找场景中的第一个骨骼对象
        for obj in bpy.context.scene.objects:
            if obj.type == 'ARMATURE':
                return obj
        
        return None
    
    def clear_scene(self):
        """清理场景"""
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.object.delete(use_global=False)
        
        # 安全清理材质（只清理项目相关材质，避免删除库材质）
        self.safe_clear_materials()
        
        # 清理对象引用
        self.armature = None
        self.original_mesh = None
        self.render_mesh = None
        self.smart_camera = None
    
    def safe_clear_materials(self, prefix_filters=None):
        """安全清理材质，避免删除外部库材质
        
        Args:
            prefix_filters: 要清理的材质名前缀列表，默认为项目相关前缀
        """
        if prefix_filters is None:
            prefix_filters = [
                "DeadCells_",
                "ToonMaterial",
                "Emission_",
                "Character_"
            ]
        
        materials_to_remove = []
        
        # 1. 按前缀筛选要删除的材质
        for material in bpy.data.materials:
            should_remove = False
            
            # 检查是否匹配任何前缀
            for prefix in prefix_filters:
                if material.name.startswith(prefix):
                    should_remove = True
                    break
            
            # 检查是否为临时生成的材质（通常有数字后缀）
            if material.name.startswith("Material") and material.name != "Material":
                # 匹配 Material.001, Material.002 等
                import re
                if re.match(r"^Material\.\d{3}$", material.name):
                    should_remove = True
            
            if should_remove:
                materials_to_remove.append(material)
        
        # 2. 清理当前场景对象的材质槽（如果有的话）
        cleared_slots = 0
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH' and obj.data and obj.data.materials:
                # 清空材质槽但保留槽数量（避免破坏UV映射等）
                for i in range(len(obj.data.materials)):
                    if obj.data.materials[i]:
                        cleared_slots += 1
                        obj.data.materials[i] = None
        
        # 3. 删除筛选出的材质
        removed_count = 0
        for material in materials_to_remove:
            try:
                # 检查材质是否还被其他地方引用
                if material.users == 0:
                    bpy.data.materials.remove(material)
                    removed_count += 1
                else:
                    print(f"  跳过删除材质 '{material.name}' (仍被引用，用户数: {material.users})")
            except Exception as e:
                print(f"  警告：删除材质 '{material.name}' 时出错: {e}")
        
        if removed_count > 0 or cleared_slots > 0:
            print(f"✓ 安全清理完成: 删除了 {removed_count} 个材质，清空了 {cleared_slots} 个材质槽")
        else:
            print("✓ 材质清理：无需清理的内容")
    
    def import_fbx_character(self):
        """导入FBX角色模型"""
        if not os.path.exists(self.fbx_path):
            print(f"错误: FBX文件不存在: {self.fbx_path}")
            return None
        
        # 导入FBX
        print("正在导入FBX文件...")
        bpy.ops.import_scene.fbx(filepath=self.fbx_path)
        
        # 使用多策略方法查找角色主网格
        character_mesh = self.find_character_mesh()
        
        # 同时查找骨骼对象
        self.armature = self.find_armature()
        
        if character_mesh:
            character_mesh.name = f"{self.character_name}_Original"
            self.original_mesh = character_mesh
            print(f"✓ 成功设置角色网格: {character_mesh.name}")
        else:
            print("✗ 未找到合适的角色网格")
        
        if self.armature:
            print(f"✓ 成功找到骨骼对象: {self.armature.name}")
        else:
            print("⚠ 警告: 未找到骨骼对象，动画功能可能无法正常工作")
        
        # 为导入的纹理应用像素化设置
        print("🔧 为新导入的纹理应用像素化设置...")
        pixel_applied_count = 0
        for image in bpy.data.images:
            if self.apply_pixel_settings_to_image(image):
                pixel_applied_count += 1
        
        if pixel_applied_count > 0:
            print(f"  ├─ 对 {pixel_applied_count} 个纹理应用了像素化设置")
        
        return character_mesh
    
    def ensure_armature_modifier_points_to(self, obj_mesh, armature):
        """确保网格的Armature修改器正确指向指定骨骼，并位于修改器栈顶部"""
        if not obj_mesh or not armature:
            return False
        
        print(f"  ├─ 检查 {obj_mesh.name} 的Armature修改器连接...")
        
        # 寻找现有的Armature修改器
        arm_mod = None
        for modifier in obj_mesh.modifiers:
            if modifier.type == 'ARMATURE':
                arm_mod = modifier
                break
        
        # 如果没有Armature修改器，创建一个
        if arm_mod is None:
            print(f"  ├─ 创建新的Armature修改器...")
            arm_mod = obj_mesh.modifiers.new(name="Armature", type='ARMATURE')
        
        # 确保修改器指向正确的骨骼
        if arm_mod.object is None or arm_mod.object != armature:
            print(f"  ├─ 修正Armature修改器连接: {armature.name}")
            arm_mod.object = armature
            # 确保使用顶点组和骨骼包络
            arm_mod.use_vertex_groups = True
            arm_mod.use_bone_envelopes = True
        else:
            print(f"  ├─ ✓ Armature修改器已正确连接到: {armature.name}")
        
        # 将Armature修改器移动到修改器栈的顶部（在Decimate等之前）
        current_modifiers = list(obj_mesh.modifiers)
        arm_mod_index = current_modifiers.index(arm_mod)
        
        if arm_mod_index > 0:
            print(f"  ├─ 将Armature修改器移动到修改器栈顶部...")
            # 尝试使用operator移动
            bpy.context.view_layer.objects.active = obj_mesh
            for _ in range(arm_mod_index):
                try:
                    bpy.ops.object.modifier_move_up(modifier=arm_mod.name)
                except Exception as e:
                    print(f"  ├─ operator移动失败，使用备用方法: {e}")
                    # 备用方案：重新创建修改器栈
                    self.reorder_modifiers_fallback(obj_mesh, arm_mod)
                    break
        
        print(f"  ├─ ✓ Armature修改器配置完成")
        return True
    
    def reorder_modifiers_fallback(self, obj_mesh, armature_modifier):
        """备用方案：重新排序修改器，确保Armature在最前面"""
        print(f"  ├─ 使用备用方法重排修改器...")
        
        # 保存所有修改器的配置
        modifiers_config = []
        for mod in obj_mesh.modifiers:
            if mod != armature_modifier:
                # 保存修改器配置
                mod_config = {
                    'name': mod.name,
                    'type': mod.type,
                    'settings': {}
                }
                
                # 保存常用设置
                if hasattr(mod, 'ratio'):
                    mod_config['settings']['ratio'] = mod.ratio
                if hasattr(mod, 'use_collapse_triangulate'):
                    mod_config['settings']['use_collapse_triangulate'] = mod.use_collapse_triangulate
                
                modifiers_config.append(mod_config)
        
        # 删除除Armature外的所有修改器
        for mod in list(obj_mesh.modifiers):
            if mod != armature_modifier:
                obj_mesh.modifiers.remove(mod)
        
        # 重新创建其他修改器
        for config in modifiers_config:
            new_mod = obj_mesh.modifiers.new(config['name'], config['type'])
            # 恢复设置
            for setting, value in config['settings'].items():
                if hasattr(new_mod, setting):
                    setattr(new_mod, setting, value)
        
        print(f"  ├─ ✓ 修改器重排完成，Armature位于顶部")
    
    def create_render_copy(self, original_mesh):
        """创建渲染用的优化网格副本，保护原始数据"""
        if not original_mesh:
            return None
        
        # 【检查点1】确认原始网格上确实存在Armature修改器
        armature_mod_found = False
        original_armature_in_modifier = None
        
        print(f"🔍 检查原始网格 {original_mesh.name} 的修改器:")
        for i, modifier in enumerate(original_mesh.modifiers):
            print(f"  ├─ 修改器 {i}: {modifier.name} ({modifier.type})")
            if modifier.type == 'ARMATURE':
                if modifier.object:
                    armature_mod_found = True
                    original_armature_in_modifier = modifier.object
                    print(f"  ├─ ✓ 找到Armature修改器，指向: {modifier.object.name}")
                else:
                    print(f"  ├─ ⚠ Armature修改器存在但未指向任何对象")
        
        if not armature_mod_found:
            print(f"⚠ 警告：原始网格 {original_mesh.name} 没有Armature修改器，可能无法正确跟随骨骼")
            print("  └─ 渲染副本将无法获得骨骼绑定")
        
        print(f"正在创建渲染副本: {original_mesh.name}")
        
        # 1. 复制原始网格
        render_mesh = original_mesh.copy()
        render_mesh.data = original_mesh.data.copy()
        render_mesh.name = f"{self.character_name}_Render"
        
        # 2. 链接到场景
        bpy.context.collection.objects.link(render_mesh)
        
        # 3. 材质槽已通过 data.copy() 自动复制，无需重复添加
        print(f"  ├─ 材质槽数量: {len(render_mesh.data.materials)}")
        if render_mesh.data.materials:
            material_names = [mat.name if mat else "None" for mat in render_mesh.data.materials]
            print(f"  ├─ 继承材质: {', '.join(material_names)}")
        
        # 4. 【关键一致性检查】确保原始网格的骨骼与后续动画绑定的骨骼一致
        original_armature = original_armature_in_modifier or self.find_armature()
        
        if original_armature:
            # 如果self.armature尚未设置，使用从原始网格找到的骨骼
            if not self.armature:
                self.armature = original_armature
                print(f"  ├─ ✓ 设置主骨骼对象: {self.armature.name}")
            # 如果已设置但不一致，发出警告并使用原始网格的骨骼
            elif self.armature != original_armature:
                print(f"  ├─ ⚠ 骨骼不一致！原始网格: {original_armature.name}, 当前主骨骼: {self.armature.name}")
                print(f"  ├─ → 使用原始网格的骨骼以确保一致性")
                self.armature = original_armature
            else:
                print(f"  ├─ ✓ 骨骼对象一致: {self.armature.name}")
            
            # 配置渲染网格的Armature修改器
            self.ensure_armature_modifier_points_to(render_mesh, self.armature)
        else:
            print("⚠ 警告：未找到骨骼对象，渲染网格可能显示为T-pose")
            # 尝试从场景中找到并设置
            if not self.armature:
                self.armature = self.find_armature()
                if self.armature:
                    print(f"  ├─ 从场景找到骨骼: {self.armature.name}")
                    self.ensure_armature_modifier_points_to(render_mesh, self.armature)
        
        # 5. 添加Decimate修改器到渲染副本（会被放置在Armature之后）
        decimate = render_mesh.modifiers.new("RenderDecimate", 'DECIMATE')
        decimate.ratio = 0.3  # 保留30%的面数
        decimate.use_collapse_triangulate = True  # 更好的三角化
        
        # 6. 设置为活动对象并应用Decimate
        bpy.context.view_layer.objects.active = render_mesh
        bpy.ops.object.modifier_apply(modifier="RenderDecimate")
        
        # 7. 隐藏原始网格，显示渲染网格
        original_mesh.hide_viewport = True
        original_mesh.hide_render = True
        render_mesh.hide_viewport = False
        render_mesh.hide_render = False
        
        self.render_mesh = render_mesh
        print(f"渲染副本创建完成: {render_mesh.name}")
        print(f"原始网格已隐藏但保留: {original_mesh.name}")
        
        return render_mesh
    
    def optimize_character_mesh(self, character_mesh):
        """优化角色网格 - 使用安全的副本方式"""
        if not character_mesh:
            return None
        
        # 创建渲染副本而不是直接修改原始网格
        return self.create_render_copy(character_mesh)
    
    def create_dead_cells_toon_material(self, material_name="DeadCells_ToonMaterial", color_type="skin", enable_outline=False):
        """创建像素化死亡细胞材质：Shader to RGB → ColorRamp → Palette Lookup → Emission"""
        
        # 创建材质
        material = bpy.data.materials.new(name=material_name)
        material.use_nodes = True
        nodes = material.node_tree.nodes
        links = material.node_tree.links
        
        # 清除默认节点
        self.safe_clear_collection(nodes, "材质节点")
        
        # === 主材质链 ===
        x_offset = 0
        y_offset = 0
        
        # 1. 创建Toon BSDF节点（光照计算）
        toon_bsdf = nodes.new(type='ShaderNodeBsdfToon')
        toon_bsdf.location = (x_offset, y_offset)
        
        # 安全设置节点输入（兼容不同版本）
        self.safe_set_node_input(toon_bsdf, 'Color', (1.0, 1.0, 1.0, 1.0))
        self.safe_set_node_input(toon_bsdf, 'Roughness', 0.9)
        
        # 设置组件类型（如果可用）
        if hasattr(toon_bsdf, 'component'):
            toon_bsdf.component = 'DIFFUSE'
        
        # 2. Shader to RGB（转换为颜色值）
        x_offset += 200
        shader_to_rgb = nodes.new(type='ShaderNodeShaderToRGB')
        shader_to_rgb.location = (x_offset, y_offset)
        
        # 3. 光照量化ColorRamp（2-3段硬边阴影）
        x_offset += 200
        lighting_ramp = nodes.new(type='ShaderNodeValToRGB')
        lighting_ramp.location = (x_offset, y_offset)
        
        # 设置3段光照：阴影-中调-高光
        lighting_positions = [0.0, 0.4, 0.8]
        lighting_colors = [(0.3, 0.3, 0.3, 1.0), (0.7, 0.7, 0.7, 1.0), (1.0, 1.0, 1.0, 1.0)]
        self.safe_setup_colorramp(lighting_ramp.color_ramp, lighting_positions, lighting_colors, 'CONSTANT')
        
        # 4. 调色板查表ColorRamp（锁定到Dead Cells颜色）
        x_offset += 200
        palette_lookup = nodes.new(type='ShaderNodeValToRGB')
        palette_lookup.location = (x_offset, y_offset)
        
        # 设置Dead Cells调色板颜色
        palette = self.dead_cells_palette[color_type]
        
        # 3色映射：暗 → 中 → 亮
        palette_positions = [0.0, 0.5, 1.0]
        palette_colors = [palette['shadow'], palette['mid'], palette['highlight']]
        self.safe_setup_colorramp(palette_lookup.color_ramp, palette_positions, palette_colors, 'CONSTANT')
        
        # 5. Emission节点（像素化输出）
        x_offset += 200
        emission = nodes.new(type='ShaderNodeEmission')
        emission.location = (x_offset, y_offset)
        self.safe_set_node_input(emission, 'Strength', 1.0)
        
        # 6. 材质输出
        x_offset += 200
        output = nodes.new(type='ShaderNodeOutputMaterial')
        output.location = (x_offset, y_offset)
        
        # === 连接节点链 ===
        links.new(toon_bsdf.outputs['BSDF'], shader_to_rgb.inputs['Shader'])
        links.new(shader_to_rgb.outputs['Color'], lighting_ramp.inputs['Fac'])
        links.new(lighting_ramp.outputs['Color'], palette_lookup.inputs['Fac'])
        links.new(palette_lookup.outputs['Color'], emission.inputs['Color'])
        links.new(emission.outputs['Emission'], output.inputs['Surface'])
        
        # === 描边材质（可选）===
        if enable_outline:
            self.add_outline_material(material, nodes, links, x_offset)
        
        print(f"✓ 像素化材质创建: {material_name} (调色板: {color_type}, 描边: {enable_outline})")
        return material
    
    def add_outline_material(self, material, nodes, links, x_offset):
        """添加描边材质节点（背面法线反转法）- 完整连接版本"""
        
        # 获取现有的主材质emission节点和输出节点
        main_emission = None
        output_node = None
        
        for node in nodes:
            if node.type == 'EMISSION' and node.name != 'outline_emission':
                main_emission = node
            elif node.type == 'OUTPUT_MATERIAL':
                output_node = node
        
        if not main_emission or not output_node:
            print("  ⚠ 描边集成失败：找不到主材质节点")
            return
        
        print("  ├─ 集成背面法线描边到材质输出...")
        
        # === 描边检测节点 ===
        # 几何信息节点
        geometry = nodes.new(type='ShaderNodeNewGeometry')
        geometry.location = (x_offset - 600, -300)
        geometry.name = "outline_geometry"
        
        # === 描边着色器 ===
        # 描边颜色（纯黑emission）
        outline_emission = nodes.new(type='ShaderNodeEmission')
        outline_emission.location = (x_offset - 200, -300)
        outline_emission.name = "outline_emission"
        self.safe_set_node_input(outline_emission, 'Color', (0.0, 0.0, 0.0, 1.0))  # 黑色描边
        self.safe_set_node_input(outline_emission, 'Strength', 1.0)
        
        # === 混合逻辑 ===
        # 背面检测混合器
        backface_mix = nodes.new(type='ShaderNodeMixShader')
        backface_mix.location = (x_offset, -150)
        backface_mix.name = "outline_backface_mix"
        
        # === 完整连接链 ===
        # 1. 背面检测作为混合因子
        links.new(geometry.outputs['Backfacing'], backface_mix.inputs['Fac'])
        
        # 2. 主材质连接到Shader1（正面显示）
        links.new(main_emission.outputs['Emission'], backface_mix.inputs[1])  # Shader1
        
        # 3. 描边材质连接到Shader2（背面显示）  
        links.new(outline_emission.outputs['Emission'], backface_mix.inputs[2])  # Shader2
        
        # 4. 混合结果连接到最终输出
        # 首先断开现有连接
        for link in output_node.inputs['Surface'].links:
            links.remove(link)
        
        # 连接新的混合输出
        links.new(backface_mix.outputs['Shader'], output_node.inputs['Surface'])
        
        # === 可选：描边厚度控制（通过位移） ===
        # 注：真正的描边厚度需要在几何层面实现，这里提供基础框架
        
        print("  ├─ 背面描边节点链已完整连接：")
        print("      主材质(正面) ←→ [背面检测] ←→ 描边(背面) → 输出")
        print("  ├─ 描边颜色：纯黑 (0,0,0)")
        print("  ├─ 混合方式：背面检测自动切换")
        
        # 应用几何描边效果
        self.apply_geometry_outline_effect(material.name)
    
    def apply_geometry_outline_effect(self, material_name, outline_thickness=0.02):
        """为背面法线描边添加几何厚度效果"""
        # 寻找使用此材质的对象
        outline_objects = []
        for obj in bpy.context.scene.objects:
            if obj.type == 'MESH' and obj.data.materials:
                for mat in obj.data.materials:
                    if mat and mat.name == material_name:
                        outline_objects.append(obj)
                        break
        
        for obj in outline_objects:
            # 检查是否已有Solidify修改器
            solidify_mod = None
            for modifier in obj.modifiers:
                if modifier.type == 'SOLIDIFY' and modifier.name.startswith('Outline_'):
                    solidify_mod = modifier
                    break
            
            # 如果没有，添加Solidify修改器用于描边
            if not solidify_mod:
                solidify_mod = obj.modifiers.new(name="Outline_Solidify", type='SOLIDIFY')
                
                # 描边参数
                solidify_mod.thickness = outline_thickness
                solidify_mod.offset = 1.0  # 向外扩展
                solidify_mod.use_flip_normals = True  # 翻转法线
                solidify_mod.use_even_offset = True
                solidify_mod.use_quality_normals = True
                
                # 材质设置：外壳使用描边材质
                solidify_mod.material_offset = 0  # 使用相同材质（依靠背面检测）
                
                # 关键：确保Solidify在Armature修改器之后
                self.move_modifier_after_armature(obj, solidify_mod)
                
                print(f"    ├─ 为 {obj.name} 添加几何描边 (厚度: {outline_thickness}, 在Armature后)")
            else:
                print(f"    ├─ {obj.name} 已有描边修改器")
        
        if not outline_objects:
            print("    ⚠ 未找到使用此材质的对象，无法应用几何描边")
    
    def move_modifier_after_armature(self, obj, target_modifier):
        """将指定修改器移动到Armature修改器之后"""
        if not obj or not target_modifier:
            return
        
        # 确保在对象模式
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        
        # 设置活动对象
        old_active = bpy.context.view_layer.objects.active
        bpy.context.view_layer.objects.active = obj
        
        try:
            # 找到最后一个Armature修改器的索引
            armature_index = -1
            for i, modifier in enumerate(obj.modifiers):
                if modifier.type == 'ARMATURE':
                    armature_index = i
            
            if armature_index == -1:
                print("      ├─ 未找到Armature修改器，Solidify保持默认位置")
                return
            
            # 找到目标修改器的当前索引
            target_index = -1
            for i, modifier in enumerate(obj.modifiers):
                if modifier == target_modifier:
                    target_index = i
                    break
            
            if target_index == -1:
                print("      ├─ 未找到目标修改器")
                return
            
            # 如果Solidify已经在Armature之后，无需移动
            if target_index > armature_index:
                print(f"      ├─ Solidify已在Armature后 (位置 {target_index} > {armature_index})")
                return
            
            # 计算目标位置：Armature之后的第一个位置
            desired_index = armature_index + 1
            
            # 方法1：使用modifier_move_to_index（如果可用）
            try:
                bpy.context.object = obj
                # 直接移动到目标索引
                bpy.ops.object.modifier_move_to_index(modifier=target_modifier.name, index=desired_index)
                print(f"      ├─ 使用move_to_index移动到位置 {desired_index}")
            except (AttributeError, RuntimeError):
                # 方法2：逐步移动（兼容旧版本）
                print(f"      ├─ 使用逐步移动方法")
                moves_needed = desired_index - target_index
                
                for move_step in range(moves_needed):
                    try:
                        current_index = None
                        for i, mod in enumerate(obj.modifiers):
                            if mod == target_modifier:
                                current_index = i
                                break
                        
                        if current_index is not None and current_index < len(obj.modifiers) - 1:
                            bpy.ops.object.modifier_move_down(modifier=target_modifier.name)
                        else:
                            break
                            
                    except Exception as move_error:
                        print(f"      ⚠ 移动步骤 {move_step} 失败: {move_error}")
                        break
            
            # 验证最终位置
            final_index = -1
            for i, modifier in enumerate(obj.modifiers):
                if modifier == target_modifier:
                    final_index = i
                    break
            
            print(f"      ├─ Solidify修改器已移动: {target_index} → {final_index} (Armature在 {armature_index})")
            
        except Exception as e:
            print(f"      ⚠ 修改器排序失败: {e}")
        
        finally:
            # 恢复原来的活动对象
            bpy.context.view_layer.objects.active = old_active
    
    def ensure_freestyle_lineset(self, freestyle_settings):
        """确保有可用的Freestyle lineset（API优先，operator兜底）"""
        # 方法1: 如果已有lineset，直接使用
        if freestyle_settings.linesets:
            print("  ├─ 使用现有的Freestyle lineset")
            return freestyle_settings.linesets[0]
        
        # 方法2: 尝试使用API直接创建
        try:
            print("  ├─ 尝试通过API创建Freestyle lineset")
            lineset = freestyle_settings.linesets.new("DeadCells_Outline")
            if lineset:
                print("  ├─ ✓ API创建成功")
                return lineset
        except Exception as e:
            print(f"  ├─ API创建失败: {e}")
        
        # 方法3: 尝试使用operator创建（兜底方案）
        if not self.is_background_mode():
            try:
                print("  ├─ 尝试通过operator创建Freestyle lineset")
                # 确保上下文正确
                if bpy.context.view_layer and bpy.context.scene:
                    bpy.ops.scene.freestyle_lineset_add()
                    if freestyle_settings.linesets:
                        print("  ├─ ✓ operator创建成功")
                        return freestyle_settings.linesets[0]
            except RuntimeError as e:
                print(f"  ├─ operator创建失败: {e}")
        else:
            print("  ├─ 后台模式下跳过operator方式")
        
        # 方法4: 最后的尝试 - 手动构建最小lineset
        try:
            print("  ├─ 尝试手动构建最小lineset")
            # 这个方法依赖于Blender的具体实现，可能不稳定
            # 但在某些情况下可能是唯一的选择
            # 注意：这个API可能不总是存在
            lineset = None
            if hasattr(freestyle_settings.linesets, 'new'):
                lineset = freestyle_settings.linesets.new("DeadCells_Manual")
            
            if lineset:
                print("  ├─ ✓ 手动构建成功")
                return lineset
        except Exception as e:
            print(f"  ├─ 手动构建失败: {e}")
        
        print("  ├─ ⚠ 所有创建方式都失败")
        return None
    
    def setup_freestyle_outline(self, thickness=2.0, color=(0, 0, 0)):
        """设置Freestyle渲染层描边（API优先，operator兜底）"""
        scene = bpy.context.scene
        
        # 启用Freestyle
        scene.render.use_freestyle = True
        
        # 获取或创建视图层
        view_layer = bpy.context.view_layer
        freestyle_settings = view_layer.freestyle_settings
        
        # 确保有lineset（优先使用API，失败时使用operator）
        lineset = self.ensure_freestyle_lineset(freestyle_settings)
        if lineset is None:
            print("  ├─ 无法创建Freestyle lineset，跳过描边设置")
            return
        
        # 配置lineset
        lineset.name = "DeadCells_Outline"
        
        # 描边设置（添加属性检查，确保兼容性）
        try:
            if hasattr(lineset, 'linestyle') and lineset.linestyle:
                linestyle = lineset.linestyle
                
                # 基础设置
                if hasattr(linestyle, 'color'):
                    linestyle.color = color
                if hasattr(linestyle, 'thickness'):
                    linestyle.thickness = thickness
                
                # 链接设置
                if hasattr(linestyle, 'use_chaining'):
                    linestyle.use_chaining = True
                if hasattr(linestyle, 'chaining'):
                    linestyle.chaining = 'PLAIN'  # 简单连接
                    
                print(f"  ├─ ✓ Freestyle描边样式配置完成 (厚度: {thickness})")
            else:
                print("  ├─ ⚠ linestyle不可用，跳过样式设置")
            
            # 边缘类型设置
            if hasattr(lineset, 'edge_type_negation'):
                lineset.edge_type_negation = 'EXCLUSIVE'
            if hasattr(lineset, 'edge_type_combination'):
                lineset.edge_type_combination = 'OR'
                
        except Exception as e:
            print(f"  ├─ 配置lineset时出错: {e}")
            print("  ├─ 继续使用基础设置")
            
        # 边缘选择设置（安全的属性检查）
        edge_selections = {
            'select_silhouette': True,
            'select_border': True, 
            'select_crease': False,
            'select_ridge_valley': False
        }
        
        for attr, value in edge_selections.items():
            if hasattr(lineset, attr):
                try:
                    setattr(lineset, attr, value)
                except Exception as e:
                    print(f"  ├─ 设置 {attr} 失败: {e}")
        
        print(f"  ├─ Freestyle描边设置完成 (厚度: {thickness}, 颜色: {color})")
    
    def setup_dead_cells_materials(self, character_mesh, outline_method=None):
        """设置像素化死亡细胞材质系统
        
        Args:
            character_mesh: 角色网格对象
            outline_method: 描边方法 ("freestyle", "backface", "none")
        """
        # 根据运行模式选择默认描边方法
        if outline_method is None:
            outline_method = "backface" if self.is_background_mode() else "freestyle"
            
        print(f"  ├─ 选择描边方法: {outline_method} {'(后台模式自动选择)' if self.is_background_mode() else ''}")
            
        # 优先使用渲染网格，如果没有则使用传入的网格
        target_mesh = self.render_mesh if self.render_mesh else character_mesh
        if not target_mesh:
            return
        
        # 关键验证：确保目标网格的Armature修改器正确连接
        if self.armature or self.find_armature():
            armature = self.armature if self.armature else self.find_armature()
            print(f"  ├─ 验证网格 {target_mesh.name} 的骨骼绑定...")
            self.ensure_armature_modifier_points_to(target_mesh, armature)
        
        # 检查和设置EEVEE渲染引擎
        self.ensure_eevee_engine()
        
        # 创建像素化死亡细胞材质
        enable_backface_outline = (outline_method == "backface")
        material = self.create_dead_cells_toon_material(
            "DeadCells_PixelMaterial", 
            "skin", 
            enable_outline=enable_backface_outline
        )
        
        # 清理可能的重复材质槽
        self.clean_duplicate_material_slots(target_mesh)
        
        # 应用材质到目标网格
        if target_mesh.data.materials:
            target_mesh.data.materials[0] = material
        else:
            target_mesh.data.materials.append(material)
        
        # 设置描边
        if outline_method == "freestyle":
            self.setup_freestyle_outline(thickness=2.0, color=(0, 0, 0))
        elif outline_method == "backface":
            print("  ├─ 使用背面法线描边（已集成在材质中）")
        else:
            print("  ├─ 无描边")
        
        # 设置EEVEE像素化渲染优化
        self.setup_eevee_pixel_settings()
        
        # 设置纹理像素完美插值
        self.setup_pixel_perfect_textures()
        
        print(f"✓ 像素化材质系统配置完成: {target_mesh.name} (描边: {outline_method})")
    
    def setup_eevee_pixel_settings(self):
        """设置EEVEE渲染引擎的专业像素化渲染优化"""
        scene = bpy.context.scene
        render = scene.render
        
        # 获取EEVEE设置对象（兼容新旧版本）
        eevee = self.get_eevee_settings()
        
        if eevee is None:
            print("⚠ EEVEE设置不可用，跳过像素化设置")
            return
            
        print("🎨 配置像素风渲染设置...")
        
        # === 反走样设置 ===
        print("  ├─ 禁用反走样系统")
        if hasattr(eevee, 'taa_render_samples'):
            eevee.taa_render_samples = 1     # 完全禁用TAA时域抗锯齿
        if hasattr(eevee, 'taa_samples'):
            eevee.taa_samples = 1            # 视口也禁用TAA
        render.filter_size = 0.01        # 最小像素滤波（接近最近邻）
        if hasattr(render, 'use_antialiasing'):
            render.use_antialiasing = False  # 禁用传统抗锯齿
        
        # === 采样和精度 ===
        print("  ├─ 最小采样配置")
        if hasattr(eevee, 'gi_diffuse_bounces'):
            eevee.gi_diffuse_bounces = 1     # 最少光照反弹
        if hasattr(eevee, 'gi_cubemap_resolution'):
            self.safe_set_enum_property(eevee, 'gi_cubemap_resolution', ['64', '128', '256'], '128')   # 低分辨率环境贴图
        
        # === 硬边阴影系统 ===
        print("  ├─ 硬边阴影配置")
        if hasattr(eevee, 'use_soft_shadows'):
            eevee.use_soft_shadows = False  # 旧版EEVEE支持
        else:
            print("  ├─ EEVEE Next: use_soft_shadows已移除，使用默认硬阴影")
        self.safe_set_enum_property(eevee, 'shadow_cascade_size', ['4096', '2048', '1024', '512', '256', '128'], '2048')   # 高精度避免锯齿，优先高分辨率
        self.safe_set_enum_property(eevee, 'shadow_cube_size', ['2048', '1024', '512', '256', '128'], '1024')  # 点光源阴影，优先高分辨率
        if hasattr(eevee, 'light_threshold'):
            eevee.light_threshold = 0.01         # 精确光照阈值
        
        # === 禁用现代渲染特效 ===
        print("  ├─ 禁用现代渲染效果")
        if hasattr(eevee, 'use_bloom'):
            eevee.use_bloom = False              # 禁用辉光
        if hasattr(eevee, 'use_ssr'):
            eevee.use_ssr = False                # 禁用屏幕空间反射
        if hasattr(eevee, 'use_ssr_refraction'):
            eevee.use_ssr_refraction = False     # 禁用折射
        if hasattr(eevee, 'use_volumetric_lights'):
            eevee.use_volumetric_lights = False  # 禁用体积光
        if hasattr(eevee, 'use_volumetric_shadows'):
            eevee.use_volumetric_shadows = False # 禁用体积阴影
        if hasattr(eevee, 'use_motion_blur'):
            eevee.use_motion_blur = False        # 禁用运动模糊
        if hasattr(eevee, 'use_gtao'):
            eevee.use_gtao = False               # 禁用环境光遮蔽（保持硬边）
        
        # === 色彩管理（像素艺术标准）===
        print("  ├─ 像素艺术色彩管理")
        try:
            scene.view_settings.view_transform = 'Standard'  # 标准色彩变换
            scene.view_settings.look = 'None'                # 无色彩查找表
            scene.view_settings.exposure = 0.0               # 零曝光偏移
            scene.view_settings.gamma = 1.0                  # 线性伽马
            
            # 色彩空间设置
            if hasattr(scene.sequencer_colorspace_settings, 'name'):
                scene.sequencer_colorspace_settings.name = 'sRGB'
                
            print("    ├─ 色彩管理：Standard/sRGB配置完成")
        except Exception as e:
            print(f"    ⚠ 色彩管理配置警告: {e}")
            print("    ├─ 使用默认色彩设置继续")
        
        # === 渲染输出设置 ===
        print("  ├─ 像素完美输出配置")
        render.image_settings.file_format = 'PNG'
        render.image_settings.color_mode = 'RGBA'        # 保持透明通道
        render.image_settings.color_depth = '8'          # 8位深度（像素艺术标准）
        render.image_settings.compression = 0            # 无压缩保证质量
        
        # PNG特定设置
        if hasattr(render.image_settings, 'use_zbuffer'):
            render.image_settings.use_zbuffer = False    # 不需要深度缓冲
        
        # === 视口显示设置（仅在UI模式下）===
        def configure_viewports():
            viewport_configured = 0
            if bpy.context.screen and bpy.context.screen.areas:
                for area in bpy.context.screen.areas:
                    if area.type == 'VIEW_3D':
                        for space in area.spaces:
                            if space.type == 'VIEW_3D':
                                # 配置3D视口的像素化相关设置
                                if hasattr(space.shading, 'use_scene_lights_render'):
                                    space.shading.use_scene_lights_render = True
                                if hasattr(space.shading, 'use_scene_world_render'):
                                    space.shading.use_scene_world_render = True
                                viewport_configured += 1
            print(f"    配置了 {viewport_configured} 个3D视口")
        
        self.safe_ui_operation(
            "视口像素化显示配置", 
            configure_viewports,
            "视口配置仅在交互模式有效"
        )
        
        # === 纹理采样（最近邻插值）===
        print("  ├─ 设置纹理最近邻采样")
        # 这个设置主要影响纹理，在材质节点中处理
        # 这里设置全局默认行为
        for image in bpy.data.images:
            if image.name.startswith("DeadCells") or "pixel" in image.name.lower():
                # 对像素艺术纹理使用最近邻插值
                image.interpolation = 'Closest'
                image.use_alpha = True
        
        print("✓ 像素风渲染配置完成:")
        print("  • TAA抗锯齿: 禁用")
        print("  • 滤波模式: 最近邻")
        print("  • 阴影: 硬边高精度")
        print("  • 色彩: 标准sRGB")
        print("  • 输出: PNG 8位 无压缩")
    
    def setup_pixel_perfect_textures(self):
        """设置所有纹理为像素完美（最近邻插值，禁用MipMap）"""
        print("🔧 配置纹理像素化设置...")
        
        modified_count = 0
        mipmap_disabled_count = 0
        alpha_fixed_count = 0
        
        # 处理所有已加载的图像
        for image in bpy.data.images:
            if image.name not in ['Render Result', 'Viewer Node']:
                # 设置为最近邻插值（无平滑）
                if hasattr(image, 'interpolation'):
                    image.interpolation = 'Closest'
                    modified_count += 1
                
                # 禁用MipMap（在Image对象上，不是纹理节点上）
                if hasattr(image, 'use_mipmap'):
                    try:
                        image.use_mipmap = False
                        mipmap_disabled_count += 1
                    except Exception as e:
                        print(f"  ├─ 禁用 {image.name} 的MipMap失败: {e}")
                
                # 安全设置Alpha模式（兼容不同版本）
                if hasattr(image, 'alpha_mode'):
                    try:
                        # 尝试现代版本的alpha_mode值
                        if hasattr(bpy.types.Image, 'bl_rna'):
                            alpha_prop = image.bl_rna.properties.get('alpha_mode')
                            if alpha_prop and 'CHANNEL_PACKED' in [item.identifier for item in alpha_prop.enum_items]:
                                image.alpha_mode = 'CHANNEL_PACKED'
                                alpha_fixed_count += 1
                            elif 'STRAIGHT' in [item.identifier for item in alpha_prop.enum_items]:
                                image.alpha_mode = 'STRAIGHT'
                                alpha_fixed_count += 1
                    except Exception as e:
                        print(f"  ├─ 设置 {image.name} 的alpha_mode失败: {e}")
                        # 尝试备选方案
                        try:
                            image.alpha_mode = 'STRAIGHT'  # 备选方案
                            alpha_fixed_count += 1
                        except:
                            pass  # 如果都失败就跳过
        
        # 处理材质中的纹理节点
        node_count = 0
        for material in bpy.data.materials:
            if material.use_nodes:
                for node in material.node_tree.nodes:
                    if node.type == 'TEX_IMAGE':
                        # 设置纹理节点为最近邻
                        if hasattr(node, 'interpolation'):
                            node.interpolation = 'Closest'
                            node_count += 1
                        
                        # 注意：纹理节点通常没有use_mipmap属性
                        # MipMap应该在Image对象上控制
        
        print(f"  ├─ 修改了 {modified_count} 个图像插值模式")
        print(f"  ├─ 禁用了 {mipmap_disabled_count} 个图像的MipMap")
        print(f"  ├─ 修正了 {alpha_fixed_count} 个图像的Alpha模式")
        print(f"  ├─ 设置了 {node_count} 个纹理节点为最近邻")
        print("✓ 纹理像素化设置完成")
    
    def apply_pixel_settings_to_image(self, image):
        """为单个图像应用像素化设置（可在导入时调用）"""
        if not image or image.name in ['Render Result', 'Viewer Node']:
            return False
        
        success = False
        
        # 设置插值模式
        if hasattr(image, 'interpolation'):
            image.interpolation = 'Closest'
            success = True
        
        # 禁用MipMap
        if hasattr(image, 'use_mipmap'):
            try:
                image.use_mipmap = False
                success = True
            except:
                pass
        
        # 设置Alpha模式
        if hasattr(image, 'alpha_mode'):
            try:
                # 检测可用的alpha_mode选项
                alpha_prop = image.bl_rna.properties.get('alpha_mode')
                if alpha_prop:
                    available_modes = [item.identifier for item in alpha_prop.enum_items]
                    if 'CHANNEL_PACKED' in available_modes:
                        image.alpha_mode = 'CHANNEL_PACKED'
                    elif 'STRAIGHT' in available_modes:
                        image.alpha_mode = 'STRAIGHT'
                success = True
            except:
                pass
        
        return success
    
    def safe_set_node_input(self, node, input_name, value):
        """安全设置节点输入，兼容不同版本的输入名称"""
        if not node or not hasattr(node, 'inputs'):
            return False
        
        # 常见的输入名称变换映射表（旧版本 -> 新版本）
        input_mappings = {
            'Roughness': ['Roughness', 'Surface Roughness', 'roughness'],
            'Color': ['Color', 'Base Color', 'Albedo', 'color'],
            'Strength': ['Strength', 'Emission Strength', 'strength'],
            'Metallic': ['Metallic', 'metallic'],
            'Normal': ['Normal', 'normal'],
            'Alpha': ['Alpha', 'alpha'],
            'IOR': ['IOR', 'ior'],
            'Transmission': ['Transmission', 'transmission']
        }
        
        # 获取可能的输入名称列表
        possible_names = input_mappings.get(input_name, [input_name])
        
        # 尝试每个可能的名称
        for name in possible_names:
            try:
                if name in node.inputs:
                    node.inputs[name].default_value = value
                    return True
            except (KeyError, TypeError, AttributeError) as e:
                continue
        
        # 如果都失败，尝试通过索引访问（最后手段）
        try:
            input_indices = {
                'Color': 0,
                'Roughness': 1,
                'Strength': 1,
                'Normal': 2,
                'Alpha': 3
            }
            
            if input_name in input_indices:
                index = input_indices[input_name]
                if len(node.inputs) > index:
                    node.inputs[index].default_value = value
                    return True
        except:
            pass
        
        print(f"  ├─ ⚠ 无法设置节点输入 {input_name}，可能的版本兼容性问题")
        return False
    
    def safe_set_enum_property(self, obj, prop_name, preferred_values, fallback_value):
        """安全设置枚举属性，选择第一个可用值"""
        if not obj or not hasattr(obj, prop_name):
            return False
        
        try:
            # 获取属性的可用枚举值
            prop = obj.bl_rna.properties[prop_name]
            if hasattr(prop, 'enum_items'):
                available_values = [item.identifier for item in prop.enum_items]
                
                # 按优先级尝试设置
                for value in preferred_values:
                    if value in available_values:
                        setattr(obj, prop_name, value)
                        return True
                
                # 如果所有首选值都不可用，尝试使用fallback
                if fallback_value in available_values:
                    setattr(obj, prop_name, fallback_value)
                    return True
                
                # 最后尝试使用第一个可用值
                if available_values:
                    setattr(obj, prop_name, available_values[0])
                    print(f"  ├─ ⚠ {prop_name} 使用可用的最小值: {available_values[0]}")
                    return True
                    
            print(f"  ├─ ⚠ {prop_name} 无可用枚举值")
            return False
            
        except Exception as e:
            print(f"  ├─ 设置 {prop_name} 失败: {e}")
            return False
    
    def safe_clear_collection(self, collection, collection_name="集合"):
        """安全清空集合（兼容不同版本）"""
        if not collection:
            return False
        
        try:
            # 方法1: 使用clear()方法（现代版本）
            if hasattr(collection, 'clear'):
                collection.clear()
                return True
        except (AttributeError, RuntimeError):
            pass
        
        try:
            # 方法2: 逐个移除（兼容方案）
            while len(collection) > 0:
                if hasattr(collection, 'remove'):
                    collection.remove(collection[0])
                else:
                    break
            return True
        except (AttributeError, RuntimeError, IndexError):
            pass
        
        print(f"  ├─ ⚠ 无法清空{collection_name}，可能的版本兼容性问题")
        return False
    
    def safe_setup_colorramp(self, colorramp, positions, colors, interpolation='CONSTANT'):
        """安全设置ColorRamp（强制首尾+中间插入方案）"""
        if not colorramp or not hasattr(colorramp, 'elements'):
            return False
        
        if len(positions) != len(colors):
            print(f"  ├─ ⚠ ColorRamp位置和颜色数量不匹配: {len(positions)} vs {len(colors)}")
            return False
        
        # 设置插值模式
        if hasattr(colorramp, 'interpolation'):
            colorramp.interpolation = interpolation
        
        try:
            elements = colorramp.elements
            
            # 策略：保持ColorRamp最少2个元素的要求，使用首尾+中间插入法
            
            # 1. 确保至少有2个默认元素（大多数ColorRamp的最小要求）
            while len(elements) < 2:
                try:
                    elements.new(0.5)  # 在中间位置创建临时元素
                except:
                    print("  ├─ ⚠ 无法创建最少的ColorRamp元素")
                    return False
            
            # 2. 删除多余元素，但保留前2个
            while len(elements) > 2:
                try:
                    elements.remove(elements[-1])  # 从末尾删除
                except:
                    break
            
            # 3. 重新配置现有的2个元素作为首尾
            if len(positions) >= 2:
                # 配置第一个元素（起始）
                elements[0].position = positions[0]
                elements[0].color = colors[0] if len(colors[0]) == 4 else (*colors[0], 1.0)
                
                # 配置第二个元素（结尾）
                elements[1].position = positions[-1]
                elements[1].color = colors[-1] if len(colors[-1]) == 4 else (*colors[-1], 1.0)
                
                # 4. 在中间插入其他元素（如果有的话）
                for i in range(1, len(positions) - 1):
                    try:
                        element = elements.new(positions[i])
                        element.position = positions[i]
                        element.color = colors[i] if len(colors[i]) == 4 else (*colors[i], 1.0)
                    except Exception as e:
                        print(f"  ├─ 插入中间元素失败 {i}: {e}")
                        
            elif len(positions) == 1:
                # 只有一个颜色，设置首尾相同
                color = colors[0] if len(colors[0]) == 4 else (*colors[0], 1.0)
                elements[0].position = 0.0
                elements[0].color = color
                elements[1].position = 1.0
                elements[1].color = color
            
            # 5. 验证设置结果
            final_count = len(elements)
            expected_count = len(positions)
            if final_count != expected_count:
                print(f"  ├─ ⚠ ColorRamp元素数量: 期望{expected_count}, 实际{final_count}")
            
            # 6. 验证位置排序（ColorRamp要求位置递增）
            for i in range(len(elements) - 1):
                if elements[i].position > elements[i + 1].position:
                    print(f"  ├─ ⚠ ColorRamp位置顺序错误: {elements[i].position} > {elements[i + 1].position}")
            
            return True
            
        except Exception as e:
            print(f"  ├─ ColorRamp安全设置失败: {e}")
            return False
    
    def is_background_mode(self):
        """检测是否在后台模式下运行"""
        # 多重检测确保准确性
        return (
            bpy.app.background or  # Blender后台模式标志
            bpy.context.screen is None or  # 无UI屏幕
            not hasattr(bpy.context, 'window') or  # 无窗口上下文
            bpy.context.window is None
        )
    
    def safe_ui_operation(self, operation_name, operation_func, fallback_msg="跳过UI操作"):
        """安全执行UI相关操作，在后台模式下跳过"""
        if self.is_background_mode():
            print(f"  ├─ {fallback_msg} (后台模式)")
            return False
        
        try:
            operation_func()
            print(f"  ├─ {operation_name} 完成")
            return True
        except Exception as e:
            print(f"  ⚠ {operation_name} 失败: {e}")
            return False
    
    def create_material_variants(self, target_mesh):
        """创建多种材质变体（皮肤、布料、金属等）"""
        materials = {}
        
        # 创建不同类型的材质
        material_types = ['skin', 'cloth', 'metal']
        
        for mat_type in material_types:
            if mat_type in self.dead_cells_palette:
                material = self.create_dead_cells_toon_material(
                    f"DeadCells_{mat_type.capitalize()}", 
                    mat_type
                )
                materials[mat_type] = material
        
        # 将皮肤材质设为默认
        if 'skin' in materials:
            if target_mesh.data.materials:
                target_mesh.data.materials[0] = materials['skin']
            else:
                target_mesh.data.materials.append(materials['skin'])
        
        print(f"创建了 {len(materials)} 种材质变体: {list(materials.keys())}")
        return materials
    
    def should_auto_render(self):
        """判断是否应该自动渲染 - 多种控制方式的综合方案"""
        
        print("\n=== 渲染控制检查 ===")
        
        # 1. 检查环境变量（最高优先级）
        env_render = os.getenv('DEADCELLS_AUTO_RENDER', '').lower()
        if env_render in ['true', '1', 'yes']:
            print("✓ 环境变量 DEADCELLS_AUTO_RENDER=true，自动开始渲染")
            return True
        elif env_render in ['false', '0', 'no']:
            print("✗ 环境变量 DEADCELLS_AUTO_RENDER=false，跳过渲染")
            return False
        
        # 2. 检查命令行参数
        if '--auto-render' in sys.argv:
            print("✓ 检测到命令行参数 --auto-render，自动开始渲染")
            return True
        elif '--no-render' in sys.argv:
            print("✗ 检测到命令行参数 --no-render，跳过渲染")
            return False
        
        # 3. 检查配置文件 - 修正skip_confirmation语义
        automation_config = self.config.get('automation', {})
        config_auto_render = automation_config.get('auto_render', False)
        skip_confirmation = automation_config.get('skip_confirmation', False)
        
        # 组合逻辑判断
        if config_auto_render and skip_confirmation:
            # auto_render=True & skip_confirmation=True: 不问直接渲
            print("✓ 配置：auto_render=true & skip_confirmation=true，直接开始渲染")
            return True
        elif config_auto_render and not skip_confirmation:
            # auto_render=True & skip_confirmation=False: 问一下再渲染
            print("✓ 配置：auto_render=true，但需要确认")
            # 继续到交互确认环节
        elif not config_auto_render and skip_confirmation:
            # auto_render=False & skip_confirmation=True: 不问也不渲
            print("✓ 配置：auto_render=false & skip_confirmation=true，跳过渲染")
            return False
        else:
            # auto_render=False & skip_confirmation=False: 默认行为，继续到交互确认
            print("✓ 配置：使用默认交互模式")
            # 继续到交互确认环节
        
        # 4. 检查是否后台模式
        if bpy.app.background:
            print("✓ 检测到Blender后台模式，默认开始渲染")
            print("  提示: 使用 --no-render 参数可跳过渲染")
            return True
        
        # 5. 检查是否非交互环境
        if not self.is_interactive_environment():
            print("✓ 检测到非交互环境，默认开始渲染")
            print("  提示: 设置环境变量 DEADCELLS_AUTO_RENDER=false 可跳过")
            return True
        
        # 6. 交互模式询问（最后备选）
        print("🔄 交互模式：等待用户确认")
        
        # 检查是否在GUI环境
        if not self.is_background_mode() and bpy.context.screen is not None:
            print("⚠ 检测到Blender GUI环境，input()会阻塞")
            print("  建议: 修改config.json中automation.skip_confirmation=true")
            print("  或设置环境变量 DEADCELLS_AUTO_RENDER=true")
            print("  当前默认跳过渲染以避免阻塞")
            return False
        
        try:
            choice = input("是否开始渲染动画? (y/n): ")
            result = choice.lower() in ['y', 'yes', '是', '确定']
            if result:
                print("✓ 用户确认开始渲染")
            else:
                print("✗ 用户选择跳过渲染")
            return result
        except (EOFError, KeyboardInterrupt, OSError):
            print("⚠ 检测到输入异常，默认跳过渲染")
            print("  提示: 在非交互环境中请使用环境变量或命令行参数控制")
            return False
    
    def is_interactive_environment(self):
        """检查是否为真正的交互终端环境（非GUI）"""
        try:
            # 检查是否在Blender GUI中运行
            if not self.is_background_mode() and bpy.context.screen is not None:
                # GUI环境不支持input()，返回False
                return False
            
            # 检查stdin是否可用且连接到终端
            return sys.stdin.isatty()
        except (AttributeError, OSError):
            return False
    
    def get_render_limit(self):
        """获取渲染动画数量限制，默认-1（无限制）"""
        
        # 1. 检查环境变量
        env_limit = os.getenv('DEADCELLS_RENDER_LIMIT')
        if env_limit:
            try:
                limit = int(env_limit)
                if limit == -1:
                    print("使用环境变量：无渲染限制")
                else:
                    print(f"使用环境变量渲染限制: {limit}")
                return limit if limit > 0 else -1  # -1表示无限制
            except ValueError:
                print(f"警告: 环境变量 DEADCELLS_RENDER_LIMIT='{env_limit}' 不是有效数字")
        
        # 2. 检查命令行参数
        for i, arg in enumerate(sys.argv):
            if arg == '--render-limit' and i + 1 < len(sys.argv):
                try:
                    limit = int(sys.argv[i + 1])
                    if limit == -1:
                        print("使用命令行参数：无渲染限制")
                    else:
                        print(f"使用命令行参数渲染限制: {limit}")
                    return limit if limit > 0 else -1
                except ValueError:
                    print(f"警告: --render-limit 参数值无效: {sys.argv[i + 1]}")
        
        # 3. 检查配置文件（默认-1，即无限制）
        automation_config = self.config.get('automation', {})
        config_limit = automation_config.get('render_limit', -1)
        if config_limit == -1:
            print("配置文件设置：无渲染限制")
        else:
            print(f"使用配置文件渲染限制: {config_limit}")
        return config_limit
    
    def print_usage_help(self):
        """显示使用帮助信息"""
        print("\n=== 使用方法 ===")
        print("交互模式:")
        print("  blender -P character_DeadCellTest.py")
        print()
        print("后台模式:")
        print("  blender -b -P character_DeadCellTest.py")
        print()
        print("环境变量控制:")
        print("  DEADCELLS_AUTO_RENDER=true blender -b -P character_DeadCellTest.py")
        print("  DEADCELLS_RENDER_LIMIT=10 blender -b -P character_DeadCellTest.py")
        print()
        print("命令行参数:")
        print("  blender -b -P character_DeadCellTest.py -- --auto-render")
        print("  blender -b -P character_DeadCellTest.py -- --no-render")
        print("  blender -b -P character_DeadCellTest.py -- --render-limit 10")
        print("  blender -b -P character_DeadCellTest.py -- --render-limit -1  # 渲染全部")
        print()
        print("配置文件:")
        print('  在config.json中设置 "automation": {"auto_render": true}')
        print('  设置 "render_limit": -1 表示渲染全部动画')
        print('  设置 "render_limit": 5 表示只渲染前5个动画')
        print("")
        print("渲染行为配置组合:")
        print('  auto_render=false & skip_confirmation=false: 交互确认 (默认)')
        print('  auto_render=false & skip_confirmation=true:  不问也不渲')
        print('  auto_render=true  & skip_confirmation=false: 询问后渲染')  
        print('  auto_render=true  & skip_confirmation=true:  不问直接渲')
        print("=================\n")
    
    def switch_to_original_mesh(self):
        """切换到原始高精度网格（用于编辑）"""
        if self.original_mesh and self.render_mesh:
            self.original_mesh.hide_viewport = False
            self.original_mesh.hide_render = False
            self.render_mesh.hide_viewport = True
            self.render_mesh.hide_render = True
            print("已切换到原始高精度网格")
    
    def switch_to_render_mesh(self):
        """切换到渲染优化网格（用于渲染）"""
        if self.original_mesh and self.render_mesh:
            self.original_mesh.hide_viewport = True
            self.original_mesh.hide_render = True
            self.render_mesh.hide_viewport = False
            self.render_mesh.hide_render = False
            print("已切换到渲染优化网格")
    
    def get_render_stats(self):
        """获取渲染统计信息"""
        if self.original_mesh and self.render_mesh:
            original_faces = len(self.original_mesh.data.polygons)
            render_faces = len(self.render_mesh.data.polygons)
            reduction = (1 - render_faces / original_faces) * 100
            
            print(f"网格优化统计:")
            print(f"  原始网格面数: {original_faces}")
            print(f"  渲染网格面数: {render_faces}")
            print(f"  面数减少: {reduction:.1f}%")
    
    def setup_orthographic_camera(self, for_animation=None):
        """设置智能自适应正交相机"""
        target_mesh = self.render_mesh if self.render_mesh else self.original_mesh
        return self.setup_adaptive_camera(target_mesh, for_animation)
    
    def setup_adaptive_camera(self, character_mesh, for_animation=None):
        """设置自适应正交相机，根据角色大小和动画自动调整"""
        if not character_mesh:
            print("⚠ 无法设置相机：未找到角色网格")
            return None
        
        print(f"📷 设置自适应相机 (动画: {for_animation or 'T-pose'})")
        
        # 删除现有相机
        for obj in bpy.context.scene.objects:
            if obj.type == 'CAMERA':
                bpy.data.objects.remove(obj, do_unlink=True)
        
        # 创建新相机
        bpy.ops.object.camera_add(location=(0, 0, 0))
        camera = bpy.context.active_object
        camera.name = "DeadCells_AdaptiveCamera"
        
        # 设置为正交投影
        camera.data.type = 'ORTHO'
        
        # 计算边界框
        bounds = self.calculate_mesh_bounds(character_mesh, for_animation)
        
        # 配置相机参数
        self.configure_camera_position(camera, bounds)
        
        # 应用到场景
        bpy.context.scene.camera = camera
        self.smart_camera = camera
        
        print(f"✓ 自适应相机设置完成: {camera.name}")
        return camera
    
    def calculate_mesh_bounds(self, mesh, for_animation=None):
        """计算网格边界框（注意：for_animation参数已弃用，仅保留兼容性）"""
        # 确保在对象模式
        if bpy.context.mode != 'OBJECT':
            bpy.ops.object.mode_set(mode='OBJECT')
        
        # 注意：动画边界计算已迁移到 calculate_animation_bounds_with_action()
        # 此方法现在总是计算静态边界，for_animation参数被忽略
        if for_animation:
            print(f"  ├─ 注意：for_animation='{for_animation}' 参数已弃用，计算静态边界")
        
        # 计算静态T-pose边界
        return self.calculate_character_bounds(mesh)
    
    def calculate_character_bounds(self, mesh, use_evaluated=False):
        """计算角色网格的边界框
        
        Args:
            mesh: 网格对象
            use_evaluated: 是否使用已评估网格（适用于有修改器的情况）
        """
        if use_evaluated:
            try:
                # 尝试使用已评估网格获得更准确的边界
                current_frame = bpy.context.scene.frame_current
                return self.calculate_evaluated_mesh_bounds(mesh, current_frame)
            except Exception as e:
                print(f"  ⚠ 已评估网格计算失败: {e}")
                # 降级到静态计算
        
        # 获取世界坐标下的边界框（基于对象bound_box）
        bbox_corners = [mesh.matrix_world @ Vector(corner) for corner in mesh.bound_box]
        
        xs = [corner.x for corner in bbox_corners]
        ys = [corner.y for corner in bbox_corners]  
        zs = [corner.z for corner in bbox_corners]
        
        bounds = {
            'min_x': min(xs), 'max_x': max(xs),
            'min_y': min(ys), 'max_y': max(ys),
            'min_z': min(zs), 'max_z': max(zs),
            'center_x': (min(xs) + max(xs)) / 2,
            'center_y': (min(ys) + max(ys)) / 2,
            'center_z': (min(zs) + max(zs)) / 2,
            'width': max(xs) - min(xs),
            'height': max(zs) - min(zs),
            'depth': max(ys) - min(ys)
        }
        
        bound_type = "静态" if not use_evaluated else "已评估"
        print(f"  ├─ {bound_type}边界: {bounds['width']:.2f}×{bounds['height']:.2f}×{bounds['depth']:.2f}")
        return bounds
    
    def configure_camera_position(self, camera, bounds):
        """根据边界框配置相机位置和参数"""
        config = self.config.get('camera_settings', {})
        margin_ratio = config.get('margin_ratio', 0.15)
        min_ortho_scale = config.get('min_ortho_scale', 0.5)
        max_ortho_scale = config.get('max_ortho_scale', 15.0)
        
        # 计算相机位置（侧视图）
        # 对于正交相机，距离不影响画面，但影响裁剪面
        camera_distance = bounds['depth'] + bounds['width'] * 2  # 确保在裁剪范围内
        camera_x = bounds['center_x'] + camera_distance
        camera_y = bounds['center_y']  
        camera_z = bounds['center_z']
        
        camera.location = (camera_x, camera_y, camera_z)
        
        # 设置相机旋转（90度侧视角）
        import math
        camera.rotation_euler = (math.radians(90), 0, math.radians(90))
        
        # === 关键：设置裁剪面避免超跳帧被裁剪 ===
        clip_start = config.get('clip_start', 0.01)
        clip_end_multiplier = config.get('clip_end_multiplier', 10.0)
        min_clip_end = config.get('min_clip_end', 100.0)
        
        camera.data.clip_start = clip_start  # 近裁剪面，避免过近裁剪
        
        # 动态远裁剪面：基于场景深度 + 配置参数
        safe_clip_end = max(
            min_clip_end,  # 配置的最小远裁剪距离
            bounds['depth'] * clip_end_multiplier + 10,  # 深度相关动态距离
            camera_distance * 1.5  # 相机距离的1.5倍安全系数
        )
        camera.data.clip_end = safe_clip_end
        
        # 计算正交缩放
        # 需要包含角色的宽度和高度，取较大值
        required_scale = max(bounds['width'], bounds['height']) * (1 + margin_ratio)
        
        # 应用限制
        ortho_scale = max(min_ortho_scale, min(max_ortho_scale, required_scale))
        camera.data.ortho_scale = ortho_scale
        
        print(f"  ├─ 相机位置: ({camera_x:.2f}, {camera_y:.2f}, {camera_z:.2f})")
        print(f"  ├─ 正交缩放: {ortho_scale:.2f} (原始需求: {required_scale:.2f})")
        print(f"  ├─ 裁剪面: {camera.data.clip_start:.2f} ~ {camera.data.clip_end:.1f}")
        print(f"  ├─ 边距比例: {margin_ratio*100:.1f}%")
    
    def update_camera_for_action(self, action):
        """为指定动作更新相机设置（安全版本：直接使用action对象）"""
        if not self.smart_camera:
            print(f"⚠ 无法更新相机：smart_camera未设置")
            return False
        
        config = self.config.get('camera_settings', {})
        if not config.get('per_animation_adjustment', True):
            print(f"  ├─ 跳过动画相机调整 (配置禁用)")
            return True
        
        print(f"📷 更新相机适配动画: {action.name}")
        
        # 获取目标网格
        target_mesh = self.render_mesh if self.render_mesh else self.original_mesh
        if not target_mesh:
            print("  ⚠ 无法找到目标网格")
            return False
        
        # 重新计算动画边界（直接传递action对象）
        bounds = self.calculate_animation_bounds_with_action(target_mesh, action)
        
        # 更新相机配置
        self.configure_camera_position(self.smart_camera, bounds)
        
        print(f"  ✓ 相机已更新适配动画: {action.name}")
        return True
    
    def calculate_animation_bounds_with_action(self, mesh, action):
        """计算动画过程中的最大边界框（安全版本：直接使用action对象）"""
        if not self.armature or not action:
            return self.calculate_character_bounds(mesh)
        
        print(f"  ├─ 分析动画 '{action.name}' 的边界范围...")
        
        # 直接使用传入的action对象，无需查找
        self.armature.animation_data.action = action
        
        # 采样关键帧
        frame_range = action.frame_range
        sample_frames = []
        
        # 采样策略：开始、结束、中间点，加上1/4和3/4点
        sample_count = 8
        for i in range(sample_count):
            frame = frame_range[0] + (frame_range[1] - frame_range[0]) * i / (sample_count - 1)
            sample_frames.append(int(frame))
        
        # 收集所有帧的边界
        all_bounds = []
        current_frame = bpy.context.scene.frame_current
        
        for frame in sample_frames:
            bpy.context.scene.frame_set(frame)
            # 强制更新场景以应用动画
            bpy.context.view_layer.update()
            
            # 使用depsgraph获取已评估的网格数据
            bounds = self.calculate_evaluated_mesh_bounds(mesh, frame)
            all_bounds.append(bounds)
        
        # 恢复原始帧
        bpy.context.scene.frame_set(current_frame)
        
        # 计算联合边界框
        combined_bounds = {
            'min_x': min(b['min_x'] for b in all_bounds),
            'max_x': max(b['max_x'] for b in all_bounds),
            'min_y': min(b['min_y'] for b in all_bounds),
            'max_y': max(b['max_y'] for b in all_bounds),
            'min_z': min(b['min_z'] for b in all_bounds),
            'max_z': max(b['max_z'] for b in all_bounds),
        }
        
        # 计算中心点
        combined_bounds['center_x'] = (combined_bounds['min_x'] + combined_bounds['max_x']) / 2
        combined_bounds['center_y'] = (combined_bounds['min_y'] + combined_bounds['max_y']) / 2
        combined_bounds['center_z'] = (combined_bounds['min_z'] + combined_bounds['max_z']) / 2
        
        # 计算尺寸（与其他边界计算方法保持一致：height=Z轴，depth=Y轴）
        combined_bounds['width'] = combined_bounds['max_x'] - combined_bounds['min_x']
        combined_bounds['height'] = combined_bounds['max_z'] - combined_bounds['min_z']  # Z轴=高度
        combined_bounds['depth'] = combined_bounds['max_y'] - combined_bounds['min_y']   # Y轴=深度
        
        print(f"  ├─ 动画边界: {combined_bounds['width']:.2f} x {combined_bounds['height']:.2f} x {combined_bounds['depth']:.2f}")
        
        return combined_bounds
    
    def clean_duplicate_material_slots(self, mesh_obj):
        """清理重复的材质槽"""
        if not mesh_obj or not mesh_obj.data.materials:
            return 0
        
        original_count = len(mesh_obj.data.materials)
        materials = list(mesh_obj.data.materials)
        
        # 清空所有槽
        self.safe_clear_collection(mesh_obj.data.materials, "材质槽")
        
        # 重新添加，去除重复
        seen_materials = set()
        unique_materials = []
        
        for material in materials:
            material_id = id(material) if material else None
            if material_id not in seen_materials:
                seen_materials.add(material_id)
                unique_materials.append(material)
        
        # 添加去重后的材质
        for material in unique_materials:
            mesh_obj.data.materials.append(material)
        
        cleaned_count = len(mesh_obj.data.materials)
        if original_count != cleaned_count:
            print(f"  ├─ 清理材质槽重复: {original_count} → {cleaned_count}")
        
        return cleaned_count
    
    def check_timeout(self, current_task="处理"):
        """检查是否超时，如果超过10分钟则显示提示"""
        current_time = time.time()
        elapsed_time = current_time - self.start_time
        
        if self.should_abort:
            print(f"⚠ 用户选择中止任务")
            return True
        
        if elapsed_time > self.timeout_seconds:
            if not self.timeout_warned:
                self.timeout_warned = True
                elapsed_minutes = elapsed_time / 60
                print(f"\n🕒 ================ 超时提示 ================")
                print(f"⏰ 任务已运行 {elapsed_minutes:.1f} 分钟，超过10分钟预期时间")
                print(f"📝 当前任务: {current_task}")
                print(f"🤔 可能的原因:")
                print(f"   • 大量动画需要渲染")
                print(f"   • 高精度网格处理较慢")
                print(f"   • 复杂材质计算耗时")
                print(f"   • 硬件性能限制")
                print(f"")
                print(f"⚡ 建议操作:")
                print(f"   • 等待任务完成（推荐）")
                print(f"   • 设置 render_limit 限制动画数量")
                print(f"   • 降低渲染分辨率")
                print(f"   • 检查FBX文件是否过大")
                print(f"==========================================\n")
                
                # 在非后台模式下给用户选择机会
                if not self.is_background_mode():
                    # 检查是否在GUI环境
                    if bpy.context.screen is not None:
                        print("⚠ GUI环境下无法使用input()，默认继续等待")
                        print("  提示: 可按Ctrl+C强制中断，或关闭Blender")
                    else:
                        try:
                            response = input("是否继续等待? (y=继续, n=中止): ")
                            if response.lower() in ['n', 'no', '否', '中止']:
                                self.should_abort = True
                                return True
                        except (EOFError, KeyboardInterrupt):
                            print("检测到用户中断信号")
                            self.should_abort = True
                            return True
            
            # 每2分钟再次提示
            if elapsed_time > self.timeout_seconds + (2 * 60):
                extra_minutes = (elapsed_time - self.timeout_seconds) / 60
                print(f"⏰ 任务已超时额外 {extra_minutes:.1f} 分钟...")
                self.timeout_seconds += 2 * 60  # 更新下次提示时间
        
        return False
    
    def update_progress(self, current_task, progress_info=""):
        """更新进度并检查超时"""
        if self.check_timeout(current_task):
            raise RuntimeError("用户中止或超时")
        
        elapsed_time = (time.time() - self.start_time) / 60
        if progress_info:
            print(f"⏳ [{elapsed_time:.1f}min] {current_task}: {progress_info}")
        else:
            print(f"⏳ [{elapsed_time:.1f}min] {current_task}")
        
        # 强制更新UI（如果在交互模式）
        if not self.is_background_mode() and bpy.context.screen:
            try:
                bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP', iterations=1)
            except:
                pass
    
    def calculate_evaluated_mesh_bounds(self, mesh, frame):
        """使用depsgraph计算已评估网格的真实边界框（考虑骨骼变形）- 稳妥的to_mesh版本"""
        evaluated_mesh_data = None
        evaluated_obj = None  # 预先定义，避免finally块中的引用错误
        
        try:
            # 获取当前场景的depsgraph
            depsgraph = bpy.context.evaluated_depsgraph_get()
            
            # 获取已评估的网格对象
            evaluated_obj = mesh.evaluated_get(depsgraph)
            
            if not evaluated_obj:
                print(f"    ⚠ 帧 {frame}: 无法获取已评估对象，使用静态边界")
                return self.calculate_character_bounds(mesh)
            
            # 使用to_mesh()获取稳定的网格数据引用
            try:
                # Blender 2.80+的API
                evaluated_mesh_data = evaluated_obj.to_mesh()
            except AttributeError:
                try:
                    # 旧版本API兼容
                    evaluated_mesh_data = evaluated_obj.to_mesh(depsgraph, True)
                except:
                    print(f"    ⚠ 帧 {frame}: to_mesh()调用失败，尝试直接访问")
                    # 降级到直接访问
                    evaluated_mesh_data = evaluated_obj.data
            
            if not evaluated_mesh_data or not evaluated_mesh_data.vertices:
                print(f"    ⚠ 帧 {frame}: 无有效网格数据，使用静态边界")
                return self.calculate_character_bounds(mesh)
            
            # 转换到世界坐标系
            world_matrix = evaluated_obj.matrix_world
            
            # 初始化边界
            vertices = evaluated_mesh_data.vertices
            first_vert = world_matrix @ vertices[0].co
            min_x = max_x = first_vert.x
            min_y = max_y = first_vert.y  
            min_z = max_z = first_vert.z
            
            # 遍历所有顶点求真实边界
            for vertex in vertices:
                world_co = world_matrix @ vertex.co
                
                min_x = min(min_x, world_co.x)
                max_x = max(max_x, world_co.x)
                min_y = min(min_y, world_co.y)
                max_y = max(max_y, world_co.y)
                min_z = min(min_z, world_co.z)
                max_z = max(max_z, world_co.z)
            
            # 构建边界字典
            bounds = {
                'min_x': min_x, 'max_x': max_x,
                'min_y': min_y, 'max_y': max_y,
                'min_z': min_z, 'max_z': max_z,
                'center_x': (min_x + max_x) / 2,
                'center_y': (min_y + max_y) / 2,
                'center_z': (min_z + max_z) / 2,
                'width': max_x - min_x,
                'height': max_z - min_z,
                'depth': max_y - min_y
            }
            
            print(f"    ├─ 帧 {frame}: 已评估边界 {bounds['width']:.2f}×{bounds['height']:.2f}×{bounds['depth']:.2f}")
            return bounds
            
        except Exception as e:
            print(f"    ⚠ 帧 {frame}: Depsgraph计算失败 ({e})，使用静态边界")
            return self.calculate_character_bounds(mesh)
        
        finally:
            # 重要：清理临时网格数据，避免内存泄漏
            if evaluated_mesh_data and evaluated_obj and hasattr(evaluated_obj, 'to_mesh_clear'):
                try:
                    evaluated_obj.to_mesh_clear()
                except:
                    pass  # 某些版本可能不支持，静默忽略
    
    def setup_lighting(self):
        """设置像素风格光照（适配死亡细胞风格硬边阴影）"""
        # 删除默认光源
        for obj in bpy.context.scene.objects:
            if obj.type == 'LIGHT':
                bpy.data.objects.remove(obj, do_unlink=True)
        
        # 主光源 - 超硬边阴影，45度经典像素风角度
        bpy.ops.object.light_add(type='SUN')
        main_light = bpy.context.active_object
        main_light.name = "Pixel_KeyLight"
        main_light.data.energy = 8.0  # 强烈主光
        main_light.data.color = (1.0, 0.98, 0.95)  # 略带暖调的白光
        # 经典45度角 - 像素游戏标准光照方向
        main_light.rotation_euler = (math.radians(45), 0, math.radians(45))
        
        # 启用接触阴影以增强边缘定义
        if hasattr(main_light.data, 'use_contact_shadow'):
            main_light.data.use_contact_shadow = True
            main_light.data.contact_shadow_distance = 0.1
            main_light.data.contact_shadow_thickness = 0.01
        
        # 边缘描边光 - 从背后照射增强轮廓
        bpy.ops.object.light_add(type='SUN')
        rim_light = bpy.context.active_object
        rim_light.name = "Pixel_RimLight"
        rim_light.data.energy = 3.0  # 中等强度轮廓光
        rim_light.data.color = (0.9, 0.95, 1.0)  # 稍冷的轮廓光
        # 从后方135度照射，创造轮廓描边
        rim_light.rotation_euler = (math.radians(30), 0, math.radians(-135))
        
        # 最小填充光 - 避免纯黑阴影但保持硬边
        bpy.ops.object.light_add(type='SUN')  
        fill_light = bpy.context.active_object
        fill_light.name = "Pixel_MinFill"
        fill_light.data.energy = 0.8  # 极低强度，仅防止纯黑
        fill_light.data.color = (0.6, 0.7, 0.9)  # 冷色调阴影填充
        # 与主光相对方向，强度极低
        fill_light.rotation_euler = (math.radians(-30), 0, math.radians(-45))
        
        # 配置EEVEE阴影设置以获得更硬的边缘
        eevee = self.get_eevee_settings()
        if eevee:
            if hasattr(eevee, 'use_soft_shadows'):
                eevee.use_soft_shadows = False  # 旧版EEVEE禁用软阴影
            # EEVEE Next默认使用硬阴影，无需设置
            if hasattr(eevee, 'shadow_cascade_size'):
                self.safe_set_enum_property(eevee, 'shadow_cascade_size', ['4096', '2048', '1024', '512', '256', '128'], '2048')  # 高分辨率阴影贴图，优先最高精度
            if hasattr(eevee, 'shadow_cube_size'):
                self.safe_set_enum_property(eevee, 'shadow_cube_size', ['2048', '1024', '512', '256', '128'], '1024')  # 点光源阴影，优先高分辨率
        
        print("✓ 像素风格硬边光照设置完成（45度主光 + 轮廓光 + 最小填充）")
    
    def setup_render_settings(self):
        """设置渲染参数"""
        scene = bpy.context.scene
        
        # 渲染引擎 - 确保使用EEVEE
        self.ensure_eevee_engine()
        
        # 分辨率
        scene.render.resolution_x = self.render_resolution[0]
        scene.render.resolution_y = self.render_resolution[1]
        scene.render.resolution_percentage = 100
        
        # 输出格式
        scene.render.image_settings.file_format = 'PNG'
        scene.render.image_settings.color_mode = 'RGBA'
        scene.render.film_transparent = True
        
        # 像素艺术优化设置
        scene.render.image_settings.compression = 0  # 无压缩，保持像素精度
        scene.render.dither_intensity = 0.0          # 关闭抖动
        
        # 帧率
        scene.render.fps = self.frame_rate
        
        # EEVEE特定设置
        eevee = self.get_eevee_settings()
        if eevee:
            if hasattr(eevee, 'taa_render_samples'):
                eevee.taa_render_samples = 1     # 像素艺术不需要抗锯齿
            if hasattr(eevee, 'taa_samples'):
                eevee.taa_samples = 1            # 视口采样也设为1
            if hasattr(eevee, 'use_taa_reprojection'):
                eevee.use_taa_reprojection = False
        
        # 设置输出路径
        if not os.path.exists(self.output_path):
            os.makedirs(self.output_path)
        scene.render.filepath = os.path.join(self.output_path, "frame_")
        
        print("渲染设置完成（EEVEE + 像素艺术优化）")
    
    def get_animation_list(self):
        """获取可用的动画列表"""
        animations = []
        
        # 查找动画数据
        for action in bpy.data.actions:
            if action.name not in animations:
                animations.append(action.name)
        
        print(f"发现 {len(animations)} 个动画:")
        for i, anim in enumerate(animations):
            print(f"  {i+1}. {anim}")
        
        return animations
    
    def sanitize_filename(self, filename):
        """将文件名/目录名中的非法字符替换为安全字符"""
        import re
        
        # Windows文件系统非法字符：< > : " / \ | ? * 以及ASCII控制字符
        illegal_chars = r'[<>:"/\\|?*\x00-\x1f]'
        
        # 替换非法字符为下划线
        sanitized = re.sub(illegal_chars, '_', filename)
        
        # 移除文件名开头和结尾的点和空格（Windows不允许）
        sanitized = sanitized.strip('. ')
        
        # 确保文件名不为空
        if not sanitized:
            sanitized = "unnamed"
        
        # 限制长度（Windows路径长度限制）
        if len(sanitized) > 100:
            sanitized = sanitized[:100]
        
        return sanitized
    
    def render_animation_with_action(self, animation_name, action, start_frame=1, end_frame=30):
        """渲染指定动画（安全版本：直接传递action对象）"""
        scene = bpy.context.scene
        
        # 为当前动画更新相机设置（传递action对象而非名称）
        if self.smart_camera:
            self.update_camera_for_action(action)
        
        # 设置动画帧范围
        scene.frame_start = start_frame
        scene.frame_end = end_frame
        
        # 安全化动画名用作文件/目录名
        safe_animation_name = self.sanitize_filename(animation_name)
        if safe_animation_name != animation_name:
            print(f"⚠ 动画名包含非法字符，已替换: '{animation_name}' → '{safe_animation_name}'")
        
        # 设置输出文件名（使用清洗后的名称）
        animation_output_dir = os.path.join(self.output_path, safe_animation_name)
        if not os.path.exists(animation_output_dir):
            os.makedirs(animation_output_dir)
        
        # 记录渲染信息
        print(f"🎬 开始渲染动画: {animation_name}")
        print(f"  ├─ 帧范围: {start_frame} - {end_frame}")
        print(f"  ├─ 输出目录: {animation_output_dir}")
        
        # 渲染每一帧（手动逐帧确保动作正确评估）
        for frame in range(start_frame, end_frame + 1):
            # 设置当前帧
            scene.frame_set(frame)
            
            # 关键！强制更新视图层以确保动作和修改器正确评估
            bpy.context.view_layer.update()
            
            # 设置输出文件名
            frame_filename = f"{safe_animation_name}_{frame:04d}.png"
            scene.render.filepath = os.path.join(animation_output_dir, frame_filename)
            
            # 渲染当前帧
            bpy.ops.render.render(write_still=True)
            
            # 可选：显示进度（对于长动画有用）
            if frame % 10 == 0 or frame == end_frame:
                progress = ((frame - start_frame + 1) / (end_frame - start_frame + 1)) * 100
                print(f"  ├─ 渲染进度: {progress:.1f}% (帧 {frame}/{end_frame})")
        
        print(f"✓ 动画渲染完成: {animation_name} ({end_frame - start_frame + 1} 帧)")
    
    def apply_render_limit(self, animations):
        """应用渲染限制并给出明确提示"""
        render_limit = self.get_render_limit()
        
        if render_limit == -1 or render_limit >= len(animations):
            print(f"✓ 将渲染全部 {len(animations)} 个动画")
            return animations
        
        print(f"⚠️  渲染限制：{len(animations)} 个动画中仅渲染前 {render_limit} 个")
        print(f"   剩余 {len(animations) - render_limit} 个动画将被跳过")
        print(f"   如需渲染全部，请使用: --render-limit=-1")
        print(f"   或在配置文件中设置: \"render_limit\": -1")
        
        return animations[:render_limit]
    
    def render_all_animations(self):
        """渲染所有动画"""
        animations = self.get_animation_list()
        
        # 应用渲染限制（默认无限制）
        animations_to_render = self.apply_render_limit(animations)
        
        for i, animation in enumerate(animations_to_render):
            try:
                # 检查超时和更新进度
                progress_info = f"{animation} ({i+1}/{len(animations_to_render)})"
                self.update_progress("渲染动画", progress_info)
                
                # 获取动画对象（用原始名称查找）
                action = bpy.data.actions.get(animation)
                if action:
                    # 应用动画到骨骼对象（这会自动设置场景的帧范围）
                    if not self.apply_animation_to_armature(action):
                        print(f"跳过动画 {animation}: 无法应用到骨骼对象")
                        continue
                    
                    # 使用场景中已设置的帧范围（由apply_animation_to_armature设置）
                    scene = bpy.context.scene
                    start_frame = scene.frame_start
                    end_frame = scene.frame_end
                    
                    print(f"  ├─ 渲染帧范围: {start_frame} - {end_frame}")
                    # 传递原始名称用于文件名，action对象用于相机边界计算
                    self.render_animation_with_action(animation, action, start_frame, end_frame)
            except Exception as e:
                print(f"渲染动画 {animation} 时出错: {e}")
    
    def generate_sprite_sheets(self):
        """生成Unity精灵图集"""
        print("🎯 开始生成Unity精灵图集...")
        
        # 获取所有渲染输出目录（排除SpritesheetS输出目录）
        render_dirs = []
        if os.path.exists(self.output_path):
            for item in os.listdir(self.output_path):
                item_path = os.path.join(self.output_path, item)
                # 排除SpritesheetS输出目录，只包含实际的动画渲染目录
                if os.path.isdir(item_path) and item != "SpritesheetS":
                    render_dirs.append(item_path)
        
        if not render_dirs:
            print("⚠ 没有找到渲染输出目录")
            return False
        
        sprite_sheets_path = os.path.join(self.output_path, "SpritesheetS")
        os.makedirs(sprite_sheets_path, exist_ok=True)
        
        success_count = 0
        for render_dir in render_dirs:
            try:
                animation_name = os.path.basename(render_dir)
                if self.create_sprite_sheet(render_dir, sprite_sheets_path, animation_name):
                    success_count += 1
                    print(f"✓ 已生成 {animation_name} 精灵图集")
            except Exception as e:
                print(f"⚠ 生成 {animation_name} 精灵图集时出错: {e}")
        
        print(f"🎯 精灵图集生成完成: {success_count}/{len(render_dirs)}")
        return success_count > 0
    
    def generate_sprite_sheets_with_check(self):
        """检查渲染输出并生成精灵图集"""
        print("🎯 检查渲染输出...")
        
        # 检查是否存在渲染输出目录
        if not os.path.exists(self.output_path):
            print("⚠ 渲染输出目录不存在，无法生成精灵图集")
            print(f"   缺失目录: {self.output_path}")
            print("💡 请先完成渲染流程生成动画帧")
            return False
        
        # 检查是否有动画目录
        render_dirs = []
        for item in os.listdir(self.output_path):
            item_path = os.path.join(self.output_path, item)
            if os.path.isdir(item_path) and item != "SpritesheetS":  # 排除已存在的输出目录
                # 检查是否包含PNG文件
                png_files = [f for f in os.listdir(item_path) if f.lower().endswith('.png')]
                if png_files:
                    render_dirs.append((item, len(png_files)))  # 保存动画名和帧数
        
        if not render_dirs:
            print("⚠ 未找到有效的渲染输出（包含PNG序列帧的动画目录）")
            print(f"   搜索路径: {self.output_path}")
            print("💡 请先运行渲染流程：设置 auto_render=true 或使用环境变量")
            print("   环境变量示例: DEADCELLS_AUTO_RENDER=true")
            return False
        
        # 显示找到的动画
        print(f"✓ 找到 {len(render_dirs)} 个动画渲染目录:")
        total_frames = 0
        for anim_name, frame_count in render_dirs:
            print(f"   • {anim_name}: {frame_count} 帧")
            total_frames += frame_count
        print(f"   共计 {total_frames} 帧")
        
        # 直接使用筛选后的结果生成精灵图集（不重新遍历）
        return self.generate_sprite_sheets_from_list(render_dirs)
    
    def generate_sprite_sheets_from_list(self, validated_render_dirs):
        """根据已验证的渲染目录列表生成精灵图集"""
        print("🎯 开始生成Unity精灵图集（使用预验证目录）...")
        
        if not validated_render_dirs:
            print("⚠ 没有有效的渲染输出目录")
            return False
        
        # 创建输出目录
        sprite_sheets_path = os.path.join(self.output_path, "SpritesheetS")
        os.makedirs(sprite_sheets_path, exist_ok=True)
        
        success_count = 0
        for anim_name, frame_count in validated_render_dirs:
            try:
                # 构建完整的渲染目录路径
                render_dir = os.path.join(self.output_path, anim_name)
                
                print(f"  ├─ 处理动画: {anim_name} ({frame_count} 帧)")
                if self.create_sprite_sheet(render_dir, sprite_sheets_path, anim_name):
                    success_count += 1
                    print(f"  ├─ ✓ 已生成 {anim_name} 精灵图集")
                else:
                    print(f"  ├─ ⚠ {anim_name} 精灵图集生成失败")
            except Exception as e:
                print(f"  ├─ ⚠ 生成 {anim_name} 精灵图集时出错: {e}")
        
        print(f"🎯 精灵图集生成完成: {success_count}/{len(validated_render_dirs)}")
        return success_count > 0
    
    def install_pillow(self):
        """安装Pillow到Blender的site-packages并立即可用"""
        import subprocess
        import sys
        import importlib
        
        try:
            python_exe = sys.executable
            print(f"🔧 使用Python: {python_exe}")
            
            # 首先确保pip可用
            try:
                import ensurepip
                print("🔧 确保pip可用...")
                ensurepip.bootstrap()
            except Exception as e:
                print(f"⚠ ensurepip失败: {e}")
            
            # 获取Blender的site-packages路径
            import site
            blender_site_packages = None
            for path in site.getsitepackages():
                if 'blender' in path.lower():
                    blender_site_packages = path
                    break
            
            # 如果没找到特定的blender路径，使用第一个site-packages
            if not blender_site_packages and site.getsitepackages():
                blender_site_packages = site.getsitepackages()[0]
            
            print(f"🔧 目标安装路径: {blender_site_packages}")
            
            # 直接安装到Blender的site-packages
            print("🔧 正在安装Pillow到Blender环境...")
            install_cmd = [
                python_exe, "-m", "pip", "install", "Pillow", 
                "--target", blender_site_packages, "--upgrade"
            ]
            
            result = subprocess.run(install_cmd, capture_output=True, text=True, timeout=120)
            
            if result.returncode == 0:
                print("✓ Pillow安装到Blender环境成功")
                
                # 刷新sys.path和模块缓存
                print("🔧 刷新模块缓存...")
                if blender_site_packages not in sys.path:
                    sys.path.insert(0, blender_site_packages)
                
                # 清除可能的import缓存
                if 'PIL' in sys.modules:
                    del sys.modules['PIL']
                if 'PIL.Image' in sys.modules:
                    del sys.modules['PIL.Image']
                
                # 刷新importlib缓存
                importlib.invalidate_caches()
                
                return True
            else:
                print(f"❌ pip安装失败: {result.stderr}")
                
                # 尝试不使用--target参数（直接安装）
                print("🔧 尝试直接安装...")
                result2 = subprocess.run([
                    python_exe, "-m", "pip", "install", "Pillow", "--upgrade"
                ], capture_output=True, text=True, timeout=120)
                
                if result2.returncode == 0:
                    print("✓ Pillow直接安装成功")
                    # 同样刷新缓存
                    importlib.invalidate_caches()
                    return True
                else:
                    print(f"❌ 直接安装也失败: {result2.stderr}")
                    return False
                    
        except subprocess.TimeoutExpired:
            print("❌ 安装超时，请检查网络连接")
            return False
        except Exception as e:
            print(f"❌ 安装过程出错: {e}")
            return False
    
    def create_sprite_sheet(self, frames_dir, output_dir, animation_name):
        """创建单个动画的精灵图集"""
        # 安全化动画名用作文件名
        safe_animation_name = self.sanitize_filename(animation_name)
        if safe_animation_name != animation_name:
            print(f"⚠ 精灵图集名称包含非法字符，已替换: '{animation_name}' → '{safe_animation_name}'")
        try:
            from PIL import Image
        except ImportError:
            print("⚠ Pillow库未安装，正在自动安装...")
            if not self.install_pillow():
                print("❌ Pillow安装失败，请手动安装: pip install Pillow")
                return False
            # 多次尝试导入，处理缓存刷新延迟
            for attempt in range(3):
                try:
                    # 每次尝试前都刷新一下缓存
                    import importlib
                    importlib.invalidate_caches()
                    
                    from PIL import Image
                    print("✓ Pillow安装成功并立即可用")
                    break  # 导入成功，跳出重试循环
                except ImportError as e:
                    if attempt < 2:  # 还有重试机会
                        print(f"⚠ 导入尝试 {attempt + 1}/3 失败: {e}")
                        print("🔧 刷新模块缓存并重试...")
                        # 强制刷新sys.path
                        import sys
                        import site
                        site.main()  # 重新初始化site-packages
                        continue
                    else:  # 最后一次尝试失败
                        print("❌ Pillow安装后仍无法导入")
                        print("💡 这可能是Blender Python环境的限制")
                        print("💡 解决方案：")
                        print("   1. 重启Blender后重新运行脚本")
                        print("   2. 或使用预安装Pillow的Python环境")
                        return False
        
        # 获取所有PNG文件
        png_files = []
        for file in os.listdir(frames_dir):
            if file.lower().endswith('.png'):
                png_files.append(os.path.join(frames_dir, file))
        
        if not png_files:
            print(f"⚠ {frames_dir} 中没有找到PNG文件")
            return False
        
        png_files.sort()  # 确保帧顺序正确
        
        # 读取第一张图片获取尺寸
        first_img = Image.open(png_files[0])
        frame_width, frame_height = first_img.size
        first_img.close()
        
        # 计算精灵图集尺寸（尝试接近正方形）
        frame_count = len(png_files)
        cols = int(frame_count ** 0.5)
        while cols > 0 and frame_count % cols != 0:
            cols -= 1
        if cols == 0:
            cols = frame_count
        rows = frame_count // cols
        
        # 创建精灵图集
        sheet_width = cols * frame_width
        sheet_height = rows * frame_height
        sprite_sheet = Image.new('RGBA', (sheet_width, sheet_height), (0, 0, 0, 0))
        
        # 填充精灵图集
        for i, png_file in enumerate(png_files):
            frame_img = Image.open(png_file)
            col = i % cols
            row = i // cols
            x = col * frame_width
            y = row * frame_height
            sprite_sheet.paste(frame_img, (x, y))
            frame_img.close()
        
        # 保存精灵图集
        sheet_path = os.path.join(output_dir, f"{safe_animation_name}.png")
        sprite_sheet.save(sheet_path)
        sprite_sheet.close()
        
        # 生成Unity元数据文件
        self.generate_unity_meta(sheet_path, safe_animation_name, frame_count, cols, rows, frame_width, frame_height)
        
        return True
    
    def generate_unity_meta(self, sheet_path, animation_name, frame_count, cols, rows, frame_width, frame_height):
        """生成Unity .meta文件和动画配置"""
        import uuid
        
        # 确保动画名是安全的（传入的应该已经是安全的，但加个保险）
        safe_animation_name = self.sanitize_filename(animation_name)
        
        # 生成.meta文件
        meta_content = f"""fileFormatVersion: 2
guid: {str(uuid.uuid4()).replace('-', '')}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsPerUnit: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: {rows}
  flipbookColumns: {cols}
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:"""
        
        # 生成每个精灵的配置
        for i in range(frame_count):
            col = i % cols
            row = i // cols  # 修复：应该按列数整除来计算行号
            x = col * frame_width
            y = (rows - 1 - row) * frame_height  # Unity Y轴翻转
            
            sprite_guid = str(uuid.uuid4()).replace('-', '')[:16]
            internal_id = 21300000 + (i * 2)  # Unity标准稳定递增ID，间隔2避免冲突
            meta_content += f"""
    - serializedVersion: 2
      name: {safe_animation_name}_{i:04d}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {frame_width}
        height: {frame_height}
      alignment: 0
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      outline: []
      physicsShape: []
      tessellationDetail: 0
      bones: []
      spriteID: {sprite_guid}
      internalID: {internal_id}
      vertices: []
      indices: 
      edges: []
      weights: []"""
        
        meta_content += """
    outline: []
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        
        # 保存.meta文件
        meta_path = sheet_path + ".meta"
        with open(meta_path, 'w', encoding='utf-8') as f:
            f.write(meta_content)
    
    def generate_unity_editor_script(self):
        """生成Unity Editor脚本来自动创建AnimationClip和AnimatorController"""
        print("🎮 生成Unity Editor自动化脚本...")
        
        sprite_sheets_path = os.path.join(self.output_path, "SpritesheetS")
        # 确保SpritesheetS目录存在（即使没有精灵图集也要创建）
        os.makedirs(sprite_sheets_path, exist_ok=True)
        
        # 获取动画信息：优先从精灵图集，其次从原始渲染输出
        animations_info = []
        
        # 方式1：从精灵图集获取（如果存在）
        if os.path.exists(sprite_sheets_path):
            for file in os.listdir(sprite_sheets_path):
                if file.lower().endswith('.png') and not file.startswith('.'):
                    animation_name = os.path.splitext(file)[0]
                    
                    # 从对应的原始渲染目录获取帧数
                    original_dir = os.path.join(self.output_path, animation_name)
                    frame_count = 0
                    if os.path.exists(original_dir):
                        frame_count = len([f for f in os.listdir(original_dir) if f.lower().endswith('.png')])
                    
                    if frame_count > 0:
                        animations_info.append({
                            'name': animation_name,
                            'sprite_sheet': file,
                            'frame_count': frame_count
                        })
        
        # 方式2：如果没有精灵图集，从原始渲染目录获取（生成预备脚本）
        if not animations_info and os.path.exists(self.output_path):
            print("💡 未找到精灵图集，从渲染输出生成预备脚本")
            for item in os.listdir(self.output_path):
                item_path = os.path.join(self.output_path, item)
                if os.path.isdir(item_path) and item != "SpritesheetS":
                    png_files = [f for f in os.listdir(item_path) if f.lower().endswith('.png')]
                    if png_files:
                        animations_info.append({
                            'name': item,
                            'sprite_sheet': f"{item}.png",  # 预期的精灵图集名
                            'frame_count': len(png_files)
                        })
        
        if not animations_info:
            print("⚠ 没有找到动画数据（精灵图集或渲染输出）")
            return False
        
        # 生成Editor脚本
        script_content = self.generate_unity_automation_script(animations_info)
        script_path = os.path.join(sprite_sheets_path, "CharacterAnimationSetup.cs")
        
        with open(script_path, 'w', encoding='utf-8') as f:
            f.write(script_content)
        
        print(f"✓ 已生成Unity Editor脚本: {script_path}")
        print("💡 在Unity中运行: Window → Character Animation Setup")
        return True
    
    def generate_unity_automation_script(self, animations_info):
        """生成Unity Editor自动化脚本内容"""
        # 生成C#代码来创建动画数据列表
        animations_creation = ""
        for i, anim in enumerate(animations_info):
            safe_name = anim["name"].replace('"', '\\"')  # 转义引号
            safe_sprite = anim["sprite_sheet"].replace('"', '\\"')
            animations_creation += f'        animationsData.Add(new AnimationData("{safe_name}", "{safe_sprite}", {anim["frame_count"]}));\n'
        
        script_content = f"""using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class CharacterAnimationSetup : EditorWindow
{{
    [System.Serializable]
    public class AnimationData
    {{
        public string name;
        public string spriteSheet;
        public int frameCount;
        
        public AnimationData(string name, string spriteSheet, int frameCount)
        {{
            this.name = name;
            this.spriteSheet = spriteSheet;
            this.frameCount = frameCount;
        }}
    }}

    [MenuItem("Window/Character Animation Setup")]
    public static void ShowWindow()
    {{
        GetWindow<CharacterAnimationSetup>("Character Animation Setup");
    }}

    private void OnGUI()
    {{
        GUILayout.Label("Dead Cells Character Animation Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Animations and Controller", GUILayout.Height(30)))
        {{
            CreateAnimationsAndController();
        }}
        
        GUILayout.Space(10);
        GUILayout.Label("This will create:");
        GUILayout.Label("• AnimationClips for each sprite sheet");
        GUILayout.Label("• AnimatorController with all states");
        GUILayout.Label("• Updated PlayerCharacter prefab");
    }}

    private void CreateAnimationsAndController()
    {{
        // 创建动画数据列表
        var animationsData = new List<AnimationData>();
{animations_creation}

        string basePath = "Assets/PlayerCharacter";
        
        // 确保目录存在
        if (!AssetDatabase.IsValidFolder(basePath))
        {{
            EditorUtility.DisplayDialog("Error", "PlayerCharacter folder not found in Assets!", "OK");
            return;
        }}

        // 创建动画剪辑
        var animationClips = new AnimationClip[animationsData.Count];
        for (int i = 0; i < animationsData.Count; i++)
        {{
            animationClips[i] = CreateAnimationClip(basePath, animationsData[i].name, 
                                                   animationsData[i].spriteSheet, 
                                                   animationsData[i].frameCount);
        }}

        // 创建Animator Controller
        var controller = CreateAnimatorController(basePath, animationClips);

        // 更新Prefab
        UpdatePlayerPrefab(basePath, controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", 
            $"Created {{animationClips.Length}} animations and controller!" + System.Environment.NewLine +
            "PlayerCharacter prefab has been updated.", "OK");
    }}

    private AnimationClip CreateAnimationClip(string basePath, string animName, string spriteSheet, int frameCount)
    {{
        // 加载精灵图集
        string spriteSheetPath = Path.Combine(basePath, spriteSheet).Replace(@"\", "/");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
                                 .OfType<Sprite>()
                                 .OrderBy(s => s.name)
                                 .ToArray();

        if (sprites.Length == 0)
        {{
            Debug.LogError($"No sprites found in {{spriteSheetPath}}");
            return null;
        }}

        // 创建AnimationClip
        var clip = new AnimationClip();
        clip.name = animName;
        clip.frameRate = 12; // 12fps适合像素艺术

        // 创建精灵动画曲线
        var spriteBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var spriteKeyframes = new ObjectReferenceKeyframe[sprites.Length];

        for (int i = 0; i < sprites.Length; i++)
        {{
            spriteKeyframes[i] = new ObjectReferenceKeyframe
            {{
                time = i / 12f, // 按帧率设置时间
                value = sprites[i]
            }};
        }}

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, spriteKeyframes);

        // 设置循环
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 保存动画剪辑
        string clipPath = Path.Combine(basePath, $"{{animName}}.anim").Replace(@"\", "/");
        AssetDatabase.CreateAsset(clip, clipPath);

        return clip;
    }}

    private AnimatorController CreateAnimatorController(string basePath, AnimationClip[] clips)
    {{
        string controllerPath = Path.Combine(basePath, "PlayerAnimatorController.controller").Replace(@"\", "/");
        
        // 如果存在就删除重建
        if (File.Exists(controllerPath))
            AssetDatabase.DeleteAsset(controllerPath);

        // 创建Animator Controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        // 为每个动画创建状态
        AnimatorState defaultState = null;
        for (int i = 0; i < clips.Length; i++)
        {{
            if (clips[i] == null) continue;

            var state = rootStateMachine.AddState(clips[i].name);
            state.motion = clips[i];

            // 设置第一个为默认状态
            if (defaultState == null)
                defaultState = state;
        }}

        // 设置默认状态
        if (defaultState != null)
            rootStateMachine.defaultState = defaultState;

        return controller;
    }}

    private void UpdatePlayerPrefab(string basePath, AnimatorController controller)
    {{
        string prefabPath = Path.Combine(basePath, "PlayerCharacter.prefab").Replace(@"\", "/");
        
        // 删除旧的Prefab（如果存在）
        if (File.Exists(prefabPath))
        {{
            AssetDatabase.DeleteAsset(prefabPath);
        }}
        
        // 创建新的GameObject
        GameObject playerObject = new GameObject("PlayerCharacter");
        playerObject.tag = "Player";
        
        // 添加SpriteRenderer组件
        var spriteRenderer = playerObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 0;
        
        // 添加Animator组件并设置Controller
        var animator = playerObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        
        // 添加Rigidbody2D组件
        var rigidbody = playerObject.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 3f;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // 添加BoxCollider2D组件
        var collider = playerObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.8f, 1.6f);
        
        // 创建GroundCheck子对象
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerObject.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.8f, 0);
        
        // 添加PlayerController脚本（如果存在）
        var playerControllerScript = System.Type.GetType("DeadCellsTestFramework.Player.PlayerController");
        if (playerControllerScript != null)
        {{
            var playerController = playerObject.AddComponent(playerControllerScript) as MonoBehaviour;
            // 使用反射设置groundCheck字段
            var groundCheckField = playerControllerScript.GetField("groundCheck", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (groundCheckField != null)
            {{
                groundCheckField.SetValue(playerController, groundCheck.transform);
            }}
        }}
        
        // 创建Prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(playerObject, prefabPath);
        
        // 清理场景中的临时对象
        DestroyImmediate(playerObject);
        
        if (prefab != null)
        {{
            Debug.Log($"Created PlayerCharacter prefab: {{prefabPath}}");
        }}
        else
        {{
            Debug.LogError($"Failed to create prefab: {{prefabPath}}");
        }}
    }}
}}
"""
        return script_content
    
    
    def generate_unity_assets(self):
        """生成所有Unity资产"""
        print("🎯 开始生成Unity资产...")
        
        # 开始时success设为False，只有子任务成功才设为True
        success = False
        
        # 1. 生成精灵图集（先检查渲染输出是否存在）
        sprite_sheets_success = self.generate_sprite_sheets_with_check()
        if not sprite_sheets_success:
            print("⚠ 精灵图集生成失败")
        else:
            print("✓ 精灵图集生成成功")
            success = True  # 至少一个任务成功
        
        # 2. 生成Unity Editor脚本（即使精灵图集失败也尝试生成）
        editor_script_success = self.generate_unity_editor_script()
        if not editor_script_success:
            print("⚠ Unity Editor脚本生成失败")
        else:
            print("✓ Unity Editor脚本生成成功")
            success = True  # 至少一个任务成功
            if not sprite_sheets_success:
                # 精灵图集失败但Editor脚本成功时的特殊处理
                print("💡 已生成Unity Editor脚本，重启Blender后可完成精灵图集生成")
        
        # 3. 跳过Prefab生成（Unity Editor脚本会处理）
        print("💡 Prefab将由Unity Editor脚本自动生成")
        
        # 4. 自动导入到Unity项目（如果有任何资产生成成功）
        if success:  # 只有真正有资产生成成功才进行导入
            sprite_sheets_path_final = os.path.join(self.output_path, "SpritesheetS")
            print("📁 Unity资产生成状态：")
            print(f"   • 精灵图集: {'✅ 完成' if sprite_sheets_success else '❌ 失败（需重启Blender）'}")
            print(f"   • Editor脚本: {'✅ 完成' if editor_script_success else '❌ 失败'}")
            print(f"   • Player Prefab: 💡 由Unity Editor脚本生成")  # 明确说明，不使用成功/失败
            print(f"📁 资产位置: {sprite_sheets_path_final}")
            
            if self.auto_import_to_unity():
                print("✅ 已自动导入到Unity项目！")
            else:
                print("💡 将SpritesheetS文件夹复制到Unity项目的Assets目录即可使用")
            
            if not sprite_sheets_success and editor_script_success:
                print("\n💡 完成精灵图集生成的步骤：")
                print("   1. 重启Blender")
                print("   2. 重新运行脚本")
                print("   3. 或在Unity中手动运行Editor脚本")
        else:
            print("❌ 所有Unity资产生成都失败了")
        
        # 返回真实的成功状态：至少一个子任务成功
        return sprite_sheets_success or editor_script_success
    
    def check_and_generate_unity_assets(self):
        """检查并生成Unity资产（简化版，实际检查在generate_unity_assets内部）"""
        print("\n=== Unity资产生成检查 ===")
        
        # 直接调用Unity资产生成，内部已包含检查逻辑
        success = self.generate_unity_assets()
        
        if success:
            sprite_sheets_path = os.path.join(self.output_path, "SpritesheetS")
            print(f"\n✅ Unity资产生成完成！")
            print(f"📁 资产位置: {sprite_sheets_path}")
            print("💡 接下来在Unity中运行: Window → Character Animation Setup")
        else:
            print(f"\n❌ Unity资产生成失败")
            print("💡 请检查错误信息并重试")
        
        return success
    
    def find_unity_project_root(self):
        """向上搜索Unity项目根目录（包含Assets文件夹的目录）"""
        # 从当前输出路径开始向上搜索
        current_path = os.path.abspath(self.output_path)
        
        # 最多向上搜索10级目录，防止无限循环
        max_levels = 10
        for level in range(max_levels):
            # 检查当前目录是否包含Assets文件夹
            assets_path = os.path.join(current_path, "Assets")
            if os.path.exists(assets_path) and os.path.isdir(assets_path):
                # 进一步验证是否为Unity项目（检查是否有ProjectSettings）
                project_settings = os.path.join(current_path, "ProjectSettings")
                if os.path.exists(project_settings) and os.path.isdir(project_settings):
                    print(f"🔍 在第{level}级找到Unity项目根目录: {current_path}")
                    return current_path
                else:
                    # 只有Assets但没有ProjectSettings，继续向上搜索
                    print(f"🔍 第{level}级找到Assets但缺少ProjectSettings，继续搜索...")
            
            # 向上一级目录
            parent_path = os.path.dirname(current_path)
            if parent_path == current_path:  # 到达根目录
                break
            current_path = parent_path
        
        print(f"❌ 在{max_levels}级目录内未找到Unity项目根目录")
        return None
    
    def auto_import_to_unity(self):
        """自动导入资产到Unity项目"""
        import shutil
        
        # 搜索Unity项目根目录
        unity_project_root = self.find_unity_project_root()
        if unity_project_root is None:
            print("⚠ 未找到Unity项目根目录（包含Assets文件夹的目录）")
            print("💡 请确保脚本在Unity项目目录结构内运行")
            return False
        
        unity_assets_path = os.path.join(unity_project_root, "Assets")
        print(f"✓ 找到Unity项目: {unity_project_root}")
        
        # 源目录和目标目录
        source_dir = os.path.join(self.output_path, "SpritesheetS")
        target_dir = os.path.join(unity_assets_path, "PlayerCharacter")
        
        if not os.path.exists(source_dir):
            print(f"⚠ 源目录不存在: {source_dir}")
            return False
        
        try:
            # 创建目标目录
            os.makedirs(target_dir, exist_ok=True)
            
            # 复制所有文件
            copied_files = []
            for item in os.listdir(source_dir):
                source_item = os.path.join(source_dir, item)
                target_item = os.path.join(target_dir, item)
                
                if os.path.isfile(source_item):
                    shutil.copy2(source_item, target_item)
                    copied_files.append(item)
            
            print(f"✓ 已复制 {len(copied_files)} 个文件到 Assets/PlayerCharacter/")
            
            # 尝试刷新Unity资产数据库（如果Unity正在运行）
            self.refresh_unity_assets(unity_project_root)
            
            return True
            
        except Exception as e:
            print(f"⚠ 复制文件时出错: {e}")
            return False
    
    def refresh_unity_assets(self, unity_project_path):
        """尝试刷新Unity资产数据库"""
        try:
            # 检查Unity是否正在运行（通过检查Temp目录）
            unity_temp_path = os.path.join(unity_project_path, "Temp")
            if os.path.exists(unity_temp_path):
                print("✓ 检测到Unity正在运行，资产将自动刷新")
                
                # 创建一个标记文件来触发Unity刷新
                refresh_marker = os.path.join(unity_project_path, "Assets", ".refresh_marker")
                with open(refresh_marker, 'w') as f:
                    f.write("refresh")
                
                # 立即删除标记文件
                if os.path.exists(refresh_marker):
                    os.remove(refresh_marker)
            else:
                print("💡 请在Unity中手动刷新Assets（Ctrl+R）")
                
        except Exception as e:
            print(f"⚠ 刷新Unity资产时出错: {e}")
    
    def setup_world_settings(self):
        """设置世界环境，包含空值检查和默认创建"""
        # 确保场景有World对象
        world = bpy.context.scene.world
        if world is None:
            print("⚠ 场景缺少World对象，正在创建...")
            world = bpy.data.worlds.new("World")
            bpy.context.scene.world = world
        
        # 确保World启用节点
        if not world.use_nodes:
            print("🔧 启用World节点系统...")
            world.use_nodes = True
        
        # 确保有节点树
        if world.node_tree is None:
            print("⚠ World缺少节点树，正在创建...")
            world.use_nodes = True  # 重新启用以创建节点树
        
        # 查找或创建Background节点
        bg_node = None
        if world.node_tree and world.node_tree.nodes:
            # 尝试查找现有Background节点
            for node in world.node_tree.nodes:
                if node.type == 'BACKGROUND':
                    bg_node = node
                    break
        
        # 如果没找到Background节点，创建基础的World材质设置
        if bg_node is None:
            print("🔧 创建World背景节点...")
            # 清理现有节点
            self.safe_clear_collection(world.node_tree.nodes, "世界节点")
            
            # 创建Background节点
            bg_node = world.node_tree.nodes.new(type='ShaderNodeBackground')
            bg_node.location = (0, 0)
            
            # 创建World Output节点
            output_node = world.node_tree.nodes.new(type='ShaderNodeOutputWorld')
            output_node.location = (300, 0)
            
            # 连接节点
            world.node_tree.links.new(bg_node.outputs['Background'], output_node.inputs['Surface'])
        
        # 设置背景颜色和强度
        try:
            self.safe_set_node_input(bg_node, 'Color', (0.05, 0.05, 0.05, 1.0))  # 深灰色背景
            self.safe_set_node_input(bg_node, 'Strength', 0.1)
            print("✓ World环境设置完成")
        except Exception as e:
            print(f"⚠ 设置背景参数时出现问题: {e}")
            # 尝试设置默认纯色背景
            world.color = (0.05, 0.05, 0.05)
            print("✓ 已设置为纯色背景模式")


def run_dead_cells_pipeline():
    """运行死亡细胞渲染流水线"""
    print("=== 死亡细胞角色渲染流水线 ===")
    
    pipeline = DeadCellsRenderPipeline()
    
    try:
        # 0. 验证路径
        pipeline.update_progress("验证配置和路径")
        if not pipeline.validate_paths():
            print("错误: 路径验证失败，请检查配置文件")
            return
        
        # 1. 清理场景
        pipeline.update_progress("清理场景")
        pipeline.clear_scene()
        
        # 2. 导入FBX角色
        pipeline.update_progress("导入FBX角色")
        character = pipeline.import_fbx_character()
        if not character:
            print("错误: 无法导入角色模型")
            return
        
        # 3. 创建渲染优化网格
        pipeline.update_progress("创建渲染优化网格")
        render_mesh = pipeline.optimize_character_mesh(character)
        if render_mesh:
            pipeline.get_render_stats()  # 显示优化统计
        
        # 4. 设置材质
        pipeline.update_progress("设置死亡细胞风格材质")
        pipeline.setup_dead_cells_materials(character)
        
        # 5. 设置相机
        pipeline.update_progress("设置正交相机")
        pipeline.setup_orthographic_camera()
        
        # 6. 设置光照
        pipeline.update_progress("设置光照")
        pipeline.setup_lighting()
        
        # 7. 设置世界环境
        pipeline.update_progress("设置世界环境")
        pipeline.setup_world_settings()
        
        # 8. 设置渲染参数
        pipeline.update_progress("设置渲染参数")
        pipeline.setup_render_settings()
        
        # 9. 获取动画列表
        pipeline.update_progress("分析动画数据")
        animations = pipeline.get_animation_list()
        
        # 10. 渲染决策
        pipeline.update_progress("渲染决策")
        if pipeline.should_auto_render():
            pipeline.update_progress("开始批量渲染", f"共{len(animations)}个动画")
            pipeline.render_all_animations()
        else:
            print("跳过渲染阶段")
        
        # 11. 生成Unity资产（独立于渲染流程）
        pipeline.update_progress("检查并生成Unity资产")
        pipeline.check_and_generate_unity_assets()
        
        print("\n=== 流水线设置完成 ===")
        print(f"角色模型: {character.name}")
        print(f"输出路径: {pipeline.output_path}")
        
        # 显示使用帮助
        if not bpy.app.background:
            pipeline.print_usage_help()
        
        elapsed_total = (time.time() - pipeline.start_time) / 60
        print(f"\n🎉 流水线已准备就绪！(总耗时: {elapsed_total:.1f}分钟)")
        
    except RuntimeError as e:
        if "用户中止或超时" in str(e):
            elapsed_time = (time.time() - pipeline.start_time) / 60
            print(f"\n⏹ 任务已中止 (运行时间: {elapsed_time:.1f}分钟)")
            print("💡 下次运行建议:")
            print("   • 设置 render_limit 限制动画数量")
            print("   • 降低渲染分辨率")
            print("   • 检查FBX文件复杂度")
        else:
            print(f"运行时错误: {e}")
    except Exception as e:
        print(f"流水线执行错误: {e}")
        import traceback
        traceback.print_exc()


# 主函数
if __name__ == "__main__":
    run_dead_cells_pipeline()