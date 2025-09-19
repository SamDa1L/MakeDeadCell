"""
Dead Cells - 简化版剑模型生成器
这是一个更稳定、更简单的版本，避免复杂的bmesh操作
作者：AI Assistant
用途：为Dead Cells游戏项目生成武器资产
"""

import bpy
import mathutils
from mathutils import Vector

def clear_scene():
    """清空场景中的所有对象"""
    # 确保处于对象模式
    if bpy.context.active_object and bpy.context.active_object.mode != 'OBJECT':
        bpy.ops.object.mode_set(mode='OBJECT')
    
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    print("✓ 场景已清空")

def create_sword_blade(length=3.0, width=0.3, thickness=0.1):
    """创建剑刃部分"""
    # 创建基础立方体作为剑身
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, length/2 - 0.2))
    blade_body = bpy.context.active_object
    blade_body.name = "Sword_Body"
    
    # 缩放成剑身形状（稍短一些为剑尖留空间）
    blade_body.scale = (width, thickness, (length-0.4)/2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    # 创建剑尖（圆锥体）
    bpy.ops.mesh.primitive_cone_add(
        location=(0, 0, length - 0.2),
        radius1=width/2, 
        radius2=0.01,  # 很小的顶端半径
        depth=0.4,
        vertices=6
    )
    blade_tip = bpy.context.active_object
    blade_tip.name = "Sword_Tip"
    
    # 缩放圆锥体使其更符合剑尖形状
    blade_tip.scale = (1.0, thickness/width, 1.0)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    # 选择两个对象进行合并
    bpy.ops.object.select_all(action='DESELECT')
    blade_body.select_set(True)
    blade_tip.select_set(True)
    bpy.context.view_layer.objects.active = blade_body
    
    # 合并剑身和剑尖
    bpy.ops.object.join()
    
    # 重命名最终对象
    blade = bpy.context.active_object
    blade.name = "Sword_Blade"
    
    print("✓ 剑刃（带尖端）创建完成")
    return blade

def create_sword_guard(width=0.8, thickness=0.05, depth=0.15):
    """创建剑格（护手）"""
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, 0))
    guard = bpy.context.active_object
    guard.name = "Sword_Guard"
    
    # 缩放成护手形状
    guard.scale = (width, depth, thickness)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    print("✓ 剑格创建完成")
    return guard

def create_sword_handle(length=1.0, width=0.15):
    """创建剑柄"""
    bpy.ops.mesh.primitive_cylinder_add(location=(0, 0, -length/2), vertices=8)
    handle = bpy.context.active_object
    handle.name = "Sword_Handle"
    
    # 缩放成剑柄形状
    handle.scale = (width, width, length/2)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    print("✓ 剑柄创建完成")
    return handle

def create_sword_pommel(size=0.2):
    """创建剑首（剑柄末端的装饰）"""
    bpy.ops.mesh.primitive_uv_sphere_add(location=(0, 0, -1.2), radius=size)
    pommel = bpy.context.active_object
    pommel.name = "Sword_Pommel"
    
    print("✓ 剑首创建完成")
    return pommel

