using App.WinForms.Views;

namespace App.WinForms;

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
