﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class AIResponseCustomPayload : AIResponseMessageBase
    {
        public string Task { get; set; }

        public List<String> Parameters { get; set; }
    }
}
