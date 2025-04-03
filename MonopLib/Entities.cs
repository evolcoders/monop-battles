namespace MonopLib;

public class GameConfig
{
    public bool IsManualRollMode { get; set; }
    public bool NeedShowLog { get; set; }
    public bool ConfirmRoundEnding { get; set; }
    public bool IsConsole { get; set; } = false;
    public int UpdateInterval { get; set; } = 1000; // in millisecunds
}

public enum GameState
{
    Start,
    BeginStep,
    CanBuy,
    Auction,
    Trade,
    NeedPay,
    NeedPayAndContinue,
    CantPay,
    EndStep,
    MoveToCell,
    RandomCell,
    FinishGame,
    RunBuildOrTrade,
}

public class Cell
{
    public int Id { get; set; }
    public int Cost { get; set; }
    public int Type { get; set; }
    public int Group { get; set; }
    public int? Owner { get; set; }
    public int OwnerGroupCount { get; set; }
    public string Title { get; set; }
    public string RentInfo { get; set; }
    public string Info { get; set; }
    public int HousesCount { get; set; } = 0;
    public bool IsMortgage { get; set; }

    public bool IsMonopoly => Group switch
    {
        (> 1) and (< 8) => OwnerGroupCount == 3,
        1 or 8 => OwnerGroupCount == 2,
        _ => false
    };

    static int CheckIndex(int idx) => idx < 0 ? 0 : idx;
    public int NeedPay(int index) => Convert.ToInt32(RentInfo.Split(';')[CheckIndex(index)]);
    public bool Land => Type == 1 || Type == 2 || Type == 3;
    public bool IsActive => !IsMortgage;
    public bool CanBuild => HousesCount < 5;
    public int HouseCostWhenSell => HouseCost / 2;
    public int MortgageAmount => Cost / 2;
    public int UnMortgageAmount => Convert.ToInt32(Cost * 0.55); // MortgageAmount * 1.1
    public int HouseCost => Group switch
    {
        1 or 2 => 500,
        3 or 4 => 1000,
        5 or 6 => 1500,
        7 or 8 => 2000,
        _ => 0
    };

    public int Rent()
    {
        if (Type == 6) return NeedPay(0);
        if (Group == 9) return NeedPay(OwnerGroupCount - 1);
        if (Group == 10) return OwnerGroupCount == 2 ? 500 : NeedPay(0);
        if (IsMonopoly && HousesCount == 0)
            return NeedPay(0) * 2;
        else
            return NeedPay(HousesCount);
    }
}

public enum PlayerType
{
    Bot,
    Human
}

public enum PlayerAction
{
    BuySuccessfully,
    MortgageAndBuy,
    AuctionAndBuy,
    MortgageCells,
    Pay,
    MortgageAndPay,
    BuildHouses,
    SellHouses,
    AfterRandomCard,
    SkipStep,
    RollAndGo,
    TripleDouble,
    Pay500AndGo,

}

public class Player
{
    public Player(int id, string name, PlayerType pType, int money) =>
        (Id, Name, IsBot, Money) = (id, name, pType == PlayerType.Bot, money);

    public bool IsHuman => !IsBot;

    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsBot { get; set; }
    public bool Deleted { get; set; }
    public int Money { get; set; }
    public int Pos { get; set; }
    public int LastRoll { get; set; }
    public int ManualRoll { get; set; }
    public int Police { get; set; }
    public int PoliceKey { get; set; }
    public int DoubleRoll { get; set; }

    internal void UpdateTimer()
    {
    }
    public void MoveToJail()
    {
        Pos = 10;
        Police = 1;
    }
}

public class TradeBox
{
    public int Id { get; set; }
    public bool Reversed { get; set; }
    public Player From { get; set; }
    public Player To { get; set; }
    public List<int> GiveCells { get; set; }
    public List<int> GetCells { get; set; }
    public int GiveMoney { get; set; }
    public int GetMoney { get; set; }
    public bool Equals(TradeBox an)
    {
        if (an is null)
            return false;
        bool pls = From.Id == an.From.Id && To.Id == an.To.Id;
        bool land1 = Enumerable.SequenceEqual(GiveCells, an.GiveCells);
        bool land2 = Enumerable.SequenceEqual(GetCells, an.GetCells);
        bool money = GetMoney == an.GetMoney;
        return pls && land1 && land2 && money;
    }
}

public class Auction
{
    public Cell Cell { get; set; }
    public int CurrBid { get; set; }
    public double CurrBidFactor { get; set; }
    public int CurrPlayerId { get; set; }
    public int LastBiddedPlayerId { get; set; }
    public bool Finished { get; set; }
    public List<int> AucPlayers { get; set; }

    public int NextBid(int bidAmount) => CurrBid + bidAmount;
}

public class ChestCard
{
    public int RandomGroup { get; set; }
    public int Type { get; set; }
    public string Text { get; set; }
    public int Money { get; set; }
    public int Pos { get; set; }
}

public class ARule
{
    public int id { get; set; }
    public bool disabled { get; set; }
    public int groupId { get; set; }
    public int myCount { get; set; }
    public int anCount { get; set; }
    public bool needBuildHouses { get; set; }
    public string housesGroups { get; set; }
    public int myMoney { get; set; }
    public double factor { get; set; }
}
/*
    2-1-2;3-1-2;1500-0;1;d=0
    group id - get cells_num - my cells_num ; opp_gid - i give num - opp_cells_num
 */
public class TRule
{
    public int id { get; set; }
    public int getLand { get; set; }
    public int getCount { get; set; }
    public int myCount { get; set; }
    public int getMoney { get; set; }

    public int giveLand { get; set; }
    public int giveCount { get; set; }
    public int yourCount { get; set; }
    public int giveMoney { get; set; }
    public bool disabled { get; set; }
    public double moneyFactor { get; set; }
}

public class RoundLog
{
    public int Round { get; set; }
    public int Pid { get; set; }
    public int OldPos { get; set; }
    public int NewPos { get; set; }
    public List<RoundLogItem> PlayerActions { get; set; }

    public string Message { get; set; }

}
public class RoundLogItem
{
    public string ActionText { get; set; }

}
