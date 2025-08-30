using Collector.Common;
using Collector.Common.Models.Articles;
using System.Text;
using System.Text.Json;

namespace Collector.CyberScout
{
    public static class Domain
    {
        public static void Analyze(AnalyzedArticle article, int domainId)
        {
            var systemPrompt = new StringBuilder(@"Generate meta data about a website based on the text provided by the user found on the web page.

#Title#
Extract the title from the user provided text, or generate a website title in the following format: ""Website Name - What it's used for""

#Description#
Write a short description, one paragraph in size that describes the purpose of the website.

#Domain Types#
Choose two types to assign to the domain. Make sure that when the types are combined, they make a coherent phrase that describes what kind of website 
we are analyzing. For example, ""Type"":[""crowd_funding"", ""platform""] would be interpreted as a crowd funding platform. Below is the list of available types to choose from:

internet, browser, operating_system, isp, domain_registrar, email, cloud_platform, storage_service, platform, telecommunications, blog, journal, wiki, news, magazine, ebooks, videos, music, photo_gallery, photography, live_streaming, podcast, radio, television, comics, _3d_animation, social_network, forum, chat, community, q_and_a, reviews, how_tos, ecommerce, business, corporate, firm, B2B, financial, stock_market, stock, crowd_funding, venture_capital, marketing, advertiser, services, freelance, job_board, portfolio, agency, technology, software, software_development, artificial_intelligence, metaverse, SASS, mobile_apps, video_games, tool, analytics, web_design, graphic_design, architecture, art, education, academy, library, research, science, archaeology, government, political, law, oprganization, institution, committee, association, council, national, international, union, medical, health, fitness, disibility, food, recipes, restaurant, delivery, comedy, events, gambling, competition, camping_hiking, sports, team, league, outdoor, adventure, water_sports, winter_sports, extreme_sports, martial_arts, cycling, running, golf, tennis, swimming, travel, vacation, rental, maps, local, parks, vehicles, bitcoin, blockchain, crypto, nft, currency, real_estate, cultural, history, religion, language, memorials, nature, environmental, agriculture, energy, weather, erotic, conspiracy, propaganda, gore, guns_weapons, dark_web, torrent, scam, aggregator, directory, search_engine, archive, parked_domain, equipment, security, club, author, conference, space, nonprofit, quantum, biotech, robotics, virtual_reality, augmented, iot, semiconductor, firmware, hardware, nanotechnology, wearable, drone, printing, smart, autonomous, satellite, protocol, embedded, telecom, network, consulting, legal, accounting, recruitment, engineering, certification, training, laboratory, scientific, academic, university, college, school, course, tutorial, scholarship, textbook, curriculum, manufacturing, industrial, automation, logistics, supply, construction, mining, chemical, pharmaceutical, military, defense, intelligence, aerospace, veterans, tactical, hacking, cybersecurity, darknet, anonymity, cryptography, vulnerability, exploit, movie, studio, celebrity, fan, anime, manga, cosplay, convention, theme, amusement, theater, performing, concert, festival, streaming, digital, influencer, startup, incubator, accelerator, banking, investment, insurance, retirement, credit, loan, mortgage, tax, trading, forex, commodities, bonds, fund, heritage, indigenous, folklore, tradition, translation, regional, city, tourism, embassy, consulate, diplomatic, immigration, visa, expatriate, hospital, clinic, telemedicine, pharmacy, nutrition, diet, alternative, holistic, yoga, therapy, psychology, psychiatry, dental, vision, hearing, mental, addiction, recovery, meditation, spiritual, collectible, antique, auction, handmade, craft, diy, gardening, improvement, interior, fashion, luxury, vintage, sustainable, eco, organic, vegan, pet, animal, wildlife, conservation, climate, genealogy, mythology, astrology, automotive, manufacturer, dealership, auto_parts, motorcycle, aviation, railway, transit, shipping, trucking, rideshare, electric, boat, yacht, cruise_ship, political_party, political_campaign, election, legislation, government_policy, regulatory, municipal, provincial, federal, judicial, legislative, executive, mission, government_service, government_census, gallery, museum, exhibition, sculpture, painting, illustration, typography, design, furniture, jewelry, ceramic, print, street, art_history, physics, chemistry, biology, astronomy, geology, meteorology, oceanography, neuroscience, genetics, paleontology, anthropology, sociology, economics, linguistics, adult, dating, escort, fetish, classified, marketplace, comparison, calculator, converter, reference, almanac, database, humanitarian, relief, charity, volunteer, foundation, donation, advocacy, rights, activism, publication, newspaper, broadcast, documentary, journalism, editorial, press, media, hobby, leisure, seasonal, specialty, niche, gaming, esports, mmorpg, fps, strategy, simulation, puzzle, mobile_gaming, console, game_development, game_forum, game_marketplace, parenting, pregnancy, baby, toddler, children, teen, adoption, childcare, family_planning, career, resume, interview, remote_work, gig_economy, workplace, salary, benefits, professional_development, attorney, litigation, patent, trademark, copyright, compliance, legal_practice, privacy, data_protection, self_improvement, productivity, motivation, leadership, coaching, mentoring, time_management, goal_setting, mindfulness, wedding, industry_conference, seminar, workshop, expo, trade_show, meetup, networking, virtual_event, home_decor, home_furniture, appliance, smart_home, home_security, cleaning, organization, renovation, home_gardening, landscaping, emergency, disaster, preparedness, first_aid, safety, rescue, crisis, alert, evacuation, holiday, christmas, halloween, thanksgiving, easter, valentines, new_year, seasonal_event, holiday_festival, celebration

#Services#
Extract a list of all services that the website and/or company provides, e.g. [""Domain Registration"", ""Website Hosting"", ""DNS Hosting""]

#Company#
Find the company name that owns the website. You can usually find the company name in the copyright notice.

#PayWall#
Determine whether or not the user is forced to sign up and pay for services in order to use the website.

#Free#
Determine whether or not there is a free option to sign up for.

#Output#
Only output a JSON object and nothing before or after the JSON object. Use the following template for generating the output:
{
    ""Title"": """",
    ""Description"":"",
    ""Type"":["", ""],
    ""Services"":[""],
    ""Company"":"",
    ""PayWall"":false,
    ""Free"":true
}
");

            var userPrompt = new StringBuilder();
            if (!string.IsNullOrEmpty(article.title))
            {
                userPrompt.AppendLine("Website Title = " + article.title);
            }
            userPrompt.AppendLine("Raw Text = " + article.rawText);
            try
            {
                var result = LLMs.Prompt(systemPrompt.ToString(), "", "").Result;
                var metadata = JsonSerializer.Deserialize<Models.DomainInfo>(result);
                if(metadata != null)
                {
                    if(metadata.Type != null)
                    {
                        try
                        {
                            if (metadata.Type.Count == 1)
                            {
                                App.DomainsRepository.UpdateDomainType(domainId, Enum.Parse<Data.Enums.DomainType>(metadata.Type[0]));
                            }
                            else if (metadata.Type.Count >= 2)
                            {
                                App.DomainsRepository.UpdateDomainTypes(domainId,
                                    Enum.Parse<Data.Enums.DomainType>(metadata.Type[0]),
                                    Enum.Parse<Data.Enums.DomainType>(metadata.Type[1]));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Adding Domain Types: " + ex.Message);
                        }
                    }
                    if (metadata.Services != null && metadata.Services.Count > 0)
                    {
                        try
                        {
                            var serviceIds = App.DomainsRepository.GetServiceIdsByNames([.. metadata.Services]);
                            if (serviceIds != null && serviceIds.Count > 0)
                            {
                                App.DomainsRepository.AddDomainServices(domainId, [.. serviceIds.Values]);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error Adding Domain Services: " + ex.Message);
                        }
                    }
                    if (!string.IsNullOrEmpty(metadata.Company))
                    {
                        App.DomainsRepository.UpdateCompany(domainId, metadata.Company);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("LLM Error: " + ex.Message);
            }
            

        }
    }
}