﻿using Autofac;
using Autofac.Core;
using MSOE.MediaComplete.Lib.Background;
using MSOE.MediaComplete.Lib.Files;
using MSOE.MediaComplete.Lib.Import;
using MSOE.MediaComplete.Lib.Metadata;
using MSOE.MediaComplete.Lib.Playing;
using MSOE.MediaComplete.Lib.Playlists;
using MSOE.MediaComplete.Lib.Sorting;

namespace MSOE.MediaComplete.Lib
{
    public static class Dependency
    {
        private static IContainer _afContainer;

        public static async void BuildAsync()
        {
            var builder = new ContainerBuilder();
            var fileManager = FileManager.Instance;
            fileManager.Initialize(SettingWrapper.MusicDir);
            builder.RegisterInstance(fileManager).ExternallyOwned().As<IFileManager>();
            builder.RegisterInstance(StatusBarHandler.Instance);
            builder.RegisterType<FfmpegAudioReader>().As<IAudioReader>();
            builder.RegisterType<DoresoIdentifier>().As<IAudioIdentifier>();
            builder.RegisterInstance(await SpotifyMetadataRetriever.GetInstanceAsync()).ExternallyOwned().As<IMetadataRetriever>();
            builder.RegisterType<Identifier>().WithParameters(new[]
            {
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IAudioReader), (pi, c) => c.Resolve<IAudioReader>()),
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IAudioIdentifier), (pi, c) => c.Resolve<IAudioIdentifier>()),
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IMetadataRetriever), (pi, c) => c.Resolve<IMetadataRetriever>()),
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IFileManager), (pi, c) => c.Resolve<IFileManager>())
            });
            builder.RegisterType<Sorter>().WithParameters(new[]
            {
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IFileManager), (pi, c) => c.Resolve<IFileManager>())
            });
            builder.RegisterType<Importer>().WithParameters(new[]
            {
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IFileManager), (pi, c) => c.Resolve<IFileManager>())
            });
            builder.RegisterType<NAudioWrapper>().As<INAudioWrapper>();
            var polling = new Polling();
            builder.RegisterInstance(polling).As<IPolling>();

            builder.RegisterType<PlaylistServiceImpl>().As<IPlaylistService>().WithParameters(new[]
            {
                new ResolvedParameter((pi, c) => pi.ParameterType == typeof(IFileManager), (pi, c) => c.Resolve<IFileManager>())
            });
            var queue = new Queue();
            builder.RegisterInstance(queue).As<IQueue>();
            _afContainer = builder.Build();
        }

        /// <summary>
        /// Used to get an instance of a dependency that will/should persist for the lifetime of the application. e.g. single instance classes
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Resolve<T>()
        {
            if (_afContainer == null) BuildAsync();
            return _afContainer.Resolve<T>();
        }
        /// <summary>
        /// Used to get an instance of a dependency with a lifetime. i.e. something that will end during the lifetime of the application
        /// </summary>
        /// <returns></returns>
        public static ILifetimeScope BeginLifetimeScope()
        {
            return _afContainer.BeginLifetimeScope();
        }
    }
}