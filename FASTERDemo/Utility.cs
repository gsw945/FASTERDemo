using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace FASTERDemo
{
    public class Utility
    {
        /// <summary>
        /// 当前进程的可执行文件所在目录
        /// </summary>
        /// <returns></returns>
        public static string GetExecuteDir()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        }

        /// <summary>
        /// 启动目录(当前执行的程序的入口程序集文件所在目录)
        /// </summary>
        /// <returns></returns>
        public static string GetStartupFolder()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        private delegate bool IsPathExists(string path);

        private static string getFullPath(string path, bool isFolder, out bool isExist)
        {
            // 判断存在的方法
            IsPathExists isExistsFunc = File.Exists;
            if (isFolder)
            {
                isExistsFunc = Directory.Exists;
            }

            // 【1】绝对路径
            string fullPath;
            if (Path.IsPathRooted(path))
            {
                fullPath = path;
                // 判断是否存在
                isExist = isExistsFunc(fullPath);
                if (isExist)
                {
                    return fullPath;
                }
            }

            // 【2】相对(当前执行的程序入口程序集文件所在同目录)路径
            fullPath = Path.GetFullPath(Path.Combine(GetStartupFolder(), path));
            // 判断是否存在
            isExist = isExistsFunc(fullPath);
            if (isExist)
            {
                return fullPath;
            }
            // 【3】相对(当前目录)路径
            fullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
            // 判断是否存在
            isExist = isExistsFunc(fullPath);
            if (isExist)
            {
                return fullPath;
            }
            // 【4】相对(可执行文件所在同目录)路径(注: 当打成dll以`dotnet xxx.dll`方式调用时,可执行文件为dotnet)
            fullPath = Path.GetFullPath(Path.Combine(GetExecuteDir(), path));
            // 判断是否存在
            isExist = isExistsFunc(fullPath);
            if (isExist)
            {
                return fullPath;
            }
            return fullPath;
        }

        /// <summary>
        /// 获取某个目录路径的绝对路径
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <param name="isExist">是否存在</param>
        /// <returns>目录绝对路径</returns>
        public static string GetFolderPath(string path, out bool isExist)
        {
            return getFullPath(path, true, out isExist);
        }

        /// <summary>
        /// 获取某个文件路径的绝对路径
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="isExist">是否存在</param>
        /// <returns>文件绝对路径</returns>
        public static string GetFilePath(string path, out bool isExist)
        {
            return getFullPath(path, false, out isExist);
        }
    }
}
