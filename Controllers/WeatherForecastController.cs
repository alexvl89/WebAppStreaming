using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using WebAppStreaming.Services;

namespace WebAppStreaming.Controllers;

[ApiController]
[Route("[controller]")]
public class Test : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<Test> _logger;
    private readonly IDataGenerationService _myService;
    private readonly IOllamaService _ollamaService;

    public Test(ILogger<Test> logger, IDataGenerationService myService, IOllamaService ollamaService)
    {
        _logger = logger;
        _myService = myService;
        _ollamaService = ollamaService;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }


    [HttpGet]
    [Route("GetStreaming")]
    public async Task GetStremingAsync()
    {
        Response.StatusCode = 200;
        Response.ContentType = "text/html";

        List<WeatherForecast> list = Enumerable.Range(1, 100)
            .Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
        .ToList();

        StreamWriter sw;
        await using ((sw = new StreamWriter(Response.Body))
            .ConfigureAwait(false))
        {

            int i = 0;
            foreach (WeatherForecast item in list)
            {
                // Thread.Sleep simulates a long running process, 
                // which generates some kind of output
                Thread.Sleep(1000);


                string outValue= $"{i++} \t {item.TemperatureC}";

                await sw.WriteLineAsync(outValue).ConfigureAwait(false);
                await sw.FlushAsync().ConfigureAwait(false);
            }
            await sw.WriteLineAsync("[DONE]").ConfigureAwait(false);
        };

    }


    [HttpPost("ollama/stream")]
    public async Task StreamOllamaResponse([FromBody] string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("Prompt не может быть пустым.");
            return;
        }

        Response.ContentType = "text/plain";

        try
        {
            // Открываем поток для записи данных в ответ
            await _ollamaService.StreamOllamaResponseAsync(
                prompt,
                Response.Body,
                HttpContext.RequestAborted
            );
        }
        catch (Exception ex)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Response.WriteAsync($"Ошибка: {ex.Message}");
        }
    }
}
