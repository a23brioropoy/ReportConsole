using System;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No se proporcionó un GUID de reporte.");
                return;
            }

            Console.WriteLine($"Argumentos recibidos: {args[0]}");

            Req reqObj = JsonConvert.DeserializeObject<Req>(args[0]);
            string reportId = reqObj.uiid.Trim('\'');

            Console.WriteLine($"ReportId extraído: {reportId}");

            try
            {
                await SendWebhook(reportId, "Iniciando procesamiento del reporte");

                var processedData = new Procesado
                {
                    Key = reportId,
                    Data = DateTime.Now.Ticks,
                    Processed = true,
                    Status = "completed",
                    CTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProcessedData = $"Processed: Report {reportId}"
                };

                await SendWebhook(reportId, "Procesando datos");

                string jsonContent = JsonConvert.SerializeObject(processedData);
                await SendProcessedDataToServer(reportId, jsonContent);

                await SendWebhook(reportId, $"Reporte finalizado con ID {reportId}");

                Console.WriteLine($"Report {reportId} processed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing report {reportId}: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                await SendWebhook(reportId, $"Error en el procesamiento del reporte: {ex.Message}");
            }
        }

        static async Task SendWebhook(string reportId, string status)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        status,
                        guid = reportId
                    }), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://localhost:3000/webhook", content);
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine($"Webhook enviado: {status}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error al enviar webhook: {ex.Message}");
            }
        }

        static async Task SendProcessedDataToServer(string reportId, string processedData)
        {
            try
            {
                string filePath = Path.Combine(@"C:\Users\epsil\Desktop\ProcessedReports", $"{reportId}.json");
                File.WriteAllText(filePath, processedData);

                using (HttpClient client = new HttpClient())
                {
                    StringContent content = new StringContent(JsonConvert.SerializeObject(new
                    {
                        guid = reportId,
                        status = "Reporte procesado"
                    }), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://localhost:3000/webhook", content);
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine("Notificación de datos procesados enviada al servidor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar datos procesados: {ex.Message}");
            }
        }
    }

    class Req
    {
        public string uiid { get; set; }
    }

    class Procesado
    {
        public string Key { get; set; }
        public long Data { get; set; }
        public bool Processed { get; set; }
        public string Status { get; set; }
        public string CTimestamp { get; set; }
        public string ProcessedData { get; set; }
    }
}
