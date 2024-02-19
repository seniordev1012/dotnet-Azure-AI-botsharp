using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class SqlStatement
{
    [JsonPropertyName("sql_statement")]
    public string Statement { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("table")]
    public string Table { get; set; }

    [JsonPropertyName("is_check_existence")]
    public bool IsCheckExistence { get; set; }

    [JsonPropertyName("parameters")]
    public SqlParamater[] Parameters { get; set; } = new SqlParamater[0];

    [JsonPropertyName("return_field")]
    public SqlReturn Return { get; set; }

    public override string ToString()
    {
        return $"{Statement}\t {string.Join(", ", Parameters.Select(x => x.Name + ": " + x.Value))}";
    }
}
