using Tangine.Modules;

namespace Tanji.ExtensionWindowExample
{
    [Module("Window Extension Example", "Module example showing how to create a valid Window extension for Tanji.")]
    public partial class MainWindow : ExtensionWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
