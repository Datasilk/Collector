using System.Text;
using System.Text.Json;
using Collector.Data.Enums;
using Collector.Common;
using Collector.Common.Extensions.Strings;
using Collector.Common.Models.Articles;
using System.CodeDom.Compiler;

namespace Collector.CyberScout
{
    public static class Domain
    {
        private static string DomainTypes { get; set; } = "";
        private static List<string> UnwantedDomainTypes = ["all", "unused", "website"];

        public static void Analyze(AnalyzedArticle article, int domainId)
        {
            if(DomainTypes == "") DomainTypes = string.Join(", ", 
                Enum.GetValues(typeof(DomainType)).Cast<DomainType>()
                .Select(t => t.ToString())
                .Where(a => !UnwantedDomainTypes.Contains(a)).ToList());

            var systemPrompt = new StringBuilder(@$"Generate meta data about a website based on the text provided by the user found on the web page.

#Title#
Extract the title from the user provided text, or generate a website title in the following format: ""Website Name - What it's used for""

#Description#
Write a short description, one paragraph in size that describes the purpose of the website.

#Domain Types#
* Choose the two most relevant types to assign to the domain, and don't use two types that are similar to one another. 
* Each type you choose must be from two separate categories. 
* Make sure that when choosing two types, sort them in order so they make a coherent phrase that describes what kind of website we are analyzing. 
* Only use the list of available types provided below to assign to the domain:

{DomainTypes}

#Services#
Extract a list of all available services that the website and/or company provides, e.g. [""Domain Registration"", ""Website Hosting"", ""DNS Hosting"", ""Web Development"", ""Logo Design""]
* Services must be universal, cannot use company-specific naming, and cannot be too specific. For example, instead of ""Vaccuming"" or ""Washing Dishes"", use ""House Cleaning"" instead
* Don't include website account services (signup, login, account recovery, etc)
* Don't put text inside of parenthesis
* You can include Email Newsletter as a service if the website provides it

#Company#
Find the company name that owns the website. You can usually find the company name in the copyright notice.

#Language#
Determine the language that the website uses. Use ISO 639-1 for the language, such as ""en"" or ""de-at"" for example.

#PayWall#
Determine whether or not the user is forced to sign up and pay for services in order to use the website.

#Free#
Determine whether or not there is a free option to sign up for.

#Output#
* Only output a JSON object and nothing before or after the JSON object. 
* Make sure all output is in English, even if the user input is in another language. 
* If the website is a parked domain or unused, don't bother generating any meta data except for one domain type ""unused""
* Use the following template for generating the output:
{{
    ""Title"": """",
    ""Description"":"""",
    ""Type"":["""", """"],
    ""Services"":[""""],
    ""Company"":"""",
    ""Language"":"""",
    ""PayWall"":false,
    ""Free"":true
}}");
            var assistantPrompt = "";

            var userPrompt = new StringBuilder("Domain = https://" + article.domain);
            userPrompt.AppendLine("");
            //site title
            if (!string.IsNullOrEmpty(article.title))
            {
                userPrompt.AppendLine("Website Title = " + article.title);
                userPrompt.AppendLine("");
            }
            //site base paths
            if (article.urlLinks != null && article.urlLinks.Count > 0)
            {
                userPrompt.AppendLine("Site base paths found on home page = " + string.Join(", ", article.urlLinks.Select(a =>
                {
                    var paths = a.Split(a.GetDomainName(), StringSplitOptions.RemoveEmptyEntries);
                    if (paths.Length > 1 && paths[1].Length > 0)
                    {
                        var parts = paths[1].Split("/", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0) return "/" + parts[0];
                    }
                    return "";
                }).Where(a => a.Length > 0).Distinct().ToList()));
                userPrompt.AppendLine("");
            }
            //raw text on home page
            userPrompt.AppendLine("Raw Text = " + (article.rawText.Length > 5000 ? article.rawText.Substring(0, 5000) : article.rawText));
            userPrompt.AppendLine("");

            try
            {
                var result = LLMs.Prompt(systemPrompt.ToString(), assistantPrompt, userPrompt.ToString()).Result;
                var metadata = JsonSerializer.Deserialize<Models.DomainInfo>(result);
                var logLanguages = "";
                var logCompany = "";
                var logDomainTypes = "";
                var logServices = "";
                var logParts = new StringBuilder();
                var unused = false;
                if (metadata != null)
                {
                    if (!string.IsNullOrEmpty(metadata.Language))
                    {
                        var langCode = metadata.Language;
                        var codeParts = langCode.Split("-");
                        if(codeParts.Length > 1)
                        {
                            langCode = codeParts[0] + "-" + codeParts[1].ToUpper();
                        }
                        var lang = langCode;
                        if (Common.Models.LanguageCodes.Codes.ContainsKey(langCode))
                        {
                            lang = Common.Models.LanguageCodes.Codes[langCode];
                        }
                        App.DomainsRepository.UpdateLanguage(domainId, langCode);
                        logLanguages = lang;
                    }
                    if (metadata.Type != null)
                    {
                        try
                        {
                            if (metadata.Type.Count == 1)
                            {
                                App.DomainsRepository.UpdateDomainType(domainId, Enum.Parse<DomainType>(metadata.Type[0]));
                                if (metadata.Type[0] == "unused") unused = true;
                            }
                            else if (metadata.Type.Count >= 2)
                            {
                                App.DomainsRepository.UpdateDomainTypes(domainId,
                                    Enum.Parse<DomainType>(metadata.Type[0]),
                                    Enum.Parse<DomainType>(metadata.Type[1]));
                            }
                            logDomainTypes = string.Join(" & ", metadata.Type);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error adding Domain types: " + ex.Message);
                        }
                    }
                    if (!string.IsNullOrEmpty(metadata.Company))
                    {
                        try
                        {
                            App.DomainsRepository.UpdateCompany(domainId, metadata.Company);
                            logCompany = metadata.Company;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error updating Domain company: " + ex.Message);
                        }
                    }
                    if (metadata.Services != null && metadata.Services.Count > 0)
                    {
                        try
                        {
                            var services = metadata.Services.Select(a => a.Capitalize());
                            var serviceIds = App.DomainsRepository.GetServiceIdsByNames([..services]);
                            if (serviceIds != null && serviceIds.Count > 0)
                            {
                                App.DomainsRepository.AddDomainServices(domainId, [.. serviceIds.Values]);
                                logServices = string.Join(", ", services);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error adding Domain services: " + ex.Message);
                        }
                    }

                    logParts.AppendLine("");
                    logParts.AppendLine($"----- {article.url.Split("//")[0] + "//" + article.domain} --------------------------------------------------------------");
                    logParts.AppendLine(metadata.Title);
                    logParts.AppendLine(metadata.Description);
                    logParts.AppendLine("Company: " + logCompany +
                        (!string.IsNullOrEmpty(logLanguages) ? " | Language: " + logLanguages : ""));
                    logParts.AppendLine("Type: " + logDomainTypes +
                        (!string.IsNullOrEmpty(logServices) ? " | Services: " + logServices : ""));
                    logParts.AppendLine("---------------------------------------------------------------------------------");
                    logParts.AppendLine("");
                }
                else
                {
                    logParts.Append("metadata is null!");
                }
                Console.WriteLine("AI results: " + string.Join(", ", logParts));
            }
            catch (Exception ex)
            {
                Console.WriteLine("LLM Error: " + ex.Message);
            }


        }
    }
}