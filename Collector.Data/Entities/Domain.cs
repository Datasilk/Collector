using System;

namespace Collector.Data.Entities
{
    public class Domain
    {
        public int domainId { get; set; }
        public bool paywall { get; set; }
        public bool free { get; set; }
        public bool https { get; set; }
        public bool www { get; set; }
        public bool empty { get; set; }
        public bool deleted { get; set; }
        public DomainType type { get; set; }
        public DomainType type2 { get; set; }
        public string domain { get; set; }
        public string lang { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTime lastchecked { get; set; }
        public int articles { get; set; }
        public int inqueue { get; set; }
        public bool whitelisted { get; set; }
        public bool blacklisted { get; set; }
    }

    public enum DomainFilterType
    {
        All = 0,
        Whitelisted = 1,
        Blacklisted = 2,
        Unlisted = 3,
        Paywall = 4,
        Free = 5,
        Unprocessed = 6,
        Empty = 7,
        Blacklist_Wildcard = 8,
        NotEmpty = 9
    }

    public enum DomainType
    {
        all = -1,
        unused = 0,
        website = 1,
        ecommerce = 2,
        wiki = 3,
        blog = 4,
        journal= 5,
        SASS = 6,
        social_network = 7,
        advertiser = 8,
        search_engine = 9,
        portfolio = 10,
        news = 11,
        travel = 12,
        aggregator = 13,
        government = 14,
        ebooks = 15,
        crypto = 16,
        law = 17,
        medical = 18,
        political = 19,
        software_development = 20,
        photo_gallery = 21,
        videos = 22,
        music = 23,
        maps = 24,
        mobile_apps = 25,
        video_games = 26,
        erotic = 27,
        conspiracy = 28,
        religion = 29,
        weather = 30,
        comics = 31,
        gore = 32,
        real_estate = 33,
        _3d_animation = 34,
        live_streaming = 35,
        history = 36,
        guns_weapons = 37,
        magazine = 38,
        space = 39,
        directory = 40,
        propaganda = 41,
        freelance = 42,
        job_board = 43,
        events = 44,
        nonprofit = 45,
        forum = 46,
        dark_web = 47,
        torrent = 48,
        education = 49,
        software = 50,
        business = 51,
        podcast = 52,
        isp = 53,
        domain_registrar = 54,
        email = 55,
        cloud_platform = 56,
        storage_service = 57,
        vehicles = 58,
        technology = 59,
        artificial_intelligence = 60,
        energy = 61,
        photography = 62,
        archive = 63,
        art = 64,
        environmental = 65,
        local = 66,
        cultural = 67,
        parked_domain = 68,
        financial = 69,
        tool = 70,
        oprganization = 71,
        stock_market = 72,
        crowd_funding = 73,
        firm = 74,
        web_design = 75,
        graphic_design = 76,
        marketing = 77,
        architecture = 78,
        venture_capital = 79,
        fitness = 80,
        health = 81,
        food = 82,
        recipes = 83,
        library = 84,
        q_and_a = 85,
        corporate = 86,
        internet = 87,
        browser = 88,
        operating_system = 89,
        reviews = 90,
        how_tos = 91,
        institution = 92,
        nature = 93,
        science = 94,
        agency  = 95,
        committee = 96,
        association = 97,
        sports = 98,
        memorials = 99,
        community = 100,
        academy = 101,
        comedy = 102,
        chat = 103,
        restaurant = 104,
        delivery = 105,
        analytics = 106,
        camping_hiking = 107,
        equipment = 108,
        council = 109,
        security = 110,
        parks = 111,
        services = 112,
        archaeology = 113,
        agriculture = 114,
        national = 115,
        international = 116,
        telecommunications = 117,
        union = 118,
        radio = 119,
        club = 120,
        author = 121,
        scam = 122,
        conference = 123,
        vacation = 124,
        rental = 125,
        language = 126,
        B2B = 127,
        stock = 128,
        disibility = 129,
        television = 130,
        gambling = 131,
        nft = 132,
        currency = 133,
        platform = 134,
        research = 135,
        metaverse = 136,
        competition = 137
    }

    public enum DomainSort
    {
        Alphabetical = 0,
        AlphabeticalDescending = 1,
        MostArticles = 2,
        Newest = 3,
        Oldest = 4,
        LastUpdated = 5
    }
}
