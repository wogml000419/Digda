using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Digda
{
    public static class DigdaSysLog 
    {
        private static List<string> changesHolder = new List<string>();
        private static char separator = Path.DirectorySeparatorChar;

        public static DateTime LastShow { get; set; }
        public static string SysLogSaveDirPath { get; } = DigdaLog.LogSaveDirPath + separator + "DigdaSystemLog";
        public static string FileChangesDirPath { get; } = DigdaLog.LogSaveDirPath + separator + "FileChanges";
        public static string DigChangeLogPath { get; } = SysLogSaveDirPath + separator + "DigChange.log";
        public static string DeletedFilesLogPath { get; } = SysLogSaveDirPath + separator + "DeletedFiles.log";


        public static void InsertLogContent(string logFile, string content)
        {
            List<string> list = ReadLog(logFile);

            FileStream stream = new FileStream(logFile, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            bool isWrote = false;

            if (list.Count == 0)
            {
                writer.WriteLine(content);
            }
            else
            {
                foreach(string s in list)
                {
                    if (isWrote == false && string.Compare(content, s, true) < 0)
                    {
                        writer.WriteLine(content);
                        isWrote = true;
                    }
                    else if (isWrote == false && content.Equals(s))
                    {
                        isWrote = true;
                    }
                    writer.WriteLine(s);
                }

                if (isWrote == false)
                {
                    writer.WriteLine(content);
                }
            }

            writer.Close();
            stream.Close();
        }

        public static void RemoveLogContent(string logFile, string removeContent)
        {
            List<string> list = ReadLog(logFile);

            FileStream stream = new FileStream(logFile, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            foreach (string s in list)
            {
                if (s.Equals(removeContent))
                {
                    continue;
                }
                writer.WriteLine(s);
            }
            
            writer.Close();
            stream.Close();
        }

        public static void WriteChanges()
        {
            changesHolder.Clear();
            int firstDepth = -1;

            while (true)
            {  
                List<string> list = ReadLog(DigChangeLogPath);
                if (list.Count <= 0)
                {
                    break;
                }
                if (firstDepth < 0)
                    firstDepth = list[0].Split('@').Length - 1;
                Console.WriteLine($"[Debug] {list[0]}");
                WriteChanges(DigdaLog.LogSaveDirPath + separator + list[0], firstDepth - (list[0].Split('@').Length - 1));
            }

            DateTime current = DateTime.Now;
            FileStream stream = new FileStream(FileChangesDirPath + separator + string.Format("{0:yyyy-MM-dd HH,mm,ss}.log", current), FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} => {1:yyyy-MM-dd HH:mm:ss}", LastShow, current);
            LastShow = current;

            foreach(string s in changesHolder)
            {
                writer.WriteLine(s);
            }

            writer.Close();
            stream.Close();

            File.Delete(DigChangeLogPath);
            File.Delete(DeletedFilesLogPath);

            changesHolder.Clear();
        }
        private static void WriteChanges(string logPath, int depth)
        {
            Console.WriteLine($"[Debug] {logPath}");
            List<string> log = ReadLog(logPath);
            List<string> deleted = ReadLog(DeletedFilesLogPath);

            int last = log.Count - 1;
            if(last < 0)
            {
                Console.WriteLine("log is empty, writing failed...");
                return;
            }

            if (DigdaLog.GetAddSize(log[last]) == DigdaLog.GetSize(log[last]))
            {
                changesHolder.Add(GetSpaces(depth) + "[Created] " + MakeChangesContent(log[last]));
            }
            else
            {
                changesHolder.Add(GetSpaces(depth) + "[Changed] " + MakeChangesContent(log[last]));
            }

            foreach(string s in deleted)
            {
                string[] split = s.Split('|');
                string tmpLogFilePath = DigdaLog.GetLogFilePath(Path.GetDirectoryName(split[0]));
                long size = long.Parse(split[1]);

                if (tmpLogFilePath.Equals(logPath))
                {
                    changesHolder.Add(GetSpaces(depth + 1) + "[Deleted] " + string.Format("({0:+#;-#;0}byte(s)) ", size * -1) + Path.GetFileName(split[0]));
                    RemoveLogContent(DeletedFilesLogPath, s);
                }
            }

            FileStream stream = new FileStream(logPath, FileMode.Create);
            StreamWriter writer = new StreamWriter(stream);

            foreach (string s in log)
            {
                string tmp = s;
                if (DigdaLog.GetAddSize(s) != 0)
                {
                    string status = null;

                    if (DigdaLog.GetSize(s) == DigdaLog.GetAddSize(s))
                        status = "[Created] ";
                    else
                        status = "[Changed] ";

                    switch(DigdaLog.GetFileType(s))
                    { 
                    case FileType.File:
                        changesHolder.Add(GetSpaces(depth + 1) + status + MakeChangesContent(s));
                        break;

                    case FileType.Directory:
                        string tmpLogPath = logPath.Remove(logPath.Length - 4) + '@' + DigdaLog.GetFileName(s) + ".dig";
                        WriteChanges(tmpLogPath, depth + 1);
                        break;

                    case FileType.This:
                        break;
                    }

                    tmp = DigdaLog.SetAddSize(tmp, 0);
                }
                writer.WriteLine(tmp);
            }

            writer.Close();
            stream.Close();

            RemoveLogContent(DigChangeLogPath, Path.GetFileName(logPath));
        }

        private static List<string> ReadLog(string path)
        {
            FileStream stream = new FileStream(path, FileMode.OpenOrCreate);
            StreamReader reader = new StreamReader(stream);
            List<string> list = new List<string>();

            while(true)
            {
                string s = reader.ReadLine();
                if (s == null || s.Equals(""))
                    break;
                list.Add(s);
            }

            reader.Close();
            stream.Close();
            return list;
        }

        private static string GetSpaces(int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
                sb.Append("| ");
            return sb.ToString();
        }

        private static string MakeChangesContent(string logContent)
        {
            return string.Format("({0:+#;-#;0}byte(s)) ", DigdaLog.GetAddSize(logContent)) 
                + DigdaLog.GetFileName(logContent);
        }
    }
}
