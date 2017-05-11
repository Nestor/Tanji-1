using Tangine.Modules;

namespace Tanji.ExtensionExample
{
    [Module("Extension Example", "Module example showing how to create a valid extension(UI) for Tanji.")]
    public partial class MainFrm : ExtensionForm
    {
        public MainFrm()
        {
            InitializeComponent();
        }
    }
}