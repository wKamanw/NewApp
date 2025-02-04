using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing; // Для рисования
using SixLabors.ImageSharp.Processing;          // Для обработки изображений

// ================================
// Основной код (Program.cs)
// ================================

var builder = WebApplication.CreateBuilder(args);
var userRequestHistory = new Dictionary<string, List<string>>();
builder.Services.AddAuthorization();

bool CustomLifeTimeVolidator(DateTime? notBefore, DateTime? expires,
    SecurityToken securityToken, TokenValidationParameters validationParameters)
{
    if (expires == null)
        return false;

    return expires > DateTime.UtcNow;
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Authoptions.ISSUER,
            ValidateAudience = true,
            ValidAudience = Authoptions.AUDIENCE,
            ValidateLifetime = true,
            LifetimeValidator = CustomLifeTimeVolidator,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Authoptions.GetKey()
        };
    });

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

DBManager db = new DBManager();

app.MapGet("/", () => "Ask random to generate random value");

app.MapPost("/login", (string login, string password) =>
{
    if (!db.CheckUser(login, password))
        return Results.Unauthorized();

    var jwt = new JwtSecurityToken(
        issuer: Authoptions.ISSUER,
        audience: Authoptions.AUDIENCE,
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: new SigningCredentials(Authoptions.GetKey(), SecurityAlgorithms.HmacSha256)
    );

    var encodedToken = new JwtSecurityTokenHandler().WriteToken(jwt);
    var response = new
    {
        access_token = encodedToken,
        username = login
    };

    return Results.Ok(response);
});

app.MapPost("/SignUp", (string login, string password) =>
{
    if (db.AddUser(login, password))
        return Results.Ok($"User {login} created successfully.");
    else
        return Results.Problem("Failed to create user.");
});

app.MapPatch("/change-password", (string login, string oldPassword, string newPassword) =>
{
    Console.WriteLine($"Запрос на смену пароля для {login}");

    if (!db.CheckUser(login, oldPassword))
    {
        Console.WriteLine("Ошибка: старый пароль неверный.");
        return Results.Unauthorized();
    }

    if (!db.ChangePassword(login, newPassword))
    {
        Console.WriteLine("Ошибка: не удалось обновить пароль.");
        return Results.Problem("Не удалось изменить пароль.");
    }

    Console.WriteLine("Пароль изменён, создаём новый токен.");

    var jwt = new JwtSecurityToken(
        issuer: Authoptions.ISSUER,
        audience: Authoptions.AUDIENCE,
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: new SigningCredentials(Authoptions.GetKey(), SecurityAlgorithms.HmacSha256)
    );

    var encodedToken = new JwtSecurityTokenHandler().WriteToken(jwt);
    var response = new
    {
        access_token = encodedToken,
        username = login
    };

    return Results.Ok(response);
});

app.MapPost("/primes/count", [Authorize] (HttpContext context, int count) =>
{
    string userId = context.Request.Headers["User-ID"]; // Получаем User-ID из заголовков
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("User-ID заголовок обязателен.");

    if (count <= 0)
        return Results.BadRequest("Count должно быть положительным числом.");

    // Вычисляем простые числа через отдельный класс
    var calculator = new PrimeCalculator();
    var primes = calculator.GetNPrimes(count);

    // Сохраняем историю запросов через отдельный класс
    var historyManager = new HistoryManager(userRequestHistory);
    historyManager.SaveRequestHistory(userId, $"N простых чисел: {count}");

    return Results.Ok(primes);
});

// Получить список простых чисел от 1 до N (POST)
app.MapPost("/primes/range", [Authorize] (HttpContext context, int max) =>
{
    string userId = context.Request.Headers["User-ID"];
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("User-ID заголовок обязателен.");

    if (max < 2)
        return Results.BadRequest("Число N должно быть ≥ 2.");

    var calculator = new PrimeCalculator();
    var primes = calculator.GetPrimesUpToN(max);

    var historyManager = new HistoryManager(userRequestHistory);
    historyManager.SaveRequestHistory(userId, $"Простые числа до {max}");

    return Results.Ok(primes);
});

// Получить изображение (решето Эратосфена) от 1 до N в виде Base64 строки (POST)
app.MapPost("/primes/sieve", [Authorize] (HttpContext context, int max) =>
{
    string userId = context.Request.Headers["User-ID"];
    if (string.IsNullOrEmpty(userId))
        return Results.BadRequest("User-ID заголовок обязателен.");

    if (max < 2)
        return Results.BadRequest("Число N должно быть ≥ 2.");

    var sieveGenerator = new SieveGenerator();
    string base64Image = sieveGenerator.GenerateSieveImage(max);

    var historyManager = new HistoryManager(userRequestHistory);
    historyManager.SaveRequestHistory(userId, $"Решето от 1 до {max} (картинка)");

    return Results.Ok("data:image/png;base64," + base64Image);
});

// Получить историю запросов пользователя (GET)
app.MapGet("/history/{userId}", (string userId) =>
{
    if (userRequestHistory.ContainsKey(userId))
        return Results.Ok(userRequestHistory[userId]);

    return Results.NotFound("История запросов не найдена.");
});

