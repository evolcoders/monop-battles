using System.Text;
using MonopLib;

namespace MonopConsole;

public class MapPrinter
{
    static StringBuilder[] ReadMap() => File.ReadAllLines($"map.txt")
        .Select(s => new StringBuilder(s)).ToArray();

    static char[] Marks = new[] { '★', '☢', '☤', '☭' };

    static void Print(string line) => Console.WriteLine(line);

    public static void PrintMap(Game g)
    {
        var map = ReadMap();

        foreach (var pl in g.Players)
        {
            var (r, c) = GetCell(pl.Pos);
            map[r-1][c - pl.Id-1] = Marks[pl.Id];
        }
        foreach (var mapLine in map)
        {
            Print(mapLine.ToString());
        }
    }

    private static (int, int) GetCell(int p)
    {
        // return p switch
        // {
        //     < 11 => (1, 4 * (p + 1)),
        //     < 21 => (2 * p - 19, 44),
        //     < 31 => (21, 4 * (31 - p)),
        //     < 40 => (21 - (p - 30) * 2, 4),
        //     _ => (0, 0)
        // };

        return p switch
        {
            < 11 => (3, 6*(p + 1)),
            < 21 => (3+ 3*(p - 10), 65),
            < 31 => (33, 6*(31 - p)-1),
            < 40 => (27 - 3*(p - 31), 6),
            _ => (2, 2)
        };

    }



    public static void PrintGameInfo(Game g)
    {

        Print("***************************");
        foreach (var pl in g.Players)
        {
            var cells = g.Map.CellsByUser(pl.Id);
            var cellsLine = string.Join(",", cells.Select(c => c.Id));
            var line = $"{pl.Name}(${pl.Money}) {cellsLine}";
            Print(line);
        }
        Print("***************************");
    }

    public static void PrintGameInfo2(Game g)
    {

        Print("***************************");
        var grouppedCells = g.Map.CellsByType(1).GroupBy(c => c.Group);
        var arrCells = grouppedCells.Select(gr => string.Join(",", gr.Select(c => c.Owner.HasValue?c.Owner.ToString() : "#")));
        var cells = string.Join(",", arrCells.Select(ar => $"[{ar}]"));
        Print(cells);
        Print("***************************");
    }

}
