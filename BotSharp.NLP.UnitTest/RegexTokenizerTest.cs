using BotSharp.NLP.Tokenize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BotSharp.NLP.UnitTest
{
    [TestClass]
    public class RegexTokenizerTest
    {
        [TestMethod]
        public void TokenizeInWhiteSpace()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WHITE_SPACE
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Chop into pieces, isn't it?");

            Assert.IsTrue(tokens[0].Start == 0);
            Assert.IsTrue(tokens[0].Text == "Chop");

            Assert.IsTrue(tokens[1].Start == 5);
            Assert.IsTrue(tokens[1].Text == "into");

            Assert.IsTrue(tokens[2].Start == 10);
            Assert.IsTrue(tokens[2].Text == "pieces,");

            Assert.IsTrue(tokens[3].Start == 18);
            Assert.IsTrue(tokens[3].Text == "isn't");

            Assert.IsTrue(tokens[4].Start == 24);
            Assert.IsTrue(tokens[4].Text == "it?");
        }

        [TestMethod]
        public void TokenizeInWordPunctuation()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.WORD_PUNC,
                SpecialWords = new List<string> { "n't" }
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize("Chop into pieces, isn't it?");

            Assert.IsTrue(tokens[0].Start == 0);
            Assert.IsTrue(tokens[0].Text == "Chop");

            Assert.IsTrue(tokens[1].Start == 5);
            Assert.IsTrue(tokens[1].Text == "into");

            Assert.IsTrue(tokens[2].Start == 10);
            Assert.IsTrue(tokens[2].Text == "pieces");

            Assert.IsTrue(tokens[3].Start == 16);
            Assert.IsTrue(tokens[3].Text == ",");

            Assert.IsTrue(tokens[4].Start == 18);
            Assert.IsTrue(tokens[4].Text == "is");

            Assert.IsTrue(tokens[5].Start == 20);
            Assert.IsTrue(tokens[5].Text == "n't");

            Assert.IsTrue(tokens[6].Start == 24);
            Assert.IsTrue(tokens[6].Text == "it");

            Assert.IsTrue(tokens[7].Start == 26);
            Assert.IsTrue(tokens[7].Text == "?");
        }

        [TestMethod]
        public void TokenizeInBlankLine()
        {
            var tokenizer = new TokenizerFactory<RegexTokenizer>(new TokenizationOptions
            {
                Pattern = RegexTokenizer.BLANK_LINE
            }, SupportedLanguage.English);

            var tokens = tokenizer.Tokenize(@"Chop into pieces, 

isn't

it?");

            Assert.IsTrue(tokens[0].Start == 0);
            Assert.IsTrue(tokens[0].Text == "Chop into pieces,");

            Assert.IsTrue(tokens[1].Start == 18);
            Assert.IsTrue(tokens[1].Text == "isn't");

            Assert.IsTrue(tokens[2].Start == 28);
            Assert.IsTrue(tokens[2].Text == "it?");
        }
    }
}
