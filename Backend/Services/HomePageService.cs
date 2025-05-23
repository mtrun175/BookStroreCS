using Spectre.Console;

namespace BookStoreConsoleApp.Services
{
    public static class HomePageService
    {
        public static void DisplayHomePage(bool isLoggedIn, string? currentUsername)
        {
            var title = new FigletText("Book Store")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(title);

            var welcomeText = isLoggedIn
                ? $"[bold green]Xin chào, [underline]{currentUsername}[/]![/]"
                : "[italic yellow]Chào mừng bạn đến với ứng dụng quản lý Book Store![/]";

            var panel = new Panel(welcomeText)
                .Border(BoxBorder.Rounded)
                .Padding(1, 1)
                .Expand();

            AnsiConsole.Write(panel);
        }
    }

}
