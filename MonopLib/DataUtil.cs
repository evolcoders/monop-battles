using System.Reflection;

namespace MonopLib;

internal class DataUtil
{
	static int ParseInt(string arr)
	{
		Int32.TryParse(arr.Trim(), out int result);
		return result;
	}
	static string GameDataFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

	internal static void InitCellsAndChestCards(Game g, GameMap map)
	{
		InitCells(g, map, GameDataFolder);
		InitChestCards(g, map, GameDataFolder);
	}

	static void InitCells(Game g, GameMap map, string folder)
	{
		var lang = g.UILanguage;
		var lines = File.ReadAllLines(Path.Combine(folder, $"GameData/lands_{lang}.txt"));
		var Cells = new List<Cell>(40);
		foreach (var line in lines.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var arr = line.Split('|');
			var cell = new Cell
			{
				Title = arr[0].Trim(),
				Id = ParseInt(arr[1]),
				Cost = ParseInt(arr[2]),
				Type = ParseInt(arr[3]),
				Group = ParseInt(arr[4]),
				RentInfo = arr[5].Trim(),
				Info = arr[6],
			};
			Cells.Add(cell);
		}
		map.Cells = Cells.OrderBy(c => c.Id).ToList();
	}

	static void InitChestCards(Game g, GameMap map, string folder)
	{
		var lang = g.UILanguage;
		var lines = File.ReadAllLines(Path.Combine(folder, $"GameData/chest_cards_{lang}.txt"));
		var Cards = new List<ChestCard>(30);
		foreach (var line in lines.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var arr = line.Split('|');
			var card = new ChestCard
			{
				RandomGroup = ParseInt(arr[0]),
				Type = ParseInt(arr[1]),
				Text = arr[2].Trim(),
			};
			if (arr.Length > 3)
				card.Money = ParseInt(arr[3]);
			if (arr.Length > 4)
				card.Pos = ParseInt(arr[4]);
			Cards.Add(card);
		}
		map.CommunityChest = Cards.Where(cr => cr.Type == 1).ToArray();
		map.ChanceChest = Cards.Where(cr => cr.Type == 2).ToArray();
	}

	internal static void InitBotRules(BotRules rules)
	{
		InitAuctionBotRules(rules, GameDataFolder);
		InitTradeBotRules(rules, GameDataFolder);
	}

	internal static void InitAuctionBotRules(BotRules rules, string folder)
	{
		var lines = File.ReadAllLines(Path.Combine(folder, "GameData/auc_rules.txt"));
		foreach (var line in lines.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var rule = line.Split(';').ToDictionary(k => k.Split('=').First(), x => x.Split('=').Last());
			var arule = new ARule
			{
				groupId = ParseInt(rule["gid"]),
				myCount = ParseInt(rule["myc"]),
				anCount = ParseInt(rule["anc"]),
				myMoney = ParseInt(rule["money"]),
				housesGroups = rule["nb"],
				factor = Convert.ToDouble(rule["fac"])
			};
			rules.AuctionRules.Add(arule);
		}
	}

	/*
	3-1-2;4-1-2;0-0;1;d=0
	3-1-1;4-1-1;0-0;1;d=0
	4-1-2;8-1-1;0-0;1;d=0
	4-1-2;3-2-1;0-0;1;d=0

	4-1-2; 5-1-2; 0-0;1;d=0
	4-1-2 у меня 4ая группа, мне дают 1у карту (у меня 2 других),
	5-1-2 5ая группа у игрока, я даю свою одну(у него уже 2 есть)
	0-0   даю сумму - получаю сумму
	1;    мани фактор, отношение my_money/another_moneu


	*/
	internal static void InitTradeBotRules(BotRules rules, string folder)
	{
		var lines = File.ReadAllLines(Path.Combine(folder, "GameData/trade_rules.txt"));
		int id = 0;
		foreach (var line in lines.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var ruleParts = line.Split(';');
			var mm1 = ruleParts[0].Split("-");
			var mm2 = ruleParts[1].Split("-");
			var trule = new TRule
			{
				getLand = ParseInt(mm1[0]),
				getCount = ParseInt(mm1[1]),
				myCount = ParseInt(mm1[2]),

				giveLand = ParseInt(mm2[0]),
				giveCount = ParseInt(mm2[1]),
				yourCount = ParseInt(mm2[2]),
				id = id++
			};
			if (ruleParts.Length > 2)
			{
				var mm3 = ruleParts[2].Split("-");
				trule.getMoney = ParseInt(mm3[0]);
				trule.giveMoney = ParseInt(mm3[1]);
			}
			trule.moneyFactor = ruleParts.Length > 3 ? Convert.ToDouble(ruleParts[3]) : 1;
			if (ruleParts.Length > 4)
				trule.disabled = ruleParts[4].Trim() == "d=1";
			if (!trule.disabled)
				rules.TradeRules.Add(trule);
		}
	}
}
