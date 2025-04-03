namespace MonopLib.BotBrains;

public class TradeLogic
{
    public static bool TryDoTrade(Game g)
    {
        var validBotTrades = GetValidTrades(g, g.CurrPlayer);
        TradeBox found = null;
        foreach (var trade in validBotTrades)
        {
            var isRejected = g.RejectedTrades.Any(rejectedTrade => rejectedTrade == trade);
            if (!isRejected)
            {
                found = trade;
                break;
            }
        }
        if (found != null)
        {
            g.CurrTradeBox = found;
            g.State = GameState.Trade;
            return true;
        }
        return false;
    }

    public static List<TradeBox> GetValidTrades(Game g, Player pl)
    {
        List<TradeBox> result = new(10);
        foreach (var trule in g.BotRules.PlayerTradeRules(pl.Id))
        {
            var trade = FindTradeByTradeRule(g, trule, pl.Id);
            if (trade == null)
            {
                trade = FindTradeByTradeRule(g, ReverseRule(trule), pl.Id);
                if (trade != null)
                    trade.Reversed = true;
            }
            if (trade != null)
            {
                result.Add(trade);
            }
        }
        return result;
    }

    private static TradeBox FindTradeByTradeRule(Game g, TRule trule, int my)
    {
        var myPl = g.FindPlayerBy(my);
        var playerGroups = g.Map.PlayerCellGroups;
        for (int anPid = 1; anPid < 5; anPid++)
        {
            if (anPid == my) continue;
            if (playerGroups[anPid][trule.getLand] != trule.getCount) continue;
            var anPlayer = g.FindPlayerBy(anPid);
            // i have
            bool myCount = playerGroups[my][trule.getLand] == trule.myCount;
            bool yourCount = playerGroups[anPid][trule.giveLand] == trule.yourCount;
            //i give you
            var giveCells = g.Map.CellsByUserByGroup(my, trule.giveLand);
            //money factor
            var myMoney = g.CalcPlayerAssets(my, false);
            var anMoney = g.CalcPlayerAssets(anPid, false);
            bool mfac = ((double)myMoney / anMoney) >= trule.moneyFactor;

            if (giveCells.Length == trule.giveCount && myCount && yourCount && mfac)
                return new TradeBox()
                {
                    From      = myPl,
                    GiveCells = giveCells.Select(c => c.Id).ToList(),
                    GiveMoney = trule.giveCount,
                    To        = anPlayer,
                    GetCells  = g.Map.CellsByUserByGroup(anPid, trule.getLand).Select(c => c.Id).ToList(),
                    GetMoney  = trule.getMoney,
                    Id        = trule.id,
                };
        }
        return null;
    }

    private static TRule ReverseRule(TRule trule)
    {
        TRule reversed = new()
        {
            id          = trule.id,
            myCount     = trule.yourCount,
            yourCount   = trule.myCount,
            getCount    = trule.giveCount,
            getLand     = trule.giveLand,
            getMoney    = trule.giveMoney,
            giveCount   = trule.getCount,
            giveLand    = trule.getLand,
            giveMoney   = trule.getMoney,
            moneyFactor = 1 / trule.moneyFactor,
        };
        return reversed;
    }
}
