using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

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
            string indexTemplate = File.ReadAllText(Path.Combine(resourcePath, "indexTemplate.html"));

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
            List<string> links = new List<string>();
            for (int i = 0; i < blogs.Count; i++)
            {
                string link = "BLOG_TOKEN" + blogs[i].FileName + ".html";
                links.Add(link);
                sidebar += String.Format("<a href=\"{0}\">{1}</a><br/><hr/>\n", link, blogs[i].Header);
            }

            string indexOutputDirectory = Path.Combine(resourcePath, "output");
            string blogOutputDirectory = Path.Combine(indexOutputDirectory, "blogs");
            if (!Directory.Exists(blogOutputDirectory))
            {
                Directory.CreateDirectory(blogOutputDirectory);
            }

            string indexOutputPath = Path.Combine(indexOutputDirectory, "index.html");
            indexTemplate = indexTemplate.Replace("FIRST_BLOG_URL", Path.Combine("blogs", blogs[0].FileName + ".html"));
            File.WriteAllText(indexOutputPath, indexTemplate);
            for(int i = 0; i < blogs.Count; i++)
            {
                string output = template.Replace("INSERT_BLOG_HERE", blogs[i].Text);
                output = output.Replace("INSERT_SIDEBAR_HERE", sidebar);
                
                if (i == 0)
                {
                    if (blogs.Count != 1)
                    {
                        output = output.Replace("INSERT_BACK_NEXT_LINKS_HERE", "<a href=\"" + links[i + 1] + "\">&lt;-- " + blogs[i + 1].Header + "</a>");
                    }
                }
                else if (i != blogs.Count - 1)
                {
                    output = output.Replace("INSERT_BACK_NEXT_LINKS_HERE", "<a href=\"" + links[i + 1] + "\">&lt;-- " + blogs[i + 1].Header + "</a><span style=\"float:right;\"><a href=\"" + links[i - 1] + "\">" + blogs[i - 1].Header + " --&gt;</a></span>");
                }
                else
                {
                    output = output.Replace("INSERT_BACK_NEXT_LINKS_HERE", "<div style=\"text-align:right\"><a href=\"" + links[i - 1] + "\">" + blogs[i - 1].Header + " --&gt;</a></div>");
                }

                string outputPath = Path.Combine(blogOutputDirectory, blogs[i].FileName + ".html");
                output = output.Replace("BLOG_TOKEN", "")
                                .Replace("INDEX_TOKEN", "../")
                                .Replace("IMAGE_TOKEN", "../images/");
                File.WriteAllText(outputPath, output);
            }

            CopyFilesRecursive(Path.Combine(resourcePath, "extras"), Path.Combine(indexOutputDirectory));
        }

        internal static void CopyFilesRecursive(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }
            foreach (string directory in Directory.EnumerateDirectories(from))
            {
                CopyFilesRecursive(directory, Path.Combine(to, Path.GetFileName(directory)));
            }
            foreach (string file in Directory.EnumerateFiles(from))
            {
                string extension = Path.GetExtension(file);
                if (extension == "png" || extension == "jpg")
                {
                    int MaxImageWidth = 915;
                    // TODO(ian): Warning for images over the max size.

                    //Image img = new Bitmap("test.png");
                    //Console.WriteLine(img.Width + " x " + img.Height);
                }

                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }
    }
}
