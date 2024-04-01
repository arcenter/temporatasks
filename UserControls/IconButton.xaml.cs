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

        private async void IconButton_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Button.BeginAnimation(OpacityProperty, new DoubleAnimation(Button.IsMouseOver? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
            Icon.BeginAnimation(OpacityProperty, new DoubleAnimation(Button.IsMouseOver? 0.75 : 0.25, TimeSpan.FromMilliseconds(250)));

            if (Button.IsMouseOver)
            {
                Icon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

                {
                    DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                    Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }

                await Task.Delay(251);

                {
                    DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                    Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                    Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
                }
            }
        }

        public async void RunAnimation()
        {
            Icon.RenderTransform = new ScaleTransform() { ScaleX = 1, ScaleY = 1 };

            {
                DoubleAnimation ani = new(0.75, TimeSpan.FromMilliseconds(250));
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            }

            await Task.Delay(251);

            {
                DoubleAnimation ani = new(1, TimeSpan.FromMilliseconds(250));
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, ani);
                Icon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, ani);
            }
        }
    }
}
