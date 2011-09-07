using System;
using System.ServiceModel;
using System.ServiceModel.Activation;
using ServiceProxy.SampleServices;

namespace ServiceProxy.DemoApplication
{
    public class CustomServiceHostFactory : ServiceHostFactory
    {
        private static readonly ServiceProxyFactory ServiceProxyFactory = new ServiceProxyFactory();
        
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            Type serviceProxy = ServiceProxyFactory.GetProxyType(serviceType, () => new SampleService(new Logger()));
            var serviceHost = new ServiceHost(serviceProxy,baseAddresses);
            serviceHost.Description.Name = serviceType.Name;
            return serviceHost;
        }
    }
}