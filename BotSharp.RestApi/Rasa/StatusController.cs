﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
#if RASA_UI
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public ActionResult<RasaVersionModel> Get()
        {
            var status = new RasaStatusModel();
            status.AvailableProjects = JObject.FromObject(new RasaProjectModel
            {
                Status = "ready",
                AvailableModels = new List<string> { "<model_XXXXXX>" }
            });

            return Ok(status);
        }
    }
#endif
}
