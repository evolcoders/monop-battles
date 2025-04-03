using System.Text.Json;
using MonopConsole;
using MonopLib;

class App
{
	static void Main(string[] args)
	{
		var players = new string[] { "RudikBot", "AshotBot" };

		//var db = new MonopConsole.Data.DbService();
		var g = ConsoleGameHelper.CreateAndStartAutoGame(players);
		//RunGame(players, g);
		//GenJson();

	}

	private static void GenJson()
	{
		var brules = new BotRules();
		string json = JsonSerializer.Serialize(brules.TradeRules);
		File.WriteAllText("traderules.json", json);
	}

	private static void RunGame(string[] players, Game g)
	{
		string cmd;
		do
		{
			//MapPrinter.PrintGameInfo2(g);
			if (g.State == GameState.BeginStep)
				MapPrinter.PrintMap(g);

			var roundText = ConsoleGameHelper.ShowGameState(g, g.CurrPlayer.Name);
			if (!g.CurrPlayer.IsBot)
				Console.WriteLine(roundText);


			cmd = Console.ReadLine();

			if (ConsoleGameHelper.IsValidCommand(g, cmd))
			{
				ConsoleGameHelper.ProcessCommand(g, cmd, players[0]);
			}

		} while (cmd != "q");

	}

	private static void Test()
	{
		string cmd = "um2-3-4-5";
		var cells = cmd[2..].Split('-').Select(n => Int32.Parse(n)).ToArray();
		Console.WriteLine(string.Join('-', cells));
	}

	static void Print(Game g, string str) => Console.WriteLine($"[{g.State}]---{str}");

}
