# SPT 配置中心 · SptConfigStudio

一个面向 Windows 的图形化配置编辑器，用于调整《逃离塔科夫》（Escape from Tarkov）本地离线服务端
`SPT_Runtime/SPT_Data/configs/` 目录下的 JSON 配置文件：藏身处、空投、战局规则、商人、机器人、
死亡掉落、保险、跳蚤市场等服务端设置。工具不修改游戏客户端本体，也不改动服务端程序本体。

**English**: A Windows desktop editor for the JSON config files of a *locally installed, offline,
single-player* Escape from Tarkov server. It edits plain JSON files on your own machine, always
creates a verified ZIP backup before writing, and ships no game or server assets of its own.

---

## 免责声明（请务必先阅读） / Disclaimer

> **非官方项目。** 本仓库是独立开发的第三方工具，与 Battlestate Games Ltd.（以及其关联公司）
> 没有任何隶属、赞助、授权或背书关系，也不隶属于任何离线服务端项目的开发团队。
> **Escape from Tarkov** 及相关名称、标志、游戏内容均为 Battlestate Games Ltd. 的商标或受版权保护的材料。
>
> **不含任何第三方内容。** 本仓库仅包含本编辑器自身的源代码，**不包含、不附带、不链接、也不提供**
> 游戏客户端文件、游戏资源、游戏数据、服务端分发版本或任何第三方项目的源代码与二进制文件。
> 使用者须自行在本机已获得相应程序，本项目的作者不协助获取上述内容。
>
> **仅限本地离线单人用途。** 本工具的设计目标是对使用者自己机器上的本地配置文件做读写，
> **不适用于、也不得用于**官方在线服务器、多人对战、绕过或对抗反作弊系统、或任何损害其他玩家体验的场景。
> 本工具不含任何作弊功能，也不提供联机能力。
>
> **风险自负。** 修改服务端配置可能影响存档、平衡或导致服务端无法正常启动。使用前请保留自己的备份，
> 并确保已阅读并理解你所使用的每一款程序的用户协议与许可条款。你是否符合当地法律与相关协议，由你自行判断并承担全部责任。
>
> ---
>
> **This is an unofficial, independently developed third-party tool.** It is not affiliated with,
> sponsored, endorsed, or authorised by Battlestate Games Ltd. or by the developers of any offline
> server project. *Escape from Tarkov* and all related names, logos and game content are trademarks
> and/or copyrighted material of Battlestate Games Ltd.
>
> **This repository contains no third-party content.** It holds only the source code of this editor.
> It does not contain, bundle, link to, host, or help obtain game client files, game assets, game
> data, server distributions, or the source or binaries of any other project.
>
> **Local, offline, single-player use only.** The tool reads and writes configuration files that
> already exist on the user's own machine. It must not be used against official online servers, in
> multiplayer contexts, or to bypass or interfere with anti-cheat systems. It provides no cheats
> and no online functionality.
>
> **Use at your own risk.** The software is provided as-is, without warranty of any kind (see the
> license). Editing server configuration can affect saves or prevent the server from starting.
> You are solely responsible for complying with the terms of any software you use and with
> applicable law.

---

## 它做什么 / 不做什么

| 范围 | 说明 |
| --- | --- |
| 读取 | `<游戏根目录>/SPT_Runtime/SPT_Data/configs/*.json`、`database/templates/items.json`、`prices.json`、`database/locales/global/{ch,en}.json`（仅用于把物品 ID 显示为中文名） |
| 写入 | **仅** `SPT_Runtime/SPT_Data/configs/` 下的 JSON 文件，以及 `<游戏根目录>/Backups/` 中的自建压缩包 |
| 配置 | `%APPDATA%\SptConfigStudio\prefs.json`（记住游戏根目录、备份保留数量、向导状态） |
| 网络 | 不联网。唯一的网络访问是探测 `https://127.0.0.1:<端口>` 上本机服务端面板的健康状态，用于显示运行状态 |
| 不做 | 不注入游戏进程、不修改客户端文件、不解包或重打包任何游戏资源、不含作弊或联机功能 |

