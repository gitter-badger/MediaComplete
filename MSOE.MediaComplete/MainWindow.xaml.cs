﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MSOE.MediaComplete.Lib;
using WinForms = System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using MSOE.MediaComplete.CustomControls;
using MSOE.MediaComplete.Lib.Sorting;

namespace MSOE.MediaComplete
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string _homeDir;

        private const string Mp3FileFormat = "MP3 Files (*.mp3)|*.mp3";
        private const string FileDialogTitle = "Select Music File(s)";

        public MainWindow()
        {
            InitializeComponent();

            var homeDir = SettingWrapper.GetHomeDir() ??
                          Path.GetPathRoot(Environment.SystemDirectory);
            if (!homeDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
            {
                homeDir += Path.DirectorySeparatorChar;
            }

            if (SettingWrapper.GetIsPolling())
            {
                Polling.Instance.TimeInMinutes = SettingWrapper.GetPollingTime();
                Polling.Instance.Start();
            }
            Polling.InboxFilesDetected += ImportFromInbox;
            Directory.CreateDirectory(homeDir);
            _homeDir = homeDir;

            InitTreeView();
        }

        private async void ImportFromInbox(IEnumerable<FileInfo> files)
        {
            if (SettingWrapper.GetShowInputDialog())
            {
                Dispatcher.BeginInvoke(new Action(() => InboxImportDialog.Prompt(this, files)));
            }
            else
            {
                await Importer.Instance.ImportFiles(files.Select(f => f.FullName).ToArray(), false);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            new Settings().Show();
        }

        private async void AddFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new WinForms.OpenFileDialog
            {
                Filter = Mp3FileFormat,
                InitialDirectory = "C:",
                Title = FileDialogTitle,
                Multiselect = true
            };
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await Importer.Instance.ImportFiles(fileDialog.FileNames, true);
            } 
            RefreshTreeView();
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new WinForms.FolderBrowserDialog();
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedDir = folderDialog.SelectedPath;
                await Importer.Instance.ImportDirectory(selectedDir, true);
            }
            RefreshTreeView();
        }

        public void RefreshTreeView()
        {
            // TODO this will cause ugly flickering when we have a ton of files
            LibraryTree.Items.Clear();

            var rootDirInfo = new DirectoryInfo(_homeDir);
            var tree = CreateDirectoryItem(rootDirInfo);
            tree.Header = _homeDir;
            LibraryTree.Items.Add(tree);
        }

        private void InitTreeView()
        {
            RefreshTreeView();

            var watcher = new FileSystemWatcher(_homeDir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            };
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnChanged;

            watcher.EnableRaisingEvents = true;
        }

        private TreeViewItem CreateDirectoryItem(DirectoryInfo dirInfo)
        {
            var dirItem = new FolderTreeViewItem { Header = dirInfo.Name };
            foreach (var dir in dirInfo.GetDirectories())
            {
                dirItem.Items.Add(CreateDirectoryItem(dir));
            }

            foreach (var file in dirInfo.GetFiles())
            {
                dirItem.Items.Add(new SongTreeViewItem
                {
                    Header = file.Name,
                    ContextMenu = LibraryTree.Resources["SongContextMenu"] as ContextMenu
                });
            }

            return dirItem;
        }

        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                if (win != null)
                {
                    win.RefreshTreeView();
                }
            });
        }

        private async void Toolbar_AutoIDMusic_Click(object sender, RoutedEventArgs e)
        {
            // TODO support multi-select
            var selection = LibraryTree.SelectedItem as TreeViewItem;
            if (!(selection is SongTreeViewItem)) return;
            await MusicIdentifier.IdentifySong(selection.FilePath());
        }

        private async void ContextMenu_AutoIDMusic_Click(object sender, RoutedEventArgs e)
        {
            // TODO support multi-select
            var menuItem = sender as MenuItem;
            if (menuItem == null)
                return;
            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu == null)
                return;
            var selection = contextMenu.PlacementTarget as TreeViewItem;
            try
            {
                await MusicIdentifier.IdentifySong(selection.FilePath());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Triggers an asyncronous sort operation. The sort engine first calculates the magnitude of the changes, and reports it to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Toolbar_SortMusic_Click(object sender, RoutedEventArgs e)
        {
            // TODO - obtain from settings file, make configurable
            var settings = new SortSettings
            {
                SortOrder = new List<MetaAttribute> { MetaAttribute.Artist, MetaAttribute.Album }
            };

            var sorter = new Sorter(new DirectoryInfo(_homeDir), settings);

            if (sorter.MoveActions.Count == 0) // Nothing to sort! Notify and return.
            {
                MessageBox.Show(this,
                    String.Format(Resources["Dialog-SortLibrary-NoSort"].ToString(), sorter.UnsortableCount),
                    Resources["Dialog-SortLibrary-NoSortTitle"].ToString(), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(this,
                String.Format(Resources["Dialog-SortLibrary-Confirm"].ToString(), sorter.MoveActions.Count,
                    sorter.UnsortableCount),
                Resources["Dialog-SortLibrary-Title"].ToString(), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            try
            {
                await sorter.PerformSort();
            }
            catch (IOException ioe)
            {
                // TODO - This should get localized and put in the application status bar (TBD)
                MessageBox.Show("Encountered an error while sorting files: " + ioe.Message);
            }
        }
    }
}