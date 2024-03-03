using Automation.Helpers;
using Automation.RecordHandlers;
using Automation.TraceBack;
using Microsoft.Playwright;

namespace Automation.Parser
{
	internal class PostParser : IParser
	{
		private readonly string AuthFile;
		private readonly List<string> SearchParam;
		private readonly string Message;
		private readonly string FollowUpText;
		private readonly int TimeOut;
		private readonly bool HeadLess;
		private readonly int IterationLimit;
		public PostParser(string auth_file, List<string> search_param, string message, string followup_text, int timeout, bool head_less, int iteration_limit)
		{
			AuthFile = auth_file;
			SearchParam = search_param;
			Message = message;
			FollowUpText = followup_text;
			TimeOut = Utility.MinToMiliSec(timeout);
			HeadLess = head_less;
			IterationLimit = iteration_limit;
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

			string post_record_file = FileGenerator.CreateFileAndCombinePath("bin", "Models", "posts.json");
			string csv_destination = FileGenerator.CreateFileAndCombinePath("src", "Records", "posts.csv");

			if (SearchParam.Count > 0)
			{ 
				foreach (string query in SearchParam)
				{
					string keyword = Utility.EncodeKeyword(query.Trim());
					string url = "https://linkedin.com";
					await page.GotoAsync($"{url}/search/results/content/?datePosted=%22past-24h%22&keywords={keyword}");
					await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
					Utility.Interrupt();

					Console.Write("Fetching posts ");
					for (int i = 0; i < IterationLimit; i++)
					{
						try
						{
							await page.EvaluateAsync("window.scrollTo(0,document.body.scrollHeight)");
							await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
							Console.Write('.');
							Utility.Interrupt();
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					}

					Console.WriteLine();

					ILocator divs = page.Locator(".feed-shared-update-v2");

					List<ILocator> post_divs = [];
					for (int x = 0; x < await divs.CountAsync(); x++)
					{
						post_divs.Add(divs.Nth(x));
					}

					foreach (ILocator post in post_divs)
					{
						ILocator url_holder = post.Locator(".update-components-actor").Locator(".update-components-actor__container-link").First;
						await page.WaitForSelectorAsync(".update-components-actor");

						IPage profile_page = await context.NewPageAsync();
						string? posters_profile_url = await url_holder.GetAttributeAsync("href");
						posters_profile_url = posters_profile_url!.Split('?')[0].TrimEnd('/');
						if (posters_profile_url!.Contains("/in/"))
						{
							try
							{
								await profile_page.GotoAsync(posters_profile_url!);
								await profile_page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
								Utility.Interrupt();

								ILocator nameholder = profile_page.Locator(".text-heading-xlarge");

								string? name = await nameholder.TextContentAsync();
								bool message_flag = false;

								ILocator distance_badge = profile_page.Locator(".distance-badge").Locator(".dist-value").First;
								string distance = await distance_badge.InnerTextAsync();

								if (distance != "1st")
								{
									ILocator connect_button = profile_page.Locator(".pvs-profile-actions").Last.Locator(@"div[class='pvs-profile-actions__action']").Locator("button").Locator("span:has-text(\"Connect\")");
									ILocator more_button = profile_page.Locator(".pvs-profile-actions").Last.Locator(".artdeco-dropdown").Locator(@"button:has-text('More')");

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
								string _usermame = posters_profile_url.Split("/in/")[1].Trim('/');

								await profile_page.Locator("[aria-label=\"Dismiss\"]").First.ClickAsync();
								RecordLabel.WritingDatabase();
								PostRecords.SaveRecords(post_record_file, json_label,
									_usermame, query, name!, designation!, company!, website!
									, phone!, email!, posters_profile_url, twitter_handle!, message_flag);

								Utility.Interrupt();
								JsonToOtherFormats.ConvertToCSV(post_record_file, csv_destination, csv_label);
								RecordLabel.RecordAdded();

								await profile_page.CloseAsync();
							}
							catch (Exception ex)
							{
								await profile_page.CloseAsync();
								Console.WriteLine(ex.Message);
							}
						}
						else
						{
							await profile_page.CloseAsync();
						}
					}
				}
			}
		}
	}
}
