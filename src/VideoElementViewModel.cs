using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using Zhai.VideoView.Commands;

namespace Zhai.VideoView
{
    public partial class VideoElementViewModel : BaseViewModel
    {
        public VideoElementViewModel()
        {
            try
            {
                VideoSourceProvider = new VideoSourceProvider();

                VideoSourceProvider.CreatePlayer();

                LoadPlayer();
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine("VideoViewer: Initialize Error ...", ex);
#endif
            }
        }

        public VideoSourceProvider VideoSourceProvider { get; private set; }

        public LibVLC LibVLC => VideoSourceProvider.LibVLC;

        public MediaPlayer MediaPlayer => VideoSourceProvider.MediaPlayer;

        public Media CurrentMedia { get; private set; }


        #region Properties

        private bool isOpened = false;
        public bool IsOpened
        {
            get => isOpened;
            set => SetProperty(ref isOpened, value);
        }

        private bool isPaused;
        public bool IsPaused
        {
            get => isPaused;
            set => SetProperty(ref isPaused, value);
        }

        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        private long length;
        public long Length
        {
            get => length;
            set => SetProperty(ref length, value);
        }

        private long time;
        public long Time
        {
            get => time;
            set => SetProperty(ref time, value);
        }

        private float position;
        public float Position
        {
            get => position;
            set => SetProperty(ref position, value);
        }

        private bool isPositionChanging = false;
        public bool IsPositionChanging
        {
            get => isPositionChanging;
            set => SetProperty(ref isPositionChanging, value);
        }

        private bool isMuted;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                if (SetProperty(ref isMuted, value))
                {
                    SetMuted(isMuted);
                }
            }
        }

        private int volume;
        public int Volume
        {
            get => volume;
            set
            {
                if (SetProperty(ref volume, value))
                {
                    SetVolume(volume);
                }
            }
        }

        private bool isLooping;
        public bool IsLooping
        {
            get => isLooping;
            set => SetProperty(ref isLooping, value);
        }

        #endregion

        #region MediaPlayer EventHandlers

        public void RegisteredEvents(LibVLC libVLC, MediaPlayer mediaPlayer)
        {
            if (mediaPlayer != null)
            {
                libVLC.Log += LibVLC_Log;
                mediaPlayer.EndReached += MediaPlayer_EndReached;
                mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
                mediaPlayer.Opening += MediaPlayer_Opening;
                mediaPlayer.Playing += MediaPlayer_Playing;
                mediaPlayer.Paused += MediaPlayer_Paused;
                mediaPlayer.Stopped += MediaPlayer_Stopped;
                mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
                mediaPlayer.TimeChanged += MediaPlayer_TimeChanged;
                mediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
            }
        }

        public void UnRegisteredEvents(LibVLC libVLC, MediaPlayer mediaPlayer)
        {
            if (mediaPlayer != null)
            {
                libVLC.Log -= LibVLC_Log;
                mediaPlayer.EndReached -= MediaPlayer_EndReached;
                mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                mediaPlayer.Opening -= MediaPlayer_Opening;
                mediaPlayer.Playing -= MediaPlayer_Playing;
                mediaPlayer.Paused -= MediaPlayer_Paused;
                mediaPlayer.Stopped -= MediaPlayer_Stopped;
                mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
                mediaPlayer.TimeChanged -= MediaPlayer_TimeChanged;
                mediaPlayer.PositionChanged -= MediaPlayer_PositionChanged;
            }
        }

        private void MediaPlayer_Opening(object sender, EventArgs e)
        {
            //this.MediaPlayer.SetVideoTitleDisplay(Vlc.DotNet.Core.Interops.Signatures.Position.TopLeft, 5000);

            this.IsOpened = true;

            this.IsLoading = false;

            this.CurrentMedia = this.MediaPlayer.Media;

            this.OnMediaOpened();
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            this.IsPaused = false;
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            this.IsPaused = true;
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {

        }

        private void MediaPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            this.Time = e.Time;
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            this.Length = e.Length;
        }

        private void MediaPlayer_PositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            if (!this.IsPositionChanging)
            {
                this.Position = e.Position;
            }
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            this.ResetPlayer();

            if (this.IsLooping && this.CurrentMedia != null)
            {
                ThreadPool.QueueUserWorkItem(_ => this.MediaPlayer.Play(this.CurrentMedia));
            }
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
#if DEBUG
            Debug.WriteLine("libVlc : Media Encountered Error !");
