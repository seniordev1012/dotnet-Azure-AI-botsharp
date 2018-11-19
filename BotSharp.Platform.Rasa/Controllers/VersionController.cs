﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Controllers
{
#if RASA
    [Route("[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public ActionResult<RasaVersionModel> Get()
        {
            return Ok(new RasaVersionModel
            {
                Version = "0.13.0",
                MinimumCompatibleVersion = "0.13.0"
            });
        }
    }
#endif
}
