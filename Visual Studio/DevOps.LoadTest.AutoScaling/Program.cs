namespace DevOps.LoadTest.AutoScaling
{
    using System;

    public class Program
    {
        static void Main(string[] args)
        {
            var autoScaler = new WebsiteAutoscalerHelper();
            var settings = autoScaler.CreateCpuSettings(2, 10);
            autoScaler.CreateOrUpdateSettings(settings);
            Console.ReadLine();
        }
    }
}
