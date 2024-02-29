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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TemporaTasks.UserControls
{
    /// <summary>
    /// Interaction logic for Tags.xaml
    /// </summary>
    public partial class Tags : UserControl
    {
        public Tags(string tagText)
        {
            InitializeComponent();
            TagText = tagText;
        }

        public static readonly DependencyProperty TagTextProperty = DependencyProperty.Register("TagText", typeof(string), typeof(Tags), new PropertyMetadata(""));
        public string TagText
        {
            get { return (string)GetValue(TagTextProperty); }
            set { SetValue(TagTextProperty, value); }
        }
    }
}