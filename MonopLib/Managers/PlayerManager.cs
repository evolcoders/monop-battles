namespace MonopLib.Managers;

public class PlayerManager
{
    public static bool OnlyPay(Game g, Player pl, int amount)
    {
        bool ok = g.CellsLogic.MortgageSell(pl, amount);
        if (ok)
        {
            pl.Money -= amount;
            return true;
        }
        return false;
    }

    public static bool Pay(Game g, bool finishRound = true)
    {
        var p = g.CurrPlayer;
        p.UpdateTimer();
        var amount = g.PayAmount;
        bool ok = p.IsBot ? g.CellsLogic.MortgageSell(p, amount) : p.Money >= amount;
        if (ok)
        {
            p.Money -= amount;
            if (p.Police > 0)
            {
                p.Police = 0;
                g.AddRoundMessageByLabel("_player_paid_to_exit_from_jail", p.Name);
                PlayerStepsManager.ChangePosAndProcessPosition(g);
                return true;
            }
            else if (g.PayToUser.HasValue)
            {
                var cellPl = g.FindPlayerBy(g.PayToUser.Value);
                cellPl.Money += amount;
                g.AddRoundMessage(g.PayMessage);
                g.PayToUser = default;
            }
            else
                g.AddRoundMessageByLabel("_player_paid", g.CurrPlayer.Name, amount);

            if (finishRound)
                g.FinishStep("_paid ${amount}");
            else
                g.State = GameState.BeginStep;

            g.PayAmount = 0;
            return true;
        }
        else
        {
            g.AddRoundMessage("_not_enough_money");
            g.ToCantPay();
            return false;
        }
    }

    public static void BotBuy(Game g)
    {
        if (g.State != GameState.CanBuy) return;
        var bot = g.CurrPlayer;
        if (!bot.IsBot) return;

        var cell = g.CurrCell;
        if (cell.Land && !cell.Owner.HasValue)
        {
            var ff = g.BuyingLogic.FactorOfBuy(bot, cell);
            bool needBuy = ff >= 1;
            if (ff == 1 && bot.Money < cell.Cost)
                needBuy = false;
            else if (ff > 1 && bot.Money < cell.Cost)
                needBuy = g.CellsLogic.MortgageSell(bot, cell.Cost);

            if (needBuy)
            {
                g.Map.SetOwner(bot, cell, cell.Cost);
                g.AddRoundMessageByLabel("_player_bought_cell", cell.Title, $"{cell.Cost} ff:{ff}");
                g.FinishStep($"_bought #{cell.Title}");
            }
            else
                g.ToAuction();
        }
    }

    public static void Buy(Game g)
    {
        if (g.State != GameState.CanBuy) return;

        var p = g.CurrPlayer;
        p.UpdateTimer();
        var cell = g.CurrCell;
        if (cell.Land && !cell.Owner.HasValue)
        {

            if (p.Money < cell.Cost)
            {
                g.State = GameState.CanBuy;
                g.AddRoundMessageByLabel("_not_enough_money");
            }
            else
            {
                g.Map.SetOwner(p, cell, cell.Cost);
                g.AddRoundMessageByLabel("_player_bought_cell", cell.Title, cell.Cost);
                g.FinishStep($"_bought #{cell.Title}");
            }

        }
    }

    public static string ManualMortgageCells(Game g, int[] cidArr)
    {
        string res = "";
        var pl = g.CurrPlayer;
        foreach (var cid in cidArr)
        {
            var cell = g.Map.Cells[cid];
            if (cell.IsMortgage || cell.HousesCount > 0) continue;
            pl.Money += cell.MortgageAmount;
            cell.IsMortgage = true;
            res += $"_{cid}";
        }
        return res;
    }

    public static string ManualUnmortgageCells(Game g, int[] cidArr)
    {
        string res = "";
        var pl = g.CurrPlayer;

        foreach (var cid in cidArr)
        {
            var cell = g.Map.Cells[cid];
            if (cell.IsMortgage)
            {
                pl.Money -= cell.UnMortgageAmount;
                cell.IsMortgage = false;
                res += $"_{cid}";
            }
        }
        return res;
    }
    public static string GoBuildHouses(Game g, Player pl, int[] cidArr)
    {
        string res = "";
        foreach (var cid in cidArr)
        {
            var cell = g.Map.Cells[cid];
            if (cell.IsMonopoly && cell.HousesCount < 4)
            {
                cell.HousesCount++;
                pl.Money -= cell.HouseCost;
                res += $"_{cid}";
            }
        }
        return res;
    }

    public static string GoSellHouses(Game g, Player pl, int[] cidArr)
    {
        string res = "";
        foreach (var cid in cidArr)
        {
            var cell = g.Map.Cells[cid];
            if (cell.HousesCount > 0)
            {
                cell.HousesCount--;
                pl.Money += cell.HouseCostWhenSell;
                res += $"_{cid}";
            }
        }
        return res;
    }
}
