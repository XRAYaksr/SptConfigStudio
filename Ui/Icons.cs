using System.Text;
using SptConfigStudio.Core;
using Wpf.Ui.Controls;

namespace SptConfigStudio.Views;

/// <summary>Icon choices for navigation entries and group headers. Kept on this side on purpose: putting a
/// SymbolRegular into Core's FileMeta would drag Wpf.Ui into the JSON/IO layer.</summary>
public static class Icons
{
    private static readonly Dictionary<string, SymbolRegular> Files = new(StringComparer.OrdinalIgnoreCase)
    {
        ["quick"] = SymbolRegular.Gauge24,
        ["core.json"] = SymbolRegular.Server24,
        ["http.json"] = SymbolRegular.NetworkCheck24,
        ["locale.json"] = SymbolRegular.Translate24,
        ["backup.json"] = SymbolRegular.History24,
        ["match.json"] = SymbolRegular.PeopleCommunity24,
        ["hideout.json"] = SymbolRegular.Home24,
        ["airdrop.json"] = SymbolRegular.Airplane24,
        ["inraid.json"] = SymbolRegular.Flag24,
        ["lostondeath.json"] = SymbolRegular.Drop24,
        ["insurance.json"] = SymbolRegular.ShieldCheckmark24,
        ["scavcase.json"] = SymbolRegular.BoxMultiple24,
        ["health.json"] = SymbolRegular.HeartPulse24,
        ["repair.json"] = SymbolRegular.Wrench24,
        ["ragfair.json"] = SymbolRegular.Cart24,
        ["trader.json"] = SymbolRegular.BuildingShop24,
        ["quest.json"] = SymbolRegular.ClipboardTask24,
        ["gifts.json"] = SymbolRegular.Gift24,
        ["weather.json"] = SymbolRegular.WeatherPartlyCloudyDay24,
        ["location.json"] = SymbolRegular.Map24,
        ["bot.json"] = SymbolRegular.Bot24,
        ["pmc.json"] = SymbolRegular.PersonAdd24,
        ["playerscav.json"] = SymbolRegular.Scales24,
        ["pmcchatresponse.json"] = SymbolRegular.ChatMultiple24,
        ["btrdelivery.json"] = SymbolRegular.VehicleTruck24,
        ["item.json"] = SymbolRegular.Tag24,
        ["inventory.json"] = SymbolRegular.Backpack24,
        ["loot.json"] = SymbolRegular.Diamond24,
        ["seasonalevents.json"] = SymbolRegular.CalendarSparkle24,
        ["backup"] = SymbolRegular.Vault24,
        ["appearance"] = SymbolRegular.ColorFill24,
        ["wizard"] = SymbolRegular.Lightbulb24
    };

    private static readonly Dictionary<string, SymbolRegular> Categories = new(StringComparer.OrdinalIgnoreCase)
    {
        ["基础"] = SymbolRegular.Window24,
        ["玩法"] = SymbolRegular.PlayCircle24,
        ["世界"] = SymbolRegular.Globe24,
        ["高级"] = SymbolRegular.Options24,
        ["其他"] = SymbolRegular.AppsList24,
        ["工具"] = SymbolRegular.Toolbox24
    };

