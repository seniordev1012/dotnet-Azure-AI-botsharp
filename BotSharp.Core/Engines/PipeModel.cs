﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Engines
{
    public class PipeModel
    {
        /// <summary>
        /// Pipe name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pipe type name
        /// </summary>
        public string Class { get; set; }

        public DateTime Time { get; set; }

        public string Model { get; set; }

        /// <summary>
        /// Extra meta data according to pipe
        /// </summary>
        public JObject Meta { get; set; }
    }
}
