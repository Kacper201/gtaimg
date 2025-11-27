/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GtaImgTool.Dialogs;
using GtaImgTool.ViewModels;

namespace GtaImgTool
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.SetOwnerWindow(this);
        }

        private void FileGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.UpdateSelection(FileGrid.SelectedItems.Cast<object>().ToList());
            }
        }

        private void FileGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Double-click to export single file
            if (FileGrid.SelectedItem is FileEntryViewModel entry)
            {
                ViewModel.ExportSelectedCommand.Execute(null);
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            FileGrid.SelectAll();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var ext = System.IO.Path.GetExtension(files[0]).ToLowerInvariant();
                    e.Effects = ext == ".img" ? DragDropEffects.Copy : DragDropEffects.None;
                    e.Handled = true;
                    return;
                }
            }
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var file = files[0];
                    var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    if (ext == ".img")
                    {
                        ViewModel.OpenArchiveFile(file);
                    }
                }
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.Show(this);
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchBorder.BorderBrush = (SolidColorBrush)FindResource("AccentBrush");
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")!);
        }

        #region Custom Title Bar

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Double-click to maximize/restore
                MaximizeButton_Click(sender, e);
            }
            else
            {
                // Single click to drag
                if (WindowState == WindowState.Maximized)
                {
                    // Restore before dragging from maximized state
                    var mousePos = e.GetPosition(this);
                    var screenPos = PointToScreen(mousePos);

                    WindowState = WindowState.Normal;

                    // Position window so mouse is at same relative position
                    Left = screenPos.X - (ActualWidth / 2);
                    Top = screenPos.Y - 16;
                }
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExitCommand.Execute(null);
        }

        #endregion
    }
}

