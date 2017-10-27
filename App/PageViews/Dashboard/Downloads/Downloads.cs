﻿namespace Collector.PageViews
{
    public class Downloads : PageView
    {
        public Downloads(Core CollectorCore, Scaffold ParentScaffold) : base(CollectorCore, ParentScaffold)
        {

        }

        public override string Render()
        {
            Scaffold scaffold = null;

            //setup feeds menu
            string menu = "<div class=\"menu left\"><nav><ul>" +
                "<li><a href=\"javascript:\" id=\"btnaddserver\" class=\"button blue\">Add Server</a></li>" +
                "<li><a href=\"javascript:\" id=\"btndownload\" class=\"button green\">Download Articles</a></li>" +
                "</ul></nav></div>";
            parentScaffold.Data["menu"] = menu;

            if (scaffold == null)
            {
                //get feeds list from web service
                scaffold = new Scaffold(S, "/app/pageviews/dashboard/downloads/downloads.html", "", new string[] { });
                Services.Downloads downloads = new Services.Downloads(S, S.Page.Url.paths);
                scaffold.Data["servers"] = downloads.LoadServersUI();
                scaffold.Data["host"] = S.Request.Host.ToString();
                scaffold.Data["queue"] = downloads.LoadQueueUI();
                S.Page.RegisterJS("downloads", "setTimeout(function(){S.downloader.consoleLog('Collector (v" + S.Version + ") Download Queue Console: >', 'startup');},100);");
                S.Page.RegisterJSFromFile("/app/pageviews/dashboard/downloads/downloads.js");
            }
            return scaffold.Render();
        }

    }
}