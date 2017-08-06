using System;
using System.Collections.Generic;

namespace MirrorBall.Cmd
{
    class Program
    {
        static bool CheckDuplicates(List<API.FileState> states)
        {
            var duplicates = API.Operations.FindDuplicates(states);
            if (duplicates.Count != 0)
            {
                Console.WriteLine("The following groups of duplicates exist:");

                foreach (var group in duplicates)
                {
                    Console.WriteLine();
                    foreach (var file in group)
                    {
                        Console.WriteLine("  - " + file.Path);
                    }
                }

                return false;
            }

            return true;
        }

        static void Main(string[] args)
        {
            Console.WriteLine($"Scanning {args[0]}...");
            var left = API.Operations.GetStates(args[0], (progress, text) => { });
            if (!CheckDuplicates(left))
            {
                return;
            }

            Console.WriteLine($"Scanning {args[1]}...");
            var right = API.Operations.GetStates(args[1], (progress, text) => { });
            if (!CheckDuplicates(right))
            {
                return;
            }

            var diffs = API.Operations.Compare(left, right);

            foreach (var diff in diffs)
            {
                switch (diff.Type)
                {
                    case API.DiffType.LeftOnly:
                        Console.WriteLine($"Unique: {diff.Left} in {args[0]}");
                        break;
                    case API.DiffType.RightOnly:
                        Console.WriteLine($"Unique: {diff.Right} in {args[1]}");
                        break;
                    case API.DiffType.Renamed:
                        Console.WriteLine($@"Renamed: 
    {diff.Left} in {args[0]}
    {diff.Right} in {args[1]}");
                        break;
                }
            }
        }
    }
}
