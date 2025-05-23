using System;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardDiscount
    {
        public static void ManageDiscounts(string connectionString)
        {
            while (true)
            {
                Console.Clear();
                var title = new FigletText("Discount Manager").Centered().Color(Color.Yellow);
                AnsiConsole.Write(title);

                AnsiConsole.MarkupLine("[yellow]1. Xem danh sách mã giảm giá[/]");
                AnsiConsole.MarkupLine("[yellow]2. Thêm mã mới[/]");
                AnsiConsole.MarkupLine("[yellow]3. Cập nhật mã[/]");
                AnsiConsole.MarkupLine("[yellow]4. Xóa mã[/]");
                AnsiConsole.MarkupLine("[yellow]5. Quay lại[/]");

                Console.Write("🔎 Chọn chức năng: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        DisplayDiscountList(connectionString);
                        break;
                    case "2":
                        AddDiscount(connectionString);
                        break;
                    case "3":
                        UpdateDiscount(connectionString);
                        break;
                    case "4":
                        DeleteDiscount(connectionString);
                        break;
                    case "5":
                        return;
                    default:
                        AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ![/]");
                        Thread.Sleep(1500);
                        break;
                }
            }
        }

        private static void DisplayDiscountList(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold]📋 Danh sách mã giảm giá:[/]");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"SELECT DiscountID, Code, Value, ExpiryDate FROM discounts ORDER BY ExpiryDate DESC";
            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("ID", "Mã", "Giá trị (%)", "Hạn sử dụng");

            while (reader.Read())
            {
                table.AddRow(
                    reader["DiscountID"].ToString(),
                    reader["Code"].ToString(),
                    reader["Value"].ToString(),
                    Convert.ToDateTime(reader["ExpiryDate"]).ToString("yyyy-MM-dd")
                );
            }

            AnsiConsole.Write(table);
            Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
            Console.ReadKey();
        }

        private static void AddDiscount(string connectionString)
        {
            Console.Write("🆕 Nhập mã giảm giá: ");
            string code = Console.ReadLine();

            Console.Write("🔢 Nhập giá trị (%): ");
            if (!int.TryParse(Console.ReadLine(), out int value)) return;

            Console.Write("📅 Nhập hạn sử dụng (yyyy-MM-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime expiryDate)) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"INSERT INTO discounts (Code, Value, ExpiryDate) VALUES (@Code, @Value, @ExpiryDate)";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Code", code);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]✅ Thêm mã thành công![/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Thêm thất bại.[/]");

            Thread.Sleep(1500);
        }

        private static void UpdateDiscount(string connectionString)
        {
            Console.Write("🖊️ Nhập ID mã cần cập nhật: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;

            Console.Write("🔢 Nhập giá trị mới (%): ");
            if (!int.TryParse(Console.ReadLine(), out int value)) return;

            Console.Write("📅 Nhập hạn sử dụng mới (yyyy-MM-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime expiry)) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"UPDATE discounts SET Value = @Value, ExpiryDate = @Expiry WHERE DiscountID = @ID";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Value", value);
            cmd.Parameters.AddWithValue("@Expiry", expiry);
            cmd.Parameters.AddWithValue("@ID", id);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]✅ Cập nhật thành công![/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy mã.[/]");

            Thread.Sleep(1500);
        }

        private static void DeleteDiscount(string connectionString)
        {
            Console.Write("🗑️ Nhập ID mã cần xóa: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;

            Console.Write("⚠️ Bạn chắc chắn muốn xóa? (y/N): ");
            var confirm = Console.ReadLine();
            if (!confirm?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? true) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = "DELETE FROM discounts WHERE DiscountID = @ID";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ID", id);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]✅ Đã xóa mã giảm giá.[/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy mã.[/]");

            Thread.Sleep(1500);
        }
    }
}
