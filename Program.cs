using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Playwright;

const string outputFolder = "output";
string outputFile = DateTime.Now.ToString("dd-MM-yyyy-h-mm-ss-tt") + ".txt";

if (!Directory.Exists(outputFolder))
{
  Directory.CreateDirectory(outputFolder);
}

using (StreamWriter file = new StreamWriter(Path.Combine(outputFolder, outputFile)))
{
  file.Write("");
}


// Set initial viewports
int[] viewport = new int[2] { 1920, 1080 };
// Define a user data folder to use persistent user data like a real user.
const string userDataDir = "./BrowserUser";

using var playwright = await Playwright.CreateAsync();

var proxy = new Proxy { Server = "per-context" };
await using var browser = await playwright.Chromium.LaunchAsync(new()
{
  Headless = false,
  IgnoreDefaultArgs = new string[] { "--enable-automation", "--no-sandbox" },
  Channel = "chrome",
  // Proxy = proxy
});

var iPhone = playwright.Devices["iPhone 13"];

var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions()
{
  // BaseURL = "https://sahibinden.com",
  // UserAgent = UserAgent.MacOS
});
var iPhoneContext = await browser.NewContextAsync(iPhone);

// Create a browser context that uses persistent data.
// var context = await playwright.Firefox.LaunchPersistentContextAsync(userDataDir, new()
// {
//   Headless = false,
//   // To make it seem a real user.
//   UserAgent = userAgent,
//   // To prevent detection 'cause of Chrome defaults.
//   IgnoreDefaultArgs = new string[] { "--enable-automation", "--no-sandbox" }, // Remove --no-sandbox
//   Channel = "firefox"
//   // ExecutablePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
// });
var page = await browserContext.NewPageAsync();
var detailPage = await browserContext.NewPageAsync();
var iPhonePage = await iPhoneContext.NewPageAsync();

// Set viewport automatically depends on OS.
// await page.SetViewportSizeAsync(viewport[0], viewport[1]);

string script = @"const defaultGetter = Object.getOwnPropertyDescriptor(
      Navigator.prototype,
      'webdriver'
    ).get;
    defaultGetter.apply(navigator);
    defaultGetter.toString();
    Object.defineProperty(Navigator.prototype, 'webdriver', {
      set: undefined,
      enumerable: true,
      configurable: true,
      get: new Proxy(defaultGetter, {
        apply: (target, thisArg, args) => {
          Reflect.apply(target, thisArg, args);
          return false;
        },
      }),
    });
    const patchedGetter = Object.getOwnPropertyDescriptor(
      Navigator.prototype,
      'webdriver'
    ).get;
    patchedGetter.apply(navigator);
    patchedGetter.toString();";

await page.AddInitScriptAsync(script);

await page.GotoAsync("https://bot.sannysoft.com/");

string webdriverResult = await page.Locator("#webdriver-result").TextContentAsync();
if (webdriverResult != "missing (passed)")
{
  throw new Exception("Webdriver test failed.");
}
// await page.GotoAsync("https://fingerprint.com/products/bot-detection/");

await page.GotoAsync("https://www.sahibinden.com/");
await page.WaitForTimeoutAsync(5000); // Wait for Cloudflare is loaded.
await page.GotoAsync("https://www.sahibinden.com/"); // Then go to the origin domain again.

page.Locator(".vitrin-list.clearfix"); // Wait until showcase is loaded.

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
        detailPageURL = "https://sahibinden.com" + URLPostfix;
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