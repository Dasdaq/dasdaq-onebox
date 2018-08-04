using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Dasdaq.Dev.Agent.Models
{
    public enum ContractStatus
    {
        Updating,
        Available,
        Failed
    }

    public class Contract
    {
        [Key]
        public string Name { get; set; }

        public string Cpp { get; set; }

        public string Hpp { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ContractStatus Status { get; set; }

        public DateTime DeployedTime { get; set; } = DateTime.Now;
    }
}
