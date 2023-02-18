using LibVLCSharp.Shared;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Zhai.Famil.Common.Mvvm;

namespace Zhai.VideoView
{
    public class VideoSourceProvider : ViewModelBase, IDisposable
    {
        public VideoSourceProvider()
        {
            Core.Initialize();
        }

        public LibVLC LibVLC { get; private set; }

        public LibVLCSharp.Shared.MediaPlayer MediaPlayer { get; private set; }

        public void CreatePlayer(params string[] options)
        {
            this.LibVLC = new LibVLC(options);

            this.MediaPlayer = new LibVLCSharp.Shared.MediaPlayer(LibVLC);
        }

        public async Task<bool> TakeSnapshot(string mrl, string targetPath, uint width = 1024, uint height =0)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            CreatePlayer(
                "--intf", "dummy", /* no interface                   */
                "--vout", "dummy", /* we don't want video output     */
                "--no-audio", /* we don't want audio decoding   */
                "--no-video-title-show", /* nor the filename displayed     */
                "--no-stats", /* no stats */
                "--no-sub-autodetect-file", /* we don't want subtitles        */
                "--no-snapshot-preview" /* no blending in dummy vout      */
            );

            void PositionChanged(object? sender, MediaPlayerPositionChangedEventArgs e)
            {
                MediaPlayer.PositionChanged -= PositionChanged;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        bool isTaked = MediaPlayer.TakeSnapshot(0, targetPath, width, height);

                        MediaPlayer.Stop();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"VideoViewer: TakeSnapshot Error ...  Mrl:{mrl} TargetPath:{targetPath}", ex);
                    }
                    finally
                    {
                        tcs.TrySetResult(true);
                    }
                });
            };

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            MediaPlayer.PositionChanged += PositionChanged;

            MediaPlayer.EncounteredError += (sender, e) => tcs.TrySetCanceled();

            MediaPlayer.Play(new Media(LibVLC, mrl));

            MediaPlayer.Position = (float)0.5;

            return await tcs.Task;
        }

        #region Video Source Player

        private ImageSource videoSource;
        public ImageSource VideoSource
        {
            get => videoSource;

            private set => Set(() => VideoSource, ref videoSource, value);
        }

        private bool isAlphaChannelEnabled;
        public bool IsAlphaChannelEnabled
        {
            get => this.isAlphaChannelEnabled;

            set
            {
                if (!playerCreated)
                    this.isAlphaChannelEnabled = value;
                else
                    throw new InvalidOperationException("IsAlphaChannelEnabled property should be changed only before CreatePlayer method is called.");
            }
        }

        /// <summary>
        /// The memory mapped file handle that contains the picture data
        /// </summary>
        private IntPtr memoryMappedFile;

        /// <summary>
        /// The pointer to the buffer that contains the picture data
        /// </summary>
        private IntPtr memoryMappedView;

        private bool playerCreated;

        /// <summary>
        /// Creates the player. This method must be called before using <see cref="MediaPlayer"/>
        /// </summary>
        /// <param name="options">The initialization options to be given to libvlc</param>
        public void CreateVideoSourcePlayer(params string[] options)
        {
            CreatePlayer(options);

            this.MediaPlayer.SetVideoFormatCallbacks(this.VideoFormat, this.CleanupVideo);
            this.MediaPlayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);

            this.playerCreated = true;
        }

        /// <summary>
        /// Aligns dimension to the next multiple of mod
        /// </summary>
        /// <param name="dimension">The dimension to be aligned</param>
        /// <param name="mod">The modulus</param>
        /// <returns>The aligned dimension</returns>
        private uint GetAlignedDimension(uint dimension, uint mod)
        {
            var modResult = dimension % mod;
            if (modResult == 0)
            {
                return dimension;
            }

            return dimension + mod - (dimension % mod);
        }

        /// <summary>
        /// Called by vlc when the video format is needed. This method allocats the picture buffers for vlc and tells it to set the chroma to RV32
        /// </summary>
        /// <param name="userdata">The user data that will be given to the <see cref="LockVideo"/> callback. It contains the pointer to the buffer</param>
        /// <param name="chroma">The chroma</param>
        /// <param name="width">The visible width</param>
        /// <param name="height">The visible height</param>
        /// <param name="pitches">The buffer width</param>
        /// <param name="lines">The buffer height</param>
        /// <returns>The number of buffers allocated</returns>
        private uint VideoFormat(ref IntPtr userdata, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
        {
            var pixelFormat = IsAlphaChannelEnabled ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
            FourCCConverter.ToFourCC("RV32", chroma);

            //Correct video width and height according to TrackInfo
            foreach (MediaTrack track in MediaPlayer.Media.Tracks)
            {
                if (track.TrackType == TrackType.Video)
                {
                    var trackInfo = (VideoTrack)track.Data.Video;
                    if (trackInfo.Width > 0 && trackInfo.Height > 0)
                    {
                        width = trackInfo.Width;
                        height = trackInfo.Height;
                        if (trackInfo.SarDen != 0)
                        {
                            width = width * trackInfo.SarNum / trackInfo.SarDen;
                        }
                    }

                    break;
                }
            }

            pitches = this.GetAlignedDimension((uint)(width * pixelFormat.BitsPerPixel) / 8, 32);
            lines = this.GetAlignedDimension(height, 32);

            var size = pitches * lines;

            this.memoryMappedFile = Win32Interop.CreateFileMapping(new IntPtr(-1), IntPtr.Zero,
                 Win32Interop.PageAccess.ReadWrite, 0, (int)size, null);
            var handle = this.memoryMappedFile;

            var args = new
            {
                width,
                height,
                pixelFormat,
                pitches
            };

            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                this.VideoSource = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(handle,
                    (int)args.width, (int)args.height, args.pixelFormat, (int)args.pitches, 0);
            }));

            this.memoryMappedView = Win32Interop.MapViewOfFile(this.memoryMappedFile, Win32Interop.FileMapAccess.AllAccess, 0, 0, size);
            var viewHandle = this.memoryMappedView;

            userdata = viewHandle;
            return 1;
        }

        /// <summary>
        /// Called by Vlc when it requires a cleanup
        /// </summary>
        /// <param name="userdata">The parameter is not used</param>
        private void CleanupVideo(ref IntPtr userdata)
        {
            // This callback may be called by Dispose in the Dispatcher thread, in which case it deadlocks if we call RemoveVideo again in the same thread.
            if (!disposedValue)
            {
                Application.Current.Dispatcher.Invoke((Action)this.RemoveVideo);
            }
        }

        /// <summary>
        /// Called by libvlc when it wants to acquire a buffer where to write
        /// </summary>
        /// <param name="userdata">The pointer to the buffer (the out parameter of the <see cref="VideoFormat"/> callback)</param>
        /// <param name="planes">The pointer to the planes array. Since only one plane has been allocated, the array has only one value to be allocated.</param>
        /// <returns>The pointer that is passed to the other callbacks as a picture identifier, this is not used</returns>
        private IntPtr LockVideo(IntPtr userdata, IntPtr planes)
        {
            Marshal.WriteIntPtr(planes, userdata);
            return userdata;
        }

        /// <summary>
        /// Called by libvlc when the picture has to be displayed.
        /// </summary>
        /// <param name="userdata">The pointer to the buffer (the out parameter of the <see cref="VideoFormat"/> callback)</param>
        /// <param name="picture">The pointer returned by the <see cref="LockVideo"/> callback. This is not used.</param>
        private void DisplayVideo(IntPtr userdata, IntPtr picture)
        {
            // Invalidates the bitmap
            Application.Current.Dispatcher.BeginInvoke((Action)(() =>
            {
                (this.VideoSource as InteropBitmap)?.Invalidate();
            }));
        }

        /// <summary>
        /// Removes the video (must be called from the Dispatcher thread)
        /// </summary>
        private void RemoveVideo()
        {
            this.VideoSource = null;

            if (this.memoryMappedFile != IntPtr.Zero)
            {
                Win32Interop.UnmapViewOfFile(this.memoryMappedView);
                this.memoryMappedView = IntPtr.Zero;
                Win32Interop.CloseHandle(this.memoryMappedFile);
                this.memoryMappedFile = IntPtr.Zero;
            }
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                this.LibVLC?.Dispose();
                this.LibVLC = null;
                this.MediaPlayer?.Dispose();
                this.MediaPlayer = null;
                Application.Current.Dispatcher.BeginInvoke((Action)this.RemoveVideo);
            }
        }

        ~VideoSourceProvider()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    internal static class Win32Interop
    {
        /// <summary>
        /// Creates or opens a named or unnamed file mapping object for a specified file.
        /// </summary>
        /// <param name="hFile">A handle to the file from which to create a file mapping object.</param>
        /// <param name="lpAttributes">A pointer to a SECURITY_ATTRIBUTES structure that determines whether a returned handle can be inherited by child processes. The lpSecurityDescriptor member of the SECURITY_ATTRIBUTES structure specifies a security descriptor for a new file mapping object.</param>
        /// <param name="flProtect">Specifies the page protection of the file mapping object. All mapped views of the object must be compatible with this protection.</param>
        /// <param name="dwMaximumSizeLow">The high-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="dwMaximumSizeHigh">The low-order DWORD of the maximum size of the file mapping object.</param>
        /// <param name="lpName">The name of the file mapping object.</param>
        /// <returns>The value is a handle to the newly created file mapping object.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, PageAccess flProtect, int dwMaximumSizeLow, int dwMaximumSizeHigh, string lpName);

        /// <summary>
        /// Maps a view of a file mapping into the address space of a calling process.
        /// </summary>
        /// <param name="hFileMappingObject">A handle to a file mapping object. The CreateFileMapping and OpenFileMapping functions return this handle.</param>
        /// <param name="dwDesiredAccess">The type of access to a file mapping object, which determines the protection of the pages. This parameter can be one of the following values.</param>
        /// <param name="dwFileOffsetHigh">A high-order DWORD of the file offset where the view begins.</param>
        /// <param name="dwFileOffsetLow">A low-order DWORD of the file offset where the view is to begin. The combination of the high and low offsets must specify an offset within the file mapping.</param>
        /// <param name="dwNumberOfBytesToMap">The number of bytes of a file mapping to map to the view. All bytes must be within the maximum size specified by CreateFileMapping. If this parameter is 0 (zero), the mapping extends from the specified offset to the end of the file mapping.</param>
        /// <returns>The value is the starting address of the mapped view.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        /// <summary>
        /// Unmaps a mapped view of a file from the calling process's address space.
        /// </summary>
        /// <param name="lpBaseAddress">A pointer to the base address of the mapped view of a file that is to be unmapped. This value must be identical to the value returned by a previous call to the MapViewOfFile or MapViewOfFileEx function.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="handle">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        public enum PageAccess
        {
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            Guard = 0x100,
            NoCache = 0x200,
            WriteCombine = 0x400
        }

        public enum FileMapAccess : uint
        {
            Write = 0x00000002,
            Read = 0x00000004,
            AllAccess = 0x000f001f,
            Copy = 0x00000001,
            Execute = 0x00000020
        }
    }

    internal static class FourCCConverter
    {
        /// <summary>
        /// Converts a 4CC integer to a string
        /// </summary>
        /// <param name="fourCC">The fourcc code</param>
        /// <returns>The string representation of this 4CC</returns>
        public static string FromFourCC(UInt32 fourCC)
        {
            return string.Format(
                "{0}{1}{2}{3}",
                (char)(fourCC & 0xff),
                (char)(fourCC >> 8 & 0xff),
                (char)(fourCC >> 16 & 0xff),
                (char)(fourCC >> 24 & 0xff));
        }

        /// <summary>
        /// Converts a 4CC string representation to its UInt32 equivalent
        /// </summary>
        /// <param name="fourCCString">The 4CC string</param>
        /// <returns>The UInt32 representation of the 4cc</returns>
        public static UInt32 ToFourCC(string fourCCString)
        {
            if (fourCCString.Length != 4)
            {
                throw new ArgumentException("4CC codes must be 4 characters long", nameof(fourCCString));
            }

            var bytes = Encoding.ASCII.GetBytes(fourCCString);
            return (UInt32)bytes[0] &
                   (UInt32)bytes[1] << 8 &
                   (UInt32)bytes[2] << 16 &
                   (UInt32)bytes[3] << 24;
        }

        /// <summary>
        /// Converts a 4CC string representation to its UInt32 equivalent, and write it to the memory
        /// </summary>
        /// <param name="fourCCString">The 4CC string</param>
        /// <param name="destination">The pointer to where to write the bytes</param>
        public static void ToFourCC(string fourCCString, IntPtr destination)
        {
            if (fourCCString.Length != 4)
            {
                throw new ArgumentException("4CC codes must be 4 characters long", nameof(fourCCString));
            }

            var bytes = Encoding.ASCII.GetBytes(fourCCString);

            for (var i = 0; i < 4; i++)
            {
                Marshal.WriteByte(destination, i, bytes[i]);
            }
        }
    }
}
