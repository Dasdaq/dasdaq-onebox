using System.ComponentModel.DataAnnotations;

namespace Dasdaq.Dev.Agent.Models
{
    public class Account
    {
        [Key]
        [MaxLength(64)]
        public string Name { get; set; }
    }
}
