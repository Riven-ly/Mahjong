using System;

public sealed class GuidePopData
{
    public Action Completed { get; }

    public GuidePopData(Action completed)
    {
        Completed = completed;
    }
}
