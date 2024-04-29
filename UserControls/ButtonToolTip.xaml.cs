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
        public static readonly DependencyProperty T1Property = DependencyProperty.Register("T1", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string T1
        {
            get { return (string)GetValue(T1Property); }
            set { SetValue(T1Property, value); }
        }

        public static readonly DependencyProperty B1Property = DependencyProperty.Register("B1", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string B1
        {
            get { return (string)GetValue(B1Property); }
            set { SetValue(B1Property, value); }
        }

        public static readonly DependencyProperty T2Property = DependencyProperty.Register("T2", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string T2
        {
            get { return (string)GetValue(T2Property); }
            set { SetValue(T2Property, value); }
        }

        public static readonly DependencyProperty B2Property = DependencyProperty.Register("B2", typeof(string), typeof(ButtonToolTip), new PropertyMetadata(""));
        public string B2
        {
            get { return (string)GetValue(B2Property); }
            set { SetValue(B2Property, value); }
        }

        public ButtonToolTip()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (T2 == "") T2Label.Visibility = Visibility.Collapsed;
            if (B2 == "") Button2Background.Visibility = Visibility.Collapsed;
        }
    }
}