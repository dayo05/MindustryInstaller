using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindustryLauncher
{
    class Program
    {
        static void Main(string[] args)
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = @"cmd.exe",
                Arguments= @"/c start /b java\bin\java -jar Mindustry.jar > " + Path.GetTempPath() + @"MindustryLog.txt",
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory()
            });
        }
    }
}
