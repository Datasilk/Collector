using System.Net;
using Collector.Common.Extensions.Strings;
using Collector.Common.Extensions;

namespace Collector.Common
{
    public static class Syndication
    {
        //used to download & parse RSS & Atom feeds
        public struct SyndicatedFeed
        {
            public string title;
            public string description;
            public string link;
            public string copyright;
            public string language;
            public string feedUrl;
            public SyndicatedItemImage image;
            public List<SyndicatedItem> items;
        }

        public struct SyndicatedItem
        {
            public int index;
            public string link;
            public string title;
            public string description;
            public string publisher;
            public DateTime publishDate;
            public List<string> images;
        }

        public struct SyndicatedItemImage
        {
            public string title;
            public string url;
            public string link;
            public int width;
            public int height;
        }

        public static SyndicatedFeed Read(string html)
        {
            var feed = new SyndicatedFeed();
            var items = new List<SyndicatedItem>();
            //use DOM parser instead of System.Xml
            var dom = new DOM.Parser(html);
            var elements = dom.Find("//channel");
            List<DOM.DomElement> elems;
            //get channel info
            if (elements.Count > 0)
            {
                var channel = elements[0];

                //get channel title
                elems = channel.Find("/title");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            feed.title = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }
                    }
                }

                //get channel description
                elems = channel.Find("/description");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            feed.description = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }
                    }
                }

                //get channel link
                elems = channel.Find("/link");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            feed.link = WebUtility.HtmlDecode(elems[0].FirstChild.text);
                        }
                        catch (Exception) { }
                    }
                }

                //get channel language
                elems = channel.Find("/language");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            feed.language = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }
                    }
                }

                //get channel copyright
                elems = channel.Find("/copyright");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            feed.copyright = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }
                    }
                }

                //get channel image properties
                elems = channel.Find("/image");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        var img = elems[0];
                        var image = new SyndicatedItemImage();

                        //get channel image title
                        elems = img.Find("/title");
                        if (elems.Count > 0)
                        {
                            if (elems[0].FirstChild != null)
                            {
                                try
                                {
                                    image.title = elems[0].FirstChild.text;
                                }
                                catch (Exception) { }
                            }
                        }

                        //get channel image url
                        elems = img.Find("/url");
                        if (elems.Count > 0)
                        {
                            if (elems[0].FirstChild != null)
                            {
                                try
                                {
                                    image.url = WebUtility.HtmlDecode(elems[0].FirstChild.text);
                                }
                                catch (Exception) { }
                            }
                        }

                        //get channel image link
                        elems = img.Find("/link");
                        if (elems.Count > 0)
                        {
                            if (elems[0].FirstChild != null)
                            {
                                try
                                {
                                    image.link = WebUtility.HtmlDecode(elems[0].FirstChild.text);
                                }
                                catch (Exception) { }
                            }
                        }

                        //get channel image width
                        elems = img.Find("/width");
                        if (elems.Count > 0)
                        {
                            if (elems[0].FirstChild != null)
                            {
                                if (elems[0].FirstChild.text.IsNumeric())
                                {
                                    try
                                    {
                                        image.width = int.Parse(elems[0].FirstChild.text);
                                    }
                                    catch (Exception) { }
                                }
                            }
                        }

                        //get channel image height
                        elems = img.Find("/height");
                        if (elems.Count > 0)
                        {
                            if (elems[0].FirstChild != null)
                            {
                                if (elems[0].FirstChild.text.IsNumeric())
                                {
                                    try
                                    {
                                        image.height = int.Parse(elems[0].FirstChild.text);
                                    }
                                    catch (Exception) { }
                                }
                            }
                        }
                        feed.image = image;
                    }
                }

                //get channel feed URL
                elems = channel.Find("/atom:link");
                if (elems.Count > 0)
                {
                    if (Objects.IsEmpty(elems[0].attribute["href"]) == false)
                    {
                        try
                        {
                            feed.feedUrl = WebUtility.HtmlDecode(elems[0].attribute["href"]);
                        }
                        catch (Exception) { }
                    }
                }
            }
            elements = dom.Find("//item");
            foreach (var element in elements)
            {
                var item = new SyndicatedItem();

                //get title
                elems = element.Find("/title");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            item.title = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }
                    }
                }

                //get url link
                elems = element.Find("/link");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            item.link = WebUtility.HtmlDecode(elems[0].FirstChild.text);
                        }
                        catch (Exception) { }
                    }
                }

                //get url link from atom:link
                elems = element.Find("/atom:link");
                if (elems.Count > 0)
                {
                    if (Objects.IsEmpty(elems[0].attribute["href"]) == false)
                    {
                        try
                        {
                            item.link = WebUtility.HtmlDecode(elems[0].attribute["href"]);
                        }
                        catch (Exception) { }
                    }
                }

                //get description
                elems = element.Find("/description");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            item.description = elems[0].FirstChild.text;
                        }
                        catch (Exception) { }

                    }
                }

                //get publish date
                elems = element.Find("/pubDate");
                if (elems.Count > 0)
                {
                    if (elems[0].FirstChild != null)
                    {
                        try
                        {
                            item.publishDate = DateTime.Parse(elems[0].FirstChild.text);
                        }
                        catch (Exception) { }
                    }
                }
                items.Add(item);

            }
            feed.items = items;
            return feed;
        }
    }
}
