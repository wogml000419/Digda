using System;
using System.IO;
using System.Collections.Generic;
public static class Digda
{
    private static string newLine = Environment.NewLine;
    private static char separator = Path.DirectorySeparatorChar;
    private static string programDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", "");
    private static string logSaveDirPath = Path.GetPathRoot(programDirPath) + separator + "DigdaLogs";
    private static FileInfo[] logs = new DirectoryInfo(logSaveDirPath).GetFiles("*.dig");
    private static FileSystemWatcher watcher;
    

    static int Main(string[] args)
    {
        DirectoryInfo current = new DirectoryInfo(Directory.GetCurrentDirectory());
        char[] options = null;

        if (Directory.Exists(logSaveDirPath) == false)
        {
            Directory.CreateDirectory(logSaveDirPath);
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
                Console.WriteLine($"Calculated Size: ({GetDirectorySize(current, 0)}byte(s)) {current.FullName}");
            }
        }

        watcher = new FileSystemWatcher(current.FullName, "*.*");
        watcher.NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName;

        Console.WriteLine(watcher.Path);

        watcher.Changed += new FileSystemEventHandler(OnChanged);
        watcher.Created += new FileSystemEventHandler(OnChanged);
        watcher.Deleted += new FileSystemEventHandler(OnChanged);
        watcher.Renamed += new RenamedEventHandler(OnRenamed);

        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;

