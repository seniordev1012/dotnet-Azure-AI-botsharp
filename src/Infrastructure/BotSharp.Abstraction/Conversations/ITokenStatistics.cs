namespace BotSharp.Abstraction.Conversations;

public interface ITokenStatistics
{
    int Total { get; }
    float AccumulatedCost { get; }
    float Cost { get; }
    void AddToken(int promptCount, int completionCount);
    void PrintStatistics();
}
