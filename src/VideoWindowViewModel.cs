using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using Zhai.Famil.Common.Mvvm;
using Zhai.Famil.Common.Mvvm.Command;
using Zhai.Famil.Common.Threads;
using Zhai.Famil.Controls;
using Zhai.PictureView;
using static System.Windows.Forms.Design.AxImporter;

namespace Zhai.VideoView
{
    internal partial class VideoWindowViewModel : ViewModelBase
    {
        private Folder folder;
        public Folder Folder
        {
            get => folder;
            set => Set(() => Folder, ref folder, value);
        }

        private Video currentVideo;
        public Video CurrentVideo
        {
            get => currentVideo;
            set
            {
                if (Set(() => CurrentVideo, ref currentVideo, value))
                {
                    CurrentVideoChanged?.Invoke(this, value);
                }
            }
        }

        private int currentVideoIndex;
        public int CurrentVideoIndex
        {
            get => currentVideoIndex;
            set => Set(() => CurrentVideoIndex, ref currentVideoIndex, value);
        }

        private bool isVideoCountMoreThanOne = false;
        public bool IsVideoCountMoreThanOne
        {
            get => isVideoCountMoreThanOne;
            set => Set(() => IsVideoCountMoreThanOne, ref isVideoCountMoreThanOne, value);
        }

        private bool isShowVideoListView = false;
        public bool IsShowVideoListView
        {
            get => isShowVideoListView;
            set => Set(() => IsShowVideoListView, ref isShowVideoListView, value);
        }

        private bool isShowVideoHistoryView = false;
        public bool IsShowVideoHistoryView
        {
            get => isShowVideoHistoryView;
            set => Set(() => IsShowVideoHistoryView, ref isShowVideoHistoryView, value);
        }

        private bool isShowFolderBorthersView = false;
        public bool IsShowFolderBorthersView
        {
            get => isShowFolderBorthersView;
            set => Set(() => IsShowFolderBorthersView, ref isShowFolderBorthersView, value);
        }

        private DirectoryInfo currentFolder;
        public DirectoryInfo CurrentFolder
        {
            get => currentFolder;
            set
            {
                if (Set(() => CurrentFolder, ref currentFolder, value))
                {
                    if (value != null)
                    {
                        var file = value.EnumerateFiles().Where(VideoSupport.VideoSupportExpression).FirstOrDefault();

                        if (file != null)
                        {
                            OpenVideo(value, file.FullName, folder.Borthers).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private ConcurrentObservableCollection<VideoSeen> videoSeens;
        public ConcurrentObservableCollection<VideoSeen> VideoSeens
        {
            get => videoSeens;
            set => Set(() => VideoSeens, ref videoSeens, value);
        }

        public VideoWindowViewModel()
        {
            InitPlayer();

            LoadVideoHistory();
        }

        #region Methods

        public async Task OpenVideo(DirectoryInfo dir, string filename = null, List<DirectoryInfo> borthers = null)
        {
            var newFolder = new Folder(dir, borthers);

            if (newFolder.IsAccessed)
            {
                var oldFolder = Folder;

                Folder = newFolder;
                await Folder.LoadAsync();
                IsVideoCountMoreThanOne = Folder?.Count > 1;
                CurrentVideo = (filename == null ? Folder : Folder.Where(t => t.VideoPath == filename)).FirstOrDefault();

                ThreadPool.QueueUserWorkItem(_ => ApplicationDispatcher.InvokeOnUIThread(() => oldFolder?.Cleanup()));
            }
            else
            {
                var box = new Famil.Dialogs.MessageBox(App.Current.MainWindow as WindowBase, ($"软件对路径：“{dir.FullName}”没有访问权限！"));
                box.Show();
            }
        }

        public Task OpenVideo(string filename)
            => OpenVideo(Directory.GetParent(filename), filename);

        public void LoadVideoHistory()
        {
            try
            {
                var list = System.Text.Json.JsonSerializer.Deserialize<List<VideoSeen>>(Properties.Settings.Default.VideoHistory);

                if (list != null)
                {
                    VideoSeens = new ConcurrentObservableCollection<VideoSeen>(list);

                    return;
                }
            }
            catch
            {

            }

            VideoSeens = new ConcurrentObservableCollection<VideoSeen>();
        }

        public void AddVideoHistory(string filename)
        {
            if (VideoSeens.Any() && VideoSeens.First().Path == filename)
            {

            }
            else
            {
                var seen = new VideoSeen { Path = filename, Name = Path.GetFileNameWithoutExtension(filename), Date = DateTime.Now };
                VideoSeens.Insert(0, seen);
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Properties.Settings.Default.VideoHistory = System.Text.Json.JsonSerializer.Serialize(VideoSeens.Take(20).ToList());
                Properties.Settings.Default.Save();
            });
        }

        #endregion

        #region Commands

        public RelayCommand ExecuteOpenCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = VideoSupport.Filter
            };

            if (dialog.ShowDialog() is true)
                OpenVideo(dialog.FileName);

        })).Value;

