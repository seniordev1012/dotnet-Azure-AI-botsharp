﻿using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Platform.Abstraction
{
    /// <summary>
    /// Platform abstraction
    /// Implement this interface to build a Chatbot platform
    /// </summary>
    public interface IPlatformBuilder<TStorage, TAgent, TExtraData, TEntity> 
        where TStorage : IAgentStorage<TExtraData, TEntity>, new()
    {
        /// <summary>
        /// Parse options for the incoming text or voice request from the sender.
        /// </summary>
        // DialogRequestOptions RequestOptions { get; set; }

        /// <summary>
        /// Convert platform specific agent to standard agent format
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        StandardAgent<TExtraData, TEntity> StandardizeAgent(TAgent agent);

        /// <summary>
        /// Recover standard agent to specific agent format
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        TAgent RecoverAgent(StandardAgent<TExtraData, TEntity> agent);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStorage"></typeparam>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool SaveAgent(StandardAgent<TExtraData, TEntity> agent);

        StandardAgent<TExtraData, TEntity> GetAgentById(string agentId);
    }
}
