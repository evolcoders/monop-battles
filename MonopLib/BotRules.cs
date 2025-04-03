namespace MonopLib;

public class BotRules
{
    public BotRules()
    {
        DataUtil.InitBotRules(this);
    }

    public List<ARule> AuctionRules { get; set; } = new(100);
    public List<TRule> TradeRules { get; set; } = new(30);
    public List<TRule> PlayerTradeRules(int pid) => TradeRules;

}
