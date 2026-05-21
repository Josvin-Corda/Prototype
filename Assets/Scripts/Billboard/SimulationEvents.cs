using System;

public static class SimulationEvents
{
    public static event Action<int, string, string> OnETAUpdated;

    public static void RaiseETAUpdated(int secondsRemaining, string pickupTag, string statusMessage)
    {
        OnETAUpdated?.Invoke(secondsRemaining, pickupTag, statusMessage);
    }
}
