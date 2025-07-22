using Spectre.Console;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardBookService
    {
        public static void ShowBookDashboard(string connectionString)
        {
            const int pageSize = 10;
            int currentPage = 1;

            while (true)
            {
                //Console.Clear();
                Console.Clear();
                var title = new FigletText("🧾 BOOK MANAGER")
                    .Centered()
                    .Color(Color.Teal);
                AnsiConsole.Write(title);
                AnsiConsole.Write(new Rule("[deepskyblue1]📦 QUẢN LÝ SÁCH[/]").LeftJustified());
                var table = new Table
                {
                    Border = TableBorder.Rounded,
                    Title = new TableTitle($"[yellow bold underline]📚 QUẢN LÝ SÁCH — TRANG {currentPage}[/]")
                };

                table.AddColumns("STT", "Mã sách", "Tên sách", "Tác giả", "Giá (VNĐ)", "Số lượng", "Thể loại", "Mô tả", "Tạo lúc", "Cập nhật");

                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();

                    int offset = (currentPage - 1) * pageSize;
                    var query = @"
                        SELECT 
                            b.BookID, b.Title, b.Author, b.Price, b.Quantity, b.Description, 
                            c.Name AS CategoryName,
                            b.CreatedAt, b.UpdatedAt
                        FROM books b
                        LEFT JOIN categories c ON b.CategoryID = c.CategoryID
                        ORDER BY b.BookID
                        LIMIT @PageSize OFFSET @Offset";

                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);

                    using var reader = cmd.ExecuteReader();
                    int index = offset + 1;

                    while (reader.Read())
                    {
                        table.AddRow(
                            index++.ToString(),
                            reader["BookID"].ToString(),
                            reader["Title"].ToString(),
                            reader["Author"].ToString(),
                            string.Format("{0:N0}", reader.GetDecimal("Price")),
                            reader["Quantity"].ToString(),
                            reader["CategoryName"]?.ToString() ?? "[grey italic]Chưa có[/]",
                            reader["Description"].ToString(),
                            Convert.ToDateTime(reader["CreatedAt"]).ToString("dd/MM/yyyy HH:mm"),
                            Convert.ToDateTime(reader["UpdatedAt"]).ToString("dd/MM/yyyy HH:mm")
                        );
                    }

                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine("\n[bold cyan]Chức năng:[/] ← [green]P[/]revious | [green]N[/]ext → | [green]A[/]dd | [green]E[/]dit | [green]S[/]earch | [green]ESC[/] để thoát");
                    AnsiConsole.Markup("[yellow]👉 Nhấn phím: [/]");

                    var keyInfo = Console.ReadKey(true);
                    switch (keyInfo.Key)
                    {
                        case ConsoleKey.N:
                            currentPage++;
                            break;
                        case ConsoleKey.P:
                            if (currentPage > 1) currentPage--;
                            break;
                        case ConsoleKey.A:
                            AddNewBook(connectionString);
                            break;
                        case ConsoleKey.E:
                            EditBook(connectionString);
                            break;
                        case ConsoleKey.S:
                            SearchBooks(connectionString);
                            break;
                        case ConsoleKey.Escape:
                            if (AnsiConsole.Confirm("❓ Bạn có chắc chắn muốn thoát?")) return;
                            break;
                        default:
                            AnsiConsole.MarkupLine("[red]⚠ Lựa chọn không hợp lệ. Vui lòng thử lại.[/]");
                            PauseScreen();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Lỗi: {ex.Message}[/]");
                    PauseScreen();
                }
            }
        }

        private static void AddNewBook(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold green]➕ THÊM SÁCH MỚI[/]");

            string title = AnsiConsole.Ask<string>("📕 Tên sách:");
            string author = AnsiConsole.Ask<string>("✍️ Tác giả:");
            decimal price = AnsiConsole.Ask<decimal>("💰 Giá (VNĐ):");
            int quantity = AnsiConsole.Ask<int>("📦 Số lượng:");
            string description = AnsiConsole.Ask<string>("📄 Mô tả:");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var categories = GetCategories(connection);
            if (categories.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]⚠ Chưa có danh mục nào. Vui lòng thêm danh mục trước.[/]");
                PauseScreen();
                return;
            }

            var selectedCategory = AnsiConsole.Prompt(
                new SelectionPrompt<KeyValuePair<int, string>>()
                    .Title("📚 Chọn thể loại sách:")
                    .AddChoices(categories)
                    .UseConverter(kv => $"{kv.Key}. {kv.Value}")
            );

            var cmd = new MySqlCommand(@"
                INSERT INTO books (Title, Author, CategoryID, Price, Quantity, Description, CreatedAt, UpdatedAt)
                VALUES (@Title, @Author, @CategoryID, @Price, @Quantity, @Description, NOW(), NOW())", connection);

            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Author", author);
            cmd.Parameters.AddWithValue("@CategoryID", selectedCategory.Key);
            cmd.Parameters.AddWithValue("@Price", price);
            cmd.Parameters.AddWithValue("@Quantity", quantity);
            cmd.Parameters.AddWithValue("@Description", description);

            try
            {
                cmd.ExecuteNonQuery();
                AnsiConsole.MarkupLine("[green]✅ Thêm sách thành công![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi thêm sách: {ex.Message}[/]");
            }

            PauseScreen();
        }

        private static void EditBook(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold yellow]✏️ CHỈNH SỬA THÔNG TIN SÁCH[/]");

            int bookId = AnsiConsole.Ask<int>("🔢 Nhập mã sách cần chỉnh sửa:");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var getCmd = new MySqlCommand("SELECT * FROM books WHERE BookID = @BookID", connection);
            getCmd.Parameters.AddWithValue("@BookID", bookId);

            using var reader = getCmd.ExecuteReader();
            if (!reader.Read())
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy sách với mã đã nhập.[/]");
                PauseScreen();
                return;
            }

            string currentTitle = reader["Title"].ToString()!;
            string currentAuthor = reader["Author"].ToString()!;
            decimal currentPrice = reader.GetDecimal("Price");
            int currentQuantity = reader.GetInt32("Quantity");
            string currentDesc = reader["Description"].ToString()!;
            int? currentCategoryID = reader["CategoryID"] == DBNull.Value ? null : Convert.ToInt32(reader["CategoryID"]);
            reader.Close();

            string newTitle = AnsiConsole.Ask<string>($"📕 Tên sách [italic grey](hiện tại: {currentTitle})[/]:", currentTitle);
            string newAuthor = AnsiConsole.Ask<string>($"✍️ Tác giả [italic grey](hiện tại: {currentAuthor})[/]:", currentAuthor);
            decimal newPrice = AnsiConsole.Ask<decimal>($"💰 Giá [italic grey](hiện tại: {currentPrice:N0})[/]:", currentPrice);
            int newQuantity = AnsiConsole.Ask<int>($"📦 Số lượng [italic grey](hiện tại: {currentQuantity})[/]:", currentQuantity);
            string newDesc = AnsiConsole.Ask<string>($"📄 Mô tả [italic grey](hiện tại: {currentDesc})[/]:", currentDesc);

            var categories = GetCategories(connection);
            int? newCategoryId = currentCategoryID;
            if (categories.Count > 0 && AnsiConsole.Confirm("📚 Sửa CategoryID?"))
            {
                var selected = AnsiConsole.Prompt(
                    new SelectionPrompt<KeyValuePair<int, string>>()
                        .Title("📚 Chọn danh mục mới:")
                        .AddChoices(categories)
                        .UseConverter(kv => $"{kv.Key}. {kv.Value}")
                );
                newCategoryId = selected.Key;
            }

            var updateCmd = new MySqlCommand(@"
                UPDATE books SET 
                    Title = @Title,
                    Author = @Author,
                    Price = @Price,
                    Quantity = @Quantity,
                    Description = @Description,
                    CategoryID = @CategoryID,
                    UpdatedAt = NOW()
                WHERE BookID = @BookID", connection);

            updateCmd.Parameters.AddWithValue("@Title", newTitle);
            updateCmd.Parameters.AddWithValue("@Author", newAuthor);
            updateCmd.Parameters.AddWithValue("@Price", newPrice);
            updateCmd.Parameters.AddWithValue("@Quantity", newQuantity);
            updateCmd.Parameters.AddWithValue("@Description", newDesc);
            updateCmd.Parameters.AddWithValue("@CategoryID", (object?)newCategoryId ?? DBNull.Value);
            updateCmd.Parameters.AddWithValue("@BookID", bookId);

            try
            {
                updateCmd.ExecuteNonQuery();
                AnsiConsole.MarkupLine("[green]✅ Cập nhật sách thành công![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi cập nhật: {ex.Message}[/]");
            }

            PauseScreen();
        }

        private static void SearchBooks(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold blue]🔍 TÌM KIẾM SÁCH[/]");
            string keyword = AnsiConsole.Ask<string>("🔎 Nhập tên sách hoặc thể loại:");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var cmd = new MySqlCommand(@"
                SELECT 
                    b.BookID, b.Title, b.Author, b.Price, b.Quantity, b.Description, 
                    c.Name AS CategoryName, b.CreatedAt, b.UpdatedAt
                FROM books b
                LEFT JOIN categories c ON b.CategoryID = c.CategoryID
                WHERE b.Title LIKE @keyword OR c.Name LIKE @keyword", connection);

            cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            using var reader = cmd.ExecuteReader();

            var table = new Table
            {
                Border = TableBorder.Rounded,
                Title = new TableTitle($"[bold green underline]📖 KẾT QUẢ TÌM KIẾM: \"{keyword}\"[/]")
            };
            table.AddColumns("Mã sách", "Tên", "Tác giả", "Giá", "SL", "Thể loại", "Mô tả", "Tạo lúc", "Cập nhật");

            int found = 0;
            while (reader.Read())
            {
                found++;
                table.AddRow(
                    reader["BookID"].ToString(),
                    reader["Title"].ToString(),
                    reader["Author"].ToString(),
                    string.Format("{0:N0}", reader.GetDecimal("Price")),
                    reader["Quantity"].ToString(),
                    reader["CategoryName"]?.ToString() ?? "[grey]Chưa có[/]",
                    reader["Description"].ToString(),
                    Convert.ToDateTime(reader["CreatedAt"]).ToString("dd/MM/yyyy HH:mm"),
                    Convert.ToDateTime(reader["UpdatedAt"]).ToString("dd/MM/yyyy HH:mm")
                );
            }

            if (found == 0)
                AnsiConsole.MarkupLine("[yellow]⚠ Không tìm thấy sách nào phù hợp.[/]");
            else
                AnsiConsole.Write(table);

            PauseScreen();
        }

        private static Dictionary<int, string> GetCategories(MySqlConnection connection)
        {
            var categories = new Dictionary<int, string>();
            using var cmd = new MySqlCommand("SELECT CategoryID, Name FROM categories", connection);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                categories[reader.GetInt32("CategoryID")] = reader.GetString("Name");
            return categories;
        }

        private static void PauseScreen()
        {
            AnsiConsole.MarkupLine("\n[grey]⏳ Nhấn phím bất kỳ để tiếp tục...[/]");
            try { Console.ReadKey(true); } catch { }
        }
    }
}
