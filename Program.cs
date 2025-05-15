using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using DotNetEnv;

// This is a simple web scraper that uses HtmlAgilityPack to scrape data from a webpage.
// It fetches the HTML content of the for specified product pages found at Tokullectibles.com and extracts specific information.

namespace TokullectiblesScraper
{
    // This class is responsible for sending email notifications using SMTP.
    // It reads SMTP configuration from environment variables.
    // The SendAsync method sends an email with the specified subject and body.
    internal static class EmailNotifier
    {
        // Read everything from .env file
        public static async Task SendAsync(string subject, string body)
        {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPortStr = Environment.GetEnvironmentVariable("SMTP_PORT");
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var fromEmail = Environment.GetEnvironmentVariable("EMAIL_FROM");
            var toEmail = Environment.GetEnvironmentVariable("EMAIL_TO");

            int smtpPort = int.TryParse(smtpPortStr, out int port) ? port : 587; // Default to 587 if parsing fails

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }

    // This class represents a product with properties for its name, price, URL, and availability status.
    // The properties are used to store the scraped data from the product page.
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
            // Load environment variables from .env file
            DotNetEnv.Env.Load();

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

        // This method monitors the availability of a product at regular intervals.
        private static async Task MonitorProductAvailability()
        {
            Console.WriteLine("Enter the URL of the product page to monitor:");
            var productUrl = Console.ReadLine();

            if (string.IsNullOrEmpty(productUrl) || !productUrl.StartsWith(BaseUrl))
            {
                Console.WriteLine("Invalid URL. Please try again.");
                return;
            }

            // Ask user for the interval and number of iterations
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

            // Start monitoring the product availability
            try
            {
                // Wait for the task to complete or the cancellation token to be triggered
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
                            // Console.Beep(880, 1000); // Beep sound for notification, can be replaced eventually
                            await EmailNotifier.SendAsync(
                                subject: $"STOCK CHANGE DETECTED: {product.Name}",
                                body: $"[{DateTime.Now}] STOCK CHANGE DETECTED: Product is {product.IsAvailable}\n\n{product.Url}"
                            );

                            Console.WriteLine("Email notification sent.");
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

        // This method scrapes product data from a single product page.
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
            Console.WriteLine("===============================");

            // Test email notification
            // await EmailNotifier.SendAsync(
            //     "Test Email", "If you're reading this, your SMTP settings work!"
            // );

        }

        // Helper function which scrapes product data from the specified product URL.
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