    /// <summary>
    /// Field rows match the Chinese label first, because it is assembled from MetadataCatalog.Words and the
    /// vocabulary is therefore shared. Money leads: 「藏身处制造供奉阈值（卢布）」is about a price, not about the hideout.
    /// </summary>
    private static readonly (string Zh, SymbolRegular Icon)[] ZhRules =
    [
        ("税费", SymbolRegular.ReceiptMoney24),
        ("卢布", SymbolRegular.Money24), ("价格", SymbolRegular.Money24), ("花费", SymbolRegular.Money24),
        ("费用", SymbolRegular.Money24), ("货币", SymbolRegular.Money24), ("现金", SymbolRegular.Money24),
        ("出售", SymbolRegular.MoneyHand24), ("购买", SymbolRegular.Cart24), ("商品", SymbolRegular.ShoppingBagTag24),
        ("报价", SymbolRegular.ShoppingBagTag24), ("上架", SymbolRegular.ShoppingBagTag24),
        ("百分比", SymbolRegular.TagPercent24), ("概率", SymbolRegular.TargetArrow24), ("倍率", SymbolRegular.ArrowTrending24),
        ("数量", SymbolRegular.NumberSymbol24),
        ("空投", SymbolRegular.Airplane24), ("藏身处", SymbolRegular.Home24), ("保险箱", SymbolRegular.BoxCheckmark24),
        ("保险", SymbolRegular.ShieldCheckmark24), ("背包", SymbolRegular.Backpack24), ("仓库", SymbolRegular.Archive24),
        ("商人", SymbolRegular.BuildingShop24), ("跳蚤市场", SymbolRegular.Cart24), ("任务", SymbolRegular.ClipboardTask24),
        ("礼物", SymbolRegular.Gift24), ("奖励", SymbolRegular.Gift24),
        ("制造", SymbolRegular.BuildingFactory24), ("建造", SymbolRegular.BuildingFactory24),
        ("维修", SymbolRegular.Wrench24), ("耐久", SymbolRegular.Wrench24),
        ("治疗", SymbolRegular.HeartPulse24), ("医疗", SymbolRegular.HeartPulse24), ("健康", SymbolRegular.HeartPulse24),
        ("伤害", SymbolRegular.Fire24), ("死亡", SymbolRegular.HeartBroken24), ("致死", SymbolRegular.HeartBroken24),
        ("尸体", SymbolRegular.HeartBroken24), ("机器人", SymbolRegular.Bot24), ("武器", SymbolRegular.Target24),
        ("护甲", SymbolRegular.Shield24), ("防护", SymbolRegular.Shield24), ("弹匣", SymbolRegular.Target24),
        ("战利品", SymbolRegular.Diamond24), ("散落", SymbolRegular.Diamond24), ("物品", SymbolRegular.Cube24),
        ("容器", SymbolRegular.Box24), ("箱子", SymbolRegular.Box24), ("撤离", SymbolRegular.ArrowExit24),
        ("生成", SymbolRegular.Sparkle24), ("刷新", SymbolRegular.ArrowSync24), ("波次", SymbolRegular.NumberRow24),
        ("冷却", SymbolRegular.Timer24), ("延迟", SymbolRegular.Timer24), ("间隔", SymbolRegular.Timer24),
        ("超时", SymbolRegular.Timer24),
        ("季节", SymbolRegular.CalendarSparkle24), ("活动", SymbolRegular.CalendarSparkle24),
        ("每日", SymbolRegular.CalendarSparkle24), ("天气", SymbolRegular.WeatherPartlyCloudyDay24),
        ("雨", SymbolRegular.WeatherRain24), ("雪", SymbolRegular.WeatherSnow24), ("雾", SymbolRegular.WeatherFog24),
        ("温度", SymbolRegular.Temperature24), ("夜晚", SymbolRegular.WeatherMoon24), ("白天", SymbolRegular.WeatherSunny24),
        ("地图", SymbolRegular.Location24), ("区域", SymbolRegular.Location24), ("端口", SymbolRegular.PlugConnected24),
        ("网络", SymbolRegular.NetworkCheck24), ("日志", SymbolRegular.Notepad24), ("语言", SymbolRegular.Translate24),
        ("存档", SymbolRegular.Save24), ("备份", SymbolRegular.History24), ("载具", SymbolRegular.VehicleTruck24),
        ("邮件", SymbolRegular.Mail24), ("图片", SymbolRegular.Image24), ("声望", SymbolRegular.Scales24),
        ("权重", SymbolRegular.Scales24), ("经验", SymbolRegular.Trophy24), ("等级", SymbolRegular.Trophy24),
        ("技能", SymbolRegular.Star24), ("玩家", SymbolRegular.People24), ("平民", SymbolRegular.PeopleCommunity24),
        ("Boss", SymbolRegular.Crown24), ("时长", SymbolRegular.Clock24), ("半径", SymbolRegular.Circle24),
        ("大小", SymbolRegular.Resize24), ("距离", SymbolRegular.Ruler24), ("速度", SymbolRegular.Gauge24),
        ("启用", SymbolRegular.Power24), ("禁用", SymbolRegular.Power24), ("屏蔽", SymbolRegular.ShieldProhibited24)
    ];

