using System.Text.Json;

namespace Ludamo.Cache.SourceGenerator.Demo.Console;

public partial class UserService : IUserService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [CacheDecorated]
    public async Task<string> GetUsersAsync()
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/users");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    [CacheDecorated(expiresInSeconds: 30)]
    public async Task<User?> GetUserAsync(int id)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync($"https://jsonplaceholder.typicode.com/users/{id}");
        response.EnsureSuccessStatusCode();

        var user = JsonSerializer.Deserialize<User>(await response.Content.ReadAsStringAsync(), JsonSerializerOptions);

        return user;
    }
}

public interface IUserService
{
    Task<string> GetUsersAsync();
    Task<User?> GetUserAsync(int id);
}

public record User
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Username { get; init; }
    public string Email { get; init; }
}
