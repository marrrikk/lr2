using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UsersProxy;

const string phone = "79991234567";
int passed = 0;
void Check(bool value, string description)
{
    if (!value) throw new Exception(description);
    Console.WriteLine("PASS: " + description);
    passed++;
}

var server = new FakeService();
var clock = new FakeClock();
var proxy = new UserProxy(new HttpClient(server) { BaseAddress = new Uri("http://users/") },
    "Client", TimeSpan.FromSeconds(60), clock);

Check(await proxy.AcceptRequestAsync("Иван", "+7 (999) 123-45-67"), "Первая заявка создаёт пользователя");
Check(server.Users[phone].Roles.SequenceEqual(["Client"]), "Назначена роль Client");
Check(await proxy.AcceptRequestAsync("Иван", phone), "Вторая заявка, тот же телефон в другом формате");
Check(await proxy.AcceptRequestAsync("Иван", phone), "Третья заявка разрешена");
server.Users[phone].ExtraFields["ownRequests"] = JsonSerializer.SerializeToElement(true);
Check(!await proxy.AcceptRequestAsync("Иван", phone), "Четвёртая заявка отклонена");
Check(server.Users[phone].Archive && server.Users[phone].Blocked && server.Updates == 1,
    "Блокировка отправлена через POST users/false");
Check(server.Users[phone].ExtraFields["ownRequests"].GetBoolean(), "Остальные поля пользователя сохранены");
Check(!await proxy.AcceptRequestAsync("Иван", phone) && server.Updates == 1, "Заблокированный пользователь не обслуживается");

server.Users[phone].Archive = false;
server.Users[phone].Blocked = false;
clock.Now += TimeSpan.FromSeconds(60);
Check(await proxy.AcceptRequestAsync("Иван", phone), "Пауза 60 секунд сбрасывает последовательность");
server.Users[phone].Archive = true;
Check(!await proxy.AcceptRequestAsync("Иван", phone), "Архивный пользователь отклонён");
server.Users[phone].Archive = false;
server.Users[phone].ExtraFields["deleted"] = JsonSerializer.SerializeToElement(true);
Check(!await proxy.AcceptRequestAsync("Иван", phone), "Удалённый пользователь отклонён");
server.Users.Remove(phone);
Check(!await proxy.AcceptRequestAsync("Иван", phone), "Исчезнувший известный пользователь не создан повторно");

var parallel = await Task.WhenAll(Enumerable.Range(0, 4)
    .Select(_ => proxy.AcceptRequestAsync("Мария", "79997654321")));
Check(parallel.Count(result => result) == 3, "Четыре одновременные заявки: приняты только три");

server.Fail = true;
try
{
    await proxy.AcceptRequestAsync("Олег", "79991112233");
    throw new Exception("Ошибка сервиса проигнорирована");
}
catch (HttpRequestException)
{
    Check(true, "Сбой смежного сервиса не считается успешной заявкой");
}
Console.WriteLine($"Всего проверок: {passed}");

class FakeClock : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 10, 9, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

class FakeService : HttpMessageHandler
{
    public Dictionary<string, User> Users { get; } = new();
    public int Updates { get; private set; }
    public bool Fail { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (Fail) return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        string path = request.RequestUri!.AbsolutePath.Trim('/');
        object? result;
        if (request.Method == HttpMethod.Post)
        {
            var user = (await request.Content!.ReadFromJsonAsync<User>(token))!;
            if (path == "users/false") Updates++;
            else if (path != "users/true") throw new Exception("Неверный адрес POST");
            Users[user.Id] = user;
            result = user;
        }
        else if (path == "roles") result = new[] { "Client" };
        else
        {
            string id = path.Split('/')[1];
            // Копия, чтобы изменения прокси не меняли сервер без POST.
            result = path.EndsWith("/exists") ? Users.ContainsKey(id) : Users.GetValueOrDefault(id);
        }
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(result) };
    }
}
