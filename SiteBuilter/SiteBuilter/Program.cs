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
        static List<string> Warnings = new List<string>();

        static void Main(string[] args)
        {
            string resourcePath = "..\\..\\..\\..";
            string template = File.ReadAllText(Path.Combine(resourcePath, "template.html"));
            //string indexTemplate = File.ReadAllText(Path.Combine(resourcePath, "indexTemplate.html"));

            List<string> codeBlockLines = new List<string>();
            var blogs = new List<BlogInfo>();
            foreach (string blogFile in Directory.EnumerateFiles(Path.Combine(resourcePath, "blogs")))
            {
                bool inCodeBlock = false;
                var newBlogInfo = new BlogInfo();
                newBlogInfo.Text = "";
                string[] fileLines = File.ReadAllLines(blogFile);
                for (int lineIndex = 0;
                    lineIndex < fileLines.Length;
                    lineIndex++)
                {
                    string inputLine = fileLines[lineIndex];
                    if (lineIndex == 0)
                    {
                        newBlogInfo.Date = DateTime.Parse(inputLine);
                        newBlogInfo.Text += "<h6>" + inputLine + "</h6>\n";
                    }
                    else if (lineIndex == 1)
                    {
                        newBlogInfo.Header = inputLine;
                        newBlogInfo.Text += "<h2>" + inputLine + "</h2>\n";
                    }
                    else if (inputLine != "")
                    {
                        int codeTagEndIndex = inputLine.IndexOf("</code>");
                        if (codeTagEndIndex != -1)
                        {
                            inCodeBlock = false;
                            foreach (string codeBlockLine in FormatCode(codeBlockLines))
                            {
                                newBlogInfo.Text += codeBlockLine + "<br/>\n";
                            }
                            codeBlockLines.Clear();
                        }

                        int j = 0;
                        while (j < inputLine.Length && inputLine[j] == ' ')
                        {
                            j++;
                        }

                        if (inCodeBlock)
                        {
                            codeBlockLines.Add(inputLine);
                        }
                        else
                        {
                            if (j < inputLine.Length && inputLine[j] == '<')
                            {
                                int codeTagIndex = inputLine.IndexOf("<code>");
                                if (codeTagIndex != -1)
                                {
                                    inputLine = inputLine.Insert(codeTagIndex, "<br/>\n");
                                    newBlogInfo.Text += "<div class=\"codeDiv\">\n";
                                }
                                if (codeTagEndIndex != -1)
                                {
                                    newBlogInfo.Text += "</div>\n";
                                }
                                newBlogInfo.Text += inputLine + "\n";
                            }
                            else
                            {
                                newBlogInfo.Text += "<p>" + inputLine + "</p>\n";
                            }
                        }

                        if (inputLine.Contains("<code>"))
                        {
                            inCodeBlock = true;
                        }
                    }
                }
                newBlogInfo.FileName = Path.GetFileNameWithoutExtension(blogFile);
                blogs.Add(newBlogInfo);
            }

            blogs.Sort(BlogInfo.Comparer);

            string indexOutputDirectory = Path.Combine(resourcePath, "output");
            string blogOutputDirectory = Path.Combine(indexOutputDirectory, "blogs");
            if (!Directory.Exists(blogOutputDirectory))
            {
                Directory.CreateDirectory(blogOutputDirectory);
            }

            string sidebar = "";
            List<string> links = new List<string>();
            for (int i = 0; i < blogs.Count; i++)
            {
                string link = "BLOG_TOKEN" + blogs[i].FileName + ".html";
                links.Add(link);
                sidebar += String.Format("<a href=\"{0}\">{1}</a><br/><hr/>\n", link, blogs[i].Header);
            }

            for (int i = 0; i < blogs.Count; i++)
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
                string indexOutput = output;
                output = output.Replace("BLOG_TOKEN", "")
                                .Replace("INDEX_TOKEN", "../")
                                .Replace("IMAGE_TOKEN", "../images/");
                File.WriteAllText(outputPath, output);

                if (i == 0)
                {
                    string indexOutputPath = Path.Combine(indexOutputDirectory, "index.html");
                    indexOutput = indexOutput.Replace("BLOG_TOKEN", "blogs/")
                                    .Replace("INDEX_TOKEN", "")
                                    .Replace("IMAGE_TOKEN", "images/");
                    File.WriteAllText(indexOutputPath, indexOutput);
                }
            }

            CopyFilesRecursive(Path.Combine(resourcePath, "extras"), Path.Combine(indexOutputDirectory));

            if (Warnings.Count > 0)
            {
                foreach (string warning in Warnings)
                {
                    Console.WriteLine(warning);
                }
                Console.ReadLine();
            }
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
                    int MaxImageWidth = 0;
                    // TODO(ian): Warning for images over the max size.

                    //Image img = new Bitmap("test.png");
                    //Console.WriteLine(img.Width + " x " + img.Height);
                }

                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
        }

        private enum TokenType
        {
            Unknown,
            Semicolon,
            LeftParenthesis,
            RightParenthesis,
            LeftBracket,
            RightBracket,
            LeftBrace,
            RightBrace,
            NewLine,
            String,
            SingleLineComment,
        }

        private class Token
        {
            public TokenType Type;
            public string Value = "";
        }

        private static List<string> FormatCode(List<string> codeLines)
        {
            var tokens = new List<Token>();
            Token currentToken = new Token();
            for (int codeLineIndex = 0;
                codeLineIndex < codeLines.Count;
                codeLineIndex++)
            {
                string line = codeLines[codeLineIndex];
                for (int charIndex = 0;
                    charIndex < line.Length;
                    charIndex++)
                {
                    char character = line[charIndex];

                    if (currentToken.Type == TokenType.String)
                    {
                        int numPreviousBackSlashes = 0;
                        for (int backwardsCharIndex = charIndex - 1;
                            backwardsCharIndex >= 0 && line[backwardsCharIndex] == '\\';
                            backwardsCharIndex--)
                        {
                            numPreviousBackSlashes++;
                        }
                        char previousCharacter = line[charIndex - 1];
                        if (character == '"' && (numPreviousBackSlashes % 2) == 0)
                        {
                            tokens.Add(currentToken);
                            currentToken = new Token();
                        }
                        else
                        {
                            currentToken.Value += character;

                            if(charIndex == line.Length - 1)
                            {
                                Warnings.Add("Code Lexer: Multi line strings not handled!");
                            }
                        }
                    }
                    else if (currentToken.Type == TokenType.Unknown)
                    {
                        TokenType newTokenType = TokenType.Unknown;
                        if (character == '"')
                        {
                            newTokenType = TokenType.String;
                        }
                        if (character == ';')
                        {
                            newTokenType = TokenType.Semicolon;
                        }
                        else if (character == '(')
                        {
                            newTokenType = TokenType.LeftParenthesis;
                        }
                        else if (character == ')')
                        {
                            newTokenType = TokenType.RightParenthesis;
                        }
                        else if (character == '{')
                        {
                            newTokenType = TokenType.LeftBrace;
                        }
                        else if (character == '}')
                        {
                            newTokenType = TokenType.RightBrace;
                        }
                        else if (character == '[')
                        {
                            newTokenType = TokenType.LeftBracket;
                        }
                        else if (character == ']')
                        {
                            newTokenType = TokenType.RightBracket;
                        }
                        else if (character == '/' &&
                            charIndex > 0 &&
                            line[charIndex - 1] == '/')
                        {
                            newTokenType = TokenType.SingleLineComment;
                            currentToken.Type = newTokenType;
                            currentToken.Value = line.Substring(charIndex + 1);
                            tokens.Add(currentToken);
                            currentToken = new Token();
                            charIndex = line.Length;
                            tokens.Add(new Token() { Type = TokenType.NewLine });
                        }

                        if (newTokenType == TokenType.Unknown &&
                            !char.IsWhiteSpace(character))
                        {
                            currentToken.Value += character;
                        }

                        if (charIndex == line.Length - 1 ||
                            newTokenType != TokenType.Unknown ||
                            char.IsWhiteSpace(character))
                        {
                            if (currentToken.Value != string.Empty)
                            {
                                tokens.Add(currentToken);
                                currentToken = new Token();
                            }
                        }

                        if (newTokenType == TokenType.Semicolon ||
                            newTokenType == TokenType.LeftParenthesis ||
                            newTokenType == TokenType.RightParenthesis ||
                            newTokenType == TokenType.LeftBrace ||
                            newTokenType == TokenType.RightBrace ||
                            newTokenType == TokenType.LeftBracket ||
                            newTokenType == TokenType.RightBracket)
                        {
                            currentToken.Type = newTokenType;
                            tokens.Add(currentToken);
                            currentToken = new Token();
                        }
                        else if (newTokenType == TokenType.String)
                        {
                            currentToken.Type = newTokenType;
                        }
                    }

                    if (charIndex == line.Length - 1)
                    {
                        Token newLineToken = new Token();
                        newLineToken.Type = TokenType.NewLine;
                        tokens.Add(newLineToken);
                    }
                }
            }

            // Todo(Ian): Put in newlines after closing braces that bring the indent to zero.

            List<string> output = new List<string>();
            string currentLine = "";
            int indentChange = 0;
            int currentIndent = 0;
            for (int tokenIndex = 0;
                tokenIndex < tokens.Count;
                tokenIndex++)
            {
                Token token = tokens[tokenIndex];
                Token nextToken = null;
                if ((tokenIndex + 1) < tokens.Count)
                {
                    nextToken = tokens[tokenIndex + 1];
                }
                string commentColor = "#608b4e";
                string keywordColor = "#569cd6";
                string stringColor = "#d69d83";
                switch (token.Type)
                {
                    case TokenType.SingleLineComment:
                        {
                            string preTag = "<span style=\" color:" + commentColor + " \">//";
                            string postTag = "</span>";
                            currentLine += preTag + token.Value + postTag;
                        }
                        break;
                    case TokenType.String:
                        {
                            string preTag = "<span style=\" color:" + stringColor + " \">";
                            string postTag = "</span>";
                            currentLine += preTag + "\"" + token.Value + "\"" + postTag;
                        }
                        break;
                    case TokenType.Unknown:
                        {
                            string tokenColor = string.Empty;
                            if (token.Value == "if" ||
                                token.Value == "else" ||
                                token.Value == "do" ||
                                token.Value == "while" ||
                                token.Value == "for" ||

                                token.Value == "int" ||
                                token.Value == "string" ||
                                token.Value == "float" ||
                                token.Value == "double" ||
                                token.Value == "String" ||
                                token.Value == "bool" ||
                                token.Value == "boolean" ||
                                token.Value == "byte" ||
                                token.Value == "Vector2" ||
                                token.Value == "List" ||
                                token.Value == "var" ||

                                token.Value == "public" ||
                                token.Value == "private" ||
                                token.Value == "static" ||
                                token.Value == "internal" ||
                                token.Value == "void" ||
                                token.Value == "main" ||
                                token.Value == "abstract" ||
                                token.Value == "virtual" ||
                                token.Value == "override" ||
                                token.Value == "type" ||
                                token.Value == "new" ||
                                token.Value == "try" ||
                                token.Value == "catch" ||

                                token.Value == "package" ||
                                token.Value == "import" ||
                                token.Value == "using" ||
                                token.Value == "include" ||
                                token.Value == "class" ||
                                token.Value == "struct" ||
                                token.Value == "base" ||
                                token.Value == "throws" ||
                                token.Value == "return" ||
                                token.Value == "yeild")
                            {
                                tokenColor = keywordColor;
                            }
                            string preTag = "";
                            string postTag = "";
                            if (tokenColor != string.Empty)
                            {
                                preTag = "<span style=\" color:" + tokenColor + " \">";
                                postTag = "</span>";
                            }

                            currentLine += preTag + token.Value + postTag;
                            if (nextToken != null &&
                                nextToken.Type == TokenType.Unknown)
                            {
                                currentLine += " ";
                            }
                        }
                        break;
                    case TokenType.Semicolon:
                        currentLine += ";";
                        break;
                    case TokenType.LeftParenthesis:
                        currentLine += "(";
                        break;
                    case TokenType.RightParenthesis:
                        currentLine += ")";
                        break;
                    case TokenType.LeftBracket:
                        currentLine += "[";
                        break;
                    case TokenType.RightBracket:
                        currentLine += "]";
                        break;
                    case TokenType.LeftBrace:
                        currentLine += "{";
                        indentChange++;
                        break;
                    case TokenType.RightBrace:
                        currentLine += "}";
                        currentIndent--;
                        break;
                    case TokenType.NewLine:
                        string oneIndent = "&nbsp;&nbsp;&nbsp;&nbsp;";
                        string toAdd = "";
                        for (int indentIndex = 0;
                            indentIndex < currentIndent;
                            indentIndex++)
                        {
                            toAdd += oneIndent;
                        }
                        output.Add(toAdd + currentLine);
                        currentLine = "";
                        currentIndent += indentChange;
                        indentChange = 0;
                        break;
                }
            }

            return output;
        }
    }
}
