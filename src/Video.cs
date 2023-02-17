using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zhai.Famil.Common.Mvvm;

namespace Zhai.VideoView
{
    internal class Video : ViewModelBase
    {
        public string Name { get; }

        public long Size { get; }

        public String VideoPath { get; }

        public Video(string filename)
        {
            var file = new FileInfo(filename);

            Name = file.Name;

            Size = file.Length;

            VideoPath = filename;
        }
    }
}
