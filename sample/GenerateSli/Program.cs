
Console.WriteLine("Generating SLI started.");
var t1 = ClientRequests();
var t2 = ClientRequests();
var t3 = ClientRequests();
await Task.WhenAll(t1, t2, t3);
Console.WriteLine("Generating SLI done.");

static async Task ClientRequests()
{
    string[] apiUrl = [
    "https://localhost:63936/weather-forecast?api-version=2023-06-06",
    "https://localhost:63936/weather-forecast/MyAction1?api-version=2023-06-06",
    "https://localhost:63936/weather-forecast/get-by-city/Redmond?api-version=2023-06-06"
    ];

    using var httpClient = new HttpClient();
    for (var i = 1; i <= 200; i++)
    {
        Random rnd = new Random();
        try
        {
            var response = await httpClient.GetAsync(apiUrl[rnd.Next(apiUrl.Length)]);

            if (response.IsSuccessStatusCode)
                await response.Content.ReadAsStringAsync();
            else
                Console.WriteLine($"Request {i}: Failed with status code {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request {i}: Exception occurred: {ex.Message}");
        }
    }
}



