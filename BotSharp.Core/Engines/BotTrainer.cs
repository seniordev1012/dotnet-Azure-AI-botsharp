﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace BotSharp.Core.Engines
{
    public class BotTrainer
    {
        private Database dc;

        private string agentId;

        public BotTrainer(string agentId, Database dc)
        {
            this.dc = dc;
            this.agentId = agentId;
        }

        public async Task<string> Train(Agent agent)
        {
            agent.Intents = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters).ThenInclude(x => x.Prompts)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)
                .Include(x => x.UserSays).ThenInclude(x => x.Data)
                .Where(x => x.AgentId == agentId)
                .ToList();

            var data = JObject.FromObject(new
            {
            });

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");
            var platform = config.GetSection($"BotPlatform").Value;
            string providerName = config.GetSection($"{platform}:Provider").Value;
            var provider = TypeHelper.GetInstance(providerName, assemblies) as INlpPipeline;
            provider.Configuration = config.GetSection(platform);

            var pipeModel = new PipeModel
            {
                Name = providerName,
                Class = provider.ToString(),
                Meta = new JObject(),
                Time = DateTime.UtcNow
            };

            await provider.Train(agent, data, pipeModel);

            var meta = new ModelMetaData
            {
                Platform = platform,
                Language = agent.Language,
                TrainingDate = DateTime.UtcNow,
                Version = config.GetValue<String>($"Version"),
                Pipeline = new List<PipeModel>() { pipeModel }
            };

            var dirTrain = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "TrainingFiles", agent.Id);
            if (!Directory.Exists(dirTrain))
            {
                Directory.CreateDirectory(dirTrain);
            }

            var dirModel = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "ModelFiles", agent.Id);
            if (!Directory.Exists(dirModel))
            {
                Directory.CreateDirectory(dirModel);
            }

            // pipe process
            var pipelines = provider.Configuration.GetSection($"Pipe").Value
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(async pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, assemblies) as INlpPipeline;
                pipe.Configuration = provider.Configuration;
                pipeModel = new PipeModel
                {
                    Name = pipeName,
                    Class = pipe.ToString(),
                    Time = DateTime.UtcNow
                };
                meta.Pipeline.Add(pipeModel);

                await pipe.Train(agent, data, pipeModel);
            });

            // save model meta data
            var dir = Path.Join(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "ModelFiles", agent.Id);
            var metaJson = JsonConvert.SerializeObject(meta, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
            File.WriteAllText(Path.Join(dir, "metadata.json"), metaJson);

            Console.WriteLine(metaJson);

            return metaJson;
        }
    }
}
