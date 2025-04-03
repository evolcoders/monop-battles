namespace MonopLib.BotBrains;

public class BuyLogic
{
    public static double FactorOfBuy(Game g, Player p, Cell cell)
    {
        var pid = p.Id;
        var cg = cell.Group;
        var availableMoney = g.CalcPlayerAssets(pid);
        if (availableMoney < cell.Cost) return 0;

        var myGroupsWithHouses = HousesLogic.GetGroupsWhereNeedBuildHouses(g, pid);
        var myGroupsIds = myGroupsWithHouses.Select(g => g.Item1).ToArray();

        bool needBuild = myGroupsWithHouses.Count > 0;
        if (needBuild)
        {
            var ff = myGroupsWithHouses.Select(gh =>
            {
                var arr = GroupsWithMaxHouseCount(gh.Item1, gh.Item2);
                var mf = arr.FirstOrDefault(mf => mf.Item1 > availableMoney);
                return mf != default ? mf.Item2 : 0;
            }).Min();
            return ff;
        }

        IEnumerable<Cell> currCellGroup = g.Map.CellsByGroup(cell.Group);
        IEnumerable<Cell> notMine = currCellGroup.Where(x => x.Owner.HasValue && x.Owner != pid);
        int myCount = currCellGroup.Where(x => x.Owner == pid).Count();

        int aCount = 0;
        int? aOwner = null;
        if (notMine.Any())
        {
            aCount = notMine.Max(x => x.OwnerGroupCount);
            aOwner = notMine.FirstOrDefault(x => x.OwnerGroupCount == aCount)?.Owner;
        }
        if (aCount == 2 && aOwner.HasValue)
        {
            var aSum = g.CalcPlayerAssets(aOwner.Value);
            if (cg == 2 && aSum > 4000) return 4;
            if (cg == 3 && aSum > 4000) return 3;
            if (cg == 4 && aSum > 5000) return 2;
            if (cg == 5 && aSum > 7000) return 2;
        }
        var manualFactor = GetManualFactor(g.BotRules.AuctionRules, cg, myCount, aCount, myGroupsIds);
        if (manualFactor.HasValue)
            return manualFactor.Value;

        double needBuyfactor = 0;
        if ((cg > 0 && cg < 9) || (cg == 10))
            needBuyfactor = AnotherPlayerFactor(cg, aCount);
        if (cg == 9 && needBuild)
            needBuyfactor = AnotherPlayerFactor(cg, aCount);
        if (myCount > 0)
            needBuyfactor = myPlayerFactor(cg, myCount);

        return needBuyfactor;
    }

    private static double myPlayerFactor(int cg, int myCount)
    {
        var gfactors = cg switch
        {
            1 => "1,2",
            2 => "1.1, 2, 3",
            3 => "1.1, 2, 2",
            4 => "1.1, 2, 2",
            5 => "1.1, 2, 2",
            6 => "1.1, 2, 2",
            7 => "1.1, 2, 2",
            8 => "1.1, 2",
            9 => "1.3, 1.5, 2, 2, 2",
            10 => "1.1, 1.4, 0",
            _ => ""
        };
        return Convert.ToDouble(gfactors.Split(',')[myCount]);
    }

    private static double AnotherPlayerFactor(int cg, int aCount)
    {
        var gfactors = cg switch
        {
            1 => "1,2",
            2 => "1.1, 2, 3",
            3 => "1.1, 2, 2",
            4 => "1.1, 2, 3",
            5 => "1.1, 2, 2",
            6 => "1.1, 2, 2",
            7 => "1, 2, 2",
            8 => "2, 3",
            9 => "1.3, 1.5, 2.5, 3",
            10 => "1.1, 1.4",
            _ => ""
        };
        return Convert.ToDouble(gfactors.Split(',')[aCount]);
    }

    private static double? GetManualFactor(List<ARule> botAuctionRules, int group, int myCount, int aCount, int[] groupsWithHouses)
    {
        foreach (var arule in botAuctionRules)
        {
            var rh = arule.housesGroups;
            bool needBuild = true;
            if (!string.IsNullOrEmpty(rh) && rh != "any")
                needBuild = rh.Split(',').Select(x => Convert.ToInt32(x)).Intersect(groupsWithHouses).Any();
            else if (rh == "any")
                needBuild = groupsWithHouses.Any();
            if (arule.groupId == group && arule.myCount == myCount && arule.anCount == aCount && needBuild)
                return arule.factor;
        }
        return null;
    }

