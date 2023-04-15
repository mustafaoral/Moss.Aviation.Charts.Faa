using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HtmlAgilityPack.CssSelectors.NetCore;
using Moss.Extensions;

namespace Moss.Aviation.Charts.Faa
{
    public partial class Program
    {
        private static readonly HttpClient _httpClient = new();
#if DEBUG
        private static readonly string _downloadPathEnvironmentVariable = "moss_aviation_charts_faa_download_path_debug";
#else
        private static readonly string _downloadPathEnvironmentVariable = "moss_aviation_charts_faa_download_path";
#endif

        static async Task Main(string[] args)
        {
            if (!TryGetDownloadPath(out var downloadPath))
            {
                return;
            };

            _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            if (!TryGetAirportId(args, out var id))
            {
                return;
            }

            var chartLinks = await GetChartLinks(id);
            DisplayChartLinks(chartLinks);

            if (!TryGetSelectedCharts(chartLinks, out var selectedChartLinks))
            {
                return;
            }

            await SaveSelectedCharts(downloadPath, id, selectedChartLinks);
        }

        private static bool TryGetDownloadPath(out string downloadPath)
        {
            downloadPath = null;

            var environmentVariableValue = Environment.GetEnvironmentVariable(_downloadPathEnvironmentVariable);

            if (environmentVariableValue == null)
            {
                Console.WriteLine($"Environment variable '{_downloadPathEnvironmentVariable}' missing");

                return false;
            }

            if (!Path.Exists(environmentVariableValue))
            {
#if DEBUG
                Directory.CreateDirectory(environmentVariableValue);
#else
                Console.WriteLine($"Path specified by environment variable '{_downloadPathEnvironmentVariable}' doesn't exist: {downloadPath}");

                return false;
#endif
            }

            downloadPath = environmentVariableValue;
            return true;
        }

        private static async Task SaveSelectedCharts(string downloadPath, string id, List<ChartLink> selectedChartLinks)
        {
            foreach (var chartLink in selectedChartLinks)
            {
                var directoryPath = Path.Combine(downloadPath, id);
                var path = Path.Combine(directoryPath, $"{GetPrefix(chartLink.Kind)} - {chartLink.Title}.pdf");

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                using (var stream = await _httpClient.GetStreamAsync(chartLink.Link))
                using (var fileStream = File.OpenWrite(path))
                {
                    await stream.CopyToAsync(fileStream);
                }
            }

            static string GetPrefix(string kind)
            {
                return kind switch
                {
                    "Root" => "root",
                    "Minimums" => "minimums",
                    "Standard Terminal Arrival (STAR) Charts" => "star",
                    "Departure Procedure (DP) Charts" => "dp",
                    "Obstacle Departure Procedures (ODP) Charts" => "odp",
                    "Instrument Approach Procedure (IAP) Charts" => "iap",
                    "Land and Hold-Short Operations (LAHSO)" => "lahso",
                    "Hot Spots" => "hot-spots",
                    _ => throw new ArgumentOutOfRangeException($"Chart kind '{kind}' not mapped to file prefix")
                };
            }
        }

        private static bool TryGetSelectedCharts(List<ChartLink> chartLinks, out List<ChartLink> selectedChartLinks)
        {
            selectedChartLinks = new List<ChartLink>();

            Console.Write("Select charts: ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var tokens = input.Split(" ");

            if (!tokens.All(x => NumberRegex().IsMatch(x)))
            {
                Console.WriteLine($"input contains non-numbers");

                return false;
            }

            var numbers = tokens.Select(int.Parse).ToArray();
            selectedChartLinks = chartLinks.IntersectBy(numbers, x => x.Number).ToList();

            return true;
        }

        private static async Task<List<ChartLink>> GetChartLinks(string id)
        {
            var html = await _httpClient.GetHtmlDocument(new Uri($"https://nfdc.faa.gov/nfdcApps/services/ajv5/airportDisplay.jsp?airportId={id}"));
            var i = 1;
            var chartLinks = new List<ChartLink>();

            chartLinks.AddRange(html.QuerySelectorAll("#charts > .chartLink a").Select(x => new ChartLink(i++, "Root", x.InnerText, x.Attributes["href"].Value)));

            foreach (var section in html.QuerySelectorAll("#charts .row"))
            {
                var title = section.QuerySelector("h3").InnerText;

                chartLinks.AddRange(section.QuerySelectorAll(".chartLink a").Select(x => new ChartLink(i++, title, x.InnerText, x.Attributes["href"].Value)));
            }

            return chartLinks;
        }

        private static void DisplayChartLinks(List<ChartLink> chartLinks)
        {
            foreach (var grouping in chartLinks.GroupBy(x => x.Kind))
            {
                var border = new string('=', grouping.Key.Length);
                Console.WriteLine(border);
                Console.WriteLine(grouping.Key);
                Console.WriteLine(border);

                foreach (var chartLink in grouping)
                {
                    var padding = grouping.Max(x => x.Number).GetNumberOfDigits();

                    Console.WriteLine($"{chartLink.Number.ToString().PadLeft(padding)}. {chartLink.Title}");
                }

                Console.WriteLine();
            };
        }

        private static bool TryGetAirportId(string[] args, out string airportIdentifier)
        {
            airportIdentifier = null;

            if (args.Length == 0)
            {
                Console.Write("Enter airport identifier: ");

                args = new string[] { Console.ReadLine() };

                Console.WriteLine();
            }

            var arg = args.Single().Trim();

            if (!IcaoIdentifierRegex().IsMatch(arg))
            {
                Console.WriteLine($"Airport identifier invalid");

                return false;
            }

            airportIdentifier = arg;

            return true;
        }

        [GeneratedRegex("^[a-z0-9]{4}$", RegexOptions.IgnoreCase)]
        private static partial Regex IcaoIdentifierRegex();

        [GeneratedRegex("^\\d+$")]
        private static partial Regex NumberRegex();
    }
}
