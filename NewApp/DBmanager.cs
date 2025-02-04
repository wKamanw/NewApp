using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

public class DBManager
{
    private SqliteConnection? connection = null;

    // Хеширование пароля через SHA-256 без Unicode
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password); // Используем UTF-8 (НЕ Unicode)
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // 16-ричный формат
        }
    }

    public bool ConnectToDB(string path)
    {
        Console.WriteLine("Connecting to DB...");
        try
        {
            connection = new SqliteConnection("Data Source=" + path);
            connection.Open();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                Console.WriteLine("Failed to connect.");
                return false;
            }
        }
        catch (Exception exp)
        {
            Console.WriteLine($"DB connection error: {exp.Message}");
            return false;
        }

        Console.WriteLine("Connected.");
        return true;
    }

    public void Disconnect()
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
            return;

        connection.Close();
        Console.WriteLine("Disconnected from DB.");
    }

    public bool AddUser(string login, string password)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
            return false;

        string hashedPassword = HashPassword(password);
        string request = "INSERT INTO users (Login, Password) VALUES(@login, @password)";
        using var command = new SqliteCommand(request, connection);
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", hashedPassword);

        try
        {
            return command.ExecuteNonQuery() > 0;
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error adding user: {exp.Message}");
            return false;
        }
    }

    public bool CheckUser(string login, string password)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
            return false;

        string hashedPassword = HashPassword(password);
        string request = "SELECT COUNT(*) FROM users WHERE Login = @login AND Password = @password";

        using var command = new SqliteCommand(request, connection);
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", hashedPassword);

        try
        {
            var result = command.ExecuteScalar();
            return Convert.ToInt64(result) > 0;
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error checking user: {exp.Message}");
            return false;
        }
    }

    public bool ChangePassword(string login, string newPassword)
    {
        if (connection == null || connection.State != System.Data.ConnectionState.Open)
            return false;

        string hashedPassword = HashPassword(newPassword);

        string request = "UPDATE users SET Password = @password WHERE Login = @login";
        using var command = new SqliteCommand(request, connection);
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", hashedPassword);

        try
        {
            int result = command.ExecuteNonQuery();
            return result > 0;
        }
        catch (Exception exp)
        {
            Console.WriteLine($"Error changing password: {exp.Message}");
            return false;
        }
    }
}


