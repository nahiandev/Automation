using Automation.Helpers;
using Automation.RecordHandlers;
using Automation.TraceBack;
using Microsoft.Playwright;

namespace Automation.Parser
{
    internal class JobParser : IParser
    {
        private readonly string AuthFile;
        private readonly List<string> SearchParam;
        private readonly string Message;
        private readonly string FollowUpText;
        private readonly int TimeOut;
        private readonly bool HeadLess;
        private readonly string CountryCode;
        public JobParser(string auth_file, List<string> search_param, string country_code, string message, string followup_text, int timeout, bool head_less)
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
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                LoginResponse.AttemptLabel();
                ILocator username_holder = page.Locator("#username");
                ILocator password_holder = page.Locator("#password");
                ILocator button = page.Locator("[aria-label=\"Sign in\"]");

                await username_holder.FillAsync(username);
                await password_holder.FillAsync(password);

                await button.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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
            string jobs_record_file = FileGenerator.CreateFileAndCombinePath("bin", "Models", "jobs.json");
            
            const string csv_label = "Job Title,Job Type,Job Location,Job URL,Company Name,Company Website,Company Size,Recruiter Name,Recruiter LinkedIn,Ceo Name, Ceo LinkedIn";
            string csv_destination = FileGenerator.CreateFileAndCombinePath("src", "Records", "jobs.csv");

            string url = "https://linkedin.com";

