namespace MonopLib.Managers;

public class PlayerStepsManager
{
    static readonly Random _currRandom = new Random();

    public static void RollAndMakeStep(Game g)
    {
        if (g.State != GameState.BeginStep) return;

        var currPlayer = g.CurrPlayer;
        if (currPlayer.IsBot &&
            GameManager.BotActionsBeforeRoll(g))
            return;

        //g.CurrPlayer.UpdateTimer();
        int r1, r2;

        if (!g.Config.IsManualRollMode)
        {
            r1 = 1 + _currRandom.Next(6);
            r2 = 1 + _currRandom.Next(6);
        }
        else
        {
            g.Players.ForEach(pl =>
               {
                   if (pl.IsBot) pl.ManualRoll = _currRandom.Next(6) + 1;
               });
            int sum = 0;
            g.Players.ForEach(pl => { if (pl.Id != currPlayer.Id) sum += pl.ManualRoll; });
            r1 = g.CurrPlayer.ManualRoll;
            r2 = sum != 0 ? (int)Math.Floor((double)sum / (g.Players.Count - 1)) : _currRandom.Next(6) + 1;
        }

        if (r1 == r2) currPlayer.DoubleRoll++;
        else
            currPlayer.DoubleRoll = 0;
        g.LastRollAsInt = r1 * 10 + r2;
        g.MethodsTrace.Add($"[MakeStep] rolled {g.LastRollAsInt}");


        //process movement after rolling
        var result = CheckRoll(g);
        if (result == PlayerAction.RollAndGo)
        {
            var oldPos = currPlayer.Pos;
            g.PlayersRolls[currPlayer.Id].Add(g.LastRollAsInt);
            MoveToNewPos(currPlayer, r1, r2);
            g.AddRoundMessageByLabel("_you_visisted_cell", $"(#{oldPos}->#{currPlayer.Pos})", g.CurrCell.Title);

            ProcessPosition(g);
        }
        else
            g.FinishStep(result.ToString());

        // g.LogRounds.Add(new() { Round = g.Round, Pid = g.Selected, Roll = g.LastRoll });
    }

    static void MoveToNewPos(Player curr, int r1, int r2)
    {
        curr.Pos += r1 + r2;
        if (curr.Pos > 39)
        {
            curr.Pos %= 40;
            curr.Money += 2000;
        }
    }

    static PlayerAction CheckRoll(Game g)
    {
        var pl = g.CurrPlayer;
        (int r1, int r2) = g.LastRoll;
        int maxMoneys = g.CalcPlayerAssets(pl.Id, false);

        if (pl.IsBot && pl.Police > 0 && CalcJailExit(g, pl.Id) && maxMoneys > 500)
        {
            //pl.Money -= 500;
            pl.Police = 0;
            PlayerManager.OnlyPay(g, 500);
            g.AddRoundMessage($"{pl.Name} заплатил $500 чтобы выйти из тюрьмы", $"{pl.Name} paid $500 to exit from jail");
        }
        var rolls = $"({r1},{r2})";
        var plInfo = $"{pl.Name}(money: ${pl.Money})";

        g.AddRoundMessageByLabel("_player_rolled", plInfo, rolls);

        if (pl.Police > 0)
        {
            if (r1 == r2)
            {
                g.AddRoundMessage("вы выходите из тюрьмы по дублю", "you exit from jail because of double roll");
                pl.Police = 0;
            }
            else
            {
                pl.Police += 1;
                if (pl.Police == 4)
                {
                    g.AddRoundMessage("вы должны заплатить $500 чтобы выйти из тюрьмы", "you must pay $500 to go from jail");
                    g.PayAmount = 500;
                    g.ToPayAndGo();
                    return PlayerAction.Pay500AndGo;
                }
                else
                {
                    g.AddRoundMessage("вы пропускаете ход в тюрьме", "you passed turn");
                    return PlayerAction.SkipStep;
                }

            }
        }

        if (CheckOnTripple(g.PlayersRolls[pl.Id], pl.DoubleRoll))
        {
            pl.Pos = 10;
            pl.Police = 1;
            return PlayerAction.TripleDouble;
        }
        return PlayerAction.RollAndGo;
    }

    static bool CalcJailExit(Game g, int pid)
    {
        bool group4or5or6isMonop = g.Map.GroupNotMyMonop(pid, [3, 4, 5, 6]);
        return !group4or5or6isMonop;
    }

    static bool CheckOnTripple(List<int> rolls, int count)
    {
        if (count > 0 && rolls.Count > 2)
            return rolls.TakeLast(3).All(ss => Game.DOUBLE_ROLLS.Contains(ss));
        return false;
    }

