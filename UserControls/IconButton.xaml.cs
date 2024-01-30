using System;
using System.Collections.Generic;
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
    public partial class IconButton : UserControl
    {

        public static readonly DependencyProperty SVGSourceProperty = DependencyProperty.Register("SVGSource", typeof(DrawingImage), typeof(IconButton), new PropertyMetadata(null));

        public DrawingImage SVGSource
        {
            get { return (DrawingImage)GetValue(SVGSourceProperty); }
            set { SetValue(SVGSourceProperty, value); }
        }

        public IconButton()
        {
            InitializeComponent();
        }

        private void IconButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            double newValueButton = 0;
            double newValueIcon = 0.25;
            if (Button.IsMouseOver)
            {
                newValueButton = 0.5;
                newValueIcon = 0.75;
            }
            Button.BeginAnimation(OpacityProperty, new DoubleAnimation(newValueButton, TimeSpan.FromMilliseconds(250)));
            Icon.BeginAnimation(OpacityProperty, new DoubleAnimation(newValueIcon, TimeSpan.FromMilliseconds(250)));
        }
    }
}
