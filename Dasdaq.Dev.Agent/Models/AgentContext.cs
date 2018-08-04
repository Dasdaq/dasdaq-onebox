using Microsoft.EntityFrameworkCore;

namespace Dasdaq.Dev.Agent.Models
{
    public class AgentContext : DbContext
    {
        public AgentContext(DbContextOptions opt) : base(opt)
        {
        }

        public DbSet<Contract> Contracts { get; set; }

        public DbSet<Instance> Instances { get; set; }
    }
}
