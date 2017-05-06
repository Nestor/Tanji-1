using System.Windows.Forms;

namespace Tanji.Controls
{
    // Why not just use a RichTextBox control for the child in the WindowsFormHost.Child property?
    // Well, for some reason, the whole window lags when re-sizing if it's not hosted in a WinForm container(UserControl|Form).
    // I have no idea why, but this works fine.
    public partial class HPacketLogger : UserControl
    {
        public HPacketLogger()
        {
            InitializeComponent();
        }
    }
}