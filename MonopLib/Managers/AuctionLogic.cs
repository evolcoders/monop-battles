
namespace MonopLib.Managers;


public class AuctionManager(Game g)
{

    public void InitAuction()
    {
        var cell = g.CurrCell;
        g.CurrAuction = new()
        {
            Cell = cell,
            CurrPlayerId = g.CurrPlayer.Id,
            CurrBid = cell.Cost,
            LastBiddedPlayerId = 0,
            AucPlayers = [.. g.Players.Select(p => p.Id)],
        };

    }
    public void RunActionJob(string command)
    {
        var pl = g.FindPlayerBy(g.CurrAuction.CurrPlayerId);
        if (pl.IsHuman && command == "auto") return;
        if (g.CurrAuction.AucPlayers.Contains(pl.Id))
            MakeBid(pl, command);

        CheckIfFinished();
    }

    private void MakeBid(Player pl, string cmd)
    {
        var auc = g.CurrAuction;
        var needbid = NeedBid(pl, cmd);
        if (needbid)
        {
            auc.CurrBid += 50;
            auc.LastBiddedPlayerId = pl.Id;
            g.AddRoundMessageByLabel("_player_bid", pl.Name, auc.CurrBid);

        }
        else
        {
            g.AddRoundMessageByLabel("_player_left_auction", pl.Name);
            auc.AucPlayers.Remove(pl.Id);
            NextAuctionPlayer();
        }

    }

    private bool NeedBid(Player pl, string cmd)
    {
        var cell = g.CurrAuction.Cell;
        var fact = g.BuyingLogic.FactorOfBuy(pl, cell);
        g.CurrAuction.CurrBidFactor = fact;
        var maxCost = cell.Cost * fact;
        var maxMoney = g.CalcPlayerAssets(pl.Id);
        if (pl.IsBot)
            return maxMoney > maxCost && g.CurrAuction.CurrBid + 50 < maxCost;
        else
            return cmd == "y";
    }

    private void NextAuctionPlayer()
    {
        var pls = g.CurrAuction.AucPlayers;
        if (pls.Count == 0) return;

        var next = pls.FirstOrDefault(pid => pid > g.CurrAuction.CurrPlayerId);
        if (next == 0) next = pls.First();
        g.CurrAuction.CurrPlayerId = next;
    }

    private void CheckIfFinished()
    {
        var auc = g.CurrAuction;
        var pls = g.CurrAuction.AucPlayers;
        var count = g.CurrAuction.AucPlayers.Count;
        if (count == 0 || (count == 1 && auc.LastBiddedPlayerId == pls.First()))
        {
            auc.Finished = true;
            var result = count switch
            {
                0 => "no_winner",
                _ => $"winner_{g.FindPlayerBy(auc.CurrPlayerId).Name}"
            };

            SetAucWinner();
            if (count != 0)
                g.AddRoundMessageByLabel("_player_bought_cell_on_auction", auc.Cell.Title, auc.CurrBid, result);
            else
                g.AddRoundMessage("аукцион не состоялся", "auction failed");

            g.FinishStep($"auc_finished");
        }
    }

    private void SetAucWinner()
    {
        var cell = g.CurrAuction.Cell;
        var auc = g.CurrAuction;
        var pl = g.FindPlayerBy(auc.CurrPlayerId);
        g.Map.SetOwner(pl, cell, auc.CurrBid);
    }
}
