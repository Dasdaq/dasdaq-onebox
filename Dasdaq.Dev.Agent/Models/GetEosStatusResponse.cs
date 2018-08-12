using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dasdaq.Dev.Agent.Models
{
    public enum LaunchStatus
    {
        未启动,
        正在启动,
        正在运行,
        启动失败
    }

    public class GetEosStatusResponse
    {
        public string Status { get; set; }

        public string ChainId { get; set; }

        public Guid? LogStreamId { get; set; }
    }
}
