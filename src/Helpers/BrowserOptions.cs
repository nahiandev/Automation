using Microsoft.Playwright;

namespace Automation.Helpers
{
    internal class BrowserOptions
    {
        private BrowserOptions() {}
        public static LocatorTypeOptions TypeOptions()
        {
            LocatorTypeOptions type_options = new()
            {
                Delay = 0,
                Timeout = 0
            };
            return type_options;
        }
        public static BrowserNewContextOptions BrowserContextOptions(string auth_file)
        {
            BrowserNewContextOptions context_options = new()
            {
                StorageStatePath = auth_file,
                ViewportSize = ViewportSize.NoViewport
                
                //new()
                //{
                //    Height = 0,
                //    Width = 0
                //}
            };
            return context_options;
        }

        public static BrowserTypeLaunchOptions BrowserLaunchOptions(int timeout, bool headless)
        {
            BrowserTypeLaunchOptions options = new()
            {
                Timeout = timeout,
                Headless = headless,
                SlowMo = 2000
            };

            return options;
        }
    }
}
