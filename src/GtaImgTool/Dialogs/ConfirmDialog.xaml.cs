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
    public enum DialogIcon
    {
        Question,
        Warning,
        Error,
        Info
    }

    public partial class ConfirmDialog : Window
    {
        public bool? ThreeWayResult { get; private set; }

        public ConfirmDialog(string title, string message, string? subMessage = null, 
            DialogIcon icon = DialogIcon.Question, string confirmText = "Yes", string cancelText = "Cancel",
            string? thirdText = null)
        {
            InitializeComponent();
            
            TitleText.Text = title;
            Title = title;
            MessageText.Text = message;
            
            if (!string.IsNullOrEmpty(subMessage))
            {
                SubMessageText.Text = subMessage;
                SubMessageText.Visibility = Visibility.Visible;
            }
            else
            {
                SubMessageText.Visibility = Visibility.Collapsed;
            }

            ConfirmButton.Content = confirmText;
            CancelButton.Content = cancelText;

            if (thirdText != null)
            {
                ThirdButton.Content = thirdText;
                ThirdButton.Visibility = Visibility.Visible;
            }

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
                case DialogIcon.Info:
                    IconAwesome.Icon = FontAwesomeIcon.InfoCircle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0091cd")!);
                    break;
                case DialogIcon.Question:
                default:
                    IconAwesome.Icon = FontAwesomeIcon.QuestionCircle;
                    IconAwesome.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff6a00")!);
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
            ThreeWayResult = null;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ThreeWayResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ThreeWayResult = true;
            Close();
        }

        private void ThirdButton_Click(object sender, RoutedEventArgs e)
        {
            ThreeWayResult = null;
            Close();
        }

        public static bool Show(Window owner, string title, string message, string? subMessage = null,
            DialogIcon icon = DialogIcon.Question, string confirmText = "Yes", string cancelText = "Cancel")
        {
            var dialog = new ConfirmDialog(title, message, subMessage, icon, confirmText, cancelText)
            {
                Owner = owner
            };
            dialog.ShowDialog();
            return dialog.ThreeWayResult == true;
        }

        /// <summary>
        /// Shows a dialog with three options.
        /// </summary>
        /// <returns>true for confirm, false for second option, null for third option/cancel</returns>
        public static bool? ShowThreeWay(Window owner, string title, string message, string? subMessage,
            DialogIcon icon, string confirmText, string secondText, string thirdText)
        {
            var dialog = new ConfirmDialog(title, message, subMessage, icon, confirmText, secondText, thirdText)
            {
                Owner = owner
            };
            dialog.ShowDialog();
            return dialog.ThreeWayResult;
        }
    }
}
