using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MirrorBall.API
{
	public enum DiffType
	{
		LeftOnly, RightOnly, Renamed, Modified
	}

	public class Diff
	{
		public DiffType Type { get; set; }
		public string Left { get; set; }
		public string Right { get; set; }
	}

	public class FileState
	{
		public string Path { get; set; }
        public string Hash { get; set; }
		public DateTime Time { get; set; }
        public long Size { get; set; }
	}

    public enum IssueState
    {
        New,
        Queued,
        Busy,
        Failed
    }

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

    public class IssueResolution
    {
        public int Id { get; set; }
        public string Choice { get; set; }
    }

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

    public static class Operations
    {
        public static int SampleSize = 1024 * 1024;

        public static string GetFileHash(byte[] buffer, string filePath)
        {
            byte[] hash;

            using (var hasher = System.Security.Cryptography.SHA512.Create())
            using (var file = File.OpenRead(filePath))
            {
                hasher.Initialize();
                if (file.Length < SampleSize)
                {
                    var got = file.Read(buffer, 0, (int)file.Length);
                    if (got != file.Length)
                    {
                        throw new Exception("Oi");
                    }
                    hash = hasher.ComputeHash(buffer, 0, got);
                }
                else
                {
                    var halfSize = SampleSize / 2;

                    file.Seek(0, SeekOrigin.Begin);

					var got = file.Read(buffer, 0, halfSize);
					if (got != halfSize)
					{
						throw new Exception($"Oi {halfSize} {got}");
					}

                    file.Seek(-halfSize, SeekOrigin.End);
                    if (file.Position != file.Length - halfSize)
                    {
                        throw new Exception($"Bleh {file.Position} {file.Length} {halfSize}");
                    }

                    got = file.Read(buffer, halfSize, halfSize);
                    if (got != halfSize)
                    {
                        throw new Exception($"Oi {halfSize} {got}");
                    }
                    hash = hasher.ComputeHash(buffer);
                }
            }

            return Convert.ToBase64String(hash);
        }

        public static bool IsNonHidden(string filePath)
        {
            return !Path.GetFileName(filePath).StartsWith(".");
        }

        public static IEnumerable<string> GetVisibleFiles(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path).Where(IsNonHidden))
            {
                yield return file;
            }

            foreach (var folder in Directory.EnumerateDirectories(path).Where(IsNonHidden))
            {
                foreach (var file in GetVisibleFiles(folder))
                {
                    yield return file;
                }
            }
        }

        public static List<FileState> GetStates(string folderPath, Action<double, string> progress)
        {
            Console.WriteLine($"Loading states {folderPath}");

            var stateFilePath = Path.Combine(folderPath, ".mirrorballdnc");

            List<FileState> states = null;

            if (File.Exists(stateFilePath))
            {
                try
                {
                    states = JsonConvert.DeserializeObject<List<FileState>>(
                       File.ReadAllText(stateFilePath));
                }
                catch (Exception) { }
            }

            if (states == null)
            {
                Console.WriteLine("No existing states file");
                states = new List<FileState>();
            }

            var statesByPath = states.ToDictionary(s => s.Path);

            var buffer = new byte[SampleSize];
            var filePaths = new HashSet<string>(GetVisibleFiles(folderPath));
            var newStates = new List<FileState>();

            Console.WriteLine($"Scanning {filePaths.Count} files");

            var count = 0;

            foreach (var filePath in filePaths)
            {
                var subPath = filePath.Substring(folderPath.Length).Trim(Path.DirectorySeparatorChar);

				count++;
				progress((double)count / filePaths.Count, subPath);

				var fileInfo = new FileInfo(filePath);

                FileState fileState;
                if (!statesByPath.TryGetValue(subPath, out fileState) ||
                    fileState.Time != fileInfo.LastWriteTimeUtc ||
                    fileState.Size != fileInfo.Length)
                {
                    fileState = new FileState
                    {
                        Path = subPath,
                        Time = fileInfo.LastWriteTimeUtc,
                        Size = fileInfo.Length,
                        Hash = GetFileHash(buffer, filePath)
                    };
                }

                newStates.Add(fileState);            
            }

            File.WriteAllText(stateFilePath,
                JsonConvert.SerializeObject(newStates, Formatting.Indented));

            Console.WriteLine($"Finished loading states {folderPath}");

            return newStates;
        }

        public static List<List<FileState>> FindDuplicates(List<FileState> files)
        {            
            return files.GroupBy(l => l.Hash).Where(g => g.Count() > 1).Select(g => g.ToList()).ToList();
        }

        public static List<Diff> Compare(List<FileState> left, List<FileState> right)
        {
            var leftByHash = left.ToDictionary(l => l.Hash);
            var rightByHash = right.ToDictionary(l => l.Hash);
            var diffs = new List<Diff>();

            foreach (var leftState in left)
            {
                FileState rightState;
                if (!rightByHash.TryGetValue(leftState.Hash, out rightState))
                {
                    diffs.Add(new Diff 
                    { 
                        Type = DiffType.LeftOnly, 
                        Left = leftState.Path 
                    });
                }
                else if (rightState.Path != leftState.Path)
                {
                    diffs.Add(new Diff
                    {
                        Type = DiffType.Renamed,
                        Left = leftState.Path,
                        Right = rightState.Path,
                    });
                }
            }

            foreach (var rightState in right)
            {
				FileState leftState;
				if (!leftByHash.TryGetValue(rightState.Hash, out leftState))
				{
					diffs.Add(new Diff
					{
						Type = DiffType.RightOnly,
						Right = rightState.Path
					});
				}
            }

            var leftOnlyByPaths = diffs.Where(d => d.Type == DiffType.LeftOnly)
                                       .ToDictionary(d => d.Left);

            foreach (var rightOnly in diffs.Where(d => d.Type == DiffType.RightOnly).ToList())
            {
                Diff leftOnly;
                if (leftOnlyByPaths.TryGetValue(rightOnly.Right, out leftOnly))
                {
                    diffs.Remove(rightOnly);
                    leftOnly.Right = leftOnly.Left;
                    leftOnly.Type = DiffType.Modified;
                }
            }

            return diffs;
        }

        private static List<Issue> _issues = new List<Issue>();
        private static int _nextIssueId;
        private static Task _issueWorker;

        private static bool SimilarIssues(IssueInfo issue1, IssueInfo issue2)
        {
            return issue1.Title == issue2.Title &&
                   issue1.Message == issue2.Message &&
                   issue1.Options.OrderBy(o => o).SequenceEqual(issue2.Options.OrderBy(o => o));
        }

        public static void AddIssue(Issue issue)
        {
            issue.Info.Id = _nextIssueId++;

            if (issue.Info.State != IssueState.Queued)
            {
                issue.Info.State = IssueState.New;
            }

            lock (_issues)
            {
                if (!_issues.Any(i => SimilarIssues(issue.Info, i.Info)))
                {
                    _issues.Add(issue);
                }

				if (_issueWorker == null)
				{
					_issueWorker = Task.Run(IssueWorker);
				}
            }
        }

        public static List<IssueInfo> GetIssues()
        {
            lock (_issues)
            {
                return _issues.Select(i => new IssueInfo
                {
                    Id = i.Info.Id,
                    Title = i.Info.Title,
                    Options = i.Info.Options,
                    State = i.Info.State,
                    Progress = i.Info.Progress,
                    ProgressText = i.Info.ProgressText,
                    Message = i.Info.Message,
                    Choice = i.Info.Choice
                              
                }).ToList();
            }
        }

        private static async Task IssueWorker()
        {
            for (;;)
            {
                await Task.Delay(500);

                Issue busy = null;

                lock (_issues)
                {
                    busy = _issues.FirstOrDefault(i => i.Info.State == IssueState.Queued);
                    if (busy != null)
                    {
                        busy.Info.State = IssueState.Busy;
                    }
                }

                if (busy != null)
                {                    
                    Console.WriteLine($"{busy.Info.Id}: {busy.Info.Title} - {busy.Info.Message}");
                    Console.WriteLine($"{busy.Info.Id}: choice = {busy.Info.Choice}");
                    try
                    {
                        await busy.Resolve(busy.Info.Choice, (progress, text) =>
                        {
                            lock (_issues)
                            {
                                busy.Info.Progress = progress;
                                busy.Info.ProgressText = text;
                            }
                        });

                        lock (_issues)
                        {
                            _issues.Remove(busy);
                        }

                        Console.WriteLine($"{busy.Info.Id}: completed and removed");
                    }
                    catch (Exception x)
                    {
                        x = x.GetBaseException();

                        Console.WriteLine($"{ busy.Info.Id}: issue resolution failed");
                        Console.WriteLine(x.Message);
                        Console.WriteLine(x.StackTrace);

                        lock (_issues)
                        {
                            busy.Info.State = IssueState.Failed;
                            busy.Info.Message = x.Message;
                        }
                    }
                }
            }
        }

        public static void ResolveIssue(IssueResolution resolution)
        {
            lock (_issues)
            {
                var issue = _issues.FirstOrDefault(i => i.Info.Id == resolution.Id);
                if (issue == null)
                {
                    var validIds = string.Join(", ", _issues.Select(i => i.Info.Id));
                    Console.WriteLine($"No issue with ID {resolution.Id}, available issues are: {validIds}");
                }
                else if (issue.Info.State == IssueState.New)
                {
                    issue.Info.Choice = resolution.Choice;
                    issue.Info.State = IssueState.Queued;
                    Console.WriteLine($"Issue {resolution.Id} now queued with choice {resolution.Choice}");
                }
                else if (issue.Info.State == IssueState.Failed)
                {
                    _issues.Remove(issue);
                    Console.WriteLine($"Failed issue {resolution.Id} removed");
                }
                else
                {
                    Console.WriteLine($"Cannot resolve issue {resolution.Id} - state is {issue.Info.State}");
                }
            }
        }
    }
}