            if (SearchParam.Count > 0)
            {
                foreach (string query in SearchParam)
                {
                    string keyword = Utility.EncodeKeyword(query.Trim());
                    string geo_id = string.Empty;
                    if (CountryCode != string.Empty)
                    {
                        geo_id = $"&geoId={CountryCode}";
                    }
                    string url_without_page_no = $"{url}/jobs/search/?f_TPR=r86400{geo_id}&keywords={keyword}&refresh=true";
                    await page.GotoAsync(url_without_page_no);
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    Utility.Interrupt();

                    await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    Utility.Interrupt();

                    ILocator no_match = page.Locator("h1[text=\"No matching jobs found.\"]");
                    ILocator last_pagination_button = page.Locator(".artdeco-pagination__indicator--number").Last.Locator("button");

                    bool no_match_seen = await no_match.IsVisibleAsync();
                    bool pagination_seen = await last_pagination_button.IsVisibleAsync();

                    string? num_placeholder = "1";

                    ILocator jobs_number_holder = page.Locator(".jobs-search-results-list__title-heading").Locator("small").Last;
                    string jobs_number = (await jobs_number_holder.InnerTextAsync()).Split(' ')[0];

                    jobs_number = jobs_number.Replace(",", "").Trim();
                    int jobs = Convert.ToInt32(jobs_number);

                    while (!pagination_seen && !no_match_seen)
                    {
                        await page.ReloadAsync();
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                        Utility.Interrupt(5);
                        last_pagination_button = page.Locator(".artdeco-pagination__indicator--number").Last.Locator("button");
                        if (pagination_seen || jobs <= 25) break;
                    }

                    if (pagination_seen)
                    {
                        num_placeholder = await last_pagination_button.TextContentAsync();
                    }

                    int total_pages = Convert.ToInt32(num_placeholder);

                    for (int i = 0; i < total_pages; i++)
                    {
                        string page_no = $"&start={i * 25}";
                        Console.WriteLine($"Page no:- {i + 1}");

                        if (i < 0) page_no = string.Empty;

                        string jobs_pagination_url = $"{url_without_page_no}{page_no}";
                        await page.GotoAsync(jobs_pagination_url);
                        await page.WaitForLoadStateAsync(LoadState.Load);
                        Utility.Interrupt();

                        ILocator job_list_items = page.Locator(".scaffold-layout__list-container").Locator(".jobs-search-results__list-item");

                        int length = await job_list_items.CountAsync();

                        List<ILocator> all_li = new();
                        for (int increment = 0; increment < length; increment++)
                        {
                            all_li.Add(job_list_items.Nth(increment));
                        }

                        foreach (ILocator li in all_li)
                        {
                            await li.ClickAsync();
                            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                            Utility.Interrupt();

                            const string not_available = "N/A";
                            string job_id = page.Url.Split("currentJobId=")[1].Split("&f_TPR")[0];
                            string job_title = not_available;
                            string job_type = not_available;
                            string job_location = not_available;
                            string job_url = not_available;
                            string? recruiter_name = not_available;
                            string? recruiter_linkedin = not_available;
                            string company_name = not_available;
                            string? company_website = not_available;
                            string company_size = not_available;
                            string? ceo_name = not_available;
                            string? ceo_linkedin = not_available;

                            try
                            {
                                ILocator job_title_holder = page.Locator(".jobs-unified-top-card__content--two-pane").Locator("a").Locator(".jobs-unified-top-card__job-title");
                                ILocator job_type_holder = page.Locator(".jobs-unified-top-card__job-insight").First.Locator("span").First;
                                ILocator job_location_holder = page.Locator(".jobs-unified-top-card__bullet").First;
                                ILocator job_url_holder = page.Locator(".jobs-unified-top-card__content--two-pane").Locator("a").First;

                                job_title = await job_title_holder.InnerTextAsync();
                                job_type = await job_type_holder.InnerTextAsync();
                                job_location = await job_location_holder.InnerTextAsync();
                                job_url = $"{url}{await job_url_holder.GetAttributeAsync("href")}";

                                await page.EvaluateAsync("window.scrollTo(0,document.body.scrollHeight)");
                                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                Utility.Interrupt();

                                ILocator hiring_card = page.Locator(".t-20:has-text(\"Meet the hiring team\")");

                                if (await hiring_card.IsVisibleAsync())
                                {
                                    IPage recruiter_profile_page = await context.NewPageAsync();
                                    ILocator url_holder = page.Locator(".hirer-card__container").Locator(".hirer-card__hirer-information").Locator(".app-aware-link");
                                    recruiter_linkedin = await url_holder.GetAttributeAsync("href");

                                    await recruiter_profile_page.GotoAsync(recruiter_linkedin!);
                                    await recruiter_profile_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                    Utility.Interrupt();

                                    try
                                    {
                                        ILocator recruiter_nameholder = recruiter_profile_page.Locator(".text-heading-xlarge");
                                        recruiter_name = await recruiter_nameholder.TextContentAsync();

                                        ILocator recruiter_connect_button = recruiter_profile_page.Locator(".pvs-profile-actions").Last.Locator(@"div[class='pvs-profile-actions__action']").Locator("button").Locator("span:has-text(\"Connect\")");

                                        ILocator recruiter_more_button = recruiter_profile_page.Locator(".pvs-profile-actions").Last.Locator(".artdeco-dropdown").Locator(@"button:has-text('More')");

                                        if (await recruiter_connect_button.IsVisibleAsync())
                                        {
                                            await recruiter_connect_button.ClickAsync();
                                            Utility.Interrupt();

                                            ILocator add_note_button = recruiter_profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");

                                            if (await add_note_button.IsVisibleAsync())
                                            {
                                                string custom_message = Utility.CustomizesMessage(recruiter_name!, Message);
                                                Console.WriteLine($"Sending your customized message to {recruiter_name}");

                                                await add_note_button.ClickAsync();
                                                ILocator message_box = recruiter_profile_page.Locator("#custom-message");
                                                await message_box.ClickAsync();
                                                Utility.Interrupt();
                                                await message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                                Utility.Interrupt();

                                                ILocator send_button = recruiter_profile_page.Locator("button[aria-label=\"Send now\"]");

                                                Utility.Interrupt();
                                                await send_button.ClickAsync();

                                                Utility.Interrupt();
                                            }
                                        }

                                        else if (await recruiter_more_button.IsVisibleAsync())
                                        {
                                            await recruiter_more_button.ClickAsync();
                                            Utility.Interrupt();

                                            ILocator recruiter_drop_connect_button = recruiter_profile_page.Locator(".artdeco-dropdown__content-inner").Last.Locator("ul").Locator("li:has-text(\"Connect\")");

                                            if (await recruiter_drop_connect_button.IsVisibleAsync())
                                            {
                                                await recruiter_drop_connect_button.ClickAsync();
                                                Utility.Interrupt();

                                                ILocator recruiter_add_note_button = recruiter_profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");

                                                ILocator blocker = recruiter_profile_page.Locator(".artdeco-modal__header").Locator("#send-invite-modal");
                                                string? blocker_text = await blocker.TextContentAsync();

                                                if (await blocker.IsVisibleAsync() && blocker_text!.Contains("How do you know"))
                                                {
                                                    ILocator blocker_dismiss = recruiter_profile_page.Locator(".artdeco-modal__dismiss[aria-label=\"Dismiss\"]");
                                                    await blocker_dismiss.ClickAsync();
                                                    Console.WriteLine($"{recruiter_name} is not receiving messages from someone not in his/her network");
                                                }
                                                else if (await recruiter_add_note_button.IsVisibleAsync())
                                                {
                                                    string custom_message = Utility.CustomizesMessage(recruiter_name!, Message);
                                                    Console.WriteLine($"Sending your customized message to {recruiter_name}");
                                                    await recruiter_add_note_button.ClickAsync();
                                                    ILocator message_box = recruiter_profile_page.Locator("#custom-message");

                                                    await message_box.ClickAsync();
                                                    Utility.Interrupt();

                                                    await message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                                    Utility.Interrupt();

                                                    ILocator recruiter_send_button = recruiter_profile_page.Locator("button[aria-label=\"Send now\"]");

                                                    Utility.Interrupt();
                                                    await recruiter_send_button.ClickAsync();
                                                    Utility.Interrupt();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{recruiter_name} is not receiving messages from someone not in his/her network");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    await recruiter_profile_page.CloseAsync();
                                }

                                ILocator company_profile_url_holder = page.Locator(".jobs-unified-top-card__company-name").Locator("a");
                                string? href = await company_profile_url_holder.GetAttributeAsync("href");
                                href = href!.Replace("life", "about");
                                string company_profile_url = $"{url}{href}";

                                IPage company_page = await context.NewPageAsync();
                                await company_page.GotoAsync(company_profile_url);
                                await company_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                await company_page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                                await company_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                Utility.Interrupt();

                                ILocator company_name_holder = company_page.Locator(".org-top-card__primary-content").Locator(".mt2").Locator("div").First.Locator("h1");
                                ILocator company_website_holder = company_page.Locator("a.link-without-visited-state").Last.Locator("span.link-without-visited-state");
                                ILocator company_size_holder = company_page.Locator(".overflow-hidden").Locator(".text-body-small").Nth(2);

                                company_name = await company_name_holder.InnerTextAsync();
                                company_website = await company_website_holder.InnerTextAsync();
                                company_size = await company_size_holder.InnerTextAsync();


                                href = href.Replace("about", "people");
                                href = $"{url}{href}?keywords=ceo";
                                await company_page.GotoAsync(href);
                                await company_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                Utility.Interrupt();

                                ILocator no_employees = company_page.Locator("text=\"0 employees\"");
                                if (!await no_employees.IsVisibleAsync())
                                {
                                    ILocator ceo_cards = company_page.Locator(".artdeco-entity-lockup__content");

                                    int count = await ceo_cards.CountAsync();
                                    for (int h = 0; h < count; h++)
                                    {
                                        ILocator ceo_card = ceo_cards.Nth(h);

                                        ILocator designation = ceo_card.Locator(".artdeco-entity-lockup__subtitle").Locator(".t-14").Locator(".lt-line-clamp").First;

                                        string inner_text = (await designation.InnerTextAsync()).ToLower();
                                        if (inner_text.Contains("ceo"))
                                        {
                                            ILocator ceo_url_holder = ceo_card.Locator(".artdeco-entity-lockup__title").Locator("a");

                                            ceo_linkedin = await ceo_url_holder.GetAttributeAsync("href");
                                            break;
                                        }
                                    }
                                }
                                await company_page.CloseAsync();

                                Utility.Interrupt();
                                if (ceo_linkedin != not_available)
                                {
                                    IPage ceo_profile_page = await context.NewPageAsync();
                                    await ceo_profile_page.GotoAsync(ceo_linkedin!);
                                    await ceo_profile_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                    Utility.Interrupt();

                                    try
                                    {
                                        ILocator ceo_nameholder = ceo_profile_page.Locator(".text-heading-xlarge");
                                        ceo_name = await ceo_nameholder.TextContentAsync();

                                        ILocator ceo_connect_button = ceo_profile_page.Locator(".pvs-profile-actions").Last.Locator(@"div[class='pvs-profile-actions__action']").Locator("button").Locator("span:has-text(\"Connect\")");

                                        ILocator ceo_more_button = ceo_profile_page.Locator(".pvs-profile-actions").Last.Locator(".artdeco-dropdown").Locator(@"button:has-text('More')");

                                        if (await ceo_connect_button.IsVisibleAsync())
                                        {
                                            await ceo_connect_button.ClickAsync();
                                            Utility.Interrupt();

                                            ILocator ceo_add_note_button = ceo_profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");

                                            if (await ceo_add_note_button.IsVisibleAsync())
                                            {
                                                string custom_message = Utility.CustomizesMessage(ceo_name!, Message);
                                                Console.WriteLine($"Sending your customized message to {ceo_name}");

                                                await ceo_add_note_button.ClickAsync();
                                                ILocator message_box = ceo_profile_page.Locator("#custom-message");
                                                await message_box.ClickAsync();
                                                Utility.Interrupt();
                                                await message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                                Utility.Interrupt();

                                                ILocator ceo_send_button = ceo_profile_page.Locator("button[aria-label=\"Send now\"]");

                                                Utility.Interrupt();
                                                await ceo_send_button.ClickAsync();
                                                Utility.Interrupt();
                                            }
                                        }

                                        else if (await ceo_more_button.IsVisibleAsync())
                                        {
                                            await ceo_more_button.ClickAsync();
                                            Utility.Interrupt();

                                            ILocator ceo_drop_connect_button = ceo_profile_page.Locator(".artdeco-dropdown__content-inner").Last.Locator("ul").Locator("li:has-text(\"Connect\")");

                                            if (await ceo_drop_connect_button.IsVisibleAsync())
                                            {
                                                await ceo_drop_connect_button.ClickAsync();
                                                Utility.Interrupt();

                                                ILocator ceo_add_note_button = ceo_profile_page.Locator(".send-invite").Locator(".artdeco-modal__actionbar").Locator("button[aria-label=\"Add a note\"]");

                                                ILocator ceo_blocker = ceo_profile_page.Locator(".artdeco-modal__header").Locator("#send-invite-modal");
                                                string? blocker_text = await ceo_blocker.TextContentAsync();

                                                if (await ceo_blocker.IsVisibleAsync() && blocker_text!.Contains("How do you know"))
                                                {
                                                    ILocator blocker_dismiss = ceo_profile_page.Locator(".artdeco-modal__dismiss[aria-label=\"Dismiss\"]");
                                                    await blocker_dismiss.ClickAsync();
                                                    Console.WriteLine($"{ceo_name} is not receiving messages from someone not in his/her network");
                                                }
                                                else if (await ceo_add_note_button.IsVisibleAsync())
                                                {
                                                    string custom_message = Utility.CustomizesMessage(ceo_name!, Message);
                                                    Console.WriteLine($"Sending your customized message to {ceo_name}");
                                                    await ceo_add_note_button.ClickAsync();
                                                    ILocator ceo_message_box = ceo_profile_page.Locator("#custom-message");

                                                    await ceo_message_box.ClickAsync();
                                                    Utility.Interrupt();

                                                    await ceo_message_box.TypeAsync(custom_message, BrowserOptions.TypeOptions());
                                                    Utility.Interrupt();

                                                    ILocator ceo_send_button = ceo_profile_page.Locator("button[aria-label=\"Send now\"]");
                                                    Utility.Interrupt();
                                                    await ceo_send_button.ClickAsync();

                                                    Utility.Interrupt();
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{ceo_name} is not receiving messages from someone not in his/her network");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    await ceo_profile_page.CloseAsync();
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }

                            ceo_linkedin = ceo_linkedin!.Split("?miniProfileUrn")[0];
                            RecordLabel.WritingDatabase();
                            JobRecords.SaveRecords(jobs_record_file, json_label, job_id, job_title, job_type, job_location, job_url, company_name, company_website, company_size,
                                recruiter_name!, recruiter_linkedin!, ceo_name!, ceo_linkedin);
                            Utility.Interrupt();
                            JsonToOtherFormats.ConvertToCSV(jobs_record_file, csv_destination, csv_label);
                            RecordLabel.RecordAdded();
                        }
                    }
                }
            }
        }
    }
}
