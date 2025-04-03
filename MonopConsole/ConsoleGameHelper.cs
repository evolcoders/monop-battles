
using System.Text.Json;
using MonopLib;
using MonopLib.Managers;

namespace MonopConsole;

public class ConsoleGameHelper
{
    //static Timer LifeTimer;
    public static Game CreateAndStartWithInteraction(string[] playerNames)
    {
        Game g = new Game(1, "ru");
        g.Players.AddRange(
            playerNames.Select((nm, idx) => new Player(idx, nm,
             nm.EndsWith("Bot") ? PlayerType.Bot : PlayerType.Human, 15000)));

        g.Config.ConfirmRoundEnding = false;
        g.Config.IsConsole = true;

        GameManager.StartGameAsBackgroundThread(g);
        return g;

    }

    public static Game CreateAndStartAutoGame(string[] playerNames)
    {
        Game g = new Game(1, "ru");
        g.Players.AddRange(
            playerNames.Select((nm, idx) => new Player(idx, nm,
             nm.EndsWith("Bot") ? PlayerType.Bot : PlayerType.Human, 15000)));

        g.Config.ConfirmRoundEnding = false;
        g.Config.IsConsole = true;

        // GameManager.StartGameAsBackgroundThread(g);
        GameManager.StartGame(g, 100);
        string json = JsonSerializer.Serialize(g.GameLogs);
        File.WriteAllText("gamelog.json", json);
        return g;

    }

    public static bool IsValidCommand(Game g, string cmd)
    {
        if (g.State == GameState.MoveToCell || g.State == GameState.CanBuy || g.State == GameState.EndStep)
            return true;
        if (string.IsNullOrEmpty(cmd))
            return false;
        return true;
    }


    public static void ProcessCommand(Game g, string cmd, string curr, bool printMap = true)
    {
        switch (g.State)
        {
            case GameState.BeginStep:

                if (cmd.StartsWith("m"))
                    Mortgage(g, cmd);
                if (cmd.StartsWith("um"))
                    UnMortgage(g, cmd);

                g.GoToNextRound();
                break;
            case GameState.CanBuy:
                if (cmd == "b" || string.IsNullOrEmpty(cmd))
                    PlayerManager.Buy(g);
                else if (cmd == "a")
                    g.ToAuction();
                else if (cmd.StartsWith("m"))
                    Mortgage(g, cmd);
                else if (cmd.StartsWith("um"))
                    UnMortgage(g, cmd);

                break;

            case GameState.Auction:
                g.AuctionManager.RunActionJob(cmd);
                break;

            case GameState.Trade:
                if (cmd != "y")
                    TradeManager.CompleteTrade(g);
                else
                    TradeManager.AddToRejectedTrades(g);
                break;

            case GameState.CantPay or GameState.NeedPay:
                PlayerManager.Pay(g);
                break;

            case GameState.RandomCell:
                g.FinishStep("");
                break;

            case GameState.MoveToCell:
                PlayerStepsManager.MoveAfterRandom(g);
                break;

            case GameState.EndStep:
                if (cmd == "p")
                    PrintMethodTrace(g);
                else
                {
                    g.FinishGameRound();
                }
                break;

            default:
                //Console.WriteLine(g.State.ToString());
                break;
        }
    }

    public static string ShowGameState(Game g, string curr)
    {
        string infoText = g.State switch
        {
            GameState.BeginStep => string.Format("start round, {0}", g.Config.IsManualRollMode ? "choose one number [1..6]" : "write [game roll]"),
            GameState.CanBuy => $"you can buy #{g.CurrCell.Title} or auction, write [game b] or [game a]",
            GameState.Auction => "do you want bid? [y n]",
            GameState.Trade => $"player #{g.CurrTradeBox.From.Id} wants trade, give #{string.Join(",                                            ", g.CurrTradeBox.GiveCells)} wants #{string.Join(", ", g.CurrTradeBox.GetCells)}, write [game y] or [game n]",
            GameState.CantPay => "you need mortgage cells to find money",
            GameState.NeedPay => "yoy need pay, write [p]",
            GameState.RandomCell => "RandomCell",
            GameState.MoveToCell => "press 'go' to proceed new position",
            GameState.EndStep => "press any key to  finish round or #p to show methods trace",
            _ => "unknown game state"
        };
        return infoText;
    }

    static void UnMortgage(Game g, string cmd, string commandPrefix = "um")
    {
        var cells = cmd[2..].Split('-').Select(n => Int32.Parse(n)).ToArray();
        PlayerManager.ManualUnmortgageCells(g, cells);
    }

    static void Mortgage(Game g, string cmd)
    {
        var cells = cmd[1..].Split('-').Select(n => Int32.Parse(n)).ToArray();
        PlayerManager.ManualMortgageCells(g, cells);
    }

    private static void PrintMethodTrace(Game g)
    {
        var trace = string.Join("\\n", g.MethodsTrace);
        g.MethodsTrace.ForEach(l => Console.WriteLine(l));
    }

}
