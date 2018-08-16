using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace IpAddressChange
{
    public static class ReadFile
    {
        public static ReturnValue GetFilePath()
        {
            //获取当前路径下的txt,文件名ipadress
            //获取应用程序的当前工作目录  
            string path = System.IO.Directory.GetCurrentDirectory();
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] fi = di.GetFiles("IpAddress.txt", SearchOption.TopDirectoryOnly);
            if (fi == null || fi.Length != 1)
            {
                return new ReturnValue() { isbo = false, msg = "IpAddress文件不存在" };
            }
            return new ReturnValue() { isbo = true, msg = fi[0].FullName };

        }

    }

    public class ReturnValue
    {
        public bool isbo { get; set; }
        public string msg { get; set; }

        //public FileInfo file { get; set; }
    }
}
