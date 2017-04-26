using System.IO;

namespace Digda
{
    public static class DigdaSysLog    //변경사항 저장하는 기능 필요
    {
        private static char separator = Path.DirectorySeparatorChar;
        private static FileStream sysLogStream;
        private static StreamWriter sysLogWriter;

        public static string SysLogSaveDirPath { get; } = DigdaLog.LogSaveDirPath + separator + "DigdaSystemLog";
        public static bool IsStreamOpen { get; private set; } = false;


        public static void OpenStream(string path, FileMode mode)
        {
            sysLogStream = new FileStream(path, mode);
            sysLogWriter = new StreamWriter(sysLogStream);
            IsStreamOpen = true;
        }

        public static void CloseStream()
        {
            sysLogWriter.Close();
            sysLogStream.Close();
            IsStreamOpen = false;
        }

        public static void OnChanged(object source, FileSystemEventArgs e)
        {

        }

        public static void OnCreated(object source, FileSystemEventArgs e)
        {

        }

        public static void OnDeleted(object source, FileSystemEventArgs e)
        {

        }
    }
}
