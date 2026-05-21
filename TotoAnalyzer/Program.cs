using TotoAnalyzer;
using TotoAnalyzer.Models;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Visualizer.PrintHeader();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  Добре дошли в Тото Анализатор!");
        Console.WriteLine("  Данните се зареждат от info.toto.bg (6x49)");
        Console.ResetColor();
        Console.WriteLine();

        var loader = new DataLoader();
        IEnumerable<Draw>? allDraws = null;
        IEnumerable<Draw>? filteredDraws = null;
        int fromYear = 0, toYear = 0;

        // Main Menu

        bool running = true;
        while (running)
        {
            Visualizer.PrintHeader();
            Console.WriteLine();

            if (filteredDraws != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  Активен период: {fromYear} – {toYear}  " +
                                  $"({filteredDraws.Count()} тиража)");
                Console.ResetColor();
                Console.WriteLine();
            }

            Visualizer.PrintSeparator();
            Console.WriteLine("  [1]  Избери период (от година – до година)");
            Console.WriteLine("  [2]  Топ N най-чести числа");
            Console.WriteLine("  [3]  Горещи двойки");
            Console.WriteLine("  [4]  Разпределение по десетици");
            Console.WriteLine();
            Console.WriteLine("  [0]  Изход");
            Visualizer.PrintSeparator();
            Console.Write("\n  Избор: ");

            ReadValidInt(out int choice);

            switch (choice)
            {
                case 1:
                    await HandleSelectPeriod();
                    break;
                case 2:
                    HandleTopN();
                    break;
                case 3:
                    HandleHotPairs();
                    break;
                case 4:
                    HandleDecadeDistribution();
                    break;
                case 0:
                    running = false;
                    Console.WriteLine("\n  Довиждане!");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n  Невалиден избор. Моля въведете число от менюто.");
                    Console.ResetColor();
                    Thread.Sleep(1200);
                    break;
            }
        }

        // Handlers

        async Task HandleSelectPeriod()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  ── Избор на период ──");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("  От година (напр. 2010): ");
            if (!TryReadYear(out int from)) { InvalidInput(); return; }

            Console.Write("  До година  (напр. 2026): ");
            if (!TryReadYear(out int to)) { InvalidInput(); return; }

            if (from > to)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n  Грешка: началната година е след крайната.");
                Console.ResetColor();
                Thread.Sleep(1500);
                return;
            }

            fromYear = from;
            toYear = to;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Зареждане на данни за периода {from}–{to}...");
            Console.ResetColor();
            Console.WriteLine();

            if (allDraws == null)
            {
                allDraws = (await loader.LoadAllDrawsAsync(from, to)).ToList();
            }
            else
            {
                var existing = allDraws.Select(d => d.Date.Year).Distinct().ToHashSet();
                var needed = Enumerable.Range(from, to - from + 1).Where(y => !existing.Contains(y)).ToList();

                if (needed.Count > 0)
                {
                    Console.WriteLine($"  Зареждане на допълнителни данни...");
                    var extra = (await loader.LoadAllDrawsAsync(needed.Min(), needed.Max())).ToList();
                    allDraws = allDraws.Concat(extra).ToList();
                }
            }

            filteredDraws = allDraws.Where(d => d.Date.Year >= from && d.Date.Year <= to).ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  Заредени {filteredDraws.Count()} тиража за периода {from}–{to}.");
            Console.ResetColor();
            Thread.Sleep(1500);
        }

        void HandleTopN()
        {
            if (!EnsurePeriodSelected()) return;

            Console.WriteLine();
            Console.Write("  Колко числа да покажа (N)? ");
            if (!TryReadPositiveInt(out int n)) { InvalidInput(); return; }

            var stats = new Statistics(filteredDraws!);
            var top = stats.TopNFrequent(n);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Честотата на най-срещаните числа в избрания период.");
            Console.ResetColor();

            new Visualizer().DrawBarChart(top, $"Топ {n} най-чести числа ({fromYear}–{toYear})");
            Visualizer.WaitForKey();
        }

        void HandleHotPairs()
        {
            if (!EnsurePeriodSelected()) return;

            Console.WriteLine();
            Console.Write("  Колко двойки да покажа (N)? ");
            if (!TryReadPositiveInt(out int n)) { InvalidInput(); return; }

            var stats = new Statistics(filteredDraws!);
            var pairs = stats.HotPairs(n).ToList();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Двойки числа, появявали се заедно в един тираж най-много пъти.");
            Console.ResetColor();

            new Visualizer().DrawPairsTable(pairs, $"Топ {n} горещи двойки ({fromYear}–{toYear})");
            Visualizer.WaitForKey();
        }

        void HandleDecadeDistribution()
        {
            if (!EnsurePeriodSelected()) return;

            var stats = new Statistics(filteredDraws!);
            var dist = stats.DecadeDistribution();
            var freq = stats.AllFrequencies();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Групира всички изтеглени числа по диапазони и показва топлинна карта.");
            Console.ResetColor();

            var viz = new Visualizer();
            viz.DrawDecadeChart(dist, $"Разпределение по десетици ({fromYear}–{toYear})");
            viz.DrawHeatMap(freq, $"Топлинна карта 7×7 ({fromYear}–{toYear})");
            Visualizer.WaitForKey();
        }

        // Utilities

        bool EnsurePeriodSelected()
        {
            if (filteredDraws != null) return true;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  Моля, първо изберете период [1] преди да продължите.");
            Console.ResetColor();
            Thread.Sleep(1800);
            return false;
        }

        void InvalidInput()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  Невалидна стойност. Моля въведете цяло число.");
            Console.ResetColor();
            Thread.Sleep(1500);
        }

        bool TryReadYear(out int year)
        {
            year = 0;
            var line = Console.ReadLine()?.Trim();
            return int.TryParse(line, out year) && year >= 1990 && year <= DateTime.Now.Year;
        }

        bool TryReadPositiveInt(out int value)
        {
            value = 0;
            var line = Console.ReadLine()?.Trim();
            return int.TryParse(line, out value) && value > 0;
        }

        void ReadValidInt(out int value)
        {
            value = -1;
            var line = Console.ReadLine()?.Trim();
            int.TryParse(line, out value);
        }
    }
}