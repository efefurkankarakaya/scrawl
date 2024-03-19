using Microsoft.Playwright;

const string baseURL = "https://www.sahibinden.com";
const string outputFolder = "output";
string outputFile = DateTime.Now.ToString("dd-MM-yyyy-h-mm-ss-tt") + ".txt";
// Proxy proxy = new Proxy { Server = "per-context" }; // To support multiple proxies for each tab

if (!Directory.Exists(outputFolder))
{
  Directory.CreateDirectory(outputFolder);
}

using (StreamWriter file = new StreamWriter(Path.Combine(outputFolder, outputFile)))
{
  file.Write("");
}

using var playwright = await Playwright.CreateAsync();

/* Browser Creation */
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
  Headless = false,
  IgnoreDefaultArgs = new string[] { "--enable-automation", "--no-sandbox" },
  Channel = "chrome",
  // Proxy = proxy
});
var iPhone = playwright.Devices["iPhone 13"];

/* Browser and iPhone context (incognito) creation */
var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions()
{ });
var iPhoneContext = await browser.NewContextAsync(iPhone); /* Mobile devices are more likely to pass bot detection, their reputation is higher. */

/* Create Tabs */
var page = await browserContext.NewPageAsync(); /* To see the home */
var detailPage = await browserContext.NewPageAsync(); /* To open detail pages */
var iPhonePage = await iPhoneContext.NewPageAsync(); /* To trick Cloudflare */

await page.AddInitScriptAsync(Utils.initialScript); /* Remove webdriver info from the page */
await detailPage.AddInitScriptAsync(Utils.initialScript); /* Remove webdriver info from the page */
/* We don't remove webdriver info from iPhonePage because we want it to be detected. */

await page.GotoAsync("https://bot.sannysoft.com/"); /* Go to the bot detection website. */

/* Check if we're able to pass webdriver test, otherwise stop. No need to decrease IP reputation. */
string webdriverResult = await page.Locator("#webdriver-result").TextContentAsync();
if (webdriverResult != "missing (passed)")
{
  throw new Exception("Webdriver test failed.");
}

/* This is another bot detection site, but not necessary right now. */
// await page.GotoAsync("https://fingerprint.com/products/bot-detection/");

/* Go to the base URL */
await page.GotoAsync(baseURL);
await page.WaitForTimeoutAsync(5000); /* Wait for Cloudflare is loaded and request verification over secure subdomain. */
await page.GotoAsync(baseURL); /* Then go to the origin domain, it'll allow us to pass. 
As I observed, there's a error in Cloudflare configuration. */

page.Locator(".vitrin-list.clearfix"); /* Wait until showcase is loaded. */

// var items = await page.EvaluateAsync<String[]>(@"() => [...document.querySelectorAll('.vitrin-list.clearfix > li')]");
var items = await page.Locator(".vitrin-list.clearfix > li").AllAsync();
int itemCount = items.Count();
Console.WriteLine(itemCount);

string URLPostfix, detailPageURL, title, price, data;
string[] details = new string[itemCount];
int emptySectionCount = 0, totalPrice = 0;

using (StreamWriter file = new StreamWriter(Path.Combine(outputFolder, outputFile), true))
{
  int index = 1; /* Start index from 1 (only used in displaying the items) */
  foreach (var item in items)
  {
    /* Get the link of the detail page */
    URLPostfix = (await item.Locator("a").GetAttributeAsync("href")).ToString();

    if (!string.IsNullOrEmpty(URLPostfix) && URLPostfix.Contains("/ilan/")) /* Check if it's an ad */
    {
      try
      {
        detailPageURL = baseURL + URLPostfix; /* Build URL of the next detail page */
        Console.WriteLine(detailPageURL);
        await iPhonePage.GotoAsync(detailPageURL); /* Send request to the URL and expect Cloudflare detects that iPhone is actually bot in 2nd request. */
        await detailPage.WaitForTimeoutAsync(5000); /* TODO: Random timeout would be better here */
        await detailPage.GotoAsync(detailPageURL); /* Send request to the URL over Google Chrome (automation informations are overridden above with the configurations) */
        /* Cloudflare detects iPhone as a bot but Cloudflare will think bot is already detected and the secondary browser must be the real one. */

        title = (await detailPage.Locator(".classifiedDetailTitle > h1").TextContentAsync()).ToString().Trim(); /* Get title of the ad */
        price = (await detailPage.Locator("#favoriteClassifiedPrice").GetAttributeAsync("value")).ToString().Trim(); /* Get price of the ad (with the currency) */

        data = index + ") " + title + ": " + price; /* Build the data that's going to be written to the file */
        Console.WriteLine(data);
        file.WriteLine(data); /* Append the file */
        file.Flush(); /* Append file each time it's written, I don't want to be waited until the stream ends. */

        if (!string.IsNullOrEmpty(price)) /* Some ads do not have price */
        {
          string cleanedPrice = price.Substring(0, price.Length - 3).Replace(".", "");
          totalPrice += Int32.Parse(cleanedPrice);
          Console.WriteLine(totalPrice);
        }
        else
        {
          /* If an ad does not have price, then count it like empty section to calculate average accurately. */
          emptySectionCount++;
        }
      }
      catch (Exception error)
      {
        Console.WriteLine("=== START OF ERROR ===");
        Console.WriteLine(error.Message);
        Console.WriteLine("=== END OF ERROR ===");
        /* If any unknown error is happened during crawling, exclude the broken one */
        emptySectionCount++;
      }
    }
    else
    {
      /* If it's not an ad, then count it as empty section */
      emptySectionCount++;
    }
    index++;
  }
}

double average = totalPrice / (itemCount - emptySectionCount); /* Calculate the average */
Console.WriteLine("Average Price: " + average);

await page.WaitForTimeoutAsync(100000);
await browser.CloseAsync();