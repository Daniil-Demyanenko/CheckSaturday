using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace CheckSaturday.ScheduleProcessors;

public static class ScheduleDownloader
{
    /// <summary>
    /// Сколько часов расписания считаются актуальными
    /// </summary>
    public const int HoursCacheIsActual = 4;

    /// <summary>
    /// Путь к дирректирии с расписаниями
    /// </summary>
    public static readonly string CacheDir = AppDomain.CurrentDomain.BaseDirectory + "/Cache"; //Путь к папке Cache в директории программы

    private static readonly HttpClient _client = new HttpClient();
    private static readonly string[] _scheduleNames = { "/ИФМОИОТ_ОФО_БАК.", "/ИФМОИОТ_ЗФО_БАК.", "/ИФМОИОТ_ОФО_МАГ.", "/ИФМОИОТ_ЗФО_МАГ." };

    /// <summary>
    /// Скачивает расписания, если они могли устареть
    /// </summary>
    /// <returns>True, если обновил расписания</returns>
    public static async Task<bool> CheckUpdate()
    {
        if (CacheIsRelevant()) return false;

        await Download();
        return true;
    }

    private static bool CacheIsRelevant()
    {
        if (!Directory.Exists(CacheDir))
        {
            Directory.CreateDirectory(CacheDir);
            return false;
        }

        string[] dirFiles = Directory.GetFileSystemEntries(CacheDir);

        if (dirFiles.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < dirFiles.Length; i++)
        {
            var fileWriteDate = File.GetLastWriteTime(dirFiles[i]);
            if ((DateTime.Now - fileWriteDate).TotalHours >= HoursCacheIsActual) return false;
        }

        return true;
    }

    private static async Task Download()
    {
        var parsedLinks = await ParseLinkFromPage();
        foreach (var item in parsedLinks)
        {
            (string name, string link) = GetDirectLinkAsync(item.Item2).Result;
            Console.WriteLine($"DOWNLOADER >> Обновление файла: {name}\n{link}");

            string extension = name.Split('.')[^1]; // получаем расширение файла (xls или xlsx)
            string filePath = CacheDir + item.Item1 + extension; // генерируем имя скачанного файла

            DownloadFromDirectLinkAsync(link, filePath).Wait(); // ждём окончания загрузки
        }
    }

    private static async Task<List<(string, string)>> ParseLinkFromPage()
    {
        var link = await LoadPage(@"https://lgpu.org/elektronnyy-resurs-distancionnogo-obucheniya-ifmit.html");
        var document = new HtmlDocument();
        document.LoadHtml(link);
        List<(string, string)> links = new();
        for (int i = 2; i <= 5; i++) //Перебираем со 2 по 4 ячейку в таблице расписаний
        {
            HtmlNodeCollection xpathLink = document.DocumentNode.SelectNodes($"/html/body/div[2]/div[4]/div/div[2]/article/div[1]/table/tbody/tr[3]/td[{i}]/a");
            if (xpathLink != null)
            {
                links.Add((_scheduleNames[i - 2], xpathLink[0].GetAttributeValue("href", "")));
            }
        }

        return links;
    }

    private static async Task<string> LoadPage(string url)
    {
        string result = string.Empty;
        using HttpResponseMessage response = await _client.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            result = await response.Content.ReadAsStringAsync();
        }

        return result;
    }

    static async Task<(string, string)> GetDirectLinkAsync(string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://cloud-api.yandex.net/v1/disk/public/resources?public_key={url}");
        using var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        dynamic result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
        return (result.name, result.file);
    }

    private static async Task DownloadFromDirectLinkAsync(string url, string path)
    {
        using var httpClient = new HttpClient();

        try
        {
            using var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = File.Create(path);
            await stream.CopyToAsync(fileStream);
        }
        catch (Exception e)
        {
            Console.WriteLine($"DOWNLOADER >> Ошибка при загрузке файла: {e.Message}");
        }
    }
}