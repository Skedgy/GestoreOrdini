using System.Text.Json;
using System.IO;

namespace GestoreOrdini
{
    public static class OrderStorage
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        private static readonly string OrdersFilePath = Path.Combine(AppContext.BaseDirectory, "orders.json");

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

        public static void AppendOrder(OrderRecord order)
        {
            var orders = GetOrders();
            orders.Add(order);
            var json = JsonSerializer.Serialize(orders, SerializerOptions);
            File.WriteAllText(OrdersFilePath, json);
        }
    }
}
