﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class DialogflowIntentResponseMessage : AIResponseMessageBase
    {
        public Object Speech { get; set; }
        public Object Payload { get; set; }
    }
}
