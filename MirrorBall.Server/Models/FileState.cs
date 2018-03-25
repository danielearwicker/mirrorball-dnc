using System;

namespace MirrorBall.API
{
    public class FileState
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public DateTime Time { get; set; }
        public long Size { get; set; }
    }
}
