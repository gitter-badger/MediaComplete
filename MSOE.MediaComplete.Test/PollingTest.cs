﻿using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSOE.MediaComplete.Lib;
using MSOE.MediaComplete.Test.Util;
using Constants = MSOE.MediaComplete.Test.Util.Constants;

namespace MSOE.MediaComplete.Test
{
    [TestClass]
    public class PollingTest
    {
        private static FileInfo _file;
        private DirectoryInfo _dir;
        private const string DirectoryPath = "TESTinboxForLibrary";

        /// <summary>
        /// deletes the file and directory if they still exist
        /// </summary>
        [TestCleanup]
        public void After()
        {
            Directory.Delete(_dir.FullName, true);
        }
        /// <summary>
        /// tests that polling works in sub-directories
        /// </summary>
        [TestMethod]
        [Timeout(40000)]
        public void CheckSubDirectoryPolling()
        {
            var polling = new Polling();
            _dir = FileHelper.CreateDirectory(DirectoryPath+"/Subdir/Subdirdir");
            _file = FileHelper.CreateFile(_dir, Constants.FileTypes.ValidMp3);

            var pass = false;
            SettingWrapper.InboxDir = DirectoryPath;
            polling.TimeInMinutes = 0.0005;
            Polling.InboxFilesDetected += delegate
            {
                pass = true;
            };
            polling.Start();

            while (!pass)
            {
                if (pass)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// tests that it calls the delegate
        /// </summary>
        [TestMethod]
        [Timeout(40000)]
        public void CheckForSongMp3()
        {
            var polling = new Polling();
            _dir = FileHelper.CreateDirectory(DirectoryPath);
            _file = FileHelper.CreateFile(_dir, Constants.FileTypes.ValidMp3);

            var pass = false;
            SettingWrapper.InboxDir = DirectoryPath;
            polling.TimeInMinutes = 0.0005;
            Polling.InboxFilesDetected += delegate
            {
                pass = true;
            };
            polling.Start();

            while (!pass)
            {
                if (pass)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// tests that it calls the delegate
        /// </summary>
        [TestMethod]
        [Timeout(40000)]//Milliseconds
        public void CheckForSongWma()
        {
            var polling = new Polling();
            _dir = FileHelper.CreateDirectory(DirectoryPath);
            _file = FileHelper.CreateFile(_dir, Constants.FileTypes.ValidWma);

            var pass = false;
            SettingWrapper.InboxDir = DirectoryPath;
            polling.TimeInMinutes = 0.0005;
            Polling.InboxFilesDetected += delegate
            {
                pass = true;
            };
            polling.Start();

            while (!pass)
            {
                if (pass)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// tests that it calls the delegate
        /// </summary>
        [TestMethod]
        public void CheckForSongNotMusic()
        {
            var polling = new Polling();
            _dir = FileHelper.CreateDirectory(DirectoryPath);
            _file = FileHelper.CreateFile(_dir, Constants.FileTypes.NonMusic);

            var pass = false;
            SettingWrapper.InboxDir = DirectoryPath;

            polling.TimeInMinutes = 0.0005;
            Polling.InboxFilesDetected += delegate
            {
                pass = true;
            };
            polling.Start();

            Thread.Sleep(500);

            if (!File.Exists(_file.FullName))
            {
                Assert.Fail("File does not exist in source directory - it should not have been moved.");
            }
            if (pass)
            {
                Assert.Fail("Event was triggered, it shouldnt have been.");
            }
        }
    }
}
