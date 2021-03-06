﻿using System;
using System.Collections.Generic;
using System.Linq;
using Utility.DOM;
using Utility.Strings;

namespace Collector.Models.Article
{
    public class AnalyzedArticle
    {
        public int id;
        public int feedId;
        public int fileSize;
        public int relevance;
        public int importance;
        public int totalWords;
        public int totalSentences;
        public int totalParagraphs;
        public int totalImportantWords;
        public int totalBugsOpen;
        public int totalBugsResolved;
        public int yearStart;
        public int yearEnd;
        public List<int> years;
        public bool fiction;
        public string rawHtml;
        public string url;
        public string domain;
        public string title;
        public string summary;
        public List<DomElement> elements;
        public AnalyzedTags tags;
        public List<AnalyzedTag> tagNames;
        public List<AnalyzedParentIndex> parentIndexes;
        public List<AnalyzedWord> words;
        public List<AnalyzedPhrase> phrases;
        public List<ArticleSubject> subjects;
        public List<AnalyzedImage> images;
        public List<int> body;
        public List<DomElement> bodyElements;
        public List<string> sentences;
        public AnalyzedAuthor author;
        public DateTime publishDate;
        public List<AnalyzedPerson> people;
        
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
            tags = new AnalyzedTags();
            tags.anchorLinks = new List<int>();
            tags.headers = new List<int>();
            tags.text = new List<AnalyzedText>();
            title = "";
            summary = "";
            totalImportantWords = 0;
            totalParagraphs = 0;
            totalSentences = 0;
            totalWords = 0;
            this.url = url != "" ? Web.CleanUrl(url, true, false, Rules.commonQueryKeys) : "";
            words = new List<AnalyzedWord>();
            yearEnd = 0;
            yearStart = 0;
            years = new List<int>();
        }
    }

    public class AnalyzedTags
    {
        public List<AnalyzedText> text;
        public List<int> anchorLinks;
        public List<int> headers;
    }

    public class AnalyzedTag
    {
        public string name;
        public int count;
        public int[] index;
    }

    public class AnalyzedText
    {
        public int index;
        public List<AnalyzedWordInText> words;
        public TextType type;
        //public List<PossibleTextType> possibleTypes;
    }

    public class AnalyzedWord
    {
        public int id;
        public string word;
        public int count;
        public int importance;
        public bool suspicious;
        public WordType type;
        public WordCategory category;
        public bool apostrophe;
    }

    public class AnalyzedPhrase
    {
        public int id;
        public string phrase;
        public int[] words;
        public int count;
    }

    public class AnalyzedWordInText
    {
        public string word;
        public AnalyzedWord[] relations;
        public int index;
    }

    public class AnalyzedImage
    {
        public int index;
        public string url;
        public string filename;
        public string extension;
        public bool exists = false;
    }

    public class AnalyzedAuthor
    {
        public string name;
        public AuthorSex sex;
    }

    public class AnalyzedFile
    {
        public string filename;
        public string fileType;
    }

    public class AnalyzedParentIndex
    {
        public List<int> elements;
        public int index;
        public int textLength;
    }

    public class AnalyzedElement
    {
        public DomElement Element;
        public int index;
        public int depth; //node hierarchy depth
        public int wordsInHierarchy;
        public int badClasses;
        public bool isBad = false;
        public bool isContaminated = false; //if contaminated, ignore contaminated element & all child elements when querying parent elements
        public List<ElementFlags> flags = new List<ElementFlags>();
        public Dictionary<ElementFlagCounters, int> counters = new Dictionary<ElementFlagCounters, int>();
        public List<string> badClassNames = new List<string>();
        public List<AnalyzedElement> hierarchy = new List<AnalyzedElement>();

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
            foreach(var item in counters)
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
        BadHeaderMenu = 12
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
        public TextType type;
        public int count;
    }

    public class ArticleSubject
    {
        public int id;
        public int parentId;
        public string title;
        public WordType type;
        public string[] breadcrumb;
        public int[] hierarchy;
        public List<int> parentIndexes;
        public int count;
        public int score;
    }

    public class AnalyzedPerson
    {
        public string fullName;
        public string firstName;
        public string middleName;
        public string lastName;
        public string surName;
        public int[] references; //word indexes within article words (he, she, his, hers, him, her, he'd, she'd, he's, she's, etc...)
    }

    public class ArticleHtmlList
    {
        public string html;
        public List<string> list;
        public int id;
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
        public List<TextType> type = new List<TextType>();
        public string title = "";
        public string value = "";
        public string classNames = "";
        public int fontSize = 1;
        public int indent = 0;
        public int quote = 0;
        public int listItem = 0;
    }
}
