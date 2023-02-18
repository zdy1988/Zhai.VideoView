using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Zhai.Famil.Common.Mvvm;

namespace Zhai.VideoView
{
    internal class Video : VideoThumbBase
    {
        public string Name { get; }

        public long Size { get; }

        public long Duration { get; }

        public String VideoPath { get; }

        public Video(string filename)
            : base(filename)
        {
            var file = new FileInfo(filename);

            Name = file.Name;

            Size = file.Length;

            Duration = GetDuration(filename);

            VideoPath = filename;
        }

        private long GetDuration(string filename)
        {
            try
            {
                ShellObject obj = ShellObject.FromParsingName(filename);

                return (long)obj.Properties.System.Media.Duration.Value.Value;
            }
            catch
            {
                return 0;
            }
        }
    }
}
