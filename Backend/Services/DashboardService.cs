using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class DashboardService
    {
        public static void DisplayDashboard(string connectionString, string? loggedInUser)
        {
            while (true)
            {
                Console.Clear();
                DisplayBanner();
                DisplayWelcome(loggedInUser);

                var menu = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\n[bold yellow]👉 Chọn chức năng quản trị:[/]")
                        .PageSize(7)
                        .AddChoices(new[]
                        {
                            "📚 Quản lý sách",
                            "📦 Quản lý đơn hàng",
                            "👥 Quản lý khách hàng",
                            //"🎟️ Quản lý mã giảm giá",
                            "🚪 Đăng xuất"
                        }));

                switch (menu)
                {
                    case "📚 Quản lý sách":
                        Console.Clear();
                        DashboardBookService.ShowBookDashboard(connectionString);
                        PauseScreen();
                        break;

                    case "📦 Quản lý đơn hàng":
                        Console.Clear();
                        DashboardOrder.ManageOrders(connectionString);
                        PauseScreen();
                        break;

                    case "👥 Quản lý khách hàng":
                        Console.Clear();
                        DashboardCustomer.ManageCustomers(connectionString);
                        PauseScreen();
                        break;

                    // case "🎟️ Quản lý mã giảm giá":
                    //     Console.Clear();
                    //     DashboardDiscount.ManageDiscounts(connectionString);
                    //     PauseScreen();
                    //     break;

                    case "🚪 Đăng xuất":
                        var confirm = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("❓ [yellow]Bạn có chắc chắn muốn đăng xuất không?[/]")
                                .AddChoices("✅ Có", "❌ Không"));

                        if (confirm == "✅ Có")
                            return; // Thoát khỏi dashboard
                        else
                        {
                            AnsiConsole.MarkupLine("[grey]⏩ Đã hủy lệnh đăng xuất.[/]");
                            Thread.Sleep(1500);
                        }
                        break;
                }
            }
        }

        private static void DisplayBanner()
        {
            var title = new FigletText("ADMIN DASHBOARD")
                .Centered()
                .Color(Color.Red);
            AnsiConsole.Write(title);
        }

        private static void DisplayWelcome(string? username)
        {
            var text = $"[bold green]Xin chào quản trị viên, [underline]{username ?? "Admin"}[/]![/]";
            var panel = new Panel(text)
                .Border(BoxBorder.Double)
                .Padding(1, 1)
                .Expand();
            AnsiConsole.Write(panel);
        }

        private static void PauseScreen()
        {
            AnsiConsole.MarkupLine("[grey]\nNhấn phím bất kỳ để quay lại menu...[/]");
            Console.ReadKey(true);
        }
    }
}
