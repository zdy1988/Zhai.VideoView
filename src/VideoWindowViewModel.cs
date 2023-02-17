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
using Zhai.Famil.Common.Threads;
using Zhai.Famil.Controls;
using Zhai.PictureView;

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


        public VideoWindowViewModel()
        {
            InitPlayer();
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

                IsShowVideoListView = Folder != null && Folder.Count > 1;
            }
            else
            {
                var box = new Famil.Dialogs.MessageBox(App.Current.MainWindow as WindowBase, ($"软件对路径：“{dir.FullName}”没有访问权限！"));
                box.Show();
            }
        }

        public Task OpenVideo(string filename)
            => OpenVideo(Directory.GetParent(filename), filename);

        #endregion


        public event EventHandler<Video> CurrentVideoChanged;

        public override void Cleanup()
        {
            base.Cleanup();

            Folder.Cleanup();
        }
    }
}
