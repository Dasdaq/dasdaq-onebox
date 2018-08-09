using System.ComponentModel.DataAnnotations;

namespace Dasdaq.Dev.Agent.Models
{
    public class Currency
    {
        [Key]
        [MaxLength(64)]
        public string Name { get; set; }

        public string BaseAccount { get; set; }
    }
}
