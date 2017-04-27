using System.IO;
using System.Collections.Generic;
namespace Digda
{
    public static class DigdaSysLog    //변경사항 저장하는 기능 필요
    {
        private static char separator = Path.DirectorySeparatorChar;
        //private static FileStream sysLogStream;
        //private static StreamWriter sysLogWriter;
        private static bool changedTrigger = false;

        public static string SysLogSaveDirPath { get; } = DigdaLog.LogSaveDirPath + separator + "DigdaSystemLog";
        public static string DigChangeLogPath { get; } = SysLogSaveDirPath + separator + "DigChange.log";
        //public static bool IsStreamOpen { get; private set; } = false;


        //public static void OpenStream(string path, FileMode mode)
        //{
        //    sysLogStream = new FileStream(path, mode);
        //    sysLogWriter = new StreamWriter(sysLogStream);
        //    IsStreamOpen = true;
        //}

        //public static void CloseStream()
        //{
        //    sysLogWriter.Close();
        //    sysLogStream.Close();
        //    IsStreamOpen = false;
        //}

        public static void OnChanged(object source, FileSystemEventArgs e)
        {
            if(changedTrigger == false)
            {
                InsertLogContent(DigChangeLogPath, e.Name);
                changedTrigger = true;
            }
            else
            {
                changedTrigger = false;
            }
        }

        public static void OnCreated(object source, FileSystemEventArgs e)
        {
            System.Console.WriteLine($"{e.ChangeType} : {e.Name}");
            InsertLogContent(DigChangeLogPath, e.Name);
        }

        public static void OnDeleted(object source, FileSystemEventArgs e)
        {

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

        private static void InsertLogContent(string logFile, string content)
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
                for (int i = 0; i < list.Count; i++)
                {
                    if (isWrote == false && string.Compare(content, list[i], true) < 0)
                    {
                        writer.WriteLine(content);
                        isWrote = true;
                    }
                    else if (isWrote == false && content.Equals(list[i]))
                    {
                        isWrote = true;
                    }
                    writer.WriteLine(list[i]);
                }

                if (isWrote == false)
                {
                    writer.WriteLine(content);
                }
            }

            writer.Close();
            stream.Close();
        }
    }
}
