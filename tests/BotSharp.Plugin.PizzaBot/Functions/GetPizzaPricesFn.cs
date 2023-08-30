using BotSharp.Abstraction.Conversations.Models;
using System.Text.Json;

namespace BotSharp.Plugin.PizzaBot.Functions;

public class GetPizzaPricesFn : IFunctionCallback
{
    public string Name => "get_pizza_price";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.ExecutionData = new
        {
            cheese = "3.5"
        };
        message.ExecutionResult = "Pepperoni Pizza: $3.5/slice, Cheese Pizza: $2.5/slice, Margherita Pizza: $3.0/slice";
        return true;
    }
}
