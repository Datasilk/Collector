namespace Collector.Common.Models.Articles
{

    public static class Rules
    {
        //-------------------------------------------------------------
        //rule name prefix/suffix meanings:
        //good = used to score DOM elements positively
        //bad = used to score DOM elements negetively
        //reallybad = used to score DOM elements extremely negetive
        //ignore = used to exclude certain keywords from scoring
        //-------------------------------------------------------------

        //various separators used when splitting a string into an array
        public static string[] wordSeparators = new string[] { "(", ")", ".", ",", "?", "/", "\\", "|", "!", "\"", "'", ";", ":", "[", "]", "{", "}", "”", "“", "_", "~", "…", "&" };
        public static string[] sentenceSeparators = new string[] { "(", ")", ".", ",", "?", "/", "\\", "|", "!", "\"", ";", ":", "[", "]", "{", "}", "”", "“", "_", "~", "…", "&" };
        public static string[] separatorExceptions = new string[] { "'", "\"" };
        public static string[] scriptSeparators = new string[] { "{", "}", ";", "$", "=", "(", ")" };
        public static string[] ofPhraseStartSeparators = new string[] { "the", "heard", "wary", "course", "in", "and", "at", "is", "has" };

        //used to identify words that may be a date
        public static string[] dateTriggers = new string[] {
            "published","written","posted",
            "january","february","march","april","may","june", "july","august",
            "september","october","november","december"
        };

        //used to find URLs that may be used for advertisements or UI buttons
        public static string[] badUrls = new string[] {
            "/ads/", "/ad/", "/advert/", "javascript:",
            "login", "/submit", "/job", "advertis", "/privacy/", "privacy-policy", "privacypolicy",
            "/signup", "/signin", "/sign-up", "/sign-in", "/career", "/donate", "/newsletter", "/legal",
            "doubleclick", "/subscribe", "/cookies/", ".ashx"
        };

        public static string[] badUrlExtensions = new string[]
        {
            "jpg", "jpeg", "png", "gif", "aiff", // image file extensions
            "zip", "z7", "rar", "tar", "arc", "cab", "tgz", //compressed file extensions
            //"pdf", "doc", "docx", "rtf", "txt", "odt" //document file extensions
            "xml", "json", "dat", "sql", "js", "css", "less", "scss", //dev file extensions
            "exe", "so", "dll", "bat" //system file extensions
        };

        public static string[] documentExtensions = new string[]
        {
            "pdf", "doc", "docx", "rtf", "txt", "odt", "csv", "ppt", "pptx", "xls", "xlsx"
        };

        //used to identify page titles that disqualifies the page as an article
        public static string[] badPageTitles = new string[]{
            "terms of use", "terms & conditions", "terms and conditions", "our policies",
            "privacy policy", "site map", "sitemap", "sign in", "sign up", "signin", "signup",
            "subscribe", "cookie statement", "cookie policy", "403 forbidden", "404", "page not found",
            "access denied"
        };

        public static string[] badHomePageTitles = new string[]{
            "403 ", "404 ", "page not found", "access denied", "forbidden", "404 not found",
            "domain expired", "domain is expired", "domain parking", "domain is parked"
        };

        public static string[] badHomePageTitlesStartWith = new string[]{
            "error", "new tab", "bad request", "iis windows server", "Welcome to nginx", "optimum safe browsing"
        };

        //used to identify various DOM elements that are used as titles
        public static string[] headerTags = new string[] { "h1", "h2", "h3", "h4", "h5", "h6" };

        //used to identify header tags that may be part of the UI instead of the article
        public static string[] badHeaderWords = new string[] {
            "comments", "related articles", "related posts"
        };

        //used to identify DOM elements that should be skipped entirely when analyzing the document
        public static string[] skipTags = new string[] {
            "html", "body", "meta", "title", "link", "script", "style"
        };

        //used to identify DOM elements that should not be included in the article
        public static string[] badTags = new string[]  {
            "head", "meta", "link", "applet", "area", "style",
            "audio", "canvas", "dialog", "embed", "iframe",
            "input", "label", "nav", "object", "option", "s", "script",
            "textarea", "video", "noscript", "footer", "aside"
            //, "small"
        };

        //used for complex bad classname matching
        public class BadClassOptions
        {
            public string Name { get; set; }
            /// <summary>
            /// if any exceptions are found in the element's className attribute, 
            /// Collector will ignore this particular bad class for the specific element
            /// </summary>
            public string[] Exceptions { get; set; }
        }

        //used to determine if a DOM element is used for advertisements or UI
        public static string[] badClasses = new string[] {
            "social", "advert", "keyword", "twitter", "replies", "reply",
            "trending", "logo", "disqus", "popular", "contacts",
            "bread", "callout", "masthead", "addthis", "-ad-", "ad-cont", "tags",
            "subscri", "button", "reddit", "login", "signup", "promo", "sponsor",
            "signin", "recommend", "promot", "share", "sharing", "facebook",
            "poweredby", "powered-by", "invisible", "newsletter", "related",
            "navi", "toolbar", "sidecontent", "tab",
            "carousel", "overlay", "progress", "comment",
            "guestbook", "free-trial", "rating", "message", "divid", "article-collection",
            "privacy", "captcha", "badge", "comment", "cancel",
            "apply", "dropdown", "drop-down", "truncated",
            "editors-picks", "categories", "notifi", "timer", "next", "previous",
            "signout", "sign-out", "player", "picker", "stories"
        };

        //used to protect DOM elements that may be a part of the article
        public static string[] ignoreClasses = new string[]
        {
            "table"
        };

        //used to find anchor links with flagged words they may be UI links
        public static string[] badLinkWords = new string[] {
            "more", "about", "back", "previous", "next", "link", "follow"
        };

        //used to find vulgure language
        public static string[] badWords = new string[] {
            "shit", "crap", "asshole", "shitty", "bitch", "slut", "whore",
            "fuck", "fucking", "fucker", "fucked", "fuckers", "fucks"
        };

        //used to determine if a string should be flagged
        public static string[] badKeywords = new string[] {
            "disqus", "advertisement", "follow on twitter", "check out our", "announcement",
            "users", "sign up", "log in", "sign in", "reset pass", "subscribe", "learn more",
            "more stories", "click for", "update required", "update your browser", "supports html5",
            "support html5", "member", "this site", "exclusive", "podcast", "newsletter",
            "tick the box", "recent stories", "recent articles", "recent posts", "viewers", "followers", "skip",
            "related content", "sponsored", "links"
        };

        //used to find 2 or more words in a sentence that suggests the sentence is used after the end of an article
        public static string[] badTrailing = new string[] {
            "additional", "resources", "notes", "comment", "discuss", "post", "links",
            "share", "article"
        };

        //used to determine if parent DOM element should be flagged for contamination
        public static string[] badKeywordsForParentElement = new string[]
        {
            "further discussion"
        };

        //used to determine if a single word should be flagged
        public static string[] badSingleWords = new string[] {
            "comments", "about", "articles", "members", "membership", "login", "log in", "signup", "sign up",
            "signin", "sign in", "topics", "subscribe"
        };


        //used to determine if a small group of words ( < 5) is actually a menu item
        public static string[] badMenu = new string[] {
            "previous", "next", "post", "posts", "entry", "entries", "article", "articles",
            "more", "back", "view", "about", "home", "blog", "rules", "resources", "skip",
            "create", "account", "signin", "sign in", "login", "log in", "signup", "sign up", "content",
            "jump", "contents", "comment", "comments", "prev", "members", "articles", "membership",
            "topics", "subscribe"
        };

        //used to check DOM hierarchy tag names of potential menu items
        public static string[] menuTags = new string[]
        {
            "ul", "li"
        };

        //used to check DOM hierarchy tag names of potential text blocks
        public static string[] textTags = new string[]
        {
            "span", "p", "h1", "h2", "h3", "h4", "h5", "h6", "em"
        };

        //used to determine if a sentence (or group of sentences) is part of a legal document
        public static string[] badLegal = new string[]
        {
            "legal", "illegal", "must directly", "distribute", "official", "promote", "piracy", "must include",
            "must be accompanied", "appropriate", "do not use", "promotion", "privacy", "policy", "agreement",
            "provisions", "prohibited", "please report", "you agree to", "accept all", "liability", "accept the",
            "cookies", "your device", "usage", "marketing", "analyze"
        };

        //check DOM element role attribute for bad role names
        public static string[] badRoles = new string[] { "application" };

        //used to check for malicious characters within a string
        public static string[] badChars = new string[] { "|", ":", "{", "}", "[", "]" };

        //domain suffixes
        public static string[] domainSuffixes = new string[] { "com", "net", "org", "biz" };

        //used to remove common query key/value pairs from URLs
        public static string[] commonQueryKeys = new string[] { "ie", "utm_source", "utm_medium", "utm_campaign" };

        //used to verify if a string is in fact HTML
        public static string[] HtmlVerify = new string[] { "<div", "<html", "<a ", "<img ", "<p>" };

        //used to determine if a line break should be added after a block element
        public static string[] blockElements = new string[] {
            "address", "article", "aside", "blockquote", "dd", "div", "dl", "dt",
            "fieldset", "figcaption", "figure", "footer", "form", "h1", "h2", "h3",
            "h4", "h5", "h6", "header", "hr", "li", "main", "nav", "noscript", "ol",
            "output", "p", "pre", "section", "table", "tfoot", "ul" };

        //used to detect poorly written text
        public static string[] badLanguageDetectChars = new string[]
        {
            "@", "#", "%", "^", "*", "(", ")", "_", "{", "}", "[", "]", "/", "\\", "|", "<", ">", ":", ";"
        };

        //common words stored in the database
        public static string[] commonWords;
    }
}
