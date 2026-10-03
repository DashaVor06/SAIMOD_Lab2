using Simulation.Engine;
using Simulation.Engine.Statistics;
using Workshop.Model.Entities;
using Workshop.Model.Events;
using Workshop.Model.Random;

namespace Workshop.Model;

/// <summary>
/// Предметная имитационная модель красильной мастерской (Вариант 10).
/// Реализует ISimulationModel, взаимодействуя с универсальным ядром симулятора через Список событий.
/// </summary>
public class WorkshopModel : ISimulationModel
{
    public WorkshopConfig Config { get; }
    private IRandomGenerator _rng;
    private int _itemSequence = 0;

    public List<Worker> Workers { get; private set; } = new();
    public List<SprayGun> SprayGuns { get; private set; } = new();
    public Queue<Item> PreparationQueue { get; private set; } = new();
    public Queue<Item> PreparedPool { get; private set; } = new();
    public List<Item> CompletedItems { get; private set; } = new();

    // 4 основных отклика модели
    public int ProcessedItemsCount { get; private set; }
    public DiscreteStatistic TimeInSystemStat { get; private set; } = new("Время изделия в мастерской");
    public ContinuousStatistic BusyWorkersStat { get; private set; } = new("Занятые рабочие");
    public ContinuousStatistic BusySprayGunsStat { get; private set; } = new("Занятые краскопульты");

    // Дополнительные отклики
    public ContinuousStatistic QueueLengthStat { get; private set; } = new("Длина очереди подготовки");

    public int CurrentBusyWorkers => Workers.Count(w => w.IsBusy);
    public int CurrentBusySprayGuns => SprayGuns.Count(sg => sg.IsBusy);

    public WorkshopModel(WorkshopConfig? config = null, IRandomGenerator? rng = null)
    {
        Config = config ?? new WorkshopConfig();
        _rng = rng ?? new StandardRandomGenerator();
    }

    public void Initialize(Simulator simulator)
    {
        // Инициализация датчика ПСЧ из конфигурации симулятора, если он задан
        if (simulator.Config.RngSeed.HasValue)
        {
            _rng = new StandardRandomGenerator(simulator.Config.RngSeed.Value);
        }

        _itemSequence = 0;
        Workers = Enumerable.Range(1, Config.WorkersCount).Select(id => new Worker(id)).ToList();
        SprayGuns = Enumerable.Range(1, Config.SprayGunsCount).Select(id => new SprayGun(id)).ToList();
        PreparationQueue = new Queue<Item>();
        PreparedPool = new Queue<Item>();
        CompletedItems = new List<Item>();

        ProcessedItemsCount = 0;
        TimeInSystemStat = new DiscreteStatistic("Время изделия в мастерской (мин)");
        BusyWorkersStat = new ContinuousStatistic("Занятые рабочие", initialValue: 0, startTime: 0);
        BusySprayGunsStat = new ContinuousStatistic("Занятые краскопульты", initialValue: 0, startTime: 0);
        QueueLengthStat = new ContinuousStatistic("Длина очереди подготовки", initialValue: 0, startTime: 0);

        // Подписываем непрерывные статистики на продвижение времени симулятора
        simulator.OnTimeAdvanced += (dt, currTime) =>
        {
            BusyWorkersStat.AdvanceTo(currTime);
            BusySprayGunsStat.AdvanceTo(currTime);
            QueueLengthStat.AdvanceTo(currTime);
        };

        // Начальное состояние: изделия, ожидающие подготовки на момент открытия
        for (int i = 0; i < Config.InitialItemsAtStart; i++)
        {
            var initialItem = new Item(++_itemSequence, 0.0);
            if (i < Workers.Count)
            {
                var worker = Workers[i];
                worker.StartPreparing(initialItem, 0.0);
                double prepDuration = SamplePreparationTime();
                simulator.Schedule(new PreparationFinishedEvent(prepDuration, worker, this));
            }
            else
            {
                PreparationQueue.Enqueue(initialItem);
            }
        }

        BusyWorkersStat.Update(0.0, CurrentBusyWorkers);
        QueueLengthStat.Update(0.0, PreparationQueue.Count);

        // Первое поступление изделия из входного экспоненциального потока
        double firstArrival = SampleInterarrivalTime();
        simulator.Schedule(new ItemArrivalEvent(firstArrival, this));
    }

    public void OnWarmupEnded(Simulator simulator)
    {
        ProcessedItemsCount = 0;
        TimeInSystemStat.Reset();
        BusyWorkersStat.Reset(simulator.CurrentTime);
        BusySprayGunsStat.Reset(simulator.CurrentTime);
        QueueLengthStat.Reset(simulator.CurrentTime);
    }

    public bool HasWorkRemaining()
    {
        return PreparationQueue.Count > 0
               || PreparedPool.Count > 0
               || Workers.Any(w => w.IsBusy);
    }

    public string GetStateSummary()
    {
        int prepWorkers = Workers.Count(w => w.IsPreparing);
        int paintWorkers = Workers.Count(w => w.IsPainting);
        int idleWorkers = Workers.Count(w => w.IsIdle);
        int busyGuns = SprayGuns.Count(sg => sg.IsBusy);

        return $"Оч.Подг:{PreparationQueue.Count} | Раб.[П:{prepWorkers} К:{paintWorkers} Св:{idleWorkers}] | ПулГот:{PreparedPool.Count} | Краск:[{busyGuns}/{SprayGuns.Count}] | Сделано:{ProcessedItemsCount}";
    }

