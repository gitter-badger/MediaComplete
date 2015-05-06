﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSOE.MediaComplete.Lib.Background;
using MSOE.MediaComplete.Lib.Files;
using MSOE.MediaComplete.Lib.Import;
using MSOE.MediaComplete.Lib.Metadata;
using MSOE.MediaComplete.Lib.Sorting;

namespace MSOE.MediaComplete.Test.Background
{
    [TestClass]
    public class MusicIdentiferManagementTest
    {
        [TestMethod]
        public void Test_AddIDEmptyQueue_Added()
        {
            var queue = new List<List<Task>>();
            var subject = new MusicIdentifier(new List<LocalSong>());
            TaskAdder.ResolveConflicts(subject, queue);

            Assert.AreEqual(1, queue.Count, "Queue doesn't have the right number of stages!");
            Assert.AreEqual(1, queue[0].Count, "Stage 1 doesn't have the new task!");
            Assert.AreSame(subject, queue[0][0], "Stage 1 doesn't have the subject task!");
        }

        /// <summary>
        /// Test that an ID will correctly insert itself after imports, but before sorts
        /// </summary>
        [TestMethod]
        public void Test_AddID_GoesBeforeSortButAfterIdentify()
        {
            var queue = new List<List<Task>>
            {
                new List<Task> {new ImportTask(null, null, false), new ImportTask(null, null, false)},
                new List<Task> {new ImportTask(null, null, false)},
                new List<Task> {new SortingTask(null)}
            };

            var subject = new MusicIdentifier(new List<LocalSong>());
            TaskAdder.ResolveConflicts(subject, queue);

            Assert.AreEqual(4, queue.Count, "Queue doesn't have the right number of stages!");

            Assert.AreEqual(2, queue[0].Count, "Stage 1 isn't the same size!");
            Assert.IsInstanceOfType(queue[0][0], typeof(ImportTask), "Stage 1 doesn't have an ImportTask!");
            Assert.IsInstanceOfType(queue[0][1], typeof(ImportTask), "Stage 1 doesn't have an ImportTask!");
            Assert.AreEqual(1, queue[1].Count, "Stage 2 isn't the same size!");
            Assert.IsInstanceOfType(queue[1][0], typeof(ImportTask), "Stage 2 doesn't have an ImportTask!");
            Assert.AreEqual(1, queue[2].Count, "Stage 3 isn't the same size!");
            Assert.AreSame(subject, queue[2][0], "Stage 3 doesn't have the subject!");
            Assert.AreEqual(1, queue[2].Count, "Stage 3 isn't the same size!");
            Assert.IsInstanceOfType(queue[3][0], typeof(SortingTask), "Stage 4 doesn't have a SortingTask!");
        }

        /// <summary>
        /// Test that an ID task will remove duplicates and run in the same stage as another ID.
        /// </summary>
        [TestMethod]
        public void Test_AddID_GoesWithOtherID()
        {
            var song1 = new LocalSong("id1", new SongPath("path1"));
            var song2 = new LocalSong("id2", new SongPath("path2"));
            var song3 = new LocalSong("id3", new SongPath("path3"));
            var queue = new List<List<Task>>
            {
                new List<Task> {new MusicIdentifier(new [] { song1, song2 })}
            };

            var subject = new MusicIdentifier(new[] { song2, song3 });
            TaskAdder.ResolveConflicts(subject, queue);

            Assert.AreEqual(1, queue.Count, "Queue doesn't have the right number of stages!");

            Assert.AreEqual(2, queue[0].Count, "Stage 1 doesn't have the new task!");
            Assert.IsInstanceOfType(queue[0][0], typeof(MusicIdentifier), "Stage 1 doesn't have the old ID task!");
            Assert.AreSame(subject, queue[0][1], "Stage 1 doesn't have the subject task!");

            Assert.AreEqual(2, ((MusicIdentifier)queue[0][0]).Files.Count(), "First ID task doesn't have the right number of files!");
            Assert.AreSame(song1, ((MusicIdentifier)queue[0][0]).Files.First(), "First ID task doesn't have the right file!");
            Assert.AreSame(song2, ((MusicIdentifier)queue[0][0]).Files.Skip(1).First(), "First ID task doesn't have the right file!");

            Assert.AreEqual(1, subject.Files.Count(), "Subject doesn't have the right number of files!");
            Assert.AreSame(song3, subject.Files.First(), "Subject doesn't have the right file!");
        }
    }
}