        //Console.WriteLine($"Calculated Size: {current.FullName} ({GetDirectorySize(current, 0)}byte(s))");
        while (Console.Read() != 'q');
        return 0;
    }

    private static long GetDirectorySize(DirectoryInfo dir, int depth)
    {
        PrintSpaces(depth);
        Console.WriteLine($"Calculating Size: {dir.FullName}");

        long size = 0;
        string logPath = ReturnLogFilePath(dir.FullName);
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
            Console.WriteLine($"[Error] Access at {dir.FullName} is denied");
            return 0;
        }

        foreach (FileInfo subFile in subFiles)
        {
            PrintSpaces(depth + 1);
            Console.WriteLine($"Found File: ({subFile.Length}byte(s)) {subFile.Name}" /*"in {dir.FullName}"*/);

            writer.WriteLine( ReturnFileInfos(subFile, 0) );

            size += subFile.Length;
        }

        foreach (DirectoryInfo subDir in subDirectories)
        {
            PrintSpaces(depth + 1);
            Console.WriteLine($"Found Directory: {subDir.Name}" /*"in {dir.FullName}"*/);

            long dirSize = GetDirectorySize(subDir, depth + 1);
            size += dirSize;

            PrintSpaces(depth + 1);
            Console.WriteLine($"Calculated Size: ({dirSize}byte(s)) {subDir.FullName}");

            writer.WriteLine( ReturnDirectoryInfos(subDir, dirSize, 0) );
        }

        writer.WriteLine( ReturnThisDirInfos(dir, size, 0) );

        writer.Close();
        writingFile.Close();
        return size;
    }

    private static void OnChanged(object source, FileSystemEventArgs e)
    {
        Console.WriteLine($"[{e.ChangeType}] : {e.FullPath}");

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            AddLogContent(e.FullPath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            DeleteLogContent(e.FullPath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            ChangeLogContent(e.FullPath);
        }
    }

    private static void OnRenamed(object source, RenamedEventArgs e)
    {
        Console.WriteLine($"[Renamed] {e.OldFullPath} -> {e.FullPath}");
        RenameLogContent(e.OldFullPath, e.FullPath);
    }

    private static void PrintSpaces(int num)
    {
        for(int i=0; i<num; i++)
        {
            Console.Write("| ");
        }
    }
    
    private static string ReturnFileInfos(FileInfo file, long addSize)
    {
        return $"[File] {file.Name}|{file.Length}|{addSize}";
    }
    private static string ReturnFileInfos(string fullPath, long size, long addSize)
    {
        return $"[File] {Path.GetFileName(fullPath)}|{size}|{addSize}";
    }

    private static string ReturnDirectoryInfos(DirectoryInfo dir, long size, long addSize)
    {
        return $"[Directory] {dir.Name}|{size}|{addSize}";
    }
    private static string ReturnDirectoryInfos(string fullPath, long size, long addSize)
    {
        return $"[Directory] {Path.GetFileName(fullPath)}|{size}|{addSize}";
    }

    private static string ReturnThisDirInfos(DirectoryInfo dir, long size, long addSize)
    {
        return $"[This] {dir.Name}|{size}|{addSize}";
    }
    private static string ReturnThisDirInfos(string fullPath, long size, long addSize)
    {
        return $"[This] {Path.GetFileName(fullPath)}|{size}|{addSize}";
    }

    private static string ReturnFileNameFromInfo(string info)
    {
        return info.Split('|')[0].Split(']')[1].Trim();
    }

    private static long ReturnSizeFromInfo(string info)
    {
        return long.Parse(info.Split('|')[1]);
    }

    private static long ReturnAddSizeFromInfo(string info)
    {
        return long.Parse(info.Split('|')[2]);
    }

    private static string ReturnLogFilePath(string path)
    {
        return logSaveDirPath + separator + path.Replace(separator, '@').Replace(":", "") + ".dig";
    }

    private static string AddSizeToAddSize(string info, long addSize)
    {
        string[] s = info.Split('|');
        s[2] = (ReturnAddSizeFromInfo(info) + addSize).ToString();
        
        return s[0] + "|" + s[1] + "|" + s[2];
    }

    private static string AddSizeToSize(string info, long addSize)
    {
        string[] s = info.Split('|');
        s[1] = (ReturnSizeFromInfo(info) + addSize).ToString();

        return s[0] + "|" + s[1] + "|" + s[2];
    }

    private static List<string> ReadLogFile(StreamReader reader)
    {
        List<string> contents = new List<string>();
        
        while(true)
        {
            string s = reader.ReadLine();
            if (s == null || s.Equals(""))
                break;
            contents.Add(s);
        }

        return contents;
    }
    private static List<string> ReadLogFile(string path)
    {
        FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
        StreamReader reader = new StreamReader(stream);

        List<string> r = ReadLogFile(reader);
        reader.Close();
        stream.Close();
        return r;
    }

    private static void WriteLogFile(StreamWriter writer, List<string> list)
    {
        foreach(string s in list)
        {
            writer.WriteLine(s);
        }
    }
    private static void WriteLogFile(string path, List<string> list)
    {
        FileStream stream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(stream);
        WriteLogFile(writer, list);
        stream.Close();
        writer.Close();
    }

    private static void AddLogContent(string fileFullPath)
    {
        string path = ReturnLogFilePath(Path.GetDirectoryName(fileFullPath));

        List<string> list =  ReadLogFile(path);
        int last = list.Count - 1;
        
        string content = null;
        bool isWrote = false;

        if (File.Exists(fileFullPath))
        {
            FileInfo file = new FileInfo(fileFullPath);
            content = ReturnFileInfos(file, file.Length);
        }
        else
        {
            DirectoryInfo dir = new DirectoryInfo(fileFullPath);
            long size = GetDirectorySize(dir, 0);
            content = ReturnDirectoryInfos(dir, size, size);
        }

        long diff = ReturnSizeFromInfo(content);

        if (diff != 0)
        {
            list[last] = AddSizeToAddSize(AddSizeToSize(list[last], diff), diff);
        }

        FileStream stream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(stream);

        foreach(string s in list)
        {
            if(isWrote == false && FileInfoCompare(content, s) < 0)
            {
                writer.WriteLine(content);
                isWrote = true;
            }
            writer.WriteLine(s);
        }

        writer.Close();
        stream.Close();
    }

    private static void ChangeLogContent(string fileFullPath)      //여기엔 파일밖에 안 들어오겠지..?
    {
        string path = ReturnLogFilePath(Path.GetDirectoryName(fileFullPath));

        List<string> list = ReadLogFile(path);

        FileStream stream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(stream);

        for (int i=0; i<list.Count; i++)
        {
            Console.WriteLine($"{ReturnFileNameFromInfo(list[i])} == {Path.GetFileName(fileFullPath)} => {ReturnFileNameFromInfo(list[i]).Equals(Path.GetFileName(fileFullPath))}");
            if(ReturnFileNameFromInfo(list[i]).Equals(Path.GetFileName(fileFullPath)))
            {
                
                FileInfo file = new FileInfo(fileFullPath);
                long diff = file.Length - ReturnSizeFromInfo(list[i])/* + ReturnAddSizeFromInfo(list[i])*/;
                string content = AddSizeToAddSize(ReturnFileInfos(file, diff), ReturnAddSizeFromInfo(list[i]));
                writer.WriteLine(content);

                int last = list.Count - 1;
                list[last] = AddSizeToAddSize(AddSizeToSize(list[last], diff), diff);
                continue;
            }
            writer.WriteLine(list[i]);
        }

        writer.Close();
        stream.Close();
    }

    private static void DeleteLogContent(string fileFullPath)
    {
        string path = ReturnLogFilePath(Path.GetDirectoryName(fileFullPath));
        string fileName = Path.GetFileName(fileFullPath);
        List<string> list = ReadLogFile(path);

        FileStream stream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(stream);

        long removeSize;

        for(int i = 0; i < list.Count; i++)   //!!!!
        {
            if (ReturnFileNameFromInfo(list[i]).Equals(fileName))
            {
                removeSize = ReturnSizeFromInfo(list[i]);
                int last = list.Count - 1;
                list[last] = AddSizeToAddSize(AddSizeToSize(list[last], removeSize), removeSize);
                continue;
            }
            writer.WriteLine(list[i]);
        }

        writer.Close();
        stream.Close();

        if(Directory.Exists(fileFullPath))
        {
            File.Delete(ReturnLogFilePath(fileFullPath));
        }
    }

    private static void RenameLogContent(string oldFile, string newFile)
    {
        string path = ReturnLogFilePath(Path.GetDirectoryName(oldFile));
        List<string> list = ReadLogFile(path);

        FileStream stream = new FileStream(path, FileMode.Create);
        StreamWriter writer = new StreamWriter(stream);

        bool isWrote = false;
        string newContent = null;

        if (File.Exists(newFile))
        {
            FileInfo file = new FileInfo(newFile);
            newContent = ReturnFileInfos(file, file.Length);
        }
        else
        {
            DirectoryInfo dir = new DirectoryInfo(newFile);
            long size = GetDirectorySize(dir, 0);
            newContent = ReturnDirectoryInfos(dir, size, size);
        }

        foreach (string s in list)
        {
            if (ReturnFileNameFromInfo(s).Equals(Path.GetFileName(oldFile)))
            {
                continue;
            }
            if (isWrote == false && FileInfoCompare(newContent, s) < 0)
            {
                writer.WriteLine(newContent);
                isWrote = true;
            }
            writer.WriteLine(s);
        }

        writer.Close();
        stream.Close();
    }

    private static int FileInfoCompare(string s1, string s2)
    {
        if(s1.StartsWith("[File]"))
        {
            if (s2.StartsWith("[File]"))
                return string.Compare(s1, s2, true);
            else
                return -1;
        }
        else if(s1.StartsWith("[Directory]"))
        {
            if (s2.StartsWith("[Directory]"))
                return string.Compare(s1, s2, true);
            else if (s2.StartsWith("[This]"))
                return -1;
            else
                return 1;
        }
        else
        {
            return 1;   //[This]는 한 파일 내 오직 하나뿐이고, 제일 뒤에 있으므로
        }
    }
}

