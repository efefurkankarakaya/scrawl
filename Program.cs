using System.Runtime.InteropServices;
using Microsoft.Playwright;

// Set initial viewports
int[] viewport = new int[2] { 1920, 1080 };
// Define a user data folder to use persistent user data like a real user.
const string userDataDir = "./BrowserUser";

string userAgent = UserAgent.Windows;
if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
{
  viewport[0] = 1512;
  viewport[1] = 982;
  userAgent = UserAgent.MacOS;
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
  userAgent = UserAgent.Windows;
}
else
{
  userAgent = UserAgent.Linux;
}

using var playwright = await Playwright.CreateAsync();
// Create a browser context that uses persistent data.
await using var browser = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new()
{
  Headless = false,
  // To make it seem a real user.
  UserAgent = userAgent,
  // To prevent detection 'cause of Chrome defaults..
  IgnoreDefaultArgs = new string[] { "--enable-automation", "--no-sandbox" }
});
var page = await browser.NewPageAsync();

// Set viewport automatically depends on OS.
await page.SetViewportSizeAsync(viewport[0], viewport[1]);
await page.AddInitScriptAsync("navigation.webdriver = false");

await page.GotoAsync("https://bot.sannysoft.com/");

await page.WaitForTimeoutAsync(100000);

// await page.ScreenshotAsync(new()
// {
//   Path = "screenshot.png"
// });