def add_simple_materials():
    """为剑添加简单材质"""
    try:
        # 创建剑刃材质（金属）
        blade_mat = bpy.data.materials.new(name="Blade_Material")
        blade_mat.use_nodes = True
        
        # 安全地获取Principled BSDF节点
        nodes = blade_mat.node_tree.nodes
        bsdf = None
        
        # 尝试不同的节点名称（支持不同语言版本）
        possible_names = ["Principled BSDF", "原理化BSDF", "Shader BSDF"]
        for name in possible_names:
            if name in nodes:
                bsdf = nodes[name]
                break
        
        # 如果没有找到，查找第一个着色器节点
        if bsdf is None:
            for node in nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    bsdf = node
                    break
        
        if bsdf:
            # 设置银色金属材质
            # 使用索引访问输入，更稳定
            if len(bsdf.inputs) > 0:
                bsdf.inputs[0].default_value = (0.8, 0.8, 0.9, 1.0)  # Base Color
            if len(bsdf.inputs) > 4:
                bsdf.inputs[4].default_value = 0.9  # Metallic
            if len(bsdf.inputs) > 7:
                bsdf.inputs[7].default_value = 0.1  # Roughness
        else:
            # 如果没有找到节点，使用简单的漫反射着色器
            blade_mat.diffuse_color = (0.8, 0.8, 0.9, 1.0)
        
        print("✓ 剑刃材质创建完成")
        
        # 创建剑柄材质（皮革/木头）
        handle_mat = bpy.data.materials.new(name="Handle_Material")
        handle_mat.use_nodes = True
        
        handle_nodes = handle_mat.node_tree.nodes
        handle_bsdf = None
        
        # 同样的方法查找节点
        for name in possible_names:
            if name in handle_nodes:
                handle_bsdf = handle_nodes[name]
                break
        
        if handle_bsdf is None:
            for node in handle_nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    handle_bsdf = node
                    break
        
        if handle_bsdf:
            # 设置棕色皮革材质
            if len(handle_bsdf.inputs) > 0:
                handle_bsdf.inputs[0].default_value = (0.4, 0.2, 0.1, 1.0)  # Base Color
            if len(handle_bsdf.inputs) > 4:
                handle_bsdf.inputs[4].default_value = 0.0  # Metallic
            if len(handle_bsdf.inputs) > 7:
                handle_bsdf.inputs[7].default_value = 0.8  # Roughness
        else:
            # 如果没有找到节点，使用简单的漫反射着色器
            handle_mat.diffuse_color = (0.4, 0.2, 0.1, 1.0)
        
        print("✓ 剑柄材质创建完成")
        
        # 分配材质到对象
        assign_materials(blade_mat, handle_mat)
        print("✓ 材质添加完成")
        
    except Exception as e:
        print(f"⚠ 材质创建遇到问题: {str(e)}")
        print("➤ 将创建简单的颜色材质")
        create_simple_color_materials()

def create_simple_color_materials():
    """创建简单的颜色材质作为备用方案"""
    # 创建简单的银色材质
    blade_mat = bpy.data.materials.new(name="Simple_Blade_Material")
    blade_mat.diffuse_color = (0.8, 0.8, 0.9, 1.0)  # 银色
    
    # 创建简单的棕色材质
    handle_mat = bpy.data.materials.new(name="Simple_Handle_Material")
    handle_mat.diffuse_color = (0.4, 0.2, 0.1, 1.0)  # 棕色
    
    # 分配材质
    assign_materials(blade_mat, handle_mat)
    print("✓ 简单颜色材质创建完成")

def assign_materials(blade_mat, handle_mat):
    """分配材质到相应的对象"""
    # 金属材质给剑刃和剑格
    metal_parts = ["Sword_Blade", "Sword_Guard"]
    for part_name in metal_parts:
        if part_name in bpy.data.objects:
            obj = bpy.data.objects[part_name]
            if obj.data.materials:
                obj.data.materials[0] = blade_mat
            else:
                obj.data.materials.append(blade_mat)
    
    # 皮革材质给剑柄和剑首
    leather_parts = ["Sword_Handle", "Sword_Pommel"]
    for part_name in leather_parts:
        if part_name in bpy.data.objects:
            obj = bpy.data.objects[part_name]
            if obj.data.materials:
                obj.data.materials[0] = handle_mat
            else:
                obj.data.materials.append(handle_mat)

