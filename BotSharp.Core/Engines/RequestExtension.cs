﻿using BotSharp.Core.Agents;
using BotSharp.Core.Intents;
using BotSharp.Core.Models;
using BotSharp.Core.Conversations;
using DotNetToolkit;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BotSharp.Core.Engines
{
    public static class RequestExtension
    {
        public static AIResponse TextRequest(this RasaAi rasa, string text, RequestExtras requestExtras)
        {
            return rasa.TextRequest(new AIRequest(text, requestExtras));
        }

        public static AIResponse TextRequest(this RasaAi rasa, AIRequest request)
        {
            AIResponse aiResponse = new AIResponse();
            Database dc = rasa.dc;

            var result = CallRasa(rasa.agent.Id, request.Query.First(), rasa.agent.Id);
            RasaResponse response = result.Data;
            var intentResponse = HandleIntentPerContextIn(rasa, request, result.Data);

            aiResponse.Id = Guid.NewGuid().ToString();
            aiResponse.Lang = rasa.agent.Language;
            aiResponse.Status = new AIResponseStatus { };
            aiResponse.SessionId = rasa.AiConfig.SessionId;
            aiResponse.Timestamp = DateTime.UtcNow;

            HandleParameter(rasa.agent, intentResponse, response, request);

            HandleMessage(intentResponse);

            aiResponse.Result = new AIResponseResult
            {
                Source = "agent",
                ResolvedQuery = request.Query.First(),
                Action = intentResponse.Action,
                Parameters = intentResponse.Parameters.ToDictionary(x => x.Name, x=> x.Value),
                Score = response.Intent.Confidence,
                Metadata = new AIResponseMetadata { IntentId = intentResponse.IntentId, IntentName = intentResponse.IntentName },
                Fulfillment = new AIResponseFulfillment
                {
                    Messages = intentResponse.Messages.Select(x => {
                        if (x.Type == AIResponseMessageType.Custom)
                        {
                            return (new
                            {
                                x.Type,
                                x.Payload
                            }) as Object;
                        }
                        else
                        {
                            return (new { x.Type, x.Speech }) as Object;
                        }
                        
                    }).ToList()
                }
            };

            HandleContext(dc, rasa, intentResponse, aiResponse);

            Console.WriteLine(JsonConvert.SerializeObject(aiResponse.Result));

            return aiResponse;
        }

        private static IntentResponse HandleIntentPerContextIn(RasaAi rasa, AIRequest request, RasaResponse response)
        {
            Database dc = rasa.dc;

            // Merge input contexts
            var contexts = dc.Table<ConversationContext>()
                .Where(x => x.ConversationId == rasa.AiConfig.SessionId && x.Lifespan > 0)
                .ToList()
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToList();

            contexts.AddRange(request.Contexts.Select(x => new AIContext { Name = x.Name.ToLower(), Lifespan = x.Lifespan }));
            contexts = contexts.OrderBy(x => x.Name).ToList();

            // search all potential intents which input context included in contexts
            var intents = rasa.agent.Intents.Where(it =>
            {
                if (contexts.Count == 0)
                {
                    return it.Contexts.Count() == 0;
                }
                else
                {
                    return it.Contexts.Count() == 0 ||
                        it.Contexts.Count(x => contexts.Select(ctx => ctx.Name).Contains(x.Name.ToLower())) == it.Contexts.Count;
                }
            }).OrderByDescending(x => x.Contexts.Count).ToList();

            if (response.IntentRanking == null)
            {
                response.IntentRanking = new List<RasaResponseIntent>
                {
                    response.Intent
                };
            }
            response.IntentRanking = response.IntentRanking.Where(x => intents.Select(i => i.Name).Contains(x.Name)).ToList();
            response.Intent = response.IntentRanking.First();

            var intent = (dc.Table<Intent>().Where(x => x.Name == response.Intent.Name)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)).First();

            var intentResponse = ArrayHelper.GetRandom(intent.Responses);
            intentResponse.IntentName = intent.Name;

            return intentResponse;
        }

        private static void HandleParameter(Agent agent, IntentResponse intentResponse, RasaResponse response, AIRequest request)
        {
            intentResponse.Parameters.ForEach(p => {
                string query = request.Query.First();
                var entity = response.Entities.FirstOrDefault(x => x.Entity == p.Name);
                if (entity != null)
                {
                    p.Value = query.Substring(entity.Start, entity.End - entity.Start);
                }

                // convert to Standard entity value
                if (!String.IsNullOrEmpty(p.Value) && !p.DataType.StartsWith("@sys."))
                {
                    p.Value = agent.Entities.FirstOrDefault(x => x.Name == p.Name).Entries.FirstOrDefault((entry) => {
                        return entry.Value.ToLower() == p.Value.ToLower() ||
                            entry.Synonyms.Select(synonym => synonym.Synonym.ToLower()).Contains(p.Value.ToLower());
                    })?.Value;
                }

                // fixed entity per request
                if (request.Entities != null)
                {
                    var fixedEntity = request.Entities.FirstOrDefault(x => x.Name == p.Name);
                    if (fixedEntity != null)
                    {
                        if (query.ToLower().Contains(fixedEntity.Entries.First().Value.ToLower()))
                        {
                            p.Value = fixedEntity.Entries.First().Value;
                        }
                    }
                }
            });
        }

        private static void HandleMessage(IntentResponse intentResponse)
        {
            intentResponse.Messages = intentResponse.Messages.OrderBy(x => x.UpdatedTime).ToList();
            intentResponse.Messages.ToList()
                .ForEach(msg =>
                {
                    if (msg.Type == AIResponseMessageType.Custom)
                    {

                    }
                    else
                    {
                        msg.Speech = msg.Speech.StartsWith("[") ?
                            ArrayHelper.GetRandom(msg.Speech.Substring(2, msg.Speech.Length - 4).Split("\",\"").ToList()) :
                            msg.Speech;

                        msg.Speech = ReplaceParameters4Response(intentResponse.Parameters, msg.Speech);
                    }
                });
        }

        private static string ReplaceParameters4Response(List<IntentResponseParameter> parameters, string text)
        {
            var reg = new Regex(@"\$\w+");

            reg.Matches(text).ToList().ForEach(token => {
                text = text.Replace(token.Value, parameters.FirstOrDefault(x => x.Name == token.Value.Substring(1))?.Value.ToString());
            });

            return text;
        }

        private static void HandleContext(Database dc, RasaAi rasa, IntentResponse intentResponse, AIResponse aiResponse)
        {
            // Merge context lifespan
            // override if exists, otherwise add, delete if lifespan is zero
            dc.DbTran(() =>
            {
                var sessionContexts = dc.Table<ConversationContext>().Where(x => x.ConversationId == rasa.AiConfig.SessionId).ToList();

                // minus 1 round
                sessionContexts.Where(x => !intentResponse.Contexts.Select(ctx => ctx.Name).Contains(x.Context))
                    .ToList()
                    .ForEach(ctx => ctx.Lifespan = ctx.Lifespan - 1);

                intentResponse.Contexts.ForEach(ctx =>
                {
                    var session1 = sessionContexts.FirstOrDefault(x => x.Context == ctx.Name);

                    if (session1 != null)
                    {
                        if (ctx.Lifespan == 0)
                        {
                            dc.Table<ConversationContext>().Remove(session1);
                        }
                        else
                        {
                            session1.Lifespan = ctx.Lifespan;
                        }
                    }
                    else
                    {
                        dc.Table<ConversationContext>().Add(new ConversationContext
                        {
                            ConversationId = rasa.AiConfig.SessionId,
                            Context = ctx.Name,
                            Lifespan = ctx.Lifespan
                        });
                    }
                });
            });

            aiResponse.Result.Contexts = dc.Table<ConversationContext>()
                .Where(x => x.Lifespan > 0 && x.ConversationId == rasa.AiConfig.SessionId)
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToArray();
        }

        private static IRestResponse<RasaResponse> CallRasa(string projectId, string text, string model)
        {
            var client = new RestClient($"{Database.Configuration.GetSection("Rasa:Nlu").Value}");

            var rest = new RestRequest("parse", Method.POST);
            string json = JsonConvert.SerializeObject(new { Project = projectId, Q = text, Model = model },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
            rest.AddParameter("application/json", json, ParameterType.RequestBody);

            return client.Execute<RasaResponse>(rest);
        }

        public static AIResponse TextRequestPerContexts(this RasaAi rasa, AIRequest request)
        {
            AIResponse aiResponse = new AIResponse();
            RasaResponse response = null;
            Database dc = rasa.dc;

            // Merge input contexts
            var contexts = dc.Table<ConversationContext>()
                .Where(x => x.ConversationId == rasa.AiConfig.SessionId && x.Lifespan > 0)
                .ToList()
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToList();

            contexts.AddRange(request.Contexts.Select(x => new AIContext { Name = x.Name.ToLower(), Lifespan = x.Lifespan }));
            contexts = contexts.OrderBy(x => x.Name).ToList();

            // search all potential intents which input context included in contexts
            var intents = rasa.agent.Intents.Where(it =>
            {
                if (contexts.Count == 0)
                {
                    return it.Contexts.Count() == 0;
                }
                else
                {
                    return it.Contexts.Count() > 0 &&
                        it.Contexts.Count(x => contexts.Select(ctx => ctx.Name).Contains(x.Name.ToLower())) == it.Contexts.Count;
                }
            }).OrderByDescending(x => x.Contexts.Count).ToList();

            // training per request contexts
            {
                string contextId = $"{String.Join(',', contexts.Select(x => x.Name))}".GetMd5Hash();
                string modelName = dc.Table<ContextModelMapping>().FirstOrDefault(x => x.ContextId == contextId)?.ModelName;
                // need training
                if (String.IsNullOrEmpty(modelName))
                {
                    request.Contexts = contexts.Select(x => new AIContext { Name = x.Name.ToLower() })
                    .OrderBy(x => x.Name)
                    .ToList();

                    dc.DbTran(() =>
                    {
                        modelName = TrainWithContexts(rasa, dc, request, contextId);
                    });
                }

                var result = CallRasa(rasa.agent.Id, request.Query.First(), modelName);

                if (result.Data.Intent != null)
                {
                    response = result.Data;
                }
            }

            // Max contexts match
            if (response == null)
            {
                foreach (var it in intents)
                {
                    request.Contexts = it.Contexts.Select(x => new AIContext { Name = x.Name.ToLower() })
                        .OrderBy(x => x.Name)
                        .ToList();
                    string contextId = $"{String.Join(',', request.Contexts.Select(x => x.Name))}".GetMd5Hash();

                    string modelName = dc.Table<ContextModelMapping>().FirstOrDefault(x => x.ContextId == contextId)?.ModelName;

                    // need training
                    if (String.IsNullOrEmpty(modelName))
                    {
                        dc.DbTran(() =>
                        {
                            modelName = TrainWithContexts(rasa, dc, request, contextId);
                        });
                    }

                    var result = CallRasa(rasa.agent.Id, request.Query.First(), modelName);

                    if (result.Data.Intent != null)
                    {
                        response = result.Data;
                        break;
                    }
                };
            }

            var intent = (dc.Table<Intent>().Where(x => x.Name == response.Intent.Name)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)).First();

            var intentResponse = ArrayHelper.GetRandom(intent.Responses);
            aiResponse.Id = Guid.NewGuid().ToString();
            aiResponse.Lang = rasa.agent.Language;
            aiResponse.Status = new AIResponseStatus { };
            aiResponse.SessionId = rasa.AiConfig.SessionId;
            aiResponse.Timestamp = DateTime.UtcNow;
            intentResponse.Messages = intentResponse.Messages.OrderBy(x => x.UpdatedTime).ToList();
            intentResponse.Messages.ToList()
                .ForEach(msg =>
                {
                    if (msg.Type == AIResponseMessageType.Custom)
                    {

                    }
                    else
                    {
                        msg.Speech = msg.Speech.StartsWith("[") ?
                            ArrayHelper.GetRandom(msg.Speech.Substring(2, msg.Speech.Length - 4).Split("\",\"").ToList()) :
                            msg.Speech;
                    }
                });

            aiResponse.Result = new AIResponseResult
            {
                Source = "agent",
                ResolvedQuery = request.Query.First(),
                Action = intentResponse.Action,
                Parameters = new Dictionary<string, string>(),
                Score = response.Intent.Confidence,
                Metadata = new AIResponseMetadata { IntentId = intent.Id, IntentName = intent.Name },
                Fulfillment = new AIResponseFulfillment
                {
                    Messages = intentResponse.Messages.Select(x => {
                        if (x.Type == AIResponseMessageType.Custom)
                        {
                            return (new
                            {
                                x.Type,
                                x.Payload
                            }) as Object;
                        }
                        else
                        {
                            return (new { x.Type, x.Speech }) as Object;
                        }

                    }).ToList()
                }
            };

            // Merge context lifespan
            // override if exists, otherwise add, delete if lifespan is zero
            dc.DbTran(() =>
            {
                var sessionContexts = dc.Table<ConversationContext>().Where(x => x.ConversationId == rasa.AiConfig.SessionId).ToList();

                // minus 1 round
                sessionContexts.Where(x => !intentResponse.Contexts.Select(ctx => ctx.Name).Contains(x.Context))
                    .ToList()
                    .ForEach(ctx => ctx.Lifespan = ctx.Lifespan - 1);

                intentResponse.Contexts.ForEach(ctx =>
                {
                    var session1 = sessionContexts.FirstOrDefault(x => x.Context == ctx.Name);

                    if (session1 != null)
                    {
                        if (ctx.Lifespan == 0)
                        {
                            dc.Table<ConversationContext>().Remove(session1);
                        }
                        else
                        {
                            session1.Lifespan = ctx.Lifespan;
                        }
                    }
                    else
                    {
                        dc.Table<ConversationContext>().Add(new ConversationContext
                        {
                            ConversationId = rasa.AiConfig.SessionId,
                            Context = ctx.Name,
                            Lifespan = ctx.Lifespan
                        });
                    }
                });
            });

            aiResponse.Result.Contexts = dc.Table<ConversationContext>()
                .Where(x => x.ConversationId == rasa.AiConfig.SessionId)
                .Select(x => new AIContext { Name = x.Context.ToLower(), Lifespan = x.Lifespan })
                .ToArray();

            return aiResponse;
        }

        public static string Train(this RasaAi console, Database dc)
        {
            var corpus = console.agent.GrabCorpus(dc);

            // Add some fake data
            if(corpus.UserSays.Count < 3)
            {
                corpus.UserSays.Add(new RasaIntentExpression
                {
                    Intent = "Welcome",
                    Text = "Hi"
                });

                corpus.UserSays.Add(new RasaIntentExpression
                {
                    Intent = "Welcome",
                    Text = "Hey"
                });

                corpus.UserSays.Add(new RasaIntentExpression
                {
                    Intent = "Welcome",
                    Text = "Hello"
                });
            }

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

            var client = new RestClient($"{Database.Configuration.GetSection("Rasa:Nlu").Value}");
            var rest = new RestRequest("train", Method.POST);
            rest.AddQueryParameter("project", console.agent.Id);
            rest.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(rest);

            if (response.IsSuccessful)
            {
                var result = JObject.Parse(response.Content);

                string modelName = result["info"].Value<String>().Split(": ")[1];

                return modelName;
            }
            else
            {
                var result = JObject.Parse(response.Content);

                Console.WriteLine(result["error"]);

                return String.Empty;
            }
        }

        /// <summary>
        /// Need two categories at least
        /// </summary>
        /// <param name="console"></param>
        /// <param name="dc"></param>
        /// <param name="request"></param>
        /// <param name="contextId"></param>
        /// <returns></returns>
        public static string TrainWithContexts(this RasaAi console, Database dc, AIRequest request, String contextId)
        {
            var corpus = console.agent.GrabCorpusPerContexts(dc, request.Contexts);

            corpus.UserSays.Add(new RasaIntentExpression
            {
                Intent = "Welcome",
                Text = "Hi"
            });

            corpus.UserSays.Add(new RasaIntentExpression
            {
                Intent = "Welcome",
                Text = "Hey"
            });

            corpus.UserSays.Add(new RasaIntentExpression
            {
                Intent = "Welcome",
                Text = "Hello"
            });

            string json = JsonConvert.SerializeObject(new { rasa_nlu_data = corpus },
                new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    NullValueHandling = NullValueHandling.Ignore
                });

            var client = new RestClient($"{Database.Configuration.GetSection("Rasa:Host").Value}");
            var rest = new RestRequest("train", Method.POST);
            rest.AddQueryParameter("project", console.agent.Id);
            rest.AddParameter("application/json", json, ParameterType.RequestBody);

            var response = client.Execute(rest);

            if (response.IsSuccessful)
            {
                var result = JObject.Parse(response.Content);

                string modelName = result["info"].Value<String>().Split(": ")[1];

                dc.Table<ContextModelMapping>().Add(new ContextModelMapping
                {
                    AgentId = console.agent.Id,
                    ModelName = modelName,
                    ContextId = contextId
                });

                return modelName;
            }
            else
            {
                var result = JObject.Parse(response.Content);

                Console.WriteLine(result["error"]);

                return String.Empty;
            }
        }
    }
}
