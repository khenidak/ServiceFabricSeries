using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services;
using System.Diagnostics;
using System.Fabric;

namespace HostingActivationSvcs
{
    public class ComputeSvc : StatefulService
    {
        public ComputeSvc()
        {
            Task.Factory.StartNew(async () =>
            {
                // wait  for the service to complete initlization and ref below are allocated. 
                // we can not have this code on RunAsync since it is only called for primary replicas only
                // if you are on a slower dev machine, increase the delay below to avoid NullRef exceptions
                await Task.Delay(20 * 1000);

                var ServiceName = "ComputeSvc";
                var processID = Process.GetCurrentProcess().Id.ToString();
                var partitionID = ServiceInitializationParameters.PartitionId.ToString();
                var replicaId = ServiceInitializationParameters.ReplicaId.ToString();
                var nodeName = (await FabricRuntime.GetNodeContextAsync(TimeSpan.MaxValue, new CancellationToken())).NodeName;
                var ServiceVer = ServiceInitializationParameters.CodePackageActivationContext.CodePackageVersion;

                var sLog = string.Format("Node:{0} Svc:{1} Proc:{2} P/R:{3}/{4} SvcVer:{5}", nodeName, ServiceName, processID, partitionID, replicaId, ServiceVer);

                while (true)
                {
                    Debug.WriteLine(sLog);
                    await Task.Delay(5 * 1000);
                }

            });
        }
        
        
        protected override ICommunicationListener CreateCommunicationListener()
        {
            //no op
            return base.CreateCommunicationListener();
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // no op
        }
    }
}
