using TotoAnalyzer.Models;

namespace TotoAnalyzer;

/// <summary>
/// Module 2 - Статистика и анализ на данните: Този модул ще се фокусира върху извличането на статистически данни от тиражите. Ще се разработят функции за:
/// </summary>
public class Statistics
{
    private readonly IEnumerable<Draw> _draws;

    public Statistics(IEnumerable<Draw> draws)
    {
        _draws = draws;
    }

    // Top N most frequent numbers 

    /// <summary>
    /// Връща най-често срещаните N числа в тиражите, заедно с броя на появяванията им. Резултатът е речник, където ключът е числото, а стойността е броят на появяванията му,
    /// подреден по низходящ ред на честотата. Ако има по-малко от N уникални числа, връща всички тях. Например, ако N=5, може да върне нещо като { 7: 150, 3: 140, 12: 130, 25: 120, 42: 110 }.
    /// </summary>
    public Dictionary<int, int> TopNFrequent(int n)
    {
        return _draws
            .SelectMany(d => d.Numbers)
            .GroupBy(num => num)
            .OrderByDescending(g => g.Count())
            .Take(n)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    // Hot pairs 

    /// <summary>
    /// Намира най-често срещаните N двойки числа, които са били изтеглени
    /// в един и същи тираж. Връща списък от кортежи, където всеки кортеж съдържа първото число, второто число и броя на появяванията на тази двойка. Двойките са подредени по низходящ ред на честотата. Например, ако N=3, може да върне нещо като [ (7, 12, 50), (3, 25, 45), (12, 42, 40) ].
    /// </summary>
    public IEnumerable<(int Num1, int Num2, int Count)> HotPairs(int n)
    {
        return _draws
            .SelectMany(d =>
                from a in d.Numbers
                from b in d.Numbers
                where a < b
                select (a, b))
            .GroupBy(pair => pair)
            .OrderByDescending(g => g.Count())
            .Take(n)
            .Select(g => (g.Key.a, g.Key.b, g.Count()));
    }

    // Диаграма на разпределението по десетки (1-10, 11-20, 21-30, 31-40, 41-49)

    /// <summary>
    /// Групира изтеглените числа в диапазони (1-10, 11-20, 21-30, 31-40, 41-49) и брои колко пъти всяко число от тези диапазони е било изтеглено. Връща речник, където ключът е етикетът на диапазона (напр. "1-10"), а стойността е общият брой появявания на числата от този диапазон. Например, може да върне нещо като { "1-10": 500, "11-20": 450, "21-30": 400, "31-40": 350, "41-49": 300 }.
    /// Връща резултата в подреден вид, така че диапазоните да са в логичен ред (1-10, 11-20, 21-30, 31-40, 41-49), дори ако някои от тях нямат изтеглени числа (в този случай броят ще бъде 0). Например, ако няма изтеглени числа от диапазона 31-40, резултатът може да изглежда така: { "1-10": 500, "11-20": 450, "21-30": 400, "31-40": 0, "41-49": 300 }.
    /// </summary>
    public Dictionary<string, int> DecadeDistribution()
    {
        var labels = new[] { "1-10", "11-20", "21-30", "31-40", "41-49" };

        return _draws
            .SelectMany(d => d.Numbers)
            .GroupBy(num =>
                num <= 10 ? "1-10" :
                num <= 20 ? "11-20" :
                num <= 30 ? "21-30" :
                num <= 40 ? "31-40" : "41-49")
            .OrderBy(g => labels.ToList().IndexOf(g.Key))
            .ToDictionary(g => g.Key, g => g.Count());
    }

    // Frequency map for all 49 numbers

    /// <summary>
    /// Връща речник, който показва колко пъти всяко от числата от 1 до 49 е било изтеглено в тиражите. Ключът на речника е числото (от 1 до 49), а стойността е броят на появяванията му. Резултатът включва всички числа от 1 до 49, дори ако някои от тях не са се появили в тиражите (в този случай броят ще бъде 0). Например, може да върне нещо като { 1: 120, 2: 110, ..., 49: 90 }.
    /// </summary>
    public Dictionary<int, int> AllFrequencies()
    {
        var freq = _draws
            .SelectMany(d => d.Numbers)
            .GroupBy(n => n)
            .ToDictionary(g => g.Key, g => g.Count());

        // Подсигуряваме, че всички числа от 1 до 49 са включени в резултата, дори ако не са се появили в тиражите (тогава честотата ще бъде 0).
        for (int i = 1; i <= 49; i++)
            if (!freq.ContainsKey(i))
                freq[i] = 0;

        return freq;
    }

    // Helper: размер на набора от тиражи и диапазон от дати

    public int DrawCount() => _draws.Count();

    public (DateTime Min, DateTime Max) DateRange()
    {
        var dates = _draws.Select(d => d.Date).ToList();
        return dates.Count == 0
            ? (DateTime.MinValue, DateTime.MaxValue)
            : (dates.Min(), dates.Max());
    }
}
