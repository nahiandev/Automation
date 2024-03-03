using Automation.Helpers;
using Microsoft.Playwright;

namespace Automation.TraceBack
{
    internal class TraceConnections
    {
        private static async Task FollowPeople(IBrowserContext context, string log_file, string message)
        {
            IDictionary<string, bool> pairs = JsonToOtherFormats.LogToUrlDictionary(log_file);
            if(pairs.Count > 0)
            {
                IPage follow_up_page = await context.NewPageAsync();
                foreach (var url_pair in pairs)
                {
                    if (!url_pair.Value)
                    {
                        try
                        {
                            string url = url_pair.Key;
                            await follow_up_page.GotoAsync(url);
                            await follow_up_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                            Utility.Interrupt();

                            ILocator distance_badge = follow_up_page.Locator(".distance-badge").Locator(".dist-value").First;
                            string distance = await distance_badge.InnerTextAsync();

                            if (distance is "1st")
                            {
                                ILocator nameholder = follow_up_page.Locator(".text-heading-xlarge");
                                string? name = await nameholder.TextContentAsync();
                                ILocator message_button = follow_up_page.Locator(".pvs-profile-actions").Last.Locator(".entry-point").Locator("a:has-text(\"Message\")");
                                await message_button.ClickAsync();
                                await follow_up_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                                Utility.Interrupt();

                                ILocator content_box = follow_up_page.Locator(".msg-form__msg-content-container--scrollable").Last;

                                while (!await content_box.IsVisibleAsync())
                                {
                                    await follow_up_page.ReloadAsync();
                                    await follow_up_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                    await follow_up_page.WaitForSelectorAsync(".msg-form__contenteditable");

                                    if (await content_box.IsVisibleAsync()) break;
                                }

                                if (await content_box.IsVisibleAsync())
                                {
                                    try
                                    {
                                        ILocator message_box = content_box.Locator(".flex-grow-1").First.Locator(".msg-form__contenteditable").First
                                        .Locator("p").Last;
                                        string follow_up_message = Utility.CustomizesMessage(name!, message);
                                        
                                        await message_box.FillAsync(follow_up_message);
                                        await message_box.ClickAsync();
                                        await follow_up_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                                        ILocator buttons = follow_up_page.Locator(".msg-form__right-actions").Locator("div");

                                        int length = await buttons.CountAsync();

                                        for (int i = 0; i < length; i++)
                                        {
                                            ILocator container = buttons.Nth(i);
                                            string html = await container.InnerHTMLAsync();

                                            if (!html.Contains("disabled"))
                                            {
                                                ILocator send_button = container.Locator("button[type=\"submit\"]");
                                                await send_button.ClickAsync();
                                                break;
                                            }
                                        }

                                        Utility.Interrupt();
                                        ILocator dismiss = follow_up_page.Locator(".msg-overlay-bubble-header__controls").Last.Locator("button").Last;
                                        await dismiss.ClickAsync();
                                        Utility.Interrupt();
                                        JsonToOtherFormats.ToggleFlag(log_file, url, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await follow_up_page.CloseAsync();
                            Console.WriteLine(ex.Message);
                        }
                    }
                }

                await follow_up_page.CloseAsync();
                Console.WriteLine("\nAll recent 1st degree connections caught up for now :)");
            }
        }

        public static async Task FollowerTask(IBrowserContext context, string follow_up_text)
        {
            string log_file = FileGenerator.CombineFilePath("bin", "TraceModel", "urls.json");
            await FollowPeople(context, log_file, follow_up_text);
        }
    }
}