    public void HandleItemArrival(Simulator simulator)
    {
        // Если период поступления еще не завершен, планируем следующее прибытие
        if (!simulator.IsArrivalStopped)
        {
            double nextArrivalTime = simulator.CurrentTime + SampleInterarrivalTime();
            simulator.Schedule(new ItemArrivalEvent(nextArrivalTime, this));
        }

        var item = new Item(++_itemSequence, simulator.CurrentTime);

        // Проверяем наличие свободного рабочего для подготовки
        var idleWorker = Workers.FirstOrDefault(w => w.IsIdle);
        if (idleWorker != null)
        {
            idleWorker.StartPreparing(item, simulator.CurrentTime);
            BusyWorkersStat.Update(simulator.CurrentTime, CurrentBusyWorkers);

            double prepTime = SamplePreparationTime();
            simulator.Schedule(new PreparationFinishedEvent(simulator.CurrentTime + prepTime, idleWorker, this));
        }
        else
        {
            PreparationQueue.Enqueue(item);
            QueueLengthStat.Update(simulator.CurrentTime, PreparationQueue.Count);
        }
    }

    public void HandlePreparationFinished(Worker worker, Simulator simulator)
    {
        var item = worker.FinishPreparing(simulator.CurrentTime);
        PreparedPool.Enqueue(item);

        // Проверка наличия свободного краскопульта
        var freeSprayGun = SprayGuns.FirstOrDefault(sg => !sg.IsBusy);
        if (freeSprayGun != null && PreparedPool.Count > 0)
        {
            // Рабочий переходит к краскопульту и красит все подготовленные изделия
            var batch = new List<Item>();
            while (PreparedPool.Count > 0)
            {
                batch.Add(PreparedPool.Dequeue());
            }

            // Извлекаем первое изделие для немедленной покраски, а остальные пойдут в пачку
            var firstItem = batch[0];
            batch.RemoveAt(0);

            worker.StartPainting(freeSprayGun, batch);
            BusySprayGunsStat.Update(simulator.CurrentTime, CurrentBusySprayGuns);
            BusyWorkersStat.Update(simulator.CurrentTime, CurrentBusyWorkers);

            worker.SetCurrentPaintingItem(firstItem, simulator.CurrentTime);

            double paintTime = SamplePaintingTime();
            simulator.Schedule(new PaintingFinishedEvent(simulator.CurrentTime + paintTime, worker, freeSprayGun, firstItem, this));
        }
        else
        {
            // Краскопульт занят -> рабочий не ждет, а начинает подготовку следующего изделия
            if (PreparationQueue.Count > 0)
            {
                var nextItem = PreparationQueue.Dequeue();
                QueueLengthStat.Update(simulator.CurrentTime, PreparationQueue.Count);

                worker.StartPreparing(nextItem, simulator.CurrentTime);
                BusyWorkersStat.Update(simulator.CurrentTime, CurrentBusyWorkers);

                double prepTime = SamplePreparationTime();
                simulator.Schedule(new PreparationFinishedEvent(simulator.CurrentTime + prepTime, worker, this));
            }
            else
            {
                // Очередь пуста, рабочий ожидает
                BusyWorkersStat.Update(simulator.CurrentTime, CurrentBusyWorkers);
            }
        }
    }

    public void HandlePaintingFinished(Worker worker, SprayGun sprayGun, Item item, Simulator simulator)
    {
        worker.FinishSinglePaintedItem(item, simulator.CurrentTime);
        CompletedItems.Add(item);

        // Учет в статистике (если закончился прогрев)
        if (simulator.IsWarmupCompleted)
        {
            ProcessedItemsCount++;
            TimeInSystemStat.Add(item.TimeInSystem);
        }

        // Проверяем, есть ли еще изделия для окраски у данного рабочего
        if (worker.PaintingBatch.Count > 0)
        {
            var nextItem = worker.PaintingBatch[0];
            worker.PaintingBatch.RemoveAt(0);
            worker.SetCurrentPaintingItem(nextItem, simulator.CurrentTime);

            double paintTime = SamplePaintingTime();
            simulator.Schedule(new PaintingFinishedEvent(simulator.CurrentTime + paintTime, worker, sprayGun, nextItem, this));
        }
        else if (PreparedPool.Count > 0)
        {
            // Во время окраски накопились новые подготовленные изделия - красит и их ("красит все подготовленные изделия")
            var nextItem = PreparedPool.Dequeue();
            worker.SetCurrentPaintingItem(nextItem, simulator.CurrentTime);

            double paintTime = SamplePaintingTime();
            simulator.Schedule(new PaintingFinishedEvent(simulator.CurrentTime + paintTime, worker, sprayGun, nextItem, this));
        }
        else
        {
            // Все подготовленные изделия покрашены -> рабочий освобождает краскопульт
            worker.FinishPainting();
            BusySprayGunsStat.Update(simulator.CurrentTime, CurrentBusySprayGuns);

            // Рабочий возвращается к подготовке изделий
            if (PreparationQueue.Count > 0)
            {
                var nextItem = PreparationQueue.Dequeue();
                QueueLengthStat.Update(simulator.CurrentTime, PreparationQueue.Count);

                worker.StartPreparing(nextItem, simulator.CurrentTime);
                double prepTime = SamplePreparationTime();
                simulator.Schedule(new PreparationFinishedEvent(simulator.CurrentTime + prepTime, worker, this));
            }

            BusyWorkersStat.Update(simulator.CurrentTime, CurrentBusyWorkers);
        }
    }

    public double SampleInterarrivalTime()
    {
        return _rng.NextExponential(Config.ArrivalIntensityPerMinute);
    }

    public double SamplePreparationTime()
    {
        return _rng.NextNormal(Config.PrepMean, Config.PrepSigma, Config.PrepMin);
    }

    public double SamplePaintingTime()
    {
        return _rng.NextNormal(Config.PaintMean, Config.PaintSigma, Config.PaintMin);
    }
}
