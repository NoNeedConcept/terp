namespace Terp.Akka.Example;

internal interface IGreeter
{
    string Greet(string name);
}

internal sealed class Greeter : IGreeter
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}
