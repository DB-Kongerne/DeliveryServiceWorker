using DeliveryServiceWorker.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace DeliveryServiceWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _rabbitHost;
        private readonly string _queueName = "forsendelsesKø"; // Den samme kø som i ShippingService

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _rabbitHost = configuration["RabbitHost"] ?? "localhost"; // Hent RabbitHost fra appsettings.json
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"Connection to RabbitMQ at {_rabbitHost}");
            var factory = new ConnectionFactory() { HostName = _rabbitHost };

            // Opret en synkron forbindelse
            using (var connection = factory.CreateConnection()) // Bruger CreateConnection i stedet for CreateConnectionAsync
            using (var channel = connection.CreateModel()) // Bruger CreateModel synkront
            {
                // Deklarer køen, hvis den ikke allerede eksisterer
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // Start med at lytte på køen med BasicGet
                while (!stoppingToken.IsCancellationRequested)
                {
                    var result = channel.BasicGet(_queueName, autoAck: true); // Brug synkron BasicGet

                    if (result != null)
                    {
                        var message = Encoding.UTF8.GetString(result.Body.ToArray());

                        // Deserialiser beskeden til ShipmentDelivery-objektet
                        var shipment = JsonSerializer.Deserialize<ShipmentDelivery>(message);

                        if (shipment != null)
                        {
                            _logger.LogInformation($"Modtog forsendelse: {shipment.MedlemsNavn}, {shipment.PakkeId}");
                            // Processér beskeden - fx skriv til CSV
                            WriteShipmentToCsv(shipment);
                        }
                        else
                        {
                            _logger.LogWarning("Modtog ugyldig ShipmentDelivery.");
                        }
                    }
                    else
                    {
                        // Hvis der ikke er nogen besked i køen, vent lidt
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }
        }

        private void WriteShipmentToCsv(ShipmentDelivery shipment)
        {
            string csvFilePath = "shipment_deliveries.csv";

            // Hvis filen ikke findes, opret den med overskrifter
            if (!System.IO.File.Exists(csvFilePath))
            {
                using (var writer = new StreamWriter(csvFilePath, append: false))
                {
                    writer.WriteLine("MedlemsNavn,PickupAdresse,PakkeId,AfleveringsAdresse");
                }
            }

            // Tilføj ShipmentDelivery til CSV-filen
            using (var writer = new StreamWriter(csvFilePath, append: true))
            {
                string line = $"{shipment.MedlemsNavn},{shipment.PickupAdresse},{shipment.PakkeId},{shipment.AfleveringsAdresse}";
                writer.WriteLine(line);
            }

            _logger.LogInformation($"Forsendelse skrevet til CSV: {shipment.MedlemsNavn}, {shipment.PakkeId}");
        }
    }
}
