namespace Aevatar.GAgents.Pipeline.Abstract;

[GenerateSerializer]
public class JobProgressResult<T>
{
    [Id(0)] public bool IfContinue { get; set; } = true;
    [Id(1)] public T Result { get; set; }
}