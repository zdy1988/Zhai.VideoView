using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Effects;
using Zhai.Famil.Common.Mvvm;
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
           
        }


        public event EventHandler<Video> CurrentVideoChanged;

        public override void Cleanup()
        {
            base.Cleanup();

            Folder.Cleanup();
        }
    }
}
