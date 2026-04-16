using System.Text.Json;
using System.IO;

namespace GestoreOrdini
{
    // Salvataggio e recupero ordini su file JSON locale.
    public static class OrderStorage
    {
        // Opzioni comuni per serializzazione/deserializzazione JSON.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        // File locale che contiene tutti gli ordini.
        private static readonly string OrdersFilePath = Path.Combine(AppContext.BaseDirectory, "orders.json");

        // Legge gli ordini; in caso di errore restituisce lista vuota.
        public static List<OrderRecord> GetOrders()
        {
            try
            {
                if (!File.Exists(OrdersFilePath))
                {
                    return [];
                }

                var json = File.ReadAllText(OrdersFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return [];
                }

                return JsonSerializer.Deserialize<List<OrderRecord>>(json, SerializerOptions) ?? [];
            }
            catch
            {
                return [];
            }
        }

        // Aggiunge un nuovo ordine mantenendo quelli già presenti.
        public static void AppendOrder(OrderRecord order)
        {
            var orders = GetOrders();
            orders.Add(order);
            var json = JsonSerializer.Serialize(orders, SerializerOptions);
            File.WriteAllText(OrdersFilePath, json);
        }
    }
}
