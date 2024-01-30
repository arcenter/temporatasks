using Microsoft.VisualBasic;
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
    public partial class DateTimePicker : UserControl
    {
        private FontFamily font = new(new Uri("pack://TemporaTasks:,,,/Resources/Fonts/Manrope.ttf"), "Manrope Regular");

        private int currentYear = DateTime.Now.Year;
        private int currentMonth = DateTime.Now.Month;

        public DateTimePicker()
        {
            InitializeComponent();

            for (int i = 0; i < 11; i++)
            {
                yearComboBox.Items.Add((currentYear + i).ToString());
            }

            yearComboBox.SelectedIndex = 0;
            monthComboBox.SelectedIndex = currentMonth-1;
            GenerateCalendar();
        }

        private void GenerateCalendar(int year = -1, int month = -1)
        {
            if (year == -1) year = int.Parse(yearComboBox.Text);
            if (month == -1) month = monthComboBox.SelectedIndex + 1;

            calendarGrid.Children.RemoveRange(7, calendarGrid.Children.Count-7);

            if (calendarGrid.RowDefinitions.Count >= 7) calendarGrid.RowDefinitions.RemoveRange(6, calendarGrid.RowDefinitions.Count-6);

            int rowNumber = 1;
            int colNumber = (int)(new DateTime(year, month, 01).DayOfWeek);

            int maxDays = DateTime.DaysInMonth(year, month);

            for (int day = 1; ;)
            {
                Border border = new()
                {
                    Background = (SolidColorBrush)((MainWindow)Application.Current.MainWindow).FindResource("DarkBlue"),
                    Opacity = 0
                };

                border.IsMouseDirectlyOverChanged += Border_MouseOver;

                Label label = new()
                {
                    Content = day.ToString(),
                    Foreground = (SolidColorBrush)((MainWindow)Application.Current.MainWindow).FindResource("Text"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = font,
                    IsHitTestVisible = false
                };

                Grid.SetRow(border, rowNumber);
                Grid.SetColumn(border, colNumber);
                calendarGrid.Children.Add(border);

                Grid.SetRow(label, rowNumber);
                Grid.SetColumn(label, colNumber);
                calendarGrid.Children.Add(label);

                if (++day > maxDays) break;

                if (++colNumber > 6)
                {
                    rowNumber++;
                    colNumber = 0;
                }

                if (rowNumber == 6)
                {
                    calendarGrid.RowDefinitions.Add(new RowDefinition());
                }
            }
        }

        private void Border_MouseOver(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : 0, TimeSpan.FromMilliseconds(250)));
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                GenerateCalendar();
            }
            catch { }
        }

        private void BackIconButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (monthComboBox.SelectedIndex == 0)
            {
                monthComboBox.SelectedIndex = 11;
                yearComboBox.Text = (int.Parse(yearComboBox.Text) - 1).ToString();
                GenerateCalendar();
            }
            else
            {
                monthComboBox.SelectedIndex -= 1;
                GenerateCalendar();
            }
        }

        private void NextIconButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (monthComboBox.SelectedIndex == 11)
            {
                monthComboBox.SelectedIndex = 0;
                yearComboBox.Text = (int.Parse(yearComboBox.Text) + 1).ToString();
                GenerateCalendar();
            }
            else
            {
                monthComboBox.SelectedIndex += 1;
                GenerateCalendar();
            }
        }
    }
}
