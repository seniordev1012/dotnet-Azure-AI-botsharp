﻿using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Abstraction
{
    public interface IAgentImporter<TAgent>
    {
        /// <summary>
        /// data dir
        /// </summary>
        string AgentDir { get; set; }

        /// <summary>
        /// Load agent summary
        /// </summary>
        /// <returns></returns>
        TAgent LoadAgent(AgentImportHeader agentHeader);

        /// <summary>
        /// Load user customized entity type which defined in dictionary
        /// </summary>
        /// <param name="agent"></param>
        void LoadCustomEntities(TAgent agent);

        /// <summary>
        /// Load user customized intents
        /// </summary>
        /// <param name="agent"></param>
        void LoadIntents(TAgent agent);

        /// <summary>
        /// Add entities that labeled in intent.UserSays into user customized entity dictionary 
        /// </summary>
        /// <param name="agent"></param>
        void LoadBuildinEntities(TAgent agent);
    }
}
