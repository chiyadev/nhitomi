using System;

namespace nhitomi.Scrapers
{
    public static class ScraperAgent
    {
        public static string GetUserAgent()
        {
            var random = new Random();

            var os = random.Next(3) switch
            {
                0 => "Windows NT 10.0; Win64; x64",
                1 => $"Macintosh; Intel Mac OS X 10_{random.Next(12, 16)}_{random.Next(0, 10)}",
                2 => "X11; Linux x86_64",
                _ => null
            };

            // 537.36 never changes https://www.reddit.com/r/chrome/comments/47ft7n/why_is_chrome_stuck_on_blink_53736/
            return $"Mozilla/5.0 ({os}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{random.Next(80, 84)}.0.{random.Next(4000, 4100)}.{random.Next(0, 100)} Safari/537.36";
        }
    }
}