using Collector.Common.DOM;
using Collector.Common.Extensions.Strings;

namespace Collector.Common.Models.Articles
{
    public class AnalyzedArticle
    {
        public int id { get; set; }
        public int feedId { get; set; }
        public int fileSize { get; set; }
        public int relevance { get; set; }
        public int importance { get; set; }
        public int totalWords { get; set; }
        public int totalSentences { get; set; }
        public int totalParagraphs { get; set; }
        public int totalImportantWords { get; set; }
        public int totalBugsOpen { get; set; }
        public int totalBugsResolved { get; set; }
        public int yearStart { get; set; }
        public int yearEnd { get; set; }
        public List<int> years { get; set; }
        public bool fiction { get; set; }
        public string rawHtml { get; set; }
        public string rawText { get; set; }
        public string url { get; set; }
        public string domain { get; set; }
        public string title { get; set; }
        public string summary { get; set; }
        public List<DomElement> elements { get; set; }
        public List<AnalyzedTag> tagNames { get; set; }
        public List<AnalyzedParentIndex> parentIndexes { get; set; }
        public List<string> words { get; set; }
        public List<AnalyzedPhrase> phrases { get; set; }
        public List<ArticleSubject> subjects { get; set; }
        public List<AnalyzedImage> images { get; set; }
        public List<int> body { get; set; }
        public List<DomElement> bodyElements { get; set; }
        public List<string> sentences { get; set; }
        public AnalyzedAuthor author { get; set; }
        public DateTime publishDate { get; set; }
        public List<AnalyzedPerson> people { get; set; }
        public AnalyzedByLlm llm { get; set; }

        /// <summary>
        /// All urls grouped by domain
        /// </summary>
        public Dictionary<string, List<KeyValuePair<string, string>>> urls { get; set; }
        
        /// <summary>
        /// safe URLs found on the web page
        /// </summary>
        public List<string> urlLinks { get; set; }

        public AnalyzedArticle(string url = "", string html = "")
        {
            author = new AnalyzedAuthor();
            body = new List<int>();
            bodyElements = new List<DomElement>();
            domain = Web.GetDomainName(url);
            elements = new List<DomElement>();
            fiction = true;
            feedId = 0;
            id = 0;
            importance = 0;
            parentIndexes = new List<AnalyzedParentIndex>();
            people = new List<AnalyzedPerson>();
            phrases = new List<AnalyzedPhrase>();
            images = new List<AnalyzedImage>();
            publishDate = DateTime.Now;
            rawHtml = html;
            relevance = 0;
            importance = 0;
            sentences = new List<string>();
            subjects = new List<ArticleSubject>();
            tagNames = new List<AnalyzedTag>();
            title = "";
            summary = "";
            totalImportantWords = 0;
            totalParagraphs = 0;
            totalSentences = 0;
            totalWords = 0;
            this.url = url != "" ? Web.CleanUrl(url, true, false, Rules.commonQueryKeys) : "";
            yearEnd = 0;
            yearStart = 0;
            years = new List<int>();
        }
    }

    public class AnalyzedWord
    {
        public string word { get; set; }
        public int subjectId { get; set; }
    }

    public class AnalyzedTag
    {
        public string name { get; set; }
        public int count { get; set; }
        public int[] index { get; set; }
    }

    public class AnalyzedPhrase
    {
        public int id { get; set; }
        public string phrase { get; set; }
        public int[] words { get; set; }
        public int count { get; set; }
    }

    public class AnalyzedImage
    {
        public int index { get; set; }
        public string url { get; set; }
        public string filename { get; set; }
        public string extension { get; set; }
        public bool exists { get; set; } = false;
    }

    public class AnalyzedAuthor
    {
        public string name { get; set; }
        public AuthorSex sex { get; set; }
    }

    public class AnalyzedFile
    {
        public string filename { get; set; }
        public string fileType { get; set; }
    }

    public class AnalyzedParentIndex
    {
        public List<int> elements { get; set; }
        public int index { get; set; }
        public int textLength { get; set; }
    }

    public class AnalyzedElement
    {
        public DomElement Element { get; set; }
        public int index { get; set; }
        public int depth { get; set; } //node hierarchy depth
        public int wordsInHierarchy { get; set; }
        public int badClasses { get; set; }
        public bool isBad { get; set; } = false;
        public bool isContaminated { get; set; } = false; //if contaminated, ignore contaminated element & all child elements when querying parent elements
        public List<ElementFlags> flags { get; set; } = new List<ElementFlags>();
        public Dictionary<ElementFlagCounters, int> counters { get; set; } = new Dictionary<ElementFlagCounters, int>();
        public List<string> badClassNames { get; set; } = new List<string>();
        public List<AnalyzedElement> hierarchy { get; set; } = new List<AnalyzedElement>();
        public string flagInfo { get; set; }

        public void AddFlag(ElementFlags flag)
        {
            if (!flags.Contains(flag)) { flags.Add(flag); }
        }

        public void UpdateCounter(ElementFlagCounters counter, int count)
        {
            if (!counters.ContainsKey(counter))
            {
                counters.Add(counter, count);
            }
            else
            {
                counters[counter] += count;
            }
        }

