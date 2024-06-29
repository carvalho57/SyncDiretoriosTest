using System.IO.Enumeration;
using Microsoft.VisualBasic;

namespace SyncDir;

class Program
{
    static void Main(string[] args)
    {
        string origem = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pasta1");
        string destino = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pasta2");

        using var syncOrigemEDestino = new SyncDirectories(origem, destino);
        syncOrigemEDestino.StartSync();

        Console.WriteLine("Pressione uma tecla para encerrar!");
        Console.ReadKey();
    }
}
