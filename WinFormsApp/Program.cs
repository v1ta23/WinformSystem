using WinFormsApp.Views;

namespace WinFormsApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var compositionRoot = new AppCompositionRoot();
        Application.Run(compositionRoot.CreateLoginForm());
    }
}
