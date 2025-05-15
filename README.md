# 🛒 Tokullectibles Product Availability Monitor

A console-based C# web scraper that monitors product pages on [Tokullectibles.com](https://tokullectibles.com) and sends you an email notification when a product’s stock status changes.

---

## ✨ Features

- ✅ Scrapes product name, price, and stock status from any Tokullectibles product page
- 🔁 Monitors stock availability at a user-defined interval
- 🔔 Sends an email alert (via Gmail SMTP or any SMTP server) when stock changes
- 📬 Uses MailKit for secure and modern email handling
- 🔐 Configuration via `.env` file to keep credentials private

---

## 📦 Requirements

- [.NET 6+ SDK](https://dotnet.microsoft.com/en-us/download)
- NuGet Packages:
  - `HtmlAgilityPack`
  - `MailKit`
  - `DotNetEnv`

Install them via:

```bash
dotnet add package HtmlAgilityPack
dotnet add package MailKit
dotnet add package DotNetEnv
```

## 🔧 Setup Instructions
1. Clone the Repo
```base
git clone https://github.com/yourusername/tokullectibles-monitor.git
cd tokullectibles-monitor
```
2. Create a .env File
```dotenv
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your_email@gmail.com
SMTP_PASSWORD=your_app_specific_password
EMAIL_FROM=your_email@gmail.com
EMAIL_TO=destination_email@example.com
```
💡 Use an [App Password](https://myaccount.google.com/apppasswords) if you're using Gmail. Regular Gmail passwords will not work.

## 🚀 How to Use
Run the program:
```bash
dotnet run
```
Choose from the menu:

    1 – Scrape a single product page once and print the results (also sends a test email)

    2 – Continuously monitor a product for stock changes and send an email notification

    3 – Exit

You’ll be prompted to enter:

    The product URL (must begin with https://tokullectibles.com)

    How often to check (in seconds)

    How many times to repeat (0 for infinite)

## 🛠 Troubleshooting
❌ Username and Password not accepted error
→ Make sure you're using a Gmail App Password and have 2FA enabled.

❌ Could not open connection to the host, port 587: Connect failed
→ Your network might block outbound port 587. Try port 465, a VPN, or switch networks.

✅ Tip
Use Console.WriteLine(Environment.GetEnvironmentVariable("SMTP_USER")) to debug .env issues.
