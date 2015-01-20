﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSOE.MediaComplete.Test.Util
{
    /// <summary>
    /// Test related constant values
    /// </summary>
    class Constants
    {
        private const string ResourceDirName = "Resources";
        private const string InvalidMp3FileName = "InvalidMp3File.mp3";
        private const string InvalidMp3FullPath = ResourceDirName + "/" + InvalidMp3FileName;
        private const string ValidMp3FileName = "ValidMp3File.mp3";
        private const string ValidMp3FullPath = ResourceDirName + "/" + ValidMp3FileName;
        private const string UnknownMp3FileName = "UnknownMp3.mp3";
        private const string UnknownMp3FullPath = ResourceDirName + "/" + UnknownMp3FileName;
        private const string BlankedFileName = "Blanked.mp3";
        private const string BlankedFullPath = ResourceDirName + "/" + BlankedFileName;
        private const string MissingAlbumMp3FileName = "MissingAlbumMp3File.mp3";
        private const string MissingAlbumMp3FullPath = ResourceDirName + "/" + MissingAlbumMp3FileName;
        private const string MissingArtistMp3FileName = "MissingArtistMp3File.mp3";
        private const string MissingArtistMp3FullPath = ResourceDirName + "/" + MissingArtistMp3FileName;

        private const string NonMusicFileName = "NonMusicTestFile.txt";
        private const string NonMusicFullPath = ResourceDirName + "/" + NonMusicFileName;
        
        public enum FileTypes
        {
            Valid, Unknown, Blanked, NonMusic, Invalid, MissingAlbum, MissingArtist
        }

        public static readonly ReadOnlyDictionary<FileTypes, Tuple<string, string>> TestFiles =
            new ReadOnlyDictionary<FileTypes, Tuple<string, string>>(new Dictionary<FileTypes, Tuple<string, string>>
            {
                { FileTypes.Valid, new Tuple<string, string>(ValidMp3FileName, ValidMp3FullPath)},
                { FileTypes.Invalid, new Tuple<string, string>(InvalidMp3FileName, InvalidMp3FullPath)},
                { FileTypes.NonMusic, new Tuple<string, string>(NonMusicFileName, NonMusicFullPath)},
                { FileTypes.Unknown, new Tuple<string, string>(UnknownMp3FileName, UnknownMp3FullPath)},
                { FileTypes.Blanked, new Tuple<string, string>(BlankedFileName, BlankedFullPath)},
                { FileTypes.MissingAlbum, new Tuple<string, string>(MissingAlbumMp3FileName, MissingAlbumMp3FullPath)},
                { FileTypes.MissingArtist, new Tuple<string, string>(MissingArtistMp3FileName, MissingArtistMp3FullPath)},
            });
    }
}