    /*
    group - группа земель(мнополия)
    maxHousesOnCell - макс кол-во домов на указанной группе

    этот массив кэффициентов нужен для определения коэффициента покупки другой карточки,
    если уже есть у игрока монополия и на ней надо строить дома
    (чем меньше домов - ниже коэффициент купить другую карточку, нужно строить дома на своей монополии)
    но если у оппонента получается своя монополия, то увеличиваем фактор покупки этой карточки,
    так как его монополия дает возможность ему строить дома
    */
    private static (int, double)[] GroupsWithMaxHouseCount(int group, int maxHousesOnCell)
    {
        var group1 = new (int, double)[][] {
            [(4000,0), (6000, 1.1)],
            [(4000,0), (6000, 1.1)],
            [(4000,0), (5000, 1.2)],
            [(4000,0), (5000, 1.2)],
            [(4000,0), (5000, 1.2)],
         };

        var group2 = new (int, double)[][]
        {
            [(6000, 0), (7000, 1.1)],
            [(6000, 0), (7000, 1.1)],
            [(5000, 0), (7000, 1.2), (9000, 1.2)],
            [(4000, 0), (5000, 1.0)],
            [(4000, 0), (5000, 1.0)],
         };

        var group3 = new (int, double)[][]
        {
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 7000,0), ( 9000, 1), (11000, 1.2)],
            [( 4000,0), ( 5000, 1), ( 7000, 1.2)],
            [( 3000,0), ( 5000, 1), ( 6000, 1.2)],
         };

        var group4 = new (int, double)[][]
        {
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 7000,0), ( 9000, 1), (11000, 1.2)],
            [( 4000,0), ( 5000, 1), ( 7000, 1.2)],
            [( 3000,0), ( 4000, 1), ( 6000, 1.2)],
         };
        var group5 = new (int, double)[][]
        {
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 7000,0), ( 9000, 1), (11000, 1.2)],
            [( 4000,0), ( 5000, 1), ( 7000, 1.2)],
            [( 3000,0), ( 4000, 1), ( 6000, 1.2)],
        };

        var group6 = new (int, double)[][]
        {
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 9000,0), (11000, 1), (13000, 1.2)],
            [( 7000,0), ( 9000, 1), (11000, 1.2)],
            [( 4000,0), ( 5000, 1), ( 7000, 1.2)],
            [( 3000,0), ( 4000, 1), ( 6000, 1.2)],
        };

        var group7 = new (int, double)[][]
        {
            [( 9000,0), (7000, 1.1), (9000, 1.2)],
            [( 6000,0), (7000, 1.1), (9000, 1.2)],
            [( 5000,0), (7000, 1.1), (9000, 1.2)],
            [( 3000,0), (5000, 1.1), (7000, 1.2)],
            [( 2000,0), (4000, 1.1), (6000, 1.2)],
         };

        var group8 = new (int, double)[][]
        {
            [( 8000,0), (11000, 1), (13000, 1.2)],
            [( 8000,0), (10000, 1), (12000, 1.2)],
            [( 5000,0), ( 7000, 1), ( 9000, 1.2)],
            [( 1000,0), ( 5000, 1), ( 7000, 1.2)],
            [( 1000,0), ( 4000, 1), ( 6000, 1.2)],
         };

        var factors = new Dictionary<int, (int, double)[][]>
         {
            {1, group1},
            {2, group2},
            {3, group3},
            {4, group4},
            {5, group5},
            {6, group6},
            {7, group7},
            {8, group8},
         };
        return factors[group][maxHousesOnCell];
    }
}
/*
        var group1 = new (int, double)[][]
        {
            new[] { (8000, 0), (11000,1), (13000, 1.2)},
            new[] { (8000, 0), (10000,1), (12000, 1.2)},
            new[] { (5000,0), (7000,  1), (9000,  1.2)},
        };
        var factors = new Dictionary<int, (int, double)[][]>
        { {1,group1} };
 */
