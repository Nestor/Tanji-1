using System.Windows;
using System.Windows.Controls;

namespace Tanji.Controls
{
    public class LabelBox : TextBox
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title),
            typeof(string), typeof(LabelBox), new UIPropertyMetadata("Title"));

        public string Title
        {
            get { return GetValue(TitleProperty) as string; }
            set { SetValue(TitleProperty, value); }
        }
    }
}