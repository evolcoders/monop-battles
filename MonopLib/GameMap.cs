using MonopLib.Managers;

namespace MonopLib;

public class GameMap
{
    private Game _Game {get;set;}

    public GameMap(Game g)
    {
        _Game = g;
        DataUtil.InitCellsAndChestCards(_Game, this);
    }

    public List<Cell> Cells { get; set; }

    public IEnumerable<Cell> CellsByUser(int pid) => Cells.Where(x => x.Owner == pid);
    public IEnumerable<Cell> CellsByGroup(int group) => Cells.Where(x => x.Group == group);
    public Cell[] CellsByType(int type) => Cells.Where(x => x.Type == type).ToArray();
    public bool GroupNotMyMonop(int pid, int[] groups) => Cells.Any(x => groups.Contains(x.Group) && x.IsMonopoly && x.Owner != pid);

    public Cell[] CellsByUserByGroup(int pid, int group) => Cells.Where(x => x.Owner == pid && x.Group == group).ToArray();
    public Cell[] CellsByUserByType(int pid, int type) => Cells.Where(x => x.Owner == pid && x.Type == type).ToArray();

    public int[][] PlayerCellGroups { get; } = [new int[11], new int[11], new int[11], new int[11], new int[11]];

    public ChestCard[] CommunityChest { get; set; }
    public ChestCard[] ChanceChest { get; set; }

    public (int, int) GetHotelsAndHouses(int pid)
    {
        var cc = CellsByUserByType(pid, 1);
        var houses = cc.Where(h => h.HousesCount > 0 && h.HousesCount < 5).Sum(h => h.HousesCount);
        var hotels = cc.Where(h => h.HousesCount == 5).Sum(h => h.HousesCount);
        return (hotels, houses);
    }

    public IEnumerable<IGrouping<int, Cell>> MonopGroupsByUser(int pid)
    {
        return CellsByUserByType(pid, 1).Where(c => c.IsMonopoly).GroupBy(c => c.Group)
        .Where(gr => gr.All(c => c.IsActive));
    }

    public List<(int, int)> GetGroupsWhereNeedBuildHouses(int pid, int maxCount = 3)
    {
        return _Game.Map.CellsByUser(pid)
        .Where(c => c.Type == 1 && c.HousesCount > 0 && c.HousesCount < maxCount)
        .GroupBy(c => c.Group)
        .Select(gr => (gr.Key, gr.Max(c => c.HousesCount))).ToList();
    }

    public void SetOwner(Player p, Cell cell, int cost)
    {
        if (cell.Owner == p.Id) return;
        cell.Owner = p.Id;
        //cell.IsMortgage = false;
        PlayerManager.OnlyPay(_Game, p, cell.Cost);
        UpdateCellsGroupInfo();
    }

    public void UpdateCellsGroupInfo()
    {
        // groupped by Cell.Group and Cell.Owner
        var groups = Cells.Where(c => c.Land && c.Owner.HasValue)
        .GroupBy(c => new { c.Group, c.Owner });

        foreach (var group in groups)
        {
            foreach (var gcell in group)
            {
                gcell.OwnerGroupCount = group.Count();
            }
            PlayerCellGroups[group.Key.Owner.Value][group.Key.Group] = group.Count();
        }
    }

    public void TakeRandomCard()
    {
        var rand = new Random();
        if (new[] { 7, 22, 36 }.Contains(_Game.CurrCell.Id))
        {
            var count = ChanceChest.Length;
            _Game.LastRandomCard = ChanceChest[rand.Next(count)];
        }
        else
        {
            var count = CommunityChest.Length;
            _Game.LastRandomCard = CommunityChest[rand.Next(count)];
        }
    }

}
