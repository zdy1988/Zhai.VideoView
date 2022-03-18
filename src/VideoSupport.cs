using System;
using System.IO;
using System.Linq;

namespace Zhai.VideoView
{
    internal static class VideoSupport
    {
        internal static string[] All { get; } = new string[] {

            ".3g2",".3gp",".3gp2",".3gpp",".amv",".asf",".avi",".bik",".bin",".divx",".drc",".dv",".f4v",".flv",".gvi",".gxf",".iso",".m1v",".m2v",".m2t",".m2ts",".m4v",".mkv",".mov",".mp2",".mp4",".mp4v",".mpe",".mpeg",".mpeg1",".mpeg2",".mpeg4",".mpg",".mpv2",".mts",".mxf",".mxg",".nsv",".nuv",".ogg",".ogm",".ogv",".ps",".rec",".rm",".rmvb",".rpl",".thp",".tod",".ts",".tts",".txd",".vob",".vro",".webm",".wm",".wmv",".wtv",".xesc"
        
        };

        internal static string AllSupported = String.Join(";", All.Select(t => $"*{t}"));

        internal static string Filter { get; } =
    $@"All Supported ({AllSupported})|{AllSupported}|
All Files (*.*)|*.*";

        internal static bool IsSupported(string filename) => All.Contains(Path.GetExtension(filename).ToLower());
    }
}
