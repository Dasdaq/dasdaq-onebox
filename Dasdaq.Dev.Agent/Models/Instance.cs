using System;
using System.ComponentModel.DataAnnotations;

namespace Dasdaq.Dev.Agent.Models
{
    public enum InstanceStatus
    {
        Running,
        Succeeded,
        Failed
    }

    public enum InstanceUploadMethod
    {
        Zip,
        Git
    }

    public class Instance
    {
        [Key]
        public string Name { get; set; }

        public int? ExitCode { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;

        public DateTime? ExitTime { get; set; }

        public InstanceStatus Status { get; set; }

        public string StartScript { get; set; }

        public InstanceUploadMethod UploadMethod { get; set; }

        public string Data { get; set; }
    }
}
