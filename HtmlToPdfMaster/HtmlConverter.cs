using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HtmlToPdfMaster
{
    /// <summary>
    /// Html Converter. Converts HTML string and URLs to image bytes
    /// </summary>
    public class HtmlConverter
    {
        private static string toolFilename = "wkhtmltopdf";
        private static string directory;
        private static string toolFilepath;

        static HtmlConverter()
        {
            directory = AppContext.BaseDirectory;

            //Check on what platform we are
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                toolFilepath = Path.Combine(directory, toolFilename + ".exe");

                if (!File.Exists(toolFilepath))
                {
                    var assembly = typeof(HtmlConverter).GetTypeInfo().Assembly;
                    var type = typeof(HtmlConverter);
                    var ns = type.Namespace;

                    using (var resourceStream = assembly.GetManifestResourceStream($"{ns}.{toolFilename}.exe"))
                    using (var fileStream = File.OpenWrite(toolFilepath))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                //Check if wkhtmltoimage package is installed on this distro in using which command
                Process process = Process.Start(new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = "/bin/",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = "/bin/bash",
                    Arguments = "which wkhtmltopdf"

                });
                string answer = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(answer) && answer.Contains("wkhtmltopdf"))
                {
                    toolFilepath = "wkhtmltopdf";
                }
                else
                {
                    throw new Exception("wkhtmltoimage does not appear to be installed on this linux system according to which command; go to https://wkhtmltopdf.org/downloads.html");
                }
            }
            else
            {
                //OSX not implemented
                throw new Exception("OSX Platform not implemented yet");
            }
        }

        /// <summary>
        /// Converts a HTML-String into an PDF-File as Byte format.
        /// </summary>
        /// <param name="html">Raw HTLM as String.</param>        
        /// <param name="width">Width in mm.</param>
        /// <param name="height">Height in mm</param>
        /// <param name="encoding">Set the default text encoding, for input.</param>
        /// <returns></returns>
        public static byte[] FromHtmlString(string html, int width, int height,int quality=100,int dpi=100, int margin_left=0, int margin_top=0, int margin_right = 0 , int margin_bottom = 0, string encoding="UTF-8")
        {
            var filename = Path.Combine(directory, $"{Guid.NewGuid()}.html");
            File.WriteAllText(filename, html);
            var bytes = FromUrl(filename, width, height,quality,dpi, margin_left, margin_top, margin_right, margin_bottom, encoding);
            File.Delete(filename);
            return bytes;
        }

        /// <summary>
        /// Converts a HTML-Page into an PDF-File as Byte format.
        /// </summary>
        /// <param name="url">Valid http(s)://example.com URL</param>        
        /// <param name="width">Width in mm.</param>
        /// <param name="height">Height in mm</param>
        /// <param name="encoding">Set the default text encoding, for input.</param>
        /// <returns></returns>
        public static byte[] FromUrl(string url, int width, int height,int quality=100,int dpi=100, int margin_left = 0, int margin_top = 0, int margin_right = 0, int margin_bottom = 0, string encoding="UTF-8")
        {
            var filename = Path.Combine(directory, $"{Guid.NewGuid().ToString()}.pdf");

            string args;

            if (IsLocalPath(url))
            {
                args = $" --encoding {encoding} --page-height {height} --page-width {width} --image-quality {quality} --dpi {dpi} --disable-smart-shrinking --margin-bottom {margin_bottom} --margin-left {margin_left} --margin-right {margin_right} --margin-top {margin_top} \"{url}\" \"{filename}\"";
            }
            else
            {
                args = $"--encoding {encoding} --page-height {height} --page-width {width} --image-quality {quality} --dpi {dpi} --disable-smart-shrinking --margin-bottom {margin_bottom} --margin-left {margin_left} --margin-right {margin_right} --margin-top {margin_top} {url} \"{filename}\"";
            }

            Process process = Process.Start(new ProcessStartInfo(toolFilepath, args)
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = directory,
                RedirectStandardError = true
            });

            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.WaitForExit();

            if (File.Exists(filename))
            {
                var bytes = File.ReadAllBytes(filename);
                File.Delete(filename);
                return bytes;
            }

            throw new Exception("Something went wrong. Please check input parameters");
        }

        private static bool IsLocalPath(string path)
        {
            if (path.StartsWith("http"))
            {
                return false;
            }

            return new Uri(path).IsFile;
        }

        private static void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            throw new Exception(e.Data);
        }
    }
}