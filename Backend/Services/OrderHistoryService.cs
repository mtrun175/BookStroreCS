using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class OrderHistoryService
    {
        public static void ShowOrderHistory(string email, string connectionString)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("DON HANG")
                    .Centered()
                    .Color(Color.Green));

            AnsiConsole.Write(new Rule("[yellow]🧾 LỊCH SỬ ĐẶT HÀNG CỦA BẠN[/]").LeftJustified());

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var getCustomerIdCmd = new MySqlCommand("SELECT CustomerID FROM customers WHERE Email = @Email", connection);
                getCustomerIdCmd.Parameters.AddWithValue("@Email", email);
                var customerId = getCustomerIdCmd.ExecuteScalar();

                if (customerId == null)
                {
                    AnsiConsole.MarkupLine("[red]❌ Không tìm thấy thông tin khách hàng.[/]");
                    return;
                }

                var cmd = new MySqlCommand(@"
                    SELECT 
                        o.OrderID, 
                        b.Title, 
                        o.Quantity, 
                        o.TotalPrice,
                        o.Status, 
                        o.PaymentStatus,
                        o.CreatedAt
                    FROM orders o
                    JOIN books b ON o.BookID = b.BookID
                    WHERE o.CustomerID = @CustomerID
                    ORDER BY o.CreatedAt DESC", connection);

                cmd.Parameters.AddWithValue("@CustomerID", customerId);
                using var reader = cmd.ExecuteReader();

                if (!reader.HasRows)
                {
                    AnsiConsole.MarkupLine("[yellow]⚠️ Bạn chưa có đơn hàng nào.[/]");
                    return;
                }

                var orderList = new List<(int OrderID, string Title, int Quantity, decimal TotalPrice, string Status, string PaymentStatus, DateTime CreatedAt)>();
                var cancelableOrders = new List<int>();

                while (reader.Read())
                {
                    var status = reader["Status"].ToString() ?? "Không rõ";
                    var orderId = Convert.ToInt32(reader["OrderID"]);

                    if (status == "Đã đặt")
                        cancelableOrders.Add(orderId);

                    orderList.Add((
                        orderId,
                        reader["Title"].ToString() ?? "",
                        Convert.ToInt32(reader["Quantity"]),
                        Convert.ToDecimal(reader["TotalPrice"]),
                        status,
                        reader["PaymentStatus"].ToString() ?? "",
                        Convert.ToDateTime(reader["CreatedAt"])
                    ));
                }

                reader.Close(); // Đóng reader trước khi thực hiện các thao tác khác

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold aqua]📦 Danh sách đơn hàng[/]")
                    .AddColumn("[blue]Mã[/]")
                    .AddColumn("[green]Sách[/]")
                    .AddColumn("[yellow]SL[/]")
                    .AddColumn("[cyan]Tổng tiền[/]")
                    .AddColumn("[magenta]Trạng thái[/]")
                    .AddColumn("[green]Thanh toán[/]")
                    .AddColumn("[grey]Ngày đặt[/]");

                foreach (var order in orderList)
                {
                    string statusColor = order.Status switch
                    {
                        "Đã huỷ" => "red",
                        "Đã đặt" => "blue",
                        "Đã xác nhận" => "yellow",
                        "Đang giao" => "teal",
                        "Đã giao" => "green",
                        _ => "grey"
                    };

                    table.AddRow(
                        $"#{order.OrderID}",
                        order.Title,
                        order.Quantity.ToString(),
                        $"{order.TotalPrice:N0} đ",
                        $"[{statusColor}]{order.Status}[/]",
                        order.PaymentStatus,
                        order.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine("\n[bold cyan]Điều hướng:[/] ← [green]C[/]ancel Order | [green]Q[/]uit");
                AnsiConsole.Markup("[yellow]👉 Nhập lựa chọn của bạn: [/]");

                var choice = Console.ReadLine()?.Trim().ToLower();
                if (choice == "c")
                {
                    if (cancelableOrders.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[grey]📭 Không có đơn hàng nào có thể hủy.[/]");
                    }
                    else
                    {
                        int orderToCancel = AnsiConsole.Ask<int>("🔢 Nhập mã đơn hàng (OrderID) bạn muốn hủy (ví dụ: 3):");

                        if (!cancelableOrders.Contains(orderToCancel))
                        {
                            AnsiConsole.MarkupLine("[red]⚠️ Đơn hàng không tồn tại hoặc không thể hủy.[/]");
                        }
                        else
                        {
                            var confirm = AnsiConsole.Confirm($"❓ Bạn có chắc muốn hủy đơn hàng #{orderToCancel} không?");
                            if (confirm)
                            {
                                CancelOrder(orderToCancel, (int)customerId, connection);
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[grey]❎ Đã huỷ thao tác hủy đơn hàng.[/]");
                            }

                        }
                    }
                }
                else if (choice == "q")
                {
                    AnsiConsole.MarkupLine("[grey]🔙 Quay lại menu chính...[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ.[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi truy vấn đơn hàng: {ex.Message}[/]");
            }

            //AnsiConsole.MarkupLine("\n[grey]Nhấn bất kỳ phím để quay lại...[/]");
            Console.ReadKey(true);
        }

        private static void CancelOrder(int orderId, int customerId, MySqlConnection connection)
        {
            try
            {
                var getOrderCmd = new MySqlCommand(
    "SELECT BookID, Quantity FROM orders WHERE OrderID = @OrderID AND Status = 'Đã đặt'", connection);
                getOrderCmd.Parameters.AddWithValue("@OrderID", orderId);

                using var reader = getOrderCmd.ExecuteReader();
                if (!reader.Read())
                {
                    AnsiConsole.MarkupLine("[red]❌ Không thể tìm thấy đơn hàng hoặc đơn đã bị xử lý.[/]");
                    return;
                }

                int bookId = Convert.ToInt32(reader["BookID"]);
                int quantity = Convert.ToInt32(reader["Quantity"]);
                reader.Close();

                var updateOrderCmd = new MySqlCommand(
                    "UPDATE orders SET Status = 'Đã huỷ', UpdatedAt = NOW() WHERE OrderID = @OrderID", connection);
                updateOrderCmd.Parameters.AddWithValue("@OrderID", orderId);
                updateOrderCmd.ExecuteNonQuery();

                var updateBookCmd = new MySqlCommand(
                    "UPDATE books SET Quantity = Quantity + @Qty WHERE BookID = @BookID", connection);
                updateBookCmd.Parameters.AddWithValue("@Qty", quantity);
                updateBookCmd.Parameters.AddWithValue("@BookID", bookId);
                int updatedRows = updateBookCmd.ExecuteNonQuery();
                if (updatedRows == 0)
                {
                    AnsiConsole.MarkupLine("[red]⚠️ Không có sách nào được cập nhật trong kho.[/]");
                }

                var updateCustomerCmd = new MySqlCommand(
                    "UPDATE customers SET Canceled_orders = Canceled_orders + 1 WHERE CustomerID = @CustomerID", connection);
                updateCustomerCmd.Parameters.AddWithValue("@CustomerID", customerId);
                updateCustomerCmd.ExecuteNonQuery();

                AnsiConsole.MarkupLine($"[green]✅ Đơn hàng #{orderId} đã được hủy thành công. Sách đã được hoàn kho.[/]");

            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi hủy đơn hàng: {ex.Message}[/]");
            }
        }
    }
}
