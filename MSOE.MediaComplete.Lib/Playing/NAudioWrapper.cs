﻿using System;
using System.IO;
using NAudio.Wave;
using NAudio.WindowsMediaFormat;
using TagLib;

namespace MSOE.MediaComplete.Lib.Playing
{
    /// <summary>
    /// concrete implementation of the wavestream factory
    /// </summary>
    public class NAudioWrapper : INAudioWrapper
    {
        private WaveStream _waveStream;
        private WaveOut _waveOut;

        /// <summary>
        /// returns the proper wavestream based on the given filetype
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private static WaveStream GetWaveStream(FileInfo file)
        {
            if(file == null) throw new ArgumentNullException("file");
            
            try
            {
                switch (file.Extension.ToLower())
                {
                    case Constants.Mp3FileExtension:
                        return new Mp3FileReader(file.FullName);
                    case Constants.WavFileExtension:
                        return new WaveFileReader(file.FullName);
                    case Constants.WmaFileExtension:
                        return new WMAFileReader(file.FullName);
                    default:
                        throw new UnsupportedFormatException(file.Extension + " is not supported");
                }
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException || ex is IndexOutOfRangeException || ex is UnsupportedFormatException || ex is CorruptFileException)
                {
                    throw new CorruptFileException(file.FullName +
                        " cannot be loaded, the file may be corrupt or have the wrong extension.", ex);
                }
                throw;
            }
        }

        public void Setup(FileInfo fileInfo, EventHandler<StoppedEventArgs> handler)
        {
            _waveOut = new WaveOut();
            _waveOut.Init(_waveStream = GetWaveStream(fileInfo));
            _waveOut.PlaybackStopped += handler;
        }

        public PlaybackState Play()
        {
            if (_waveOut == null) return PlaybackState.Stopped;
            _waveOut.Play();
            return _waveOut.PlaybackState;
        }

        public PlaybackState Pause()
        {
            if (_waveOut == null) return PlaybackState.Stopped;
            _waveOut.Pause();
            return _waveOut.PlaybackState;
        }

        public PlaybackState Stop()
        {
            if (_waveOut != null)
            {
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }
            if (_waveStream != null)
            {
                _waveStream.Dispose();
                _waveStream = null;
            }
            return PlaybackState.Stopped;
        }
    }
}