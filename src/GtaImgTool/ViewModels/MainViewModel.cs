/*
    GtaImgTool - A GUI tool for viewing and editing GTA IMG archive files.
    Copyright (c) 2025 Vaibhav Pandey <contact@vaibhavpandey.com>

    Licensed under the MIT License. See LICENSE file in the project root for full license information.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GtaImg;
using GtaImgTool.Dialogs;
using Microsoft.Win32;

namespace GtaImgTool.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IMGArchive? _archive;
        private string _currentFilePath = string.Empty;
        private string _statusMessage = "Ready";
        private string _windowTitle = "GTA IMG Tool by VPZ";
        private bool _isLoading;
        private bool _hasUnsavedChanges;
        private string _searchFilter = string.Empty;
        
        private Window? _ownerWindow;
        
        public void SetOwnerWindow(Window window)
        {
            _ownerWindow = window;
        }
        
        private Window OwnerWindow => _ownerWindow ?? Application.Current.MainWindow;

        public MainViewModel()
        {
            Entries = new ObservableCollection<FileEntryViewModel>();
            SelectedEntries = new ObservableCollection<FileEntryViewModel>();

            // Commands
            NewArchiveCommand = new RelayCommand(NewArchive);
            OpenArchiveCommand = new RelayCommand(OpenArchive);
            SaveArchiveCommand = new RelayCommand(SaveArchive, _ => HasUnsavedChanges);
            CloseArchiveCommand = new RelayCommand(CloseArchive, _ => IsArchiveOpen);
            
            ImportFilesCommand = new RelayCommand(ImportFiles, _ => IsArchiveOpen);
            ImportFolderCommand = new RelayCommand(ImportFolder, _ => IsArchiveOpen);
            
            ExportSelectedCommand = new RelayCommand(ExportSelected, _ => HasSelection);
            ExportAllCommand = new RelayCommand(ExportAll, _ => IsArchiveOpen && Entries.Count > 0);
            
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, _ => HasSelection);
            ReplaceEntryCommand = new RelayCommand(ReplaceEntry, _ => SelectedEntries.Count == 1);
            RenameEntryCommand = new RelayCommand(RenameEntry, _ => SelectedEntries.Count == 1);
            
            SelectAllCommand = new RelayCommand(SelectAll, _ => IsArchiveOpen && Entries.Count > 0);
            RefreshCommand = new RelayCommand(Refresh, _ => IsArchiveOpen);
            
            PackArchiveCommand = new RelayCommand(PackArchive, _ => IsArchiveOpen);
            ExitCommand = new RelayCommand(Exit);
        }

        #region Properties

        public ObservableCollection<FileEntryViewModel> Entries { get; }
        public ObservableCollection<FileEntryViewModel> SelectedEntries { get; set; }

        public IEnumerable<FileEntryViewModel> FilteredEntries
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SearchFilter))
                    return Entries;
                
                return Entries.Where(e => 
                    e.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
            }
        }

        public string CurrentFilePath
        {
            get => _currentFilePath;
            set
            {
                if (SetProperty(ref _currentFilePath, value))
                {
                    UpdateWindowTitle();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    OnPropertyChanged(nameof(FilteredEntries));
                }
            }
        }

        public bool IsArchiveOpen => _archive != null;
        public bool HasSelection => SelectedEntries.Count > 0;

        public string ArchiveInfo
        {
            get
            {
                if (_archive == null)
                    return string.Empty;

                var version = _archive.Version == IMGArchive.IMGVersion.VER2 ? "VER2 (GTA SA)" : "VER1 (GTA III/VC)";
                return $"{Entries.Count} files | {version}";
            }
        }

        #endregion

        #region Commands

        public ICommand NewArchiveCommand { get; }
        public ICommand OpenArchiveCommand { get; }
        public ICommand SaveArchiveCommand { get; }
        public ICommand CloseArchiveCommand { get; }
        public ICommand ImportFilesCommand { get; }
        public ICommand ImportFolderCommand { get; }
        public ICommand ExportSelectedCommand { get; }
        public ICommand ExportAllCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand ReplaceEntryCommand { get; }
        public ICommand RenameEntryCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand PackArchiveCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region Command Implementations

        private void NewArchive(object? parameter)
        {
            if (!PromptSaveIfNeeded())
                return;

            var dialog = new SaveFileDialog
            {
                Title = "Create New IMG Archive",
                Filter = "IMG Archive (*.img)|*.img",
                DefaultExt = ".img"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                // Ask for version - default to VER2 (GTA SA) as it's more common
                var useVer2 = ConfirmDialog.Show(
                    OwnerWindow,
                    "Select Archive Version",
                    "Create a VER2 archive (GTA San Andreas)?",
                    "VER2 = single .img file (GTA SA)\nVER1 = .img + .dir files (GTA III/VC)",
                    DialogIcon.Question,
                    "VER2",
                    "VER1");

                var version = useVer2 
                    ? IMGArchive.IMGVersion.VER2 
                    : IMGArchive.IMGVersion.VER1;

                CloseCurrentArchive();

                _archive = IMGArchive.CreateArchive(dialog.FileName, version);
                CurrentFilePath = dialog.FileName;
                
                RefreshEntries();
                StatusMessage = $"Created new {(version == IMGArchive.IMGVersion.VER2 ? "VER2" : "VER1")} archive";
            }
            catch (Exception ex)
            {
                ShowError("Failed to create archive", ex);
            }
        }

        private void OpenArchive(object? parameter)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open IMG Archive",
                Filter = "IMG Archive (*.img)|*.img|All Files (*.*)|*.*",
                DefaultExt = ".img"
            };

            if (dialog.ShowDialog() != true)
                return;

            OpenArchiveFile(dialog.FileName);
        }

        public void OpenArchiveFile(string filePath)
        {
            if (!PromptSaveIfNeeded())
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Opening archive...";

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    ShowError("File not found", new FileNotFoundException("The specified file does not exist.", filePath));
                    return;
                }

                // Try to detect version first
                IMGArchive.IMGVersion version;
                try
                {
                    version = IMGArchive.GuessIMGVersion(filePath);
                }
                catch (Exception ex)
                {
                    ShowError("Invalid or corrupt IMG file", ex);
                    return;
                }

                CloseCurrentArchive();

                _archive = new IMGArchive(filePath, IMGArchive.IMGMode.ReadWrite);
                CurrentFilePath = filePath;

                RefreshEntries();
                StatusMessage = $"Opened archive with {Entries.Count} files";
            }
            catch (IMGException ex)
            {
                ShowError("Failed to open IMG archive. The file may be corrupt or in an unsupported format.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowError("Access denied. The file may be in use or you don't have permission to open it.", ex);
            }
            catch (Exception ex)
            {
                ShowError("Failed to open archive", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SaveArchive(object? parameter)
        {
            if (_archive == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Saving archive...";

                _archive.Sync();
                HasUnsavedChanges = false;
                StatusMessage = "Archive saved successfully";
            }
            catch (Exception ex)
            {
                ShowError("Failed to save archive", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseArchive(object? parameter)
        {
            if (!PromptSaveIfNeeded())
                return;

            CloseCurrentArchive();
            StatusMessage = "Ready";
        }

        private void ImportFiles(object? parameter)
        {
            if (_archive == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "Import Files",
                Filter = "All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            ImportFilesInternal(dialog.FileNames);
        }

        private void ImportFolder(object? parameter)
        {
            if (_archive == null) return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to import all files from",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            var files = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                MessageDialog.Show(OwnerWindow, "Import", "No files found in the selected folder.", DialogIcon.Info);
                return;
            }

            var shouldImport = ConfirmDialog.Show(
                OwnerWindow,
                "Confirm Import",
                $"Import {files.Length} files from this folder?",
                null,
                DialogIcon.Question,
                "Import",
                "Cancel");

            if (shouldImport)
                ImportFilesInternal(files);
        }

        private void ImportFilesInternal(string[] filePaths)
        {
            if (_archive == null) return;

            try
            {
                IsLoading = true;
                int imported = 0;
                int skipped = 0;
                var errors = new List<string>();

                foreach (var filePath in filePaths)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        
                        // Check name length
                        if (fileName.Length > IMGEntry.MaxNameLength)
                        {
                            errors.Add($"{fileName}: Name too long (max {IMGEntry.MaxNameLength} chars)");
                            skipped++;
                            continue;
                        }

                        // Check if entry already exists
                        if (_archive.ContainsEntry(fileName))
                        {
                            var shouldReplace = ConfirmDialog.Show(
                                OwnerWindow,
                                "File Exists",
                                $"'{fileName}' already exists. Replace it?",
                                null,
                                DialogIcon.Question,
                                "Replace",
                                "Skip");

                            if (!shouldReplace)
                            {
                                skipped++;
                                continue;
                            }

                            _archive.RemoveEntry(fileName);
                        }

                        StatusMessage = $"Importing {fileName}...";
                        _archive.ImportFile(filePath, fileName);
                        imported++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                        skipped++;
                    }
                }

                RefreshEntries();
                HasUnsavedChanges = imported > 0;

                var message = $"Imported {imported} file(s)";
                if (skipped > 0)
                    message += $", {skipped} skipped";

                StatusMessage = message;

                if (errors.Count > 0)
                {
                    MessageDialog.Show(
                        OwnerWindow,
                        "Import Complete",
                        $"{message}\n\nErrors:\n{string.Join("\n", errors.Take(10))}" + 
                        (errors.Count > 10 ? $"\n...and {errors.Count - 10} more" : ""),
                        DialogIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                ShowError("Import failed", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExportSelected(object? parameter)
        {
            if (_archive == null || SelectedEntries.Count == 0) return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select destination folder for exported files"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            ExportEntries(SelectedEntries.ToList(), dialog.SelectedPath);
        }

        private void ExportAll(object? parameter)
        {
            if (_archive == null || Entries.Count == 0) return;

            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select destination folder for all exported files"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            ExportEntries(Entries.ToList(), dialog.SelectedPath);
        }

        private void ExportEntries(List<FileEntryViewModel> entries, string destinationFolder)
        {
            if (_archive == null) return;

            try
            {
                IsLoading = true;
                int exported = 0;
                var errors = new List<string>();

                foreach (var entry in entries)
                {
                    try
                    {
                        var destPath = Path.Combine(destinationFolder, entry.Name);
                        StatusMessage = $"Exporting {entry.Name}...";
                        _archive.ExtractEntry(entry.Name, destPath);
                        exported++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{entry.Name}: {ex.Message}");
                    }
                }

                StatusMessage = $"Exported {exported} file(s)";

                if (errors.Count > 0)
                {
                    MessageDialog.Show(
                        OwnerWindow,
                        "Export Complete",
                        $"Exported {exported} file(s)\n\nErrors:\n{string.Join("\n", errors.Take(10))}" +
                        (errors.Count > 10 ? $"\n...and {errors.Count - 10} more" : ""),
                        DialogIcon.Warning);
                }
                else
                {
                    MessageDialog.Show(
                        OwnerWindow,
                        "Export Complete",
                        $"Successfully exported {exported} file(s) to:\n{destinationFolder}",
                        DialogIcon.Info);
                }
            }
            catch (Exception ex)
            {
                ShowError("Export failed", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void DeleteSelected(object? parameter)
        {
            if (_archive == null || SelectedEntries.Count == 0) return;

            var count = SelectedEntries.Count;
            var shouldDelete = ConfirmDialog.Show(
                OwnerWindow,
                "Confirm Delete",
                $"Are you sure you want to delete {count} selected file(s)?",
                "This action cannot be undone.",
                DialogIcon.Warning,
                "Delete",
                "Cancel");

            if (!shouldDelete)
                return;

            try
            {
                IsLoading = true;
                var toDelete = SelectedEntries.ToList();
                int deleted = 0;

                foreach (var entry in toDelete)
                {
                    try
                    {
                        StatusMessage = $"Deleting {entry.Name}...";
                        _archive.RemoveEntry(entry.Name);
                        deleted++;
                    }
                    catch (Exception ex)
                    {
                        ShowError($"Failed to delete {entry.Name}", ex);
                    }
                }

                RefreshEntries();
                HasUnsavedChanges = deleted > 0;
                StatusMessage = $"Deleted {deleted} file(s)";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ReplaceEntry(object? parameter)
        {
            if (_archive == null || SelectedEntries.Count != 1) return;

            var entry = SelectedEntries[0];
            
            var dialog = new OpenFileDialog
            {
                Title = $"Select replacement file for '{entry.Name}'",
                Filter = "All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Replacing {entry.Name}...";

                // Remove old entry and import new one with the same name
                _archive.RemoveEntry(entry.Name);
                _archive.ImportFile(dialog.FileName, entry.Name);

                RefreshEntries();
                HasUnsavedChanges = true;
                StatusMessage = $"Replaced {entry.Name}";

                MessageDialog.Show(
                    OwnerWindow,
                    "Replace Complete",
                    $"Successfully replaced '{entry.Name}'",
                    DialogIcon.Info);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to replace {entry.Name}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RenameEntry(object? parameter)
        {
            if (_archive == null || SelectedEntries.Count != 1) return;

            var entry = SelectedEntries[0];
            
            var renameDialog = new InputDialog();
            renameDialog.Owner = OwnerWindow;
            var newName = renameDialog.ShowDialog("Rename Entry", "Enter new name:", entry.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name)
                return;

            // Validate name length (max 24 chars for IMG format)
            if (newName.Length > 24)
            {
                MessageDialog.Show(
                    OwnerWindow,
                    "Invalid Name",
                    "Entry name cannot exceed 24 characters.",
                    DialogIcon.Error);
                return;
            }

            // Check if name already exists
            if (_archive.ContainsEntry(newName))
            {
                MessageDialog.Show(
                    OwnerWindow,
                    "Name Exists",
                    $"An entry named '{newName}' already exists in the archive.",
                    DialogIcon.Error);
                return;
            }

            try
            {
                IsLoading = true;
                StatusMessage = $"Renaming {entry.Name} to {newName}...";

                _archive.RenameEntry(entry.Name, newName);

                RefreshEntries();
                HasUnsavedChanges = true;
                StatusMessage = $"Renamed to {newName}";
            }
            catch (Exception ex)
            {
                ShowError($"Failed to rename {entry.Name}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SelectAll(object? parameter)
        {
            // This is handled by the DataGrid in the view
        }

        private void Refresh(object? parameter)
        {
            if (HasUnsavedChanges)
            {
                var shouldContinue = ConfirmDialog.Show(
                    OwnerWindow,
                    "Unsaved Changes",
                    "Refreshing will reload the archive from disk and discard any unsaved changes.",
                    "Do you want to continue?",
                    DialogIcon.Warning,
                    "Refresh",
                    "Cancel");

                if (!shouldContinue)
                    return;
            }

            RefreshEntries();
            HasUnsavedChanges = false;
            StatusMessage = "Refreshed";
        }

        private void Exit(object? parameter)
        {
            if (!PromptSaveIfNeeded())
                return;

            Application.Current.Shutdown();
        }

        /// <summary>
        /// Prompts the user to save if there are unsaved changes.
        /// Returns true if the operation should continue, false to cancel.
        /// </summary>
        private bool PromptSaveIfNeeded()
        {
            if (!HasUnsavedChanges)
                return true;

            var result = ConfirmDialog.ShowThreeWay(
                OwnerWindow,
                "Unsaved Changes",
                "You have unsaved changes. Do you want to save before continuing?",
                null,
                DialogIcon.Warning,
                "Save",
                "Don't Save",
                "Cancel");

            if (result == true) // Save
            {
                SaveArchive(null);
                return true;
            }
            else if (result == false) // Don't Save
            {
                return true;
            }
            else // Cancel (null)
            {
                return false;
            }
        }

        private void PackArchive(object? parameter)
        {
            if (_archive == null) return;

            var shouldPack = ConfirmDialog.Show(
                OwnerWindow,
                "Pack Archive",
                "Pack the archive to remove unused space?",
                "This will defragment the archive and may reduce file size.",
                DialogIcon.Question,
                "Pack",
                "Cancel");

            if (!shouldPack)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Packing archive...";

                var newSize = _archive.Pack();
                HasUnsavedChanges = true;
                
                StatusMessage = $"Archive packed to {newSize} blocks";
            }
            catch (Exception ex)
            {
                ShowError("Failed to pack archive", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void RefreshEntries()
        {
            Entries.Clear();
            SelectedEntries.Clear();

            if (_archive != null)
            {
                foreach (var entry in _archive)
                {
                    Entries.Add(new FileEntryViewModel(entry));
                }
            }

            OnPropertyChanged(nameof(IsArchiveOpen));
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(FilteredEntries));
            OnPropertyChanged(nameof(ArchiveInfo));
            UpdateWindowTitle();
        }

        private void CloseCurrentArchive()
        {
            _archive?.CloseWithoutSync();
            _archive = null;
            CurrentFilePath = string.Empty;
            HasUnsavedChanges = false;
            Entries.Clear();
            SelectedEntries.Clear();
            OnPropertyChanged(nameof(IsArchiveOpen));
            OnPropertyChanged(nameof(ArchiveInfo));
        }

        private void UpdateWindowTitle()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                WindowTitle = "GTA IMG Tool by VPZ";
            }
            else
            {
                var fileName = Path.GetFileName(CurrentFilePath);
                var modified = HasUnsavedChanges ? " *" : "";
                WindowTitle = $"{fileName}{modified} - GTA IMG Tool by VPZ";
            }
        }

        private void ShowError(string message, Exception ex)
        {
            StatusMessage = message;
            MessageDialog.Show(
                OwnerWindow,
                "Error",
                $"{message}\n\nDetails: {ex.Message}",
                DialogIcon.Error);
        }

        public void UpdateSelection(IList<object> selectedItems)
        {
            SelectedEntries.Clear();
            foreach (var item in selectedItems.OfType<FileEntryViewModel>())
            {
                SelectedEntries.Add(item);
            }
            OnPropertyChanged(nameof(HasSelection));
        }

        #endregion
    }
}

