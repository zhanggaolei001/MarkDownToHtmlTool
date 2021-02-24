using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MarkDownToHtmlTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"C:\Users\Administrator\Desktop\数据结构与算法_local";
            var dir = new DirectoryInfo(folder);
            var mdFiles = dir.GetFiles("*.md");
            foreach (var mdFile in mdFiles)
            {
                var targetHtml = mdFile.FullName.Replace(".md", ".html");

                using (var reader = new System.IO.StreamReader(mdFile.OpenRead(), Encoding.UTF8))
                using (var writer = new System.IO.StreamWriter(File.OpenWrite(targetHtml), Encoding.UTF8))
                {
                    try
                    {
                        CommonMark.CommonMarkConverter.Convert(reader, writer);
                        var mdDoc=   CommonMark.CommonMarkConverter.Parse(mdFile.Name);
                        Console.WriteLine(mdDoc);
                    
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                } 
              
            }
            var htmlFiles = dir.GetFiles("*.html");
            foreach (var htmlFile in htmlFiles)
            {
                  RunCmd("kindlegen", htmlFile.FullName);
            }
        }
        static bool RunCmd(string cmdExe, string cmdStr)
        {
            Console.WriteLine("执行:" + cmdExe + " " + cmdStr);
            bool result = false;
            try
            {
                Process myPro = new Process();
                //指定启动进程是调用的应用程序和命令行参数
                ProcessStartInfo psi = new ProcessStartInfo(cmdExe, cmdStr);
                myPro.StartInfo = psi;
                myPro.Start();
                myPro.WaitForExit();
                result = true;
            }
            catch (Exception e)
            {
                throw e;
            }
            return result;
        }
    }
}
