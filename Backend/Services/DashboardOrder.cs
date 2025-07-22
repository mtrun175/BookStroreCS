using System;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardOrder
    {
        private const int PageSize = 10;

        public static void ManageOrders(string connectionString)
        {
            int currentPage = 1;
            while (true)
            {
                Console.Clear();
                var title = new FigletText("🧾 MANAGER ORDERS")
                    .Centered()
                    .Color(Color.Teal);
                AnsiConsole.Write(title);
                AnsiConsole.Write(new Rule("[deepskyblue1]📦 QUẢN LÝ ĐƠN HÀNG[/]").LeftJustified());

                DisplayPagedOrders(connectionString, currentPage);

                AnsiConsole.MarkupLine("\n[yellow]Chức năng:[/]");
                AnsiConsole.MarkupLine("[cyan]N. Trang tiếp | P. Trang trước | U. Cập nhật trạng thái | H. Hủy đơn | B. Quay lại[/]");
                Console.Write("🔎 Chọn chức năng: ");
                var input = Console.ReadLine()?.Trim().ToUpper();

                switch (input)
                {
                    case "N": currentPage++; break;
                    case "P": if (currentPage > 1) currentPage--; break;
                    case "U": UpdateOrderStatus(connectionString); break;
                    case "H": CancelOrder(connectionString); break;
                    case "B": return;
                    default:
                        AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ![/]");
                        Thread.Sleep(1000);
                        break;
                }
            }
        }

        private static void DisplayPagedOrders(string connectionString, int page)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                int offset = (page - 1) * PageSize;

                string query = @"
                    SELECT OrderID, BookTitle, Quantity, CustomerID, ReceiverName, PhoneNumber,
                           Address, AddressDetail, CreatedAt, Status, TotalPrice, PaymentStatus
                    FROM orders
                    ORDER BY CreatedAt DESC
                    LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@PageSize", PageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);

                using var reader = cmd.ExecuteReader();

                var table = new Table().Border(TableBorder.Rounded)
                    .Title("[bold lime]📋 DANH SÁCH ĐƠN HÀNG[/]")
                    .AddColumn("[blue]ID[/]")
                    .AddColumn("[green]Sách[/]")
                    .AddColumn("[yellow]Số lượng[/]")
                    .AddColumn("[cyan]Khách hàng[/]")
                    .AddColumn("[cyan]Người nhận[/]")
                    .AddColumn("[magenta]Điện thoại[/]")
                    .AddColumn("[grey]Địa chỉ[/]")
                    .AddColumn("[grey]Ngày đặt[/]")
                    .AddColumn("[green]Trạng thái[/]")
                    .AddColumn("[blue]Tổng tiền[/]")
                    .AddColumn("[red]Thanh toán[/]");

                while (reader.Read())
                {
                    table.AddRow(
                        reader["OrderID"].ToString(),
                        reader["BookTitle"].ToString(),
                        reader["Quantity"].ToString(),
                        reader["CustomerID"].ToString(),
                        reader["ReceiverName"].ToString(),
                        reader["PhoneNumber"].ToString(),
                        $"{reader["Address"]}, {reader["AddressDetail"]}",
                        Convert.ToDateTime(reader["CreatedAt"]).ToString("dd/MM/yyyy"),
                        GetStatusText(reader["Status"]?.ToString() ?? ""),
                        string.Format("{0:N0}₫", reader["TotalPrice"]),
                        reader["PaymentStatus"].ToString()
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"[green]Trang:[/] {page}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi hiển thị đơn hàng: {ex.Message}[/]");
            }
        }

        private static void UpdateOrderStatus(string connectionString)
        {
            try
            {
                Console.Write("🆔 Nhập ID đơn hàng cần cập nhật: ");
                if (!int.TryParse(Console.ReadLine(), out int orderId)) return;

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string selectQuery = "SELECT Status FROM orders WHERE OrderID = @OrderID";
                using var selectCmd = new MySqlCommand(selectQuery, connection);
                selectCmd.Parameters.AddWithValue("@OrderID", orderId);
                var statusObj = selectCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    AnsiConsole.MarkupLine("[red]❌ Không tìm thấy đơn hàng.[/]");
                    Thread.Sleep(1500);
                    return;
                }

                string currentStatus = statusObj.ToString().Trim().ToLower();

                string nextStatus = currentStatus switch
                {
                    "đã đặt" => "Đã xác nhận",
                    "đã xác nhận" => "Đang giao",
                    "đang giao" => "Đã giao",
                    "đã giao" or "đã hủy" or "đã huỷ" => null,
                    _ => null
                };

                if (nextStatus == null)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ Không thể cập nhật đơn hàng này (đơn đã giao hoặc đã huỷ).[/]");
                    Thread.Sleep(1500);
                    return;
                }

                Console.Write($"❓ Bạn có chắc muốn cập nhật trạng thái đơn hàng sang '{nextStatus}'? (Y/N): ");
                string confirm = Console.ReadLine()?.Trim().ToUpper();
                if (confirm != "Y") return;

                string updateQuery = @"
                    UPDATE orders 
                    SET Status = @NewStatus, 
                        UpdatedAt = NOW(), 
                        PaymentStatus = CASE WHEN @NewStatus = 'Đã giao' THEN 'Đã thanh toán' ELSE PaymentStatus END
                    WHERE OrderID = @OrderID";

                using var updateCmd = new MySqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@NewStatus", nextStatus);
                updateCmd.Parameters.AddWithValue("@OrderID", orderId);

                int rows = updateCmd.ExecuteNonQuery();
                AnsiConsole.MarkupLine(rows > 0
                    ? "[green]✅ Cập nhật trạng thái thành công![/]"
                    : "[red]❌ Không thể cập nhật đơn hàng.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi cập nhật: {ex.Message}[/]");
            }

            Thread.Sleep(1500);
        }

        private static void CancelOrder(string connectionString)
        {
            try
            {
                Console.Write("🆔 Nhập ID đơn hàng cần hủy: ");
                if (!int.TryParse(Console.ReadLine(), out int orderId)) return;

                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                string selectQuery = "SELECT Status FROM orders WHERE OrderID = @OrderID";
                using var selectCmd = new MySqlCommand(selectQuery, connection);
                selectCmd.Parameters.AddWithValue("@OrderID", orderId);
                var statusObj = selectCmd.ExecuteScalar();

                if (statusObj == null)
                {
                    AnsiConsole.MarkupLine("[red]❌ Không tìm thấy đơn hàng.[/]");
                    Thread.Sleep(1500);
                    return;
                }

                string currentStatus = statusObj.ToString().Trim().ToLower();

                if (currentStatus is "đã giao" or "đã hủy" or "đã huỷ")
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ Không thể hủy đơn hàng này (đơn đã giao hoặc đã huỷ).[/]");
                    Thread.Sleep(1500);
                    return;
                }

                Console.Write("❓ Bạn có chắc muốn hủy đơn hàng này? (Y/N): ");
                string confirm = Console.ReadLine()?.Trim().ToUpper();
                if (confirm != "Y") return;

                string updateQuery = "UPDATE orders SET Status = 'Đã huỷ', UpdatedAt = NOW() WHERE OrderID = @OrderID";
                using var updateCmd = new MySqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@OrderID", orderId);

                int rows = updateCmd.ExecuteNonQuery();
                AnsiConsole.MarkupLine(rows > 0
                    ? "[green]✅ Đơn hàng đã được huỷ thành công![/]"
                    : "[red]❌ Không thể huỷ đơn hàng.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi huỷ đơn hàng: {ex.Message}[/]");
            }

            Thread.Sleep(1500);
        }

        private static string GetStatusText(string status)
        {
            string s = status.Trim().ToLower();

            return s switch
            {
                "đã đặt" => "[blue]Đã đặt[/]",
                "đã xác nhận" => "[yellow]Đã xác nhận[/]",
                "đang giao" => "[cyan]Đang giao[/]",
                "đã giao" => "[green]Đã giao[/]",
                "đã hủy" or "đã huỷ" => "[red]Đã hủy[/]",
                _ => "[grey]Không xác định[/]"
            };
        }
    }
}
