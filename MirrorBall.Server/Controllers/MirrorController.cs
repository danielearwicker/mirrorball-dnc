﻿using System;
using System.IO;
using System.Collections.Generic;
using MirrorBall.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace MirrorBall.Server.Controllers
{

    [Route("api/[controller]")]
    public class MirrorController : Controller
    {
        private readonly MirrorOptions _options;

        public MirrorController(IOptions<MirrorOptions> accessor)
        {
            _options = accessor.Value;
        }

        private void AddDuplicates(List<List<FileState>> duplicates, bool remote)
        {
            foreach (var group in duplicates)
            {
                var options = group.Select(i => i.Path).ToArray();
                Operations.AddIssue(new Issue(new IssueInfo
                {
                    Title = "Duplicates",
                    Message = $"Which location is preferred? Others will be deleted",
                    Options = options
                },
                async (choice, progress) =>
                {
                    foreach (var option in options.Where(o => o != choice))
                    {
                        await PerformDelete(remote, option);
                    }
                }));
            }
        }

        private static readonly HttpClient Http = new HttpClient();

        private const int ChunkSize = 0x100000;
        private const string OctetStream = "application/octet-stream";
        private static readonly string[] Units = new[] { "bytes", "KB", "MB", "GB", "TB", "PB" };

        class CopyProgress
        {
            private Stopwatch _timer = new Stopwatch();
            private long _size;

            public CopyProgress(long size)
            {
                _timer.Start();
                _size = size;
            }

            public string Message(long position)
            {
                var rate = FormatFileSize(position / _timer.Elapsed.TotalSeconds);
                return $"{rate}/second, {FormatFileSize(position)} of {FormatFileSize(_size)}";
            }

            private static string FormatFileSize(double size)
            {
                var unit = 0;
                while (size > 1024)
                {
                    unit++;
                    size /= 1024;
                }
                return size.ToString("0.##") + " " + Units[unit];
            }
        }

        private async Task PerformCopy(bool remote, string path, Action<double, string> progress)
        {
            if (!remote)
            {
                Console.WriteLine($"Pushing local {path} to peer");

                using (var stream = new FileStream(GetFullPath(path), FileMode.Open))
                {
                    var length = stream.Length;
                    var position = 0L;

                    var buffer = new byte[ChunkSize];
                    var timer = new CopyProgress(length);

                    int got;
                    while ((got = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        var mode = position == 0 ? "truncate" : "append";

                        var url = $"{_options.PeerServer}api/mirror/{mode}/{path}";

                        var content = new ByteArrayContent(buffer, 0, got);
                        content.Headers.ContentType = new MediaTypeHeaderValue(OctetStream);

                        var response = await Operations.Retry(50, () => Http.PutAsync(url, content));
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"{response.StatusCode} - {response.ReasonPhrase} [{url}]");
                        }

                        position += got;

                        progress((double)position / length, timer.Message(position));
                    }
                }
            }
            else
            {
                Console.WriteLine($"Pulling remote {path} from peer");

                var length = long.Parse(
                    await Operations.Retry(50, () => 
                        Http.GetStringAsync($"{_options.PeerServer}api/mirror/length/{path}")
                    )
                );
                    
                Console.WriteLine($"Remote {path} is {length} bytes");

                var position = 0L;

                CreateParentFolders(path);

                using (var stream = new FileStream(GetFullPath(path), FileMode.Create))
                {
                    var timer = new CopyProgress(length);

                    while (position < length)
                    {
                        var count = Math.Min(length, position + ChunkSize) - position;

                        var url = $"{_options.PeerServer}api/mirror/pull/{position}/{count}/{path}";

                        var content = await Operations.Retry(50, () => Http.GetByteArrayAsync(url));

                        stream.Write(content, 0, content.Length);

                        position += count;

                        progress((double)position / length, timer.Message(position));
                    }
                }
            }

            Console.WriteLine("Finished file copy");
        }

        private async Task PerformDelete(bool remote, string path)
        {
            if (!remote)
            {
                Delete(path);
            }
            else
            {
                await Http.DeleteAsync($"{_options.PeerServer}api/mirror/delete/{path}");
            }
		}

        private async Task PerformRename(bool remote, string oldPath, string newPath)
        {
            var op = new RenameOperation
            {
                OldName = oldPath,
                NewName = newPath
            };

			if (!remote)
			{
                Rename(op);
			}
			else
			{                
                await Http.PostAsync(
                    $"{_options.PeerServer}api/mirror/rename",
                    new StringContent(
                        JsonConvert.SerializeObject(op),
                        Encoding.UTF8,
                        "application/json"));
            }
        }

        private void AddExtra(bool remote, string path)
        {
            var location = remote ? _options.PeerName : _options.OurName;

            Operations.AddIssue(new Issue(new IssueInfo
            {
                Title = "Extra file",
                Message = $"Only on {location} - {path}",
                Options = new[] { "Copy", "Delete" }
            },
            (choice, progress) =>
            {
                return choice == "Copy"
                    ? PerformCopy(remote, path, progress)
                    : PerformDelete(remote, path);
            }));
        }

        private void AddRename(string leftPath, string rightPath)
        {
            Operations.AddIssue(new Issue(new IssueInfo
            {
                Title = "Different names/locations",
                Message = "Select the name to use",
                Options = new[] { leftPath, rightPath }
            },
            (choice, progress) =>
            {
                return choice == leftPath
                    ? PerformRename(true, rightPath, leftPath)
                    : PerformRename(false, leftPath, rightPath);
            }));
        }

        private void AddReplace(string path)
        {
            Operations.AddIssue(new Issue(new IssueInfo
            {
                Title = "Different contents at the same path",
                Message = $"Which version of {path}",
                Options = new[] { _options.PeerName, _options.OurName }
            },
            (choice, progress) =>
            {
                return choice == _options.PeerName
                    ? PerformCopy(true, path, progress)
                    : PerformCopy(false, path, progress);
            }));
        }

        private async Task Diff(Action<double, string> progress)
        {
            var rightTask = Http.GetStringAsync($"{_options.PeerServer}api/mirror/states");

			var left = Operations.GetStates(_options.RootFolder, progress);
			var right = JsonConvert.DeserializeObject<List<FileState>>(await rightTask);

            var leftDuplicates = Operations.FindDuplicates(left);
            var rightDuplicates = Operations.FindDuplicates(right);

            Operations.ClearNonBusy();

            AddDuplicates(leftDuplicates, false);
            AddDuplicates(rightDuplicates, true);

            if (leftDuplicates.Count == 0 && rightDuplicates.Count == 0)
            {
                foreach (var diff in Operations.Compare(left, right))
                {
                    switch (diff.Type)
                    {
                        case DiffType.LeftOnly:
                            AddExtra(false, diff.Left);
                            break;

                        case DiffType.RightOnly:
                            AddExtra(true, diff.Right);
                            break;

                        case DiffType.Renamed:
                            AddRename(diff.Left, diff.Right);
                            break;

                        case DiffType.Modified:
                            AddReplace(diff.Left);
                            break;
                    }
                }
            }
        }

        [HttpGet("states")]
        public List<FileState> GetStates()
        {
            return Operations.GetStates(_options.RootFolder, (arg1, arg2) => { });
        }

        [HttpGet("issues")]
        public List<IssueInfo> GetIssues()
        {
            return Operations.GetIssues();
        }

        [HttpPost("resolve")]
        public void PostResolve([FromBody] IssueResolution resolution)
        {
            Operations.ResolveIssue(resolution);
        }

        [HttpPost("diff")]
        public void PostDiff()
        {
            Operations.AddIssue(new Issue(new IssueInfo
            {
                State = IssueState.Queued,
                Title = "Refresh",
                Message = "Comparing files to discover issues"

            }, (resolution, progress) => Diff(progress)));
        }

        private string GetFullPath(string path)
        {
            return Path.Combine(_options.RootFolder, path);
        }

        [HttpGet("length/{*path}")]
        public long GetLength(string path)
        {
            return new FileInfo(GetFullPath(path)).Length;
        }

        [HttpGet("pull/{start}/{count}/{*path}")]
        public void GetPull(long start, int count, string path)
        {
            using (var stream = new FileStream(GetFullPath(path), FileMode.Open))
            {
                stream.Seek(start, SeekOrigin.Begin);

                var content = new byte[count];

                var got = stream.Read(content, 0, count);
                if (got != count)
                {
                    throw new Exception($"Tried to read {count} but only got {got}");
                }

                Response.ContentType = OctetStream;
                Response.Body.Write(content, 0, count);
            }
        }

        [HttpPut("truncate/{*path}")]
        public void PutTruncate(string path)
        {
            Console.WriteLine($"Overwriting {path}");

            CreateParentFolders(path);

            using (var stream = new FileStream(GetFullPath(path), FileMode.Create))
            {
                Request.Body.CopyTo(stream);
            }
        }

        [HttpPut("append/{*path}")]
        public void PutAppend(string path)
        {
            using (var stream = new FileStream(GetFullPath(path), FileMode.Append))
            {
                Request.Body.CopyTo(stream);
            }
        }

        [HttpDelete("delete/{*path}")]
        public void Delete(string path)
        {
            Console.WriteLine($"Deleting file {path}");

            System.IO.File.Delete(GetFullPath(path));
            RemoveEmptyParentFolders(path);
        }

        [HttpPost("rename")]
        public void Rename([FromBody] RenameOperation op)
        {
            Console.WriteLine($"Renaming {op.OldName} to {op.NewName}");

            CreateParentFolders(op.NewName);
            System.IO.File.Move(GetFullPath(op.OldName), GetFullPath(op.NewName));
            RemoveEmptyParentFolders(op.OldName);
        }

        private void CreateParentFolders(string path)
        {
            var parent = Path.GetDirectoryName(GetFullPath(path));
            if (!Directory.Exists(parent))
            {
                Console.WriteLine($"Creating directory {parent}");
                Directory.CreateDirectory(parent);
            }
            else
            {
                Console.WriteLine($"Directory {parent} already exists");
            }
        }

        private void RemoveEmptyParentFolders(string path)
        {
            var parent = Path.GetDirectoryName(GetFullPath(path));
            if (!Directory.EnumerateFileSystemEntries(parent).Any())
            {
                Console.WriteLine($"Directory {parent} is empty, will delete it");

                Directory.Delete(parent);

                var unprefixed = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(unprefixed))
                {
                    RemoveEmptyParentFolders(unprefixed);
                }
            }
            else
            {
                Console.WriteLine($"Directory {parent} is not empty");
            }
        }
    }
}