    public static void ProcessPosition(Game g)
    {
        var p = g.CurrPlayer;
        var cell = g.CurrCell;
        //cell is Land(type 1 or 2)
        if (cell.Land)
            ProcessLand(g, p, cell);

        else if (cell.Type == 6) // tax cells
            g.ToPayAndFinish(cell.Rent());

        else if (cell.Type == 4) // Chest cells
            ProcessChestCard(g, p);

        else if (p.Pos == 30) //police cell
        {
            p.MoveToJail();
            g.FinishStep("_go_jail_from_cell30");
        }
        else
            g.FinishStep("_no_functional_cell");
    }

    private static void ProcessLand(Game g, Player p, Cell cell)
    {
        g.MethodsTrace.Add($"[ProcessLand] cell:{cell.Id}");

        if (!cell.Owner.HasValue)
        {
            //g.AddRoundMessage($"Вы можете купить эту землю #{g.CurrCell.Title} за ${g.CurrCell.Cost}", $"You can buy this cell #{g.CurrCell.Title}");
            g.ToCanBuy();
        }
        else if (cell.Owner != p.Id)
        {
            if (cell.IsMortgage)
                g.FinishStep("_cell_mortgaged");
            else
            {
                g.PayToUser = cell.Owner;
                //g.AddRoundMessage($"заплатите ренту {cell.Rent()}", $"pay rent $#{cell.Rent()}");
                g.ToPayAndFinish(cell.Rent());

            }
        }
        else if (cell.Owner == p.Id)
        {
            g.AddRoundMessage($"вы попали на свою землю", $"you visited your own cell");
            g.FinishStep($"_mycell #{cell.Title}");
        }

    }

    // invoke  in Player.Pay()
    public static void ChangePosAndProcessPosition(Game g)
    {
        g.MethodsTrace.Add($"[ChangePosAndProcessPosition] cell:{g.CurrPlayer.Pos}");
        var pl = g.CurrPlayer;
        (int r1, int r2) = g.LastRoll;
        pl.Pos += r1 + r2;
        ProcessPosition(g);
    }

    public static void ProcessChestCard(Game g, Player p)
    {
        g.Map.TakeRandomCard();
        g.MethodsTrace.Add($"[ProcessChestCard] card #{g.LastRandomCard.Text}");

        var card = g.LastRandomCard;
        g.AddRoundMessageByLabel("_random_took_card", card.Text);

        switch (card.RandomGroup)
        {
            //получить мани
            case 1:
                p.Money += card.Money;
                g.FinishAfterChestCard();
                break;
            case 2 or 3:
                g.MoveToCell();
                break;
            case 4:
                g.PayAmount = card.Money * g.Players.Count;
                g.Players.ForEach(pl => pl.Money += card.Money);
                g.ToPayAndFinish();
                break;
            case 5:
                p.PoliceKey++;
                g.FinishAfterChestCard();
                break;
            //заплатить
            case 12:
                g.ToPayAndFinish(card.Money);
                break;
            case 15:
                var hh = g.Map.GetHotelsAndHouses(p.Id);
                var houseCost = card.Money;
                g.PayAmount = 4 * houseCost * hh.Item1 + houseCost * hh.Item2;
                g.ToPayAndFinish();
                break;
            default:
                g.FinishStep("finish_unknown_random");
                break;
        }
    }

    public static void MoveAfterRandom(Game g)
    {
        var c = g.LastRandomCard;
        g.MethodsTrace.Add($"[MoveAfterRandom] group:{c.RandomGroup} #{c.Text}");

        var pl = g.CurrPlayer;
        if (c.RandomGroup == 1)
        {
            g.FinishStep("_after_chest_card");
        }
        else if (c.RandomGroup == 2 && c.Pos == 10)
        {
            pl.MoveToJail();
            g.AddRoundMessage("мусора вас забрали в тюрьму!", "you went to police jain after chest card");
            g.FinishStep("_after_chest_card");
        }
        else if (c.RandomGroup == 2)
        {
            if (pl.Pos > c.Pos)
            {
                pl.Money += 2000;
                g.AddRoundMessage("вы прошли старт и получили $2000", "you passed start and got $2000");
            }
            pl.Pos = c.Pos;
            ProcessPosition(g);
        }
        else if (c.RandomGroup == 3)
        {
            if (pl.Pos > 3) pl.Pos -= 3;
            ProcessPosition(g);
        }
        else
        {
            //g.AddRoundMessage($"неизвестная карточка {c.Text}", $"undefined card {c.Text}");
            g.FinishStep("_no_move_chest_card_");
        }
    }
}
