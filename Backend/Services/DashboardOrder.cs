using System;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardOrder
    {
        public static void ManageOrders(string connectionString)
        {
            while (true)
            {
                Console.Clear();
                // ✅ Sửa lỗi: Không dùng Styled nữa, chỉ tạo FigletText và thêm màu
                var title = new FigletText("Admin Panel")
                    .Centered();

                AnsiConsole.Write(title);

                AnsiConsole.MarkupLine("[cyan]1. Xem danh sách đơn hàng[/]");
                AnsiConsole.MarkupLine("[cyan]2. Xem chi tiết đơn hàng[/]");
                AnsiConsole.MarkupLine("[cyan]3. Cập nhật trạng thái đơn[/]");
                AnsiConsole.MarkupLine("[cyan]4. Hủy đơn hàng[/]");
                AnsiConsole.MarkupLine("[cyan]5. Quay lại[/]");

                Console.Write("🔎 Chọn chức năng: ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        DisplayOrderList(connectionString);
                        break;
                    case "2":
                        ViewOrderDetails(connectionString);
                        break;
                    case "3":
                        UpdateOrderStatus(connectionString);
                        break;
                    case "4":
                        CancelOrder(connectionString);
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

        private static void DisplayOrderList(string connectionString)
        {
            Console.Clear();
            AnsiConsole.MarkupLine("[bold]📋 Danh sách đơn hàng:[/]");

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT OrderID, CustomerID, OrderDate, Status 
                FROM orders 
                ORDER BY OrderDate DESC";

            using var cmd = new MySqlCommand(query, connection);
            using var reader = cmd.ExecuteReader();

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumns("Order ID", "Customer ID", "Ngày đặt", "Trạng thái");

            while (reader.Read())
            {
                table.AddRow(
                    reader["OrderID"].ToString(),
                    reader["CustomerID"].ToString(),
                    reader["OrderDate"].ToString(),
                    GetStatusText(Convert.ToInt32(reader["Status"]))
                );
            }

            AnsiConsole.Write(table);
            Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
            Console.ReadKey();
        }

        private static void ViewOrderDetails(string connectionString)
        {
            Console.Write("🔍 Nhập ID đơn hàng cần xem: ");
            if (!int.TryParse(Console.ReadLine(), out int orderId)) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = @"
                SELECT o.OrderID, o.CustomerID, o.OrderDate, o.Status,
                       c.FullName, c.Email
                FROM orders o
                JOIN customers c ON o.CustomerID = c.CustomerID
                WHERE o.OrderID = @OrderID";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@OrderID", orderId);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[bold]🧾 Thông tin đơn hàng:[/]");
                AnsiConsole.MarkupLine($"[yellow]Order ID:[/] {reader["OrderID"]}");
                AnsiConsole.MarkupLine($"[yellow]Khách hàng:[/] {reader["FullName"]} ({reader["Email"]})");
                AnsiConsole.MarkupLine($"[yellow]Ngày đặt:[/] {reader["OrderDate"]}");
                AnsiConsole.MarkupLine($"[yellow]Trạng thái:[/] {GetStatusText(Convert.ToInt32(reader["Status"]))}");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy đơn hàng.[/]");
            }

            Console.WriteLine("\nNhấn phím bất kỳ để quay lại...");
            Console.ReadKey();
        }

        private static void UpdateOrderStatus(string connectionString)
        {
            Console.Write("🔄 Nhập ID đơn hàng cần cập nhật: ");
            if (!int.TryParse(Console.ReadLine(), out int orderId)) return;

            Console.WriteLine("Chọn trạng thái mới:");
            Console.WriteLine("1. Chờ xử lý");
            Console.WriteLine("2. Đang giao");
            Console.WriteLine("3. Hoàn tất");

            Console.Write("➡️ Nhập số tương ứng: ");
            if (!int.TryParse(Console.ReadLine(), out int newStatus) || newStatus < 1 || newStatus > 3)
            {
                AnsiConsole.MarkupLine("[red]❌ Trạng thái không hợp lệ.[/]");
                return;
            }

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = "UPDATE orders SET Status = @Status WHERE OrderID = @OrderID";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@Status", newStatus);
            cmd.Parameters.AddWithValue("@OrderID", orderId);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]✅ Cập nhật trạng thái thành công![/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy đơn hàng.[/]");

            Thread.Sleep(1500);
        }

        private static void CancelOrder(string connectionString)
        {
            Console.Write("❌ Nhập ID đơn hàng cần hủy: ");
            if (!int.TryParse(Console.ReadLine(), out int orderId)) return;

            Console.Write("⚠️ Bạn chắc chắn muốn hủy? (y/N): ");
            var confirm = Console.ReadLine();
            if (!confirm?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? true) return;

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            string query = "UPDATE orders SET Status = 0 WHERE OrderID = @OrderID"; // 0 = hủy
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@OrderID", orderId);

            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                AnsiConsole.MarkupLine("[green]🗑️ Đơn hàng đã được hủy![/]");
            else
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy đơn hàng.[/]");

            Thread.Sleep(1500);
        }

        private static string GetStatusText(int status)
        {
            return status switch
            {
                0 => "[red]Đã hủy[/]",
                1 => "[yellow]Chờ xử lý[/]",
                2 => "[blue]Đang giao[/]",
                3 => "[green]Hoàn tất[/]",
                _ => "[grey]Không xác định[/]"
            };
        }
    }
}