#endif
        }

        private void LibVLC_Log(object sender, LogEventArgs e)
        {
#if DEBUG
            Debug.WriteLine($"libVlc : {e.Level} {e.Message} @ {e.Module}");
#endif
        }

        #endregion

        #region Methods

        public void LoadPlayer()
        {
            this.RegisteredEvents(this.LibVLC, this.MediaPlayer);

            this.ResetPlayer();

            this.Volume = this.MediaPlayer.Volume;
        }

        public void DisposePlayer()
        {
            this.IsOpened = false;

            this.UnRegisteredEvents(this.LibVLC, this.MediaPlayer);

            this.VideoSourceProvider?.Dispose();
        }

        public void ResetPlayer()
        {
            this.IsOpened = false;
            this.IsPaused = true;
            this.Time = 0;
            this.Length = 0;
            this.Position = 0;

            this.MediaPlayer.SetRate(1);
        }

        public bool TryOpenMedia(string uriString)
        {
            if (this.IsLoading || string.IsNullOrWhiteSpace(uriString) || !VideoSupport.IsSupported(uriString))
                return false;

            this.IsLoading = true;

            try
            {
                var media = new Media(this.LibVLC, new Uri(uriString));

                this.MediaPlayer.Play(media);

                return true;
            }
            catch (Exception ex)
            {
#if DEBUG
                Debug.WriteLine($"libVlc : Media Open Failed: {ex.GetType()}\r\n{ex.Message}", ex);
#endif

                return false;
            }
        }

        public bool TryPlayMedia()
        {
            if (this.MediaPlayer == null) return false;

            if (IsOpened && IsPaused)
            {
                this.MediaPlayer.Play();

                return true;
            }

            if (!IsOpened && this.CurrentMedia != null)
            {
                this.MediaPlayer.Play(this.CurrentMedia);

                return true;
            }

            return false;
        }

        public bool TryPausMedia()
        {
            if (IsOpened && this.MediaPlayer.IsPlaying)
            {
                this.MediaPlayer.Pause();

                this.MediaPlayer.SetRate(1);

                return true;
            }

            return false;
        }

        public void ToggledPlayMedia()
        {
            if (IsOpened)
            {
                if (this.MediaPlayer.IsPlaying)
                {
                    this.MediaPlayer.Pause();
                }
                else if (IsPaused)
                {
                    this.MediaPlayer.Play();
                }
            }
        }

        public void SetMuted(bool isMuted)
        {
            if (IsOpened)
            {
                this.MediaPlayer.Mute = isMuted;
            }
        }

        public void SetVolume(int volume)
        {
            if (IsOpened)
            {
                this.MediaPlayer.Volume = volume;
            }
        }

        public void SetPosition(float position)
        {
            if (IsOpened)
            {
                this.MediaPlayer.Position = position;
            }
        }

        #endregion

        #region Commands

        public RelayCommand<string> ExecuteOpenCommand => new Lazy<RelayCommand<string>>(() => new RelayCommand<string>(uriString =>
        {
            TryOpenMedia(uriString);

        })).Value;

        public RelayCommand ExecutePlayCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {
            if (IsOpened)
            {
                this.TryPlayMedia();
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = VideoSupport.Filter
                };

                if (dialog.ShowDialog() is true)
                    TryOpenMedia(dialog.FileName);
            }

        })).Value;

        public RelayCommand ExecutePauseCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {
            this.TryPausMedia();

        })).Value;

        public RelayCommand ExecuteStopCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {
            this.MediaPlayer.Stop();

        })).Value;

        public RelayCommand ExecuteCloseCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {

            ThreadPool.QueueUserWorkItem(_ => this.DisposePlayer());

        })).Value;

        public RelayCommand ExecuteForwardCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {
            var rate = MediaPlayer.Rate + 0.5f;

            if (rate <= 5)
            {
                this.MediaPlayer.SetRate(rate);
            }

        })).Value;

        public RelayCommand ExecuteRewindCommand => new Lazy<RelayCommand>(() => new RelayCommand(o =>
        {
            var rate = MediaPlayer.Rate - 0.5f;

            if (rate >= -5)
            {
                this.MediaPlayer.SetRate(rate);
            }

        })).Value;

        #endregion

        #region Events

        public event EventHandler<VideoElementViewModel> MediaOpened;

        protected virtual void OnMediaOpened()
        {
            MediaOpened?.Invoke(this, this);
        }

        #endregion

        public override void Clean()
        {
            throw new NotImplementedException();
        }
    }
}
