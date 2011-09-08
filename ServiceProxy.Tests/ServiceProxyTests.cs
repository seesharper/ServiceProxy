using System;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceProxy.SampleServices;

namespace ServiceProxy.Tests
{
    [TestClass]
    public class ServiceProxyTests
    {
        private static SampleService CreateSampleService()
        {
            return new SampleService(new Logger());
        }
        
        [TestMethod]
        public void GetProxyType_SampleService_ReturnsProxyImplementingInterface()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            Type proxyType = serviceProxyFactory.GetProxyType(typeof(ISampleService), CreateSampleService);
            Assert.IsTrue(typeof(ISampleService).IsAssignableFrom(proxyType));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetProxyType_ConcreteType_ThrowsException()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            serviceProxyFactory.GetProxyType(typeof(SampleService), CreateSampleService);
        }


        [TestMethod]
        public void GetProxyType_SampleService_ReturnsProxyWithStaticFactoryDelegate()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            Type proxyType = serviceProxyFactory.GetProxyType(typeof(ISampleService), CreateSampleService);
            var targetFactoryProperty = proxyType.GetField("TargetFactory", BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(targetFactoryProperty);
        }

        [TestMethod]
        public void GetProxyType_SampleService_ReturnCreateableProxy()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            Type proxyType = serviceProxyFactory.GetProxyType(typeof(ISampleService), CreateSampleService);
            var instance = Activator.CreateInstance(proxyType);
            Assert.IsInstanceOfType(instance, typeof(ISampleService));
        }

        [TestMethod]
        public void GetProxyType_SampleService_ReturnsCallableProxy()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            Type proxyType = serviceProxyFactory.GetProxyType(typeof(ISampleService), CreateSampleService);
            var instance = (ISampleService)Activator.CreateInstance(proxyType);
            var result = instance.Calculate(2, 2);
            Assert.AreEqual(4, result);
        }

        [TestMethod]
        public void GetProxyType_SampleService_CanBePassedToServiceHostConstructor()
        {
            var serviceProxyFactory = new ServiceProxyFactory();
            Type serviceProxy = serviceProxyFactory.GetProxyType(typeof (ISampleService), () => new SampleService(new Logger()));
            ServiceHost serviceHost = new ServiceHost(serviceProxy, new Uri(@"net.pipe://localhost/SampleService.svc")); 
            serviceHost.Open();
            ChannelFactory<ISampleService> channelFactory = new ChannelFactory<ISampleService>(new NetNamedPipeBinding());
            var client = channelFactory.CreateChannel(new EndpointAddress(@"net.pipe://localhost/SampleService.svc"));
            var result = client.Calculate(2, 2);
            Assert.AreEqual(4,result);
            serviceHost.Close();

        }


    }
}
