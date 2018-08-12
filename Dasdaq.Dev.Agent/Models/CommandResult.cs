namespace Dasdaq.Dev.Agent.Models
{
    public class CommandResult
    {
        public bool IsSucceeded { get; set; }
        public string StandardOutput { get; set; }
        public string ErrorOutput { get; set; }
        public int ExitCode { get; set; }
    }
}
