using Automation.Helpers;
using Automation.RecordHandlers;
using Automation.TraceBack;
using Microsoft.Playwright;

namespace Automation.Parser
{
    internal class ConnectionParser : IParser
    {
        private readonly string AuthFile;
        private readonly List<string> SearchParam;
        private readonly string Message;
        private readonly string FollowUpText;
        private readonly int TimeOut;
        private readonly bool HeadLess;
        private readonly string CountryCode;
        public ConnectionParser(string auth_file, List<string> search_param, string country_code, string message, string followup_text, int timeout, bool head_less)
        {
            AuthFile = auth_file;
            SearchParam = search_param;
            Message = message;
            FollowUpText = followup_text;
            TimeOut = Utility.MinToMiliSec(timeout);
            HeadLess = head_less;
            CountryCode = country_code;
        }

        [Obsolete]
        public async Task Parse(string username, string password)
        {
            using IPlaywright playwright = await Playwright.CreateAsync();
            BrowserTypeLaunchOptions options = BrowserOptions.BrowserLaunchOptions(TimeOut, HeadLess);
            await using IBrowser browser = await playwright.Chromium.LaunchAsync(options);

            IBrowserContext context;
            IPage page;
            string url = "https://linkedin.com";

            if (File.Exists(AuthFile))
            {
                BrowserNewContextOptions context_options = BrowserOptions.BrowserContextOptions(AuthFile);
                context = await browser.NewContextAsync(context_options);
                page = await context.NewPageAsync();
            }
            else
            {
                context = await browser.NewContextAsync();
                page = await context.NewPageAsync();

                await page.GotoAsync($"{url}/login");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                LoginResponse.AttemptLabel();
                ILocator username_holder = page.Locator("#username");
                ILocator password_holder = page.Locator("#password");
                ILocator button = page.Locator("[aria-label=\"Sign in\"]");

                await username_holder.FillAsync(username);
                await password_holder.FillAsync(password);

                await button.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                ILocator error_marker = page.Locator(".form__input--floating").First;

                if (!await error_marker.IsVisibleAsync())
                {
                    LoginResponse.AttemptSuccess();
                    await context.StorageStateAsync(new()
                    {
                        Path = AuthFile
                    });
                }
                else
                {
                    LoginResponse.FailedResponse();
                    return;
                }
            }

            context.SetDefaultNavigationTimeout(TimeOut);

            Task follower_task = TraceConnections.FollowerTask(context, FollowUpText);
            Task parser_task = ParserTask(context, page);
            await Task.WhenAll(follower_task, parser_task);
        }

