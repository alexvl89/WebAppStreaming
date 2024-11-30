using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;
using Kernel = Microsoft.SemanticKernel.Kernel;
using Microsoft.SemanticKernel.Plugins.Memory;
using Codeblaze.SemanticKernel.Connectors.Ollama;
using OllamaSharp;
using System.IO;
using Azure;
using System.Text;

namespace WebAppStreaming.Services;

public interface IOllamaService
{
    Task StreamOllamaResponseAsync(string prompt, Stream outputStream, CancellationToken cancellationToken);
}

public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaService> logger;

    public OllamaService(HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        this.logger = logger;
    }

    public async Task StreamOllamaResponseAsync(string prompt, Stream outputStream, CancellationToken cancellationToken)
    {
        // Настраиваем клиента Ollama
        var uri = new Uri("http://localhost:11434");
        var ollama = new OllamaApiClient(uri);

        // Указываем модель для генерации
        ollama.SelectedModel = "llama3.1:8b";

        // Логируем доступные модели
        var models = await ollama.ListLocalModelsAsync();
        logger.LogInformation("Доступные модели: {Models}", string.Join(", ", models));

        // Логгируем начало обработки
        logger.LogInformation("Начинаем потоковую генерацию для промпта: {Prompt}", prompt);

        try
        {
            await foreach (var stream in ollama.GenerateAsync(prompt))
            {
                // Получаем часть данных и записываем в поток
                var responseData = stream.Response;

                // Логгируем ответ
                logger.LogDebug("Получена часть ответа: {ResponseData}", responseData);

                // Записываем в outputStream
                var buffer = Encoding.UTF8.GetBytes(responseData);
                await outputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);

                // Принудительная отправка данных (особенно полезно для больших потоков)
                await outputStream.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации данных через Ollama.");
            throw;
        }
        finally
        {
            logger.LogInformation("Потоковая генерация завершена.");
        }
    }
}
