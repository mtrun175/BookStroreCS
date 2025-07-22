using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class UserInfoService
    {
        public static void ShowUserInfo(string email, string connectionString)
        {
            ShowTitle("👤 THÔNG TIN TÀI KHOẢN", "teal");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var query = "SELECT * FROM customers WHERE Email = @Email";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Email", email);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    ShowError("❌ Không tìm thấy người dùng. Vui lòng đăng nhập lại.");
                    PauseScreen();
                    return;
                }

                var fullName = reader["FullName"].ToString();
                var phone = reader["PhoneNumber"].ToString();
                var address = reader["Address"].ToString();
                var addressDetail = reader["AddressDetail"].ToString();
                var canceledOrders = reader["Canceled_orders"];
                var totalOrders = reader["Total_orders"];
                var createdAt = reader["created_at"];
                var currentPasswordHash = reader["Password"].ToString();

                var panel = new Panel($"""
[bold yellow]👤 Họ tên:[/] {fullName}
[bold yellow]📧 Email:[/] {email}
[bold yellow]📱 SĐT:[/] {phone}
[bold yellow]🏠 Địa chỉ:[/] {address}
[bold yellow]🏠 Địa chỉ chi tiết:[/] {addressDetail}
[bold yellow]❌ Đơn huỷ:[/] {canceledOrders}
[bold yellow]📦 Tổng đơn:[/] {totalOrders}
[bold yellow]🕒 Ngày tạo:[/] {createdAt}
""").Border(BoxBorder.Rounded).Header("🗂️ Hồ sơ người dùng");

                AnsiConsole.Write(panel);
                reader.Close();

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\n[bold green]👉 Bạn muốn làm gì tiếp theo?[/]")
                        .AddChoices("📝 Sửa thông tin", "🔐 Đổi mật khẩu", "⬅️ Về trang chủ")
                        .UseConverter(x => x)
                        .HighlightStyle("cyan"));

                switch (choice)
                {
                    case "📝 Sửa thông tin":
                        UpdateUserInfo(email, connection, fullName, phone, address, addressDetail);
                        break;

                    case "🔐 Đổi mật khẩu":
                        ChangePassword(email, connection, currentPasswordHash);
                        break;

                    case "⬅️ Về trang chủ":
                        AnsiConsole.MarkupLine("[grey]👋 Quay lại menu chính...[/]");
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Lỗi hệ thống: {ex.Message}");
            }

            PauseScreen();
        }

        private static void UpdateUserInfo(string email, MySqlConnection connection, string fullName, string phone, string address, string addressDetail)
        {
            AnsiConsole.MarkupLine("[grey]💡 Nhấn Esc để quay lại. Nhấn Enter để giữ nguyên giá trị hiện tại.[/]\n");

            string PromptInput(string label, string currentValue)
            {
                while (true)
                {
                    AnsiConsole.Markup($"{label} ([green italic]{currentValue}[/]): ");
                    string? input = ReadLineWithEscape()?.Trim();

                    if (input == "__BACK")
                    {
                        AnsiConsole.MarkupLine("[grey]⏩ Đã hủy. Tiếp tục nhập...[/]");
                        return "__BACK";
                    }

                    string finalValue = string.IsNullOrEmpty(input) ? currentValue : input;

                    // Ghi đè dòng trước đó bằng cách quay lại đầu dòng và in lại
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    AnsiConsole.MarkupLine($"{label} ([green italic]{currentValue}[/]): {finalValue}");

                    return finalValue;
                }
            }

            string newName, newEmail, newPhone, newAddress, newAddressDetail;

            // 👤 Họ tên
            while (true)
            {
                newName = PromptInput("👤 Họ tên", fullName);
                if (newName == "__BACK") return;
                if (newName.Length >= 2) break;
                ShowError("❌ Họ tên phải có ít nhất 2 ký tự.");
            }

            // 📧 Email
            while (true)
            {
                newEmail = PromptInput("📧 Email", email);
                if (newEmail == "__BACK") return;
                if (Regex.IsMatch(newEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) break;
                ShowError("❌ Email không hợp lệ.");
            }

            // 📱 SĐT
            while (true)
            {
                newPhone = PromptInput("📱 SĐT", phone);
                if (newPhone == "__BACK") return;
                if (Regex.IsMatch(newPhone, @"^\d{9,11}$")) break;
                ShowError("❌ SĐT không hợp lệ (phải từ 9 đến 11 chữ số).");
            }

            // 🏠 Địa chỉ
            while (true)
            {
                newAddress = PromptInput("🏠 Địa chỉ", address);
                if (newAddress == "__BACK") return;
                if (!string.IsNullOrWhiteSpace(newAddress)) break;
                ShowError("❌ Địa chỉ không được để trống.");
            }

            // 📍 Chi tiết
            while (true)
            {
                newAddressDetail = PromptInput("📍 Chi tiết", addressDetail);
                if (newAddressDetail == "__BACK") return;
                if (!string.IsNullOrWhiteSpace(newAddressDetail)) break;
                ShowError("❌ Chi tiết địa chỉ không được để trống.");
            }

            try
            {
                var updateCmd = new MySqlCommand(@"
            UPDATE customers 
            SET FullName = @Name, Email = @Email, PhoneNumber = @Phone, Address = @Address, AddressDetail = @AddressDetail 
            WHERE Email = @OldEmail", connection);

                updateCmd.Parameters.AddWithValue("@Name", newName);
                updateCmd.Parameters.AddWithValue("@Email", newEmail);
                updateCmd.Parameters.AddWithValue("@Phone", newPhone);
                updateCmd.Parameters.AddWithValue("@Address", newAddress);
                updateCmd.Parameters.AddWithValue("@AddressDetail", newAddressDetail);
                updateCmd.Parameters.AddWithValue("@OldEmail", email); // dùng để xác định bản ghi cũ

                int rowsAffected = updateCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                    AnsiConsole.MarkupLine("\n[green]✅ Thông tin tài khoản đã được cập nhật thành công![/]");
                else
                    ShowError("❌ Không có thay đổi nào được lưu.");
            }
            catch (Exception ex)
            {
                ShowError($"❌ Lỗi khi cập nhật thông tin: {ex.Message}");
            }

            PauseScreen();
        }






        private static void ChangePassword(string email, MySqlConnection connection, string currentPasswordHash)
        {
            AnsiConsole.MarkupLine("[blue]💡 Nhấn [bold]Esc[/] để quay lại bất kỳ lúc nào.[/]\n");

            string? oldPass = ReadPasswordWithEscape("🔑 Nhập mật khẩu hiện tại:");
            if (oldPass == "__BACK") return;

            if (HashPasswordSHA256(oldPass) != currentPasswordHash)
            {
                ShowError("❌ Mật khẩu hiện tại không đúng.");
                return;
            }

            string? newPass = ReadPasswordWithEscape("🆕 Nhập mật khẩu mới (8 ký tự, chữ hoa, số, ký tự đặc biệt):");
            if (newPass == "__BACK") return;

            if (newPass.Length < 8)
            {
                ShowError("❌ Mật khẩu phải có ít nhất 8 ký tự.");
                return;
            }

            string? confirmPass = ReadPasswordWithEscape("🆗 Nhập lại mật khẩu mới:");
            if (confirmPass == "__BACK") return;

            if (newPass != confirmPass)
            {
                ShowError("❌ Mật khẩu xác nhận không khớp.");
                return;
            }

            string hashedPassword = HashPasswordSHA256(newPass);
            var passCmd = new MySqlCommand("UPDATE customers SET Password = @Password WHERE Email = @Email", connection);
            passCmd.Parameters.AddWithValue("@Password", hashedPassword);
            passCmd.Parameters.AddWithValue("@Email", email);

            int changed = passCmd.ExecuteNonQuery();
            if (changed > 0)
                AnsiConsole.MarkupLine("\n[green]✅ Mật khẩu đã được cập nhật thành công![/]");
            else
                ShowError("❌ Không thể cập nhật mật khẩu.");
        }

        private static string HashPasswordSHA256(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private static string? ReadLineWithEscape()
        {
            var input = new StringBuilder();
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    AnsiConsole.Markup("\n[yellow]❓ Bạn muốn quay lại? (Y/N): [/]");

                    var confirm = Console.ReadKey(true);
                    if (char.ToUpper(confirm.KeyChar) == 'Y')
                    {
                        Console.WriteLine();
                        return "__BACK";
                    }
                    else
                    {
                        Console.WriteLine();
                        AnsiConsole.MarkupLine("[blue]⏩ Đã hủy. Tiếp tục nhập...[/]");
                        continue;
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input.ToString();
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
        }

        private static string? ReadPasswordWithEscape(string prompt)
        {
            AnsiConsole.Markup($"{prompt} ");
            var input = new StringBuilder();

            while (true)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    AnsiConsole.Markup("\n[yellow]❓ Bạn muốn quay lại? (Y/N): [/]");

                    var confirm = Console.ReadKey(true);
                    if (char.ToUpper(confirm.KeyChar) == 'Y')
                    {
                        Console.WriteLine();
                        return "__BACK";
                    }
                    else
                    {
                        Console.WriteLine();
                        AnsiConsole.MarkupLine("[blue]⏩ Đã hủy. Tiếp tục nhập...[/]");
                        continue;
                    }
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return input.ToString();
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Length--;
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    input.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
        }

        private static void ShowTitle(string title, string color)
        {
            var rule = new Rule($"[{color} bold]{title}[/]").RuleStyle(color).LeftJustified();
            AnsiConsole.Write(rule);
        }

        private static void ShowError(string msg) =>
            AnsiConsole.MarkupLine($"[bold red]{msg}[/]");

        private static void PauseScreen()
        {
            Console.ReadKey(true);
        }
    }
}
