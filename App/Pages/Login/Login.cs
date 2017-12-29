﻿namespace Collector.Pages
{
    public class Login: Page
    {
        public Login(Core CollectorCore) : base(CollectorCore)
        {
        }

        public override string Render(string[] path, string body = "", object metadata = null)
        {
            if(S.User.userId > 0)
            {
                //redirect to subjects list
                return base.Render(path, Redirect("/subjects/"));
            }

            //check for database reset
            var scaffold = new Scaffold(S, "/Pages/Login/login.html");

            if(S.Server.environment == Server.enumEnvironment.development && S.Server.hasAdmin == false)
            {
                //load new administrator form
                scaffold = new Scaffold(S, "/Pages/Login/new-admin.html");
                scaffold.Data["title"] = "Create an administrator account";
                scripts += "<script src=\"/js/pages/login/new-admin.js\"></script>";
            }
            else if (S.Server.environment == Server.enumEnvironment.development && S.Server.resetPass == true)
            {
                //load new password form (for admin only)
                scaffold = new Scaffold(S, "/Pages/Login/new-pass.html");
                scaffold.Data["title"] = "Create an administrator password";
                scripts += "<script src=\"/js/pages/login/new-pass.js\"></script>";
            }
            else
            {
                //load login form (default)
                scripts += "<script src=\"/js/pages/login/login.js\"></script>";
            }

            //load login page
            return base.Render(path, scaffold.Render());
        }
    }
}