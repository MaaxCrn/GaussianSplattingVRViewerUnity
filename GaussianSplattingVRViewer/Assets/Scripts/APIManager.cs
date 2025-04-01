using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

class APIManager
{
    private static readonly string API_URL = "http://localhost:3000";

    public static async Task<Gaussian[]> GetGaussians()
    {
        string jsonResponse = await FetchApiDataAsync("/image");
        
        return DeserializeJson<Gaussian[]>(jsonResponse);
    }
    public static async Task<string> GetServerPath()
    {
        return await FetchApiDataAsync("/server/absolute-path");
    }

    public static async Task<string> FetchApiDataAsync(string endpoint)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("User-Agent", "C# App");

        try
        {
            HttpResponseMessage response = await client.GetAsync(API_URL + endpoint);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            throw new Exception("Erreur lors de l'exécution de la requête HTTP", e);
        }
    }
    private static T DeserializeJson<T>(string json)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        return (T)serializer.ReadObject(stream);
    }
}

[DataContract]
class Gaussian
{
    [DataMember(Name = "id")]
    public long Id { get; set; }

    [DataMember(Name = "name")]
    public string Name { get; set; }
    [DataMember(Name = "image")]
    public string Image { get; set; }
    [DataMember(Name = "plyDirectory")]
    public string PlyDirectory { get; set; }
    public int UserId { get; set; }
}