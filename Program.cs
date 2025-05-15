using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

// This is a simple web scraper that uses HtmlAgilityPack to scrape data from a webpage.
// It fetches the HTML content of the for specified product pages found at Tokullectibles.com and extracts specific information.

namespace TokullectiblesScraper
{
    public class Product
    {
        public string Name { get; set; }
        public string Price { get; set; }
        public required string Url { get; set; }
        public string IsAvailable { get; set; }
    }

    class Program
    {
        private static readonly string BaseUrl = "https://tokullectibles.com";
        private static readonly HttpClient HttpClient = new HttpClient();
        static async Task Main(string[] args)
        {
            Console.WriteLine("Tokullectibles Scraper");
            Console.WriteLine("===========================");

            // Set up HttpClient with appropriate headers
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            HttpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            HttpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");

            try
            {
                bool exit = false;

                while (!exit)
                {
                    Console.WriteLine("Choose an option:");
                    Console.WriteLine("1. Scrape product data");
                    Console.WriteLine("2. Monitor product availability");
                    Console.WriteLine("3. Exit");
                    var choice = Console.ReadLine();
                    switch (choice)
                    {
                        case "1":
                            await ScrapeOnce();
                            break;
                        case "2":
                            await MonitorProductAvailability();
                            break;
                        case "3":
                            exit = true;
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            Console.WriteLine("\nThank you for using the Tokullectibles Scraper!");
        }

        private static async Task MonitorProductAvailability()
        {
            Console.WriteLine("Enter the URL of the product page to monitor:");
            var productUrl = Console.ReadLine();

            if (string.IsNullOrEmpty(productUrl) || !productUrl.StartsWith(BaseUrl))
            {
                Console.WriteLine("Invalid URL. Please try again.");
                return;
            }

            Console.Write("How often (in seconds) would you like to check for availability? ");
            if (!int.TryParse(Console.ReadLine(), out int interval) || interval <= 0)
            {
                Console.WriteLine("Invalid interval. Please enter a positive number.");
                return;
            }

            Console.Write("How many times would you like to check for availability? (0 for indefinite)");
            if (!int.TryParse(Console.ReadLine(), out int iterations) || iterations < 0)
            {
                Console.WriteLine("Invalid number of iterations. Please enter a non-negative number.");
                return;
            }

            Console.WriteLine($"\nMonitoring {productUrl} every {interval} seconds. Press any key to stop...");

            string previousStockStatus = "";
            bool firstcheck = true;
            int checkCount = 0;

            // Start a task to listen for a key press to stop monitoring
            var cancellationTokenSource = new CancellationTokenSource();
            var keyPressTask = Task.Run(() =>
            {
                Console.Read();
                cancellationTokenSource.Cancel();
            });

            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested && (iterations == 0 || checkCount <= iterations))
                {
                    var product = await ScrapeProductData(productUrl);
                    checkCount++;

                    if (product != null)
                    {
                        if (firstcheck)
                        {
                            Console.WriteLine("===================================");
                            Console.WriteLine($"Initial Availability: {product.IsAvailable}");
                            Console.WriteLine("===================================");
                            previousStockStatus = product.IsAvailable;
                            firstcheck = false;
                        }
                        else if (previousStockStatus != product.IsAvailable)
                        {
                            // Stock status has changed
                            Console.WriteLine("===================================");
                            Console.WriteLine($"[{DateTime.Now}] STOCK CHANGE DETECTED: Product is {product.IsAvailable}");
                            Console.WriteLine("===================================");

                            previousStockStatus = product.IsAvailable;

                            // Add logic to send a notification (e.g., email, SMS) here.
                            Console.Beep(880, 1000); // Beep sound for notification, can be replaced eventually
                        }
                        else
                        {
                            // No change in stock status
                            Console.WriteLine($"[{DateTime.Now}] No change in stock status: {product.IsAvailable}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to scrape product data.");
                    }
                    // Wait for the specified interval before checking again
                    try
                    {
                        await Task.Delay(interval * 1000, cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Task was canceled, exit the loop
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("Monitoring stopped.\n");
        }

        private static async Task ScrapeOnce()
        {
            Console.WriteLine("Enter the URL of the product page to scrape:");
            var productUrl = Console.ReadLine();

            if (string.IsNullOrEmpty(productUrl) || !productUrl.StartsWith(BaseUrl))
            {
                Console.WriteLine("Invalid URL. Please try again.");
                return;
            }

            // Scrape the product data
            var product = await ScrapeProductData(productUrl);

            // Display the scraped product data
            Console.WriteLine("\nScraped Product Data:");
            Console.WriteLine("===================================");
            Console.WriteLine($"Name: {product.Name}");
            Console.WriteLine($"Price: {product.Price}");
            Console.WriteLine($"Availability: {product.IsAvailable}");
            Console.WriteLine($"URL: {product.Url}");
            Console.WriteLine("===================================");
        }
        
        private static async Task<Product> ScrapeProductData(string productUrl)
        {
            var productPageHtml = await HttpClient.GetStringAsync(productUrl);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(productPageHtml);

            var product = new Product
            {
                Url = productUrl
            };

            // Extract product name
            var NameNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'product-meta__title')]");
            if (NameNode != null)
            {
                product.Name = NameNode.InnerText.Trim();
            }

            // Extract product availability
            var availabilityNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'product-label')]");
            if (availabilityNode != null)
            {
                product.IsAvailable = availabilityNode.InnerText.Trim();
            }

            // Extract product price
            var priceNode = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(@class, 'price')]");
            if (priceNode != null)
            {
                string priceText = priceNode.InnerText.Trim();
                // Remove "Sale Price" prefix if it exists
                if (priceText.StartsWith("Sale price"))
                {
                    product.Price = priceText.Substring("Sale price".Length);
                }
                else
                {
                    product.Price = priceText;
                }
            }

            return product;
        }
    }
}