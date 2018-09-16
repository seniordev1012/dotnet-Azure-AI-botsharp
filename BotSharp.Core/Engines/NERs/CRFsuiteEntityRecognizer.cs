﻿using BotSharp.Core.Abstractions;
using BotSharp.Core.Agents;
using BotSharp.Models.NLP;
using BotSharp.NLP.Tokenize;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.NERs
{
    public class CRFsuiteEntityRecognizer : INlpTrain, INlpPredict, INlpNer
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }

        public List<OntologyEnum> Ontologies
        {
            get
            {
                return new List<OntologyEnum>
                {
                    OntologyEnum.Location,
                    OntologyEnum.DateTime
                };
            }
        }

        public async Task<bool> Train(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var corpus = agent.Corpus;

            meta.Model = "ner-crf.model";

            List<TrainingIntentExpression<TrainingIntentExpressionPart>> userSays = corpus.UserSays;
            List<List<TrainingData>> list = new List<List<TrainingData>>();

            string rawTrainingDataFileName = Path.Combine(Settings.TempDir, "ner-crf.corpus.txt");
            string parsedTrainingDataFileName = Path.Combine(Settings.TempDir, "ner-crf.parsed.txt");
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            using (FileStream fs = new FileStream(rawTrainingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int i = 0; i < doc.Sentences.Count; i++)
                    {
                        List<TrainingData> curLine = Merge(doc, doc.Sentences[i].Tokens, userSays[i].Entities);
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

            new NLP.Models.CRFsuite.Ner()
                .NerStart(rawTrainingDataFileName, parsedTrainingDataFileName, fields, uniFeatures.Split(' '), biFeatures.Split(' '));

            var algorithmDir = Path.Combine(AppDomain.CurrentDomain.GetData("ContentRootPath").ToString(), "Algorithms");

            CmdHelper.Run(Path.Combine(algorithmDir, "crfsuite"), $"learn -m \"{modelFileName}\" \"{parsedTrainingDataFileName}\"", false); // --split=3 -x
            Console.WriteLine($"Saved model to {modelFileName}");
            meta.Meta = new JObject();
            meta.Meta["fields"] = fields;
            meta.Meta["uniFeatures"] = uniFeatures;
            meta.Meta["biFeatures"] = biFeatures;

            return true;
        }

        public List<TrainingData> Merge(NlpDoc doc, List<Token> tokens, List<TrainingIntentExpressionPart> entities)
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
                            var vDoc = new NlpDoc { Sentences = new List<NlpDocSentence> { new NlpDocSentence { Text = entity.Value } } };
                            doc.Tokenizer.Predict(null, vDoc, null);
                            string[] words = vDoc.Sentences[0].Tokens.Select(x => x.Text).ToArray();

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
                            if (wordCandidateCount != 0) // && entity.Start == tokens[i].Offset)
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

        public async Task<bool> Predict(Agent agent, NlpDoc doc, PipeModel meta)
        {
            var uniFeatures = meta.Meta["uniFeatures"].ToString();
            var biFeatures = meta.Meta["biFeatures"].ToString();
            string field = meta.Meta["fields"].ToString();
            string[] fields = field.Split(' ');
            
            string rawPredictingDataFileName = Path.Combine(Settings.TempDir, "ner-crf.corpus.predict.txt");
            string parsedPredictingDataFileName = Path.Combine(Settings.TempDir, "ner-crf.parsed.predict.txt");
            string modelFileName = Path.Combine(Settings.ModelDir, meta.Model);

            using (FileStream fs = new FileStream(rawPredictingDataFileName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    List<string> curLine = new List<string>();
                    foreach (NlpDocSentence sentence in doc.Sentences) 
                    {
                        foreach (Token token in sentence.Tokens) 
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

            new NLP.Models.CRFsuite.Ner()
                .NerStart(rawPredictingDataFileName, parsedPredictingDataFileName, field, uniFeatures.Split(' '), biFeatures.Split(' '));

            var output = CmdHelper.Run(Path.Combine(Settings.AlgorithmDir, "crfsuite"), $"tag -i -m \"{modelFileName}\" \"{parsedPredictingDataFileName}\"", false);

            var entities = new List<NlpEntity>();

            string[] entityProbabilityPairs = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Where(x => !String.IsNullOrEmpty(x)).ToArray();
            for (int i = 0; i < entityProbabilityPairs.Length; i++)
            {
                string entityProbabilityPair = entityProbabilityPairs[i];
                string entity = entityProbabilityPair.Split(':')[0];
                decimal probability = decimal.Parse(entityProbabilityPair.Split(':')[1]);
                entities.Add(new NlpEntity
                {
                    Entity = entity,
                    Start = doc.Sentences[0].Tokens[i].Start,
                    Value = doc.Sentences[0].Tokens[i].Text,
                    Confidence = probability,
                    Extrator = "CRFsuiteEntityRecognizer"
                });
            }

            List<NlpEntity> unionedEntities = MergeEntity(doc.Sentences[0].Text, entities);

            doc.Sentences[0].Entities = unionedEntities.Where(x => x.Entity != "O").ToList();
            
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

        public List<NlpEntity> MergeEntity (string sentence, List<NlpEntity> tokens)
        {
            List<NlpEntity> res = new List<NlpEntity>();
            for (int i = 0; i < tokens.Count ; i++) 
            {
                NlpEntity nlpEntity = new NlpEntity();
                var current = tokens[i];

                if (current.Entity != "O")
                {
                    nlpEntity = current.ToObject<NlpEntity>();
                    // greedy search until next entity
                    int j = 0;
                    for (j = i + 1; j < tokens.Count; j++)
                    {
                        var next = tokens[j];
                        if (current.Entity == next.Entity)
                        {
                            i = j;
                            nlpEntity.Value = sentence.Substring(current.Start, next.End - current.Start + 1);
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                    res.Add(nlpEntity);
                }
            }
            return res;
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
