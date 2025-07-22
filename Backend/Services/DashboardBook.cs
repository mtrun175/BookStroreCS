using System;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardBook
    {
        public static void ManageBooks(string connectionString, string? loggedInUser)
        {
            while (true)
            {
                Console.Clear();
                var title = new FigletText("Book Manager").Centered().Color(Color.Yellow);
                AnsiConsole.Write(title);

                AnsiConsole.MarkupLine("[yellow]1. Xem danh sách sách[/]");
                AnsiConsole.MarkupLine("[yellow]2. Thêm sách mới[/]");
                AnsiConsole.MarkupLine("[yellow]3. Cập nhật sách[/]");
                AnsiConsole.MarkupLine("[yellow]4. Xóa sách[/]");
                AnsiConsole.MarkupLine("[yellow]5. Quay lại[/]");

                Console.Write("🔎 Chọn chức năng: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        BookService.DisplayBookList(connectionString, loggedInUser);
                        Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
                        Console.ReadKey();
                        break;
                    case "2":
                        AddBook(connectionString);
                        break;
                    case "3":
                        UpdateBook(connectionString);
                        break;
                    case "4":
                        DeleteBook(connectionString);
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

        private static void AddBook(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold green]➕ Thêm sách mới[/]");

            Console.Write("📖 Tên sách: ");
            string title = Console.ReadLine()?.Trim();

            Console.Write("✍️ Tác giả: ");
            string author = Console.ReadLine()?.Trim();

            Console.Write("💲 Giá: ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal price))
            {
                AnsiConsole.MarkupLine("[red]❌ Giá không hợp lệ.[/]");
                Thread.Sleep(1500);
                return;
            }

            Console.Write("📦 Số lượng: ");
            if (!int.TryParse(Console.ReadLine(), out int quantity))
            {
                AnsiConsole.MarkupLine("[red]❌ Số lượng không hợp lệ.[/]");
                Thread.Sleep(1500);
                return;
            }

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"INSERT INTO books (Title, Author, Price, Quantity) 
                             VALUES (@Title, @Author, @Price, @Quantity)";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Author", author);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@Quantity", quantity);

            cmd.ExecuteNonQuery();

            AnsiConsole.MarkupLine("[green]✅ Đã thêm sách thành công![/]");
            Thread.Sleep(2000);
        }

        private static void UpdateBook(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold yellow]✏️ Cập nhật thông tin sách[/]");
            Console.Write("🆔 Nhập ID sách cần cập nhật: ");
            if (!int.TryParse(Console.ReadLine(), out int bookId)) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = "SELECT * FROM books WHERE BookID = @BookID";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@BookID", bookId);
            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy sách.[/]");
                Thread.Sleep(1500);
                return;
            }

            string currentTitle = reader["Title"].ToString();
            string currentAuthor = reader["Author"].ToString();
            decimal currentPrice = Convert.ToDecimal(reader["Price"]);
            int currentQty = Convert.ToInt32(reader["Quantity"]);

            reader.Close();

            Console.Write($"📖 Tên sách [{currentTitle}]: ");
            string newTitle = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newTitle)) newTitle = currentTitle;

            Console.Write($"✍️ Tác giả [{currentAuthor}]: ");
            string newAuthor = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newAuthor)) newAuthor = currentAuthor;

            Console.Write($"💲 Giá [{currentPrice}]: ");
            string priceInput = Console.ReadLine()?.Trim();
            decimal newPrice = string.IsNullOrEmpty(priceInput) ? currentPrice : decimal.Parse(priceInput);

            Console.Write($"📦 Số lượng [{currentQty}]: ");
            string qtyInput = Console.ReadLine()?.Trim();
            int newQty = string.IsNullOrEmpty(qtyInput) ? currentQty : int.Parse(qtyInput);

            string updateQuery = @"UPDATE books 
                                   SET Title = @Title, Author = @Author, Price = @Price, Quantity = @Quantity 
                                   WHERE BookID = @BookID";

            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@Title", newTitle);
            updateCmd.Parameters.AddWithValue("@Author", newAuthor);
            updateCmd.Parameters.AddWithValue("@Price", newPrice);
            updateCmd.Parameters.AddWithValue("@Quantity", newQty);
            updateCmd.Parameters.AddWithValue("@BookID", bookId);

            updateCmd.ExecuteNonQuery();

            AnsiConsole.MarkupLine("[green]✅ Đã cập nhật thông tin sách thành công![/]");
            Thread.Sleep(2000);
        }

        private static void DeleteBook(string connectionString)
        {
            Console.Write("🗑️ Nhập ID sách cần xóa: ");
            if (!int.TryParse(Console.ReadLine(), out int bookId)) return;

            Console.Write("⚠️ Bạn có chắc chắn muốn xóa không? (y/N): ");
            var confirm = Console.ReadLine();
            if (!confirm?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? true) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = "DELETE FROM books WHERE BookID = @BookID";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@BookID", bookId);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]🗑️ Đã xóa sách thành công![/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy sách.[/]");

            Thread.Sleep(2000);
        }
    }
}

