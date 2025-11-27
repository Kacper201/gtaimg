/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome.WPF;

namespace GtaImgTool.Dialogs
{
    public partial class MessageDialog : Window
    {
        public MessageDialog(string title, string message, DialogIcon icon = DialogIcon.Info)
        {
            InitializeComponent();

            TitleText.Text = title;
            Title = title;
            MessageText.Text = message;

            // Set icon based on type
            switch (icon)
            {
                case DialogIcon.Warning:
                    IconAwesome.Icon = FontAwesomeIcon.ExclamationTriangle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ecb731")!);
                    break;
                case DialogIcon.Error:
                    IconAwesome.Icon = FontAwesomeIcon.TimesCircle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ed1b2e")!);
                    break;
                case DialogIcon.Question:
                    IconAwesome.Icon = FontAwesomeIcon.QuestionCircle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff6a00")!);
                    break;
                case DialogIcon.Info:
                default:
                    IconAwesome.Icon = FontAwesomeIcon.InfoCircle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0091cd")!);
                    break;
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static void Show(Window owner, string title, string message, DialogIcon icon = DialogIcon.Info)
        {
            var dialog = new MessageDialog(title, message, icon)
            {
                Owner = owner
            };
            dialog.ShowDialog();
        }
    }
}
