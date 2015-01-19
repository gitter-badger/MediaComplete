﻿using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using MSOE.MediaComplete.Lib;
using MSOE.MediaComplete.Lib.Sorting;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Label = System.Windows.Controls.Label;

namespace MSOE.MediaComplete
{
    /// <summary>

    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        private readonly SortSettings _sortSettings;
        private readonly bool _hasBeenSelected;
        private readonly List<Label> _labels;
        private List<MetaAttribute> _sortOrderList; 

        public Settings()
        {

            InitializeComponent();
            TxtboxSelectedFolder.Text = SettingWrapper.GetHomeDir();
            TxtboxInboxFolder.Text = SettingWrapper.GetInboxDir();
            ComboBoxPollingTime.SelectedValue = SettingWrapper.GetPollingTime().ToString(CultureInfo.InvariantCulture);
            CheckboxPolling.IsChecked = SettingWrapper.GetIsPolling();
            CheckboxShowImportDialog.IsChecked = SettingWrapper.GetShowInputDialog();
            CheckBoxSorting.IsChecked = SettingWrapper.GetIsSorting();
            PollingCheckBoxChanged(CheckboxPolling, null);

            _hasBeenSelected = false;
            _labels = new List<Label>();
            var list = SettingWrapper.GetSortOrder();
            _sortOrderList = SortHelper.MetaAttributesFromString(list);
            _sortSettings = new SortSettings();
            LoadSortListBox();

            MinusButton.Click += MinusClicked;
            PlusButton.Click += PlusClicked;
            SortOrderComboBox.SelectionChanged += SelectChanged;
        }

        private void LoadSortListBox()
        {
            for (var i = 0; i < _sortOrderList.Count; i++)
            {
                var label = new Label
                {
                    Content = _sortOrderList[i],
                    Padding = new Thickness(8 * (i + 1), 8, 8, 8)
                        
                };
                _labels.Add(label);
                SortConfig.Children.Add(label);
            }
            SortOrderComboBox.ItemsSource = SortHelper.GetAllUnusedMetaAttributes(_sortOrderList);
            
            ChangingColumn.Width = new GridLength(8 *_sortOrderList.Count);

        }

        private void PlusClicked(object sender, RoutedEventArgs e)
        {
            SortConfig.Children.Clear();
            _labels.Clear();
            _sortOrderList.Add((MetaAttribute)SortOrderComboBox.SelectedValue);
            LoadSortListBox();
            PlusButton.Visibility = Visibility.Hidden;
        }

        private void MinusClicked(object sender, RoutedEventArgs e)
        {
            var toRemove = _labels.Count - 1;

            if (toRemove < 0) return;
            _sortOrderList.RemoveAt(toRemove);
            _labels.Clear();

            SortConfig.Children.Clear();
            LoadSortListBox();
                
            if(toRemove == 0)
            {
                MinusButton.Visibility = Visibility.Hidden;
            }
        }

        private void SelectChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_hasBeenSelected) return;
            PlusButton.Visibility = Visibility.Visible;
            MinusButton.Visibility = Visibility.Visible;
        }


        /// <summary>
        /// The handler for the folder selection buttons of the setting screen.
        /// Handles where the path from the dialog will appear.
        /// </summary>
        /// <param name="sender">The sender of the action(the folder selection button)</param>
        /// <param name="e">Type of event</param>
        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            var folderBrowserDialog1 = new FolderBrowserDialog();
            var button = sender as Button;
            if (folderBrowserDialog1.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            if (button == null) return;
            switch (button.Name)
            {
                case "BtnSelectFolder":
                    TxtboxSelectedFolder.Text = folderBrowserDialog1.SelectedPath;
                    break;
                case "BtnInboxFolder":
                    TxtboxInboxFolder.Text = folderBrowserDialog1.SelectedPath;
                    break;
            }
        }


        /// <summary>
        /// The handler of the checkbox change event for the setting screen.
        /// Will enable or disable the polling related fields.
        /// </summary>
        /// <param name="sender">The sender of the action(the checkbox for polling)</param>
        /// <param name="e">Type of event</param>
        private void PollingCheckBoxChanged(object sender, RoutedEventArgs e)
        {

            var button = sender as System.Windows.Controls.CheckBox;
            if (button != null && button.IsChecked == true)
            {
                CheckboxShowImportDialog.IsEnabled = true;

                TxtboxInboxFolder.IsEnabled = true;
                ComboBoxPollingTime.IsEnabled = true;
                BtnInboxFolder.IsEnabled = true;
                LblPollTime.IsEnabled = true;
                LblMin.IsEnabled = true;
                LblSelectInboxLocation.IsEnabled = true;
            }
            else
            {
                CheckboxShowImportDialog.IsEnabled = false;
                TxtboxInboxFolder.IsEnabled = false;
                ComboBoxPollingTime.IsEnabled = false;
                BtnInboxFolder.IsEnabled = false;
                LblPollTime.IsEnabled = false;
                LblMin.IsEnabled = false;
                LblSelectInboxLocation.IsEnabled = false;
            }
        }

        /// <summary>
        /// The handler for a save button click.
        /// Will save the values to the properties file.
        /// </summary>
        /// <param name="sender">The sender of the action</param>
        /// <param name="e">Type of event</param>
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // add settings here as they are added to the UI
            var homeDir = TxtboxSelectedFolder.Text;
            if (!homeDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.CurrentCulture)))
            {
                homeDir += Path.DirectorySeparatorChar;
            }
            var inboxDir = TxtboxInboxFolder.Text;
            if (!inboxDir.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.CurrentCulture)))
            {
                inboxDir += Path.DirectorySeparatorChar;
            }

            SettingWrapper.SetHomeDir(homeDir);

            SettingWrapper.SetInboxDir(inboxDir);
            SettingWrapper.SetPollingTime(ComboBoxPollingTime.SelectedValue);
            SettingWrapper.SetIsPolling(CheckboxPolling.IsChecked.GetValueOrDefault(false));
            SettingWrapper.SetShowInputDialog(CheckboxShowImportDialog.IsChecked.GetValueOrDefault(false));
            SettingWrapper.SetIsSorting(CheckBoxSorting.IsChecked.GetValueOrDefault(false));

            var pastSort = _sortSettings.SortOrder;
            SettingWrapper.SetSortOrder(_sortOrderList);

            SettingWrapper.Save();

            _sortSettings.SortOrder = _sortOrderList;

            if (pastSort != _sortSettings.SortOrder)
            {

                var sorter = new Sorter(new DirectoryInfo(SettingWrapper.GetHomeDir()), _sortSettings);
                await (sorter.PerformSort());
            }

            Close();
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs args)
        {
            var dataList = new List<string> { "0.5", "1", "5", "10", "30", "60", "120", "240" };

            var box = sender as ComboBox;
            if (box != null) box.ItemsSource = dataList;
        }

        private void ResetDefault(object sender, RoutedEventArgs e)
        {
            _sortOrderList = SortHelper.GetDefault();
            SortConfig.Children.Clear();
            LoadSortListBox();

        }
    }
}
