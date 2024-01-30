using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TemporaTasks.UserControls
{
    public partial class ButtonToolTip : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("ToolTipText", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string ToolTipText
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty Button1Property = DependencyProperty.Register("Button1Text", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string Button1Text
        {
            get { return (string)GetValue(Button1Property); }
            set { SetValue(Button1Property, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(string), typeof(ButtonToolTip), new PropertyMetadata("4"));
        public string CornerRadius
        {
            get { return (string)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty BorderPaddingProperty = DependencyProperty.Register("BorderPadding", typeof(string), typeof(ButtonToolTip), new PropertyMetadata("5"));
        public string BorderPadding
        {
            get { return (string)GetValue(BorderPaddingProperty); }
            set { SetValue(BorderPaddingProperty, value); }
        }

        public ButtonToolTip()
        {
            InitializeComponent();
        }
    }
}