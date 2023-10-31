#region FileHeader

// (c) E.H. Terwiel, 2023, the Netherlands 
// This program is written by E.H. Terwiel, the Netherlands

// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your option)
// any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
// A copy of the GNU General Public License is included in this project.
// If not, see <https://www.gnu.org/licenses/>. 

#endregion

using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CreateFileHeaders
{
    internal class Program
    {
        /// <summary>
        /// Adds a file header to all .cs and .xaml source files in a VS solution
        /// </summary>
        /// <see cref="https://rosettacode.org/wiki/Walk_a_directory/Recursively"/>

        // remove old fileheaders, which may be at most 40 lines:
        const int MaxActualFileheaderLength = 40;

        const string root = "C:\\Users\\erik\\Documents\\Visual Studio 2022\\Projects\\CoCa9";         

        // Each DirectoryTree branch may have its own set of two. A set determines the
        // content of the header for THAT branch.
        const string copyright = "CopyrightStatement";

        // delimiting phrases used to recognize the old headers.
        // First two elements must be lowercase
        static string HeaderMarker = "FileHeader";
        static string[][] delimiters =
            {
                new[] {$"#region {HeaderMarker}\n/*", $"*/\n#endregion {HeaderMarker}",   "CS",  ".cs"},
                new[] {$"<!-- {HeaderMarker}"   , $" {HeaderMarker} -->", "XAML",".xaml"}
            };
        static List<string> copyrightStatementLines = new List<string>() 
            {
            "© Normec Rei-Lux, 2023",
            "All rights reserved.",
            "No part of this file may be copied in any form",
            " without written consent of the copyrightholder."
            };
        const string HashFile = "ProjectHashCodes";
        static List<string> HashCodes = new List<string>();
        static string Project;
        static int n;

        static void Main(string[] args)
        {
            foreach (var d in GetProjects(root))
            {
                foreach (string[] delim in delimiters) // cd / xaml
                {
                    string hashFile;
                    HashCodes.Clear();
                    File.Delete(hashFile = $"{d.Item2}\\{HashFile}{delim[2]}.txt");
                    if (!File.Exists($"{d.Item2}/{copyright}{delim[2]}.txt"))
                    {
                        // do NOT switch CopyrightStatement  files.
                        // Keep the old one:
                        continue;
                    }
                    Project = d.Item1;
                    foreach (var file in TraverseDirectory(d.Item2.FullName,  delim))
                    {
                        
                        Console.WriteLine(file.FullName);
                    }
                    // save the hashes.
                    File.AppendAllLines(hashFile, HashCodes);

                }
            }
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        static IEnumerable<FileInfo> TraverseDirectory(string rootPath,  string[] delim)
        {
            string fileType = delim[3];
            var directoryStack = new Stack<DirectoryInfo>();
            directoryStack.Push(new DirectoryInfo(rootPath));
            while (directoryStack.Count > 0)
            {
                var dir = directoryStack.Pop();
                string crFile;
                if (File.Exists(crFile = $"{dir}\\{copyright}{fileType}.txt"))
                {
                    copyrightStatementLines = File.ReadAllLines(crFile).ToList();
                }

                try
                {
                    foreach (var i in dir.GetDirectories().Where(dr =>
                     !dr.FullName.Contains("\\bin") &&
                     !dr.FullName.Contains("\\obj") &&
                     !dr.FullName.Contains("\\.git") &&
                     !dr.FullName.Contains("\\.vs")))
                        directoryStack.Push(i);
                }
                catch (UnauthorizedAccessException)
                {
                    continue; // We don't have access to this directory, so skip it
                }
                foreach (var f in dir.GetFiles().Where(fl => fl.Extension.ToLower().Equals(fileType))) // "Pattern" is a function
                {
                    var lines = File.ReadAllLines(f.FullName).ToList();
                    ReplaceHeader(lines, f, delim);

                    yield return f;
                }
            }
        }

        /// <summary>
        /// Find all the projects in the solution:
        /// </summary>
        /// <param name="root">the root folder of the project</param>
        /// <returns>The projects in the solution we need to process</returns>
        static List<(string, DirectoryInfo)> GetProjects(string root)
        {
            List<(string, DirectoryInfo)> lst = new List<(string, DirectoryInfo)>();
            string solution = Directory.GetFiles(root).Where(f => f.EndsWith(".sln")).First();
            var content = File.ReadAllLines(solution);
            string[] projs;
            foreach (string item in content)
            {
                if (item.Length >= 7 &&
                    item[0..7].Equals("Project") &&
                    (projs = item.Split(new char[] { ',', '=' }))[2].Contains(".csproj")
                    )
                {
                    var proj2 = projs[1][2..(projs[1].Length - 1)];
                    var proj3 = projs[2][2..(projs[2].Length - 1)];
                    lst.Add((proj2, new DirectoryInfo(Path.GetFullPath($"{root}\\{proj2}"))));
                }
            }
            return lst;
        }

        /// <summary>
        /// Find the old FileHeader region, if any
        /// </summary>
        /// <param name="lines">The content of the file that needs a new FileHeader region</param>
        /// <param name="fi">The file's name</param>
        static void ReplaceHeader(List<string> lines, FileInfo fi, string[] delim)
        {
            int start = 0;
            int end = 0;
            int i;
            
            // remove extra spaces:
            for (int p = 0; p < lines.Count; p++)
            {
                int n = 0;
                //lines[p] = Regex.Replace(lines[p], "[ \t]+$", " ");
                // skip leading spaces:
                if (lines[p].Length > 2)
                {
                    //while (lines[p][n..].Contains("  "))
                    {
                        string t= Regex.Replace(lines[p].TrimEnd(' '), @"(\w+ )\s+", "$0",RegexOptions.IgnoreCase);
                    }
                }
            }
            // find start and end of old region
            end = start = -1;
            for (int m = 0; m < lines.Count; m++)
            {
                if (start == -1)
                {
                    if (lines[m].ToLower().Contains(HeaderMarker.ToLower()))
                    {
                        start = m;
                    }
                }
                else
                {
                    if (lines[m].ToLower().Contains(HeaderMarker.ToLower()))
                    {
                        end = m;
                        break;
                    }
                }                
            }

            // remove it:
            if (start >= 0 && end > start)
            {
                Console.WriteLine($"{start},{lines[start]}");
                Console.WriteLine($"{end}, {lines[end]}");
                lines.RemoveRange(start, end - start + 1);
                while (string.IsNullOrEmpty(lines[0]))
                {
                    lines.RemoveAt(0);
                }
            }

            // prepend new FileHeader region and a fixed part for all headers:
            List<string> newlines = new List<string>()
            {
                delim[0],
                $"Project:     {Project}",
                $"Filename:    {fi.Name}",
                $"Last write:  {fi.LastWriteTime.ToString()}",
                $"Creation:    {fi.CreationTime.ToString()}",
                ""
            };

            // Add the lines from the CopyrightStatement.txt file:
            foreach (var c in copyrightStatementLines)
            {
                newlines.Add(c);
            };
            newlines.Add(delim[1]);
            newlines.Add("");

            //List<string> newlines = new List<string>();
            newlines.AddRange(lines);
            Debug.WriteLine($"{Project} {fi.Directory.Name} {fi.Name}");

            // disable this line for a trial run:
            File.WriteAllLines(fi.FullName, newlines.ToArray());
            // we also gather file hashes and file lengths in a text file 
            int hash = fi.GetHashCode();
            long fileLength = fi.Length;
            HashCodes.Add($"{Project} ; {fi.Name} ; {hash} ; {fileLength}");

        }
    }
}
