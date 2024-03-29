﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Meta.Internal.Playback
{
    /// <summary>
    /// Handles generic playback functionality for frames in separate files. 
    /// Must be extended for format-specific playback.
    /// </summary>
    /// <typeparam name="T">The type of frame file to be loaded.</typeparam>
    internal class GenericDirectoryPlayback<T> : IPlaybackSource<T>
    {
        protected string _playbackFolder;
        protected List<T> _frames;
        protected T _currFrame;
        protected IFileParser<T> _parser;
        protected int _totalFrames = 0;
        protected int _currIndex = -1;
        protected string _extension;
        private bool _hasValidSource = false;

        public T CurrentFrame
        {
            get { return _currFrame; }
        }

        public GenericDirectoryPlayback()
        {
            _frames = new List<T>();
        }

        /// <summary>
        /// Creates a new playback object which reads from the given source.
        /// </summary>
        /// <param name="directory">The path to the playback directory.</param>
        /// <param name="extension"></param>
        public GenericDirectoryPlayback(string directory, string extension) 
        {
            _playbackFolder = directory;
            _extension = extension;
            if (!ValidRecordedFolder(_playbackFolder))
            {
                throw new DirectoryNotFoundException("The playback directory was not found");
            }
            _hasValidSource = true;
            _frames = new List<T>();
        }

        /// <summary>
        /// Creates a new playback object which reads from the given source.
        /// </summary>
        /// <param name="directory">The path to the playback directory.</param>
        /// <param name="extension">The extension associated with the frame files.</param>
        /// <param name="parser">The predefined parser to use for file reading.</param>
        public GenericDirectoryPlayback(string directory, string extension, IFileParser<T> parser) : this(directory, extension)
        {
            _parser = parser;
        }

        protected int TryGetFileIDLength(FileInfo f)
        {
            string filename = Path.GetFileNameWithoutExtension(f.Name);
            int idDelimiter = filename.IndexOf('-');
            int idLength = (idDelimiter == -1) ? filename.Length : idDelimiter;
            return int.Parse(filename.Substring(0, idLength));
        } 

        #region Playback storage

        /// <summary>
        /// Finds files of the specified format in the playback directory and processes them for playback.
        /// </summary>
        /// <param name="ext"></param>
        public virtual void LoadFrameFiles()
        {
            DirectoryInfo dir = new DirectoryInfo(_playbackFolder);
            IOrderedEnumerable<FileInfo> files = dir.GetFiles(_extension).OrderBy(
                f => TryGetFileIDLength(f)
            );

            // This may need to be updated if there are invalid frames found, if checking # frames read as the stopping condition.
            _totalFrames = files.Count();
            foreach (FileInfo f in files)
            {
                ProcessFile(f, 0);
            }
        }

        /// <summary>
        /// Reads the file and queues data for this frame using the connected parser.
        /// </summary>
        /// <param name="f">The file to be parsed.</param>
        /// <param name="id">The id used to order processing of files. This is different to the frame ID.</param>
        protected virtual void ProcessFile(FileInfo f, int id)
        {
            try
            {
                T frame = _parser.ParseFile(f, id);
                AddToPlayback(frame);
            }
            catch (Exception e)
            {
                // This is an invalid file
                _totalFrames--;
                UnityEngine.Debug.Log(e.Message);
            }
        }

        /// <summary>
        /// Adds the frame data object to the playback queue.
        /// </summary>
        /// <param name="data"></param>
        protected virtual void AddToPlayback(T data)
        {
            _frames.Add(data);
        }

        /// <summary>
        /// Checks if the playback folder exists and is valid.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to be checked.</param>
        /// <returns></returns>
        private bool ValidRecordedFolder(string sourcePath)
        {
            return Directory.Exists(sourcePath);
        }

        #endregion

        #region Playback Controls

        public virtual bool HasValidSource()
        {
            return _hasValidSource;
        }

        public virtual int GetTotalFrameCount()
        {
            return _totalFrames;
        }

        public virtual int GetCurrentFrameIndex()
        {
            return _currIndex;
        }

        public virtual bool AreFramesLoaded()
        {
            return (_frames.Count != 0) && (_frames.Count == _totalFrames);
        }   

        public virtual bool IsFinished()
        {
            return (_currIndex + 1 == _totalFrames);
        }

        public virtual bool HasNextFrame()
        {
            return (_currIndex >= -1 && _currIndex + 1 < _frames.Count);
        }

        public virtual T NextFrame()
        {
            _currIndex++;
            _currFrame = _frames[_currIndex];
            return _currFrame;
        }

        public string GetPlaybackSourcePath()
        {
            return _playbackFolder;
        }

        public virtual bool HasPrevFrame()
        {
            return (_currIndex > -1 && _currIndex <= _frames.Count);
        }

        public virtual T PreviousFrame()
        {
            _currIndex--;
            _currFrame = _frames[_currIndex];
            return _currFrame;
        }

        public virtual void Reset()
        {
            if (_frames.Count > 0)
            {
                _currIndex = -1;
                _currFrame = _frames[0];
            }
        }

        public void Clear()
        {
            _frames.Clear();
            _currIndex = -1;
            _totalFrames = 0;
            _hasValidSource = false;
            _playbackFolder = "";
        }

        public virtual void UseNewPlaybackSourcePath(string directory, string extension)
        {
            _playbackFolder = directory;
            _extension = extension;
            if (!ValidRecordedFolder(_playbackFolder))
            {
                throw new DirectoryNotFoundException("The playback directory was not found");
            }
            _hasValidSource = true;
            _currFrame = default(T);
            _currIndex = -1;
            _frames.Clear();
            LoadFrameFiles();
        }

        #endregion
    }
}