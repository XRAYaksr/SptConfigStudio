using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

public sealed record FileMeta(string Title, string Category, string Description, bool Advanced = false);
    /// <summary>One entry on the curated "快速调整" dashboard: a field or a whole subtree of a config file.</summary>
public sealed record QuickEntry(string File, string Path);

public sealed record QuickGroup(string Title, string Subtitle, IReadOnlyList<QuickEntry> Entries);
    /// <summary>
/// Hand-written Chinese labels, explanations and sane ranges. Everything not listed here still renders through
/// the generic editor, so a new or modded field never becomes uneditable.
/// </summary>
public static class MetadataCatalog
{
    public static readonly Dictionary<string, FileMeta> Files = new(StringComparer.OrdinalIgnoreCase)
    {
        ["core.json"] = new("服务端核心", "基础", "服务器名称、功能开关、BSG 日志、修复补丁与配置加载策略。"),
        ["http.json"] = new("网络与面板", "基础", "监听地址端口、请求日志、Web 管理面板账号。改动端口需重启服务端。"),
        ["locale.json"] = new("语言", "基础", "游戏与服务端语言、支持语种列表与回退规则。"),
        ["backup.json"] = new("服务端自动备份", "基础", "SPT 自身的存档备份频率与保留数量（与本工具的压缩包备份无关）。"),
        ["match.json"] = new("战局匹配", "基础", "在线式战局匹配系统总开关。"),
        ["hideout.json"] = new("藏身处", "玩法", "制造与建造时间、技能经验、供奉圈（邪教徒）奖励与阈值。"),
        ["airdrop.json"] = new("空投", "玩法", "空投类型权重、每种空投的战利品数量、物品白/黑名单与强制投放。"),
        ["inraid.json"] = new("战局规则", "玩法", "战局菜单默认值、载具/协作撤离点、撤离人设声望与 Scav 敌对概率。"),
        ["lostondeath.json"] = new("死亡掉落", "玩法", "死亡时哪些装备槽会掉落、任务物品与保险箱处理。"),
        ["insurance.json"] = new("保险", "玩法", "保险返还概率、返还与保存时长、附件被拿走的判定。"),
        ["scavcase.json"] = new("每日任务奖励箱", "玩法", "Scav -case 各稀有度的价值区间、货币与弹药奖励规则。"),
        ["health.json"] = new("健康与致死", "玩法", "倒地与致死的健康倍率、是否保存健康状态。"),
        ["repair.json"] = new("维修", "玩法", "维修价格倍率、维修 Kits 的智力经验与武器保养。"),
        ["ragfair.json"] = new("跳蚤市场", "玩法", "动态定价、上架时长、税费与商人限购。"),
        ["trader.json"] = new("商人", "玩法", "商人刷新时间、库存重置方式、黑商（Fence）行为与价格倍率。"),
        ["quest.json"] = new("任务", "玩法", "-repeatable 任务、事件任务、阵营专属任务与邮件兑换时间。", true),
        ["gifts.json"] = new("礼物", "玩法", "游戏内可赠送物品与礼物配置。", true),
        ["weather.json"] = new("天气", "世界", "天气推进速度、季节日期与季节覆写。"),
        ["location.json"] = new("地图与波次", "世界", "各地图战利品倍率、开放区域、机器人波次与容器随机化。", true),
        ["bot.json"] = new("机器人", "世界", "机器人数量上限、装备与战利品生成、Boss 与狗牌规则。", true),
        ["pmc.json"] = new("PMC 生成", "世界", "PMC 等级、装备、口袋/背心战利品与敌对行为。", true),
        ["playerscav.json"] = new("Scav karma", "世界", "玩家 Scav 声望等级与相关系数表。", true),
        ["pmcchatresponse.json"] = new("机器人语音回复", "世界", "击杀/被击杀时 PMC 的回复概率与语气权重。"),
        ["btrdelivery.json"] = new("科恩 BTR 送货", "世界", "BTR 交货返回时间与轮询间隔。"),
        ["item.json"] = new("物品规则", "高级", "物品黑名单、Boss 物品、手册价格覆写与奖励物品过滤。", true),
        ["inventory.json"] = new("背包与技能", "高级", "随机战利品容器、密封空投箱、货币模板与技能经验倍率。", true),
        ["loot.json"] = new("散落战利品", "高级", "各地图散落物品、生成点权重与静态物品权重调整。", true),
        ["seasonalevents.json"] = new("季节活动", "高级", "圣诞/新年等季节活动的机器人外观、波次与礼物设置。", true)
    };
    /// <summary>Lower-case tokens used to build a Chinese label out of any camelCase key.</summary>
    public static readonly Dictionary<string, string> Words = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enabled"] = "启用", ["disable"] = "禁用", ["disabled"] = "已禁用", ["allow"] = "允许", ["block"] = "屏蔽",
        ["blocked"] = "屏蔽", ["blacklist"] = "黑名单", ["whitelist"] = "白名单", ["chance"] = "概率",
        ["percent"] = "百分比", ["multiplier"] = "倍率", ["interval"] = "间隔",
        ["seconds"] = "秒", ["second"] = "秒", ["minutes"] = "分钟", ["hours"] = "小时", ["time"] = "时间",
        ["override"] = "覆写", ["max"] = "最大", ["min"] = "最小", ["count"] = "数量", ["amount"] = "数量",
        ["level"] = "等级", ["levels"] = "等级", ["radius"] = "半径", ["cooldown"] = "冷却", ["weight"] = "权重",
        ["weighting"] = "权重", ["weights"] = "权重", ["limit"] = "上限", ["limits"] = "上限", ["cap"] = "上限",
        ["cost"] = "花费", ["price"] = "价格", ["value"] = "价值", ["rub"] = "卢布", ["rubles"] = "卢布",
        ["stack"] = "堆叠", ["stacksize"] = "堆叠数", ["size"] = "大小", ["length"] = "长度", ["type"] = "类型",
        ["types"] = "类型", ["items"] = "物品", ["item"] = "物品", ["weapon"] = "武器", ["weapons"] = "武器",
        ["armor"] = "护甲", ["armors"] = "护甲", ["ammo"] = "弹药", ["money"] = "货币", ["currency"] = "货币",
        ["craft"] = "制造", ["crafting"] = "制造", ["crafts"] = "制造", ["build"] = "建造", ["building"] = "建造",
        ["hideout"] = "藏身处", ["airdrop"] = "空投", ["raid"] = "战局", ["raids"] = "战局", ["bot"] = "机器人",
        ["bots"] = "机器人", ["boss"] = "Boss", ["scav"] = "Scav", ["pmc"] = "PMC", ["trader"] = "商人",
        ["traders"] = "商人", ["quest"] = "任务", ["quests"] = "任务", ["reward"] = "奖励", ["rewards"] = "奖励",
        ["exp"] = "经验", ["skill"] = "技能", ["skills"] = "技能", ["karma"] = "声望", ["standing"] = "声望",
        ["loot"] = "战利品", ["lootable"] = "可拾取", ["spawn"] = "生成", ["spawns"] = "生成", ["spawnpoint"] = "生成点",
        ["weather"] = "天气", ["season"] = "季节", ["container"] = "容器", ["containers"] = "容器", ["pocket"] = "口袋",
        ["pockets"] = "口袋", ["vest"] = "背心", ["backpack"] = "背包", ["secure"] = "保险箱", ["securecontainer"] = "保险箱",
        ["profile"] = "存档", ["profiles"] = "存档", ["server"] = "服务端", ["client"] = "客户端", ["port"] = "端口",
        ["ip"] = "IP", ["log"] = "日志", ["logging"] = "日志", ["requests"] = "请求", ["speed"] = "速度",
        ["frequency"] = "频率", ["duration"] = "持续", ["delay"] = "延迟", ["threshold"] = "阈值", ["attempt"] = "尝试",
        ["attempts"] = "尝试", ["range"] = "区间", ["slot"] = "槽位", ["slots"] = "槽位", ["equipment"] = "装备",
        ["death"] = "死亡", ["died"] = "死亡", ["insurance"] = "保险", ["return"] = "返还", ["storage"] = "存储",
        ["difficult"] = "难度", ["difficulty"] = "难度", ["hostile"] = "敌对", ["chat"] = "聊天",
        ["response"] = "回复", ["replies"] = "回复", ["prefix"] = "前缀", ["nickname"] = "昵称", ["name"] = "名称",
        ["version"] = "版本", ["default"] = "默认", ["custom"] = "自定义", ["random"] = "随机", ["always"] = "总是",
        ["never"] = "从不", ["use"] = "使用", ["keep"] = "保留", ["save"] = "保存", ["reset"] = "重置", ["update"] = "更新",
        ["refresh"] = "刷新", ["run"] = "轮询", ["cycle"] = "周期", ["pool"] = "池", ["map"] = "映射", ["maps"] = "地图",
        ["location"] = "地图", ["locations"] = "地图", ["extract"] = "撤离点", ["extracts"] = "撤离点", ["car"] = "载具",
        ["coop"] = "协作", ["tagged"] = "标记", ["cursed"] = "诅咒", ["wars"] = "混战", ["cultist"] = "邪教徒",
        ["circle"] = "圈", ["sacrifice"] = "供奉", ["rewarditem"] = "奖励物品", ["preset"] = "预设", ["presets"] = "预设",
        ["batch"] = "批量", ["points"] = "点", ["point"] = "点", ["per"] = "每", ["for"] = "用于", ["of"] = "的",
        ["to"] = "到", ["on"] = "在", ["in"] = "内", ["out"] = "外", ["within"] = "以内", ["without"] = "无",
        ["message"] = "消息", ["text"] = "文本", ["locale"] = "语言", ["language"] = "语言", ["wipes"] = "删档",
        ["wipe"] = "删档", ["repair"] = "维修", ["kit"] = "工具", ["kits"] = "工具", ["durability"] = "耐久",
        ["dispersion"] = "扩散", ["shotgun"] = "霰弹枪", ["helmet"] = "头盔", ["gas"] = "气体",
        ["dogtag"] = "狗牌", ["armband"] = "臂章", ["wallet"] = "钱包", ["fence"] = "黑商", ["purchase"] = "购买",
        ["purchases"] = "购买", ["sell"] = "出售", ["offer"] = "报价", ["listing"] = "上架", ["tax"] = "税费",
        ["redeem"] = "兑换", ["mail"] = "邮件", ["repeatable"] = "可重复", ["event"] = "活动", ["events"] = "活动",
        ["seasonal"] = "季节", ["christmas"] = "圣诞", ["gift"] = "礼物", ["gifts"] = "礼物", ["appearance"] = "外观",
        ["gear"] = "装备", ["wave"] = "波次", ["waves"] = "波次", ["open"] = "开放",
        ["fill"] = "填充", ["magazine"] = "弹匣", ["static"] = "静态", ["loose"] = "散落", ["reserve"] = "储备站",
        ["lighthouse"] = "灯塔", ["rogue"] = "流氓", ["transit"] = "过境", ["settings"] = "设置", ["config"] = "配置",
        ["global"] = "全局", ["local"] = "本地", ["backend"] = "后端", ["image"] = "图片", ["images"] = "图片",
        ["watermark"] = "水印", ["survey"] = "问卷", ["beta"] = "测试", ["disclaimer"] = "声明", ["timeout"] = "超时",
        ["bandit"] = "强盗", ["sanitar"] = "疗养师", ["killa"] = "Killa", ["reservist"] = "预备役",
        ["marksman"] = "射手", ["peacekeeper"] = "和平使者", ["role"] = "角色", ["roles"] = "角色",
        // ---- 虚词 / 连接词：让长键名读得通 ----
        ["all"] = "全部", ["any"] = "任意", ["as"] = "作为", ["be"] = "", ["can"] = "可", ["cannot"] = "无法",
        ["each"] = "每", ["has"] = "拥有", ["have"] = "拥有", ["if"] = "若", ["into"] = "入", ["is"] = "是否",
        ["it"] = "其", ["must"] = "必须", ["new"] = "新", ["non"] = "非", ["not"] = "不", ["only"] = "仅",
        ["or"] = "或", ["over"] = "超出", ["that"] = "的", ["their"] = "其", ["these"] = "这些", ["those"] = "那些",
        ["when"] = "当", ["which"] = "其中", ["who"] = "者", ["will"] = "将", ["you"] = "玩家", ["your"] = "玩家",
        ["back"] = "后", ["front"] = "前", ["left"] = "左", ["right"] = "右", ["rear"] = "后", ["side"] = "侧",
        ["big"] = "大", ["small"] = "小", ["high"] = "高", ["low"] = "低", ["older"] = "更早", ["younger"] = "更晚",
        // ---- 数值与状态 ----
        ["available"] = "可用", ["base"] = "基础", ["bonus"] = "加成", ["chanced"] = "概率", ["common"] = "普通",
        ["current"] = "当前", ["delta"] = "增量", ["difference"] = "差异", ["direct"] = "直接", ["dynamic"] = "动态",
        ["existing"] = "现有", ["expired"] = "已过期", ["expiry"] = "有效期", ["free"] = "免费", ["gain"] = "获得",
        ["higher"] = "更高", ["lower"] = "更低", ["maximum"] = "最大", ["minimum"] = "最小", ["mult"] = "倍率",
        ["multipliers"] = "倍率", ["negative"] = "负面", ["num"] = "数值", ["partial"] = "部分", ["positive"] = "正面",
        ["possible"] = "可能", ["prob"] = "概率", ["probability"] = "概率", ["quality"] = "质量", ["rating"] = "评分",
        ["reduce"] = "减少", ["reduced"] = "减少", ["reduction"] = "减少", ["relative"] = "相对", ["rep"] = "声望",
        ["required"] = "所需", ["requirement"] = "要求", ["requirements"] = "要求", ["require"] = "需要",
        ["scaling"] = "缩放", ["selected"] = "已选", ["single"] = "单个", ["specific"] = "特定", ["standard"] = "标准",
        ["thresholds"] = "阈值", ["total"] = "总计", ["unique"] = "唯一", ["unreasonable"] = "不合理",
        ["variance"] = "浮动",
        // ---- 操作 ----
        ["add"] = "增加", ["adjust"] = "调整", ["adjustment"] = "调整", ["adjustments"] = "调整", ["append"] = "追加",
        ["apply"] = "应用", ["buy"] = "购买", ["change"] = "变更", ["changes"] = "变更", ["copy"] = "复制",
        ["create"] = "创建", ["deprioritised"] = "降优先级", ["deprioritized"] = "降优先级", ["discount"] = "折扣",
        ["duplicate"] = "重复", ["edit"] = "编辑", ["enable"] = "启用", ["filter"] = "过滤", ["fix"] = "修复",
        ["fixes"] = "修复", ["generate"] = "生成", ["generation"] = "生成", ["give"] = "给予", ["hide"] = "隐藏",
        ["ignore"] = "忽略", ["invalid"] = "无效", ["make"] = "制成", ["missing"] = "缺失", ["overrides"] = "覆写",
        ["prevent"] = "阻止", ["regenerate"] = "再生", ["remove"] = "移除", ["removable"] = "可拆卸",
        ["rename"] = "重命名", ["replace"] = "替换", ["replacement"] = "替换", ["rotation"] = "轮换",
        ["send"] = "发送", ["show"] = "显示", ["simulate"] = "模拟", ["skip"] = "跳过", ["sort"] = "排序",
        ["strip"] = "拆除", ["support"] = "支持", ["supported"] = "支持的", ["supports"] = "支持", ["taken"] = "被拿走",
        ["unlock"] = "解锁", ["upd"] = "更新", ["wait"] = "等待", ["pick"] = "挑选", ["list"] = "列表",
        // ---- 时间与天气 ----
        ["acceleration"] = "推进速度", ["april"] = "四月", ["arrival"] = "到达", ["clouds"] = "云", ["cloudy"] = "多云",
        ["daily"] = "每日", ["dark"] = "黑暗", ["darkness"] = "黑暗程度", ["dates"] = "日期", ["day"] = "白天",
        ["direction"] = "风向", ["end"] = "结束", ["fog"] = "雾", ["gustiness"] = "阵风", ["intensity"] = "强度",
        ["light"] = "灯光", ["month"] = "月份", ["night"] = "夜晚", ["nighttime"] = "夜间", ["observed"] = "被观测",
        ["period"] = "周期", ["pressure"] = "气压", ["rain"] = "雨", ["rainy"] = "雨天", ["sunny"] = "晴",
        ["temp"] = "温度", ["timestamp"] = "时间戳", ["train"] = "火车", ["weekly"] = "每周", ["wind"] = "风",
        ["winter"] = "冬天", ["year"] = "年", ["yearly"] = "每年", ["start"] = "开始",
        // ---- 物品、装备与角色 ----
        ["account"] = "账号", ["achievement"] = "成就", ["ai"] = "AI", ["answer"] = "答案", ["answers"] = "答案",
        ["appearances"] = "外观", ["arena"] = "竞技场", ["attachment"] = "附件",
        ["attachments"] = "附件", ["barrel"] = "枪管", ["bipod"] = "两脚架", ["body"] = "躯体", ["bodyparts"] = "部位",
        ["boar"] = "野猪", ["bosses"] = "Boss 列表", ["bullet"] = "子弹", ["calib"] = "口径", ["caliber"] = "口径",
        ["caps"] = "大写", ["capitalisation"] = "大写", ["cartridge"] = "子弹", ["category"] = "分类",
        ["charge"] = "电量", ["chatbot"] = "聊天机器人", ["child"] = "子项", ["children"] = "子项", ["civil"] = "平民",
        ["civilian"] = "平民", ["clothing"] = "服装", ["coins"] = "现金", ["collection"] = "收藏", ["command"] = "命令",
        ["commando"] = "突击队员", ["compass"] = "指南针", ["condition"] = "状态", ["crate"] = "箱子",
        ["crit"] = "暴击", ["damage"] = "伤害", ["data"] = "数据", ["depend"] = "依赖", ["delivery"] = "送货",
        ["desc"] = "说明", ["description"] = "描述", ["develop"] = "开发", ["dictionary"] = "字典", ["dist"] = "距离",
        ["division"] = "旅", ["dog"] = "狗", ["dogtags"] = "狗牌", ["drugs"] = "药物", ["edge"] = "边缘",
        ["elimination"] = "歼灭", ["encyclopedia"] = "百科", ["entity"] = "实体", ["escape"] = "逃脱",
        ["experience"] = "经验", ["exploration"] = "探索", ["eye"] = "眼", ["eyes"] = "眼", ["eyewear"] = "眼镜",
        ["face"] = "面部", ["failure"] = "失败", ["fee"] = "费用", ["fees"] = "费用", ["feet"] = "脚",
        ["flea"] = "跳蚤市场", ["foldable"] = "可折叠", ["folded"] = "已折叠", ["food"] = "食物",
        ["foregrip"] = "前握把", ["friend"] = "友方", ["friends"] = "友方", ["functional"] = "功能性",
        ["game"] = "游戏", ["goon"] = "打手", ["grip"] = "握把", ["handguard"] = "护木", ["head"] = "头部",
        ["headwear"] = "头部装备", ["heal"] = "治疗", ["healing"] = "治疗", ["health"] = "健康", ["helper"] = "助手",
        ["holster"] = "枪套", ["hostility"] = "敌意", ["id"] = "编号", ["ids"] = "编号", ["icon"] = "图标",
        ["index"] = "索引", ["indexes"] = "索引", ["infection"] = "感染", ["infected"] = "感染者",
        ["inventory"] = "背包", ["issue"] = "问题", ["issues"] = "问题", ["killer"] = "击杀者", ["laser"] = "激光",
        ["launcher"] = "发射器", ["locales"] = "语言列表", ["loss"] = "损失",
        ["magazines"] = "弹匣", ["malfunction"] = "故障", ["meds"] = "药品", ["menu"] = "菜单", ["mod"] = "配件",
        ["modes"] = "模式", ["mods"] = "配件", ["mount"] = "导轨支架", ["muzzle"] = "枪口", ["nvg"] = "夜视仪",
        ["offers"] = "报价", ["output"] = "产出", ["pack"] = "包装", ["packs"] = "包装", ["page"] = "页",
        ["pages"] = "页", ["parent"] = "父级", ["part"] = "部件", ["parts"] = "部件", ["path"] = "路径",
        ["pen"] = "穿甲", ["pistol"] = "手枪", ["plate"] = "防弹板", ["plates"] = "防弹板", ["protection"] = "防护",
        ["protections"] = "防护", ["question"] = "问题", ["questions"] = "问题", ["reciever"] = "机匣",
        ["release"] = "发布", ["reply"] = "回复", ["resource"] = "资源", ["rifle"] = "步枪", ["scabbard"] = "刀鞘",
        ["scope"] = "瞄具", ["scopes"] = "瞄具", ["scopeless"] = "无瞄具", ["scout"] = "侦察兵",
        ["sealed"] = "密封", ["security"] = "安保", ["sent"] = "已发送", ["sender"] = "发送方", ["sentry"] = "哨兵",
        ["session"] = "会话", ["shield"] = "护板", ["shooter"] = "射手", ["sight"] = "瞄具", ["snipe"] = "狙击",
        ["sniper"] = "狙击手", ["special"] = "特殊", ["stackable"] = "可堆叠", ["stash"] = "仓库",
        ["stock"] = "枪托", ["strength"] = "力量", ["superrare"] = "超稀有", ["system"] = "系统", ["targets"] = "目标",
        ["task"] = "任务", ["template"] = "模板", ["test"] = "测试", ["title"] = "标题", ["toilet"] = "厕所",
        ["paper"] = "纸", ["tpl"] = "模板编号", ["tpls"] = "模板编号", ["treatment"] = "保养", ["trigger"] = "触发器",
        ["triggered"] = "已触发", ["tutorial"] = "教学", ["values"] = "数值", ["verbosity"] = "详细级别",
        ["victim"] = "受害者", ["war"] = "战争", ["welcome"] = "欢迎", ["zombie"] = "僵尸", ["zombies"] = "僵尸",
        ["mode"] = "模式", ["behaviour"] = "行为", ["behavior"] = "行为", ["brain"] = "行为模式",
        ["randomisation"] = "随机化", ["randomise"] = "随机化", ["randomization"] = "随机化", ["randomize"] = "随机化",
        ["rarity"] = "稀有度", ["rare"] = "稀有", ["radar"] = "雷达", ["mapping"] = "映射", ["directory"] = "目录",
        ["backup"] = "备份", ["backups"] = "备份", ["assort"] = "商品", ["assorts"] = "商品", ["exists"] = "存在",
        ["active"] = "激活", ["nontriggered"] = "未触发", ["encoded"] = "已编码", ["unheard"] = "未接听",
        ["farewell"] = "告别", ["plead"] = "求饶", ["bro"] = "兄弟", ["santa"] = "圣诞老人", ["summon"] = "召唤",
        ["summoning"] = "召唤", ["halloween"] = "万圣节", ["boost"] = "加成", ["gifted"] = "已赠送",
        ["limb"] = "肢体", ["bodies"] = "尸体", ["behind"] = "背后", ["below"] = "以下", ["above"] = "以上",
        ["exit"] = "出口", ["exits"] = "出口", ["passage"] = "通道",
        ["zone"] = "区域", ["zones"] = "区域",
        // ---- 阵营、Bot 类型与 Boss ----
        ["bear"] = "BEAR", ["usec"] = "USEC", ["pve"] = "PvE", ["pmcs"] = "PMC", ["savage"] = "野人",
        ["assault"] = "突击兵", ["crazy"] = "疯狂", ["ravange"] = "复仇", ["peacefull"] = "温和",
        ["enemies"] = "敌人", ["enemy"] = "敌人", ["neutral"] = "中立", ["follower"] = "随从",
        ["blackdiv"] = "黑色旅", ["barter"] = "以物换物", ["mixed"] = "混合", ["medical"] = "医疗",
        ["gluhar"] = "Gluhar", ["tagilla"] = "Tagilla", 
        ["zryachiy"] = "Zryachiy", ["kolontay"] = "Kolontay", ["bigpipe"] = "BigPipe", ["birdeye"] = "BirdEye",
        ["oni"] = "Oni", ["predvestnik"] = "先驱", ["prizrak"] = "幽灵", ["priest"] = "神父", ["warrior"] = "战士",
        ["sectant"] = "教徒", ["laborant"] = "实验员", ["knight"] = "骑士", ["partisan"] = "游击队员",
        ["bully"] = "恶霸", ["wedge"] = "Wedge", ["kojaniy"] = "Kojaniy", ["skier"] = "滑雪者",
        ["agro"] = "狂暴", ["fighter"] = "斗士", ["fight"] = "战斗", ["citizen"] = "平民", ["mark"] = "标记", ["estate"] = "庄园", ["ground"] = "地皮", ["close"] = "近战", ["far"] = "远程", ["wide"] = "宽", ["thin"] = "窄",
        ["force"] = "强制", ["escort"] = "护送", ["player"] = "玩家", ["first"] = "首个", ["primary"] = "主要",
        ["secondary"] = "次要", ["tactical"] = "战术", ["highest"] = "最高", ["lowest"] = "最低",
        ["objects"] = "物品", ["modifiers"] = "修正", ["repairable"] = "可维修", ["stims"] = "兴奋剂",
        ["grenades"] = "手雷", ["earpiece"] = "耳机", ["roubles"] = "卢布", ["reputation"] = "声望",
        ["spt"] = "SPT", ["with"] = "带", ["no"] = "无", ["bossplayer"] = "Boss 玩家",
        ["spawned"] = "已生成", ["desired"] = "期望", ["avoid"] = "避免", ["prefer"] = "优先",
        ["area"] = "区域", ["kills"] = "击杀", ["kill"] = "击杀", ["requested"] = "所需",
        ["normal"] = "常规", ["pity"] = "怜悯",
        ["spread"] = "散布", ["forced"] = "强制", ["additional"] = "额外的", ["friendly"] = "友方",
        ["entry"] = "进入", ["zoom"] = "倍率", ["durabilities"] = "耐久度", ["class"] = "等级", ["probabilities"] = "概率",
        ["randomised"] = "随机化", ["randomized"] = "随机化"
    };

    /// <summary>
    /// Whole-key labels for keys that read badly once split into tokens: map ids, bot types, weapon slots, skill
    /// names and long compounds repeated across files. Case and underscores do not matter here.
    /// </summary>
    public static readonly Dictionary<string, string> KeyLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        // ---- 地图 / 区域编号 ----
        ["bigmap"] = "海关", ["woods"] = "森林", ["shoreline"] = "海岸线", ["interchange"] = "立交桥",
        ["rezervbase"] = "储备站", ["lighthouse"] = "灯塔", ["lighthouse2"] = "灯塔（码头）",
        ["factory4"] = "工厂", ["factory4_day"] = "工厂（白天）", ["factory4_night"] = "工厂（夜间）",
        ["laboratory"] = "实验室", ["laboratory_dark"] = "实验室（断电）", ["tarkovstreets"] = "塔科夫街道",
        ["terminal"] = "车站", ["terminal_ui"] = "车站（仅 UI）", ["town"] = "城镇", ["suburbs"] = "郊区",
        ["privatearea"] = "私人区域", ["labyrinth"] = "迷宫", ["icebreaker"] = "破冰船",
        ["sandbox"] = "军事基地（低地）", ["sandbox_high"] = "军事基地（高地）", ["sandbox_start"] = "军事基地（起点）",
        ["develop"] = "开发地图", ["arena"] = "竞技场", ["any"] = "任意地图",
        // ---- Bot 类型分组 ----
        ["crazyassault"] = "疯狂突击兵", ["crazyassaultevent"] = "疯狂突击兵（活动）",
        ["cursedassault"] = "被诅咒的突击兵", ["assaultgroup"] = "突击小组", ["assaulttutorial"] = "突击兵（教学）",
        ["marksman"] = "精确射手", ["commando"] = "突击队员", ["sentry"] = "哨兵", ["civilian"] = "平民",
        ["peacemaker"] = "和平使者", ["gifter"] = "赠礼者", ["test"] = "测试类型",
        ["arenafighter"] = "竞技场斗士", ["arenafighterevent"] = "竞技场斗士（活动）", ["blackdivision"] = "黑色旅",
        ["exusec"] = "叛逃 USEC", ["exusecfree"] = "叛逃 USEC（自由）", ["pmcbot"] = "PMC 机器人",
        ["pmcbear"] = "PMC BEAR", ["pmcusec"] = "PMC USEC", ["pmcbotblackdiv"] = "PMC（黑色旅）",
        ["pmcbotblackdivseason"] = "PMC（黑色旅 · 季节）", ["skier"] = "滑雪者", ["shooterbtr"] = "BTR 射手",
        ["zombie"] = "僵尸", ["ravangezryachiyevent"] = "Zryachiy 复仇（活动）",
        ["bossboar"] = "Boss·Boar", ["bossboarsniper"] = "Boss·Boar 狙击手", ["bossbully"] = "Boss·恶霸",
        ["bossbullyblackdiv"] = "Boss·恶霸（黑色旅）", ["bossgluhar"] = "Boss·Sanitar",
        ["bosskilla"] = "Boss·Killa", ["bosskillaagro"] = "Boss·Killa（狂暴）", ["bossknight"] = "Boss·黑骑士",
        ["bosskojaniy"] = "Boss·Kojaniy", ["bosskolontay"] = "Boss·Kolontay", ["bosspartisan"] = "Boss·游击队",
        ["bosssanitar"] = "Boss·Sanitar", ["bosstagilla"] = "Boss·Tagilla", ["bosstagillaagro"] = "Boss·Tagilla（狂暴）",
        ["tagillahelperagro"] = "Tagilla 副手（狂暴）", ["bosstest"] = "Boss·测试", ["bosszryachiy"] = "Boss·Zryachiy",
        ["bosswedge"] = "Boss·Wedge", ["bosswedgelab"] = "Boss·Wedge（实验室）",
        ["followerbigpipe"] = "随从·BigPipe", ["followerbirdeye"] = "随从·BirdEye", ["followerboar"] = "随从·Boar",
        ["followerboarclose1"] = "随从·Boar（近战 1）", ["followerboarclose2"] = "随从·Boar（近战 2）",
        ["followerbully"] = "随从·恶霸", ["followerbullyblackdiv"] = "随从·恶霸（黑色旅）",
        ["followergluharassault"] = "随从·Sanitar 突击", ["followergluharscout"] = "随从·Sanitar 侦察",
        ["followergluharsecurity"] = "随从·Sanitar 安保", ["followergluharsnipe"] = "随从·Sanitar 狙击",
        ["followerkojaniy"] = "随从·Kojaniy", ["followerkolontayassault"] = "随从·Kolontay 突击",
        ["followerkolontaysecurity"] = "随从·Kolontay 安保", ["followersanitar"] = "随从·Sanitar",
        ["followertagilla"] = "随从·Tagilla", ["followertest"] = "随从·测试", ["followerzryachiy"] = "随从·Zryachiy",
        ["followerwedgelab"] = "随从·Wedge（实验室）", ["sectantoni"] = "教徒·Oni",
        ["sectantpredvestnik"] = "教徒·先驱", ["sectantpriest"] = "教徒·神父", ["sectantprizrak"] = "教徒·幽灵",
        ["sectantwarrior"] = "教徒·战士", ["infectedassault"] = "感染者·突击兵", ["infectedcivil"] = "感染者·平民",
        ["infectedlaborant"] = "感染者·实验员", ["infectedpmc"] = "感染者·PMC", ["infectedtagilla"] = "感染者·Tagilla",
        ["vsrf"] = "对局·俄军", ["vsrffight"] = "对局·俄军步兵", ["vsrfsniper"] = "对局·俄军狙击手",
        ["weeklyboss"] = "每周 Boss", ["goonspawn"] = "打手生成",
        // ---- 装备槽位 ----
        ["back_plate"] = "后防弹板", ["front_plate"] = "前防弹板", ["left_side_plate"] = "左侧防弹板",
        ["right_side_plate"] = "右侧防弹板", ["side_plate"] = "侧防弹板", ["rig"] = "弹挂",
        ["armoredrig"] = "装甲弹挂", ["bodyarmor"] = "护甲", ["faceshield"] = "面罩", ["facecover"] = "面部遮蔽",
        ["facemask"] = "面罩", ["mod_barrel"] = "配件·枪管", ["mod_bipod"] = "配件·两脚架",
        ["mod_charge"] = "配件·枪口装置", ["mod_flashlight"] = "配件·手电", ["mod_foregrip"] = "配件·前握把",
        ["mod_front_sight"] = "配件·准星", ["mod_rear_sight"] = "配件·照门", ["mod_handguard"] = "配件·护木",
        ["mod_launcher"] = "配件·下挂发射器", ["mod_muzzle"] = "配件·枪口", ["mod_nvg"] = "配件·夜视仪",
        ["mod_pistol_grip"] = "配件·手枪握把", ["mod_pistol_grip_akms"] = "配件·手枪握把（AK 系）",
        ["mod_reciever"] = "配件·机匣", ["mod_scope"] = "配件·瞄准镜", ["mod_sight_front"] = "配件·前瞄具",
        ["mod_sight_rear"] = "配件·后瞄具", ["mod_stock"] = "配件·枪托", ["mod_tactical"] = "配件·战术装置",
        ["mod_mount"] = "配件·导轨", ["mod_magazine"] = "配件·弹匣",
        ["mod_tactical_000"] = "配件·战术装置", ["mod_tactical_001"] = "配件·战术装置 2",
        ["mod_tactical_002"] = "配件·战术装置 3", ["mod_tactical_003"] = "配件·战术装置 4",
        ["mod_tactical_2"] = "配件·战术装置 3", ["mod_tactical001"] = "配件·战术装置 2",
        ["mod_tactical002"] = "配件·战术装置 3", ["mod_mount_000"] = "配件·导轨", ["mod_mount_001"] = "配件·导轨 2",
        ["mod_mount_002"] = "配件·导轨 3", ["mod_mount_003"] = "配件·导轨 4", ["mod_mount_004"] = "配件·导轨 5",
        ["mod_mount_005"] = "配件·导轨 6", ["mod_mount_006"] = "配件·导轨 7", ["mod_muzzle_000"] = "配件·枪口",
        ["mod_muzzle_001"] = "配件·枪口 2", ["mod_scope_000"] = "配件·瞄准镜", ["mod_scope_001"] = "配件·瞄准镜 2",
        ["mod_scope_002"] = "配件·瞄准镜 3", ["mod_scope_003"] = "配件·瞄准镜 4", ["mod_stock_000"] = "配件·枪托",
        ["mod_equipment"] = "配件·装备",
        // ---- 机器人装备开关（bot.json / pmc.json 每种类型都有同名键）----
        ["faceShieldIsActiveChancePercent"] = "面罩开启概率（%）",
        ["laserIsActiveChancePercent"] = "激光指示器开启概率（%）",
        ["lightIsActiveDayChancePercent"] = "手电白天开启概率（%）",
        ["lightIsActiveNightChancePercent"] = "手电夜间开启概率（%）",
        ["nvgIsActiveChanceDayPercent"] = "夜视仪白天开启概率（%）",
        ["nvgIsActiveChanceNightPercent"] = "夜视仪夜间开启概率（%）",
        ["forceOnlyArmoredRigWhenNoArmor"] = "无护甲时只穿装甲弹挂",
        ["forceRigWhenNoVest"] = "无背心时改穿弹挂",
        ["forceStock"] = "强制携带枪托",
        ["minExtractsWithSpecificExit"] = "含指定出口的撤离点下限",
        ["maxExtractsWithSpecificExit"] = "含指定出口的撤离点上限",
        // ---- 技能名 ----
        ["vitality"] = "生命力", ["mining"] = "搜刮", ["memory"] = "记忆", ["charisma"] = "魅力",
        ["metabolism"] = "新陈代谢", ["sniping"] = "狙击", ["covertmovement"] = "潜行",
        ["endurance"] = "耐力", ["perception"] = "感知", ["attention"] = "专注", ["surgery"] = "手术",
        ["magdrilling"] = "弹匣装填", ["magloading"] = "弹匣装填", ["throwables"] = "投掷物",
        ["preparation"] = "战前准备", ["firstaid"] = "医疗", ["shooting"] = "射击",
        ["weaponmaintenance"] = "武器维护", ["intellect"] = "智力", ["strength"] = "力量",
        // ---- 分词后读不通的长键 ----
        ["healthmultipliers"] = "健康倍率", ["skillgainmultipliers"] = "技能经验倍率", ["blacked"] = "倒地阈值",
        ["foundinraid"] = "战局内获得（FiR）", ["purchasesarefoundinraid"] = "购买物品记为 FiR",
        ["keepfirsecurecontainerondeath"] = "死亡保留保险箱内 FiR",
        ["alwayskeepfoundinraidonraidend"] = "战局结束仍保留 FiR",
        ["botroleswithdogtags"] = "会掉落狗牌的机器人角色",
        ["botrolesthatmusthaveuniquename"] = "必须使用唯一名称的机器人角色",
        ["chanceassaultscavhasplayerscavname"] = "突击兵沿用玩家 Scav 名称的概率",
        ["showtypeinnickname"] = "昵称中显示机器人类型", ["maxbackups"] = "保留备份份数",
        ["backupcooldown"] = "备份冷却（秒）", ["backupinterval"] = "备份间隔", ["intervalminutes"] = "间隔（分钟）",
        ["directory"] = "备份目录", ["bsglogging"] = "BSG 日志", ["verbosity"] = "日志详细级别",
        ["sendtoserver"] = "发送到 BSG 服务器", ["projectname"] = "项目名称",
        ["compatibletarkovversion"] = "兼容的塔科夫版本", ["sptfriendnickname"] = "SPT 好友昵称",
        ["release"] = "版本发布说明", ["chatbotfeatures"] = "聊天机器人功能", ["compressprofile"] = "压缩存档",
        ["customwatermarklocalekeys"] = "自定义水印文案键", ["logresponsebodies"] = "记录响应正文",
        ["serverimagepathoverride"] = "服务端图片路径覆写", ["webauthenticationconfig"] = "Web 面板账号验证",
        ["enabledefaultuser"] = "启用默认账号", ["allowdefaultuserfromanyip"] = "允许默认账号从任意 IP 登录",
        ["requirecredentialsonlocalhost"] = "本机访问也需凭据", ["defaultuser"] = "默认账号",
        ["username"] = "用户名", ["password"] = "密码", ["isadministrator"] = "是否管理员",
        ["serversupportedlocales"] = "服务端支持的语言", ["fallbacks"] = "语言回退规则",
        ["gamelocale"] = "游戏语言", ["serverlocale"] = "服务端语言", ["raidmenusettings"] = "战局菜单默认值",
        ["aiamount"] = "AI 数量", ["aidifficulty"] = "AI 难度", ["bossenabled"] = "生成 Boss",
        ["scavwars"] = "Scav 混战", ["enablepve"] = "启用 PvE", ["taggedandcursed"] = "标记与诅咒",
        ["carextractbasestandinggain"] = "载具撤离基础声望", ["coopextractbasestandinggain"] = "协作撤离基础声望",
        ["scavextractstandinggain"] = "Scav 撤离声望获得", ["playerscavhostilechancepercent"] = "玩家 Scav 被敌对概率（%）",
        ["minattachmentroublepricetobetaken"] = "附件被拿走的价格门槛（₽）",
        ["chancenattachmentsbeingtakenpercent"] = "附件全部保住概率（%）",
        ["chance_no_attachments_taken_percent"] = "附件全部保住概率（%）",
        ["simulateitemsbeingtaken"] = "模拟附件被拿走", ["returnchancepercent"] = "保险返还概率（%）",
        ["lootitemresourcerandomization"] = "战利品资源随机化", ["itemspawnlimits"] = "物品生成上限",
        ["securecontainerammostackcount"] = "保险箱内弹药堆叠数", ["walletloot"] = "钱包战利品",
        ["lowprofilegasblocktpls"] = "低轮廓防毒面具黑名单", ["presetbatch"] = "预设批量",
        ["randomlootcontainers"] = "随机战利品容器", ["rewardtplpool"] = "奖励模板池",
        ["sealedairdropcontainer"] = "密封空投箱", ["defaultpresetsonly"] = "仅用默认预设",
        ["weaponmodrewardlimits"] = "武器配件奖励上限", ["ammoboxwhitelist"] = "弹药箱白名单",
        ["custommoneytpls"] = "自定义货币模板", ["deprioritisedmoneycontainers"] = "降低优先级的货币容器",
        ["handbookpriceoverride"] = "手册价格覆写", ["parentid"] = "父级编号",
        ["customitemglobalpresets"] = "自定义物品全局预设", ["lootmaxspawnlimits"] = "战利品生成点上限",
        ["customwaves"] = "自定义波次", ["roguelighthousespawntimesettings"] = "灯塔流氓生成设置",
        ["scavraidtimesettings"] = "Scav 战局时长设置", ["fitlootintocontainerattempts"] = "战利品塞入容器尝试次数",
        ["addopenzonestoallmaps"] = "为全部地图开放新区域", ["addcustombotwavestomaps"] = "为地图注入自定义波次",
        ["enablebottypelimits"] = "启用机器人类型上限", ["bottypelimits"] = "机器人类型上限",
        ["containerrandomisationsettings"] = "容器随机化设置",
        ["containertypesToNotRandomise"] = "不参与随机化的容器类型",
        ["containergroupminsizemultiplier"] = "容器组最小容量倍率",
        ["containergroupmaxsizemultiplier"] = "容器组最大容量倍率",
        ["allowduplicateitemsinstaticcontainers"] = "静态容器允许重复物品",
        ["magazineloothasammochancepercent"] = "弹匣内已装弹概率（%）",
        ["staticmagazineloothasammochancepercent"] = "静态弹匣内已装弹概率（%）",
        ["trainarrivaldelayobservedseconds"] = "火车到达延迟（观测后，秒）", ["waittimeseconds"] = "等待时间（秒）",
        ["bearonlyquests"] = "仅 BEAR 任务", ["useconlyquests"] = "仅 USEC 任务", ["locationidmap"] = "地图编号映射",
        ["repeatablequesttemplateids"] = "可重复任务模板编号", ["eventquests"] = "活动任务",
        ["starttimestamp"] = "开始时间戳", ["endtimestamp"] = "结束时间戳", ["yearly"] = "每年重复",
        ["shownonseasonaleventquests"] = "显示非季节活动任务", ["completion"] = "完成类",
        ["elimination"] = "歼灭类", ["exploration"] = "探索类", ["pickup"] = "拾取类", ["side"] = "阵营",
        ["mailredeemtimehours"] = "邮件兑换时间（小时）", ["bearplayerbehaviour"] = "BEAR 玩家行为",
        ["usecplayerbehaviour"] = "USEC 玩家行为", ["savageplayerbehaviour"] = "野人玩家行为",
        ["freechanges"] = "免费更换次数", ["freechangesavailable"] = "剩余免费更换次数",
        ["keepdailyquesttypeonreplacement"] = "更换时保留每日任务类型", ["minplayerlevel"] = "最低玩家等级",
        ["numquests"] = "任务数量", ["templateids"] = "模板编号", ["updatetime"] = "更新时间",
        ["updatetimedefault"] = "默认更新时间", ["assortsize"] = "商品数量",
        ["ammoMaxPenLimit"] = "弹药穿甲上限", ["itemCategoryRoublePriceLimit"] = "物品分类价格上限（₽）",
        ["btrdeliveryexpirehours"] = "BTR 送货返回时限（小时）",
        ["chancePlateExistsInArmorPercent"] = "护甲含防弹板概率（%）",
        ["presetSlotsToRemoveChancePercent"] = "预设移除槽位概率（%）", ["coopExtractGift"] = "协作撤离礼物",
        ["sendgift"] = "发送礼物", ["giftexpiryhours"] = "礼物有效期（小时）", ["discountoptions"] = "折扣选项",
        ["itempricemult"] = "物品价格倍率", ["presetpricemult"] = "预设价格倍率",
        ["partialrefreshchangepercent"] = "部分刷新变动（%）", ["partialrefreshtimeseconds"] = "部分刷新间隔（秒）",
        ["playerrepmin"] = "玩家声望下限", ["playerrepmax"] = "玩家声望上限",
        ["preventduplicateoffersofcategory"] = "按分类屏蔽重复报价",
        ["regenerateassortsonrefresh"] = "刷新时重新生成商品", ["maxbotcap"] = "机器人总数上限",
        ["botnamelengthlimit"] = "机器人名长度上限", ["generateweatheramounthours"] = "天气生成时长（小时）",
        ["timeperiod"] = "时间周期", ["seasondates"] = "季节日期区间", ["acceleration"] = "天气推进速度",
        ["applyrandomizedurabilityloss"] = "维修随机扣除耐久",
        ["repairkitintellectgainmultiplier"] = "维修工具智力经验倍率",
        ["armorkitskillpointgainperrepairpointmultiplier"] = "护甲 Kit 每点维修智力倍率",
        ["weapontreatment"] = "武器保养", ["critfailureamount"] = "暴击失败数量", ["critfailurechance"] = "暴击失败概率",
        ["critsuccessamount"] = "暴击成功数量", ["critsuccesschance"] = "暴击成功概率",
        ["pointgainmultiplier"] = "技能点倍率", ["dynamic"] = "动态定价",
        ["offerlistingtaxmultiplier"] = "上架税费倍率", ["airdroptypeweightings"] = "空投类型权重",
        ["customairdropmapping"] = "自定义空投映射", ["useforcedloot"] = "使用强制战利品",
        ["blockseasonalitemsoutofseason"] = "非赛季时屏蔽季节物品", ["allowbossitems"] = "允许 Boss 物品",
        ["userewarditemblacklist"] = "套用奖励物品黑名单", ["armorlevelwhitelist"] = "护甲等级白名单",
        ["itemtypewhitelist"] = "物品类型白名单", ["itemblacklist"] = "物品黑名单", ["itemlimits"] = "物品数量上限",
        ["itemstacklimits"] = "物品堆叠上限", ["weaponpresetcount"] = "武器预设数量",
        ["armorpresetcount"] = "护甲预设数量", ["weaponcratecount"] = "武器箱数量", ["itemcount"] = "物品数量",
        ["maxrewarditemcount"] = "单次最大奖励物品数",
        ["maxattemptstopickrewardswithinbudget"] = "预算内挑选奖励的最大尝试",
        ["hideoutcraftsacrificethresholdrub"] = "可供奉物品价值门槛（₽）",
        ["highvaluethresholdrub"] = "高价值判定阈值（₽）", ["bonuschancemultiplier"] = "额外奖励概率倍率",
        ["rewardpricemultiplierminmax"] = "奖励价格倍率区间", ["crafttimethresholds"] = "制造时间阈值",
        ["directrewards"] = "直接奖励", ["directrewardstacksize"] = "直接奖励堆叠数",
        ["additionalrewarditempool"] = "额外奖励物品池", ["hideouttaskrewardtimeseconds"] = "任务奖励发放时间（秒）",
        ["hideoutcraftstoadd"] = "新增藏身处制造项", ["craftidtocopy"] = "复制来源制造编号",
        ["craftoutputtpl"] = "制造产出编号", ["runintervalvalues"] = "容器轮询间隔",
        ["updateprofilehideoutwhenactivewithinminutes"] = "藏身处活跃判定（分钟）",
        ["looselootmultiplier"] = "散落战利品倍率", ["staticlootmultiplier"] = "静态战利品倍率",
        ["looselootspawnpointadjustments"] = "散落战利品生成点调整", ["staticitemweightadjustment"] = "静态物品权重调整",
        ["rewarditemvaluerangerub"] = "奖励箱价值区间（₽）", ["moneyrewards"] = "现金奖励",
        ["allowbossitemsasrewards"] = "允许 Boss 物品作为奖励",
        ["allowmultipleammorewardsperrarity"] = "同稀有度可出多组弹药",
        ["allowmultiplemoneyrewardsperrarity"] = "同稀有度可出多笔现金",
        ["collectiontimehours"] = "领取时限（小时）", ["associatedevent"] = "关联活动",
        ["maxtosendplayer"] = "单玩家最大赠送数", ["localeTextId"] = "文案编号",
        ["enablebsglist"] = "启用 BSG 屏蔽清单", ["enablecustomitemcategorylist"] = "启用自定义分类屏蔽清单",
        ["enablequestlist"] = "启用任务物品屏蔽清单", ["damagedammopacks"] = "损坏的弹药包",
        ["ignorequalitypricevarianceblacklist"] = "忽略品质价差黑名单",
        ["itempriceoverriderouble"] = "物品价格覆写（₽）", ["nonstackablecount"] = "不可堆叠数量",
        ["offeradjustment"] = "报价调整", ["adjustpricewhenbelowhandbookprice"] = "低于手册价时自动调价",
        ["handbookpricemultiplier"] = "手册价格倍率",
        ["maxpricedifferencebelowhandbookpercent"] = "低于手册价最大差额（%）",
        ["generatebasefleaprices"] = "生成跳蚤基础价", ["generatepresetpricebychildren"] = "按子件推算预设价",
        ["preventpricebeingbelowtraderbuyprice"] = "禁止低于商人收购价", ["usehandbookprice"] = "使用手册价格",
        ["priceranges"] = "价格区间", ["unreasonablemodprices"] = "不合理配件价格",
        ["handbookpriceovermultiplier"] = "高于手册价倍率", ["newpricehandbookmultiplier"] = "新品价对手册倍率",
        ["usetraderpriceforoffersifhigher"] = "报价取商人价较高者", ["showassinglestack"] = "以单堆叠显示",
        ["showdefaultpresetsonly"] = "仅显示默认预设", ["removeseasonalitemswhennotinevent"] = "非活动时移除季节物品",
        ["expiredofferthreshold"] = "过期报价阈值", ["endtimeseconds"] = "上架结束时间（秒）",
        ["removeremovableplatechance"] = "可拆防弹板移除概率", ["makesinglestackonly"] = "仅生成单堆叠",
        ["minroublecosttobecomebarter"] = "转为以物换物的最低卢布价", ["pricerangevariancepercent"] = "价格区间浮动（%）",
        ["stackablepercent"] = "可堆叠占比（%）", ["conditionchance"] = "品质条件概率",
        ["btrdelivery"] = "BTR 送货", ["bosspool"] = "Boss 轮换池", ["resetday"] = "重置日",
        ["locationpool"] = "地图轮换池", ["rotationintervalhours"] = "轮换间隔（小时）",
        ["replacescavwith"] = "替换 Scav 为", ["newyear2023"] = "新年 2023", ["newyear2024"] = "新年 2024",
        ["newyear2021"] = "新年 2021", ["christmas2022"] = "圣诞 2022", ["lunarnewyear2024"] = "农历新年 2024",
        ["aprilfools"] = "愚人节", ["halloweensummon"] = "万圣节召唤", ["halloweenzombies"] = "万圣节僵尸",
        ["halloweennightcult"] = "万圣节夜祭", ["bsgbirthday2023"] = "BSG 周年 2023",
        ["twitchnewyears2023"] = "Twitch 新年 2023", ["1clickdressup"] = "一键换装", ["500cigarettes"] = "500 根香烟",
        ["armorplates"] = "防弹板", ["groundzero"] = "归零时刻", ["groundzero2"] = "归零时刻 2",
        ["richandexpensive"] = "富贵逼人", ["lefthandheadeyes"] = "左手头眼", ["streamerloot"] = "主播战利品",
        ["hideoutcat"] = "藏身处猫咪", ["endofwipe"] = "删档结束", ["unheard"] = "未接听",
        ["unheard_edition"] = "未接听 · 版本", ["discord"] = "Discord", ["il2cpp"] = "IL2CPP",
        ["developersdevelopersdevelopers"] = "开发者们", ["roadmap"] = "路线图", ["noescape"] = "无路可逃",
        ["kappa4u"] = "Kappa 4U", ["toktok1m"] = "TokTok 1M", ["greedisgood"] = "贪婪是好的",
        ["arenabattle"] = "竞技场之战", ["cupseries"] = "杯赛", ["waffle"] = "华夫饼",
        ["nightwing"] = "夜翼", ["wedge"] = "Wedge", ["kojaniy"] = "Kojaniy", ["tagilla"] = "Tagilla",
        ["killa"] = "Killa", ["gluhar"] = "Gluhar", ["sanitar"] = "Sanitar", ["zryachiy"] = "Zryachiy",
        ["kolontay"] = "Kolontay", ["knight"] = "黑骑士", ["partisan"] = "游击队员", ["bully"] = "恶霸",
        ["bigpipe"] = "BigPipe", ["birdeye"] = "BirdEye", ["oni"] = "Oni", ["predvestnik"] = "先驱",
        ["prizrak"] = "幽灵", ["priest"] = "神父", ["warrior"] = "战士", ["sectant"] = "教徒",
        ["laborant"] = "实验员", ["boar"] = "野猪", ["savage"] = "野人", ["goon"] = "打手",
        ["toiletPaper"] = "卫生纸", ["foodMedical"] = "食品医疗", ["weaponArmor"] = "武器护甲",
        ["commandofeatures"] = "突击队员功能", ["spawning"] = "生成", ["spawnchance"] = "生成概率",
        ["chancedenemies"] = "概率敌人", ["specificexits"] = "指定撤离点", ["skipsbackplateiffrontplatemissing"] = "前板缺失时跳过后板",
        ["skipBackPlateIfFrontPlateMissing"] = "前板缺失时跳过后板",
        ["scopesselectedmodes"] = "瞄具倍率模式", ["selectedscope"] = "已选瞄具", ["firemode"] = "射击模式",
        ["malfunctionprotections"] = "故障防护", ["randomisation"] = "随机化", ["enable"] = "启用",
        ["enabled"] = "启用", ["disabled"] = "禁用", ["isnew"] = "是否新增", ["isencoded"] = "是否加密",
        ["isfunctional"] = "是否可用", ["exampleparentid"] = "示例父级编号",
        ["standard"] = "标准", ["generation"] = "生成", ["relativeprobability"] = "相对概率",
        ["sideplate"] = "侧防弹板", ["headgear"] = "头部装备", ["compass"] = "指南针", ["aids"] = "辅助",
        ["questionid"] = "问题编号", ["welcomepagedata"] = "欢迎页文本", ["farewellpagedata"] = "告别页文本",
        ["en-*"] = "英语回退", ["zh-*"] = "中文回退", ["pt-*"] = "葡萄牙语回退", ["es-*"] = "西班牙语回退",
        ["_id"] = "编号", ["_parent"] = "父级", ["_tpl"] = "模板编号", ["_encyclopedia"] = "百科条目",
        ["_changeWeaponName"] = "改名武器",

        // ---- 弹药口径（分词后只剩编号，读不出含义）----
        ["caliber12g"] = "口径 12 Gauge", ["caliber20g"] = "口径 20 Gauge", ["caliber23x75"] = "口径 23×75",
        ["caliber366tkm"] = "口径 366 TKM", ["caliber46x30"] = "口径 4.6×30", ["caliber57x28"] = "口径 5.7×28",
        ["caliber545x39"] = "口径 5.45×39", ["caliber556x45nato"] = "口径 5.56×45 NATO",
        ["caliber762x35"] = "口径 7.62×35", ["caliber762x39"] = "口径 7.62×39", ["caliber762x51"] = "口径 7.62×51",
        ["caliber762x54r"] = "口径 7.62×54R", ["caliber9x18pm"] = "口径 9×18 PM", ["caliber9x19para"] = "口径 9×19 Para",
        ["caliber9x21"] = "口径 9×21", ["caliber9x39"] = "口径 9×39", ["caliber1143x23acp"] = "口径 .45 ACP",
        ["caliber127x55"] = "口径 12.7×55",

        // ---- 零散长键 ----
        ["features"] = "功能开关", ["localekey"] = "本地化文本键", ["titlelocalekey"] = "标题文本键",
        ["minpickupcount"] = "最少拾取数量", ["maxpickupcount"] = "最多拾取数量",
        ["requireditemsarefir"] = "所需物品须为 FiR", ["requesteditemcount"] = "所需物品数量",
        ["requestedbulletcount"] = "所需子弹数量", ["weaponmodlimits"] = "武器配件限制",
        ["equipmentmods"] = "装备配件", ["equipmentmodsmodifiers"] = "装备配件修正",
        ["randomisedweaponmodslots"] = "随机化武器配件槽", ["modesscopesselected"] = "配件瞄具已选倍率",
        ["en"] = "英文", ["armband"] = "臂章", ["tarcoins"] = "TarCoins",
        ["runIntervalSeconds"] = "轮询间隔（秒）", ["craftTimeSeconds"] = "制造耗时（秒）",
        ["seconds"] = "秒数", ["expireSeconds"] = "过期时间（秒）",
        ["storageTimeOverrideSeconds"] = "保存时长覆写（秒）", ["returnTimeOverrideSeconds"] = "返还时间覆写（秒）",
        ["firstPrimaryWeapon"] = "第一主武器", ["secondPrimaryWeapon"] = "第二主武器",
        ["hintlocalekey"] = "提示文本键", ["sectactpriesteve"] = "教徒·神父（事件）",
        ["sectactpriestevent"] = "教徒·神父（事件）",
    };

    /// <summary>Path-scoped overrides: "hideout.json|overrideCraftTimeSeconds".</summary>
    public static readonly Dictionary<string, FieldMetaOverride> Overrides = Build(StringComparer.Ordinal);

    public sealed record FieldMetaOverride(
        string? Label = null, string? Hint = null, string? Unit = null,
        double? Min = null, double? Max = null, string[]? Enum = null, FieldKind? Kind = null);

    private static Dictionary<string, FieldMetaOverride> Build(StringComparer c)
    {
        var d = new Dictionary<string, FieldMetaOverride>(c);
        void Add(string key, FieldMetaOverride o) => d[key] = o;

        const string NEG1 = "-1 表示不覆写，交由服务端按原始数据计算。";
        // ---- core ----
        Add("core.json|features.compressProfile", O(null, "存档压缩，能显著减小 user/profiles 体积；排障时可临时关闭以便直接读 JSON。"));
        Add("core.json|allowProfileWipe", O(null, "是否允许从 Web 面板删除存档。"));
        Add("core.json|serverName", O("服务器名称", "显示在游戏内。"));
        Add("core.json|profileSaveIntervalSeconds", O("存档保存间隔（秒）", "越小越安全，但磁盘写入更频繁。", 0, 3600));
        Add("core.json|fixes.fixShotgunDispersion", O("修复霰弹枪扩散", "SPT 提供的霰弹枪散布修复。"));
        Add("core.json|fixes.removeModItemsFromProfile", O("移除存档中的 Mod 物品", "危险：会让依赖 Mod 物品的任务失效。"));
        Add("core.json|bsgLogging.sendToServer", O("向 BSG 上报日志", "离线环境下无意义，保持关闭。"));
        // ---- http ----
        Add("http.json|port", O("监听端口", "游戏客户端与服务端面板共用；修改后需重启服务端。", 1, 65535));
        Add("http.json|logRequests", O("记录每个请求", "日志量大，排障时才开。"));
        Add("http.json|webAuthenticationConfig.enabled", O("面板账号验证", null));
        Add("http.json|webAuthenticationConfig.defaultUser.password", O("默认账号密码", "默认 spt/spt，仅本机可访问时风险较低。"));
        // ---- hideout ----
        Add("hideout.json|overrideCraftTimeSeconds", O("覆写所有制造时间（秒）", NEG1 + " 例如填 1 即可秒造。", -1, 864000, unit: "秒"));
        Add("hideout.json|overrideBuildTimeSeconds", O("覆写所有建造时间（秒）", NEG1, -1, 864000, unit: "秒"));
        Add("hideout.json|craftingExpAmount", O("每次制造基础经验", null, 0, 500));
        Add("hideout.json|craftingExpForHoursOfCrafting", O("每满一小时的额外经验", null, 0, 200));
        Add("hideout.json|hoursForSkillCrafting", O("技能制造计时（小时）", null, 0, 100000));
        Add("hideout.json|runIntervalValues", O("容器轮询间隔", "藏身处产出检查的频率：inRaid 是在战局里时，outOfRaid 是平时。"));
        Add("hideout.json|runIntervalValues.inRaid", O("战局内容器轮询间隔（秒）", "影响藏身处箱子在战局内的产出检查频率。", 1, 3600));
        Add("hideout.json|runIntervalValues.outOfRaid", O("藏身处容器轮询间隔（秒）", null, 1, 3600));
        Add("hideout.json|updateProfileHideoutWhenActiveWithinMinutes", O("活跃判定（分钟）", null, 1, 1440));
        Add("hideout.json|cultistCircle.craftTimeOverride", O("供奉圈制作时间覆写（秒）", NEG1, -1, 864000));
        Add("hideout.json|cultistCircle.maxRewardItemCount", O("供奉单次最大奖励物品数", null, 1, 50));
        Add("hideout.json|cultistCircle.hideoutCraftSacrificeThresholdRub", O("可供奉物品价值门槛（₽）", null, 0, 50_000_000));
        Add("hideout.json|cultistCircle.highValueThresholdRub", O("高价值判定阈值（₽）", null, 0, 50_000_000));
        Add("hideout.json|cultistCircle.bonusChanceMultiplier", O("额外奖励概率倍率", null, 0, 20));
        Add("hideout.json|cultistCircle.rewardPriceMultiplierMinMax", O("奖励价格倍率区间", null));
        // ---- airdrop ----
        Add("airdrop.json|airdropTypeWeightings", O("空投类型权重", "相对权重，0 表示该类型不会出现。填大数字即可提高概率。", 0, 10000));
        Add("airdrop.json|loot.mixed.allowBossItems", O("混合空投允许 Boss 物品", null));
        Add("airdrop.json|loot.weaponArmor.allowBossItems", O("武器护甲空投允许 Boss 物品", null));
        Add("airdrop.json|loot.barter.allowBossItems", O("以物换物空投允许 Boss 物品", null));
        Add("airdrop.json|loot.foodMedical.allowBossItems", O("食品医疗空投允许 Boss 物品", null));
        Add("airdrop.json|loot.mixed.armorLevelWhitelist", O("混合空投护甲等级白名单", "0~6，填 6 可出最高级护甲。"));
        Add("airdrop.json|loot.weaponArmor.armorLevelWhitelist", O("武器护甲空投护甲等级白名单", "0~6。"));
        Add("airdrop.json|loot.mixed.useRewardItemBlacklist", O("套用奖励物品黑名单", null));
        Add("airdrop.json|loot.weaponArmor.useRewardItemBlacklist", O("套用奖励物品黑名单", null));
        // ---- inraid ----
        Add("inraid.json|raidMenuSettings.aiAmount", O("AI 数量", null, null, null, ["AsOnline", "AsNoBots", "AsPlayerPlayerScav", "Low", "Medium", "High", "None"]));
        Add("inraid.json|raidMenuSettings.aiDifficulty", O("AI 难度", null, null, null, ["AsOnline", "Easy", "Normal", "Hard", "Impossible"]));
        Add("inraid.json|raidMenuSettings.bossEnabled", O("生成 Boss", null));
        Add("inraid.json|raidMenuSettings.scavWars", O("Scav 混战", null));
        Add("inraid.json|raidMenuSettings.enablePve", O("启用 PvE 模式", null));
        Add("inraid.json|raidMenuSettings.taggedAndCursed", O("标记与诅咒", null));
        Add("inraid.json|playerScavHostileChancePercent", O("玩家 Scav 敌对概率（%）", null, 0, 100));
        Add("inraid.json|keepFiRSecureContainerOnDeath", O("死亡保留保险箱内 FiR", null));
        Add("inraid.json|alwaysKeepFoundInRaidOnRaidEnd", O("战局结束保留 FiR", null));
        Add("inraid.json|carExtractBaseStandingGain", O("载具撤离基础声望", null, 0, 1));
        Add("inraid.json|coopExtractBaseStandingGain", O("协作撤离基础声望", null, 0, 1));
        // ---- lost on death ----
        Add("lostondeath.json|questItems", O("死亡掉落任务物品", "关闭可保护任务进度。"));
        Add("lostondeath.json|specialSlotItems", O("死亡掉落特殊插槽物品", null));
        Add("lostondeath.json|wipeOnRaidStart", O("开局清空气球箱", null));
        // ---- insurance / scavcase / health ----
        Add("insurance.json|returnTimeOverrideSeconds", O("保险返还时间覆写（秒）", "0 表示不覆写。", 0, 864000));
        Add("insurance.json|storageTimeOverrideSeconds", O("保险保存时长覆写（秒）", "0 表示不覆写。", 0, 864000));
        Add("insurance.json|runIntervalSeconds", O("保险轮询间隔（秒）", null, 10, 86400));
        Add("insurance.json|simulateItemsBeingTaken", O("模拟附件被拿走", null));
        Add("scavcase.json|allowBossItemsAsRewards", O("允许 Boss 物品作为奖励", null));
        Add("scavcase.json|allowMultipleMoneyRewardsPerRarity", O("同稀有度可出多笔现金", null));
        Add("health.json|healthMultipliers.blacked", O("倒地阈值倍率", null, 0, 5));
        Add("health.json|healthMultipliers.death", O("致死阈值倍率", null, 0, 5));
        // ---- traders / ragfair / repair ----
        Add("trader.json|traderPriceMultiplier", O("商人价格倍率", null, 0.1, 10));
        Add("trader.json|tradersResetFromServerStart", O("商人刷新从服务端启动计时", null));
        Add("trader.json|purchasesAreFoundInRaid", O("商人购买物品记为 FiR", null));
        Add("ragfair.json|offerListingTaxMultiplier", O("跳蚤市场税费倍率", null, 0, 5));
        Add("repair.json|priceMultiplier", O("维修价格倍率", null, 0, 20));
        // ---- bots ----
        Add("bot.json|maxBotCap", O("机器人总上限", null, 0, 500));
        Add("bot.json|botNameLengthLimit", O("机器人名长度上限", null, 1, 40));

        return d;
    }

    private static FieldMetaOverride O(string? label, string? hint = null, double? min = null, double? max = null,
        string[]? @enum = null, string? unit = null) =>
        new(label, hint, unit, min, max, @enum);

    public static void Apply(FieldNode node)
    {
        var file = node.File;
        if (string.IsNullOrEmpty(file)) return;

        // Every row gets a Chinese label, and FieldRenderer still prints the raw key beneath it as the 对照.
        if (!node.IsDocumentRoot && node.Label == node.Key)
        {
            var label = KeyLabels.TryGetValue(node.Key, out var kl) ? kl : Factory.Gloss(node.Key).label;
            if (!string.IsNullOrWhiteSpace(label)) node.Label = label;
        }

        if (!Overrides.TryGetValue(file + "|" + node.Path, out var ov)) return;
        if (ov.Label is not null) node.Label = ov.Label;
        if (ov.Hint is not null) node.Hint = ov.Hint;
        if (ov.Unit is not null) node.Unit = ov.Unit;
        if (ov.Min is not null) node.Min = ov.Min;
        if (ov.Max is not null) node.Max = ov.Max;
        if (ov.Enum is not null)
        {
            node.EnumOptions = ov.Enum;
            node.Kind = FieldKind.Enum;
        }
        if (ov.Kind is not null) node.Kind = ov.Kind.Value;
    }

    public static FileMeta Meta(string fileName) =>
        Files.TryGetValue(fileName, out var m) ? m : new FileMeta(fileName, "其他", "");

    /// <summary>Curated dashboard. Paths must exist in the shipped configs; missing ones are skipped at render.</summary>
    public static readonly QuickGroup[] QuickGroups =
    [
        new("藏身处", "制造 / 建造时间与供奉圈", [
            Q("hideout.json", "overrideCraftTimeSeconds"),
            Q("hideout.json", "overrideBuildTimeSeconds"),
            Q("hideout.json", "craftingExpAmount"),
            Q("hideout.json", "craftingExpForHoursOfCrafting"),
            Q("hideout.json", "runIntervalValues"),
            Q("hideout.json", "cultistCircle.craftTimeOverride"),
            Q("hideout.json", "cultistCircle.maxRewardItemCount"),
            Q("hideout.json", "cultistCircle.hideoutCraftSacrificeThresholdRub"),
            Q("hideout.json", "cultistCircle.highValueThresholdRub"),
            Q("hideout.json", "cultistCircle.bonusChanceMultiplier"),
            Q("hideout.json", "cultistCircle.rewardPriceMultiplierMinMax"),
            Q("hideout.json", "cultistCircle.currencyRewards"),
            Q("hideout.json", "cultistCircle.rewardItemBlacklist")
        ]),
        new("空投", "类型权重与内容数量", [
            Q("airdrop.json", "airdropTypeWeightings"),
            Q("airdrop.json", "loot.mixed.weaponPresetCount"),
            Q("airdrop.json", "loot.mixed.armorPresetCount"),
            Q("airdrop.json", "loot.mixed.itemCount"),
            Q("airdrop.json", "loot.mixed.armorLevelWhitelist"),
            Q("airdrop.json", "loot.mixed.allowBossItems"),
            Q("airdrop.json", "loot.mixed.itemBlacklist"),
            Q("airdrop.json", "loot.mixed.itemLimits"),
            Q("airdrop.json", "loot.mixed.itemStackLimits"),
            Q("airdrop.json", "loot.weaponArmor.itemCount"),
            Q("airdrop.json", "loot.weaponArmor.armorLevelWhitelist"),
            Q("airdrop.json", "loot.weaponArmor.allowBossItems"),
            Q("airdrop.json", "customAirdropMapping")
        ]),
        new("战局规则", "AI 默认值、撤离与 FiR", [
            Q("inraid.json", "raidMenuSettings"),
            Q("inraid.json", "playerScavHostileChancePercent"),
            Q("inraid.json", "keepFiRSecureContainerOnDeath"),
            Q("inraid.json", "alwaysKeepFoundInRaidOnRaidEnd"),
            Q("inraid.json", "carExtractBaseStandingGain"),
            Q("inraid.json", "coopExtractBaseStandingGain"),
            Q("match.json", "enabled")
        ]),
        new("死亡与保险", "掉落槽位、保险返还", [
            Q("lostondeath.json", "equipment"),
            Q("lostondeath.json", "questItems"),
            Q("lostondeath.json", "specialSlotItems"),
            Q("insurance.json", "returnChancePercent"),
            Q("insurance.json", "returnTimeOverrideSeconds"),
            Q("insurance.json", "storageTimeOverrideSeconds"),
            Q("insurance.json", "simulateItemsBeingTaken")
        ]),
        new("奖励与经济", "奖励箱、商人、跳蚤、维修", [
            Q("scavcase.json", "rewardItemValueRangeRub"),
            Q("scavcase.json", "moneyRewards.moneyRewardChancePercent"),
            Q("scavcase.json", "allowBossItemsAsRewards"),
            Q("scavcase.json", "allowMultipleMoneyRewardsPerRarity"),
            Q("trader.json", "traderPriceMultiplier"),
            Q("trader.json", "purchasesAreFoundInRaid"),
            Q("ragfair.json", "offerListingTaxMultiplier"),
            Q("repair.json", "priceMultiplier"),
            Q("health.json", "healthMultipliers")
        ]),
        new("机器人与世界", "上限、天气与地图倍率", [
            Q("bot.json", "maxBotCap"),
            Q("bot.json", "showTypeInNickname"),
            Q("weather.json", "acceleration"),
            Q("weather.json", "overrideSeason"),
            Q("location.json", "looseLootMultiplier"),
            Q("location.json", "staticLootMultiplier"),
            Q("location.json", "containerRandomisationSettings")
        ]),
        new("服务端与面板", "名称、网络、备份、日志", [
            Q("core.json", "serverName"),
            Q("core.json", "profileSaveIntervalSeconds"),
            Q("core.json", "features.compressProfile"),
            Q("core.json", "allowProfileWipe"),
            Q("http.json", "ip"),
            Q("http.json", "port"),
            Q("http.json", "logRequests"),
            Q("http.json", "webAuthenticationConfig"),
            Q("backup.json", "enabled"),
            Q("backup.json", "maxBackups"),
            Q("backup.json", "backupInterval"),
            Q("locale.json", "gameLocale")
        ])
    ];

    private static QuickEntry Q(string file, string path) => new(file, path);

    public sealed record Located(JsonNode Container, string Key, int Index, JsonNode? Value, string Path);
    /// <summary>Resolves a path into its owning container plus the final key or array index.</summary>
    public static Located? Locate(JsonNode root, string path)
    {
        var tokens = Tokenise(path).ToList();
        if (tokens.Count == 0) return null;

        var container = root;
        var walked = new System.Text.StringBuilder();
        for (var i = 0; i < tokens.Count - 1; i++)
        {
            var t = tokens[i];
            if (container is null) return null;
            if (t.Index >= 0)
            {
                if (container is not JsonArray arr || t.Index >= arr.Count) return null;
                container = arr[t.Index];
                walked.Append('[').Append(t.Index).Append(']');
            }
            else
            {
                if (container is not JsonObject obj || !obj.TryGetPropertyValue(t.Name!, out var next)) return null;
                container = next;
                walked.Append(walked.Length == 0 ? t.Name! : "." + t.Name);
            }
        }

        var last = tokens[^1];
        var finalPath = path;
        if (last.Index >= 0)
        {
            if (container is not JsonArray a || last.Index >= a.Count) return null;
            return new Located(a, last.Index.ToString(), last.Index, a[last.Index], finalPath);
        }

        if (container is not JsonObject o || !o.TryGetPropertyValue(last.Name!, out var value)) return null;
        return new Located(o, last.Name!, -1, value, finalPath);
    }

    /// <summary>Locates the DOM node for a dotted/bracketed path. Null when the config moved on.</summary>
    public static JsonNode? Resolve(JsonNode root, string path)
    {
        var cur = root;
        foreach (var token in Tokenise(path))
        {
            if (cur is null) return null;
            if (token.Index >= 0)
            {
                if (cur is not JsonArray arr || token.Index >= arr.Count) return null;
                cur = arr[token.Index];
            }
            else
            {
                if (cur is not JsonObject obj || !obj.TryGetPropertyValue(token.Name!, out var next)) return null;
                cur = next;
            }
        }
        return cur;
    }

    private readonly record struct Token(string? Name, int Index);

    private static IEnumerable<Token> Tokenise(string path)
    {
        var buf = new System.Text.StringBuilder();
        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '.') { if (buf.Length > 0) { yield return new Token(buf.ToString(), -1); buf.Clear(); } }
            else if (c == '[')
            {
                if (buf.Length > 0) { yield return new Token(buf.ToString(), -1); buf.Clear(); }
                var j = path.IndexOf(']', i);
                var digits = path[(i + 1)..j];
                yield return new Token(null, int.Parse(digits));
                i = j;
            }
            else buf.Append(c);
        }
        if (buf.Length > 0) yield return new Token(buf.ToString(), -1);
    }
}
