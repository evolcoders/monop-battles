using System.Reflection;
using MonopLib.BotBrains;
using MonopLib.BotBrains.Interfaces;
using MonopLib.Managers;

namespace MonopLib;


public class Game
{
    //metainfo
    public int Id { get; set; }
    public int Round { get; set; }
    public int LastRollAsInt { get; set; }
    public int ManualRoll { get; set; }
    public GameConfig Config { get; set; }
    public GameState State { get; set; }
    public string UILanguage { get; set; }

    //player and cells info
    public List<Player> Players { get; set; }

    public int Selected { get; set; }
    public int SelectedPos { get; set; }

    public List<int>[] PlayersRolls { get; } = [new(30), new(30), new(30), new(30)];

    //pay info
    public int PayAmount { get; set; }
    public int? PayToUser { get; set; }
    public string PayMessage { get; set; }

    //Chest map cards

    public ChestCard LastRandomCard { get; set; }

    //Auction
    public Auction CurrAuction { get; set; }
    //Trading
    public TradeBox CurrTradeBox { get; set; }
    public List<TradeBox> CompletedTrades { get; set; } = new(30);
    public List<TradeBox> RejectedTrades { get; set; } = new(30);
    public BotRules BotRules { get; set; }

    public GameMap Map { get; set; }
    //public bool UseGameUpdater { get; set; } = true;

    //managers
    public AuctionManager AuctionManager { get; set; }
    public IBuyingLogic BuyingLogic { get; set; }
    public ICellsLogic CellsLogic { get; set; }


    public Game(int Id, string Lang)
    {
        this.Id = Id;
        UILanguage = Lang;
        Config = new() { NeedShowLog = true, IsManualRollMode = false };
        Round = 0;
        Selected = 0;
        Players = new(4);
        Map = new GameMap(this);
        State = GameState.Start;
        //managers
        BotRules = new BotRules();
        AuctionManager = new AuctionManager(this);
        CellsLogic = new CellsLogic(this);
        BuyingLogic = new BuyingLogic(CellsLogic, this);

        CreateNewRoundLog(1, 0, 0);
    }

    public Player CurrPlayer => Players[Selected];
    public Cell CurrCell => Map.Cells[CurrPlayer.Pos];
    public Player FindPlayerBy(int pid) => Players.Find(pl => pl.Id == pid);
    public (int r1, int r2) LastRoll => (LastRollAsInt / 10, LastRollAsInt % 10);

    //Logs
    public List<string> RoundMessages { get; set; } = new(10);
    public List<string> MethodsTrace { get; set; } = new(100);
    public List<RoundLog> GameLogs { get; } = new(100);


    public void AddRoundMessage(string ru_text, string en_text) => LogRoundMessage(BuildMessage(ru_text, en_text));
    public string BuildMessage(string ru_text, string en_text) => UILanguage == "ru" ? ru_text : en_text;

    public void AddRoundMessage(string text) => LogRoundMessage(text);
    public void AddLogicMessage(string text)
    {
        LogRoundMessage(text);
    }

    public void AddRoundMessageByLabel(string label, params object[] args) =>
        LogRoundMessage(string.Format(TextRes.Get(UILanguage, label), args));

    private void LogRoundMessage(string message) =>
              //RoundMessages.Add($"{Round} {text}");
              Console.WriteLine($"{Round} {message}");
    //RoundLog(message);

    public void RoundLog(string message, int[] cells = null)
    {
        var log = GameLogs.Last();
        log.PlayerActions.Add(new()
        {
            ActionText = message,
        });

    }

    public void FinishStep(string act)
    {
        State = GameState.EndStep;
        if (CurrPlayer.IsHuman && !Config.ConfirmRoundEnding)
            FinishGameRound();
    }

    public static int[] DOUBLE_ROLLS = new[] { 11, 22, 33, 44, 55, 66 };

    public void FinishGameRound()
    {
        if (State != GameState.EndStep) return;

        //Console.WriteLine(string.Join(Environment.NewLine, this.RoundMessages));
        RoundMessages.Clear();
        MethodsTrace.Clear();

        if (CurrPlayer.IsBot)
            BotActionsWhenFinishStep(this);

        Console.WriteLine($"finished round {Round}");

        if (Config.IsManualRollMode)
            Players.ForEach(pl => pl.ManualRoll = 0);

        //Next rouns
        Round++;
        //pass turn to next player
        if (!DOUBLE_ROLLS.Contains(LastRollAsInt)) Selected++;
        if (Selected >= Players.Count) Selected = 0;

        // State = GameState.BeginStep;
        State = GameState.RunBuildOrTrade;
        CreateNewRoundLog(Round, Selected, CurrPlayer.Pos);

    }

