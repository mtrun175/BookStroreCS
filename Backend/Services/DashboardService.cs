using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardService
    {
        public static void DisplayDashboard(string connectionString)
        {
            while (true)
            {
                Console.Clear();
                // ✅ Sửa lỗi: dùng Color.Blue thay vì Color.Cyan
                var title = new FigletText("Admin Panel")
                    .Centered();

                AnsiConsole.Write(title);

                AnsiConsole.MarkupLine("[bold yellow]1.[/] 📚 Quản lý sách");
                AnsiConsole.MarkupLine("[bold yellow]2.[/] 📦 Quản lý đơn hàng");
                AnsiConsole.MarkupLine("[bold yellow]3.[/] 👥 Quản lý khách hàng");
                AnsiConsole.MarkupLine("[bold yellow]4.[/] 🎟️ Quản lý mã giảm giá");
                AnsiConsole.MarkupLine("[bold yellow]5.[/] 🚪 Đăng xuất");

                Console.Write("\n👉 Chọn chức năng: ");
                var input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        Console.Clear();
                        BookService.DisplayBookList(connectionString);
                        Pause();
                        break;
                    case "2":
                        Console.Clear();
                        DashboardOrder.ManageOrders(connectionString);
                        Pause();
                        break;
                    case "3":
                        Console.Clear();
                        DashboardCustomer.ManageCustomers(connectionString);
                        Pause();
                        break;
                    case "4":
                        Console.Clear();
                        DashboardDiscount.ManageDiscounts(connectionString);
                        Pause();
                        break;
                    case "5":
                        return; // Đăng xuất
                    default:
                        AnsiConsole.MarkupLine("[red]❌ Lựa chọn không hợp lệ![/]");
                        Thread.Sleep(1500);
                        break;
                }
            }
        }

        private static void Pause()
        {
            Console.WriteLine("\nNhấn phím bất kỳ để quay lại menu...");
            Console.ReadKey();
        }
    }
}
