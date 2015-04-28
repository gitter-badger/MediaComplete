﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MSOE.MediaComplete.CustomControls;
using MSOE.MediaComplete.Lib;
using MSOE.MediaComplete.Lib.Import;
using MSOE.MediaComplete.Lib.Logging;
using MSOE.MediaComplete.Lib.Metadata;
using MSOE.MediaComplete.Lib.Playing;
using MSOE.MediaComplete.Lib.Playlists;
using MSOE.MediaComplete.Lib.Songs;
using MSOE.MediaComplete.Lib.Sorting;
using NAudio.Wave;
using WinForms = System.Windows.Forms;

namespace MSOE.MediaComplete
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region Properties

        /// <summary>
        /// The root of the library TreeView strucutre.
        /// </summary>
        public FolderTreeViewItem RootLibraryFolderItem
        {
            get { return _rootLibItem; }
        }
        private readonly FolderTreeViewItem _rootLibItem = new FolderTreeViewItem { Header = SettingWrapper.MusicDir, IsSelected = true };

        /// <summary>
        /// Contains the songs in the middle view. Filtered based on what's happening in the left pane.
        /// </summary>
        public CollectionViewSource Songs { get { return _songs; } }
        private readonly CollectionViewSource _songs = new CollectionViewSource { Source = new ObservableCollection<SongListItem>() };

        /// <summary>
        /// Contains the songs in the middle view. Filtered based on what's happening in the left pane.
        /// </summary>
        public CollectionViewSource PlaylistSongs { get { return _playlistSongs; } }
        private readonly CollectionViewSource _playlistSongs = new CollectionViewSource { Source = new ObservableCollection<SongListItem>() };

        private readonly Timer _refreshTimer = new Timer(TimerProc);
        private readonly FileMover _fileMover = FileMover.Instance;
        #endregion

        #region Construction
        /// <summary>
        /// And so it begins.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            InitUi();
            InitEvents();
            InitTreeView();
            InitPlayer();
        }

        /// <summary>
        /// Setup special UI elements
        /// </summary>
        public static void InitUi()
        {
            var dictUri = new Uri(SettingWrapper.Layout, UriKind.Relative);
            var resourceDict = Application.LoadComponent(dictUri) as ResourceDictionary;
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(resourceDict);
        }

        /// <summary>
        /// Setup application wide events
        /// </summary>
        private void InitEvents()
        {
            StatusBarHandler.Instance.RaiseStatusBarEvent += HandleStatusBarChangeEvent;
            Logger.SetLogLevel(SettingWrapper.LogLevel);
            Polling.InboxFilesDetected += ImportFromInboxAsync;
            // ReSharper disable once ObjectCreationAsStatement
            new Sorter(_fileMover, null); // Run static constructor
            // ReSharper disable once UnusedVariable
            var tmp = Polling.Instance;  // Run singleton constructor
        }

        /// <summary>
        /// Setup the song library
        /// </summary>
        private void InitTreeView()
        {
            _fileMover.CreateDirectory(SettingWrapper.MusicDir);
            RefreshTreeView();

            var watcher = new FileSystemWatcher(SettingWrapper.MusicDir)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.Renamed += OnChanged;

            watcher.EnableRaisingEvents = true;

            Songs.Filter += LibrarySongFilter;
        }
        #endregion

        #region User triggered events

        /// <summary>
        /// Open the Settings window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToolbarSettings_Click(object sender, RoutedEventArgs e)
        {
            new Settings { Owner = this }.ShowDialog();
        }

        /// <summary>
        /// Open a file selection dialog for importing, and do the import.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            var fileDialog = new WinForms.OpenFileDialog
            {
                Filter =
                    Resources["Dialog-AddFile-MusicFilter"] + "" + Constants.FileDialogFilterStringSeparator + string.Join<string>(";",Constants.MusicFileExtensions.Select(s => Constants.Wildcard+s)) + Constants.FileDialogFilterStringSeparator +
                    Resources["Dialog-AddFile-Mp3Filter"] + "" + Constants.FileDialogFilterStringSeparator + Constants.Wildcard + Constants.MusicFileExtensions[0] + Constants.FileDialogFilterStringSeparator +
                    Resources["Dialog-AddFile-WmaFilter"] + "" + Constants.FileDialogFilterStringSeparator + Constants.Wildcard + Constants.MusicFileExtensions[1],
                    InitialDirectory = Path.GetPathRoot(Environment.SystemDirectory),
                    Title = Resources["Dialog-AddFile-Title"].ToString(),
                    Multiselect = true
            };

            if (fileDialog.ShowDialog() != WinForms.DialogResult.OK) return;

            ImportResults results;
            try
            {
                results = await new Importer(SettingWrapper.MusicDir).ImportFilesAsync(fileDialog.FileNames.Select(p => new FileInfo(p)).ToList(), SettingWrapper.ShouldRemoveOnImport);
            }
            catch (InvalidImportException)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    String.Format(Resources["Dialog-Import-Invalid-Message"].ToString()),
                    Resources["Dialog-Common-Error-Title"].ToString(),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (results.FailCount > 0)
            {
                MessageBox.Show(Application.Current.MainWindow, 
                    String.Format(Resources["Dialog-Import-ItemsFailed-Message"].ToString(), results.FailCount), 
                    Resources["Dialog-Common-Warning-Title"].ToString(), 
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Open a folder selection dialog, and import the results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void AddFolder_ClickAsync(object sender, RoutedEventArgs e)
        {
            var folderDialog = new WinForms.FolderBrowserDialog();

            if (folderDialog.ShowDialog() != WinForms.DialogResult.OK) return;
            var selectedDir = folderDialog.SelectedPath;

            var results = await new Importer(SettingWrapper.MusicDir).ImportDirectoryAsync(selectedDir, SettingWrapper.ShouldRemoveOnImport);
            if (results.FailCount > 0)
            {
                MessageBox.Show(Application.Current.MainWindow,
                    String.Format(Resources["Dialog-Import-ItemsFailed-Message"].ToString(), results.FailCount),
                    Resources["Dialog-Common-Warning-Title"].ToString(),
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// Updates the song list based on the folder selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderTree_OnSelectionChanged(object sender, EventArgs e)
        {
            if (!IsLoaded)
                return;

            FormCheck();
            if (FolderTree.SelectedItems != null)
            {
                RootLibraryFolderItem.IsSelected = FolderTree.SelectedItems.Count == 0;
                Songs.View.Refresh();
            }
            ClearDetailPane();
        }

        /// <summary>
        /// Updates the metadata form based on the song list selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormCheck();
            if (AllSongs().Any())
                PopulateMetadataForm();
            else
                ClearDetailPane();
        }

        /// <summary>
        /// Perform an auto-metadata restoration of the selected songs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Toolbar_AutoIDMusic_ClickAsync(object sender, RoutedEventArgs e)
        {
            // TODO (MC-45) mass ID of multi-selected songs and folders

            foreach (var selection in SelectedSongs())
            {
                try
                {
                    if (selection == null) continue;
                    await MusicIdentifier.IdentifySongAsync(_fileMover, selection.Data.GetPath());
                }
                catch (Exception ex)
                {
                    Logger.LogException("Automatic music identification error",ex);
                    StatusBarHandler.Instance.ChangeStatusBarMessage(
                        String.Format(Resources["MusicIdentification-Error"].ToString(), ex.Message),
                        StatusBarHandler.StatusIcon.Error);
                }
            }
        }

        /// <summary>
        /// Performs an auto-metadata restoration of the selected songs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ContextMenu_AutoIDMusic_ClickAsync(object sender, RoutedEventArgs e)
        {
            // Access the targetted song 
            // TODO (MC-45) mass ID of multi-selected songs and folders
            var menuItem = sender as MenuItem;
            if (menuItem == null)
                return;
            var contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu == null)
                return;
            foreach (var item in SelectedSongs())
            {
                try
                {
                    await MusicIdentifier.IdentifySongAsync(_fileMover, item.Data.GetPath());
                }
                catch (Exception ex)
                {
                    Logger.LogException("Automatic music identification error", ex);
                    StatusBarHandler.Instance.ChangeStatusBarMessage(
                        String.Format(Resources["MusicIdentification-Error"].ToString(), ex.Message), 
                        StatusBarHandler.StatusIcon.Error);
                }
            }
        }

        /// <summary>
        /// Triggers an asyncronous sort operation. The sort engine first calculates the magnitude of the changes, and reports it to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Toolbar_SortMusic_ClickAsync(object sender, RoutedEventArgs e)
        {
            var settings = new SortSettings
            {
                SortOrder = SettingWrapper.SortOrder,
                Files =  new DirectoryInfo(SettingWrapper.MusicDir).EnumerateFiles("*", SearchOption.AllDirectories)
                    .GetMusicFiles(),
                Root = new DirectoryInfo(SettingWrapper.MusicDir)
            };

            var sorter = new Sorter(_fileMover, settings);
            await sorter.CalculateActionsAsync();    

            if (sorter.Actions.Count == 0) // Nothing to do! Notify and return.
            {
                MessageBox.Show(Application.Current.MainWindow,
                    String.Format(Resources["Dialog-SortLibrary-NoSort"].ToString(), sorter.UnsortableCount),
                    Resources["Dialog-SortLibrary-NoSortTitle"].ToString(), MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(Application.Current.MainWindow,
                String.Format(Resources["Dialog-SortLibrary-Confirm"].ToString(), sorter.MoveCount, sorter.DupCount,
                    sorter.UnsortableCount),
                Resources["Dialog-SortLibrary-Title"].ToString(), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            
            sorter.PerformSort();
        }

        /// <summary>
        /// Updates the active list reference based on the new tab. Actual list hiding and 
        /// showing is done via bindings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeftFrame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistTab.IsSelected)
            {
                NowPlayingItem.IsSelected = true;
                // Manually fire this, NowPlayingItem.IsSelected won't do the job if that's already selected
                PlaylistTree_SelectionChanged(null, null); 
            }
            ClearDetailPane();
        }

        /// <summary>
        /// Update the list of songs based on the selected playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistTree_SelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var playlistSongs = (ObservableCollection<SongListItem>) PlaylistSongs.Source;
            var list = NowPlayingItem.IsSelected ? NowPlaying.Inst.Playlist : (Playlist)PlaylistTree.SelectedItem;
            
            playlistSongs.Clear();
            list.Songs.ForEach(s => playlistSongs.Add(new SongListItem { Content = s, Data = s }));

            // If now-playing, highlight the current song
            if (NowPlayingItem.IsSelected && NowPlaying.Inst.Index > -1 && !_player.PlaybackState.Equals(PlaybackState.Stopped))
            {
                var item = playlistSongs[NowPlaying.Inst.Index];
                item.IsPlaying = true;
                PlaylistSongList.ScrollIntoView(item);
            }

            ClearDetailPane();
        }

        #endregion

        #region Internally triggered events

        /// <summary>
        /// Triggered by the library filewatcher. Bumps the refresh timer so we don't make unnecessary 
        /// updates to the UI.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            _refreshTimer.Change(500, Timeout.Infinite);
        }

        /// <summary>
        /// Triggered when the filewatcher timer expires. This prevents a swarm of redundant treeview 
        /// updates for large-scale file movements.
        /// </summary>
        /// <param name="state"></param>
        private static void TimerProc(object state)
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

        /// <summary>
        /// Filters the songs based on whether one of their ancestors in the library treeview is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LibrarySongFilter(object sender, FilterEventArgs e)
        {
            var song = e.Item as LibrarySongItem;
            e.Accepted = song != null && song.ParentItem.IsSelectedRecursive();
        }

        /// <summary>
        /// populates the treeviews with all valid elements within the home directory
        /// </summary>
        public void RefreshTreeView()
        {
            var songs = Songs.Source as ObservableCollection<SongListItem>;
            if (songs == null)
                return; // TODO MC-125 log me
            songs.Clear();

            _rootLibItem.Children.Clear();

            PopulateFromFolder(_rootLibItem);
        }

        /// <summary>
        /// Recursively populates foldertree and songtree with elements
        /// </summary>
        /// <param name="parent"></param>
        private void PopulateFromFolder(FolderTreeViewItem parent)
        {
            var songList = Songs.Source as ICollection<SongListItem>;
            if (songList == null)
                return; // TODO MC-125 log me
            var dir = new DirectoryInfo(parent.GetPath());

            foreach (var child in dir.GetDirectories().Select(subdir => new FolderTreeViewItem { ParentItem = parent, Header = subdir.Name }))
            {
                parent.Children.Add(child);
                PopulateFromFolder(child);
            }

            foreach (var file in dir.GetFilesOrCreateDir().GetMusicFiles())
            {
                songList.Add(new LibrarySongItem { Content = file.Name, ParentItem = parent, Data = new LocalSong(file) });
            }
        }

        /// <summary>
        /// Catch status updates from the event and display them on the status bar.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        /// <param name="extraArgs"></param>
        private void HandleStatusBarChangeEvent(string format, string message, StatusBarHandler.StatusIcon icon, params object[] extraArgs)
        {
            Dispatcher.Invoke(() =>
            {
                var args = (new[] { message == null ? "" : Resources[message] }).Concat(extraArgs);
                StatusMessage.Text = String.Format(format, args.ToArray());
                var sourceUri = new Uri("./Resources/" + icon + ".png", UriKind.Relative);
                StatusIcon.Source = new BitmapImage(sourceUri);
            });
        }

        /// <summary>
        /// Triggered by the inbox file polling. Prompts the user, or just automagically imports.
        /// </summary>
        /// <param name="files">Newly discovered files.</param>
        private async void ImportFromInboxAsync(IEnumerable<FileInfo> files)
        {
            if (SettingWrapper.ShowInputDialog)
            {
                Dispatcher.BeginInvoke(new Action(() => InboxImportDialog.Prompt(this, files)));
            }
            else
            {
                await new Importer(SettingWrapper.MusicDir).ImportFilesAsync(files, true);
            }
        }
        #endregion

        #region Private helpers

        /// <summary>
        /// Helper methods to return the selected song items from the currently visible list.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SongListItem> SelectedSongs()
        {
            var currentSongs = (ObservableCollection<SongListItem>)(PlaylistTab.IsSelected ? PlaylistSongs : Songs).Source;
            return from object song in currentSongs 
                   where ((SongListItem)song).IsSelected && ((SongListItem)song).IsVisible 
                   select (song as SongListItem);
        }

        /// <summary>
        /// Helper methods to return all song items from the currently visible list.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SongListItem> AllSongs()
        {
            var currentSongs = (ObservableCollection<SongListItem>)(PlaylistTab.IsSelected ? PlaylistSongs : Songs).Source;
            return from object song in currentSongs 
                   where ((SongListItem)song).IsVisible 
                   select (song as SongListItem);
        }

        /// <summary>
        /// Returns the first index of a selected song in the currently visible list.
        /// </summary>
        /// <returns></returns>
        private int SelectedSongIndex()
        {
            var currentSongs = (ObservableCollection<SongListItem>)(PlaylistTab.IsSelected ? PlaylistSongs : Songs).Source;
            return currentSongs.IndexOf(currentSongs.FirstOrDefault(s => s.IsSelected));
        }

        #endregion
    }
}
