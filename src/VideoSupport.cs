using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;
using Zhai.Famil.Controls;

namespace Zhai.VideoView
{
    internal static class VideoSupport
    {
        internal static string[] Microsoft { get; } = new string[] {
            ".wmv",".asf",".asx"
        };

        internal static string[] RealPlayer { get; } = new string[] {
            ".rm",".rmvb"
        };

        internal static string[] MPEG { get; } = new string[] {
            ".mp4"
        };

        internal static string[] Phone { get; } = new string[] {
            ".3gp",".3gp2",".3gpp"
        };

        internal static string[] Apple { get; } = new string[] {
            ".m4v", ".mov"
        };

        internal static string[] Others { get; } = new string[] {
            ".3g2",".amv",".avi",".bik",".bin",".divx",".drc",".dv",
            ".f4v",".flv",
            ".gvi",".gxf",".iso",
            ".m1v",".m2v",".m2t",".m2ts",".mkv",".mp2",".mp4",".mp4v",".mpe",".mpeg",".mpeg1",".mpeg2",".mpeg4",".mpg",".mpv2",".mts",".mxf",".mxg",".nsv",".nuv",
            ".ogg",".ogm",".ogv",".ps",".rec",".rpl",".thp",".tod",".ts",".tts",".txd",
            ".vob",".vro",".webm",".wm",".wtv",".xesc"
        };

        internal static string[] All { get; } = (new List<IEnumerable<string>> { Microsoft, RealPlayer, MPEG, Phone, Apple, Others }).Aggregate((x, y) => x.Concat(y)).ToArray();

        internal static string ToFilter(this IEnumerable<string> strings)
        {
            return String.Join(";", strings.Select(t => $"*{t}"));
        }

        internal static string Filter { get; } =
    $@"All Supported ({All.ToFilter()})|{All.ToFilter()}|
MPEG ({MPEG.ToFilter()})|{MPEG.ToFilter()}|
RealPlayer ({RealPlayer.ToFilter()})|{RealPlayer.ToFilter()}|
Microsoft ({Microsoft.ToFilter()})|{Microsoft.ToFilter()}|
Phone ({Phone.ToFilter()})|{Phone.ToFilter()}|
Apple ({Apple.ToFilter()})|{Apple.ToFilter()}|
Others ({Others.ToFilter()})|{Others.ToFilter()}|
All Files (*.*)|*.*";

        internal static bool IsSupported(string filename) => All.Contains(Path.GetExtension(filename).ToLower());
    }
}
