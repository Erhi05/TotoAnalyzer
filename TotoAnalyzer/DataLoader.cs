using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using TotoAnalyzer.Models;

namespace TotoAnalyzer;

/// <summary>
/// Module 1 - Downloads and parses all available draw files from info.toto.bg.
/// Handles both TXT and DOCX formats using only standard .NET libraries.
/// DOCX parsing uses System.IO.Compression + System.Xml (DOCX is a ZIP containing XML).
/// </summary>
public class DataLoader
{
    private const string BaseUrl = "https://info.toto.bg/statistika/6x49";

    // Файлът с най-старите тиражи (2010-2014) е в DOCX формат, а по-новите са в TXT.
    private static readonly (int Year, string Filename, bool IsDocx)[] KnownFiles =
    {
        (2024, "6from49draws2024.txt",  false),
        (2023, "6from49draws2023.txt",  false),
        (2022, "6from49draws2022.txt",  false),
        (2021, "6from49draws2021.txt",  false),
        (2020, "6from49draws2020.txt",  false),
        (2019, "6from49draws2019.txt",  false),
        (2018, "6from49draws2018.txt",  false),
        (2017, "6from49draws2017.txt",  false),
        (2016, "6from49draws2016.txt",  false),
        (2015, "6from49draws2015.txt",  false),
        (2014, "6from49draws2014.docx", true),
        (2013, "6from49draws2013.docx", true),
        (2012, "6from49draws2012.docx", true),
        (2011, "6from49draws2011.docx", true),
        (2010, "6from49draws2010.docx", true),
    };

    private readonly HttpClient _httpClient;

