﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Adapters.Dialogflow
{
    public class DialogflowIntentExpression
    {
        public String Id { get; set; }
        public List<DialogflowIntentExpressionPart> Data { get; set; }
        public Boolean IsTemplate { get; set; }
    }
}
