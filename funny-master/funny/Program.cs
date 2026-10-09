using Microsoft.EntityFrameworkCore;
using funny.Data;
using funny.Logic;
using UsersProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IOldSexLogic, OldSexLogic>();

builder.Services.AddHttpClient("Users", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["UserService:Url"] ?? "http://localhost:8000/");
});
// Общий счётчик для колы, пиццы и обратной связи.
builder.Services.AddSingleton<IUsersProxy>(services => new UserProxy(
    services.GetRequiredService<IHttpClientFactory>().CreateClient("Users"),
    builder.Configuration["UserService:Role"] ?? "Client",
    TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("UserService:RequestIntervalSeconds", 60))));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Funny API V1");
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (HttpRequestException ex)
    {
        app.Logger.LogError(ex, "Ошибка обращения к сервису пользователей");
        context.Response.StatusCode = 502;
        await context.Response.WriteAsync("Сервис пользователей недоступен или отклонил запрос. Проверьте его запуск и роль Client.");
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync(ex.Message);
    }
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();



using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        if (!context.DigitalServices.Any())
        {
            var defaultServices = new[]
            {
                new funny.Models.DigitalService { Name = "Разработка Landing Page", Description = "Быстрый одностраничный сайт для конверсии", Price = 15000 },
                new funny.Models.DigitalService { Name = "Разработка Корпоративного сайта", Description = "Многостраничный сайт для вашей компании с админкой", Price = 45000 },
                new funny.Models.DigitalService { Name = "Настройка Яндекс.Директ", Description = "Контекстная реклама с гарантией целевых лидов", Price = 10000 },
                new funny.Models.DigitalService { Name = "SEO Оптимизация", Description = "Вывод вашего сайта в топ-10 поисковых систем", Price = 20000 },
                new funny.Models.DigitalService { Name = "Техническая поддержка 24/7", Description = "Мониторинг серверов и оперативное исправление багов", Price = 8000 }
            };

            context.DigitalServices.AddRange(defaultServices);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при автоматическом создании или заполнении БД.");
    }
}

app.Run();
