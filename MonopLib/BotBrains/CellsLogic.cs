using MonopLib.BotBrains.Interfaces;

namespace MonopLib.BotBrains;


public class CellsLogic : ICellsLogic
{
    public CellsLogic(Game g) => _Game = g;

    private Game _Game { get; set; }

    public bool MortgageSell(Player p, int amount)
    {
        if (p.Money >= amount) return true;

        //1 - mortage non monopoly lands
        Mortgage(p, amount, false);

        //2 - sell houses
        SellHouses(p, amount);

        //3 -mortage monopoly without houses
        Mortgage(p, amount, true);

        return p.Money >= amount;
    }

    private bool Mortgage(Player p, int amount, bool includeMonopoly = false)
    {
        if (p.Money >= amount) return true;
        var cells = _Game.Map.CellsByUser(p.Id).Where(c => c.IsActive);
        var lands = cells.Where(x => !x.IsMonopoly && x.Type == 1);
        var transAndPowers = cells.Where(x => x.Type == 2 || x.Type == 3);
        var allCells = lands.Union(transAndPowers);

        if (includeMonopoly)
            allCells = allCells.Union(cells.Where(c => c.IsMonopoly && c.HousesCount == 0));

        string text = "";
        foreach (var cell in allCells)
        {
            if (p.Money >= amount) break;
            p.Money += cell.MortgageAmount;
            cell.IsMortgage = true;
            text = $"{text}_{cell.Id}";
        }
        if (!string.IsNullOrEmpty(text))
            _Game.AddLogicMessage($"Mortgage {text} money:{p.Money}");
        return p.Money >= amount;
    }

    public List<int> UnmortgageSell()
    {
        string text = "";
        var p = _Game.CurrPlayer;
        var needBuild = NeedBuildHouses(p.Id);
        var trans = _Game.Map.CellsByUserByGroup(p.Id, 9);
        var monopCells = _Game.Map.CellsByUser(p.Id).Where(c => c.IsMonopoly && c.IsMortgage);

        var cells = trans.Count() > 2 ? trans.Union(monopCells) : monopCells.Union(trans);
        //var cells = trans;
        List<int> res = new(4);
        foreach (var cell in cells)
        {
            if (cell.IsActive) continue;
            var sum = cell.UnMortgageAmount;
            if (p.Money < sum) break;
            p.Money -= sum;
            cell.IsMortgage = false;
            text = $"{text}_{cell.Id}";
            res.Add(cell.Id);
        }
        if (!string.IsNullOrEmpty(text))
            _Game.AddLogicMessage($"Unmortgage {text}");
        return res;
    }

    //build houses
    private bool NeedBuildHouses(int pid) =>
        _Game.Map.CellsByUser(pid).Any(c => c.Type == 1 && c.IsMonopoly && c.HousesCount < 4);



    public void BuildHouses(int spentSum, int group = 0)
    {
        var pl = _Game.CurrPlayer;
        var allGroupCells = _Game.Map.MonopGroupsByUser(pl.Id);
        if (group != 0) allGroupCells = allGroupCells.Where(gr => gr.Key == group);
        int builtSum = 0;
        string text = "";

        foreach (var monopCells in allGroupCells.OrderByDescending(gr => gr.Max(c => c.HousesCount)))
        {
            var cost = monopCells.First().HouseCost;
            while (Mortgage(pl, cost) && monopCells.Any(c => c.CanBuild))
            {
                foreach (var cell in monopCells.OrderBy(c => c.HousesCount))
                {
                    if (builtSum + cost > spentSum)
                    {
                        return;
                    }
                    if (Mortgage(pl, cost) && cell.CanBuild)
                    {
                        cell.HousesCount += 1;
                        pl.Money -= cell.HouseCost;
                        builtSum += cost;
                        text = $"{text}_{cell.Id}";
                    }
                }
            }
            if (!string.IsNullOrEmpty(text))
                _Game.AddRoundMessage($"_build {text}");
        }
    }

    public void SellHouses(Player p, int needAmount)
    {
        if (p.Money >= needAmount) return;

        string text = "";
        var cells = _Game.Map.CellsByUser(p.Id).Where(c => c.IsMonopoly && c.IsActive);
        while (true)
        {
            if (p.Money >= needAmount || !cells.Any(c => c.HousesCount > 0)) break;

            foreach (var cell in cells.OrderByDescending(c => c.HousesCount))
            {
                if (p.Money >= needAmount) break;
                if (cell.HousesCount > 0)
                {
                    cell.HousesCount -= 1;
                    p.Money += cell.HouseCostWhenSell;
                    text = $"{text}_{cell.Id}";
                }
            }
        }

        if (!string.IsNullOrEmpty(text))
            _Game.AddRoundMessage($"_sold_houses {text} money:({p.Money})");
    }

}