        public bool HasFlag(ElementFlags flag)
        {
            return flags.Contains(flag);
        }

        public int Counter(ElementFlagCounters counter)
        {
            if (counters.ContainsKey(counter))
            {
                return counters[counter];
            }
            return 0;
        }

        public void CopyTo(AnalyzedElement element)
        {
            element.Element = Element;
            element.index = index;
            element.depth = depth;
            element.wordsInHierarchy = wordsInHierarchy;
            element.badClasses = badClasses;
            element.isBad = isBad;
            element.isContaminated = isContaminated;
            element.flags = flags.ToArray().ToList();
            foreach (var item in counters)
            {
                element.counters.Add(item.Key, item.Value);
            }
            element.badClassNames = badClassNames.ToArray().ToList();
            element.hierarchy = hierarchy.ToArray().ToList();
        }
    }

    public enum ElementFlags
    {
        BadTag = 0,
        BadUrl = 1,
        MenuItem = 2,
        BadHeaderWord = 3,
        BadLinkWord = 4,
        IsImage = 5,
        IsMenuList = 6,
        IsMenuHeader = 7,
        IsLegalContent = 8,
        IsArticleTitle = 9,
        IsArticleAuthor = 10,
        IsArticleDatePublished = 11,
        BadHeaderMenu = 12,
        ExcludedAnalyzerRule = 13,
        ProtectedAnalyzerRule = 14
    }

    public enum ElementFlagCounters
    {
        words = 0,
        childTextElements = 1,
        badKeywords = 2,
        badLegalWords = 3
    }

    public class PossibleTextType
    {
        public TextType type { get; set; }
        public int count { get; set; }
    }

    public class ArticleSubject
    {
        public int id { get; set; }
        public int parentId { get; set; }
        public string title { get; set; }
        public WordType type { get; set; }
        public string[] breadcrumb { get; set; }
        public int[] hierarchy { get; set; }
        public List<int> parentIndexes { get; set; }
        public int count { get; set; }
        public int score { get; set; }
    }

    public class AnalyzedPerson
    {
        public string fullName { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string surName { get; set; }
        public int[] references { get; set; } //word indexes within article words (he, she, his, hers, him, her, he'd, she'd, he's, she's, etc...)
    }

    public class ArticleHtmlList
    {
        public string html { get; set; }
        public List<string> list { get; set; }
        public int id { get; set; }
    }

    public enum TextType
    {
        text = 0,
        authorName = 1,
        publishDate = 2,
        comment = 3,
        advertisement = 4,
        linkTitle = 5,
        menuTitle = 6,
        header = 7,
        copyright = 8,
        script = 9,
        useless = 10,
        style = 11,
        anchorLink = 12,
        menuItem = 13,
        listItem = 14,
        image = 15,
        lineBreak = 16,
        quote = 17,
        preformatted = 18
    }

    public enum WordType
    {
        none = 0,
        verb = 1,
        adverb = 2,
        noun = 3,
        pronoun = 4,
        adjective = 5,
        article = 6,
        preposition = 7,
        conjunction = 8,
        interjection = 9,
        punctuation = 10
    }

    public enum WordCategory
    {
        person = 0,
        place = 1,
        thing = 2,
        year = 3

    }

    public enum AuthorSex
    {
        female = 0,
        male = 1
    }

    public class ArticlePart
    {
        public List<TextType> type { get; set; } = new List<TextType>();
        public string title { get; set; } = "";
        public string value { get; set; } = "";
        public string classNames { get; set; } = "";
        public int fontSize { get; set; } = 1;
        public int indent { get; set; } = 0;
        public int quote { get; set; } = 0;
        public int listItem { get; set; } = 0;
    }

    #region "Analyzed LLM Results"

    public class AnalyzedByLlm
    {
        public List<AnalyzedLlmElement> elements { get; set; } = new List<AnalyzedLlmElement>();
        public List<AnalyzedLlmComment> comments { get; set; } = new List<AnalyzedLlmComment>();
        public List<AnalyzedLlmPerson> people { get; set; } = new List<AnalyzedLlmPerson>();
        public List<AnalyzedLlmCompany> companies { get; set; } = new List<AnalyzedLlmCompany>();
    }

    public class AnalyzedLlmElement
    {
        public LlmElementType type { get; set; }
        public string value { get; set; } = "";
    }

    public enum LlmElementType
    {
        text = 0,
        image = 1
    }

    public class AnalyzedLlmComment
    {
        public string user { get; set; } = "";
        public string comment { get; set; } = "";
        public DateTime? date { get; set; }
        public List<AnalyzedLlmComment> comments { get; set; } = new List<AnalyzedLlmComment>();
    }

    public class AnalyzedLlmPerson
    {
        public string name { get; set; } = "";
        public string position { get; set; } = "";
        public string company { get; set; } = "";
        public List<string> quotes { get; set; } = new List<string>();
    }

    public class AnalyzedLlmCompany
    {
        public string name { get; set; } = "";
    }

    #endregion
}
