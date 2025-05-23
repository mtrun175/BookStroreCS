using Spectre.Console;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace BookStoreConsoleApp.Services
{
    public static class BookService
    {
        public static void DisplayBookList(string connectionString)
        {
            const int pageSize = 10; // Số sách mỗi trang
            int currentPage = 1;

            while (true)
            {
                var table = new Table();

                table.Border = TableBorder.Rounded;
                table.Title = new TableTitle($"[yellow bold underline]📚 DANH SÁCH SÁCH — TRANG {currentPage}[/]");
                table.AddColumn("[green]STT[/]");
                table.AddColumn("[blue]Tên sách[/]");
                table.AddColumn("[aqua]Tác giả[/]");
                table.AddColumn("[orange1]Giá (VNĐ)[/]");
                table.AddColumn("[purple]Số lượng[/]");
                table.AddColumn("[green]Thể loại[/]");
                table.AddColumn("[grey]Mô tả[/]");

                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();

                    var offset = (currentPage - 1) * pageSize;

                    var query = @"
    SELECT 
        b.Title, b.Author, b.Price, b.Quantity, b.Description, 
        c.Name AS CategoryName
    FROM books b
    LEFT JOIN categories c ON b.CategoryID = c.CategoryID
    LIMIT @PageSize OFFSET @Offset";


                    using var cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);

                    using var reader = cmd.ExecuteReader();
                    int index = offset + 1;

                    while (reader.Read())
                    {
                        table.AddRow(
                            index.ToString(),
                            reader["Title"].ToString(),
                            reader["Author"].ToString(),
                            string.Format("{0:N0}", reader.GetDecimal("Price")),
                            reader.GetInt32("Quantity").ToString(),
                            reader["CategoryName"]?.ToString() ?? "[grey italic]Chưa có[/]",

                            reader["Description"].ToString()
                        );
                        index++;
                    }

                    Console.Clear();
                    AnsiConsole.Write(table);

                    // Điều hướng phân trang
                    AnsiConsole.MarkupLine("\n[bold cyan]Điều hướng:[/] ← [green]P[/]revious | [green]N[/]ext → | [green]Q[/]uit");

                    AnsiConsole.Markup("[yellow]👉 Nhập lựa chọn của bạn: [/]");

                    var input = Console.ReadLine()?.Trim().ToLower();

                    if (input == "n")
                        currentPage++;
                    else if (input == "p" && currentPage > 1)
                        currentPage--;
                    else if (input == "q")
                        break;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Lỗi truy vấn:[/] {ex.Message}");
                    break;
                }
            }
        }
    }
}

