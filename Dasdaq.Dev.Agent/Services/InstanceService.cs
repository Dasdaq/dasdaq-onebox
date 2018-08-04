using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class InstanceService : IDisposable
    {
        private AgentContext _ef;
        private static Dictionary<string, Process> _dic = new Dictionary<string, Process>();

        public InstanceService(AgentContext ef)
        {
            _ef = ef;
        }

        public bool IsInstanceExisted(string name)
        {
            return _dic.ContainsKey(name);
        }

        public Task DownloadAndStartInstanceAsync(string name, InstanceUploadMethod method, string data)
        {
            return Task.Run(() => {
                try
                {
                    switch (method)
                    {
                        case InstanceUploadMethod.Git:
                            CloneGitRepo(name, data);
                            break;
                        case InstanceUploadMethod.Zip:
                            ExtractZip(name, data);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    var process = StartInstance(name);
                    _dic.Add(name, process);
                    process.WaitForExit();
                    var _instance = _ef.Instances.Single(x => x.Name == name);
                    _instance.ExitCode = process.ExitCode;
                    _instance.ExitTime = DateTime.Now;
                    _instance.Status = _instance.ExitCode == 0 ? InstanceStatus.Succeeded : InstanceStatus.Failed;
                    _ef.SaveChanges();
                }
                catch (Exception ex)
                {
                    var _instance = _ef.Instances.Single(x => x.Name == name);
                    _instance.Status = InstanceStatus.Failed;
                    _ef.SaveChanges();
                    Console.WriteLine("[Dasdaq Dev Agent] An error occurred: \r\n" + ex.ToString());
                }
            });
        }

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
            ChmodRunScript(runFile.Single());
            var startInfo = new ProcessStartInfo("bash", $"-c {runFile.Single()}");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = Path.GetDirectoryName(runFile.Single());
            return Process.Start(startInfo);
        }

        private void ChmodRunScript(string runFile)
        {
            var startInfo = new ProcessStartInfo("chmod", $"u+x {runFile}");
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
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

        public void Dispose()
        {
            this._ef.Dispose();
        }
    }
}
