﻿using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.NLP.Tokenize
{
    /// <summary>
    /// BotSharp Tokenizer Factory
    /// Tokenizers divide strings into lists of substrings.
    /// The particular tokenizer requires implement interface 
    /// models to be installed.BotSharp.NLP also provides a simpler, regular-expression based tokenizer, which splits text on whitespace and punctuation.
    /// </summary>
    public class TokenizerFactory<ITokenize> where ITokenize : ITokenizer, new()
    {
        private SupportedLanguage _lang;

        private ITokenize _tokenizer;

        private TokenizationOptions _options;

        public TokenizerFactory(TokenizationOptions options, SupportedLanguage lang)
        {
            _lang = lang;
            _options = options;
            _tokenizer = new ITokenize();
        }

        public List<Token> Tokenize(string sentence)
        {
            return _tokenizer.Tokenize(sentence, _options);
        }
    }
}
