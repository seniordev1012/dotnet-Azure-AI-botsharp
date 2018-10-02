﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace BotSharp.WebHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;
                    Console.WriteLine($"ContentRootPath: {env.ContentRootPath}");
                    string dir = Path.GetFullPath(env.ContentRootPath);
                    string settingsFolder = Path.Combine(dir, "Settings");

                    // locate setting folder
                    if (!Directory.Exists(settingsFolder))
                    {
                        dir = Path.GetFullPath(env.ContentRootPath + "/..");
                    }

                    settingsFolder = Path.Combine(dir, "Settings");

                    if (!Directory.Exists(settingsFolder))
                    {
                        dir = Path.GetFullPath(env.ContentRootPath + "/bin");
                    }

                    settingsFolder = Path.Combine(dir, "Settings");

                    Console.WriteLine($"Read settings from {settingsFolder}");

                    var settings = Directory.GetFiles(settingsFolder, "*.json");
                    settings.ToList().ForEach(setting =>
                    {
                        config.AddJsonFile(setting, optional: false, reloadOnChange: true);
                    });
                })
#if ARTICULATE
                .UseUrls("http://0.0.0.0:7500")
#elif RASA
                .UseUrls("http://0.0.0.0:5000")
#else        
                .UseUrls("http://0.0.0.0:3112")
#endif
                .UseStartup<Startup>()
                .Build();
    }
}
