# Scrawl

Scrawl is a crawling automation that passes Cloudflare.

Scrawl is created by using .NET CLI and built on C# and Playwright.

## Installation & Running

- Download the repository.
- Then go inside the folder.
- Run `$ dotnet restore`
- Run `$ dotnet run`

## How it works?

There are 3 key points:

- Replicating a real-user browser
- Avoiding detection from Cloudflare
- Crawling

### 1) Replicating a real-user browser

To achieve this, Scrawl uses 2 methods:

- Removes the webdriver information and checks the bot detection status by looking the webdriver.
- Uses a real Google Chrome

### 2) Avoiding Detection

I noticed that there's a misconfiguration in Cloudflare.

- If Cloudflare secured a domain by redirecting to a subdomain, then origin domain can be requested without verification.
- If Cloudflare detects a mobile device as an automation tool, then it thinks the other device should be the real one.

Scrawl, firstly, passes the secure redirection by going to the origin.

Then starts the iteration with requesting over an iPhone 13. You can change it to any device, but for your information, Apple devices have higher reputation.

### 3) Crawling

This is the most easiest step, just picking the correct element paths and locating it.
