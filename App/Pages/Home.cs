﻿using Newtonsoft.Json;

namespace Collector.Pages
{
    public class Home : Page
    {
        public Home() : base()
        {
        }

        public override string Render()
        {
            //setup scaffolding variables
            Scaffold scaffold = new Scaffold(S, "/app/pages/home.html", "", new string[] { });

            //load website interface
            PageViews.Interface iface = new PageViews.Interface(S, scaffold);

            return iface.Render(scaffold.Render(), "home.css");
        }
    }
}