    /// <summary>Fallback for keys whose label stayed untranslated. Words of four letters or fewer must match a
    /// whole token, so "exp" never fires on "expect" and "log" never fires on "Login".</summary>
    private static readonly (string En, SymbolRegular Icon)[] EnRules =
    [
        ("rub", SymbolRegular.Money24), ("rubles", SymbolRegular.Money24), ("roubles", SymbolRegular.Money24),
        ("price", SymbolRegular.Money24), ("cost", SymbolRegular.Money24), ("money", SymbolRegular.Money24),
        ("currency", SymbolRegular.Money24), ("coins", SymbolRegular.Money24),
        ("chance", SymbolRegular.TargetArrow24), ("percent", SymbolRegular.TagPercent24),
        ("probability", SymbolRegular.TargetArrow24), ("multiplier", SymbolRegular.ArrowTrending24),
        ("scaling", SymbolRegular.ArrowTrending24), ("count", SymbolRegular.NumberSymbol24),
        ("amount", SymbolRegular.NumberSymbol24),
        ("airdrop", SymbolRegular.Airplane24), ("hideout", SymbolRegular.Home24),
        ("insurance", SymbolRegular.ShieldCheckmark24), ("backpack", SymbolRegular.Backpack24),
        ("inventory", SymbolRegular.Backpack24), ("stash", SymbolRegular.Archive24),
        ("trader", SymbolRegular.BuildingShop24), ("ragfair", SymbolRegular.Cart24),
        ("quest", SymbolRegular.ClipboardTask24), ("reward", SymbolRegular.Gift24), ("craft", SymbolRegular.BuildingFactory24),
        ("repair", SymbolRegular.Wrench24), ("durability", SymbolRegular.Wrench24),
        ("heal", SymbolRegular.HeartPulse24), ("health", SymbolRegular.HeartPulse24), ("medical", SymbolRegular.HeartPulse24),
        ("damage", SymbolRegular.Fire24), ("death", SymbolRegular.HeartBroken24), ("bot", SymbolRegular.Bot24),
        ("bots", SymbolRegular.Bot24),
        ("weapon", SymbolRegular.Target24), ("armor", SymbolRegular.Shield24), ("protection", SymbolRegular.Shield24),
        ("loot", SymbolRegular.Diamond24), ("container", SymbolRegular.Box24), ("extract", SymbolRegular.ArrowExit24),
        ("spawn", SymbolRegular.Sparkle24), ("cooldown", SymbolRegular.Timer24), ("delay", SymbolRegular.Timer24),
        ("interval", SymbolRegular.Timer24), ("timeout", SymbolRegular.Timer24), ("season", SymbolRegular.CalendarSparkle24),
        ("weather", SymbolRegular.WeatherPartlyCloudyDay24), ("rain", SymbolRegular.WeatherRain24),
        ("snow", SymbolRegular.WeatherSnow24), ("fog", SymbolRegular.WeatherFog24), ("temperature", SymbolRegular.Temperature24),
        ("night", SymbolRegular.WeatherMoon24), ("location", SymbolRegular.Location24), ("port", SymbolRegular.PlugConnected24),
        ("network", SymbolRegular.NetworkCheck24), ("logging", SymbolRegular.Notepad24), ("locale", SymbolRegular.Translate24),
        ("language", SymbolRegular.Translate24), ("profile", SymbolRegular.Save24), ("backup", SymbolRegular.History24),
        ("vehicle", SymbolRegular.VehicleTruck24), ("mail", SymbolRegular.Mail24), ("image", SymbolRegular.Image24),
        ("karma", SymbolRegular.Scales24), ("standing", SymbolRegular.Scales24), ("reputation", SymbolRegular.Scales24),
        ("weight", SymbolRegular.Scales24), ("experience", SymbolRegular.Trophy24), ("level", SymbolRegular.Trophy24),
        ("skill", SymbolRegular.Star24), ("player", SymbolRegular.People24), ("civilian", SymbolRegular.PeopleCommunity24),
        ("boss", SymbolRegular.Crown24), ("duration", SymbolRegular.Clock24), ("radius", SymbolRegular.Circle24),
        ("distance", SymbolRegular.Ruler24), ("speed", SymbolRegular.Gauge24), ("server", SymbolRegular.Server24),
        ("id", SymbolRegular.Tag24), ("ids", SymbolRegular.Tag24)
    ];

    public static SymbolIcon ForFile(string key) => new() { Symbol = Files.TryGetValue(key, out var s) ? s : SymbolRegular.Options24 };

    public static SymbolIcon ForCategory(string category) => new() { Symbol = Categories.TryGetValue(category, out var s) ? s : SymbolRegular.AppsList24 };

    /// <summary>Row icon, or null so the Auto column collapses and no empty slot is left behind.
    /// Booleans opt out on purpose: the switch itself is the affordance.</summary>
    public static SymbolIcon? ForField(FieldNode n)
    {
        if (n.Kind == FieldKind.Toggle) return null;
        foreach (var (zh, icon) in ZhRules)
            if (n.Label.Contains(zh, StringComparison.OrdinalIgnoreCase)) return Glyph(icon);

        var words = Tokens(n.Key);
        foreach (var (en, icon) in EnRules)
            foreach (var w in words)
            {
                var hit = en.Length <= 4
                    ? string.Equals(w, en, StringComparison.OrdinalIgnoreCase)
                    : w.Contains(en, StringComparison.OrdinalIgnoreCase);
                if (hit) return Glyph(icon);
            }
        return null;
    }

    private static SymbolIcon Glyph(SymbolRegular s) => new() { Symbol = s, FontSize = 20 };

    /// <summary>camelCase / snake_case splitter; digits stay with their word so "tpl1" never becomes "tpl".</summary>
    private static List<string> Tokens(string key)
    {
        var words = new List<string>();
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            if (c is '_' or '.' or '-' or ' ' or '/')
            {
                if (sb.Length > 0) { words.Add(sb.ToString()); sb.Clear(); }
                continue;
            }
            if (char.IsUpper(c) && sb.Length > 0 && i > 0 && !char.IsUpper(key[i - 1]))
            {
                words.Add(sb.ToString());
                sb.Clear();
            }
            sb.Append(c);
        }
        if (sb.Length > 0) words.Add(sb.ToString());
        return words;
    }
}
