using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class PosApp
{
    private static Dictionary<string, Product> products = new Dictionary<string, Product>();
    private static Dictionary<string, Customer> customers = new Dictionary<string, Customer>();
    private static Dictionary<string, Receipt> receipts = new Dictionary<string, Receipt>();

    private static readonly string Green = "\x1b[32m";
    private static readonly string Yellow = "\x1b[33m";
    private static readonly string Blue = "\x1b[34m";
    private static readonly string Reset = "\x1b[0m";

    public static void Main(string[] args)
    {
        LoadData("products.json", ref products);
        LoadData("customers.json", ref customers);
        LoadData("receipts.json", ref receipts);

        int selectedIndex = 0;
        string[] options = {
            "View Products", "Add Product", "Update Product", "Delete Product", "Checkout",
            "Add Customer", "View Customers", "View Receipts", "Clear Receipts", "Exit"
        };

        while (true)
        {
            ClearTerminal();
            Console.WriteLine($"{Blue}--- POS Console ---{Reset}");
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.WriteLine($"{Green}> {options[i]}{Reset}");
                }
                else
                {
                    Console.WriteLine($"  {options[i]}");
                }
            }

            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex == 0) ? options.Length - 1 : selectedIndex - 1;
                    break;
                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex == options.Length - 1) ? 0 : selectedIndex + 1;
                    break;
                case ConsoleKey.Enter:
                    HandleMenuSelection(selectedIndex);
                    break;
                case ConsoleKey.Tab:
                    selectedIndex = (selectedIndex == options.Length - 1) ? 0 : selectedIndex + 1;
                    break;
            }
        }
    }

    private static void HandleMenuSelection(int selectedIndex)
    {
        try
        {
            switch (selectedIndex)
            {
                case 0: DisplayProducts(); break;
                case 1: AddProduct(); break;
                case 2: UpdateProduct(); break;
                case 3: DeleteProduct(); break;
                case 4: Checkout(); break;
                case 5: AddCustomer(); break;
                case 6: DisplayCustomers(); break;
                case 7: DisplayReceipts(); break;
                case 8: ClearReceipts(); break;
                case 9: Environment.Exit(0); break;
                default: Console.WriteLine($"{Yellow}Invalid choice. Please try again.{Reset}"); break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Yellow}An error occurred: {ex.Message}{Reset}");
        }

        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static void ClearTerminal()
    {
        Console.Clear();
    }

    private static void LoadData<T>(string filename, ref Dictionary<string, T> data)
    {
        try
        {
            if (File.Exists(filename))
            {
                string json = File.ReadAllText(filename);
                data = JsonSerializer.Deserialize<Dictionary<string, T>>(json) ?? new Dictionary<string, T>();
            }
            else
            {
                data = new Dictionary<string, T>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Yellow}Error loading {filename}: {ex.Message}{Reset}");
            data = new Dictionary<string, T>();
        }
    }

    private static void SaveData<T>(string filename, Dictionary<string, T> data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{Yellow}Error saving {filename}: {ex.Message}{Reset}");
        }
    }

    private static void DisplayProducts()
    {
        Console.WriteLine($"{Green}\n--- Product List ---{Reset}");
        if (products.Count == 0)
        {
            Console.WriteLine($"{Yellow}No products available.{Reset}");
            return;
        }

        foreach (var product in products)
        {
            Console.WriteLine($"ID: {Blue}{product.Key}{Reset}, Name: {product.Value.Name}, Price: ${product.Value.Price:F2}");
        }
    }

    private static void AddProduct()
    {
        Console.Write("Enter product ID: ");
        string id = Console.ReadLine();
        if (string.IsNullOrEmpty(id)) return;
        if (products.ContainsKey(id))
        {
            Console.WriteLine($"{Yellow}Product ID already exists.{Reset}");
            return;
        }

        Console.Write("Enter product name: ");
        string name = Console.ReadLine();

        Console.Write("Enter product price: ");
        if (double.TryParse(Console.ReadLine(), out double price) && price >= 0)
        {
            products[id] = new Product { Name = name, Price = price };
            SaveData("products.json", products);
            Console.WriteLine($"{Green}Product '{name}' added successfully.{Reset}");
        }
        else
        {
            Console.WriteLine($"{Yellow}Invalid price input.{Reset}");
        }
    }

    private static void UpdateProduct()
    {
        Console.Write("Enter product ID to update: ");
        string id = Console.ReadLine();
        if (string.IsNullOrEmpty(id)) return;
        if (!products.ContainsKey(id))
        {
            Console.WriteLine($"{Yellow}Product ID not found.{Reset}");
            return;
        }

        Console.Write($"Enter new name for '{products[id].Name}' (or press Enter to keep): ");
        string name = Console.ReadLine();
        if (!string.IsNullOrEmpty(name))
        {
            products[id].Name = name;
        }

        Console.Write($"Enter new price for '{products[id].Name}' (or press Enter to keep): ");
        if (double.TryParse(Console.ReadLine(), out double price) && price >= 0)
        {
            products[id].Price = price;
            SaveData("products.json", products);
            Console.WriteLine($"{Green}Product '{id}' updated successfully.{Reset}");
        }
        else if (!string.IsNullOrEmpty(Console.ReadLine()))
        {
            Console.WriteLine($"{Yellow}Invalid price input.{Reset}");
        }
    }

    private static void DeleteProduct()
    {
        Console.Write("Enter product ID to delete: ");
        string id = Console.ReadLine();
        if (string.IsNullOrEmpty(id)) return;
        if (products.Remove(id))
        {
            SaveData("products.json", products);
            Console.WriteLine($"{Green}Product '{id}' deleted successfully.{Reset}");
        }
        else
        {
            Console.WriteLine($"{Yellow}Product ID not found.{Reset}");
        }
    }

    private static void Checkout()
    {
        Dictionary<string, int> cart = new Dictionary<string, int>();
        double total = 0;

        string customerId = GetCustomerId();
        string customerName = customerId == "guest" ? "Guest" : customers[customerId].Name;

        while (true)
        {
            DisplayProducts(); // Display products with IDs during checkout
            Console.Write("Enter product ID (or 'done' to finish, 'cancel' to cancel): ");
            string productId = Console.ReadLine();

            if (productId.ToLower() == "done") break;
            if (productId.ToLower() == "cancel")
            {
                Console.WriteLine($"{Yellow}Checkout canceled.{Reset}");
                return;
            }

            if (!products.ContainsKey(productId))
            {
                Console.WriteLine($"{Yellow}Product ID not found.{Reset}");
                continue;
            }

            Console.Write($"Enter quantity for '{products[productId].Name}': ");
            if (int.TryParse(Console.ReadLine(), out int quantity) && quantity > 0)
            {
                cart[productId] = cart.ContainsKey(productId) ? cart[productId] + quantity : quantity;
            }
            else
            {
                Console.WriteLine($"{Yellow}Invalid quantity input.{Reset}");
            }
        }

        Console.WriteLine($"{Green}\n--- Receipt ---{Reset}");
        foreach (var item in cart)
        {
            double subtotal = products[item.Key].Price * item.Value;
            total += subtotal;
            Console.WriteLine($"{products[item.Key].Name} x{item.Value} = ${subtotal:F2}");
        }
        Console.WriteLine($"{Green}Total: ${total:F2}{Reset}");

        string receiptId = DateTime.Now.Ticks.ToString();
        receipts[receiptId] = new Receipt
        {
            CustomerId = customerId,
            CustomerName = customerName,
            Items = cart,
            Total = total,
            Timestamp = DateTime.Now
        };
        SaveData("receipts.json", receipts);

        Console.WriteLine("Press Enter to finish transaction.");
        Console.ReadLine();
    }

    private static string GetCustomerId()
    {
        while (true)
        {
            Console.Write("Enter customer ID (or 'list' to view customers, or leave blank for guest): ");
            string input = Console.ReadLine();
            if (input == null) continue;
            if (input.ToLower() == "list")
            {
                DisplayCustomers();
                continue;
            }

            if (string.IsNullOrEmpty(input)) return "guest";
            if (customers.ContainsKey(input)) return input;

            Console.WriteLine($"{Yellow}Customer ID not found.{Reset}");
        }
    }

    private static void AddCustomer()
    {
        Console.Write("Enter customer ID: ");
        string id = Console.ReadLine();
        if (id == null) return;
        if (customers.ContainsKey(id))
        {
            Console.WriteLine($"{Yellow}Customer ID already exists.{Reset}");
            return;
        }

        Console.Write("Enter customer name: ");
        string name = Console.ReadLine();

        customers[id] = new Customer { Name = name };
        SaveData("customers.json", customers);
        Console.WriteLine($"{Green}Customer '{name}' added successfully.{Reset}");
    }

    private static void DisplayCustomers()
    {
        Console.WriteLine($"{Green}\n--- Customer List ---{Reset}");
        if (customers.Count == 0)
        {
            Console.WriteLine($"{Yellow}No customers available.{Reset}");
            return;
        }

        foreach (var customer in customers)
        {
            Console.WriteLine($"ID: {Blue}{customer.Key}{Reset}, Name: {customer.Value.Name}");
        }
    }

    private static void DisplayReceipts()
    {
        Console.WriteLine($"{Green}\n--- Receipt List ---{Reset}");
        if (receipts.Count == 0)
        {
            Console.WriteLine($"{Yellow}No receipts available.{Reset}");
            return;
        }

        foreach (var receipt in receipts)
        {
            Console.WriteLine($"{Blue}\nReceipt ID: {receipt.Key}{Reset}");
            Console.WriteLine($"Customer: {receipt.Value.CustomerName} (ID: {receipt.Value.CustomerId})");
            foreach (var item in receipt.Value.Items)
            {
                Console.WriteLine($"  {products[item.Key].Name} x{item.Value}");
            }
            Console.WriteLine($"  Total: ${receipt.Value.Total:F2}");
            Console.WriteLine($"  Timestamp: {receipt.Value.Timestamp}");
        }
    }

    private static void ClearReceipts()
    {
        receipts.Clear();
        SaveData("receipts.json", receipts);
        Console.WriteLine($"{Green}Receipts cleared successfully.{Reset}");
    }
}

public class Product
{
    public string Name { get; set; } = "";
    public double Price { get; set; }
}

public class Customer
{
    public string Name { get; set; } = "";
}

public class Receipt
{
    public string CustomerId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
    public double Total { get; set; }
    public DateTime Timestamp { get; set; }
}
