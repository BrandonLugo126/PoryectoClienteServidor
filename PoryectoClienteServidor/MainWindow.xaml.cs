using PoryectoClienteServidor.Services;
using PoryectoClienteServidor.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PoryectoClienteServidor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly EstacionamientoService service;
        private readonly TableroViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            service = new EstacionamientoService();
            vm = new TableroViewModel(service);
            this.DataContext = vm;
            
        }
    }
}