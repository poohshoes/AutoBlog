using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SiteBuilter
{
    struct BlogInfo
    {
        public string Text;
        public string Header;
        public string FileName;
        public DateTime Date;

        internal static int Comparer(BlogInfo x, BlogInfo y)
        {
            return (int)(y.Date - x.Date).TotalDays;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string resourcePath = "..\\..\\..\\..";
            string template = File.ReadAllText(Path.Combine(resourcePath, "template.html"));

            var blogs = new List<BlogInfo>();
            foreach(string blogFile in Directory.EnumerateFiles(Path.Combine(resourcePath, "blogs")))
            {
                bool inCodeBlock = false;
                var newBlogInfo = new BlogInfo();
                newBlogInfo.Text = "";
                int i = 0;
                foreach(string inputLine in File.ReadAllLines(blogFile))
                {
                    if (i == 0)
                    {
                        newBlogInfo.Date = DateTime.Parse(inputLine);
                        newBlogInfo.Text += "<h6>" + inputLine + "</h6>\n";
                    }
                    else if (i == 1)
                    {
                        newBlogInfo.Header = inputLine;
                        newBlogInfo.Text += "<h2>" + inputLine + "</h2>\n";
                    }
                    else if(inputLine != "")
                    {
                        if (inputLine.Contains("<code>"))
                        {
                            inCodeBlock = true;
                        }

                        int j = 0;
                        string toAdd = "";
                        while (j < inputLine.Length && inputLine[j] == ' ')
                        {
                            j++;
                            toAdd += "&nbsp;";
                        }
                        if (inCodeBlock)
                        {
                            newBlogInfo.Text += toAdd + inputLine.Substring(j) + "<br/>\n";
                        }
                        else
                        {
                            if (j < inputLine.Length && inputLine[j] == '<')
                            {
                                newBlogInfo.Text += inputLine + "\n";
                            }
                            else
                            {
                                newBlogInfo.Text += "<p>" + inputLine + "<p/>\n";
                            }
                        }

                        if (inputLine.Contains("</code>"))
                        {
                            inCodeBlock = false;
                        }
                    }
                    i++;
                }
                newBlogInfo.FileName = Path.GetFileNameWithoutExtension(blogFile);
                blogs.Add(newBlogInfo);
            }

            blogs.Sort(BlogInfo.Comparer);

            string sidebar = "";
            for (int i = 0; i < blogs.Count; i++)
            {
                string link = "BLOG_TOKEN" + blogs[i].FileName + ".html";
                if (i == 0)
                {
                    link = "INDEX_TOKENindex.html";
                }
                sidebar += String.Format("<a href=\"{0}\">{1}</a><br/><hr/>\n", link, blogs[i].Header);
            }

            string indexOutputDirectory = Path.Combine(resourcePath, "output");
            string blogOutputDirectory = Path.Combine(indexOutputDirectory, "blogs");
            if (!Directory.Exists(blogOutputDirectory))
            {
                Directory.CreateDirectory(blogOutputDirectory);
            }
            
            for(int i = 0; i < blogs.Count; i++)
            {
                string output = template.Replace("INSERT_BLOG_HERE", blogs[i].Text);
                output = output.Replace("INSERT_SIDEBAR_HERE", sidebar);
                string outputPath;
                if (i == 0)
                {
                    outputPath = Path.Combine(indexOutputDirectory, "index.html");
                    output = output.Replace("BLOG_TOKEN", "blogs/")
                                   .Replace("INDEX_TOKEN", "")
                                   .Replace("IMAGE_TOKEN", "images/");
                }
                else
                {
                    outputPath = Path.Combine(blogOutputDirectory, blogs[i].FileName + ".html");
                    output = output.Replace("BLOG_TOKEN", "")
                                   .Replace("INDEX_TOKEN", "../")
                                   .Replace("IMAGE_TOKEN", "../images/");
                }
                File.WriteAllText(outputPath, output);
            }

            CopyFilesRecusive(Path.Combine(resourcePath, "extras"), Path.Combine(indexOutputDirectory));
        }

        internal static void CopyFilesRecusive(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }
            foreach (string directory in Directory.EnumerateDirectories(from))
            {
                CopyFilesRecusive(directory, Path.Combine(to, Path.GetFileName(directory)));
            }
            foreach (string file in Directory.EnumerateFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }
    }
}
