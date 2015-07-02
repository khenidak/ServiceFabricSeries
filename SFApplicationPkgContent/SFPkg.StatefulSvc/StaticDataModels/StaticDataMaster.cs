using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFPkg.StatefulSvc
{
    public class State
    {
        public string Name {get;set;}
        public string Code { get; set; }
    }
    public class Zone
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }

    /// <summary>
    /// poco classes for Static data in \svcData\StaticDataMaster.Json
    /// </summary>
    class StaticDataMaster
    {
        public StaticDataMaster()
        {
            this.Zones = new List<Zone>();
            this.States = new List<State>();
        }
        public List<State> States { get; set; }
        public List<Zone> Zones { get; set; }
    }
}
