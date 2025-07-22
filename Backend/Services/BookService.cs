using Spectre.Console;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;

namespace BookStoreConsoleApp.Services
{
    public static class BookService
    {
        public static string? DisplayBookList(string connectionString, string? loggedInUserEmail)

        //public static void DisplayBookList(string connectionString, string? loggedInUserEmail)
        {
            const int pageSize = 10;
            int currentPage = 1;

            while (true)
            {
                Console.Clear();
                var table = new Table
                {
                    Border = TableBorder.Rounded,
                    Title = new TableTitle($"[yellow bold underline]\uD83D\uDCDA DANH SÁCH SÁCH — TRANG {currentPage}[/]")
                };

                table.AddColumns("STT", "Mã sách", "Tên sách", "Tác giả", "Giá (VNĐ)", "Số lượng", "Thể loại", "Mô tả");

                try
                {
                    using var connection = new MySqlConnection(connectionString);
                    connection.Open();

                    var offset = (currentPage - 1) * pageSize;
                    var query = @"
                        SELECT 
                            b.BookID, b.Title, b.Author, b.Price, b.Quantity, b.Description, 
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
                        // table.AddRow(
                        //     index++.ToString(),
                        //     reader["BookID"].ToString(),
                        //     reader["Title"].ToString(),
                        //     reader["Author"].ToString(),
                        //     string.Format("{0:N0}", reader.GetDecimal("Price")),
                        //     reader["Quantity"].ToString(),
                        //     reader["CategoryName"]?.ToString() ?? "[grey italic]Chưa có[/]",
                        //     reader["Description"].ToString()
                        // );
                        table.AddRow(
    $"[bold yellow]{index++}[/]",
    $"[cyan]{reader["BookID"]}[/]",
    $"[white]{reader["Title"]}[/]",
    $"[green]{reader["Author"]}[/]",
    $"[bold orange1]{string.Format("{0:N0}", reader.GetDecimal("Price"))}[/]",
    $"[purple]{reader["Quantity"]}[/]",
    reader["CategoryName"]?.ToString() != null
        ? $"[aqua]{reader["CategoryName"]}[/]"
        : "[grey italic]Chưa có[/]",
    $"[grey]{reader["Description"]}[/]"
);

                    }

                    AnsiConsole.Write(table);

                    AnsiConsole.MarkupLine("\n[bold cyan]Điều hướng:[/] ← [green]P[/]revious | [green]N[/]ext → | [green]Q[/]uit | [green]B[/]uy | [green]S[/]earch");
                    AnsiConsole.Markup("[yellow]👉 Nhập lựa chọn của bạn: [/]");

                    string? input = Console.ReadLine()?.Trim().ToLower();

                    switch (input)
                    {
                        case "n":
                            currentPage++;
                            break;

                        case "p":
                            if (currentPage > 1) currentPage--;
                            break;

                        case "b":
                            if (string.IsNullOrWhiteSpace(loggedInUserEmail))
                            {
                                var newEmail = HandleBookPurchase(connectionString, loggedInUserEmail);
                                if (!string.IsNullOrWhiteSpace(newEmail))
                                {
                                    loggedInUserEmail = newEmail;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(loggedInUserEmail))
                            {
                                HandleBookPurchase(connectionString, loggedInUserEmail);
                            }
                            break;

                        case "q":
                            var confirmQuit = AnsiConsole.Confirm("❓ Bạn có chắc chắn muốn thoát không?");
                            if (confirmQuit)
                                return loggedInUserEmail;
                            break;
                        case "s":
                            {
                                SearchBooks(connectionString); // 👈 Gọi hàm tìm kiếm
                                break;
                                // (Các case khác giữ nguyên)
                            }

                        default:
                            AnsiConsole.MarkupLine("[red]⚠ Lựa chọn không hợp lệ. Vui lòng thử lại.[/]");
                            PauseScreen();
                            break;
                    }

                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Lỗi truy vấn:[/] {ex.Message}");
                    PauseScreen();
                    return loggedInUserEmail;
                }
            }
        }
        private static void SearchBooks(string connectionString)
        {
            Console.Clear();

            string keyword = AnsiConsole.Ask<string>("🔍 Nhập tên sách hoặc tên thể loại cần tìm:");

            var table = new Table
            {
                Border = TableBorder.Rounded,
                Title = new TableTitle($"[yellow bold underline]🔎 KẾT QUẢ TÌM KIẾM: {keyword}[/]")
            };

            table.AddColumns("STT", "Mã sách", "Tên sách", "Tác giả", "Giá (VNĐ)", "Số lượng", "Thể loại", "Mô tả");

            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();

                var query = @"
            SELECT 
                b.BookID, b.Title, b.Author, b.Price, b.Quantity, b.Description,
                c.Name AS CategoryName
            FROM books b
            LEFT JOIN categories c ON b.CategoryID = c.CategoryID
            WHERE b.Title LIKE @Keyword OR c.Name LIKE @Keyword";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Keyword", $"%{keyword}%");