## 功能特性

- **首次设置向导**：三步完成——确认用途、选择游戏根目录、了解备份策略；目录选择后会即时校验
  `SPT_Runtime/SPT_Data/configs` 是否存在，误选目录不会造成文件损坏。
- **写盘前强制备份**：任何一次保存都会先把目标配置文件压缩打包到 `<游戏根目录>/Backups/`，
  **备份失败则中止写入**，绝不留下无备份的改动。压缩包默认保留 30 份，可按需调整。
- **备份与还原面板**：查看历史备份、对比备份内容、按需还原单个或全部文件，并可删除过期备份。
- **全字段中文标注**：28 个配置文件、约 1100 个配置键，绝大多数显示为「中文标签 + 原始键名」对照，
  未知术语（Boss 名称、弹药口径、PMC / USEC / BEAR / FiR 等）保留原文而不臆造译名。
- **快速调整面板**：藏身处、空投、战局、死亡掉落等常用项的中文面板，不必在深层 JSON 里翻找。
- **结构化树编辑器**：按分区折叠 / 展开，展开状态按「文件 + 路径」记忆；数值字段提供输入框与
  最小 / 最大值成对编辑。
- **保存前差异预览**：逐项列出「旧值 → 新值」，确认后才落盘。
- **全文搜索**：跨所有配置文件搜索键名、路径与值，点击结果直接跳转到对应字段并展开父级节点。
- **原始 JSON 视图**：需要精细控制时可直接编辑原始文本。
- **深色主题**：仿军用终端风格的界面，全部使用系统字体（Microsoft YaHei UI / Segoe UI / Cascadia Mono / Consolas），
  不附带任何字体或图片素材。

## 系统要求

- Windows 10 / 11（x64）
- .NET 10 Desktop Runtime（或使用自包含发布包则无需安装）
- 本机已存在可正常运行的离线服务端安装

## 构建与运行