const string DB_PATH = "/home/kaman/NewApp/NewApp/users.db";
if (!db.ConnectToDB(DB_PATH))
{
    Console.WriteLine("Failed to connect to " + DB_PATH);
    Console.WriteLine("Shutdown!");
    return;
}

app.MapDelete("/history/{userId}/clear", (string userId) =>
{
    if (userRequestHistory.ContainsKey(userId))
    {
        userRequestHistory.Remove(userId);
        return Results.Ok($"История запросов пользователя {userId} была очищена.");
    }
    return Results.NotFound("История запросов не найдена.");
});

app.Run();
db.Disconnect();

// ================================
// Классы для вычислений и вспомогательных функций
// ================================

public class PrimeCalculator
{
    /// <summary>
    /// Получение первых count простых чисел через решето Эратосфена.
    /// </summary>
    public List<int> GetNPrimes(int count)
    {
        if (count < 1) return new List<int>();

        // Оценка верхней границы для n-го простого числа (для n>=6, для меньших зададим вручную)
        int estimate = count < 6 ? 15 : (int)(count * (Math.Log(count) + Math.Log(Math.Log(count))));
        estimate += 10; // небольшой запас

        bool[] sieve = new bool[estimate + 1];
        for (int i = 2; i <= estimate; i++)
            sieve[i] = true;

        for (int i = 2; i * i <= estimate; i++)
        {
            if (sieve[i])
            {
                for (int j = i * i; j <= estimate; j += i)
                    sieve[j] = false;
            }
        }

        List<int> primes = new List<int>();
        for (int i = 2; i <= estimate && primes.Count < count; i++)
        {
            if (sieve[i])
                primes.Add(i);
        }
        return primes;
    }

    /// <summary>
    /// Получение списка простых чисел от 1 до max через решето Эратосфена.
    /// </summary>
    public List<int> GetPrimesUpToN(int max)
    {
        bool[] sieve = new bool[max + 1];
        for (int i = 2; i <= max; i++)
            sieve[i] = true;

        for (int i = 2; i * i <= max; i++)
        {
            if (sieve[i])
            {
                for (int j = i * i; j <= max; j += i)
                    sieve[j] = false;
            }
        }

        List<int> primes = new List<int>();
        for (int i = 2; i <= max; i++)
        {
            if (sieve[i])
                primes.Add(i);
        }
        return primes;
    }
}

public class SieveGenerator
{
    /// <summary>
    /// Генерация изображения-решета (решето Эратосфена) в виде строки Base64.
    /// </summary>
    public string GenerateSieveImage(int max)
    {
        // Создаём решето Эратосфена
        bool[] sieve = new bool[max + 1];
        for (int i = 2; i <= max; i++)
            sieve[i] = true;
        for (int i = 2; i * i <= max; i++)
        {
            if (sieve[i])
            {
                for (int j = i * i; j <= max; j += i)
                    sieve[j] = false;
            }
        }

        // Определяем размер сетки
        int gridSize = (int)Math.Ceiling(Math.Sqrt(max));
        int cellSize = 40;
        int imageSize = gridSize * cellSize;

        // Загружаем системный шрифт
        Font font = SystemFonts.CreateFont("DejaVu Sans", cellSize * 0.4f);

        using (var image = new Image<Rgba32>(imageSize, imageSize))
        {
            image.Mutate(ctx =>
            {
                ctx.Fill(Color.White);

                for (int n = 1; n <= max; n++)
                {
                    int index = n - 1;
                    int row = index / gridSize;
                    int col = index % gridSize;
                    int x = col * cellSize;
                    int y = row * cellSize;

                    // Цвет ячейки: для 1 – серый, для простых – светло-зелёный, иначе – белый
                    Color cellColor = n == 1 ? Color.Gray :
                                      sieve[n] ? Color.LightGreen : Color.White;

                    var rect = new Rectangle(x, y, cellSize, cellSize);
                    ctx.Fill(cellColor, rect);
                    ctx.Draw(Color.Black, 1, rect);

                    // Подготавливаем текст
                    string text = n.ToString();
                    var textOptions = new RichTextOptions(font)
                    {
                        Origin = new PointF(x + cellSize / 2, y + cellSize / 2),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    ctx.DrawText(textOptions, text, Color.Black);
                }
            });

            using (var ms = new MemoryStream())
            {
                image.SaveAsPng(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}

public class HistoryManager
{
    private readonly Dictionary<string, List<string>> _history;

    public HistoryManager(Dictionary<string, List<string>> history)
    {
        _history = history;
    }

    /// <summary>
    /// Сохранение информации о запросе пользователя.
    /// </summary>
    public void SaveRequestHistory(string userId, string requestInfo)
    {
        if (!_history.ContainsKey(userId))
            _history[userId] = new List<string>();

        _history[userId].Add($"Запрос: {requestInfo}, Дата: {DateTime.UtcNow}");
    }
}

// ================================
// Прочие типы и классы
// ================================

public struct RGResult
{
    public RGResult(int rv)
    {
        random_value = rv;
    }
    public int random_value { get; set; }
}

public class Authoptions
{
    public const string ISSUER = "WebAppTest";
    public const string AUDIENCE = "WebAppTestAudience";
    public static SymmetricSecurityKey GetKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "WebAppTestPassordWebAppTestPasswordWebAppTestPasswordWebAppTestPassord"));
    }
}



