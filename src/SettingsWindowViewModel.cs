using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Zhai.Famil.Common.Mvvm;
using Zhai.VideoView.NativeMethods;

namespace Zhai.VideoView
{
    internal class SettingsWindowViewModel : ViewModelBase
    {
        private bool isStartWindowMaximized = Properties.Settings.Default.IsStartWindowMaximized;
        public bool IsStartWindowMaximized
        {
            get => isStartWindowMaximized;
            set
            {
                if (Set(() => IsStartWindowMaximized, ref isStartWindowMaximized, value))
                {
                    Properties.Settings.Default.IsStartWindowMaximized = value;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public List<VideoSupportedItem> AllSupported { get; }

        public SettingsWindowViewModel()
        {
            AllSupported = new List<VideoSupportedItem>(VideoSupport.All.Select(t => new VideoSupportedItem(t)));
        }
    }

    internal class VideoSupportedItem : ViewModelBase
    {
        private string ext;
        public string Ext { get => ext; }

        private bool isSupported;
        public bool IsSupported
        {
            get => isSupported;
            set
            {
                if (Set(() => IsSupported, ref isSupported, value))
                {
                    SetAssociation(value);
                }
            }
        }

        public VideoSupportedItem(string ext)
        {
            this.ext = ext;
          
            this.isSupported = FileAssociator.CreateInstance(ext).Exists;
        }

        public void SetAssociation(bool isSet)
        {
            var appPath = Environment.ProcessPath;

            if (isSet)
            {
                FileAssociator.CreateInstance(ext).Create(Properties.Settings.Default.AppName, null, null, new ExecApplication(appPath), null);
            }
            else
            {
                FileAssociator.CreateInstance(ext).Delete();
            }
        }
    }
}