        [Obsolete]
        private async Task ParserTask(IBrowserContext context, IPage page)
        {
            const string json_label = "{}";
            const string csv_label = "Keyword,Name,Designation,Company,Website,Phone,Email,LinkedIn,Twitter,Contacted";

            string connection_record_file = FileGenerator.CreateFileAndCombinePath("bin", "Models", "connection.json");
            string csv_destination = FileGenerator.CreateFileAndCombinePath("src", "Records", "connection.csv");

            if (SearchParam.Count > 0)
            {
                foreach (string query in SearchParam)
                {
                    string keyword = Utility.EncodeKeyword(query.Trim());
                    string url = "https://linkedin.com";

                    await page.GotoAsync($"{url}/search/results/people/?keywords={keyword}&network=%5B%22S%22%5D");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    Utility.Interrupt();
                    await page.EvaluateAsync("window.scrollTo(0,document.body.scrollHeight)");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    int total_pages = 1;
                    ILocator last_paginations = page.Locator(".artdeco-pagination__indicator--number").Last;

                    if (await last_paginations.IsVisibleAsync())
                    {
                        ILocator last_pagination = last_paginations.Locator("button").Locator("span");
                        var text = await last_pagination.TextContentAsync();
                        total_pages = Convert.ToInt32(text?.Trim());

                    }

                    for (int i = 1; i <= total_pages; i++)
                    {
                        string buffer_url = $"{url}/search/results/people/?geoUrn=%5B%22{CountryCode}%22%5D&keywords={keyword}&network=%5B%22S%22%5D&origin=FACETED_SEARCH&page={i}";
                        if (CountryCode == string.Empty)
                        {
                            buffer_url = $"{url}/search/results/people/?keywords={keyword}&network=%5B%22S%22%5D&origin=FACETED_SEARCH&page={i}";
                        }

                        await page.GotoAsync(buffer_url);
                        Console.WriteLine($"Page no: {i}");
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                        Utility.Interrupt();

                        ILocator unlisted_a_tags = page.Locator(".entity-result__title-text .app-aware-link");

                        int count = await unlisted_a_tags.CountAsync();
                        HashSet<ILocator> a_tags = new();

                        for (int increment = 0; increment < count; increment++)
                        {
                            ILocator tag = unlisted_a_tags.Nth(increment);
                            a_tags.Add(tag);
                        }

                        foreach (ILocator a_tag in a_tags)
                        {
                            IPage profile_page = await context.NewPageAsync();
                            try
                            {
                                string? profile_url = await a_tag.GetAttributeAsync("href");
                                profile_url = profile_url!.Split('?')[0].TrimEnd('/');

                                await profile_page.GotoAsync(profile_url);
                                await profile_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                Utility.Interrupt();

                                ILocator nameholder = profile_page.Locator(".text-heading-xlarge");
                                string? name = await nameholder.TextContentAsync();

                                ILocator connect_button = profile_page.Locator(".pvs-profile-actions").Last.Locator(@"div[class='pvs-profile-actions__action']").Locator("button").Locator("span:has-text(\"Connect\")");
                                ILocator more_button = profile_page.Locator(".pvs-profile-actions").Last.Locator(".artdeco-dropdown").Locator(@"button:has-text('More')");

                                bool message_flag = false;

                                if (await connect_button.IsVisibleAsync())
                                {
                                    await connect_button.ClickAsync();
                                    Utility.Interrupt();

                                    ILocator add_note_button = profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");
                                    if (await add_note_button.IsVisibleAsync())
                                    {
                                        string custom_message = Utility.CustomizesMessage(name!, Message);
                                        Console.WriteLine($"Sending your customized message to {name}");
                                        await add_note_button.ClickAsync();
                                        ILocator message_box = profile_page.Locator("#custom-message");

                                        await message_box.ClickAsync();
                                        Utility.Interrupt();
                                        await message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                        Utility.Interrupt();
                                        ILocator send_button = profile_page.Locator("button[aria-label=\"Send now\"]");
                                        Utility.Interrupt();
                                        await send_button.ClickAsync();
                                        message_flag = true;
                                        Utility.Interrupt();
                                    }
                                }
                                else if (await more_button.IsVisibleAsync())
                                {
                                    await more_button.ClickAsync();
                                    Utility.Interrupt();

                                    ILocator drop_connect_button = profile_page.Locator(".artdeco-dropdown__content-inner").Last.Locator("ul").Locator("li:has-text(\"Connect\")");
                                    if (await drop_connect_button.IsVisibleAsync())
                                    {
                                        await drop_connect_button.ClickAsync();
                                        Utility.Interrupt();

                                        ILocator add_note_button = profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");
                                        ILocator blocker = profile_page.Locator(".artdeco-modal__header").Locator("#send-invite-modal");
                                        string? blocker_text = await blocker.TextContentAsync();

                                        if (await blocker.IsVisibleAsync() && blocker_text!.Contains("How do you know"))
                                        {
                                            ILocator blocker_dismiss = profile_page.Locator(".artdeco-modal__dismiss[aria-label=\"Dismiss\"]");
                                            await blocker_dismiss.ClickAsync();
                                        }
                                        else if (await add_note_button.IsVisibleAsync())
                                        {
                                            string custom_message = Utility.CustomizesMessage(name!, Message);
                                            Console.WriteLine($"Sending your customized message to {name}");
                                            await add_note_button.ClickAsync();
                                            ILocator message_box = profile_page.Locator("#custom-message");

                                            await message_box.ClickAsync();
                                            Utility.Interrupt();

                                            await message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                            Utility.Interrupt();

                                            ILocator send_button = profile_page.Locator("button[aria-label=\"Send now\"]");
                                            Utility.Interrupt();

                                            await send_button.ClickAsync();
                                            message_flag = true;
                                            Utility.Interrupt();
                                        }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"{name} is not receiving messages from someone not in his/her network");
                                }

                                Console.WriteLine($"Collecting information of {name}");
                                ILocator designation_holder = profile_page.Locator(".text-body-medium").First;
                                ILocator company_holder = profile_page.Locator(".inline-show-more-text").First;

                                string? designation = await designation_holder.TextContentAsync();
                                string? company = await company_holder.TextContentAsync();

                                ILocator contact_popup_link = profile_page.Locator("a:has-text(\"Contact info\")");

                                await contact_popup_link.ClickAsync();

                                Utility.Interrupt(5);

                                ILocator web_holder = profile_page.Locator(".ci-websites").Locator(".pv-contact-info__ci-container").First;
                                ILocator phone_holder = profile_page.Locator(".ci-phone").Locator(".pv-contact-info__ci-container");
                                ILocator email_holder = profile_page.Locator(".ci-email").Locator(".pv-contact-info__ci-container");
                                ILocator twitter_holder = profile_page.Locator(".ci-twitter").Locator(".pv-contact-info__contact-link");


                                const string not_available = "N/A";
                                string? website = await web_holder.IsVisibleAsync() ? await web_holder.TextContentAsync() : not_available;
                                string? phone = await phone_holder.IsVisibleAsync() ? await phone_holder.TextContentAsync() : not_available;
                                string? email = await email_holder.IsVisibleAsync() ? await email_holder.TextContentAsync() : not_available;
                                string? twitter_handle = await twitter_holder.IsVisibleAsync() ? await twitter_holder.GetAttributeAsync("href") : not_available;
                                string _usermame = profile_url.Split("/in/")[1].Trim('/');

                                await profile_page.Locator("[aria-label=\"Dismiss\"]").ClickAsync();

                                RecordLabel.WritingDatabase();
                                ConnectionRecords.SaveRecords(connection_record_file, json_label,
                                    _usermame, query, name!, designation!, company!, website!
                                    , phone!, email!, profile_url, twitter_handle!, message_flag);

                                Utility.Interrupt();
                                JsonToOtherFormats.ConvertToCSV(connection_record_file, csv_destination, csv_label);
                                RecordLabel.RecordAdded();
                                await profile_page.CloseAsync();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                await profile_page.CloseAsync();
                            }
                        }
                    }
                }
            }
        }
    }
}
