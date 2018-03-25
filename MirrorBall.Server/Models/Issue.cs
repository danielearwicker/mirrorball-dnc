using System;
using System.Threading.Tasks;

namespace MirrorBall.API
{
    public class Issue
    {
        public IssueInfo Info { get; }
        public Func<string, Action<double, string>, Task> Resolve { get; }

        public Issue(IssueInfo info,
                     Func<string, Action<double, string>, Task> resolve)
        {
            Info = info;
            Resolve = resolve;
        }
    }
}
