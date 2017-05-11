using System.Windows.Forms;

using Tangine.Modules;
using Tangine.Network;

namespace Tanji.ExtensionExample
{
    [Module("Extension Example", "Module example showing how to create a valid extension(UI) for Tanji.")]
    public partial class MainFrm : ExtensionForm
    {
        public MainFrm()
        {
            InitializeComponent();
        }

        [OutDataCapture("f76a21e6be54cea897c44fbfc7c32839")]
        public void UserWalk(DataInterceptedEventArgs e)
        {
            int x = e.Packet.ReadInt32();
            int y = e.Packet.ReadInt32();
            MessageBox.Show($"X:{x}, Y:{y}");
        }
    }
}