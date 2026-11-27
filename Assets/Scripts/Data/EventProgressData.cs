using System;
using System.Collections.Generic;

[Serializable]
public sealed class EventProgressData
{
    public List<string> CompletedIds;
    public List<string> ObservedSignals;
    public List<string> InterruptedIds;
    public List<EventChoiceResult> Choices;
}

[Serializable]
public struct EventChoiceResult
{
    public string EventId;
    public string ChoiceId;
    public string Value;
}
