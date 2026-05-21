namespace TotoAnalyzer.Models;

/// <summary>
/// Изобразява информация за един тираж на Тото 6/49, включително номера на тиража, дата и изтеглените числа.
/// </summary>
public class Draw
{
    public int DrawNumber { get; set; }
    public DateTime Date { get; set; }
    public int[] Numbers { get; set; } = Array.Empty<int>();

    public override string ToString() =>
        $"Draw #{DrawNumber} ({Date:dd.MM.yyyy}): [{string.Join(", ", Numbers)}]";
}
