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
// You should have received a copy of the GNU General Public License along
// with this program. If not, see <https://www.gnu.org/licenses/>. 

#endregion

using System.Diagnostics;    
using System.Diagnostics.Tracing;
namespace CreateFileHeaders
{
    internal class Program
    {
        /// <summary>
        /// Adds a file header to all .cs source files in a VS solution
        /// </summary>
        /// <see cref="https://rosettacode.org/wiki/Walk_a_directory/Recursively"/>

        const int MaxActualFileheaderLength = 40;

        const string root = "C:\\Users\\erik\\Documents\\Visual Studio 2022\\Projects\\CoCa7";
        // Create two file in each folder that needs its own copyright statement:
        // CopyrightStatementXAML.txt
        // CopyrightStatementCs.txt
        // and exclude these files from the project:        
        const string copyright = "CopyrightStatement";
        static string[][] delimiters =
            {
                new[] {"#region fileheader", "#endregion fileheader",   "CS",  ".cs"},
                new[] {"<!-- fileheader"   , " fileheader -->", "XAML",".xaml"}
            };
        static List<string> cr = new List<string>();
        const string HashFile = "ProjectHashCodes";
        static List<string> HashCodes = new List<string>();
        static string Project;
        static int n;
        static string rem;
        static void Main(string[] args)
        {
            foreach (var d in GetProjects(root))
            {
                
                // Print the full path of all .cs files that are somewhere
                // in the C:\Windows directory or its subdirectories
                for (n = 0; n < 2; n++)
                {
                    string hashFile;
                    HashCodes.Clear();
                    File.Delete(hashFile =  $"{d.Item2}\\{HashFile}{delimiters[n][2]}.txt");
                    if (!File.Exists($"{d.Item2}/{copyright}{delimiters[n][2]}.txt"))
                    {
                        // do NOT switch CopyrightStatement  files.
                        // Keep the old one:
                        continue;
                    }
                    rem = new[] { "// ", "" }[n];
                    
                    foreach (var file in TraverseDirectory(
                        d.Item2.FullName, f => f.Extension == delimiters[n][3]))
                    {
                        Project = d.Item1;
                        Console.WriteLine(file.FullName);
                    }
                    // save the hashes.
                    File.AppendAllLines(hashFile, HashCodes);
                    
                }
            }
            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        static IEnumerable<FileInfo> TraverseDirectory(string rootPath, Func<FileInfo, bool> Pattern)
        {
            var directoryStack = new Stack<DirectoryInfo>();
            directoryStack.Push(new DirectoryInfo(rootPath));
            while (directoryStack.Count > 0)
            {
                var dir = directoryStack.Pop();
                string crFile;
                if (File.Exists(crFile = $"{dir}\\{copyright}{delimiters[n][2]}.txt"))
                {                    
                    cr = File.ReadAllLines(crFile).ToList();
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
                foreach (var f in dir.GetFiles().Where(Pattern)) // "Pattern" is a function
                {
                    var lines = File.ReadAllLines(f.FullName).ToList();
                    ReplaceHeader(lines, f);

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
        static void ReplaceHeader(List<string> lines, FileInfo fi)
        {
            int start = 0;
            int end = 0;
            int m, i;
            // While we're at it, strip surplus spaces:
            foreach (var l in lines)
            {
                l.TrimEnd();
            }
            foreach (var l in lines)
            {
                l.Replace("  ", " ");
            }
            // find start and end of old region
            for (m = 0; m < lines.Count; m++)
            {
                start = end = 0;
                if (lines[m].ToLower().Contains(delimiters[n][0]))
                {
                    start = m;
                    for (i = m + 1; i < MaxActualFileheaderLength; i++)
                    {
                        if (lines[i].ToLower().Contains(delimiters[n][1]))
                        {
                            end = i;
                            break;
                        }
                    }
                    break;
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

            // prepend new FileHeader region:
            List<string> newlines = new List<string>()
            {
                delimiters[n][0],
                $"{rem}Project:     {Project}",
                $"{rem}Filename:    {fi.Name}",
                $"{rem}Last write:  {fi.LastWriteTime.ToString()}",
                $"{rem}Creation:    {fi.CreationTime.ToString()}",               
                ""
            };
            
            foreach (var c in cr)
            {
                newlines.Add($"{rem}{c}");
            };
            newlines.Add(delimiters[n][1]);
            newlines.Add("");
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
