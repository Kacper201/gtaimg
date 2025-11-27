/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System.Windows;
using System.Windows.Input;

namespace GtaImgTool.Dialogs
{
    public partial class InputDialog : Window
    {
        public string? Result { get; private set; }

        public InputDialog()
        {
            InitializeComponent();
        }

        public string? ShowDialog(string title, string prompt, string defaultValue = "")
        {
            TitleText.Text = title;
            Title = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.SelectAll();
            InputTextBox.Focus();

            base.ShowDialog();
            return Result;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Result = InputTextBox.Text;
            Close();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Result = InputTextBox.Text;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                Result = null;
                Close();
            }
        }
    }
}