                using var reader = cmd.ExecuteReader();
                int index = 1;

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
                        reader["Description"].ToString()
                    );
                }

                if (index == 1)
                {
                    AnsiConsole.MarkupLine("[red]❌ Không tìm thấy sách nào phù hợp.[/]");
                }
                else
                {
                    AnsiConsole.Write(table);
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi tìm kiếm: {ex.Message}[/]");
            }

            PauseScreen();
        }

        private static string? HandleBookPurchase(string connectionString, string? loggedInUserEmail)

        {
            Console.Clear();

            if (string.IsNullOrWhiteSpace(loggedInUserEmail))
            {
                AnsiConsole.MarkupLine("\n[red bold]⚠ Bạn cần đăng nhập để mua sách.[/]");
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[yellow bold]👉 Vui lòng chọn thao tác:[/]")
                        .PageSize(3)
                        .HighlightStyle("green bold")
                        .AddChoices("🔐 Đăng nhập", "📝 Đăng ký", "❌ Hủy"));

                switch (choice)
                {
                    case "🔐 Đăng nhập":
                        return AuthService.Login(connectionString); // ✨ CHỈ trả về email
                    case "📝 Đăng ký":
                        AuthService.Register(connectionString);
                        return AuthService.Login(connectionString);
                    default:
                        return null;
                }
            }



            AnsiConsole.Markup("[yellow]🔢 Nhập mã sách bạn muốn mua: [/] ");
            string bookId = Console.ReadLine()?.Trim() ?? "";

            using var connection = new MySqlConnection(connectionString);
            connection.Open();

            var getBookCmd = new MySqlCommand("SELECT * FROM books WHERE BookID = @BookID", connection);
            getBookCmd.Parameters.AddWithValue("@BookID", bookId);
            using var reader = getBookCmd.ExecuteReader();

            if (!reader.Read())
            {
                AnsiConsole.MarkupLine("[red]❌ Không tìm thấy sách với mã đã nhập.[/]");
                PauseScreen();
                return null;
            }

            string title = reader["Title"].ToString()!;
            string author = reader["Author"].ToString()!;
            string description = reader["Description"].ToString()!;
            decimal price = reader.GetDecimal("Price");
            int availableQty = reader.GetInt32("Quantity");

            if (availableQty <= 0)
            {
                AnsiConsole.MarkupLine("[red bold]❌ Sách đã hết hàng. Bạn không thể đặt mua.[/]");
                PauseScreen();
                return null;
            }

            reader.Close();

            Console.Clear();
            AnsiConsole.Write(new Panel($@"
[green]📗 Tên:[/] {title}
[blue]✍️  Tác giả:[/] {author}
[orange1]💰 Giá:[/] {price:N0} VNĐ
[purple]📦 Số lượng còn:[/] {availableQty}
[grey]📄 Mô tả:[/] {description}
").Header("🔍 Thông tin sách").BorderColor(Color.Aqua));

            var confirm = AnsiConsole.Confirm("🛍️ Bạn có chắc chắn muốn tiếp tục đặt mua cuốn sách này?");
            if (!confirm)
            {
                AnsiConsole.MarkupLine("[grey]❎ Đã hủy giao dịch.[/]");
                PauseScreen();
                return null;
            }

            int buyQty = AnsiConsole.Ask<int>("🛒 Nhập số lượng muốn mua:");
            if (buyQty <= 0 || buyQty > availableQty)
            {
                AnsiConsole.MarkupLine("[red]❌ Số lượng không hợp lệ hoặc vượt quá tồn kho.[/]");
                PauseScreen();
                return null;
            }

            object? customerId;
            string defaultName = "", defaultPhone = "", defaultAddress = "", defaultAddressDetail = "";

            // Truy vấn bằng Email (nếu currentUsername thực chất là email)
            using (var getCustomerCmd = new MySqlCommand(@"
    SELECT CustomerID, FullName, PhoneNumber, Address, AddressDetail 
    FROM customers 
    WHERE Email = @Email", connection))
            {
                getCustomerCmd.Parameters.AddWithValue("@Email", loggedInUserEmail); // currentUsername

                using var custReader = getCustomerCmd.ExecuteReader();
                if (custReader.Read())
                {
                    customerId = custReader["CustomerID"];
                    defaultName = custReader["FullName"].ToString() ?? "";
                    defaultPhone = custReader["PhoneNumber"].ToString() ?? "";
                    defaultAddress = custReader["Address"].ToString() ?? "";
                    defaultAddressDetail = custReader["AddressDetail"].ToString() ?? "";
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]❌ Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại.[/]");
                    PauseScreen();
                    return null;
                }
            }


            string receiverName = AnsiConsole.Ask<string>($"👤 Tên người nhận [grey italic](mặc định: {defaultName})[/]:", defaultName);
            string phone;
            do
            {
                phone = AnsiConsole.Ask<string>($"📞 Số điện thoại [grey italic](mặc định: {defaultPhone})[/]:", defaultPhone);
                if (!Regex.IsMatch(phone, @"^(0|\+84)\d{9,10}$"))
                    AnsiConsole.MarkupLine("[red]❌ Số điện thoại không hợp lệ. Vui lòng thử lại.[/]");
            } while (!Regex.IsMatch(phone, @"^(0|\+84)\d{9,10}$"));

            string address = AnsiConsole.Ask<string>($"🏠 Địa chỉ [grey italic](mặc định: {defaultAddress})[/]:", defaultAddress);
            string addressDetail = AnsiConsole.Ask<string>($"🏠 Địa chỉ cụ thể [grey italic](mặc định: {defaultAddressDetail})[/]:", defaultAddressDetail);

            decimal total = price * buyQty;

            using var orderCmd = new MySqlCommand(@"
                INSERT INTO orders
                (CustomerID, PhoneNumber, ReceiverName, Address, AddressDetail, BookID, BookTitle, Quantity, TotalPrice, Status, PaymentStatus, CreatedAt, UpdatedAt)
                VALUES
                (@CustomerID, @Phone, @Receiver, @Address, @AddressDetail, @BookID, @Title, @Qty, @Total, 'Đã đặt', 'Chưa thanh toán', NOW(), NOW())", connection);

            orderCmd.Parameters.AddWithValue("@CustomerID", customerId);
            orderCmd.Parameters.AddWithValue("@Phone", phone);
            orderCmd.Parameters.AddWithValue("@Receiver", receiverName);
            orderCmd.Parameters.AddWithValue("@Address", address);
            orderCmd.Parameters.AddWithValue("@AddressDetail", addressDetail);
            orderCmd.Parameters.AddWithValue("@BookID", bookId);
            orderCmd.Parameters.AddWithValue("@Title", title);
            orderCmd.Parameters.AddWithValue("@Qty", buyQty);
            orderCmd.Parameters.AddWithValue("@Total", total);

            try
            {
                orderCmd.ExecuteNonQuery();

                var updateQtyCmd = new MySqlCommand("UPDATE books SET Quantity = Quantity - @BuyQty WHERE BookID = @BookID", connection);
                updateQtyCmd.Parameters.AddWithValue("@BuyQty", buyQty);
                updateQtyCmd.Parameters.AddWithValue("@BookID", bookId);
                updateQtyCmd.ExecuteNonQuery();

                var updateOrderCountCmd = new MySqlCommand("UPDATE customers SET Total_orders = Total_orders + 1 WHERE CustomerID = @CustomerID", connection);
                updateOrderCountCmd.Parameters.AddWithValue("@CustomerID", customerId);
                updateOrderCountCmd.ExecuteNonQuery();

                // AnsiConsole.MarkupLine($"[green bold]✅ Đặt mua thành công \"{title}\" ({buyQty} cuốn). Tổng tiền: {total:N0} VNĐ[/]");
                AnsiConsole.Clear();
                AnsiConsole.Write(new Panel($@"
[green]📗 Sách:[/] {title}
[blue]🧾 Mã sách:[/] {bookId}
[orange1]💰 Giá / cuốn:[/] {price:N0} VNĐ
[purple]📦 Số lượng mua:[/] {buyQty}
[cyan]🧮 Tổng cộng:[/] {total:N0} VNĐ

[green]👤 Người nhận:[/] {receiverName}
[blue]📞 SĐT:[/] {phone}
[orange1]🏠 Địa chỉ:[/] {address}
[grey]🏠 Cụ thể:[/] {addressDetail}

[bold yellow]🕒 Ngày đặt:[/] {DateTime.Now:dd/MM/yyyy HH:mm}
").Header("[bold green]🧾 HÓA ĐƠN ĐẶT HÀNG[/]").BorderColor(Color.Green));

            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Lỗi khi đặt hàng: {ex.Message}[/]");
            }

            PauseScreen();
            return loggedInUserEmail;

        }

        private static void PauseScreen()
        {
            AnsiConsole.MarkupLine("\n[grey]⏳ Nhấn phím bất kỳ để tiếp tục...[/]");
            try { Console.ReadKey(true); } catch { }
        }
    }
}
