﻿using BotSharp.Core.Engines;
using BotSharp.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.UnitTest
{
    [TestClass]
    public class IntentTest : TestEssential
    {
        [TestMethod]
        public void TextRequest()
        {
            var config = new AIConfiguration(BOT_CLIENT_TOKEN, SupportedLanguage.English);
            config.SessionId = Guid.NewGuid().ToString();

            var rasa = new RasaAi(dc, config);

            // Round 1
            var response = rasa.TextRequest(new AIRequest { Query = new String[] { "Can you play country music?" } });
            Assert.AreEqual(response.Result.Metadata.IntentName, "music.play");
        }
    }
}
