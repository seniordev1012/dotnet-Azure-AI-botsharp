﻿using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.MachineLearning.NLP;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.CRFsuite
{
    public class CRFsuiteEntityRecognizer : INlpPipeline
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public async Task<bool> Train(Agent agent, JObject data, PipeModel meta)
        {
            var dc = new DefaultDataContextLoader().GetDefaultDc();
            var corpus = agent.Corpus;

            meta.Model = "ner-crf.model";

            List<List<NlpToken>> tokens = data["Tokens"].ToObject<List<List<NlpToken>>>();
            List<TrainingIntentExpression<TrainingIntentExpressionPart>> userSays = corpus.UserSays;
            List<List<TrainingData>> list = new List<List<TrainingData>>();

            string rawTrainingDataFileName = Path.Join(Settings.TrainDir, "ner-crf.corpus.txt");
            string parsedTrainingDataFileName = Path.Join(Settings.TrainDir, "ner-crf.parsed.txt");
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);

            using (FileStream fs = new FileStream(rawTrainingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        List<TrainingData> curLine = Merge(tokens[i], userSays[i].Entities);
                        curLine.ForEach(trainingData =>
                        {
                            string[] wordParams = { trainingData.Entity, trainingData.Token, trainingData.Pos, trainingData.Chunk };
                            string wordStr = string.Join(" ", wordParams);
                            sw.Write(wordStr + "\n");
                        });
                        list.Add(curLine);
                        sw.Write("\n");
                    }
                    sw.Flush();
                }
            }

            var fields = Configuration.GetValue<String>($"CRFsuiteEntityRecognizer:fields");
            var uniFeatures = Configuration.GetValue<String>($"CRFsuiteEntityRecognizer:uniFeatures");
            var biFeatures = Configuration.GetValue<String>($"CRFsuiteEntityRecognizer:biFeatures");

            new MachineLearning.CRFsuite.Ner()
                .NerStart(rawTrainingDataFileName, parsedTrainingDataFileName, fields, uniFeatures.Split(" "), biFeatures.Split(" "));

            var algorithmDir = Path.Join(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms");

            CmdHelper.Run(Path.Join(algorithmDir, "crfsuite"), $"learn -m {modelFileName} {parsedTrainingDataFileName}"); // --split=3 -x

            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["fields"] = fields;
            meta.Meta["uniFeatures"] = uniFeatures;
            meta.Meta["biFeatures"] = biFeatures;

            return true;
        }

        public List<TrainingData> Merge(List<NlpToken> tokens, List<TrainingIntentExpressionPart> entities)
        {
            List<TrainingData> trainingTuple = new List<TrainingData>();
            HashSet<String> entityWordBag = new HashSet<String>();
            int wordCandidateCount = 0;
            
            for (int i = 0; i < tokens.Count; i++)
            {
                TrainingIntentExpressionPart curEntity = null;
                if (entities != null) 
                {
                    bool entityFinded = false;
                    entities.ForEach(entity => {
                        if (!entityFinded)
                        {
                            string[] words = entity.Value.Split(" ");
                            for (int j = 0; j < words.Length; j++)
                            {
                                if (tokens[i + j].Text == words[j])
                                {
                                    wordCandidateCount++;
                                    if (j == words.Length - 1)
                                    {
                                        curEntity = entity;
                                    }
                                }
                                else
                                {
                                    wordCandidateCount = 0;
                                    break;
                                }
                            }
                            if (wordCandidateCount != 0)
                            {
                                String entityName = curEntity.Entity.Contains(":")? curEntity.Entity.Substring(curEntity.Entity.IndexOf(":") + 1): curEntity.Entity;
                                foreach(string s in words) 
                                {
                                    trainingTuple.Add(new TrainingData(entityName, s, tokens[i].Pos, "I"));
                                }
                                entityFinded = true;
                            }
                        }
                    });
                }
                if (wordCandidateCount == 0)
                {
                    trainingTuple.Add(new TrainingData("O", tokens[i].Text, tokens[i].Pos, "O"));
                }
                else
                {
                    i = i + wordCandidateCount - 1;
                }
            }

            return trainingTuple;
        }

        public async Task<bool> Predict(Agent agent, JObject data, PipeModel meta)
        {
            List<List<NlpToken>> tokens = data["Tokens"].ToObject<List<List<NlpToken>>>();
            var uniFeatures = meta.Meta["uniFeatures"].ToString();
            var biFeatures = meta.Meta["biFeatures"].ToString();
            string field = meta.Meta["fields"].ToString();
            string[] fields = field.Split(" ");

            
            string rawPredictingDataFileName = Path.Join(Settings.PredictDir, "ner-crf.corpus.predict.txt");
            string parsedPredictingDataFileName = Path.Join(Settings.PredictDir, "ner-crf.parsed.predict.txt");
            string modelFileName = Path.Join(Settings.ModelDir, meta.Model);

            using (FileStream fs = new FileStream(rawPredictingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    List<string> curLine = new List<string>();
                    foreach (List<NlpToken> tokenList in tokens) 
                    {
                        foreach (NlpToken token in tokenList) 
                        {
                            for (int i = 0 ; i < fields.Length; i++) 
                            {
                                if (fields[i] == "y") {
                                    curLine.Add("");
                                }
                                else if (fields[i] == "w") {
                                    curLine.Add(token.Text);
                                }
                                else if (fields[i] == "pos") {
                                    curLine.Add(token.Tag);
                                }
                                else if (fields[i] == "chk") {
                                    curLine.Add("");
                                }
                            }
                            sw.Write(string.Join(" ", curLine) + "\n");
                            curLine.Clear();
                        }
                        sw.Write("\n");
                        
                    }
                    sw.Flush();
                }
            }
            new MachineLearning.CRFsuite.Ner()
                .NerStart(rawPredictingDataFileName, parsedPredictingDataFileName, field, uniFeatures.Split(" "), biFeatures.Split(" "));

            var output = CmdHelper.Run(Path.Join(Settings.AlgorithmDir, "crfsuite"), $"tag -i -m {modelFileName} {parsedPredictingDataFileName}");

            var entities = new List<NlpEntity>();
            //
            string[] entityProbabilityPairs = output.Split("\r");
            for (int i = 0 ; i < entityProbabilityPairs.Length ; i++)
            {
                string entityProbabilityPair = entityProbabilityPairs[i];
                string entity = entityProbabilityPair.Split(":")[0];
                decimal probability = decimal.Parse(entityProbabilityPair.Split(":")[1]);
                NlpEntity nlpentity = new NlpEntity();
                nlpentity.Entity = entity;
                nlpentity.Value = tokens[0][i].Text;
                nlpentity.Confidence = probability;
                entities.Add(nlpentity);
            }
            


            data["entities"] = JObject.FromObject(entities);
            if(File.Exists(rawPredictingDataFileName))
            {
                File.Delete(rawPredictingDataFileName);
            }
            if(File.Exists(parsedPredictingDataFileName))
            {
                File.Delete(parsedPredictingDataFileName);
            }
            return true;
        }
    }

    public class TrainingData
    {
        public String Token { get; set; }
        public String Entity { get; set; }
        public String Pos { get; set; }
        public String Chunk { get; set; }

        public TrainingData(string entity, string token, string pos, string chunk)
        {
            Token = token;
            Entity = entity;
            Pos = pos;
            Chunk = chunk;
        }
    }
}
