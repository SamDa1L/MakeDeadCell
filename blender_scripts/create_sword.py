"""
Dead Cells - 剑类武器生成器
这个脚本会创建一个适合2D游戏的剑模型
作者：AI Assistant
用途：为Dead Cells游戏项目生成武器资产
"""

import bpy
import bmesh
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
    # 创建基础立方体
    bpy.ops.mesh.primitive_cube_add(location=(0, 0, length/2))
    blade = bpy.context.active_object
    blade.name = "Sword_Blade"
    
    # 缩放成剑刃形状
    blade.scale = (width, thickness, length/2)
    
    # 应用变换
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    
    # 进入编辑模式创建剑尖
    bpy.context.view_layer.objects.active = blade
    bpy.ops.object.mode_set(mode='EDIT')
    
    try:
        # 取消选择所有
        bpy.ops.mesh.select_all(action='DESELECT')
        
        # 切换到面选择模式
        bpy.context.tool_settings.mesh_select_mode = (False, False, True)
        
        # 选择顶部面（Z轴正方向）
        bpy.ops.mesh.select_all(action='DESELECT')
        
        # 进入bmesh模式来精确选择顶部面
        import bmesh
        bm = bmesh.from_edit_mesh(blade.data)
        
        # 找到Z坐标最高的面
        max_z = max(face.calc_center_median().z for face in bm.faces)
        top_faces = [face for face in bm.faces if abs(face.calc_center_median().z - max_z) < 0.001]
        
        # 取消所有选择
        for face in bm.faces:
            face.select = False
            
        # 选择顶部面
        for face in top_faces:
            face.select = True
            
        bmesh.update_edit_mesh(blade.data)
        
        # 内插面来创建更小的面
        bpy.ops.mesh.inset_faces(thickness=0.1, depth=0.1)
        
        # 向上挤压创建剑尖
        bpy.ops.mesh.extrude_region_move(
            TRANSFORM_OT_translate={"value": (0, 0, 0.3)}
        )
        
        # 缩放到一个点创建尖端
        bpy.ops.transform.resize(value=(0.1, 0.1, 1.0))
        
        # 再次挤压创建真正的尖端
        bpy.ops.mesh.extrude_region_move(
            TRANSFORM_OT_translate={"value": (0, 0, 0.2)}
        )
        
        # 缩放到一个点
        bpy.ops.transform.resize(value=(0.01, 0.01, 1.0))
        
        print("✓ 剑尖创建完成")
        
    except Exception as e:
        print(f"⚠ 剑尖创建遇到问题: {str(e)}")
        print("➤ 使用基础剑刃形状")
    
    # 退出编辑模式
    bpy.ops.object.mode_set(mode='OBJECT')
    print("✓ 剑刃创建完成")
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

def add_materials():
    """为剑添加材质"""
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
        
        # 分配材质
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
    # 选择所有剑的部件
    sword_parts = ["Sword_Blade", "Sword_Guard", "Sword_Handle", "Sword_Pommel"]
    
    bpy.ops.object.select_all(action='DESELECT')
    
    for part_name in sword_parts:
        if part_name in bpy.data.objects:
            bpy.data.objects[part_name].select_set(True)
    
    # 设置剑刃为活动对象
    if "Sword_Blade" in bpy.data.objects:
        bpy.context.view_layer.objects.active = bpy.data.objects["Sword_Blade"]
        
        # 合并所有选中的对象
        bpy.ops.object.join()
        
        # 重命名最终对象
        bpy.context.active_object.name = "BasicSword_Complete"
        
        print("✓ 剑部件合并完成")

def setup_camera_and_lighting():
    """设置相机和光照以便预览"""
    # 添加相机
    bpy.ops.object.camera_add(location=(5, -5, 3))
    camera = bpy.context.active_object
    
    # 让相机朝向剑
    constraint = camera.constraints.new(type='TRACK_TO')
    if "BasicSword_Complete" in bpy.data.objects:
        constraint.target = bpy.data.objects["BasicSword_Complete"]
    
    # 添加光源
    bpy.ops.object.light_add(type='SUN', location=(2, 2, 5))
    sun = bpy.context.active_object
    sun.data.energy = 3
    
    print("✓ 相机和光照设置完成")

def main():
    """主函数 - 执行完整的剑生成流程"""
    print("=== Dead Cells 剑模型生成器开始运行 ===")
    
    # 步骤1：清空场景
    clear_scene()
    
    # 步骤2：创建剑的各个部分
    create_sword_blade(length=3.0, width=0.3, thickness=0.1)
    create_sword_guard(width=0.8, thickness=0.05, depth=0.15)
    create_sword_handle(length=1.0, width=0.15)
    create_sword_pommel(size=0.2)
    
    # 步骤3：添加材质
    add_materials()
    
    # 步骤4：合并所有部件
    join_sword_parts()
    
    # 步骤5：设置场景
    setup_camera_and_lighting()
    
    print("=== 剑模型生成完成！===")
    print("提示：")
    print("- 按小键盘0切换到相机视角查看模型")
    print("- 按Z选择视图模式（线框/实体/材质预览/渲染）")
    print("- 可以手动调整相机位置获得更好的视角")
    print("- 使用 File > Export > FBX 导出模型到Unity")

# 如果直接运行这个脚本，执行main函数
if __name__ == "__main__":
    main()