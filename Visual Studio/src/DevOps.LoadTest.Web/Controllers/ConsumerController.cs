namespace DevOps.LoadTest.Web.Controllers
{
    using Microsoft.AspNet.Mvc;
    using Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;

    public class ConsumerController : Controller
    {
        public IActionResult Cpu()
        {
            return View(new CpuConsumeRequest { Percentage = 50, Seconds = 10 });
        }

        public IActionResult Memory()
        {
            return View(new MemoryConsumeRequest { Megabytes = 512, Seconds = 10 });
        }

        [HttpPost]
        public IActionResult ConsumeCpu(CpuConsumeRequest request)
        {
            ConsumeInAllCpus(request.Percentage, request.Seconds);
            ViewBag.Message = $"Consumed {request.Percentage:#00}% of CPU for {request.Seconds:#00} seconds";
            return View("Cpu", request);
        }

        [HttpPost]
        public IActionResult ConsumeMemory(MemoryConsumeRequest request)
        {
            ConsumeMemory(request.Megabytes, request.Seconds);
            ViewBag.Message = $"Consumed {request.Megabytes:#00}Mb of Memory for {request.Seconds:#00} seconds";
            return View("Memory", request);
        }

        private static void ConsumeInAllCpus(int percentage, int seconds)
        {
            var cancel = false;
            var threads = new List<Thread>();
            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                Thread t = new Thread(new ParameterizedThreadStart(_ => ConsumeInOneCpu(percentage, ref cancel)));
                t.Start();
                threads.Add(t);
            }

            Thread.Sleep(seconds * 1000);
            cancel = true;
        }

        private static void ConsumeInOneCpu(int percentage, ref bool cancel)
        {
            if (percentage < 0 || percentage > 100) throw new ArgumentException("percentage");

            var watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                // Make the loop go on for "percentage" milliseconds then sleep the 
                // remaining percentage milliseconds. So 40% utilization means work 40ms and sleep 60ms
                if (watch.ElapsedMilliseconds > percentage)
                {
                    Thread.Sleep(100 - percentage);
                    watch.Reset();
                    watch.Start();
                }

                if (cancel) return;
            }
        }

        private static void ConsumeMemory(int megabytes, int seconds)
        {
            var list = new List<byte[]>();
            var random = new Random();
            for (var i = 0; i < megabytes; i++)
            {
                var buffer = new byte[1024 * 1024];
                random.NextBytes(buffer);
                list.Add(buffer);
            }

            Thread.Sleep(seconds * 1000);
            Trace.WriteLine(list.Count + " megabytes");
            GC.SuppressFinalize(list);
            GC.Collect();
        }
    }
}
