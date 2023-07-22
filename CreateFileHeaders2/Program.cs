
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Text;

namespace CreateFileHeaders
{
    internal class Program
    {

        static int wrongFiles = 0;
        const int MaxActualFileheaderLength = 20;
        const string Project = "SlmLicenseLib";
        const string root = "C:\\Users\\erik\\Documents\\Visual Studio 2022\\Projects\\CoCa7a\\SlmLicenseLib";
        const string copyright = "CopyrightStatement.txt";
        
        static void Main(string[] args)
        {
            List<string> cr = new List<string>();
            if (File.Exists($"{root}\\{copyright}"))
            {
                Debug.WriteLine($"{root}\\{copyright}");
                cr = File.ReadAllLines($"{root}\\{copyright}").ToList();
            }
            WalkDirectoryTree(new DirectoryInfo(root),cr);
            Console.WriteLine($"File not processed: {wrongFiles}");
        }

        static void WalkDirectoryTree(DirectoryInfo root, List<string> _cr)
        {
            FileInfo[]? files = null;
            DirectoryInfo[] subDirs;
            var dirName = root.FullName;
            if (dirName.Contains("\\obj") ||
                dirName.Contains("\\.git") ||
                dirName.Contains("\\.vs") ||
                dirName.Contains("\\bin")
                //root.FullName.EndsWith(".json")
                //root.FullName.EndsWith(".3dd") ||
                //root.FullName.EndsWith(".csproj")
                )
            {
                return;
            }

            

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.cs");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                Console.WriteLine(e.Message);
                wrongFiles++;
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                wrongFiles++;
            }

            if (files != null)
            {                
                foreach (FileInfo fi in files)
                {
                    var lines = File.ReadAllLines(fi.FullName).ToList();

                    StripHeader(lines, fi,_cr);

                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    Console.WriteLine(fi.FullName);
                }
                
                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    //var cr = new List<string>();

                    if (File.Exists($"{dirName}\\{copyright}"))
                    {
                        Debug.WriteLine($"{dirName}\\{copyright}");
                        _cr = File.ReadAllLines($"{dirName}\\{copyright}").ToList();
                    }

                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo,_cr);
                }
            }

        }

        static void StripHeader(List<string> lines, FileInfo fi,List<string> _cr)
        {
            int start = 0;
            int end = 0;
            int n, i;
            foreach (var l in lines)
            {
                l.TrimEnd();
            }
            for (n = 0; n < lines.Count; n++)
            {
                start = end = 0;
                if (lines[n].ToLower().Contains("#region") &&
                    lines[n].ToLower().Contains("fileheader"))
                {
                    start = n;
                    for (i = n + 1; i < MaxActualFileheaderLength; i++)
                    {
                        if (lines[i].ToLower().Contains("#endregion"))
                        {
                            end = i;
                            break;
                        }
                    }
                    break;
                }
            }
            if (start >= 0 && end > n)
            {
                Console.WriteLine($"{start},{lines[start]}");
                Console.WriteLine($"{end}, {lines[end]}");
                lines.RemoveRange(start, end - start + 1);                
                while (string.IsNullOrEmpty(lines[0]))
                {
                    lines.RemoveAt(0);
                }
            }
            List<string> newlines = new List<string>()
            {
                "#region FileHeader"  ,
                //"// header v.2",
                $"// Project: {Project}",
                $"// Filename:   {fi.Name}",
                $"// Last write: {fi.LastWriteTime.ToString()}",
                $"// Creation:   {fi.CreationTime.ToString()}",
                $"// Code:       {fi.GetHashCode()}/{fi.Length}",
                ""
            };
            foreach(var c in _cr)
            {
                
                newlines.Add($"// {c}");
            };
            newlines.Add("#endregion FileHeader");
            newlines.Add("");
            newlines.AddRange(lines);
            Debug.WriteLine($"{Project} {fi.Directory.Name} {fi.Name}");
            File.WriteAllLines(fi.FullName, newlines.ToArray());
            // Console.ReadLine();

        }
    }
}