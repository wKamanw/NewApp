using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientApp
{
    public struct Token
    {
        public required string access_token { get; set; }
        public required string username { get; set; }
    }

    class Program
    {
        static HttpClient client = new HttpClient();
        static Token? token = null;

        static async Task Main(string[] args)
        {
            const string DEFAULT_SERVER_URL = "http://localhost:5000";
            Console.WriteLine("Введите URL сервера (по умолчанию: http://localhost:5000):");
            string? server_url = Console.ReadLine();
            if (string.IsNullOrEmpty(server_url))
                server_url = DEFAULT_SERVER_URL;
            client.BaseAddress = new Uri(server_url);

            try
            {
                while (true)
                {
                    Console.WriteLine("\nВыберите действие:");
                    Console.WriteLine("1 - Войти");
                    Console.WriteLine("2 - Зарегистрироваться");
                    Console.WriteLine("3 - Изменить пароль");
                    Console.WriteLine("4 - Получить простые числа");
                    Console.WriteLine("5 - Получить простые числа в диапазоне");
                    Console.WriteLine("6 - Посмотреть историю запросов");
                    Console.WriteLine("7 - Получить изображение решета (Base64)");
                    Console.WriteLine("8 - Очистить историю запросов");
                    Console.WriteLine("0 - Выход");
                    Console.Write("Ваш выбор: ");
                    
                    string? input = Console.ReadLine();
                    if (!int.TryParse(input, out int choice) || choice < 0 || choice > 8)
                    {
                        Console.WriteLine("Неверный выбор, попробуйте снова.");
                        continue;
                    }
                    
                    if (choice == 1) // Вход
                    {
                        Console.Write("Введите логин: ");
                        string? username = Console.ReadLine();
                        Console.Write("Введите пароль: ");
                        string? password = Console.ReadLine();

                        token = LoginOnServ(username, password);
                        if (token != null)
                        {
                            Console.WriteLine($"Авторизация успешна. Ваш токен: {token.Value.access_token}");
                        }
                    }
                    else if (choice == 2) // Регистрация
                    {
                        Console.Write("Введите логин: ");
                        string? username = Console.ReadLine();
                        Console.Write("Введите пароль: ");
                        string? password = Console.ReadLine();

                        token = await SignupOnServer(username, password);
                    }
                    else if (choice == 3) // Смена пароля
                    {
                        Console.Write("Введите логин: ");
                        string? username = Console.ReadLine();
                        Console.Write("Введите текущий пароль: ");
                        string? oldPassword = Console.ReadLine();
                        Console.Write("Введите новый пароль: ");
                        string? newPassword = Console.ReadLine();

                        token = await ChangePasswordOnServer(username, oldPassword, newPassword);
                    }
                    else if (choice == 4) // Получить простые числа
                    {
                        if (token == null)
                        {
                            Console.WriteLine("Вы должны быть авторизованы для выполнения этой операции.");
                            continue;
                        }

                        Console.Write("Введите число N для получения простых чисел до N: ");
                        if (int.TryParse(Console.ReadLine(), out int N))
                        {
                            await GetPrimes(N);
                        }
                        else
                        {
                            Console.WriteLine("Неверный ввод.");
                        }
                    }
                    else if (choice == 5) // Получить простые числа в диапазоне
                    {
                        if (token == null)
                        {
                            Console.WriteLine("Вы должны быть авторизованы для выполнения этой операции.");
                            continue;
                        }

                        Console.Write("Введите число N для получения простых чисел в диапазоне от 1 до N: ");
                        if (int.TryParse(Console.ReadLine(), out int N))
                        {
                            await GetPrimesInRange(N);
                        }
                        else
                        {
                            Console.WriteLine("Неверный ввод.");
                        }
                    }
                    else if (choice == 6) // Получить историю запросов
                    {
                        if (token == null)
                        {
                            Console.WriteLine("Вы должны быть авторизованы для просмотра истории.");
                            continue;
                        }

                        await GetUserHistory(token.Value.username);
                    }
                    else if (choice == 7) // Получить изображение решета (Base64)
                    {
                        if (token == null)
                        {
                            Console.WriteLine("Вы должны быть авторизованы для выполнения этой операции.");
                            continue;
                        }

                        Console.Write("Введите число N для генерации решета (от 1 до N): ");
                        if (int.TryParse(Console.ReadLine(), out int N))
                        {
                            await GetSieveImage(N);
                        }
                        else
                        {
                            Console.WriteLine("Неверный ввод.");
                        }
                    }
                    else if (choice == 8) // Очистить историю запросов
                    {
                        if (token == null)
                        {
                            Console.WriteLine("Вы должны быть авторизованы для выполнения этой операции.");
                            continue;
                        }

                        await ClearUserHistory(token.Value.username);
                    }
                    else if (choice == 0) // Выход
                    {
                        break;
                    }
                }
            }
            catch (Exception exp)
            {
                Console.WriteLine("Ошибка: " + exp.Message);
            }

            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }

        // Метод для авторизации
        static Token? LoginOnServ(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            string request = $"/login?login={username}&password={password}";
            var response = client.PostAsync(request, null).Result;

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Авторизация успешна.");
                return response.Content.ReadFromJsonAsync<Token>().Result;
            }
            else
            {
                Console.WriteLine("Ошибка авторизации.");
                return null;
            }
        }

        static async Task<Token?> SignupOnServer(string? username, string? password)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        Console.WriteLine("Имя пользователя и пароль не могут быть пустыми.");
        return null;
    }

    try
    {
        Console.WriteLine("Попытка регистрации пользователя...");

        string request = $"/signup?login={username}&password={password}";
        var response = await client.PostAsync(request, null);

        // Выводим статус ответа
        Console.WriteLine($"Код ответа сервера: {response.StatusCode}");

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            Console.WriteLine("Пользователь с таким именем уже зарегистрирован.");
            return null;
        }
        else if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Не удалось зарегистрировать пользователя. Возможно он уже существует");
            return null;
        }

        // Если регистрация успешна, возвращаем результат авторизации
        Console.WriteLine("Регистрация успешна. Пытаемся войти...");
        return LoginOnServ(username, password);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Произошла ошибка при регистрации: {ex.Message}");
        return null;
    }
}



        // Метод для смены пароля
        static async Task<Token?> ChangePasswordOnServer(string? username, string? oldPassword, string? newPassword)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                Console.WriteLine("Логин, старый или новый пароль не могут быть пустыми.");
                return null;
            }

            string request = $"/change-password?login={username}&oldPassword={oldPassword}&newPassword={newPassword}";
            var response = await client.PatchAsync(request, null);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Пароль успешно изменён.");
                return await response.Content.ReadFromJsonAsync<Token>();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("Ошибка: неверный старый пароль.");
            }
            else
            {
                Console.WriteLine("Ошибка: не удалось изменить пароль.");
            }

            return null;
        }

        // Метод для очистки заголовков
        static void ClearHeaders()
        {
            client.DefaultRequestHeaders.Clear();
        }

        // Метод для получения простых чисел (POST)
        static async Task GetPrimes(int N)
        {
            ClearHeaders();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Value.access_token);
            client.DefaultRequestHeaders.Add("User-ID", token!.Value.username);

            string requestUrl = "/primes/count?count=" + N;
            var content = new StringContent(JsonSerializer.Serialize(new { count = N }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Простые числа: {result}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при получении простых чисел: {response.StatusCode} - {errorContent}");
            }
        }

        // Метод для получения простых чисел в диапазоне (POST)
        static async Task GetPrimesInRange(int N)
        {
            ClearHeaders();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Value.access_token);
            client.DefaultRequestHeaders.Add("User-ID", token!.Value.username);

            string requestUrl = "/primes/range?max=" + N;
            var content = new StringContent(JsonSerializer.Serialize(new { max = N }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Простые числа в диапазоне: {result}");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при получении простых чисел: {response.StatusCode} - {errorContent}");
            }
        }

        // Метод для получения истории запросов (GET)
        static async Task GetUserHistory(string userId)
        {
            string request = $"/history/{userId}";
            var response = await client.GetAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var history = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"История запросов пользователя {userId}:\n{history}");
            }
            else
            {
                Console.WriteLine("Ошибка при получении истории запросов.");
            }
        }

        // Метод для получения изображения решета (Base64) (POST)
        static async Task GetSieveImage(int N)
        {
            ClearHeaders();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Value.access_token);
            client.DefaultRequestHeaders.Add("User-ID", token!.Value.username);

            string requestUrl = "/primes/sieve?max=" + N;
            var content = new StringContent(JsonSerializer.Serialize(new { max = N }), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(requestUrl, content);
            if (response.IsSuccessStatusCode)
            {
                var base64Image = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Полученное изображение (Base64):");
                Console.WriteLine(base64Image);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка при получении изображения: {response.StatusCode} - {errorContent}");
            }
        }

        // Новый метод для очистки истории запросов (DELETE)
        static async Task ClearUserHistory(string userId)
        {
            ClearHeaders();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Value.access_token);
            client.DefaultRequestHeaders.Add("User-ID", userId);

            string requestUrl = $"/history/{userId}/clear";
            var response = await client.DeleteAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine(result);
            }
            else
            {
                Console.WriteLine($"Ошибка при очистке истории: {response.StatusCode}");
                var errorResponse = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ответ сервера: {errorResponse}");
            }
        }
    }
}
