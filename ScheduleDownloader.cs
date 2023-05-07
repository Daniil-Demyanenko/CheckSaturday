using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;

namespace CheckSaturday;

public static class ScheduleDownloader
{
    /// <summary>
    /// Путь к дирректирии с расписаниями
    /// </summary>
    public static readonly string CacheDir = AppDomain.CurrentDomain.BaseDirectory + "/Cache"; //Путь к папке Cache в директории программы
    private static readonly HttpClient _client = new HttpClient();
    private static string[] _sceduleNames = { "/ИФМОИОТ_ОФО_БАК.", "/ИФМОИОТ_ЗФО_БАК.", "/ИФМОИОТ_ОФО_МАГ.", "/ИФМОИОТ_ЗФО_МАГ." };

    /// <summary>
    /// Скачивает расписания, если они могли устареть
    /// </summary>
    /// <returns>true, если обновил расписания</returns>
    public static bool CheckUpdate()
    {
        if (CacheIsRelevant()) return false;
        
        Download();
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
            if ((DateTime.Now - fileWriteDate).TotalHours > 4) return false;
        }

        return true;

    }

    private static void Download()
    {
        var parsedLinks = ParseLinkFromPage();
        foreach (var item in parsedLinks)
        {
            (string name, string link) = GetDirectLinkAsync(item.Item2).Result;
            Console.WriteLine($"Обновление файла: {name}\n{link}");
            string extension = name.Split('.')[^1]; // получаем расширение файла (xls или xlsx)
            string filePath = CacheDir + item.Item1 + extension; // генерируем имя скачанного файла
            DownloadFromDirectLinkAsync(link, filePath).Wait(); // ждём окончания загрузки
        }
    }

    private static List<(string, string)> ParseLinkFromPage()
    {
        var link = LoadPage(@"https://lgpu.org/elektronnyy-resurs-distancionnogo-obucheniya-ifmit.html");
        var document = new HtmlDocument();
        document.LoadHtml(link);
        List<(string, string)> links = new();
        for (int i = 0; i < 4; i++) //Перебираем со 2 по 5 ячейку в таблице расписаний
        {
            HtmlNodeCollection xpathLink = document.DocumentNode.SelectNodes($"//tr[3]/td[{i + 2}]/p/a");
            if (xpathLink != null)
            {
                links.Add((_sceduleNames[i], xpathLink[0].GetAttributeValue("href", "")));
                continue;
            }

        }

        return links;
    }

    private static string LoadPage(string url) //Тут происходит какая то чёрная магия, в которой я не разобрался. Оно работает - и хуй с ним
    {
        var result = "";
        var request = (HttpWebRequest)WebRequest.Create(url);
        using var response = (HttpWebResponse)request.GetResponse();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var receiveStream = response.GetResponseStream();
            if (receiveStream != null)
            {
                StreamReader readStream;
                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                result = readStream.ReadToEnd();
                readStream.Close();
            }
            response.Close();
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
        using WebClient webClient = new WebClient();
        await webClient.DownloadFileTaskAsync(new Uri(url), path);
    }
}
