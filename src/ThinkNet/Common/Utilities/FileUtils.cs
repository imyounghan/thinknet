using System;
using System.IO;
using System.Web;

namespace ThinkNet.Common.Utilities
{
    /// <summary>
    /// 对文件相关操作的工具类
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// 判断目录是否存在
        /// </summary>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// 判断文件是否存在
        /// </summary>
        public static bool FileExists(string fileFullName)
        {
            return File.Exists(fileFullName);
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="sourceFileName">移动文件</param>
        /// <param name="destFileName">目标文件</param>
        /// <returns>是否成功</returns>
        public static bool FileMove(string sourceFileName, string destFileName)
        {
            if (!FileExists(sourceFileName))
                return false;

            if (string.IsNullOrEmpty(destFileName))
                return false;

            string destFilePath = destFileName.Substring(0, destFileName.LastIndexOf("\\"));

            if (!Directory.Exists(destFilePath)) {
                Directory.CreateDirectory(destFilePath);
            }
            File.Delete(destFileName);
            File.Move(sourceFileName, destFileName);

            return true;
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourceFileName">复制文件</param>
        /// <param name="destFileName">目标文件</param>
        public static bool FileCopy(string sourceFileName, string destFileName)
        {
            if (!FileExists(sourceFileName))
                return false;

            if (destFileName == string.Empty)
                return false;

            string destFilePath = destFileName.Substring(0, destFileName.LastIndexOf("\\"));

            if (!Directory.Exists(destFilePath)) {
                Directory.CreateDirectory(destFilePath);
            }
            File.Copy(sourceFileName, destFileName, true);

            return true;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public static void FileDelete(string fileName)
        {
            if (!FileExists(fileName))
                return;

            File.Delete(fileName);
        }

        /// <summary>
        /// 文件大小
        /// </summary>
        public static long FileSize(string fileName)
        {
            if (File.Exists(fileName)) {
                return new FileInfo(fileName).Length;
            }

            return -1;
        }

        /// <summary>
        /// 获得当前绝对路径
        /// </summary>
        /// <param name="strPath">指定的路径</param>
        /// <returns>绝对路径</returns>
        public static string GetMapPath(string strPath)
        {
            if (HttpContext.Current != null) {
                return HttpContext.Current.Server.MapPath(strPath);
            }
            else //非web程序引用
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPath);
            }
            //if (ReflectiveHttpContext.HttpContextCurrentGetter() != null) {
            //    return ReflectiveHttpContext.GetMapPath(strPath);
            //}
            //else //非web程序引用
            //{
            //    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPath);
            //}
        }
    }
}
