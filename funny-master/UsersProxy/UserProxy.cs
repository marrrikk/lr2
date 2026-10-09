using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace UsersProxy;

public class UserProxy : IUsersProxy
{
    private readonly HttpClient _http;
    private readonly string _role;
    private readonly TimeSpan _interval;
    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<string, RequestState> _requests = new();

    public UserProxy(HttpClient http, string role, TimeSpan interval, TimeProvider? clock = null)
    {
        _http = http;
        _role = role;
        _interval = interval;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<bool> AcceptRequestAsync(string name, string phone, CancellationToken cancellationToken = default)
    {
        // +7 (999) 123-45-67 и 79991234567 обозначают одного клиента.
        string id = new(phone.Where(char.IsAsciiDigit).ToArray());
        if (string.IsNullOrWhiteSpace(name) || id.Length < 7 || id.Length > 15)
            throw new ArgumentException("Укажите имя и телефон (от 7 до 15 цифр).");

        var state = _requests.GetOrAdd(id, _ => new RequestState());
        // Общая очередь клиента для всех трёх видов заявок.
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            bool exists = await _http.GetFromJsonAsync<bool>($"users/{id}/exists", cancellationToken);
            User? user;
            if (exists)
            {
                user = await _http.GetFromJsonAsync<User>($"users/{id}", cancellationToken);
                if (user == null || user.Archive || user.Blocked || user.IsDeleted())
                    return false;
            }
            else
            {
                // Исчезнувший известный пользователь не создаётся заново.
                if (state.KnownUser) return false;
                var roles = await _http.GetFromJsonAsync<string[]>("roles", cancellationToken);
                if (roles == null || !roles.Contains(_role))
                    throw new HttpRequestException($"Создайте роль '{_role}' в Swagger сервиса пользователей.");

                string[] parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                user = new User
                {
                    Id = id,
                    FirstName = parts.Length > 1 ? parts[1] : parts[0],
                    Surname = parts.Length > 1 ? parts[0] : "Не указана",
                    MiddleName = parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : "Не указано",
                    PhoneNumber = phone.Trim(),
                    Roles = [_role],
                    Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!"
                };
                using var created = await _http.PostAsJsonAsync("users/true", user, cancellationToken);
                created.EnsureSuccessStatusCode();
            }

            state.KnownUser = true;
            var now = _clock.GetUtcNow();
            state.Count = now - state.LastRequest < _interval ? state.Count + 1 : 1;
            state.LastRequest = now;
            if (state.Count > 3)
            {
                user.Archive = true;
                user.Blocked = true;
                user.Password = ""; // Пустой пароль не меняет существующий.
                user.NewId = ""; // Не меняем логин.
                // Сервис возвращает null, но при POST требует непустые ссылки на строки.
                user.Email ??= "";
                user.Department ??= "";
                user.Organization ??= "";
                using var updated = await _http.PostAsJsonAsync("users/false", user, cancellationToken);
                updated.EnsureSuccessStatusCode();
                return false;
            }
            return true;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private class RequestState
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public DateTimeOffset LastRequest { get; set; }
        public int Count { get; set; }
        public bool KnownUser { get; set; }
    }
}
