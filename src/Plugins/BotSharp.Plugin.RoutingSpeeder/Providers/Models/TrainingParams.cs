using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.RoutingSpeeder.Providers.Models;

public class TrainingParams
{
    public int ClientId { get; set; }
    public int Epochs { get; set; } = 10;
    public int BatchSize { get; set; } = 16;
    public float LearningRate { get; set; } = 1.0e-4f;
    public bool Inference { get; set; } = false;
}
