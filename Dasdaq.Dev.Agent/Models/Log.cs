using System;

namespace Dasdaq.Dev.Agent.Models
{
    public class Log
    {
        public string Text { get; set; }

        public bool IsError { get; set; }

        public DateTime Time { get; set; }

        public int ProcessId { get; set; }
    }
}
