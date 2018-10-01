﻿using BotSharp.Core.Engines;
using BotSharp.Core.Entities;
using BotSharp.Core.Intents;
using BotSharp.Platform.Models;
using EntityFrameworkCore.BootKit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Agents
{
    [Table("Bot_Agent")]
    public class Agent : DbRecord, IDbRecord
    {
        public Agent()
        {
            CreatedDate = DateTime.UtcNow;
        }

        [Required]
        [MaxLength(64)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        public Boolean Published { get; set; }

        [Required]
        [MaxLength(5)]
        public String Language { get; set; }

        /// <summary>
        /// Only access text/ audio rquest
        /// </summary>
        [StringLength(32)]
        public String ClientAccessToken { get; set; }

        /// <summary>
        /// Developer can access more APIs
        /// </summary>
        [StringLength(32)]
        public String DeveloperAccessToken { get; set; }

        [ForeignKey("AgentId")]
        public List<Intent> Intents { get; set; }

        [ForeignKey("AgentId")]
        [JsonProperty("entity_types")]
        public List<EntityType> Entities { get; set; }

        public String Birthday
        {
            get
            {
                return CreatedDate.ToShortDateString();
            }
        }

        [Required]
        public DateTime CreatedDate { get; set; }

        public Boolean IsSkillSet { get; set; }

        [ForeignKey("AgentId")]
        public AgentMlConfig MlConfig { get; set; }

        [NotMapped]
        public TrainingCorpus Corpus { get; set; }

        [ForeignKey("AgentId")]
        public List<AgentIntegration> Integrations { get; set; }
    }
}
