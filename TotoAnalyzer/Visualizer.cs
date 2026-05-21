namespace TotoAnalyzer;

/// <summary>
/// Module 3 - Рендериране на данните: Този модул ще се занимава с визуализацията на статистическите данни, извлечени от тиражите. Ще се разработят функции за:
/// </summary>
public class Visualizer
{
    private const int BarMaxWidth = 40;

    // Bar Chart

    /// <summary>
    /// Рендерира хоризонтална бар диаграма за даден речник от числа и техните честоти. Числата се показват в лявата колона, а броят на появяванията им – в дясната.
    /// Барът е оцветен в зелено и мащабиран спрямо най-често срещаното число, за да се осигури визуално сравнение между различните числа. Заглавието на диаграмата се показва над нея.
    /// </summary>
    public void DrawBarChart(Dictionary<int, int> topNumbers, string title)
    {
        if (topNumbers.Count == 0)
        {
            Console.WriteLine("  No data to display.");
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
        Console.WriteLine(new string('-', BarMaxWidth + 20));

        int maxCount = topNumbers.Values.Max();

        foreach (var (number, count) in topNumbers)
        {
            int barLen = maxCount > 0 ? (int)Math.Round((double)count / maxCount * BarMaxWidth) : 0;
            string bar = new string('#', barLen);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {number,2}");
            Console.ResetColor();
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(bar.PadRight(BarMaxWidth));
            Console.ResetColor();
            Console.WriteLine($" {count}");
        }

        Console.WriteLine();
    }

    // Heat Map 7×7

    /// <summary>
    /// Рендерира 7×7 мрежа от числа от 1 до 49, оцветени според тяхната честота на появяване в тиражите. Честотата се определя от речник, където ключът е числото, а стойността е броят на появяванията му. Числата се оцветяват в три категории:
    /// Hot (top 30%): Red  |  Neutral (middle 40%): Yellow  |  Cold (bottom 30%): Cyan
    /// </summary>
    public void DrawHeatMap(Dictionary<int, int> allFrequencies, string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {title}");
        Console.ResetColor();

        // Сортираме честотите, за да определим праговете за топ 30% и долни 30%
        var sorted = allFrequencies.Values.OrderBy(v => v).ToList();
        int total = sorted.Count;

        int lowThreshold = sorted[(int)(total * 0.30)]; // bottom 30%
        int highThreshold = sorted[(int)(total * 0.70)]; // top 30%

        Console.WriteLine();
        Console.WriteLine("  Легенда: ");
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Red; Console.Write("■ Hot (top 30%)   ");
        Console.ForegroundColor = ConsoleColor.Yellow; Console.Write("■ Neutral (40%)   ");
        Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("■ Cold (bottom 30%)");
        Console.ResetColor();
        Console.WriteLine("\n");

        // Рисуваме мрежата 7×7 с оцветяване според честотата
        for (int row = 0; row < 7; row++)
        {
            Console.Write("  ");
            for (int col = 0; col < 7; col++)
            {
                int number = row * 7 + col + 1;
                int freq = allFrequencies.TryGetValue(number, out int f) ? f : 0;

                ConsoleColor color =
                    freq >= highThreshold ? ConsoleColor.Red :
                    freq <= lowThreshold ? ConsoleColor.Cyan :
                    ConsoleColor.Yellow;

                Console.ForegroundColor = color;
                Console.Write($"{number,3}");
                Console.ResetColor();
                Console.Write(" ");
            }
            Console.WriteLine();
        }

        Console.WriteLine();
    }

    // Decade Distribution Bar Chart 

    public void DrawDecadeChart(Dictionary<string, int> distribution, string title)
    {
        if (distribution.Count == 0)
        {
            Console.WriteLine("  No data to display.");
            return;
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
        Console.WriteLine(new string('-', BarMaxWidth + 20));

        int maxCount = distribution.Values.Max();

        foreach (var (label, count) in distribution)
        {
            int barLen = maxCount > 0 ? (int)Math.Round((double)count / maxCount * BarMaxWidth) : 0;
            string bar = new string('#', barLen);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {label,5}");
            Console.ResetColor();
            Console.Write(" | ");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(bar.PadRight(BarMaxWidth));
            Console.ResetColor();
            Console.WriteLine($" {count}");
        }

        Console.WriteLine();
    }

    // Hot Pairs Table

    public void DrawPairsTable(IEnumerable<(int Num1, int Num2, int Count)> pairs, string title)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
        Console.WriteLine(new string('-', 35));
        Console.WriteLine($"  {"#",4}  {"Pair",10}  {"Appearances",12}");
        Console.WriteLine(new string('-', 35));

        int rank = 1;
        foreach (var (n1, n2, count) in pairs)
        {
            Console.ForegroundColor = rank <= 3 ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.WriteLine($"  {rank,4}  {$"{n1} & {n2}",10}  {count,12}");
            Console.ResetColor();
            rank++;
        }

        Console.WriteLine();
    }

    // Helpers

    public static void PrintHeader()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  " + new string('=', 45));
        Console.WriteLine("              ТОТО АНАЛИЗАТОР              ");
        Console.WriteLine("  " + new string('=', 45));
        Console.ResetColor();
    }

    public static void WaitForKey()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n  Натиснете произволен клавиш за да се върнете в менюто...");
        Console.ResetColor();
        Console.ReadKey(true);
    }

    public static void PrintSeparator() =>
        Console.WriteLine("  " + new string('=', 45));
}
