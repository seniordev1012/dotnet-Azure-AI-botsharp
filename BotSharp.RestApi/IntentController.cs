﻿using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.RestApi
{
    public class IntentController : EssentialController
    {
        /// <summary>
        /// User intent, a intent is connected to a dialog flow.
        /// </summary>
        /// <param name="agentId"></param>
        /// <param name="intent"></param>
        /// <returns></returns>
        [HttpPost("{agentId}")]
        public string CreateIntent([FromRoute] String agentId, [FromBody] Intent intent)
        {
            var agent = dc.Agent(agentId);

            return intent.Id;
        }
    }
}