        public RelayCommand ExecutePlayCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            if (IsOpened || CurrentVideo != null)
            {
                this.TryPlayVideo();
            }
            else
            {
                ExecuteOpenCommand.Execute(null);
            }

        })).Value;

        public RelayCommand ExecutePauseCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            this.TryPausVideo();

        }, () => CurrentVideo != null)).Value;

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

        }, () => CurrentVideo != null)).Value;

        public RelayCommand ExecuteRateDownCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            var rate = MediaPlayer.Rate - 0.5f;

            if (rate >= -5)
            {
                this.Rate = rate;
            }

        }, () => CurrentVideo != null)).Value;

        public RelayCommand<float> ExecuteSetRateCommand => new Lazy<RelayCommand<float>>(() => new RelayCommand<float>(rate =>
        {
            this.Rate = rate;

        }, rate => CurrentVideo != null)).Value;

        public RelayCommand ExecuteNextCommand => new Lazy<RelayCommand>(() => new RelayCommand(async () =>
        {
            if (Folder == null || !Folder.Any()) return;

            var index = CurrentVideoIndex + 1;

            if (index <= Folder.Count - 1)
            {
                CurrentVideoIndex = index;
            }
            else
            {
                var canNextFolder = Folder.GetNext(out DirectoryInfo next);

                if (canNextFolder)
                {
                    var navWindow = new NavWindow("Next", next)
                    {
                        Owner = App.Current.MainWindow,
                        DataContext = Folder.Current
                    };

                    if (navWindow.ShowDialog() == true)
                    {
                        await OpenVideo(next, null, Folder.Borthers);

                        return;
                    }
                }

                CurrentVideoIndex = 0;
            }

        }, () => Folder != null && Folder.Any() && IsVideoCountMoreThanOne)).Value;

        public RelayCommand ExecutePrevCommand => new Lazy<RelayCommand>(() => new RelayCommand(async () =>
        {
            if (Folder == null || !Folder.Any()) return;

            var index = CurrentVideoIndex - 1;

            if (index >= 0)
            {
                CurrentVideoIndex = index;
            }
            else
            {
                var canPrevFolder = Folder.GetPrev(out DirectoryInfo prev);

                if (canPrevFolder)
                {
                    var navWindow = new NavWindow("Prev", prev)
                    {
                        Owner = App.Current.MainWindow,
                        DataContext = Folder.Current
                    };

                    if (navWindow.ShowDialog() == true)
                    {
                        await OpenVideo(prev, null, Folder.Borthers);

                        return;
                    }
                }

                CurrentVideoIndex = Folder.Count - 1;
            }

        }, () => Folder != null && Folder.Any() && IsVideoCountMoreThanOne)).Value;

        public RelayCommand<VideoSeen> ExecutePlaySeenVideoCommand => new Lazy<RelayCommand<VideoSeen>>(() => new RelayCommand<VideoSeen>(async seen =>
        {
            if (seen != null && !string.IsNullOrWhiteSpace(seen.Path) && File.Exists(seen.Path))
            {
                if (Folder != null && Path.GetDirectoryName(seen.Path) == Folder.Current.FullName)
                {
                    var video = Folder.Where(t => t.VideoPath == seen.Path).FirstOrDefault();

                    if (video != null)
                    {
                        CurrentVideo = video;

                        return;
                    }
                }

                await OpenVideo(seen.Path);
            }
            else
            {
                SendNotificationMessage("播放失败！文件已丢失...");
            }

        }, seen => VideoSeens != null && VideoSeens.Any() && File.Exists(seen.Path))).Value;

        public RelayCommand ExecuteToggleVideoHistoryViewCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            IsShowVideoHistoryView = !IsShowVideoHistoryView;

        }, () => CurrentVideo != null)).Value;

        public RelayCommand ExecuteToggleFolderBorthersViewCommand => new Lazy<RelayCommand>(() => new RelayCommand(() =>
        {
            IsShowFolderBorthersView = !IsShowFolderBorthersView;

        }, () => CurrentVideo != null)).Value;

        #endregion

        public event EventHandler<Video> CurrentVideoChanged;

        public override void Cleanup()
        {
            base.Cleanup();

            Folder.Cleanup();
        }
    }
}
