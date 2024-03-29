﻿using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private int currentDay = DateTime.Now.Day;

        public ArrayList selectedDateTime = [0, 0, 0];
        public TextBox textBox;
        public Popup popUp;

        public DateTimePicker(string dateText)
        {
            InitializeComponent();

            for (int i = 0; i < 6; i++) yearComboBox.Items.Add((currentYear + i).ToString());

            selectedDateTime[2] = currentDay;

            if (new Regex("^\\d{1,4}-\\d{1,2}-\\d{1,2}$").Match(dateText).Success)
            {
                string[] splits = dateText.Split("-");
                yearComboBox.Text = splits[0];
                monthComboBox.SelectedIndex = int.Parse(splits[1]) - 1;
                GenerateCalendar(-1, -1, int.Parse(splits[2]));
            }
            else
            {
                yearComboBox.SelectedIndex = 0;
                monthComboBox.SelectedIndex = currentMonth - 1; 
                GenerateCalendar();
            }
        }

        private void GenerateCalendar(int year = -1, int month = -1, int day = -1)
        {
            if (year == -1) year = (int)selectedDateTime[0];
            if (month == -1) month = (int)selectedDateTime[1];
            if (day == -1) day = (int)selectedDateTime[2];

            calendarGrid.Children.RemoveRange(7, calendarGrid.Children.Count-7);

            if (calendarGrid.RowDefinitions.Count >= 7) calendarGrid.RowDefinitions.RemoveRange(6, calendarGrid.RowDefinitions.Count-6);

            int maxDays = DateTime.DaysInMonth(year, month);

            int colNumber = (int)new DateTime(year, month, 01).DayOfWeek;
            int rowNumber = 1;
            
            bool onCurrentDay;
            for (int _day = 1; ;)
            {
                onCurrentDay = false;

                if (monthComboBox.SelectedIndex == currentMonth - 1 && yearComboBox.Text == currentYear.ToString())
                {
                    if (_day < currentDay) goto skipCreation;
                    else if (_day == currentDay) onCurrentDay = true;
                }

                Border border = new()
                {
                    Background = (SolidColorBrush)((MainWindow)Application.Current.MainWindow).FindResource("DarkBlue"),
                    Tag = new ArrayList() { _day, onCurrentDay ? 0.25 : 0 },
                    CornerRadius = new CornerRadius(5),
                    Cursor = Cursors.Hand
                };

                border.Opacity = (double)(((ArrayList)border.Tag)[1]);
                border.IsMouseDirectlyOverChanged += Border_MouseOver;
                border.MouseDown += Border_MouseDown;

                Label label = new()
                {
                    Content = _day.ToString(),
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

                skipCreation:

                if (++_day > maxDays) break;

                if (++colNumber == 7)
                {
                    rowNumber++;
                    colNumber = 0;
                }

                if (colNumber == 0 && rowNumber == 6)
                {
                    calendarGrid.RowDefinitions.Add(new RowDefinition());
                }
            }
        }

        private void Border_MouseOver(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((Border)sender).BeginAnimation(OpacityProperty, new DoubleAnimation(((bool)e.NewValue) ? 0.5 : (double)(((ArrayList)((Border)sender).Tag)[1]), TimeSpan.FromMilliseconds(250)));
        }

        private void Border_MouseDown(object sender, MouseEventArgs e)
        {
            selectedDateTime[2] = ((ArrayList)((Border)sender).Tag)[0];
            textBox.Text = $"{selectedDateTime[0]}-{selectedDateTime[1].ToString().PadLeft(2, '0')}-{selectedDateTime[2].ToString().PadLeft(2, '0')}";
            popUp.IsOpen = false;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                selectedDateTime[0] = int.Parse(yearComboBox.SelectedItem.ToString());
                selectedDateTime[1] = monthComboBox.SelectedIndex + 1;
                GenerateCalendar();
            }
            catch { }
        }

        private void BackIconButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (yearComboBox.Text == currentYear.ToString() && monthComboBox.SelectedIndex + 1 == currentMonth) return;
            if (monthComboBox.SelectedIndex == 0)
            {
                monthComboBox.SelectedIndex = 11;
                yearComboBox.Text = (int.Parse(yearComboBox.Text) - 1).ToString();
            }
            else monthComboBox.SelectedIndex -= 1;
            GenerateCalendar();
        }

        private void NextIconButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string nextYear = (int.Parse(yearComboBox.Text) + 1).ToString();
            if (monthComboBox.SelectedIndex == 11)
            {
                if (!yearComboBox.Items.Contains(nextYear)) return;
                monthComboBox.SelectedIndex = 0;
                yearComboBox.Text = nextYear;
            }
            else monthComboBox.SelectedIndex += 1;
            GenerateCalendar();
        }
    }
}
