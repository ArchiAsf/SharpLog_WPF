# SharpLog

> 业余无线电 QSO 通联日志桌面软件 —— 基于 .NET 8 / WPF / MVVM 的 Windows 单机应用

SharpLog 用于记录、检索与管理业余无线电通联（QSO）日志。数据以 SQLite 本地存储，
字段命名对齐 **ADIF 3.1.7** 国际标准，支持完整的中文界面与深色主题。

---

## 目录

- [功能概览](#功能概览)
- [技术栈](#技术栈)
- [项目结构](#项目结构)
- [技术要点](#技术要点)
- [界面截图](#界面截图)
- [构建与运行](#构建与运行)
- [数据库设计](#数据库设计)
- [ADIF 标准对接](#adif-标准对接)
- [开发说明](#开发说明)
- [项目演进](#项目演进)
- [许可](#许可)

---

## 功能概览

| 模块 | 说明 |
|---|---|
| **日志编辑页** | 呼号（自动转大写）、频率（kHz，含波段合法性校验）、模式 / 子模式（联动下拉）、信号报告、网格定位、传播模式、功率、备注等录入；必填 / 数字 / 时间顺序三重校验 |
| **日志查看页** | 38 列日志表格；键集分页（每批 2000 条，滚动到底自动加载）；双击弹出编辑窗；右键删除；查询与筛选区（查询条件动态行 + 筛选器 + SQL 自由查询 + 统计区） |
| **EditQso 编辑窗口** | 双击日志弹出的独立悬浮窗（911 × 580 固定尺寸）：主表 + 两个滚动区 + 备注区；保存 / 取消 / 删除三个按钮均二次确认 |
| **设置页** | 本地台站设置 16 项，字段名按 ADIF 标准（`MY_GRIDSQUARE`、`MY_ANTENNA` 等）；支持保存 / 重置 / 取消 |
| **时间区** | UTC 与本地时间每秒刷新，双时钟显示 |
| **晨昏区** | 日出日落计算（`SunTimeCalculator`）+ 黎明 / 日间 / 黄昏 / 夜间四个时段图标 |
| **消息提示** | 4 个页面各自独立的 HandyControl `Growl` 消息面板 |
| **深色主题** | 10 个资源字典 + 8 个样式分册，全部界面完整适配 |

---

## 技术栈

| 分类 | 选型 | 版本 |
|---|---|---|
| 运行时 | .NET（`net8.0-windows7.0`） | 8.0 |
| 语言 | C#（`Nullable` 已开启） | 12 |
| UI 框架 | WPF | — |
| 架构模式 | MVVM | — |
| MVVM 支持 | CommunityToolkit.Mvvm | 8.4.2 |
| UI 控件库 | HandyControl | 3.5.1 |
| ORM | Microsoft.EntityFrameworkCore | 8.0.29 |
| 数据库 | Microsoft.EntityFrameworkCore.Sqlite | 8.0.29 |
| 迁移工具 | Microsoft.EntityFrameworkCore.Design | 8.0.29 |

> EF Core 三个包的版本通过 `$(EfCoreVersion)` 变量在 `SharpLog.csproj` 中统一管理，
> 升级时只需改一处。

**包管理**：NuGet　|　**IDE**：Visual Studio 2026

---

## 项目结构

```
SharpLog/
├── SharpLog.slnx                  解决方案
└── SharpLog/
    ├── App.xaml / App.xaml.cs     应用入口与全局资源
    ├── View/                      界面层（8 个文件）
    │   ├── MainLogWindow.xaml     主窗口：日志表格 + 查询筛选 + 统计区
    │   ├── EditQso.xaml           通联编辑悬浮窗
    │   ├── OneLog.xaml            单条日志卡片
    │   └── Converters/            值转换器（绑定格式转换）
    ├── ViewModel/                 视图模型层（8 个文件）
    │   ├── MainLogWindow_VM.cs            主 VM
    │   ├── MainLogWindow_VM.Query.cs      查询与筛选逻辑
    │   ├── MainLogWindow_VM.Methods.cs    命令实现
    │   ├── MainLogWindow_VM.Helpers.cs    辅助方法
    │   ├── EditQso_VM.cs                  编辑窗 VM
    │   ├── QsoLogValidator.cs             三重校验规则
    │   ├── QueryConditionRow.cs           动态查询条件行
    │   └── ModeConvertTools.cs            日期 / 时间 / 模式转换
    ├── Model/                     数据模型层（15 个文件）
    │   ├── QsoLog.cs              通联记录实体（41 字段）
    │   ├── AppSettings.cs         台站设置实体（26 字段）
    │   ├── Adif/                  ADIF 数据模型
    │   └── StaticValues.cs        静态常量表
    ├── DB/                        数据访问层（3 个文件）
    │   ├── SharpLogDbContext.cs   EF Core 上下文与索引配置
    │   ├── SharpLogDbTools.cs     日志表仓储
    │   └── AppSettingsDbTools.cs  设置表仓储
    ├── Migrations/                EF Core 迁移（6 个迁移 + 快照）
    ├── ToolsBox/                  工具类（8 个文件）
    │   ├── SqlQueryTools.cs       查询组装与执行（859 行，含注入防护）
    │   ├── SunTimeCalculator.cs   日出日落计算
    │   ├── AdifDataLoader.cs      ADIF 标准数据加载
    │   ├── RangeObservableCollection.cs  批量添加不触发逐条通知
    │   ├── JsonTools.cs           JSON 读写
    │   ├── MessageTools.cs        Growl 消息封装
    │   ├── AppSettingsCache.cs    设置缓存
    │   └── PropertyCopyTools.cs   属性复制
    ├── Style/                     样式与主题（10 个资源字典）
    │   ├── ColorDic.xaml          配色定义
    │   ├── BaseStyle.xaml         基础样式
    │   ├── FormStyle.xaml         窗体样式
    │   ├── DataGridStyle.xaml     表格样式
    │   └── ...                    编辑页 / 查询 / 滚动条 / 标签页 / 设置 / EditQso 分册
    ├── Infrastructure/            基础设施
    │   ├── Logger.cs              日志记录
    │   └── Exceptions/            自定义异常
    ├── ADIFCode_Json/             ADIF 官方数据（33 波段 / 91 模式 / 20 传播模式）
    ├── logo/                      晨昏时段图标（Dawn / Day / Dusk / Night）
    └── Languages/                 语言资源（LangCN.xaml / LangEN.xaml）
```

**代码规模**：72 个源码文件（56 个 `.cs` + 16 个 `.xaml`），手写代码约 **15,700 行**
（已排除 `AssemblyInfo`、`*.Designer.cs`、`ModelSnapshot` 等生成物）。

---

## 技术要点

### 1. SQL 自由查询的注入防护（`ToolsBox/SqlQueryTools.cs`）

日志查看页允许用户直接编写 SQL 查询。因为输入完全开放，这里做了五层约定：

| 约定 | 做法 |
|---|---|
| **值一律参数化** | 所有来自用户的字符串均通过 `SqliteParameter` 传递，绝不拼进 SQL 文本；拼接的只有列名与运算符 |
| **列名走白名单** | `QueryFields` 定义 18 个可选字段，用户只能从下拉框选，不能自由输入 |
| **运算符走枚举映射** | `OperatorToSql` 只接受枚举，不接收原始字符串 |
| **写操作必须确认** | `Classify` 把非只读语句判为 `Write`，由 ViewModel 弹出二次确认；本类只负责「判断」，不负责「拦」 |
| **行数上限与超时** | `MaxRows = 500`、`TimeoutSeconds = 5`，防止自连接把内存吃满、UI 冻住 |

**只读判定踩过的三个坑**（都能在注释里看到处理方式）：

1. **多语句注入**：`SELECT 1; DROP TABLE LogTable` 只看首个关键字会被误判为只读
   → 必须逐条判断，只要有一条是写操作，整体即判定为写操作。
2. **字符串字面量干扰**：`SELECT * FROM LogTable WHERE COMMENT = 'DELETE'`
   → 判断前必须先把字符串字面量内容抹掉，否则误判为写操作。
3. **CTE 语句**：`WITH` 可能是 `WITH ... SELECT`，也可能是 `WITH ... DELETE / UPDATE / INSERT`
   → 首关键字无法区分，需要扫描全文查找写关键字。

其他细节：

- 「波段」是**伪列**（标记 `__BAND__`）——数据库中并不存在该列，由 `FREQ` 换算为频率区间，
  因此拼接时单独走频率区间分支。
- 查询走 EF 的 `FromSqlRaw` 而非自行拼接 `SELECT *`，实体与列的映射仍由 EF 负责，
  将来新增字段无需改动此处；参数依然使用 `SqliteParameter`。
- ADIF 日期 / 时间做了多格式容错：日期接受 `2026-03-28`、`2026/03/28`、`20260328`；
  时间接受 `11:00:00`、`11:00`、`110000`。

### 2. 大批量数据下的界面流畅度

- **键集分页**：日志表格每批加载 2000 条（`LogViewPageSize`），滚动到底自动加载下一批。
  相比 `OFFSET` 分页，深翻页时不会越翻越慢。
- **批量通知集合**：`RangeObservableCollection` 重写了 `AddRange`。
  若逐条 `Add`，绑定它的 `DataGrid` 会收到 2000 次集合变更通知、逐条处理 2000 遍；
  改为一次性通知后，加载性能显著提升。
- **延迟加载**：日志查看页的数据在对应 `TabItem` 首次选中时才加载。
  WPF 的 `TabItem` 内容在首次选中前不实例化，避免程序启动时白查 2000 条记录。

### 3. 主题与样式分离

`Style/` 下 10 个资源字典按职责拆分（配色 / 基础 / 窗体 / 表格 / 编辑页 / 查询 / 滚动条 /
标签页 / 设置 / EditQso），实现深色主题时只需替换 `ColorDic.xaml` 中的配色定义，
无需改动任何控件模板。

### 4. 数据校验

`QsoLogValidator` 实现三重校验：必填项检查、数值合法性、时间顺序（`TIME_OFF` 不得早于 `TIME_ON`），
并对频率做波段合法性校验（依据 ADIF 波段表的频率区间）。

---

## 界面截图

> ⚠️ **待补充。** 仓库目前尚未包含界面截图。

建议补充以下 7 张，存放于 `docs/screenshots/`，并在本文件中按 `![说明](路径)` 插入：

| 建议文件名 | 界面 |
|---|---|
| `docs/screenshots/main-window.png` | 日志查看页（主界面） |
| `docs/screenshots/log-edit.png` | 日志编辑页 |
| `docs/screenshots/edit-qso.png` | EditQso 编辑悬浮窗 |
| `docs/screenshots/query-panel.png` | 查询与筛选区 |
| `docs/screenshots/sql-query.png` | SQL 自由查询 |
| `docs/screenshots/settings.png` | 台站设置页 |
| `docs/screenshots/dark-theme.png` | 深色主题总览 |

截图方式：运行程序后按 `Alt + PrtScn` 截取当前窗口，或 `Win + Shift + S` 区域截图，
直接粘进画图 / Snipaste 另存为 PNG 即可。

---

## 构建与运行

### 环境要求

- **操作系统**：Windows 7 SP1 及以上（目标框架为 `net8.0-windows7.0`）
- **SDK**：[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **IDE**（可选）：Visual Studio 2022 及以上，需勾选「.NET 桌面开发」工作负载

### 从源码构建

```bash
git clone https://github.com/ArchiAsf/SharpLog_WPF.git
cd SharpLog_WPF
dotnet restore
dotnet build
```

### 运行

```bash
dotnet run --project SharpLog/SharpLog.csproj
```

首次运行时，程序会在**当前工作目录**下自动创建 SQLite 数据库 `SharpLog.db`
（连接字符串为 `Data Source=SharpLog.db`），并通过 EF Core 迁移自动建表，无需手工初始化。

### 添加新的数据库迁移

```bash
dotnet ef migrations add <迁移名称> --project SharpLog/SharpLog.csproj
dotnet ef database update --project SharpLog/SharpLog.csproj
```

---

## 数据库设计

### 表结构

| 表名 | 实体 | 字段数 | 说明 |
|---|---|---:|---|
| `LogTable` | `QsoLog` | 41 | 通联记录，字段名对齐 ADIF 标准 |
| `AppSettingsTable` | `AppSettings` | 26 | 台站设置，固定单行（首次插入后仅更新） |
| `__EFMigrationsHistory` | — | — | EF Core 迁移记录（框架自动维护） |

### 索引设计

`QsoLog` 上建立了 6 个索引，覆盖实际检索场景：

| 索引字段 | 用途 |
|---|---|
| `QSO_DATE` | 按通联日期查询 / 排序 |
| `CALL` | 按呼号检索 |
| `MODE` | 按模式统计 |
| `FREQ` | 按频率检索 |
| `GroupID` | 分组管理（项目自定义扩展） |
| `StarID` | 分级管理（项目自定义扩展） |

### 迁移记录

| 迁移 | 内容 |
|---|---|
| `InitialCreate` | 初始建表 |
| `AddSubmodeField` | 新增子模式字段 |
| `AddAppSettingsTable` | 新增台站设置表 |
| `RemoveOpField` | 移除 `OP` 字段 |
| `AddContactedOpField` | 新增对方操作员字段 |
| `AddZoneAndFilterFields` | 新增 `CQZ` / `ITUZ` / `DXCC` 及筛选相关字段 |

---

## ADIF 标准对接

`ADIFCode_Json/` 下存放 ADIF 官方枚举数据（版本 **3.1.7**，状态 Released）：

| 文件 | 内容 |
|---|---|
| `enumerations_band.json` | 33 个业余无线电波段 |
| `enumerations_mode.json` | 91 种通联模式 |
| `enumerations_propagation_mode.json` | 20 种传播模式 |

这些文件在 `SharpLog.csproj` 中配置为 `Content` + `PreserveNewest`，
随编译、打包、发布自动复制到输出目录，运行时由 `ToolsBox/AdifDataLoader` 读取。

**字段命名规范**：`QsoLog` 与 `AppSettings` 的字段名统一采用 ADIF 3.1.7 标准名称
（`CALL`、`FREQ`、`MODE`、`RST_SENT`、`MY_GRIDSQUARE` 等），
为后续实现 ADIF 格式的导入 / 导出打好基础。

---

## 开发说明

### 分层约定

```
View (XAML)  ──数据绑定──>  ViewModel  ──调用──>  DB / ToolsBox
     ↑                          ↑
  只负责呈现              持有状态与命令
```

- **View** 不写业务逻辑，交互通过 `Command` 绑定到 ViewModel
- **ViewModel** 通过 `CommunityToolkit.Mvvm` 的 `ObservableObject` / `RelayCommand` 实现通知与命令
- **数据访问**统一走 `DB/` 下的仓储类，不直接在 ViewModel 中操作 `DbContext`
- **通用能力**（SQL 组装、日出日落、JSON 读写等）收敛到 `ToolsBox/`

### 修改样式

深色主题的配色集中定义在 `Style/ColorDic.xaml`。调整配色后，
其余 9 个样式分册会自动生效，无需逐控件修改。

### 代码规模

| 层 | 文件数 | 行数 |
|---|---:|---:|
| View | 8 | 3,662 |
| ViewModel | 8 | 2,425 |
| Style | 10 | 1,901 |
| ToolsBox | 8 | 1,495 |
| Migrations | 13 | 1,344 |
| Model | 15 | 885 |
| DB | 3 | 495 |
| Infrastructure | 2 | 159 |
| 其他 | 5 | 150 |

---

## 项目演进

SharpLog 存在两代实现，本仓库为 **WPF 重构版**：

| | 第一代（WinForms） | 第二代（本仓库，WPF） |
|---|---|---|
| 仓库 | [ArchiAsf/SharpLog](https://github.com/ArchiAsf/SharpLog) | 本仓库 |
| UI 框架 | Windows Forms | WPF |
| 架构 | 窗体内直接编写业务逻辑 | MVVM 分层 |
| 样式 | 代码中设置控件属性 | XAML 资源字典，主题可切换 |
| 规模 | 约 3,400 行 | 约 15,700 行（手写） |

**重构动因**：第一代做到约 3,000 行时，业务逻辑与界面代码高度耦合，
样式分散在各个窗体的 Designer 文件和 `FormStyles.cs` 中，
新增一个字段需要同时改动数据模型、Designer、样式与校验四处，
维护成本随功能增长快速上升。因此第二代改用 WPF + MVVM 重构：
界面与逻辑通过数据绑定解耦，样式收敛为可替换的资源字典，
数据访问抽为独立仓储层。

---

## 已知限制 / 待办

- [ ] **呼号解析**：DXCC 实体、CQ 区、ITU 区、国旗、字母解释法尚未实现
- [ ] **多语言切换**：`Languages/LangEN.xaml` 已准备，尚未接入切换逻辑
- [ ] **ADIF 导入 / 导出**：数据模型已对齐标准，功能未实现
- [ ] **单元测试**：目前依赖手工功能验证，尚未建立自动化测试工程
- [ ] **界面截图与演示**：见上文占位说明

---

## 许可

本项目为个人学习与业余无线电实践项目。使用请遵守当地关于业余无线电的相关法律法规。
