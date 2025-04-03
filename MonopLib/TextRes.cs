namespace MonopLib;

internal class TextRes
{
    public static string Get(string lang, string key) =>
        lang == "ru" ? GetRuText(key) : GetEnText(key);

    public static string GetRuText(string key) =>
        key switch
        {
            "_player_bought_cell" => "вы купили #{0} за ${1}",
            "_player_bought_cell_on_auction" => "{2} купил #{0} за ${1}",
            "_player_rolled" => "{0} выкинул {1}",
            "_you_visisted_cell" => "{0} вы попали на {1}",
            "_player_paid" => "{0} заплатили ${1}",
            "_player_paid_to_user" => "{0} заплатил ${1} игроку {2}",
            "_player_paid_to_exit_from_jail" => "игрок {0} заплатил $500, чтобы выйти из клетки",
            "_player_left_auction" => "{0} покинул аукцион",
            "_player_bid" => "{0} сделал ставку {1}",
            "_trade_completed" => "Сделка между {0} и {1} совершена, {0} отдал {2} и получил {3}",
            "_trade_rejected" => "Сделка отклонена между {0} и {1}, {0} планировал отдать {2} и получить {3}",
            "_random_took_card" => "вы потянули карточку ({0})",
            "_game_is_finished" => "игра закончена",
            "_player_build_houses" => "пострил дома {0}",

            _ => $"no key #{key}"
        };

    public static string GetEnText(string key) =>
        key switch
        {
            "_player_bought_cell" => "вы купили #{0} за ${1}",
            "_player_bought_cell_on_auction" => "{2} bought #{0} for ${1}",
            "_player_rolled" => "{0} rolled {1}",
            "_you_visisted_cell" => "{0} вы попали на {1}",
            "_you_paid" => "you paid ${0}",
            "_player_paid_to_user" => "{0} заплатили ${1} игроку {2}",
            "_you_paid_to_exit_from_jail" => "player {0} paid $500 and exited from jail",
            "_player_left_auction" => "{0} left auction",
            "_player_bid" => "{0} made a bid {1}",
            "_trade_completed" => "Trade completed between {0} and {1}, {0} gave {2} and got {3}",
            "_trade_rejected" => "Trade rejected between {0} and {1}, {0} планировал отдать {2} и получить {3}",
            "_random_took_card" => "вы потянули карточку ({0})",
            "_game_is_finished" => "game is finished",
            _ => $"no key #{key}"
        };
}