```bash
git clone <本仓库地址>
cd SptConfigStudio

# 调试运行
dotnet run

# 发布（免装 .NET 运行时）
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

产物位于 `bin/Release/net10.0-windows/`（框架依赖）或 `publish/`（自包含）。

## 快速开始

1. 启动程序，按向导选择游戏根目录（即包含 `SPT_Runtime` 文件夹的那一层）。
2. 在左侧导航选择配置面板，例如「玩法 → 藏身处」或「玩法 → 空投」。
3. 调整数值或开关，点击保存。
4. 在「保存前确认」对话框核对每一项差异。
5. 程序自动创建备份压缩包并写入文件；重启服务端使改动生效。
6. 如需回退，进入「备份与还原」选择对应压缩包还原。

## 覆盖的配置文件

| 分类 | 文件 | 面板名称 |
| --- | --- | --- |
| 基础 | `core.json` `http.json` `locale.json` `backup.json` `match.json` | 服务端核心、网络与面板、语言、服务端自动备份、战局匹配 |
| 玩法 | `hideout.json` `airdrop.json` `inraid.json` `lostondeath.json` `insurance.json` `scavcase.json` `health.json` `repair.json` `ragfair.json` `trader.json` `quest.json` `gifts.json` | 藏身处、空投、战局规则、死亡掉落、保险、每日任务奖励箱、健康与致死、维修、跳蚤市场、商人、任务、礼物 |
| 世界 | `weather.json` `location.json` `bot.json` `pmc.json` `playerscav.json` `pmcchatresponse.json` `btrdelivery.json` | 天气、地图与波次、机器人、PMC 生成、Scav karma、机器人语音回复、科恩 BTR 送货 |
| 高级 | `item.json` `inventory.json` `loot.json` `seasonalevents.json` | 物品规则、背包与技能、散落战利品、季节活动 |

## 代码结构

```
SptConfigStudio/
├── App.xaml(.cs)              应用入口与主题合并
├── MainWindow.xaml(.cs)       主窗口、左侧导航与页面切换
├── SptConfigStudio.csproj     net10.0-windows / WPF
├── Core/
│   ├── AppEnv.cs              定位服务端安装目录、读写 prefs
│   ├── Workspace.cs           配置集合与保存流程（含强制备份）
│   ├── ConfigDocument.cs      单文件解析与原子写盘（临时文件 + 替换）
│   ├── BackupService.cs       ZIP 备份、保留策略、还原、内容比对
│   ├── FieldNode.cs           JSON 树 → 表单节点模型与命名拆解
│   ├── MetadataCatalog.cs     文件/分区元数据、词表、整键译名与路径级覆写
│   ├── ItemLookup.cs          物品 ID → 中文名与价格（运行时读取本地数据库）
│   ├── ConfigSearch.cs        跨文件搜索
│   ├── DiffEngine.cs          差异计算
│   └── ServerStatus.cs        本机服务端健康状态探测
├── Ui/
│   ├── QuickView.cs           快速调整面板
│   ├── ConfigView.cs          结构化 / 原始 JSON 双视图
│   ├── FieldRenderer.cs       单个字段的渲染与交互
│   ├── BackupView.cs          备份与还原
│   ├── SaveDialog.cs          保存前差异确认
│   ├── SetupWizard.cs         首次设置向导
│   ├── Dialogs.cs             通用对话框
│   └── Ui.cs                  控件工厂与排版助手
└── Theme/Dark.xaml            深色主题资源字典
```

## 中文标注机制

标签按三层优先级生成，保证每一行都有可读名称且不会掩盖真实键名：

1. **整键译名表**：分词后无法读出含义的键（如地图编号、武器槽位、弹药口径）直接命中完整译名。
2. **驼峰分词 + 词表**：`SplitCamel` 拆键名，逐词查中文词表后拼接。
3. **路径级覆写**：`文件|路径` 精确覆写，优先级最高，用于同名键在不同上下文含义不同的情况
   （例如 `Fence` 在游戏中是「黑商」而非「围栏」）。

渲染时只要译名与原始键不同，就在标签下方以等宽小字显示原始键名作为对照；查不到可靠译名的术语
保留原文，不做臆测翻译。

## 已知限制

- 仅支持 JSON 配置文件，不处理数据库记录、任务定义或 mod 本身。
- 配置文件结构随服务端版本更新可能变化，新增键会退回分词翻译或原文显示。
- 部分深层数组（如机器人的分组弹药配置）以索引形式展开，阅读性有限。
- 界面为中文，暂无英文界面对照。

## 商标与第三方权利

- *Escape from Tarkov*、Battlestate Games 及其相关标志为 **Battlestate Games Ltd.** 的商标或版权材料。
- 文中提到的离线服务端项目及其名称属于各自的第三方项目，仅作识别性描述使用。
- 上述名称的出现仅用于说明本工具的用途与读写路径，**不代表任何形式的关联、授权或背书**。
- 本仓库不含上述任何一方的代码、资源、数据或二进制文件。

## 反馈

问题反馈与功能建议请提交 Issue。若涉及配置文件解析异常，请附上配置文件文件名与字段路径
（**请勿上传游戏文件、个人存档或服务端分发内容**）。

**若相关权利方认为本仓库存在需要处理的内容，请提交ISSUE，我会尽快处理。**


**If the relevant rights holders believe that this repository contains content that needs to be addressed, please submit an ISSUE, and I will handle it ASAP.**


## 许可证

本仓库源代码采用 **MIT License** 授权，详见 [LICENSE](LICENSE)。

许可证仅覆盖本仓库自身的源代码，不覆盖使用本工具编辑的任何配置文件内容、游戏内容或第三方项目，
也不授予任何与 Battlestate Games Ltd. 或其他第三方知识产权相关的权利。
