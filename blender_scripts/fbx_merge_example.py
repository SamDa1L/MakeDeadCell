#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
FBX合并工具使用示例
这个脚本展示了如何使用fbx_merge_blender.py合并模型和动画
"""

import sys
import os

# 确保能够导入fbx_merge_blender模块
script_dir = os.path.dirname(__file__)
sys.path.append(script_dir)

try:
    from fbx_merge_blender import FBXMerger
except ImportError as e:
    print(f"无法导入FBXMerger: {e}")
    print("请确保fbx_merge_blender.py在同一目录中")
    sys.exit(1)

def example_basic_usage():
    """基本使用示例"""
    print("=" * 60)
    print("📖 FBX合并工具 - 基本使用示例")
    print("=" * 60)
    
    # 方法1: 使用默认配置和配置文件
    print("\n🔧 方法1: 使用配置文件")
    merger = FBXMerger()  # 会自动尝试加载fbx_merge_config.json
    merger.run()
    
def example_custom_paths():
    """自定义路径示例"""
    print("=" * 60) 
    print("📖 FBX合并工具 - 自定义路径示例")
    print("=" * 60)
    
    print("\n🔧 方法2: 直接设置路径")
    merger = FBXMerger()
    
    # 自定义路径
    merger.model_path = r"F:\你的路径\模型文件.fbx"
    merger.animation_path = r"F:\你的路径\动画文件.fbx"
    merger.output_path = r"F:\你的路径\输出目录"
    merger.output_filename = "自定义名称.fbx"
    
    print("⚠ 注意：请修改上面的路径为实际文件路径")
    print("  示例使用默认路径运行...")
    
    # 还原为示例路径（防止文件不存在错误）
    merger.model_path = r"F:\UnityTestProjects\ArtAssests\人物\测试3\Meshes\HumanM_Model.fbx"
    merger.animation_path = r"F:\UnityTestProjects\ArtAssests\人物\测试3\Animations\Male\Idles\HumanM@Idle01-Idle02.fbx"
    merger.output_path = r"F:\UnityTestProjects\MakeDeadCell\merged_output"
    merger.output_filename = "CustomExample_Merged.fbx"
    
    merger.run()

def example_batch_processing():
    """批量处理示例（概念性）"""
    print("=" * 60)
    print("📖 FBX合并工具 - 批量处理概念")
    print("=" * 60)
    
    print("\n🔧 概念：批量处理多个角色")
    
    # 定义批处理任务列表
    batch_tasks = [
        {
            "name": "男性角色_闲置动画",
            "model": r"F:\ArtAssets\Male\Model\Male_Model.fbx",
            "animation": r"F:\ArtAssets\Male\Animations\Male_Idle.fbx",
            "output": "Male_WithIdle.fbx"
        },
        {
            "name": "女性角色_行走动画", 
            "model": r"F:\ArtAssets\Female\Model\Female_Model.fbx",
            "animation": r"F:\ArtAssets\Female\Animations\Female_Walk.fbx", 
            "output": "Female_WithWalk.fbx"
        }
    ]
    
    print(f"  计划处理 {len(batch_tasks)} 个任务:")
    for i, task in enumerate(batch_tasks, 1):
        print(f"  {i}. {task['name']}")
        print(f"     模型: {os.path.basename(task['model'])}")
        print(f"     动画: {os.path.basename(task['animation'])}")
        print(f"     输出: {task['output']}")
    
    print("\n💡 要实现批处理，可以在循环中创建FBXMerger实例:")
    print("""
    for task in batch_tasks:
        merger = FBXMerger()
        merger.model_path = task['model']
        merger.animation_path = task['animation'] 
        merger.output_filename = task['output']
        merger.run()
    """)
    
    print("⚠ 注意：这只是示例代码，实际文件路径需要存在")

def show_configuration_options():
    """显示配置选项说明"""
    print("=" * 60)
    print("📖 配置选项说明")
    print("=" * 60)
    
    print("\n🔧 fbx_merge_config.json 配置文件格式:")
    example_config = {
        "model_path": "F:\\路径\\到\\模型文件.fbx",
        "animation_path": "F:\\路径\\到\\动画文件.fbx",
        "output_path": "F:\\路径\\到\\输出目录",
        "output_filename": "合并后的文件.fbx",
        "settings": {
            "clear_scene_before_import": True,
            "preserve_original_actions": False,
            "auto_rename_merged_actions": True,
            "bone_matching_mode": "exact"
        }
    }
    
    import json
    print(json.dumps(example_config, indent=2, ensure_ascii=False))
    
    print("\n📝 主要配置项说明:")
    print("  model_path            - 模型FBX文件路径")
    print("  animation_path        - 动画FBX文件路径")
    print("  output_path           - 输出目录路径")
    print("  output_filename       - 输出文件名")
    print("  clear_scene_before_import - 导入前是否清理场景")
    print("  preserve_original_actions - 是否保留原始动作")
    print("  auto_rename_merged_actions - 是否自动重命名合并的动作")
    print("  bone_matching_mode    - 骨骼匹配模式")

def main():
    """主函数 - 选择运行示例"""
    print("🚀 FBX合并工具使用示例")
    print("\n请选择要运行的示例:")
    print("1. 基本使用（使用配置文件）")
    print("2. 自定义路径使用")
    print("3. 批量处理概念")
    print("4. 配置选项说明")
    print("0. 退出")
    
    while True:
        try:
            choice = input("\n请输入选择 (0-4): ").strip()
            
            if choice == "0":
                print("👋 再见！")
                break
            elif choice == "1":
                example_basic_usage()
                break
            elif choice == "2":
                example_custom_paths()
                break
            elif choice == "3":
                example_batch_processing()
                break
            elif choice == "4":
                show_configuration_options()
                break
            else:
                print("⚠ 请输入有效的选择 (0-4)")
                continue
                
        except KeyboardInterrupt:
            print("\n👋 用户取消，再见！")
            break
        except Exception as e:
            print(f"❌ 发生错误: {e}")
            break

if __name__ == "__main__":
    # 如果在Blender中运行，直接运行基本示例
    try:
        import bpy
        print("🎨 检测到Blender环境，运行基本示例...")
        example_basic_usage()
    except ImportError:
        # 如果在普通Python环境中运行，显示选择菜单
        main()