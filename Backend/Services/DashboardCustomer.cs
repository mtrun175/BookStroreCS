using System;
using System.Data;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardCustomer
    {
        public static void ManageCustomers(string connectionString)
        {
            while (true)
            {
                Console.Clear();
                var title = new FigletText("Customer Manager").Centered().Color(Color.Green);
                AnsiConsole.Write(title);

                AnsiConsole.MarkupLine("[yellow]1. Xem tất cả khách hàng[/]");
                AnsiConsole.MarkupLine("[yellow]2. Tìm kiếm khách hàng[/]");
                AnsiConsole.MarkupLine("[yellow]3. Khóa/Mở khóa tài khoản[/]");
                AnsiConsole.MarkupLine("[yellow]4. Quay lại[/]");

                Console.Write("🔎 Chọn chức năng: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        DisplayAllCustomers(connectionString);
                        break;
                    case "2":
                        SearchCustomer(connectionString);
                        break;
                    case "3":
                        ToggleCustomerStatus(connectionString);
                        break;
                    case "4":
                        return;
                    default:
                        AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ![/]");
                        Thread.Sleep(1500);
                        break;
                }
            }
        }

        private static void DisplayAllCustomers(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold underline green]Danh sách khách hàng:[/]");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT 
                    c.CustomerID, c.FullName, c.Email, c.PhoneNumber, c.Address, c.Status, c.created_at,
                    COUNT(o.OrderID) AS TotalOrders,
                    SUM(CASE WHEN o.Status = 0 THEN 1 ELSE 0 END) AS CancelledOrders
                FROM customers c
                LEFT JOIN orders o ON c.CustomerID = o.CustomerID
                GROUP BY c.CustomerID";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var table = new Table();
            table.AddColumns("ID", "Họ tên", "Email", "SĐT", "Địa chỉ", "Trạng thái", "Tổng đơn", "Đã hủy", "Ngày tạo");

            if (!reader.HasRows)
            {
                AnsiConsole.MarkupLine("[red]❌ Không có khách hàng nào trong hệ thống.[/]");
            }

            while (reader.Read())
            {
                table.AddRow(
                    reader["CustomerID"].ToString(),
                    reader["FullName"].ToString(),
                    reader["Email"].ToString(),
                    reader["PhoneNumber"].ToString(),
                    reader["Address"].ToString(),
                    Convert.ToInt32(reader["Status"]) == 0 ? "✅ Bình thường" : "⛔ Bị khóa",
                    reader["TotalOrders"].ToString(),
                    reader["CancelledOrders"].ToString(),
                    reader["created_at"].ToString()
                );
            }

            AnsiConsole.Write(table);
            Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
            Console.ReadKey();
        }

        private static void SearchCustomer(string connectionString)
        {
            Console.Write("🔍 Nhập tên hoặc email cần tìm: ");
            string keyword = Console.ReadLine()?.Trim();

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT CustomerID, FullName, Email, PhoneNumber, Address, Status 
                FROM customers 
                WHERE FullName LIKE @kw OR Email LIKE @kw";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
            using var reader = cmd.ExecuteReader();

            var table = new Table();
            table.AddColumns("ID", "Họ tên", "Email", "SĐT", "Địa chỉ", "Trạng thái");

            while (reader.Read())
            {
                table.AddRow(
                    reader["CustomerID"].ToString(),
                    reader["FullName"].ToString(),
                    reader["Email"].ToString(),
                    reader["PhoneNumber"].ToString(),
                    reader["Address"].ToString(),
                    Convert.ToInt32(reader["Status"]) == 0 ? "✅ Bình thường" : "⛔ Bị khóa"
                );
            }

            AnsiConsole.Write(table);
            Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
            Console.ReadKey();
        }

        private static void ToggleCustomerStatus(string connectionString)
        {
            Console.Write("🔧 Nhập ID khách hàng cần khóa/mở khóa: ");
            if (!int.TryParse(Console.ReadLine(), out int customerId)) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string getStatusQuery = "SELECT Status FROM customers WHERE CustomerID = @Id";
            using var checkCmd = new MySqlCommand(getStatusQuery, connection);
            checkCmd.Parameters.AddWithValue("@Id", customerId);

            var statusObj = checkCmd.ExecuteScalar();
            if (statusObj == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy khách hàng.[/]");
                Thread.Sleep(1500);
                return;
            }

            int currentStatus = Convert.ToInt32(statusObj);
            int newStatus = currentStatus == 0 ? 1 : 0;

            string updateQuery = "UPDATE customers SET Status = @NewStatus WHERE CustomerID = @Id";
            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@NewStatus", newStatus);
            updateCmd.Parameters.AddWithValue("@Id", customerId);
            updateCmd.ExecuteNonQuery();

            string message = newStatus == 0 ? "✅ Tài khoản đã được mở khóa (bình thường)." : "⛔ Tài khoản đã bị khóa.";
            AnsiConsole.MarkupLine($"[green]{message}[/]");
            Thread.Sleep(2000);
        }
    }
}
