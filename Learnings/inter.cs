using System;

public interface IWorker
{
    void Work();
}

public class Worker : IWorker
{
    void IWorker.Work()
    {
        Console.WriteLine("Worker is working");
    }    
}
