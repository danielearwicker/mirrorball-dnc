namespace MirrorBall.API
{
    public class IssueInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string[] Options { get; set; }
        public IssueState State { get; set; }
        public double Progress { get; set; }
        public string ProgressText { get; set; }
        public string Choice { get; set; }
    }
}
