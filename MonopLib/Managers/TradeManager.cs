using MonopLib.BotBrains;

namespace MonopLib.Managers;

public class TradeManager
{

    public static void RunTradeJob(Game g)
    {
        var trade = g.CurrTradeBox;
        if (trade.From.IsBot && trade.To.IsBot)
            CompleteTrade(g);
        if (trade.From.IsHuman && trade.To.IsBot)
            TradeBetweenHumanAndBot(g);
    }

    public static void CompleteTrade(Game g)
    {
        var tr = g.CurrTradeBox;
        g.CompletedTrades.Add(tr);

        tr.GiveCells.ForEach(c => g.Map.Cells[c].Owner = tr.To.Id);
        tr.GetCells.ForEach(c => g.Map.Cells[c].Owner = tr.From.Id);
        tr.From.Money += tr.GetMoney - tr.GiveMoney;
        tr.To.Money += tr.GiveMoney - tr.GetMoney;

        g.Map.UpdateCellsGroupInfo();
        LogTrade("_trade_completed", g, tr);
        g.ToBeginState();
    }

    public static void AddToRejectedTrades(Game g)
    {
        var tr = g.CurrTradeBox;
        g.RejectedTrades.Add(tr);
        LogTrade("_trade_rejected", g, tr);
        g.ToBeginState();
    }

    private static bool TradeBetweenHumanAndBot(Game g)
    {
        var currTrade = g.CurrTradeBox;
        var validBotTrades = TradeLogic.GetValidTrades(g, currTrade.To);
        if (PlayerApprovedTrade(currTrade, validBotTrades))
        {
            CompleteTrade(g);
            LogTrade("_trade_completed", g, currTrade);
            return true;
        }
        else
            LogTrade("_trade_rejected", g, currTrade);

        g.ToBeginState();
        return false;
    }

    private static bool PlayerApprovedTrade(TradeBox currTrade, List<TradeBox> validBotTrades)
    {
        foreach (var botTrade in validBotTrades)
        {
            if (botTrade.To.Id == currTrade.From.Id &&
             Enumerable.SequenceEqual(botTrade.GetCells, currTrade.GetCells)
             && currTrade.GiveMoney >= botTrade.GiveMoney
             && currTrade.GetMoney <= botTrade.GetMoney)
            {
                return true;
            }
        }
        return false;
    }

    static void LogTrade(string key, Game g, TradeBox tr)
    {
        var text = string.Format(TextRes.Get(g.UILanguage, key), tr.From.Name, tr.To.Name,
            string.Join(',', tr.GiveCells), string.Join(',', tr.GetCells));
        g.RoundLog(text);
    }

}
