﻿using BotSharp.Core.Abstractions;
using CherubNLP;
using CherubNLP.Tokenize;
using BotSharp.Platform.Models;
using BotSharp.Platform.Models.AiResponse;
using BotSharp.Platform.Models.MachineLearning;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Core.Engines.BotSharp
{
    public class BotSharpTokenizer : INlpTrain, INlpPredict
    {
        public IConfiguration Configuration { get; set; }
        public PipeSettings Settings { get; set; }
        private TokenizerFactory _tokenizer;

        public BotSharpTokenizer()
        {

        }

        public async Task<bool> Predict(AgentBase agent, NlpDoc doc, PipeModel meta)
        {
            Init();

            doc.Tokenizer = this;

            // same as train
            doc.Sentences.ForEach(snt =>
            {
                snt.Tokens = _tokenizer.Tokenize(snt.Text);
            });

            return true;
        }

        public async Task<bool> Train(AgentBase agent, NlpDoc doc, PipeModel meta)
        {
            Init();

            doc.Tokenizer = this;
            doc.Sentences = new List<NlpDocSentence>();

            agent.Corpus.UserSays.ForEach(say =>
            {
                doc.Sentences.Add(new NlpDocSentence
                {
                    Tokens = _tokenizer.Tokenize(say.Text),
                    Text = say.Text,
                    Intent = new TextClassificationResult { Label = say.Intent }
                });
            });

            return true;
        }

        private void Init()
        {
            if(_tokenizer == null)
            {
                _tokenizer = new TokenizerFactory(new TokenizationOptions
                {
                    Pattern = Configuration.GetValue<String>("options:pattern")
                }, SupportedLanguage.English);

                string tokenizerName = Configuration.GetValue<String>($"tokenizer");

                _tokenizer.GetTokenizer(tokenizerName);
            }
        }
    }
}
