# Dead Cells Test Framework

> 🎮 **Unity 版本要求：2022.3.14f1 LTS 或更高版本**  
> 📌 项目已锁定 Unity 版本，确保团队开发一致性

一个基于Unity引擎的类Dead Cells游戏框架，提供完整的2D动作游戏开发解决方案。

## 📋 目录
- [项目概述](#项目概述)
- [核心特性](#核心特性)
- [系统架构](#系统架构)
- [快速开始](#快速开始)
- [文件结构](#文件结构)
- [系统详解](#系统详解)
- [数据驱动开发](#数据驱动开发)
- [API文档](#api文档)
- [开发指南](#开发指南)
- [常见问题](#常见问题)

## 🎮 项目概述

DeadCellsTestFramework 是一个专为2D动作平台游戏设计的Unity框架，灵感来源于《Dead Cells》游戏。框架提供了两套完整的开发方案：

1. **传统代码驱动** - 适合程序员主导的开发
2. **数据驱动系统** - 结合CastleDB和LDtk，支持策划主导的开发

## ✨ 核心特性

### 🎯 游戏系统
- **精确的2D平台跳跃** - 支持Coyote Time和Jump Buffer
- **状态机驱动的角色控制** - 灵活的状态管理系统
- **深度战斗系统** - 伤害计算、暴击、抗性、状态效果
- **模块化武器系统** - 近战/远程/魔法武器支持
- **程序化房间生成** - 支持不同房间类型和连接
- **敌人AI系统** - 多种AI行为模式

### 🔧 开发工具
- **数据驱动架构** - CastleDB数据管理 + LDtk关卡编辑
- **可视化效果系统** - 屏幕震动、粒子特效、屏幕闪烁
- **一键项目设置** - GameSetupWizard自动配置项目
- **完整的动画系统** - 基于Animator的角色动画管理
- **跨平台LDtk集成** - 支持Windows/macOS/Linux的关卡编辑器自动导出

## 🚀 快速开始

### 环境要求

#### 核心依赖
- **Unity 2022.3.14f1 LTS** (必需，项目已锁版本)
- **Git Bash** (Windows用户) 或 **Terminal** (macOS/Linux用户)
- **LDtk 1.5.3+** (关卡编辑器，最低版本要求)

#### 关键 Unity 包版本 (已锁定)
- **LDtk for Unity** `6.11.2` - 关卡导入核心包
- **Universal Render Pipeline** `14.0.9` - 渲染管线  
- **2D Feature Set** `2.0.0` - 2D 开发工具集
- **TextMeshPro** `3.0.6` - 文字渲染
- **Timeline** `1.7.6` - 动画时间线
- **Newtonsoft JSON** `3.2.1` - JSON 数据处理

> ⚠️ **版本兼容性重要提示**：
> - 使用 **LDtk 1.5.3 或更高版本** 编辑关卡文件
> - 旧版本 LDtk 项目需要用最新版 LDtk 打开并保存后再导入 Unity
> - 包版本已在 `packages-lock.json` 中锁定，避免版本漂移

### 首次设置步骤

1. **克隆仓库**
   ```bash
   git clone <repository-url>
   cd MakeDeadCell
   ```

2. **打开Unity项目**
   - 使用Unity Hub打开项目文件夹
   - 等待Unity完成包导入和编译

3. **配置LDtk关卡编辑器（重要）**
   
   > ⚠️ **首次设置必须执行**：每个开发者在第一次拉取仓库后都需要执行此步骤
   
   - 在Project窗口中找到 `Assets/Data/LDtkLevel/LDtkLevelTest.ldtk`
   - 选中该文件，在Inspector中找到 **LDtk Importer** 组件
   - 点击 **"Install / Auto-add command"** 按钮
   - 这会自动配置适合你平台的导出命令

4. **验证设置**
   - 打开 `LDtkLevelTest.ldtk` 文件（用LDtk应用）
   - 进行任意修改并保存
   - 确认Unity中的 `.ldtkt` 文件自动更新

### 日常使用

- **编辑关卡**：直接编辑 `.ldtk` 文件，保存后Unity会自动重新生成 `.ldtkt` 文件
- **新增关卡**：参考现有的LDtk文件结构
- **团队协作**：确保 `.ldtk` 和 `.ldtkt` 文件都提交到版本控制

### 故障排除

#### LDtk 导出问题

如果LDtk保存后Unity没有自动更新：

1. **检查平台兼容性**：
   ```bash
   # Windows用户确保Git Bash可用
   bash --version
   
   # Unix/Linux/macOS用户确保脚本有执行权限
   chmod +x Library/LDtkTilesetExporter/export_tileset_universal.sh
   ```

2. **重新配置导出命令**：
   - 重新选中 `.ldtk` 文件
   - 再次点击 "Install / Auto-add command"

3. **LDtk 版本问题**：
   - 确保使用 LDtk 1.5.3+ 版本
   - 旧项目需用最新 LDtk 打开并保存

#### 包版本冲突处理

如果遇到包版本冲突：

1. **删除 Library 文件夹**：
   ```bash
   # 强制重新导入所有包
   rm -rf Library/
   # 重新打开 Unity 项目
   ```

2. **检查包版本锁定**：
   ```bash
   # 确认包版本锁定状态
   git status Packages/packages-lock.json
   
   # 如果被修改，重置到项目版本
   git checkout -- Packages/packages-lock.json
   ```

3. **重新安装特定包**：
   - Window > Package Manager
   - 找到对应包，点击 "Remove"
   - 重新安装指定版本

4. **查看详细说明**：
   - 参考 `Library/LDtkTilesetExporter/README.md` 获取完整的跨平台解决方案说明

> 💡 **最佳实践**：团队开发时，定期检查 `packages-lock.json` 是否被意外修改，保持版本一致性