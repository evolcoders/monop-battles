using System.Net.NetworkInformation;
namespace MonopLib.BotBrains;

public class HousesLogic
{
    internal static bool NeedBuildHouses(Game g, int pid) =>
        g.Map.CellsByUser(pid).Any(c => c.Type == 1 && c.IsMonopoly && c.HousesCount < 4);

    internal static List<(int, int)> GetGroupsWhereNeedBuildHouses(Game g, int pid, int maxCount = 3)
    {
        return g.Map.CellsByUser(pid)
        .Where(c => c.Type == 1 && c.HousesCount > 0 && c.HousesCount < maxCount)
        .GroupBy(c => c.Group)
        .Select(gr => (gr.Key, gr.Max(c => c.HousesCount))).ToList();
    }

    internal static void BuildHouses(Game g, int spentSum, int group = 0)
    {
        var pl = g.CurrPlayer;
        var allGroupCells = g.Map.MonopGroupsByUser(pl.Id);
        if (group != 0) allGroupCells = allGroupCells.Where(gr => gr.Key == group);
        int builtSum = 0;
        string text = "";

        foreach (var groupCells in allGroupCells.OrderByDescending(gr => gr.Max(c => c.HousesCount)))
        {
            var cost = groupCells.First().HouseCost;
            while (CellsLogic.Mortgage(g, pl, cost) && groupCells.Any(c => c.CanBuild))
            {
                foreach (var cell in groupCells.OrderBy(c => c.HousesCount))
                {
                    if (builtSum + cost > spentSum)
                    {
                        return;
                    }
                    if (CellsLogic.Mortgage(g, pl, cost) && cell.CanBuild)
                    {
                        cell.HousesCount += 1;
                        pl.Money -= cell.HouseCost;
                        builtSum += cost;
                        text = $"{text}_{cell.Id}";
                    }
                }
            }
            if (!string.IsNullOrEmpty(text))
                g.AddRoundMessage($"_build {text}");
        }
    }

    internal static void SellHouses(Game g, Player p, int needAmount)
    {
        string text = "";
        var cells = g.Map.CellsByUser(p.Id).Where(c => c.IsMonopoly && c.IsActive);
        while (true)
        {
            if (p.Money > needAmount || !cells.Any(c => c.HousesCount > 0)) break;

            foreach (var cell in cells.OrderByDescending(c => c.HousesCount))
            {
                if (p.Money > needAmount) break;
                if (cell.HousesCount > 0)
                {
                    cell.HousesCount -= 1;
                    p.Money += cell.HouseCostWhenSell;
                    text = $"{text}_{cell.Id}";
                }
            }
        }

        if (!string.IsNullOrEmpty(text))
            g.AddRoundMessage($"_sold_houses {text}");
    }

}
