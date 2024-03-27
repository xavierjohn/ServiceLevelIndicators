
Console.WriteLine("Generating SLI started.");
var t1 = ClientRequests();
var t2 = ClientRequests();
var t3 = ClientRequests();
await Task.WhenAll(t1, t2, t3);
Console.WriteLine("Generating SLI done.");

static async Task ClientRequests()
{
    string[] apiUrl = [
        "https://localhost:63936/hello-world?api-version=2023-08-06",
        "https://localhost:63936/hello-world/xavier?api-version=2023-08-06",
        "https://localhost:63936/hello-world/micheal?api-version=2023-08-06",
        "https://localhost:63936/hello-world/xavier?api-version=1996-06-06",
        "https://localhost:63936/hello-world/micheal?api-version=1996-06-06",
    ];

    Random rnd = new Random();
    using var httpClient = new HttpClient();
    for (var i = 1; i <= 200; i++)
    {
        try
        {
            await Task.Delay(rnd.Next(500, 3000));
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



