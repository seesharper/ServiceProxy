using System.ServiceModel;

namespace ServiceProxy.SampleServices
{
    [ServiceContract]
    public interface ISampleService
    {
        [OperationContract]
        int Calculate(int value1, int value2);
    }

    public class SampleService : ISampleService
    {
        public SampleService(ILogger logger)
        {
        }

        public int Calculate(int value1, int value2)
        {
            return value1 + value2;
        }
    }


    public interface ILogger
    {
        void Log(string message);
    }

    public class Logger : ILogger
    {
        public void Log(string message)
        {
            //Perform some logging
        }
    }
}
