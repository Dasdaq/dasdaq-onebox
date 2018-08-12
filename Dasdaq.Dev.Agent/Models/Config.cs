using System.Collections.Generic;

namespace Dasdaq.Dev.Agent.Models
{
    public class Config
    {
        public ConfigEos eos { get; set; }
        
        public IEnumerable<string> dapp { get; set; }
    }

    public class ConfigEos
    {
        public IEnumerable<string> plugins { get; set; }

        public ConfigEosKeyPair keyPair { get; set; }

        public ConfigEosContracts contracts { get; set; }
    }

    public class ConfigEosKeyPair
    {
        public string publicKey { get; set; }
        public string privateKey { get; set; }
    }

    public class ConfigEosContracts
    {
        public string git { get; set; }
        public string folder { get; set; }
    }
}