    public DataLoader()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _httpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            "Mozilla/5.0 (compatible; TotoAnalyzer/1.0)");
    }

    /// <summary>
    /// Зарежда всички тиражи от info.toto.bg в зададения период.
    /// Връща списък от Draw обекти, сортирани по дата. Използва асинхронни HTTP заявки.
    /// </summary>
    public async Task<IEnumerable<Draw>> LoadAllDrawsAsync(int fromYear = 2000, int toYear = 9999)
    {
        var allDraws = new List<Draw>();
        var fileLinks = await DiscoverFileLinksAsync();

        var filtered = fileLinks
            .Where(f => f.Year >= fromYear && f.Year <= toYear)
            .ToList();

        Console.WriteLine($"  Намерени {filtered.Count} файл(а) за изтегляне...");

        foreach (var (year, url, isDocx) in filtered)
        {
            try
            {
                Console.Write($"  Изтегляне {year}... ");
                var bytes = await _httpClient.GetByteArrayAsync(url);
                Console.Write($"{bytes.Length / 1024}KB – Парсване... ");

                List<Draw> draws = isDocx
                    ? ParseDocx(bytes, year)
                    : ParseTxt(Encoding.UTF8.GetString(bytes), year);

                allDraws.AddRange(draws);
                Console.WriteLine($"{draws.Count} тиража.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Грешка ({ex.Message})");
            }
        }

        return allDraws.OrderBy(d => d.Date);
    }

    // Link discovery 

    private async Task<List<(int Year, string Url, bool IsDocx)>> DiscoverFileLinksAsync()
    {
        var results = new List<(int Year, string Url, bool IsDocx)>();

        try
        {
            Console.Write("  Зареждане на списъка с файлове от toto.bg... ");
            var html = await _httpClient.GetStringAsync(BaseUrl);
            Console.WriteLine("OK");

            // Гледаме за href, който съдържа "6x49" и завършва на .txt или .docx
            var hrefPattern = new Regex(
                @"href=""([^""]*6[xх]?49[^""]*\.(txt|docx))""",
                RegexOptions.IgnoreCase);

            foreach (Match m in hrefPattern.Matches(html))
            {
                var href = m.Groups[1].Value;
                var ext = m.Groups[2].Value.ToLower();
                var fullUrl = href.StartsWith("http")
                    ? href
                    : "https://info.toto.bg" + (href.StartsWith("/") ? href : "/" + href);

                var yearMatch = Regex.Match(href, @"20(\d{2})");
                if (yearMatch.Success && int.TryParse("20" + yearMatch.Groups[1].Value, out int year))
                    results.Add((year, fullUrl, ext == "docx"));
            }
        }
        catch
        {
            Console.WriteLine("Не може да се зареди страницата. Използват се познати URL адреси.");
        }

        // Fallback: ако не сме намерили нищо, използваме твърдо кодирания списък
        if (results.Count == 0)
        {
            foreach (var (year, filename, isDocx) in KnownFiles)
                results.Add((year, $"{BaseUrl}/{filename}", isDocx));
        }

        return results
            .GroupBy(r => r.Year)
            .Select(g => g.First())
            .OrderByDescending(r => r.Year)
            .ToList();
    }

    // TXT Parser 

    /// <summary>
    /// Парсва текстовото съдържание на тиражите. Очаква се всеки ред да съдържа дата и 6 числа.
    /// Поддържа различни формати на датата (например 1.1.2020, 01-01-2020, 1/1/20 и т.н.) и различни разделители между числата.
    /// </summary>
    private List<Draw> ParseTxt(string content, int year)
    {
        var draws = new List<Draw>();

        // Гледаме за редове, които може да започват с номер на тираж (опционално), следван от дата и 6 числа.
        var linePattern = new Regex(
            @"(?:(\d+)\s+)?(\d{1,2}[.\-\/]\d{1,2}[.\-\/]\d{2,4})\s+((?:\d{1,2}\s+){5}\d{1,2})",
            RegexOptions.IgnoreCase);

        int autoNum = 1;
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            var m = linePattern.Match(line);
            if (!m.Success) continue;

            if (!TryParseDate(m.Groups[2].Value, out var date)) continue;

            var nums = m.Groups[3].Value
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Take(6)
                .Select(n => int.TryParse(n, out int v) ? v : 0)
                .Where(n => n >= 1 && n <= 49)
                .ToArray();

            if (nums.Length != 6) continue;

            draws.Add(new Draw
            {
                DrawNumber = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : autoNum,
                Date = date,
                Numbers = nums
            });
            autoNum++;
        }

        // Фallback: ако не сме намерили нищо, търсим всеки ред с 6 различни числа в диапазона 1-49, без значение от формата.
        if (draws.Count == 0)
            draws.AddRange(FallbackParse(content, year));

        return draws;
    }

    /// <summary>
    /// Подържа се като последна мярка, ако стандартният парсинг не намери нищо. Търси всеки ред, който съдържа 6 различни числа от 1 до 49, без значение от формата на датата или други текстове.
    /// </summary>
    private static List<Draw> FallbackParse(string content, int year)
    {
        var draws = new List<Draw>();
        int drawNum = 1;

        foreach (var line in content.Split('\n'))
        {
            var nums = Regex.Matches(line, @"\b([1-9]|[1-3]\d|4[0-9])\b")
                .Select(m => int.Parse(m.Value))
                .Distinct()
                .Take(6)
                .ToArray();

            if (nums.Length == 6)
            {
                draws.Add(new Draw
                {
                    DrawNumber = drawNum++,
                    Date = new DateTime(year, 1, 1),
                    Numbers = nums
                });
            }
        }

        return draws;
    }

    // ── DOCX Parser ────────────────────────────────────────────────────────────

    /// <summary>
    /// Парсва DOCX файл, като извлича текста от word/document.xml. Търси всички текстови възли (w:t) и ги събира в един голям текст, който след това се подава на TXT парсера.
    /// </summary>
    private List<Draw> ParseDocx(byte[] bytes, int year)
    {
        try
        {
            using var memStream = new MemoryStream(bytes);
            using var zip = new ZipArchive(memStream, ZipArchiveMode.Read);

            var docEntry = zip.GetEntry("word/document.xml");
            if (docEntry == null) return new List<Draw>();

            using var docStream = docEntry.Open();
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(docStream);

            // Namespaces
            var ns = new XmlNamespaceManager(xmlDoc.NameTable);
            ns.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

            // Всички текстови възли (w:t) се събират в един голям текст. Между тях се добавя интервал, а между параграфите – нов ред.
            var textBuilder = new StringBuilder();
            var textNodes = xmlDoc.SelectNodes("//w:t", ns);

            if (textNodes != null)
            {
                foreach (XmlNode node in textNodes)
                {
                    textBuilder.Append(node.InnerText);
                    textBuilder.Append(' ');
                }
            }

            // Параграфите (w:p) се използват за разделяне на тиражите. Ако има такива, използваме ги, за да добавим нов ред между тях.
            var paraNodes = xmlDoc.SelectNodes("//w:p", ns);
            if (paraNodes != null)
            {
                var lines = new StringBuilder();
                foreach (XmlNode para in paraNodes)
                {
                    var paraText = string.Join(" ",
                        para.SelectNodes(".//w:t", ns)!
                            .Cast<XmlNode>()
                            .Select(n => n.InnerText));
                    lines.AppendLine(paraText);
                }
                return ParseTxt(lines.ToString(), year);
            }

            return ParseTxt(textBuilder.ToString(), year);
        }
        catch (Exception ex)
        {
            Console.Write($"[DOCX parse error: {ex.Message}] ");
            return new List<Draw>();
        }
    }

    // Helpers

    private static bool TryParseDate(string raw, out DateTime date)
    {
        date = default;
        raw = raw.Replace('/', '.').Replace('-', '.');
        var formats = new[]
        {
            "d.M.yyyy", "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy",
            "d.M.yy",   "dd.MM.yy"
        };
        return DateTime.TryParseExact(raw, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out date);
    }
}
