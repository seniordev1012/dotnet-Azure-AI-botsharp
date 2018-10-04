﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models.Intents
{
    public class IntentExpressionPart
    {
        [Required]
        [StringLength(36)]
        public String ExpressionId { get; set; }

        [Required]
        [MaxLength(128)]
        public String Text { get; set; }

        [MaxLength(64)]
        public String Alias { get; set; }

        [MaxLength(64)]
        public String Meta { get; set; }

        public int Start { get; set; }

        public Boolean UserDefined { get; set; }

        public DateTime UpdatedTime { get; set; }
    }
}
