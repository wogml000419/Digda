﻿using System.Collections.Generic;
using System.IO;

namespace Digda
{
    public enum FileType { None, File, Directory, This };

    public static class DigdaLog
    {
        private static char separator = Path.DirectorySeparatorChar;
        private static string programDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", "");
        private static string root = Path.GetPathRoot(programDirPath);

        public static string LogSaveDirPath { get; } = root + separator + "DigdaLogs";
        

        public static string GetLogFilePath(string path)
        {
            return LogSaveDirPath + separator + path.TrimEnd(separator).Replace(separator, '@').Replace(":", "") + ".dig";
        }


        public static string MakeFileInfos(FileInfo file, long addSize)
        {
            return $"[File] {file.Name}|{file.Length}|{addSize}";
        }
        public static string MakeFileInfos(string fullPath, long size, long addSize)
        {
            return $"[File] {Path.GetFileName(fullPath)}|{size}|{addSize}";
        }

        public static string MakeDirectoryInfos(DirectoryInfo dir, long size, long addSize)
        {
            return $"[Directory] {dir.Name}|{size}|{addSize}";
        }
        public static string MakeDirectoryInfos(string fullPath, long size, long addSize)
        {
            return $"[Directory] {Path.GetFileName(fullPath)}|{size}|{addSize}";
        }

        public static string MakeThisDirInfos(DirectoryInfo dir, long size, long addSize)
        {
            return $"[This] {dir.Name}|{size}|{addSize}";
        }
        public static string MakeThisDirInfos(string fullPath, long size, long addSize)
        {
            return $"[This] {Path.GetFileName(fullPath)}|{size}|{addSize}";
        }


        public static void AddLogContent(string fileFullPath)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(fileFullPath));
            if (File.Exists(path) == false)
            {
                WriteEmptyFolderLog(Path.GetDirectoryName(fileFullPath), path);
            }

            string content = null;

            try
            {
                if (File.Exists(fileFullPath))
                {
                    FileInfo file = new FileInfo(fileFullPath);
                    content = MakeFileInfos(file, file.Length);
                }
                else if (Directory.Exists(fileFullPath))
                {
                    DirectoryInfo dir = new DirectoryInfo(fileFullPath);
                    long size = Digda.GetDirectorySize(dir, 0);
                    content = MakeDirectoryInfos(dir, size, size);
                }
                else
                {
                    return;
                }
            }
            catch(System.UnauthorizedAccessException)
            {
                return;
            }
            catch(FileNotFoundException)
            {
                return;
            }

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            long diff = GetSize(content);

            if (diff != 0)
            {
                list[last] = AddSizeToBoth(list[last], diff);
            }

            StreamWriter writer = Digda.WaitAndGetWriter(path, FileMode.Create);

