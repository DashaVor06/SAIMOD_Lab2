namespace Workshop.Model.Entities;

public enum WorkerState
{
    Idle,
    Preparing,
    Painting
}

/// <summary>
/// Рабочий красильной мастерской.
/// Может подготавливать изделия либо работать за свободным краскопультом.
/// </summary>
public class Worker
{
    public int Id { get; }
    public WorkerState State { get; private set; } = WorkerState.Idle;
    public Item? CurrentItem { get; private set; }
    public SprayGun? CurrentSprayGun { get; private set; }
    public List<Item> PaintingBatch { get; } = new();

    public int TotalItemsPrepared { get; set; }
    public int TotalItemsPainted { get; set; }

    public bool IsIdle => State == WorkerState.Idle;
    public bool IsPreparing => State == WorkerState.Preparing;
    public bool IsPainting => State == WorkerState.Painting;
    public bool IsBusy => State != WorkerState.Idle;

    public Worker(int id)
    {
        Id = id;
    }

    public void StartPreparing(Item item, double currentTime)
    {
        State = WorkerState.Preparing;
        CurrentItem = item;
        item.PrepStartTime = currentTime;
    }

    public Item FinishPreparing(double currentTime)
    {
        if (CurrentItem == null) throw new InvalidOperationException("Рабочий не готовил изделие.");
        var item = CurrentItem;
        item.PrepEndTime = currentTime;
        CurrentItem = null;
        State = WorkerState.Idle;
        TotalItemsPrepared++;
        return item;
    }

    public void StartPainting(SprayGun sprayGun, List<Item> itemsToPaint)
    {
        State = WorkerState.Painting;
        CurrentSprayGun = sprayGun;
        PaintingBatch.Clear();
        PaintingBatch.AddRange(itemsToPaint);
        sprayGun.Occupy(this);
    }

    public void FinishPainting()
    {
        PaintingBatch.Clear();
        CurrentItem = null;
        if (CurrentSprayGun != null)
        {
            CurrentSprayGun.Release();
            CurrentSprayGun = null;
        }
        State = WorkerState.Idle;
    }

    public void SetCurrentPaintingItem(Item item, double currentTime)
    {
        CurrentItem = item;
        item.PaintStartTime = currentTime;
    }

    public void FinishSinglePaintedItem(Item item, double currentTime)
    {
        item.CompletionTime = currentTime;
        TotalItemsPainted++;
        CurrentItem = null;
    }
}
