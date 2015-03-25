﻿using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using MSOE.MediaComplete.Lib;
using consts = MSOE.MediaComplete.Lib.Constants;

namespace MSOE.MediaComplete.CustomControls
{
    internal class FolderTreeViewItem : TreeViewItem
    {
        public FolderTreeViewItem()
		{
            Children = new ObservableCollection<FolderTreeViewItem>();
		}
        
        /// <summary>
        /// Contains all folders contained within this folder.
        /// </summary>
        public ObservableCollection<FolderTreeViewItem> Children { get; set; }

        /// <summary>
        /// The folder the current folder is in. 
        /// This value is null if it is the root
        /// </summary>
        public FolderTreeViewItem ParentItem { get; set; }
       
        public override string ToString()
        {
            return (string) Header;
        }
        public int myInt = 10;

        /// <summary>
        /// Used to recursively determine the folder's path
        /// </summary>
        /// <returns>string representation of path</returns>
        public string GetPath()
        {
            if (ParentItem != null)
            {
                return ParentItem.GetPath() + Header + Path.DirectorySeparatorChar;
            }
            return SettingWrapper.MusicDir;
            
        }
    }
}