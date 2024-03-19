using Microsoft.Playwright;

const string baseURL = "https://www.sahibinden.com";
const string outputFolder = "output";
string outputFile = DateTime.Now.ToString("dd-MM-yyyy-h-mm-ss-tt") + ".txt";
var proxy = new Proxy { Server = "per-context" }; // To support multiple proxies for each tab

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
await page.WaitForTimeoutAsync(5000); /* Wait for Cloudflare is loaded and request verification. */
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
  int index = 1;
  foreach (var item in items)
  {
    URLPostfix = (await item.Locator("a").GetAttributeAsync("href")).ToString();

    if (!string.IsNullOrEmpty(URLPostfix) && URLPostfix.Contains("/ilan/"))
    {
      try
      {
        detailPageURL = baseURL + URLPostfix;
        Console.WriteLine(detailPageURL);
        await iPhonePage.GotoAsync(detailPageURL);
        await detailPage.WaitForTimeoutAsync(5000);
        await detailPage.GotoAsync(detailPageURL);
        title = (await detailPage.Locator(".classifiedDetailTitle > h1").TextContentAsync()).ToString().Trim();
        price = (await detailPage.Locator("#favoriteClassifiedPrice").GetAttributeAsync("value")).ToString().Trim();

        data = index + ") " + title + ": " + price;
        Console.WriteLine(data);
        file.WriteLine(data);
        file.Flush();

        if (!string.IsNullOrEmpty(price))
        {
          string cleanedPrice = price.Substring(0, price.Length - 3).Replace(".", "");
          totalPrice += Int32.Parse(cleanedPrice);
          Console.WriteLine(totalPrice);
        }
        else
        {
          emptySectionCount++;
        }
      }
      catch (Exception error)
      {
        Console.WriteLine(error.Data.ToString());
        emptySectionCount++;
      }

      await page.WaitForTimeoutAsync(10000);
    }
    else
    {
      emptySectionCount++;
    }
    index++;
  }
}

int average = totalPrice / (itemCount - emptySectionCount);
Console.WriteLine("Average Price: " + average);

await page.WaitForTimeoutAsync(100000);