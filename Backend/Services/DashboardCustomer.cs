using System;
using MySql.Data.MySqlClient;
using Spectre.Console;
using System.Collections.Generic;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardCustomer
    {
        private const int PageSize = 10;

        public static void ManageCustomers(string connectionString)
        {
            int currentPage = 1;

            while (true)
            {
                Console.Clear();
                var title = new FigletText("🧾 CUSTOMER MANAGER")
                    .Centered()
                    .Color(Color.Teal);
                AnsiConsole.Write(title);
                AnsiConsole.Write(new Rule("[deepskyblue1]📦 QUẢN LÝ KHÁCH HÀNG[/]").LeftJustified());
                // Console.Clear();
                // AnsiConsole.Write(new FigletText("CUSTOMER MANAGER").Centered().Color(Color.Green));

                var customers = GetCustomers(connectionString, currentPage, out int totalCustomers);
                int totalPages = (int)Math.Ceiling(totalCustomers / (double)PageSize);

                var table = new Table()
                    .Title($"[bold yellow underline]📋 DANH SÁCH KHÁCH HÀNG — TRANG {currentPage}/{totalPages}[/]")
                    .Border(TableBorder.Rounded);

                table.AddColumns("ID", "Họ tên", "Email", "SĐT", "Địa chỉ", "Trạng thái", "Tổng đơn");

                foreach (var c in customers)
                {
                    table.AddRow(
                        c.CustomerID.ToString(),
                        c.FullName,
                        c.Email,
                        c.PhoneNumber,
                        c.Address,
                        c.Status == 0 ? "[green]✅ Bình thường[/]" : "[red]⛔ Bị khóa[/]",
                        c.TotalOrders.ToString()
                    );
                }

                AnsiConsole.Write(table);

                AnsiConsole.MarkupLine("\n[bold cyan]Chức năng:[/] ← [green]P[/]revious | [green]N[/]ext → | [green]S[/]earch | [green]L[/]ock/Unlock | [green]Q[/]uit");
                AnsiConsole.Markup("[yellow]👉 Nhập lựa chọn của bạn: [/]");

                string? input = Console.ReadLine()?.Trim().ToLower();

                switch (input)
                {
                    case "n":
                        if (currentPage < totalPages) currentPage++;
                        break;
                    case "p":
                        if (currentPage > 1) currentPage--;
                        break;
                    case "s":
                        SearchCustomer(connectionString);
                        break;
                    case "l":
                        ToggleCustomerStatus(connectionString);
                        break;
                    case "q":
                        if (AnsiConsole.Confirm("❓ Bạn có chắc chắn muốn thoát không?"))
                            return;
                        break;
                    default:
                        AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ.[/]");
                        PauseScreen();
                        break;
                }
            }
        }

        private static List<CustomerModel> GetCustomers(string connectionString, int page, out int totalCount)
        {
            var list = new List<CustomerModel>();
            totalCount = 0;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string countQuery = "SELECT COUNT(*) FROM customers";
            using (var countCmd = new MySqlCommand(countQuery, connection))
                totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

            string query = @"
                SELECT 
                    c.CustomerID, c.FullName, c.Email, c.PhoneNumber, c.Address, c.Status,
                    (SELECT COUNT(*) FROM orders o WHERE o.CustomerID = c.CustomerID) AS TotalOrders
                FROM customers c
                LIMIT @Limit OFFSET @Offset";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Limit", PageSize);
            cmd.Parameters.AddWithValue("@Offset", (page - 1) * PageSize);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CustomerModel
                {
                    CustomerID = Convert.ToInt32(reader["CustomerID"]),
                    FullName = reader["FullName"].ToString()!,
                    Email = reader["Email"].ToString()!,
                    PhoneNumber = reader["PhoneNumber"].ToString()!,
                    Address = reader["Address"].ToString()!,
                    Status = Convert.ToInt32(reader["Status"]),
                    TotalOrders = Convert.ToInt32(reader["TotalOrders"])
                });
            }

            return list;
        }

        private static void SearchCustomer(string connectionString)
        {
            Console.Clear();
            string keyword = AnsiConsole.Ask<string>("🔍 Nhập tên hoặc email cần tìm:");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT CustomerID, FullName, Email, PhoneNumber, Address, Status 
                FROM customers 
                WHERE FullName LIKE @kw OR Email LIKE @kw";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");

            using var reader = cmd.ExecuteReader();

            var table = new Table()
                .Title("[bold yellow]🔍 Kết quả tìm kiếm[/]")
                .Border(TableBorder.Rounded)
                .AddColumns("ID", "Họ tên", "Email", "SĐT", "Địa chỉ", "Trạng thái");

            bool found = false;
            while (reader.Read())
            {
                found = true;
                table.AddRow(
                    reader["CustomerID"].ToString(),
                    reader["FullName"].ToString(),
                    reader["Email"].ToString(),
                    reader["PhoneNumber"].ToString(),
                    reader["Address"].ToString(),
                    Convert.ToInt32(reader["Status"]) == 0 ? "[green]✅ Bình thường[/]" : "[red]⛔ Bị khóa[/]"
                );
            }

            if (!found)
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy khách hàng nào.[/]");
            else
                AnsiConsole.Write(table);

            PauseScreen();
        }

        private static void ToggleCustomerStatus(string connectionString)
        {
            int customerId = AnsiConsole.Ask<int>("🔧 Nhập [cyan]ID khách hàng[/] cần khóa/mở khóa:");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string getStatusQuery = "SELECT Status FROM customers WHERE CustomerID = @Id";
            using var checkCmd = new MySqlCommand(getStatusQuery, connection);
            checkCmd.Parameters.AddWithValue("@Id", customerId);

            var statusObj = checkCmd.ExecuteScalar();
            if (statusObj == null)
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy khách hàng.[/]");
                PauseScreen();
                return;
            }

            int currentStatus = Convert.ToInt32(statusObj);
            int newStatus = currentStatus == 0 ? 1 : 0;

            string updateQuery = "UPDATE customers SET Status = @NewStatus WHERE CustomerID = @Id";
            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@NewStatus", newStatus);
            updateCmd.Parameters.AddWithValue("@Id", customerId);
            updateCmd.ExecuteNonQuery();

            string message = newStatus == 0 ? "[green]✅ Tài khoản đã được mở khóa.[/]" : "[red]⛔ Tài khoản đã bị khóa.[/]";
            AnsiConsole.MarkupLine(message);
            PauseScreen();
        }

        private static void PauseScreen()
        {
            AnsiConsole.MarkupLine("\n[grey]⏳ Nhấn phím bất kỳ để tiếp tục...[/]");
            try { Console.ReadKey(true); } catch { }
        }

        private class CustomerModel
        {
            public int CustomerID { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Address { get; set; } = "";
            public int Status { get; set; }
            public int TotalOrders { get; set; }
        }
    }
}
