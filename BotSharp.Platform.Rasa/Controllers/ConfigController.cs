﻿using BotSharp.Platform.Rasa.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Rasa.Controllers
{
    [Route("[controller]")]
    public class ConfigController : ControllerBase
    {
        [HttpGet]
        public ActionResult<RasaVersionModel> Get()
        {
            var status = new RasaStatusModel
            {
                AvailableProjects = JObject.FromObject(new RasaProjectModel
                {
                    Status = "ready",
                    AvailableModels = new List<string> { "model_XXXXXX" },
                    LoadedModels = new List<string> { "model_XXXXXX" }
                })
            };

            return Ok(status);
        }
    }
}
