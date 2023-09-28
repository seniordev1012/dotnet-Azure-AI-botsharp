using BotSharp.Abstraction.Functions.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Models;

public class FunctionDefMongoElement
{
    public string Name { get; set; }
    public string Description { get; set; }
    public FunctionParametersDefMongoElement Parameters { get; set; } = new FunctionParametersDefMongoElement();

    public FunctionDefMongoElement()
    {
        
    }

    public static FunctionDefMongoElement ToMongoElement(FunctionDef function)
    {
        return new FunctionDefMongoElement
        {
            Name = function.Name,
            Description = function.Description,
            Parameters = new FunctionParametersDefMongoElement
            {
                Type = function.Parameters.Type,
                Properties = JsonSerializer.Serialize(function.Parameters.Properties),
                Required = function.Parameters.Required,
            }
        };
    }

    public static FunctionDef ToDomainElement(FunctionDefMongoElement mongoFunction)
    {
        return new FunctionDef
        {
            Name = mongoFunction.Name,
            Description = mongoFunction.Description,
            Parameters = new FunctionParametersDef
            {
                Type = mongoFunction.Parameters.Type,
                Properties = JsonSerializer.Deserialize<JsonDocument>(mongoFunction.Parameters.Properties),
                Required = mongoFunction.Parameters.Required,
            }
        };
    }
}

public class FunctionParametersDefMongoElement
{
    public string Type { get; set; }
    public string Properties { get; set; }
    public List<string> Required { get; set; } = new List<string>();

    public FunctionParametersDefMongoElement()
    {
        
    }
}
