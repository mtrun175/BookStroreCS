using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class AuthService
    {
        public static void Register(string connectionString)
        {
            AnsiConsole.Clear();
            ShowTitle("📝 ĐĂNG KÝ TÀI KHOẢN MỚI", "magenta");

            AnsiConsole.MarkupLine("[grey]💡 Nhấn [bold]Ctrl + Enter[/] bất kỳ lúc nào để quay về trang chính.[/]");
            string fullName, email, password, phone, address;

            // Họ tên
            while (true)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]👤 Nhập họ tên: [/]");
                fullName = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(fullName)) break;
                ShowError("Họ tên không được để trống.");
            }

            // Email
            while (true)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]📧 Nhập email (abc@gmail.com): [/]");
                email = Console.ReadLine()?.Trim();
                if (Regex.IsMatch(email ?? "", @"^[\w\.-]+@gmail\.com$")) break;
                ShowError("Email không hợp lệ. Vui lòng nhập lại.");
            }

            // Mật khẩu
            while (true)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]🔒 Nhập mật khẩu (8 ký tự, chữ hoa, số, ký tự đặc biệt): [/]");
                password = ReadPassword();
                if (Regex.IsMatch(password ?? "", @"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$")) break;
                ShowError("Mật khẩu không hợp lệ. Ví dụ: Pa$$w0rd");
            }

            // SĐT
            while (true)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]📱 Nhập số điện thoại (9-11 số): [/]");
                phone = Console.ReadLine()?.Trim();
                if (Regex.IsMatch(phone ?? "", @"^[0-9]{9,11}$")) break;
                ShowError("Số điện thoại không hợp lệ.");
            }

            // Địa chỉ
            while (true)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]🏠 Nhập địa chỉ: [/]");
                address = Console.ReadLine()?.Trim();
                if (!string.IsNullOrWhiteSpace(address)) break;
                ShowError("Địa chỉ không được để trống.");
            }

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var checkQuery = "SELECT COUNT(*) FROM customers WHERE Email = @Email";
                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@Email", email);
                long exists = (long)checkCmd.ExecuteScalar();
                if (exists > 0)
                {
                    ShowError("Email đã được đăng ký.");
                    PauseScreen();
                    return;
                }

                var hashed = HashPassword(password);
                string insertQuery = @"INSERT INTO customers 
                    (FullName, Email, Password, PhoneNumber, Address, Status, Is_admin, Canceled_orders, Total_orders)
                    VALUES (@FullName, @Email, @Password, @PhoneNumber, @Address, 0, 0, 0, 0)";
                using var cmd = new MySqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Password", hashed);
                cmd.Parameters.AddWithValue("@PhoneNumber", phone);
                cmd.Parameters.AddWithValue("@Address", address);
                cmd.ExecuteNonQuery();

                ShowSuccess("🎉 Đăng ký thành công!");
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi khi đăng ký: {ex.Message}");
            }

            PauseScreen();
        }

        public static string? Login(string connectionString)
        {
            AnsiConsole.Clear();
            ShowTitle("🔑 ĐĂNG NHẬP", "blue");
            AnsiConsole.MarkupLine("[grey]💡 Nhấn [bold]Ctrl + Enter[/] để quay về trang chính.[/]");

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                CheckExitShortcut();
                AnsiConsole.Markup("[cyan]📧 Email: [/]");
                string? email = Console.ReadLine()?.Trim();

                AnsiConsole.Markup("[cyan]🔒 Mật khẩu: [/]");
                var password = ReadPassword();
                var hashed = HashPassword(password);

                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();

                    var query = "SELECT FullName, Is_admin FROM customers WHERE Email = @Email AND Password = @Password";
                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", hashed);
                    using var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        var name = reader.GetString("FullName");
                        int isAdmin = Convert.ToInt32(reader["Is_admin"]);

                        ShowSuccess("✅ Đăng nhập thành công!");
                        PauseScreen();
                        if (isAdmin == 1)
                        {
                            DashboardService.DisplayDashboard(connectionString);
                            return null;
                        }
                        else return name;
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Lỗi hệ thống: {ex.Message}");
                    return null;
                }

                ShowError($"❌ Sai thông tin. Thử lại: {attempt}/3");
            }

            ShowError("⛔ Đăng nhập thất bại sau 3 lần.");
            PauseScreen();
            return null;
        }

        public static void ShowUserInfo(string username, string connectionString)
        {
            ShowTitle("👤 THÔNG TIN TÀI KHOẢN", "teal");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var query = "SELECT * FROM customers WHERE FullName = @FullName";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@FullName", username);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var panel = new Panel($"""
                    [bold yellow]👤 Họ tên:[/] {reader["FullName"]}
                    [bold yellow]📧 Email:[/] {reader["Email"]}
                    [bold yellow]📱 SĐT:[/] {reader["PhoneNumber"]}
                    [bold yellow]🏠 Địa chỉ:[/] {reader["Address"]}
                    [bold yellow]❌ Đơn huỷ:[/] {reader["Canceled_orders"]}
                    [bold yellow]📦 Tổng đơn:[/] {reader["Total_orders"]}
                    [bold yellow]🕒 Ngày tạo:[/] {reader["created_at"]}
                    """).Border(BoxBorder.Rounded).Header("🗂️ Hồ sơ người dùng");

                    AnsiConsole.Write(panel);
                }
                else
                {
                    ShowError("Không tìm thấy người dùng.");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi hệ thống: {ex.Message}");
            }

            PauseScreen();
        }

        private static void ShowTitle(string title, string color)
        {
            var rule = new Rule($"[{color} bold]{title}[/]").RuleStyle(color).LeftJustified();
            AnsiConsole.Write(rule);
        }

        private static void ShowSuccess(string msg) =>
            AnsiConsole.MarkupLine($"[bold green]{msg}[/]");

        private static void ShowError(string msg) =>
            AnsiConsole.MarkupLine($"[bold red]{msg}[/]");

        private static void CheckExitShortcut()
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Modifiers.HasFlag(ConsoleModifiers.Control) && key.Key == ConsoleKey.Enter)
                {
                    AnsiConsole.Markup("[yellow]❓ Bạn muốn quay lại trang chính? (Y/N): [/]");
                    var confirm = Console.ReadKey(true);
                    if (confirm.Key == ConsoleKey.Y)
                    {
                        AnsiConsole.MarkupLine("\n[green]👋 Đã quay về trang chính![/]");
                        PauseScreen();
                        Environment.Exit(0);
                    }
                    else
                    {
                        ShowSuccess("Tiếp tục thao tác...");
                    }
                }
            }
        }

        private static void PauseScreen()
        {
            AnsiConsole.MarkupLine("[grey]⏳ Nhấn phím bất kỳ để tiếp tục...[/]");
            Console.ReadKey(true);
        }

        private static string ReadPassword()
        {
            string pwd = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
                {
                    pwd = pwd[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    pwd += key.KeyChar;
                    Console.Write("*");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return pwd;
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
