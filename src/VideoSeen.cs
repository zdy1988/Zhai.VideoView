using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zhai.VideoView
{
    internal class VideoSeen
    {
        public string Name { get; set; }

        public string Path { get; set; }

        public DateTime Date { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public Video Video
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Path))
                    return new Video(Path);
                else
                    return null;
            }
        }
    }
}
