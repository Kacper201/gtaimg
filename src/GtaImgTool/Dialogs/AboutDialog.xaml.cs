/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace GtaImgTool.Dialogs
{
    public partial class AboutDialog : Window
    {
        private const string AuthorWebsite = "https://vaibhavpandey.com/";
        private const string AuthorYouTube = "https://www.youtube.com/channel/UC5uV1PRvtnNj9P8VfqO93Pw";
        private const string AuthorEmail = "contact@vaibhavpandey.com";
        private const string GitHubRepo = "https://github.com/vaibhavpandeyvpz/gtaimg";
        private const string GitHubIssues = "https://github.com/vaibhavpandeyvpz/gtaimg/issues";

        public AboutDialog()
        {
            InitializeComponent();
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

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(AuthorWebsite);
        }

        private void YouTube_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(AuthorYouTube);
        }

        private void Email_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl($"mailto:{AuthorEmail}");
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(GitHubRepo);
        }

        private void Support_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl(GitHubIssues);
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore if browser can't be opened
            }
        }

        public static void Show(Window owner)
        {
            var dialog = new AboutDialog
            {
                Owner = owner
            };
            dialog.ShowDialog();
        }
    }
}