            bool isWritten = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (isWritten == false && FileInfoCompare(content, list[i]) < 0)
                {
                    writer.WriteLine(content);
                    isWritten = true;
                }
                writer.WriteLine(list[i]);
            }

            writer.Close();

            if (diff != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fileFullPath), GetSize(list[last]));
            }

            DigdaSysLog.InsertLogContent(DigdaSysLog.DigChangeLogPath, Path.GetFileName(path));
        }

        public static void ChangeLogContent(string fileFullPath)      //여기엔 파일밖에 안 들어오겠지..?
        {
            try
            {
                ChangeLogContent(fileFullPath, new FileInfo(fileFullPath).Length);
            }
            catch(System.UnauthorizedAccessException)
            {
                System.Console.WriteLine($"[Error] Cannot make FileInfo({fileFullPath}) because access is denied.");
                return;
            }
            catch(FileNotFoundException)
            {
                System.Console.WriteLine($"[Error] Cannot make FileInfo({fileFullPath}) because file does not exist.");
                return;
            }
        }
        public static void ChangeLogContent(string fullPath, long size)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(fullPath));

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            if(last < 0)
            {
                WriteEmptyFolderLog(Path.GetDirectoryName(fullPath), path);
                list = ReadLogFile(path);
                last = list.Count - 1;
            }

            StreamWriter writer = Digda.WaitAndGetWriter(path, FileMode.Create);

            long diff = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                if (GetFileName(list[i]).Equals(Path.GetFileName(fullPath)))
                {
                    diff = size - GetSize(list[i]);
                    string content = File.Exists(fullPath) ? MakeFileInfos(fullPath, size, diff) : MakeDirectoryInfos(fullPath, size, diff);
                    content = AddSizeToAddSize(content, GetAddSize(list[i]));
                    writer.WriteLine(content);

                    list[last] = AddSizeToBoth(list[last], diff);
                    continue;
                }
                writer.WriteLine(list[i]);
            }
            writer.WriteLine(list[last]);

            writer.Close();

            if (diff != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fullPath), GetSize(list[last]));
            }

            DigdaSysLog.InsertLogContent(DigdaSysLog.DigChangeLogPath, Path.GetFileName(path));
        }

        public static void DeleteLogContent(string fileFullPath)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(fileFullPath));
            string fileName = Path.GetFileName(fileFullPath);

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            if (last < 0)
            {
                WriteEmptyFolderLog(Path.GetDirectoryName(fileFullPath), path);
                list = ReadLogFile(path);
                last = list.Count - 1;
            }

            StreamWriter writer = Digda.WaitAndGetWriter(path, FileMode.Create);

            long removeSize = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                if (GetFileName(list[i]).Equals(fileName))
                {
                    removeSize = -1 * GetSize(list[i]);
                    list[last] = AddSizeToBoth(list[last], removeSize);

                    long sizeDiff = GetSize(list[i]) - GetAddSize(list[i]);
                    if (sizeDiff > 0)
                    {
                        DigdaSysLog.InsertLogContent(DigdaSysLog.DeletedFilesLogPath, fileFullPath + '|' + sizeDiff);
                    }
                    continue;
                }
                writer.WriteLine(list[i]);
            }
            writer.WriteLine(list[last]);

            writer.Close();

            if (File.Exists(GetLogFilePath(fileFullPath)))
            {
                DeleteLogAndChilds(fileFullPath);
            }

            if (removeSize != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fileFullPath), GetSize(list[last]));
            }

            DigdaSysLog.InsertLogContent(DigdaSysLog.DigChangeLogPath, Path.GetFileName(path));
        }

        public static void RenameLogContent(string oldFile, string newFile)
        {
            string newContent = null;
            if (File.Exists(newFile))
            {
                FileInfo file = new FileInfo(newFile);
                newContent = MakeFileInfos(file, file.Length);
            }
            else if(Directory.Exists(newFile))
            {
                List<string> tmpList = ReadLogFile(GetLogFilePath(Path.GetFileName(oldFile)));
                if (tmpList.Count > 0)
                {
                    string thisInfo = tmpList[tmpList.Count - 1];
                    long size = GetSize(thisInfo);
                    long addSize = GetAddSize(thisInfo);

                    newContent = MakeDirectoryInfos(newFile, size, addSize);
                }
                else
                {
                    newContent = MakeDirectoryInfos(newFile, 0, 0);
                }

                RenameLogAndChilds(oldFile, newFile);
            }
            else
            {
                return;
            }

            string path = GetLogFilePath(Path.GetDirectoryName(oldFile));
            List<string> list = ReadLogFile(path);

            StreamWriter writer = Digda.WaitAndGetWriter(path, FileMode.Create);

            bool isRemoved = false;
            bool isWritten = false;

            foreach (string s in list)
            {
                if (isRemoved == false && GetFileName(s).Equals(Path.GetFileName(oldFile)))
                {
                    isRemoved = true;
                    continue;
                }
                if (isWritten == false && FileInfoCompare(newContent, s) < 0)
                {
                    writer.WriteLine(newContent);
                    isWritten = true;
                }
                writer.WriteLine(s);
            }

            writer.Close();
        }

        public static FileType GetFileType(string info)
        {
            if (info.StartsWith("[File]")) return FileType.File;
            if (info.StartsWith("[Directory]")) return FileType.Directory;
            if (info.StartsWith("[This]")) return FileType.This;
            return FileType.None;
        }

        public static string GetFileName(string info)
        {
            return info.Split('|')[0].Split(']')[1].Trim();
        }

        public static long GetSize(string info)
        {
            return long.Parse(info.Split('|')[1]);
        }

        public static long GetAddSize(string info)
        {
            return long.Parse(info.Split('|')[2]);
        }

        
        public static string SetAddSize(string info, long addSize)
        {
            string[] s = info.Split('|');

            return s[0] + '|' + s[1] + '|' + addSize;
        }

        public static string AddSizeToAddSize(string info, long addSize)
        {
            string[] s = info.Split('|');
            s[2] = (GetAddSize(info) + addSize).ToString();

            return s[0] + "|" + s[1] + "|" + s[2];
        }

        public static string AddSizeToSize(string info, long addSize)
        {
            string[] s = info.Split('|');
            s[1] = (GetSize(info) + addSize).ToString();

            return s[0] + "|" + s[1] + "|" + s[2];
        }

        public static string AddSizeToBoth(string info, long addSize)
        {
            return AddSizeToAddSize(AddSizeToSize(info, addSize), addSize);
        }


        private static void UpdateParentLog(string dirPath, long dirSize)
        {
            string parentDir = Path.GetDirectoryName(dirPath);
            if (parentDir != null && File.Exists(GetLogFilePath(parentDir)))
            {
                ChangeLogContent(dirPath, dirSize);
            }
        }


        private static List<string> ReadLogFile(StreamReader reader)
        {
            List<string> contents = new List<string>();

            while (true)
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
            StreamReader reader = Digda.WaitAndGetReader(path, FileMode.OpenOrCreate);

            List<string> r = ReadLogFile(reader);
            reader.Close();
            return r;
        }

        private static void WriteEmptyFolderLog(string dirPath, string filePath)
        {
            StreamWriter writer = Digda.WaitAndGetWriter(filePath, FileMode.Create);

            writer.WriteLine(MakeThisDirInfos(dirPath, 0, 0));

            writer.Close();
        }

        private static void DeleteLogAndChilds(string dirPath)
        {
            string logFilePath = GetLogFilePath(dirPath);
            if (File.Exists(logFilePath) == false)
                return;

            List<string> list = ReadLogFile(logFilePath);

            foreach(string s in list)
            {
                if(GetFileType(s) == FileType.Directory)
                {
                    DeleteLogAndChilds(dirPath + separator + GetFileName(s));
                }
            }

            File.Delete(logFilePath);
            DigdaSysLog.RemoveLogContent(DigdaSysLog.DigChangeLogPath, Path.GetFileName(logFilePath));
        }

        private static void RenameLogAndChilds(string oldDirPath, string newDirPath)
        {
            string oldLogFilePath = GetLogFilePath(oldDirPath);
            string newLogFilePath = GetLogFilePath(newDirPath);
            if (File.Exists(oldLogFilePath) == false)
                return;

            List<string> list = ReadLogFile(oldLogFilePath);

            StreamWriter writer = Digda.WaitAndGetWriter(oldLogFilePath, FileMode.Create);

            foreach (string s in list)
            {
                string content = s;
                FileType type = GetFileType(s);
                if(type == FileType.Directory)
                {
                    string dir = GetFileName(s);
                    RenameLogAndChilds(oldDirPath + separator + dir, newDirPath + separator + dir);
                }
                else if(type == FileType.This)
                {
                    content = MakeThisDirInfos(newDirPath, GetSize(s), GetAddSize(s));
                }
                writer.WriteLine(content);
            }

            writer.Close();

            File.Move(oldLogFilePath, newLogFilePath);
            DigdaSysLog.ChangeLogContent(DigdaSysLog.DigChangeLogPath, Path.GetFileName(oldLogFilePath), Path.GetFileName(newLogFilePath));
        }


        private static int FileInfoCompare(string s1, string s2)
        {
            if (GetFileType(s1) == FileType.File)
            {
                if (GetFileType(s2) == FileType.File)
                    return string.Compare(s1, s2, true);
                else
                    return -1;
            }
            else if (GetFileType(s1) == FileType.Directory)
            {
                if (GetFileType(s2) == FileType.Directory)
                    return string.Compare(s1, s2, true);
                else if (GetFileType(s2) == FileType.This)
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
}
