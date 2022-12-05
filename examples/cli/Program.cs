// Example usage:
//     dotnet run http://localhost:5200
if (args.Length != 1)
{
    Console.WriteLine(@"URL missing");
    return 2;
}

var uri = args[0];
using var httpClient = new HttpClient();
var content = await httpClient.GetStringAsync(uri);
Console.WriteLine(content);
return 0;
