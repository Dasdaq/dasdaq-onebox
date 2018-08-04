using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Dasdaq.Dev.Agent.Services
{
    public class InstanceService
    {
        public void ExtractZip(string name, string data)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Extracting zip file: {name}.zip");
            var workDirectory = EnsureWorkingDirectory(name);
            var bytes = Convert.FromBase64String(data);
            var zipFilePath = Path.Combine(workDirectory, name + ".zip");
            File.WriteAllBytes(workDirectory, bytes);
            using (var zip = new ZipArchive(File.OpenRead(zipFilePath)))
            {
                zip.ExtractToDirectory(workDirectory, true);
            }
        }

        public void CloneGitRepo(string name, string data)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Cloning instance repo: {name} {data}.");
            var workDirectory = EnsureWorkingDirectory(name);
            var startInfo = new ProcessStartInfo("git", $"clone {data}");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workDirectory;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        public Process StartInstance(string name)
        {
            var workDirectory = GetWorkingDirectory(name);
            var runFile = Directory.EnumerateFiles(workDirectory, "run.sh", SearchOption.AllDirectories);
            if (runFile.Count() != 1)
            {
                throw new InvalidDataException("Please make sure there will be only 1 run.sh file in the instance files.");
            }
            var startInfo = new ProcessStartInfo("bash", $"-c {runFile.Single()}");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workDirectory;
            return Process.Start(startInfo);
        }

        private string GetWorkingDirectory(string name)
        {
            return Path.Combine(EosService._tempFolderPath, name);
        }

        private string EnsureWorkingDirectory(string name)
        {
            var workDirectory = GetWorkingDirectory(name);
            if (Directory.Exists(workDirectory))
            {
                Directory.Delete(workDirectory, true);
            }
            Directory.CreateDirectory(workDirectory);
            return workDirectory;
        }
    }
}
