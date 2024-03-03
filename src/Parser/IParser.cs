namespace Automation.Parser
{
    internal interface IParser
    {
        Task Parse(string username, string password);
        // Task Follow(string username, string password);
    }
}
