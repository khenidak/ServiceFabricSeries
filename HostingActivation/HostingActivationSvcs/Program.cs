#define _USE_SERVICE_FACTORY


using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;

namespace HostingActivationSvcs
{

#if _USE_SERVICE_FACTORY
    class StatefulSvcFactory : IStatefulServiceFactory
    {
        public IStatefulServiceReplica CreateReplica(string serviceTypeName, Uri serviceName, byte[] initializationData, Guid partitionId, long replicaId)
        {
            Debug.WriteLine("****** Stateful Factory");


            // this factory works only with one type of services. 
            // uri will give you hosting app, which you can use in SaaS models where appname maps to data location, reource pool, user identities etc.
            ComputeSvc svc = new ComputeSvc();

            // if i have a super duper DI i can assign it here (or use the constructor). 
            // svc.MySuperDuperDI = CreateDI();
            
            return svc;
            
        }
    }

    class StatelessSvcFactory : IStatelessServiceFactory
    {
        public IStatelessServiceInstance CreateInstance(string serviceTypeName, Uri serviceName, byte[] initializationData, Guid partitionId, long instanceId)
        {
            // just like above only stateless
            Debug.WriteLine("****** Statless Factory");
            return new GatewaySvc();
        }
    }
#endif



    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
#if _USE_SERVICE_FACTORY

                    fabricRuntime.RegisterStatefulServiceFactory("ComputeSvc", new StatefulSvcFactory());
                    fabricRuntime.RegisterStatelessServiceFactory("GatewaySvc", new StatelessSvcFactory());
#else
                    // static registeration for both service type
                    fabricRuntime.RegisterServiceType("ComputeSvc", typeof(ComputeSvc));
                    fabricRuntime.RegisterServiceType("GatewaySvc", typeof(GatewaySvc));

#endif
                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(ComputeSvc).Name);

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e);
                throw;
            }
        }
    }
}
