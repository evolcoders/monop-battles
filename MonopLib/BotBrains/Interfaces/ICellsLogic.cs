namespace MonopLib.BotBrains.Interfaces;

public interface ICellsLogic
{
    void BuildHouses(int spentSum, int group = 0);
    bool MortgageSell(Player p, int amount);
    void SellHouses(Player p, int needAmount);
    List<int> UnmortgageSell();
}
