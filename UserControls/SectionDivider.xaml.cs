using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TemporaTasks.UserControls
{
    public partial class SectionDivider : UserControl
    {
        public SectionDivider(string sectionTitle)
        {
            InitializeComponent();
            SectionTitle.Content = sectionTitle;
            Background.Tag = sectionTitle;
        }

        private void Background_MouseEnter(object sender, MouseEventArgs e)
        {
            Background.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, TimeSpan.FromMilliseconds(250)));
        }

        private void Background_MouseLeave(object sender, MouseEventArgs e)
        {
            Background.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
        }

        public void Background_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var scaleTransform = Arrow.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                double scale = scaleTransform.ScaleY;
                DoubleAnimation animation = new DoubleAnimation(scale * (-1), TimeSpan.FromMilliseconds(500));
                Storyboard.SetTarget(animation, Arrow);
                Storyboard.SetTargetProperty(animation, new PropertyPath("RenderTransform.ScaleY"));
                Storyboard storyboard = new();
                storyboard.Children.Add(animation);
                storyboard.Begin();
            }
        }
    }
}
