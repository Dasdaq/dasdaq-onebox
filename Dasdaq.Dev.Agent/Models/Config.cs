using System.Collections.Generic;

namespace Dasdaq.Dev.Agent.Models
{
    public class Config
    {
        public string PublicKey { get; set; }

        public string PrivateKey { get; set; }

        public string Contracts { get; set; }

        public string ContractsPath { get; set; }

        public IEnumerable<string> Instances { get; set; }
    }
}
