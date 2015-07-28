﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using M3U.NET;
using MSOE.MediaComplete.Lib.Files;
using MSOE.MediaComplete.Lib.Metadata;
using MSOE.MediaComplete.Lib.Library.FileSystem;
using TagLib;
using File = System.IO.File;
using TaglibFile = TagLib.File;

namespace MSOE.MediaComplete.Lib.Library
{
    public class Library : ILibrary
    {
        private Dictionary<String, AbstractSong> _cachedSongs;
        /// <summary>
        /// singleton instance of the Filemanager
        /// </summary>
        private static Library _instance;
        public static ILibrary Instance { get { return _instance ?? (_instance = new Library(FileSystem.FileSystem.Instance)); } }
        private IFileSystem _fileSystem;

        private Library(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            Initialize(SettingWrapper.MusicDir);
        }

        /// <summary>
        /// Rebuilds the dictionaries using the parameter as the source. 
        /// </summary>
        /// <param name="musicDir">Source Directory for populating the dictionarires</param>
        public void Initialize(DirectoryPath musicDir)
        {
            _cachedSongs = new Dictionary<String, AbstractSong>();
            _fileSystem.Initialize(musicDir);
        }

        #region File Operations

        #endregion

        #region Data Operations
        /// <summary>
        /// Writes the attributes of the song parameter to the TagLib File and updates the stored FileInfo and song
        /// </summary>
        /// <param name="song">file with updated metadata</param>
        public void SaveSong(AbstractSong song)
        {
            if (song is LocalSong)
                _fileSystem.SaveFile(song as LocalSong);
        }

        /// <summary>
        /// Get every song object that exists in the cache
        /// </summary>
        /// <returns>IEnumerable containing every song within the cache</returns>
        public IEnumerable<AbstractSong> GetAllSongs()
        {
            return _cachedSongs.Values;
        }

        /// <summary>
        /// Removes a song from the caches, AND THE FILESYSTEM AS WELL. THAT FILE IS GONE NOW. DON'T JUST CALL THERE FOR THE HECK OF IT
        /// </summary>
        /// <param name="deletedSong">the song that needs to be deleted</param>
        public void DeleteSong(AbstractSong deletedSong)
        {
            if(deletedSong is LocalSong)
                _fileSystem.DeleteFile(deletedSong as LocalSong);
        }

        // <summary>
        // Get a LocalSong object with a matching SongPath object
        // </summary>
        // <param name="songPath">SongPath object to compare</param>
        // <returns>LocalSong if it exists, null if it doesn't</returns>
        public AbstractSong GetSong(SongPath songPath)
        {
            return _fileSystem.GetSong(songPath);
        }

        // <summary>
        // Returns a LocalSong that has a path that matches the MediaItem's location
        // </summary>
        // <param name="mediaItem">MediaItem for which the song is needed</param>
        // <returns>LocalSong if it exists, null if it doesn't</returns>
        public AbstractSong GetSong(MediaItem mediaItem)
        {
            return _fileSystem.GetSong(mediaItem);
        }
        #endregion


        public void SortSong(AbstractSong song)
        {
        }
    }

    public interface ILibrary
    {
        /// <summary>
        /// Rebuilds the dictionaries using the parameter as the source. 
        /// </summary>
        /// <param name="directory">Source Directory for populating the dictionarires</param>
        void Initialize(DirectoryPath directory);
        /// <summary>
        /// Writes the attributes of the song parameter to the TagLib File and updates the stored FileInfo and song
        /// </summary>
        /// <param name="song">file with updated metadata</param>
        void SaveSong(AbstractSong song);
        /// <summary>
        /// Get every song object that exists in the cache
        /// </summary>
        /// <returns>IEnumerable containing every song within the cache</returns>
        IEnumerable<AbstractSong> GetAllSongs();
        /// <summary>
        /// Removes a song from the caches, AND THE FILESYSTEM AS WELL. THAT FILE IS GONE NOW. DON'T JUST CALL THERE FOR THE HECK OF IT
        /// </summary>
        /// <param name="deletedSong">the song that needs to be deleted</param>
        void DeleteSong(AbstractSong deletedSong);
        /// <summary>
        /// Get a LocalSong object with a matching SongPath object
        /// </summary>
        /// <param name="songPath">SongPath object to compare</param>
        /// <returns>LocalSong if it exists, null if it doesn't</returns>
        AbstractSong GetSong(SongPath songPath);
        /// <summary>
        /// Returns a LocalSong that has a path that matches the MediaItem's location
        /// </summary>
        /// <param name="mediaItem">MediaItem for which the song is needed</param>
        /// <returns>LocalSong if it exists, null if it doesn't</returns>
        AbstractSong GetSong(MediaItem mediaItem);
        void SortSong(AbstractSong song);
    }

}
