# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 在此代码库中工作时提供指导。

## 项目概述

AutoDuty 是一个用于 FFXIV 的 Dalamud 插件，通过路径跟随和战斗自动化来自动完成副本/任务。它与外部插件（VNavmesh、BossMod/BossModReborn、RotationSolver/WrathCombo）集成，实现导航、Boss 机制和战斗循环功能。

## 构建命令

这是一个使用 Dalamud.NET.Sdk 的 .NET 项目。标准构建命令：

```bash
dotnet build AutoDuty.sln                    # 构建解决方案
dotnet build -c Release AutoDuty.sln         # Release 构建
dotnet build AutoDuty/AutoDuty.csproj        # 仅构建主项目
```

项目目标框架为 `net10.0-windows8.0`，需要 Dalamud 开发环境。在 Linux 上，需设置 `DALAMUD_HOME` 环境变量指向 Dalamud 库路径。

## 架构

### 核心状态机

插件通过基于阶段的状态机运行（`AutoDuty.cs:110-159`）：
- **Stopped** → **Reading_Path** → **Moving** → **Action** → (循环) → **Stopped**
- 特殊状态：**Waiting_For_Combat**、**Paused**、**Dead**、**Revived**

主更新循环：`Framework_Update()` 位于 `AutoDuty.cs:2039`，分发到各阶段处理器。

### 关键系统

**路径系统**（`/Paths/` 目录，`ContentPathsManager.cs`）：
- JSON 路径文件定义副本路线，包含位置和动作
- `DictionaryPaths` 将地图 ID 映射到 `ContentPathContainer` 对象
- 每个容器包含多个 `DutyPath` 对象（针对不同职业/角色的策略）
- 路径动作定义在 `Data/Classes.cs:109`（Boss、Interactable、Wait、TreasureCoffer 等）

**动作系统**（`ActionsManager.cs:32`）：
- 通过基于反射的分发执行路径动作（`InvokeAction()` 位于第 65 行）
- 20+ 种动作类型，处理 Boss 战斗、交互、等待、镜头控制、聊天命令
- 使用 TaskManager 排队动作任务

**辅助系统**（`/Helpers/` 目录）：
- 46+ 个专门的辅助类，用于特定任务
- `ActiveHelperBase`：有状态辅助类的基类，具有生命周期管理
- 关键辅助类：MovementHelper（VNavmesh IPC）、ObjectHelper、PlayerHelper、FollowHelper、LevelingHelper、TrustHelper、RepairHelper、InventoryHelper、QueueHelper、GotoHelper 变体、DesynthHelper、ExtractHelper、AutoRetainerHelper

**IPC 层**（`/IPC/` 目录）：
- `IPCSubscriber.cs`：订阅外部插件 API（VNavmesh、BossMod、AutoRetainer、RotationSolver、WrathCombo、Gearsetter）
- `IPCProvider.cs`：向其他插件暴露 AutoDuty 功能

### 项目结构

- `/AutoDuty/` - 主插件代码
  - `AutoDuty.cs` - 插件入口点（实现 `IDalamudPlugin`）、状态机、命令处理器
  - `/Managers/` - 高级系统管理器（ActionsManager、ContentPathsManager、SquadronManager、VariantManager、传输管理器）
  - `/Helpers/` - 用于特定自动化任务的专用工具类
  - `/IPC/` - 通过 IPC 集成外部插件
  - `/Data/` - 核心数据结构（Classes.cs）、枚举（Enums.cs）、扩展
  - `/Paths/` - 副本路径定义（JSON 文件，命名如 "(1036) Sastasha.json"）
  - `/Windows/` - UI 组件（MainWindow 及其标签页：MainTab、ConfigTab、BuildTab、PathsTab、LogTab、InfoTab、Overlay）
  - `/External/` - 游戏系统覆盖（AFK、Camera）
- `/ECommons/` - 共享工具库（项目引用）
- `/ECommons.IPC/` - IPC 工具库（项目引用）

### 执行流程

1. 用户通过 UI 或练级模式选择任务
2. `Run()` 方法（`AutoDuty.cs:877`）使用地图类型初始化任务
3. 通过 ContentPathsManager 从 JSON 加载路径
4. 阶段转换驱动执行
5. 每帧，`Framework_Update()` 处理当前阶段
6. ActionsManager 使用 TaskManager 执行路径动作
7. Helpers 处理特定任务（移动、交互、战斗）
8. IPC 与外部插件协调

### 自动化功能

**模式**：循环（重复单个任务）、播放列表（排队多个任务）、练级（自动选择练级任务）

**生命周期自动化**：
- 循环前：修理、传送到旅馆/房屋、执行命令
- 循环中：拾取宝箱、管理战斗
- 循环间：提取魔晶石、分解、军票兑换、AutoRetainer
- 循环后：停止条件（达到等级、休息经验耗尽）、关机

**多开**：命名管道和 TCP 传输用于客户端协调，同步步骤执行

## 开发注意事项

- 插件命令：`/autoduty`（别名：`/ad`），带子命令（start、stop、pause、config）
- 路径动作支持基于职业、物品数量、动作状态的条件执行
- 使用 ECommons 提供常用 Dalamud 工具
- 启用不安全代码块以访问游戏内存
- Debug 和 Release 配置中均抑制警告 8620
