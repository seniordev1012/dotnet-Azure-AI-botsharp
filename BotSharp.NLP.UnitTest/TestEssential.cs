﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;

namespace BotSharp.NLP.UnitTest
{
    public abstract class TestEssential
    {
        protected IConfiguration Configuration { get; }

        public TestEssential()
        {
            var rootDir = Path.GetFullPath($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}");
            var settingsDir = Path.Combine(rootDir, "BotSharp.WebHost", "Settings");

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            var settings = Directory.GetFiles(settingsDir, "*.json");
            settings.ToList().ForEach(setting =>
            {
                configurationBuilder.AddJsonFile(setting, optional: false, reloadOnChange: true);
            });
            Configuration = configurationBuilder.Build();
        }
    }


}
