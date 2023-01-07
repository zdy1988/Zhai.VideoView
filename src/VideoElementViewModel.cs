using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using Zhai.Famil.Common.Mvvm;
using Zhai.Famil.Common.Mvvm.Command;
using Zhai.Famil.Win32;

namespace Zhai.VideoView
{
    public partial class VideoElementViewModel : ViewModelBase
    {
        public VideoElementViewModel()
        {
            try
            {
                VideoSourceProvider = new VideoSourceProvider();

                VideoSourceProvider.CreatePlayer();

                LoadPlayer();

                Volume = 75;
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

        public Dictionary<string, float> Rates => new Dictionary<string, float>
        {
           {"0.25", 0.25f},
           {"0.5", 0.5f},
           {"0.75", 0.75f},
           {"正常", 1f},
           {"1.25", 1.25f},
           {"1.5", 1.5f},
           {"1.75", 1.75f},
           {"2", 2f},
        };

        #region Properties

        private bool isOpened = false;
        public bool IsOpened
        {
            get => isOpened;
            set => Set(() => IsOpened, ref isOpened, value); 
        }

        private bool isPaused;
        public bool IsPaused
        {
            get => isPaused;
            set => Set(() => IsPaused, ref isPaused, value);
        }

        private bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => Set(() => IsLoading, ref isLoading, value);
        }

        private long length;
        public long Length
        {
            get => length;
            set => Set(() => Length, ref length, value);
        }

        private long time;
        public long Time
        {
            get => time;
            set => Set(() => Time, ref time, value);
        }

        private float position;
        public float Position
        {
            get => position;
            set => Set(() => Position, ref position, value);
        }

        private bool isPositionChanging = false;
        public bool IsPositionChanging
        {
            get => isPositionChanging;
            set => Set(() => IsPositionChanging, ref isPositionChanging, value);
        }

        private bool isMuted;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                if (Set(() => IsMuted, ref isMuted, value))
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
                if (Set(() => Volume, ref volume, value))
                {
                    SetVolume(volume);
                }
            }
        }

        private float rate = 1.0f;
        public float Rate
        {
            get => rate;
            set
            {
                if (Set(() => Rate, ref rate, value))
                {
                    SetRate(rate);
                }
            }
        }

        private bool isLooping;
        public bool IsLooping
        {
            get => isLooping;
            set => Set(() => IsLooping, ref isLooping, value);
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

        public void SetRate(float rate)
        {
            if (IsOpened)
            {
                this.MediaPlayer.SetRate(rate);
            }
        }

        #endregion

        #region Commands

        public RelayCommand<string> ExecuteOpenCommand => new Lazy<RelayCommand<string>>(() => new RelayCommand<string>(uriString =>
        {
            TryOpenMedia(uriString);

        })).Value;

        public RelayCommand ExecutePlayCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            if (IsOpened)
            {
                this.TryPlayMedia();
            }
            else
            {
                var filename = FileDialog.OpenFileDialog("", VideoSupport.Filter, "");

                if (!string.IsNullOrEmpty(filename))
                    TryOpenMedia(filename);
            }

        })).Value;

        public RelayCommand ExecutePauseCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            this.TryPausMedia();

        })).Value;

        public RelayCommand ExecuteStopCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            this.MediaPlayer.Stop();

        })).Value;

        public RelayCommand ExecuteCloseCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {

            ThreadPool.QueueUserWorkItem(_ => this.DisposePlayer());

        })).Value;

        public RelayCommand ExecuteRateUpCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            var rate = MediaPlayer.Rate + 0.5f;

            if (rate <= 5)
            {
                this.Rate = rate;
            }

        })).Value;

        public RelayCommand ExecuteRateDownCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            var rate = MediaPlayer.Rate - 0.5f;

            if (rate >= -5)
            {
                this.Rate = rate;
            }

        })).Value;

        public RelayCommand<float> ExecuteSetRateCommand => new Lazy<RelayCommand<float>>(() => new RelayCommand<float>(rate =>
        {
            this.Rate = rate;

        })).Value;

        #endregion

        #region Events

        public event EventHandler<VideoElementViewModel> MediaOpened;

        protected virtual void OnMediaOpened()
        {
            MediaOpened?.Invoke(this, this);
        }

        #endregion
    }
}
