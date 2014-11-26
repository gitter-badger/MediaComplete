﻿using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace MSOE.MediaComplete.CustomControls
{
    internal class FolderTreeViewItem : TreeViewItem
    {
        public FolderTreeViewItem()
		{
            Children = new ObservableCollection<FolderTreeViewItem>();
		}


        public ObservableCollection<FolderTreeViewItem> Children { get; set; }

        public override string ToString()
        {
            return (string) Header;
        }
    }
}