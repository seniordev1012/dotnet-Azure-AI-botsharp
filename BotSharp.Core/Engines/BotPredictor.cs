﻿using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Core.Models;
using DotNetToolkit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines
{
    public class BotPredictor
    {
        public async Task<NlpDoc> Predict(Agent agent, AIRequest request)
        {
            // load model
            var dir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id, request.Model);
            Console.WriteLine($"Load model from {dir}");
            var metaJson = File.ReadAllText(Path.Combine(dir, "metadata.json"));
            var meta = JsonConvert.DeserializeObject<ModelMetaData>(metaJson);

            // Get NLP Provider
            var config = (IConfiguration)AppDomain.CurrentDomain.GetData("Configuration");
            var assemblies = (string[])AppDomain.CurrentDomain.GetData("Assemblies");

            var providerPipe = meta.Pipeline.First();
            var provider = TypeHelper.GetInstance(providerPipe.Name, assemblies) as INlpProvider;
            provider.Configuration = config.GetSection(meta.Platform);

            var data = new NlpDoc
            {
                Sentences = new List<NlpDocSentence>
                {
                    new NlpDocSentence
                    {
                        Text = request.Query.FirstOrDefault()
                    }
                }
            };

            await provider.Load(agent, providerPipe);
            meta.Pipeline.RemoveAt(0);

            var settings = new PipeSettings
            {
                ModelDir = dir,
                ProjectDir = Path.Combine(AppDomain.CurrentDomain.GetData("DataPath").ToString(), "Projects", agent.Id),
                AlgorithmDir = Path.Combine(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms")
            };


            // pipe process
            var pipelines = provider.Configuration.GetValue<String>($"Pipe:predict")
                .Split(',')
                .Select(x => x.Trim())
                .ToList();

            pipelines.ForEach(async pipeName =>
            {
                var pipe = TypeHelper.GetInstance(pipeName, assemblies) as INlpPredict;
                pipe.Configuration = provider.Configuration;
                pipe.Settings = settings;
                await pipe.Predict(agent, data, meta.Pipeline.FirstOrDefault(x => x.Name == pipeName));
            });

            Console.WriteLine(JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));

            return data;
        }
    }
}
