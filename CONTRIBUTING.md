# Contributing to Dead Cells Test Framework

感谢您对 Dead Cells Test Framework 的关注！我们欢迎各种形式的贡献，无论是代码、文档、bug 报告还是功能建议。

## 📋 目录

- [开始之前](#开始之前)
- [如何贡献](#如何贡献)
- [开发设置](#开发设置)
- [代码规范](#代码规范)
- [提交指南](#提交指南)
- [测试要求](#测试要求)
- [社区准则](#社区准则)

## 🚀 开始之前

### 预备知识

在开始贡献之前，建议您具备以下知识：

- **Unity 开发经验** (2022.3 LTS)
- **C# 编程**基础
- **Git 版本控制**
- **2D 游戏开发**概念
- **LDtk 关卡编辑器**（可选但有帮助）

### 项目架构了解

请先阅读以下文档：
- [README.md](README.md) - 项目概述和快速开始
- [ARCHITECTURE.md](ARCHITECTURE.md) - 系统架构详解
- [API_REFERENCE.md](API_REFERENCE.md) - API 文档

## 🤝 如何贡献

### 1. Bug 报告

发现 bug？请通过以下步骤报告：

1. **搜索现有 Issues** - 确认问题未被报告过
2. **使用 Bug Report 模板** - 提供详细信息
3. **包含复现步骤** - 让我们能够重现问题
4. **提供环境信息** - Unity 版本、平台等

### 2. 功能建议

有新功能想法？

1. **检查现有 Issues 和 Discussions**
2. **使用 Feature Request 模板**
3. **描述用例和价值**
4. **考虑向后兼容性**

### 3. 文档改进

文档永远可以更好：

1. **修正错误** - 拼写、语法、过时信息
2. **增加示例** - 代码示例、用法说明
3. **改进结构** - 让文档更易阅读
4. **翻译** - 帮助国际化

### 4. 代码贡献

#### 适合新手的任务

- 🏷️ `good-first-issue` - 新手友好的问题
- 🏷️ `documentation` - 文档相关
- 🏷️ `help-wanted` - 需要帮助的问题

#### 开发流程

1. **Fork 仓库**
2. **创建功能分支** (`git checkout -b feature/amazing-feature`)
3. **进行开发**
4. **提交更改** (`git commit -m 'Add amazing feature'`)
5. **推送分支** (`git push origin feature/amazing-feature`)
6. **创建 Pull Request**

## 🛠️ 开发设置

### 环境要求

- **Unity 2022.3.21f1 LTS** (推荐)
- **Git** 和 **Git Bash** (Windows)
- **LDtk** (用于关卡编辑)
- **Visual Studio** 或 **JetBrains Rider** (推荐)

### 初始设置

1. **克隆仓库**
   ```bash
   git clone https://github.com/[username]/MakeDeadCell.git
   cd MakeDeadCell
   ```

2. **打开 Unity 项目**
   - 使用 Unity Hub 打开项目
   - 等待包导入完成

3. **配置 LDtk 集成**
   - 选中 `Assets/Data/LDtkLevel/LDtkLevelTest.ldtk`
   - 在 Inspector 中点击 "Install / Auto-add command"

4. **验证设置**
   - 运行示例场景
   - 确认无编译错误

### 项目结构

```
Assets/
├── Scripts/           # C# 脚本
│   ├── Core/         # 核心系统
│   ├── Player/       # 玩家控制
│   ├── Combat/       # 战斗系统
│   ├── Rooms/        # 房间管理
│   ├── Data/         # 数据驱动
│   └── Tools/        # 开发工具
├── Data/             # 游戏数据
├── Scenes/           # 游戏场景
└── Settings/         # 项目设置
```

## 📝 代码规范

### C# 代码风格

```csharp
// 类名：PascalCase
public class PlayerController : MonoBehaviour
{
    // 公共字段/属性：PascalCase
    public float JumpForce { get; set; }
    
    // 私有字段：camelCase with underscore
    private float _currentHealth;
    
    // 方法：PascalCase
    public void TakeDamage(float damage)
    {
        // 局部变量：camelCase
        var finalDamage = CalculateDamage(damage);
        _currentHealth -= finalDamage;
    }
    
    // 私有方法：PascalCase
    private float CalculateDamage(float baseDamage)
    {
        return baseDamage;
    }
}
```

### 设计原则

1. **单一职责** - 每个类只做一件事
2. **依赖注入** - 避免硬编码依赖
3. **接口隔离** - 使用接口定义契约
4. **配置驱动** - 重要参数可配置
5. **性能考虑** - 避免每帧分配内存

### 注释规范

```csharp
/// <summary>
/// 处理玩家跳跃逻辑，包括 Coyote Time 和 Jump Buffer
/// </summary>
/// <param name="inputPressed">是否按下跳跃键</param>
/// <returns>是否成功执行跳跃</returns>
public bool HandleJump(bool inputPressed)
{
    // 检查是否在地面或 Coyote Time 内
    if (!CanJump())
        return false;
        
    // 执行跳跃
    PerformJump();
    return true;
}
```

## 📤 提交指南

### Commit 消息格式

使用 [Conventional Commits](https://conventionalcommits.org/) 格式：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

#### 类型 (Type)

- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式化
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `build`: 构建系统
- `ci`: CI/CD 相关
- `chore`: 杂项

#### 示例

```bash
feat(player): add coyote time for better jump feel

- Implement 0.1s grace period after leaving platform
- Add configurable coyote time duration
- Update player state machine to handle coyote state

Closes #123
```

### Pull Request 检查清单

在提交 PR 前，请确认：

- [ ] 代码遵循项目代码规范
- [ ] 所有测试通过
- [ ] 文档已更新（如适用）
- [ ] 跨平台兼容性测试
- [ ] LDtk 集成正常工作
- [ ] 无编译警告或错误
- [ ] PR 描述清楚

## 🧪 测试要求

### 测试类型

1. **单元测试** - 测试独立组件
2. **集成测试** - 测试系统交互
3. **性能测试** - 确保帧率稳定
4. **跨平台测试** - Windows/macOS/Linux

### 测试规范

```csharp
[Test]
public void PlayerController_WhenGrounded_CanJump()
{
    // Arrange
    var player = CreateTestPlayer();
    player.SetGrounded(true);
    
    // Act
    var result = player.HandleJump(true);
    
    // Assert
    Assert.IsTrue(result);
    Assert.AreEqual(PlayerState.Jumping, player.CurrentState);
}
```

### 性能要求

- **目标帧率**: 60 FPS
- **内存分配**: 最小化每帧分配
- **加载时间**: 场景切换 < 2秒

## 🌟 社区准则

### 行为准则

我们致力于营造一个欢迎、包容的社区环境：

1. **尊重他人** - 友善、耐心地交流
2. **建设性反馈** - 提供有用的建议
3. **开放心态** - 接受不同观点
4. **协作精神** - 帮助他人成长

### 沟通渠道

- **GitHub Issues** - Bug 报告和功能请求
- **GitHub Discussions** - 一般讨论和问答
- **Pull Requests** - 代码审查和技术讨论

### 获得帮助

如果您需要帮助：

1. **查阅文档** - README、ARCHITECTURE、API_REFERENCE
2. **搜索现有 Issues**
3. **创建 Discussion** - 询问问题
4. **提供详细信息** - 包含环境、错误信息等

## 🎖️ 贡献者认可

我们重视每一个贡献，所有贡献者都会在项目中得到认可：

- **代码贡献** - 在 Contributors 列表中展示
- **文档改进** - 在文档中致谢
- **Bug 报告** - 在 Release Notes 中提及
- **社区支持** - 特殊徽章和感谢

## 📞 联系我们

如有疑问，请通过以下方式联系：

- 创建 [GitHub Issue](https://github.com/[username]/MakeDeadCell/issues)
- 发起 [GitHub Discussion](https://github.com/[username]/MakeDeadCell/discussions)

---

感谢您花时间阅读贡献指南！我们期待您的参与，一起让这个框架变得更好！ 🚀