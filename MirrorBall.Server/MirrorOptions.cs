using System;
namespace MirrorBall.Server
{
    public class MirrorOptions
    {
        public string RootFolder { get; set; }
        public string PeerServer { get; set; }
        public string OurName { get; set; }
        public string PeerName { get; set; }
    }
}
