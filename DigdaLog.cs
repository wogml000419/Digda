﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Digda
{
    public static class DigdaLog    //변경사항 저장해야함
    {
        private static char separator = Path.DirectorySeparatorChar;
        private static string programDirPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).Replace(@"file:\", "");
        public static string logSaveDirPath = Path.GetPathRoot(programDirPath) + separator + "DigdaLogs";


        public static string GetLogFilePath(string path)
        {
            return logSaveDirPath + separator + path.Replace(separator, '@').Replace(":", "") + ".dig";
        }

        public static string GetParentLogFilePath(string logFileName)
        {
            StringBuilder sb = new StringBuilder();
            string[] array = logFileName.Split('@');
            for(int i=0; i<array.Length - 1; i++)
            {
                sb.Append(array[i]);
            }
            return logSaveDirPath + separator + sb.ToString() + ".dig";
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

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            string content = null;

            if (File.Exists(fileFullPath))
            {
                FileInfo file = new FileInfo(fileFullPath);
                content = MakeFileInfos(file, file.Length);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(fileFullPath);
                long size = Digda.GetDirectorySize(dir, 0);
                content = MakeDirectoryInfos(dir, size, size);
            }

            long diff = GetSize(content);

            if (diff != 0)
            {
                list[last] = AddSizeToBoth(list[last], diff);
            }

            FileStream stream = new FileStream(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            for (int i = 0; i < list.Count; i++)
            {
                if (FileInfoCompare(content, list[i]) < 0)
                {
                    writer.WriteLine(content);
                }
                writer.WriteLine(list[i]);
            }

            writer.Close();
            stream.Close();

            if (diff != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fileFullPath), GetSize(list[last]));
            }
        }

        public static void ChangeLogContent(string fileFullPath)      //여기엔 파일밖에 안 들어오겠지..?
        {
            ChangeLogContent(fileFullPath, new FileInfo(fileFullPath).Length);
        }
        public static void ChangeLogContent(string fullPath, long size)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(fullPath));

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            FileStream stream = new FileStream(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

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
            stream.Close();

            if (diff != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fullPath), GetSize(list[last]));
            }
        }

        public static void DeleteLogContent(string fileFullPath)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(fileFullPath));
            string fileName = Path.GetFileName(fileFullPath);

            List<string> list = ReadLogFile(path);
            int last = list.Count - 1;

            FileStream stream = new FileStream(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            long removeSize = 0;

            for (int i = 0; i < list.Count - 1; i++)
            {
                if (GetFileName(list[i]).Equals(fileName))
                {
                    removeSize = -1 * GetSize(list[i]);
                    list[last] = AddSizeToBoth(list[last], removeSize);
                    continue;
                }
                writer.WriteLine(list[i]);
            }
            writer.WriteLine(list[last]);

            writer.Close();
            stream.Close();

            if (File.Exists(GetLogFilePath(fileFullPath)))
            {
                DeleteLogAndChilds(fileFullPath);
            }

            if (removeSize != 0)
            {
                UpdateParentLog(Path.GetDirectoryName(fileFullPath), GetSize(list[last]));
            }
        }
        public static void RenameLogContent(string oldFile, string newFile)
        {
            string path = GetLogFilePath(Path.GetDirectoryName(oldFile));
            List<string> list = ReadLogFile(path);

            FileStream stream = new FileStream(path, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            bool isRemoved = false;
            string newContent = null;

            if (File.Exists(newFile))
            {
                FileInfo file = new FileInfo(newFile);
                newContent = MakeFileInfos(file, file.Length);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(newFile);
                long size = Digda.GetDirectorySize(dir, 0);
                newContent = MakeDirectoryInfos(dir, size, size);
            }

            foreach (string s in list)
            {
                if (isRemoved == false && GetFileName(s).Equals(Path.GetFileName(oldFile)))
                {
                    isRemoved = true;
                    continue;
                }
                if (FileInfoCompare(newContent, s) < 0)
                {
                    writer.WriteLine(newContent);
                }
                writer.WriteLine(s);
            }

            writer.Close();
            stream.Close();
        }

        private static void UpdateParentLog(string dirPath, long dirSize)
        {
            if (File.Exists(GetLogFilePath(Path.GetDirectoryName(dirPath))))
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
            FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
            StreamReader reader = new StreamReader(stream);

            List<string> r = ReadLogFile(reader);
            reader.Close();
            stream.Close();
            return r;
        }

        private static void WriteEmptyFolderLog(string dirPath, string filePath)
        {
            FileStream stream = new FileStream(filePath, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            writer.WriteLine(MakeThisDirInfos(dirPath, 0, 0));

            writer.Close();
            stream.Close();
        }

        private static void DeleteLogAndChilds(string dirPath)
        {
            string logFilePath = GetLogFilePath(dirPath);
            if (File.Exists(logFilePath) == false)
                return;

            List<string> list = ReadLogFile(logFilePath);

            foreach(string s in list)
            {
                if(s.StartsWith("[Directory]"))
                {
                    DeleteLogAndChilds(dirPath + separator + GetFileName(s));
                }
            }

            File.Delete(logFilePath);
        }

        private static string GetFileName(string info)
        {
            return info.Split('|')[0].Split(']')[1].Trim();
        }

        private static long GetSize(string info)
        {
            return long.Parse(info.Split('|')[1]);
        }

        private static long GetAddSize(string info)
        {
            return long.Parse(info.Split('|')[2]);
        }


        private static string AddSizeToAddSize(string info, long addSize)
        {
            string[] s = info.Split('|');
            s[2] = (GetAddSize(info) + addSize).ToString();

            return s[0] + "|" + s[1] + "|" + s[2];
        }

        private static string AddSizeToSize(string info, long addSize)
        {
            string[] s = info.Split('|');
            s[1] = (GetSize(info) + addSize).ToString();

            return s[0] + "|" + s[1] + "|" + s[2];
        }

        private static string AddSizeToBoth(string info, long addSize)
        {
            return AddSizeToAddSize(AddSizeToSize(info, addSize), addSize);
        }

        private static int FileInfoCompare(string s1, string s2)
        {
            if (s1.StartsWith("[File]"))
            {
                if (s2.StartsWith("[File]"))
                    return string.Compare(s1, s2, true);
                else
                    return -1;
            }
            else if (s1.StartsWith("[Directory]"))
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
}