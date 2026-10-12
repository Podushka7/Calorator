using Calorator.Interface.Contracts;
using Calorator.Interface.Services;
using Calorator.Interface.UI;

namespace Calorator.Interface;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        IBusinessLogic businessLogic = new InMemoryBusinessLogic();
        var controller = new InterfaceController(businessLogic);
        Application.Run(new MainWindow(controller));
    }
}
