using System;
using System.IO;

namespace Digda
{
    public static class Digda
    {
        public static int Main(string[] args)       //로그파일이 없을 때 자동으로 다시 계산해야함 
        {
            DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());
            char[] options = null;

            if (Directory.Exists(DigdaLog.logSaveDirPath) == false)
            {
                Directory.CreateDirectory(DigdaLog.logSaveDirPath);
            }

            if (args.Length > 0)
            {
                options = args[0].Trim(' ', '-').ToCharArray();
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
                string command = Console.ReadLine().ToLower();

                if (command.Equals("q") || command.Equals("quit"))
                {
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

                }
                else        //help
                {

                }
            }
            while (true);

            return 0;
        }

        public static long GetDirectorySize(DirectoryInfo dir, int depth)
        {
            PrintSpaces(depth);
            Console.WriteLine($"[Calculating Size] : {dir.FullName}");

            long size = 0;
            string logPath = DigdaLog.GetLogFilePath(dir.FullName);
            FileStream writingFile = new FileStream(logPath, FileMode.Create);
            StreamWriter writer = new StreamWriter(writingFile);

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

            foreach (FileInfo subFile in subFiles)
            {
                PrintSpaces(depth + 1);
                Console.WriteLine($"Found File : ({subFile.Length}byte(s)) {subFile.Name}" /*"in {dir.FullName}"*/);

                writer.WriteLine(DigdaLog.MakeFileInfos(subFile, 0));

                size += subFile.Length;
            }

            foreach (DirectoryInfo subDir in subDirectories)
            {
                PrintSpaces(depth + 1);
                Console.WriteLine($"Found Directory : {subDir.Name}" /*"in {dir.FullName}"*/);

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
    }
}

