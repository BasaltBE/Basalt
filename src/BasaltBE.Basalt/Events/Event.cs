namespace BasaltBE.Basalt.Events;

public abstract class Event
{
    private bool canceled;

    public void Cancel() => Volatile.Write(ref canceled, true);

    public bool IsCanceled() => Volatile.Read(ref canceled);
}
