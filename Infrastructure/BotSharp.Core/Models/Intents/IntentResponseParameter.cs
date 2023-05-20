﻿using BotSharp.Platform.Models.AiResponse;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentResponseParameter
    {
        [Required]
        [StringLength(36)]
        public String IntentResponseId { get; set; }

        public bool Required { get; set; }

        [MaxLength(32)]
        public string DataType { get; set; }

        [MaxLength(128)]
        public string DefaultValue { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(128)]
        public string Value { get; set; }

        public bool IsList { get; set; }

        [ForeignKey("ResponseParameterId")]
        public List<ResponseParameterPrompt> Prompts { get; set; }
    }
}