    public static bool BotActionsBeforeRoll(Game g)
    {
        if (TradeLogic.TryDoTrade(g))
            TradeManager.RunTradeJob(g);
        return false;
    }

    public static void BotActionsWhenFinishStep(Game g)
    {
        var cells = g.Map.MonopGroupsByUser(g.CurrPlayer.Id);
        if (cells.Any())
        {
            var sum = 0.8 * g.CalcPlayerAssets(g.CurrPlayer.Id, false);
            g.CellsLogic.BuildHouses((int)sum);
            var text = String.Join(",", cells.SelectMany(gr => gr.Select(c => $"{c.Id}_{c.HousesCount}")));
            g.AddRoundMessage($"_build {text}");
        }
        else
        {
            var unmortCells = g.CellsLogic.UnmortgageSell();
        }
    }

    public void GoToNextRound()
    {
        bool startRoll = true;
        if (Config.IsManualRollMode)
            startRoll = Players.All(pl => (pl.IsHuman ? pl.ManualRoll != 0 : true));

        if (startRoll)
        {
            PlayerStepsManager.RollAndMakeStep(this);
        }
    }

    private void CreateNewRoundLog(int round, int pid, int pos)
    {
        GameLogs.Add(new()
        {
            Round = round,
            Pid = pid,
            OldPos = pos,
            PlayerActions = new(4),
        });
    }


    public void MoveToCell()
    {
        if (CurrPlayer.IsBot)
            PlayerStepsManager.MoveAfterRandom(this);
        else
            State = GameState.MoveToCell;
    }

    public void ToBeginState()
    {
        State = GameState.BeginStep;
    }


    public void ToPayAndFinish(int amount = 0)
    {
        MethodsTrace.Add($"[ToPayAndFinish] amount:{amount}");
        if (amount != 0)
            PayAmount = amount;
        State = GameState.NeedPay;
    }

    public void ToPayAndGo()
    {
        MethodsTrace.Add($"[ToPay]");

        State = GameState.NeedPayAndContinue;
        // if (CurrPlayer.IsBot)
        // 	PlayerManager.Pay(this, FinishStep);
    }

    internal void ToCanBuy()
    {
        MethodsTrace.Add($"[ToCanBuy]");

        State = GameState.CanBuy;
        // if (!UseGameUpdater && CurrPlayer.IsBot)
        //     PlayerManager.Buy(this);
    }

    internal void ToCantPay()
    {
        MethodsTrace.Add($"[ToCantPay]");
        // if (!UseGameUpdater && CurrPlayer.IsBot)
        //     PlayerLeaveGame();
    }

    public void ToAuction()
    {
        State = GameState.Auction;
        AuctionManager.InitAuction();
    }

    internal int CalcPlayerAssets(int pid, bool includeMonop = true)
    {
        int sum = 0;
        foreach (var cell in Map.Cells.Where(c => c.IsActive && c.Owner == pid))
        {
            if (includeMonop)
            {
                sum += cell.MortgageAmount;
                sum += cell.HousesCount * cell.HouseCostWhenSell;
            }
            else
            if (!cell.IsMonopoly)
                sum += cell.MortgageAmount;
        }
        sum += FindPlayerBy(pid).Money;
        return sum;
    }

    public void PlayerLeaveGame()
    {
        var pl = CurrPlayer;
        if (Players.Count >= 2)
        {
            Players.Remove(pl);
            foreach (var cell in Map.CellsByUser(pl.Id))
            {
                cell.Owner = null;
                cell.HousesCount = 0;
            }
            Map.PlayerCellGroups[pl.Id] = new int[12];
            AddRoundMessage($"{pl.Name} покинул игру", $"{pl.Name} left game");
        }

        if (Players.Count == 1)
        {
            State = GameState.FinishGame;
            AddRoundMessageByLabel("_game_is_finished");
        }
    }
}
