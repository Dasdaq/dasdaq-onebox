using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class OneBoxProcess : IDisposable
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Alias { get; set; }
        public DateTime StartTime { get; set; } = DateTime.Now;
        public Process Process { get; set; }
        public List<Log> Logs { get; set; } = new List<Log>();

        public void Dispose()
        {
            Process.Dispose();
        }
    }

    public class ProcessService
    {
        private Dictionary<Guid, OneBoxProcess> _dic = new Dictionary<Guid, OneBoxProcess>();

        public ProcessService()
        {
        }

        public OneBoxProcess StartProcess(ProcessStartInfo startInfo, Action<Guid, Log> pushLogFunc = null, string alias = null)
        {
            var ret = new OneBoxProcess() { Alias = alias };
            var process = new Process();
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            process.StartInfo = startInfo;
            process.ErrorDataReceived += (sender, e) => {
                var log = new Log
                {
                    IsError = true,
                    ProcessId = process.Id,
                    Text = e.Data
                };
                ret.Logs.Add(log);
                pushLogFunc?.Invoke(ret.Id, log);
            };
            process.OutputDataReceived += (sender, e) => {
                var log = new Log
                {
                    IsError = false,
                    ProcessId = process.Id,
                    Text = e.Data
                };
                ret.Logs.Add(log);
                pushLogFunc?.Invoke(ret.Id, log);
            };
            _dic.Add(ret.Id, ret);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return ret;
        }

        public void KillProcessTree(int parentPid)
        {
            foreach (var x in GetChildProcessId(GetProcesses(), parentPid))
            {
                var startInfo = new ProcessStartInfo("kill", $"-9 {parentPid}");
                startInfo.UseShellExecute = false;
                var process = Process.Start(startInfo);
                process.WaitForExit();
            }
        }

        public OneBoxProcess FindOneBoxProcessById(Guid id)
        {
            if (!_dic.ContainsKey(id))
            {
                return null;
            }
            else
            {
                return _dic[id];
            }
        }

        private Dictionary<int, int> GetProcesses()
        {
            var startInfo = new ProcessStartInfo("ps", $"-ef");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
            var lines = CombineSpace(output.Replace("\r\n", "\n")).Split('\n');
            var dic = new Dictionary<int, int>();
            foreach (var line in lines.Skip(1))
            {
                try
                {
                    var columns = line.Split(' ');
                    dic.Add(Convert.ToInt32(columns[1]), Convert.ToInt32(columns[2]));
                }
                catch
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    Console.WriteLine("[Dasdaq Dev Agent] Failed to parse 'ps -ef' line: " + line);
                    throw;
                }
            }
            return dic;
        }

        private string CombineSpace(string src)
        {
            src = src.Replace("\t", " ");
            for (var i = 20; i >= 2; --i)
            {
                src = src.Replace(GenerateSpace(i), " ");
            }
            return src;
        }

        private string GenerateSpace(int count)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < count; i++)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private IEnumerable<int> GetChildProcessId(Dictionary<int, int> dic, int parentId)
        {
            yield return parentId;
            foreach (var x in dic.Where(x => x.Value == parentId))
            {
                foreach (var y in GetChildProcessId(dic, x.Key))
                {
                    yield return y;
                }
            }
        }
    }
}
