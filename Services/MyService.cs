
namespace WebAppStreaming.Services;


public interface IDataGenerationService
{
    Task<IEnumerable<DataPoint>> GetData(int id);
    Task<IEnumerable<string>> GetDataOllama(string text);
}


public class DataPoint
{
    public DateTime TimeStamp { get; set; }
    public double Value { get; set; }
}

public class MyService : IDataGenerationService
{

    public async Task<IEnumerable<DataPoint>> GetData(int id)
    {
        // Симуляция генерации данных
        await Task.Delay(100); // Имитация задержки
        var random = new Random();
        return Enumerable.Range(1, 100).Select(i => new DataPoint
        {
            TimeStamp = DateTime.UtcNow.AddSeconds(i),
            Value = random.NextDouble() * 100
        });
    }

    public Task<IEnumerable<string>> GetDataOllama(string text)
    {
        throw new NotImplementedException();
    }
}
