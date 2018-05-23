﻿using BotSharp.Core.Agents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Intents
{
    public static class IntentDriver
    {
        public static String CreateIntent(this Agent agent, Database dc, Intent intent)
        {
            if (dc.Table<Intent>().Any(x => x.Id == intent.Id)) return intent.Id;

            dc.Table<Intent>().Add(intent);

            return intent.Id;
        }
    }
}
