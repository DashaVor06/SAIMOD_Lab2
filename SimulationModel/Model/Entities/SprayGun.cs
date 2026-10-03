namespace Workshop.Model.Entities;

/// <summary>
/// Краскопульт струйной окраски.
/// Пассивный ресурс (аппарат), требующий присутствия рабочего для проведения окраски.
/// </summary>
public class SprayGun
{
    public int Id { get; }
    public bool IsBusy { get; private set; }
    public Worker? CurrentWorker { get; private set; }

    public SprayGun(int id)
    {
        Id = id;
    }

    public void Occupy(Worker worker)
    {
        if (IsBusy) throw new InvalidOperationException($"Краскопульт #{Id} уже занят рабочим #{CurrentWorker?.Id}");
        IsBusy = true;
        CurrentWorker = worker;
    }

    public void Release()
    {
        IsBusy = false;
        CurrentWorker = null;
    }
}
