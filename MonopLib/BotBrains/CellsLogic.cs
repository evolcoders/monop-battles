namespace MonopLib.BotBrains;

public class CellsLogic
{

	internal static bool MortgageSell(Game g, Player p, int amount)
	{
		if (p.Money >= amount) return true;

		//1 - mortage non monopoly lands
		Mortgage(g, p, amount, false);

		//2 - sell houses
		HousesLogic.SellHouses(g, p, amount);

		//3 -mortage monopoly without houses
		Mortgage(g, p, amount, true);

		return p.Money >= amount;
	}

	internal static bool Mortgage(Game g, Player p, int amount, bool includeMonopoly = false)
	{
		if (p.Money >= amount) return true;
		var cells = g.Map.CellsByUser(p.Id).Where(c => c.IsActive);
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
			g.AddRoundMessage($"Mortgage money:{p.Money} {text}");
		return p.Money >= amount;
	}

	internal static void UnmortgageSell(Game g)
	{
		string text = "";
		var p = g.CurrPlayer;
		var needBuild = HousesLogic.NeedBuildHouses(g, p.Id);
		var trans = g.Map.CellsByUserByGroup(p.Id, 9);
		var monopCells = g.Map.CellsByUser(p.Id).Where(c => c.IsMonopoly && c.IsMortgage);

		//var cells = trans.Count() > 2 ? trans.Union(monopCells) : monopCells.Union(trans);
		var cells = trans;
		foreach (var cell in cells)
		{
			if (cell.IsActive) continue;
			var sum = cell.UnMortgageAmount;
			if (p.Money < sum) break;
			p.Money -= sum;
			cell.IsMortgage = false;
			text = $"{text}_{cell.Id}";
		}
		if (!string.IsNullOrEmpty(text))
			g.AddRoundMessage($"Unmortgage {text}");
	}
}
