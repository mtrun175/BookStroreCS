using System;
using Spectre.Console;
using BookStoreConsoleApp.Services;

class Program
{
    static void Main(string[] args)
    {
        string connectionString = "server=localhost;user=root;pwd=mtrun0run;database=bookstoredb";
        bool isLoggedIn = false;
        string? currentUsername = null;

        using (var dbService = new DatabaseService(connectionString))
        {
            dbService.OpenConnection();

            while (true)
            {
                Console.Clear();
                DisplayBanner();
                HomePageService.DisplayHomePage(isLoggedIn, currentUsername);

                // Chọn menu tương ứng trạng thái đăng nhập
                var menuOptions = isLoggedIn
                    ? new[] {
                        "📚 Xem danh sách sách",
                        "👤 Thông tin tài khoản",
                        "🔓 Đăng xuất",
                        "❌ Thoát chương trình"
                    }
                    : new[] {
                        "📚 Xem danh sách sách",
                        "📝 Đăng ký tài khoản",
                        "🔐 Đăng nhập",
                        "❌ Thoát chương trình"
                    };

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\n[bold yellow]👉 Chọn một tùy chọn:[/]")
                        .PageSize(6)
                        .AddChoices(menuOptions)
                );

                if (!isLoggedIn)
                {
                    switch (choice)
                    {
                        case "📚 Xem danh sách sách":
                            BookService.DisplayBookList(connectionString);
                            PauseScreen();
                            break;
                        case "📝 Đăng ký tài khoản":
                            AuthService.Register(connectionString);
                            PauseScreen();
                            break;
                        case "🔐 Đăng nhập":
                            var username = AuthService.Login(connectionString);
                            if (username != null)
                            {
                                isLoggedIn = true;
                                currentUsername = username;
                                AnsiConsole.MarkupLine("[green]✅ Đăng nhập thành công![/]");
                            }
                            else
                            {
                                AnsiConsole.MarkupLine("[red]❌ Đăng nhập thất bại. Vui lòng thử lại.[/]");
                            }
                            PauseScreen();
                            break;
                        case "❌ Thoát chương trình":
                            ExitProgram();
                            return;
                    }
                }
                else
                {
                    switch (choice)
                    {
                        case "📚 Xem danh sách sách":
                            BookService.DisplayBookList(connectionString);
                            PauseScreen();
                            break;
                        case "👤 Thông tin tài khoản":
                            AuthService.ShowUserInfo(currentUsername!, connectionString);
                            PauseScreen();
                            break;
                        case "🔓 Đăng xuất":
                            isLoggedIn = false;
                            currentUsername = null;
                            AnsiConsole.MarkupLine("[green]✅ Đăng xuất thành công.[/]");
                            PauseScreen();
                            break;
                        case "❌ Thoát chương trình":
                            ExitProgram();
                            return;
                    }
                }
            }
        }
    }

    static void DisplayBanner()
    {
        var rule = new Rule("[bold magenta]📚 WELCOME TO BOOK STORE APP 📚[/]")
        {
            Justification = Justify.Center,
            Style = Style.Parse("bold magenta")
        };
        AnsiConsole.Write(rule);
    }

    static void PauseScreen()
    {
        AnsiConsole.MarkupLine("[grey]\nNhấn phím bất kỳ để quay lại...[/]");
        Console.ReadKey();
    }

    static void ExitProgram()
    {
        AnsiConsole.MarkupLine("\n[yellow]👋 Tạm biệt! Cảm ơn bạn đã sử dụng ứng dụng BookStore![/]");
        Thread.Sleep(1500);
    }
}
