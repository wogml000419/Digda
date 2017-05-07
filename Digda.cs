using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Digda
{
    public static class Digda
    {
        private static string[] excludeFiles = { @"^.*\.dig$", @"DigChange\.log", @"DeletedFiles\.log" }; 

        public static int Main(string[] args)
        {
            DigdaSysLog.LastShow = DateTime.Now;
            DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());
            char[] options = null;

            if (Directory.Exists(DigdaLog.LogSaveDirPath) == false)
            {
                Directory.CreateDirectory(DigdaLog.LogSaveDirPath);
            }
            if (Directory.Exists(DigdaSysLog.SysLogSaveDirPath) == false)
            {
                Directory.CreateDirectory(DigdaSysLog.SysLogSaveDirPath);
            }
            if(Directory.Exists(DigdaSysLog.FileChangesDirPath) == false)
            {
                Directory.CreateDirectory(DigdaSysLog.FileChangesDirPath);
            }

            if (args.Length > 0)
            {
                options = args[0].Trim(' ', '-').ToCharArray();
                if(Array.Exists(options, c => c == 'h' || c == 'H'))    //-h 는 help를 보여줍니다. 이 옵션은 프로그램을 종료시킵니다.
                {
                    PrintHelp();
                }
                if (Array.Exists(options, c => c == 'r' || c == 'R'))   //-r은 현재 디렉토리에 상관 없이 루트 디렉토리를 시작 디렉토리로 정합니다.
                {
                    current = current.Root;
                }
                if (Array.Exists(options, c => c == 'a' || c == 'A'))   //-a는 현재 디렉토리부터 하위 디렉토리/폴더들을 모두 탐색해 로그를 갱신합니다.
                {
                    Console.WriteLine($"[Calculated Size] : ({GetDirectorySize(current, 0)}byte(s)) {current.FullName}");
                }
            }

            if (File.Exists(DigdaLog.GetLogFilePath(current.FullName)) == false)
            {
                Console.WriteLine("Current Directory's log file does not exist");
                Console.WriteLine("Creating log files...");
                Console.WriteLine($"[Calculated Size] : ({GetDirectorySize(current, 0)}byte(s)) {current.FullName}");
            }

            FileSystemWatcher watcher = new FileSystemWatcher(current.FullName, "*.*")
            {
                NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true
            };

            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            watcher.EnableRaisingEvents = true;

            do
            {
                string command = Console.ReadLine().ToLower().Trim();

                if (command.Equals("q") || command.Equals("quit"))
                {
                    Console.WriteLine("Write changes before closing...");
                    DigdaSysLog.WriteChanges();
                    Console.WriteLine($"[System] ChangeLog writed at {DigdaSysLog.FileChangesDirPath}");
                    break;
                }
                else if (command.Equals("r") || command.Equals("refresh"))
                {

                }
                else if (command.Equals("c") || command.Equals("cd"))
                {

                }
                else if (command.Equals("s") || command.Equals("show"))
                {
                    DigdaSysLog.WriteChanges();
                    Console.WriteLine($"[System] ChangeLog writed at {DigdaSysLog.FileChangesDirPath}");
                }
                else if(command.Equals("h") || command.Equals("help"))
                {
                    PrintHelp();
                }
                else if(command.Equals(""))
                {
                    //pass
                }
                else        
                {
                    Console.WriteLine("[Error] Invalid Command");
                    PrintHelp();
                }
            }
            while (true);

            return 0;
        }

        public static long GetDirectorySize(DirectoryInfo dir, int depth)
        {
            PrintSpaces(depth);
            Console.WriteLine($"[Calculating Size] : {dir.FullName}");

            FileInfo[] subFiles = null;
            DirectoryInfo[] subDirectories = null;

            try
            {
                subFiles = dir.GetFiles();
                subDirectories = dir.GetDirectories();
            }
            catch (UnauthorizedAccessException)
            {
                PrintSpaces(depth);
                Console.WriteLine($"[Error] : Access at {dir.FullName} is denied");
                return 0;
            }

            if(subFiles.Length == 0 && subDirectories.Length == 0)
            {
                return 0;           //빈 폴더일 경우 파일을 만들지 않습니다.
            }

            long size = 0;
            string logPath = DigdaLog.GetLogFilePath(dir.FullName);
            FileStream writingFile = new FileStream(logPath, FileMode.Create);
            StreamWriter writer = new StreamWriter(writingFile);

            foreach (FileInfo subFile in subFiles)
            {
                PrintSpaces(depth + 1);
                Console.WriteLine($"Found File : ({subFile.Length}byte(s)) {subFile.Name}");

                writer.WriteLine(DigdaLog.MakeFileInfos(subFile, 0));

                size += subFile.Length;
            }

            foreach (DirectoryInfo subDir in subDirectories)
            {
                PrintSpaces(depth + 1);
                Console.WriteLine($"Found Directory : {subDir.Name}");

                long dirSize = GetDirectorySize(subDir, depth + 1);
                size += dirSize;

                PrintSpaces(depth + 1);
                Console.WriteLine($"Calculated Size : ({dirSize}byte(s)) {subDir.FullName}");

                writer.WriteLine(DigdaLog.MakeDirectoryInfos(subDir, dirSize, 0));
            }

            writer.WriteLine(DigdaLog.MakeThisDirInfos(dir, size, 0));

            writer.Close();
            writingFile.Close();



            return size;
        }


        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            foreach (string pattern in excludeFiles)
                if (Regex.IsMatch(e.Name, pattern))
                    return;

            Console.WriteLine($"[{e.ChangeType}] : {e.FullPath}");

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                DigdaLog.AddLogContent(e.FullPath);
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                DigdaLog.DeleteLogContent(e.FullPath);
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                DigdaLog.ChangeLogContent(e.FullPath);
            }
        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            foreach (string pattern in excludeFiles)
                if (Regex.IsMatch(e.Name, pattern))
                    return;

            Console.WriteLine($"[Renamed] {e.OldFullPath} -> {e.FullPath}");
            DigdaLog.RenameLogContent(e.OldFullPath, e.FullPath);
        }


        private static void PrintSpaces(int num)
        {
            for (int i = 0; i < num; i++)
            {
                Console.Write("| ");
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"
 _______________________________________________________________________ 
| Usage: digda [<args>]                                                 |
|                                                                       |
| args:                                                                 |
|                                                                       |
|     [-h]: Shows help                                                  |
|                                                                       |
|     [-r]: Watches root directory regardless of current directory      |
|                                                                       |
|     [-a]: Updates logs even if there are logs that written previously |
|                                                                       |
| in-program commands:                                                  |
|                                                                       | 
|     [q | quit]: Quits program                                         |
|                                                                       |
|     [r | refresh]: Updates logs. (WIP)                                |
|                                                                       |
|     [c | cd]: Changes whatching directory (WIP)                       |
|                                                                       | 
|     [s | show]: Writes changes in file                                |
|                                                                       |
|     [h | help]: Shows help                                            |
|_______________________________________________________________________|");    
        }
    }
}

