﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class AIResponseStatus
    {
        public int Code { get; set; }

        public string ErrorType { get; set; }

        public string ErrorDetails { get; set; }

        public string ErrorID { get; set; }
    }
}
