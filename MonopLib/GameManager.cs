using MonopLib.BotBrains;
using MonopLib.Managers;

namespace MonopLib;

public class GameManager
{
	static Timer LifeTimer;

	public static void StartGame(Game g, int breakRound = 30)
	{
		ToFirstRound(g, 1000);
		while (true)
		{
			Thread.Sleep(50);
			GameManager.UpdateGame(g);
			if (g.Round == breakRound)
				break;
		}
	}

	public static void StartGameAsBackgroundThread(Game g)
	{
		ToFirstRound(g, 1000);
		LifeTimer = new Timer(GameManager.UpdateGame, g, 0, g.Config.UpdateInterval);
	}

	static void ToFirstRound(Game g, int updateInterval = 200)
	{
		g.State = GameState.BeginStep;
		g.Round = 1;
		g.Config.UpdateInterval = updateInterval;
	}

	public static void UpdateGame(Object obj)
	{
		var g = (Game)obj;
		//g.MethodsTrace.Add($"[UpdateGame] {g.State}");
		// if (g.Round % 3 == 0) g.AddRoundMessage(g.State.ToString());

		switch (g.State)
		{
			case GameState.RunBuildOrTrade:
				var res = TradeLogic.TryDoTrade(g);
				if (!res) g.ToBeginState();
				break;
			case GameState.BeginStep:
				g.GoToNextRound();
				break;
			case GameState.CanBuy:
				PlayerManager.BotBuy(g);
				break;
			case GameState.NeedPay:
				if (g.CurrPlayer.IsBot)
					PlayerManager.Pay(g, true);
				break;
			case GameState.NeedPayAndContinue:
				if (g.CurrPlayer.IsBot)
					PlayerManager.Pay(g, false);
				break;
			case GameState.Auction:
				g.AuctionStrategy.RunActionJob("auto");
				break;
			case GameState.Trade:
				TradeManager.RunTradeJob(g);
				break;
			case GameState.CantPay:
				if (g.CurrPlayer.IsBot)
					g.PlayerLeaveGame();
				break;
			case GameState.EndStep:
				//if (g.CurrPlayer.IsBot || !g.Config.ConfirmRoundEnding)
				if (!g.Config.ConfirmRoundEnding)
					g.FinishGameRound();
				break;

			default:
				break;
		}
	}

	public static bool BotActionsBeforeRoll(Game g)
	{
		if (TradeLogic.TryDoTrade(g))
			TradeManager.RunTradeJob(g);
		return false;
	}

	public static void BotActionsWhenFinishStep(Game g)
	{
		CellsLogic.UnmortgageSell(g);
		var sum = 0.8 * g.CalcPlayerAssets(g.CurrPlayer.Id, false);
		HousesLogic.BuildHouses(g, (int)sum);
	}
}