def join_sword_parts():
    """将所有部件合并成一个对象"""
    # 确保所有对象都处于对象模式
    bpy.ops.object.mode_set(mode='OBJECT')
    
    # 选择所有剑的部件
    sword_parts = ["Sword_Blade", "Sword_Guard", "Sword_Handle", "Sword_Pommel"]
    
    bpy.ops.object.select_all(action='DESELECT')
    
    objects_to_join = []
    for part_name in sword_parts:
        if part_name in bpy.data.objects:
            obj = bpy.data.objects[part_name]
            obj.select_set(True)
            objects_to_join.append(obj)
    
    if objects_to_join:
        # 设置剑刃为活动对象
        bpy.context.view_layer.objects.active = bpy.data.objects["Sword_Blade"]
        
        # 合并所有选中的对象
        bpy.ops.object.join()
        
        # 重命名最终对象
        bpy.context.active_object.name = "BasicSword_Complete"
        
        print("✓ 剑部件合并完成")
        return bpy.context.active_object
    else:
        print("⚠ 没有找到剑部件进行合并")
        return None

def setup_camera_and_lighting():
    """设置相机和光照以便预览"""
    # 添加相机
    bpy.ops.object.camera_add(location=(5, -5, 3))
    camera = bpy.context.active_object
    camera.name = "Sword_Camera"
    
    # 让相机朝向原点
    camera.rotation_euler = (1.1, 0, 0.785)  # 大致朝向中心的角度
    
    # 添加光源
    bpy.ops.object.light_add(type='SUN', location=(2, 2, 5))
    sun = bpy.context.active_object
    sun.name = "Sword_Sun"
    sun.data.energy = 3
    sun.data.angle = 0.1  # 较小的角度获得更锐利的阴影
    
    # 设置相机为场景相机
    bpy.context.scene.camera = camera
    
    print("✓ 相机和光照设置完成")

def set_render_settings():
    """设置渲染参数以获得更好的预览"""
    scene = bpy.context.scene
    scene.render.engine = 'EEVEE'  # 使用EEVEE渲染器，速度更快
    scene.eevee.use_bloom = True   # 开启光晕效果
    scene.eevee.use_ssr = True     # 开启屏幕空间反射
    
    print("✓ 渲染设置完成")

def main():
    """主函数 - 执行完整的剑生成流程"""
    print("=== Dead Cells 简化版剑模型生成器开始运行 ===")
    
    try:
        # 步骤1：清空场景
        clear_scene()
        
        # 步骤2：创建剑的各个部分
        create_sword_blade(length=3.0, width=0.3, thickness=0.1)
        create_sword_guard(width=0.8, thickness=0.05, depth=0.15)
        create_sword_handle(length=1.0, width=0.15)
        create_sword_pommel(size=0.2)
        
        # 步骤3：添加材质
        add_simple_materials()
        
        # 步骤4：合并所有部件
        final_sword = join_sword_parts()
        
        # 步骤5：设置场景
        setup_camera_and_lighting()
        set_render_settings()
        
        print("=== ✅ 剑模型生成完成！===")
        print("")
        print("📋 使用提示：")
        print("- 按小键盘 0 切换到相机视角查看模型")
        print("- 按 Z 选择视图模式（线框/实体/材质预览/渲染）")
        print("- 在材质预览或渲染模式下可以看到材质效果")
        print("- 选中剑模型后可以用G键移动、R键旋转、S键缩放")
        print("")
        print("💾 导出到Unity：")
        print("- 选中 'BasicSword_Complete' 对象")
        print("- 文件 → 导出 → FBX (.fbx)")
        print("- 导出到你的Unity项目的Assets文件夹")
        print("")
        print("🎨 自定义提示：")
        print("- 可以修改脚本中main()函数里的参数来调整剑的尺寸")
        print("- 可以在材质属性面板中调整颜色和材质属性")
        
        if final_sword:
            # 选中最终的剑对象，方便用户后续操作
            bpy.ops.object.select_all(action='DESELECT')
            final_sword.select_set(True)
            bpy.context.view_layer.objects.active = final_sword
        
    except Exception as e:
        print(f"❌ 脚本执行出错: {str(e)}")
        print("请检查Blender版本和脚本内容")

# 如果直接运行这个脚本，执行main函数
if __name__ == "__main__":
    main()