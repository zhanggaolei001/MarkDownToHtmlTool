using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarkDownPhotoToBase64
{
    class Program
    {
        private static DirectoryInfo RootPath = new DirectoryInfo("./");
        private static DirectoryInfo ImagesPath = new DirectoryInfo("./image/");
        private static Dictionary<string, string> PicBase64 = new Dictionary<string, string>();
        static async Task Main(string[] args)
        {
            // var mdDir = new DirectoryInfo(@"C:\Users\Administrator\Desktop\数据结构与算法");
            //Console.WriteLine("请指定MarkDown文件夹路径:");
            //foreach (var img in ImagesPath.GetFiles())
            //{
            //    var data = Convert.ToBase64String(File.ReadAllBytes(img.FullName));
            //    PicBase64[img.FullName] = $"data:image/{img.Extension.Remove(0,1)};base64," + data;
            //}
            var mdDir = RootPath;

            // ChangeLocalPicToBase64(mdDir);
            await ChangePicToLocal(mdDir);
            //ChangeLocalPicToBase64(mdDir);
        }

        private static async Task ChangePicToLocal(DirectoryInfo mdDir)
        {
            HttpClient http = new HttpClient();
            var mdFiles = mdDir.GetFiles("*.md");
            foreach (var mdFile in mdFiles)
            {
                var mdFileContent = await File.ReadAllTextAsync(mdFile.FullName);

                Console.WriteLine(mdFile.Name);
                var picUrls = GetImageHttpUrls(mdFileContent);
                foreach (var picUrl in picUrls)
                {
                    var newPicName = GetHashString(picUrl.Replace("../","")) + "." + picUrl.Split(".").Last();
                    var picFilePath = Path.Combine(ImagesPath.FullName, newPicName);
                    if (!File.Exists(picFilePath))//如果未下载过,则下载
                    {
                        var fi = new FileInfo(picFilePath);
                        if (fi.Directory != null && !fi.Directory.Exists)
                        {
                            Directory.CreateDirectory(fi.Directory.FullName);
                        }

                        Console.WriteLine($"获取图片:{ picUrl}");
                        var fixedUrl = picUrl;

                        try
                        {
                            var temFilePath = $"image/{(new FileInfo(newPicName)).Name}";
                            if (File.Exists(temFilePath))
                            {
                                await File.WriteAllBytesAsync(picFilePath, File.ReadAllBytes(temFilePath));
                            }
                            else
                            {
                                var picData =
                                       await http.GetByteArrayAsync(
                                           fixedUrl); //https://static001.geekbang.org/resource/image/bf/1a/bfeb7fc556b1fe5f9b768ce5ec90321a.jpg
                                await File.WriteAllBytesAsync(temFilePath, picData);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            var temFilePath = $"image/{(new FileInfo(newPicName)).Name}";
                            if (File.Exists(temFilePath))
                            {
                                await File.WriteAllBytesAsync(picFilePath, File.ReadAllBytes(temFilePath));
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"已存在,跳过下载:{picUrl}");
                    }

                    var relativePath = Path.GetRelativePath(mdDir.FullName, picFilePath).Replace("\\", "/");
                    Console.WriteLine(relativePath);
                    mdFileContent = mdFileContent.Replace(picUrl,
                        /*PicBase64[(new FileInfo(picFilePath)).FullName]*/relativePath);
                }
                var newMdFileName = mdFile.FullName;
                var erweima = @"## 我的公众号

想学习**代码之外的技能**？不妨关注我的微信公众号：**千古壹号**（id：`qianguyihao`）。

扫一扫，你将发现另一个全新的世界，而这将是一场美丽的意外：

![](/image/8D28618A083B97307304B2E3FD019F90.png)

##";
                await File.WriteAllTextAsync(newMdFileName, mdFileContent.Replace(erweima, ""), Encoding.UTF8);
            }

            var subFolder = mdDir.GetDirectories();
            if (subFolder.Length > 0)
            {
                foreach (var directoryInfo in subFolder)
                {
                    await ChangePicToLocal(directoryInfo);
                }
            }
        }
        private static string RelativePath(string absolutePath, string relativeTo)
        {
            string[] absoluteDirectories = absolutePath.Split('\\');
            string[] relativeDirectories = relativeTo.Split('\\');

            //Get the shortest of the two paths
            int length = absoluteDirectories.Length < relativeDirectories.Length ? absoluteDirectories.Length : relativeDirectories.Length;

            //Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            //Find common root
            for (index = 0; index < length; index++)
                if (absoluteDirectories[index] == relativeDirectories[index])
                    lastCommonRoot = index;
                else
                    break;

            //If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
                throw new ArgumentException("Paths do not have a common base");

            //Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            //Add on the ..
            for (index = lastCommonRoot + 1; index < absoluteDirectories.Length; index++)
                if (absoluteDirectories[index].Length > 0)
                    relativePath.Append("..\\");

            //Add on the folders
            for (index = lastCommonRoot + 1; index < relativeDirectories.Length - 1; index++)
                relativePath.Append(relativeDirectories[index] + "\\");
            relativePath.Append(relativeDirectories[relativeDirectories.Length - 1]);

            return relativePath.ToString();
        }
        private static void ChangeLocalPicToBase64(DirectoryInfo mdDir)
        {
            var mdFiles = mdDir.GetFiles("*.md");
            foreach (var mdFile in mdFiles)
            {
                //"![].*?.*?(\\.jpg|\\.jpeg|\\.png|\\.gif)\\)""
                var images = GetImageFilePaths(File.ReadAllText(mdFile.FullName));
                foreach (var image in images)
                {
                    Console.WriteLine(image);
                }
            }

            //递归:
            var subFolder = mdDir.GetDirectories();
            if (subFolder.Length > 0)
            {
                foreach (var directoryInfo in subFolder)
                {
                    ChangeLocalPicToBase64(directoryInfo);
                }
            }
        }
        public static string[] GetImageFilePaths(string content)
        {
            // 定义正则表达式用来匹配 img 标签  
            Regex regImg = new Regex(/*"http.*?png"*/"\\!\\[\\]\\(.*?(\\.jpg|\\.jpeg|\\.png|\\.gif)\\)", RegexOptions.IgnoreCase);

            // 搜索匹配的字符串  
            MatchCollection matches = regImg.Matches(content);

            string[] sUrlList = new string[matches.Count];

            // 取得匹配项列表  
            for (var j = 0; j < matches.Count; j++)
            {
                Match match = matches[j];
                sUrlList[j] = match.Groups[0].Value.Replace("(", "").Replace(")", ""); ;
            }
            Console.WriteLine($"共找到图片URL:{sUrlList.Length}个");
            return sUrlList;
        }
        public static string[] GetImageHttpUrls(string content)
        {
            // 定义正则表达式用来匹配 img 标签  
            Regex regImg = new Regex(/*"http.*?png"*/"http.*?(\\.jpg|\\.jpeg|\\.png|\\.gif)", RegexOptions.IgnoreCase);

            // 搜索匹配的字符串  
            MatchCollection matches = regImg.Matches(content);

            string[] sUrlList = new string[matches.Count];

            // 取得匹配项列表  
            for (var j = 0; j < matches.Count; j++)
            {
                Match match = matches[j];
                sUrlList[j] = match.Groups[0].Value.Replace("(", "").Replace(")", ""); ;
            }
            Console.WriteLine($"共找到图片URL:{sUrlList.Length}个");
            return sUrlList;
        }
        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }

    }
}
