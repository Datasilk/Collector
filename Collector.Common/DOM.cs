using Collector.Common.Extensions.Strings;

namespace Collector.Common.DOM
{
    public class DomElement
    {
        public Parser Parser;
        public bool isSelfClosing; // <tag/>
        public bool isClosing; // </tag>
        public int index;
        public int parent;
        public int[] hierarchyIndexes;
        public string tagName;
        public string text;
        public string id;
        public List<string> className;
        public List<int> childIndexes;
        public Dictionary<string, string> attribute;
        public Dictionary<string, string> style;

        private int _nextSibling = -1;
        private int _firstChild = -1;
        private int _nofirstChild = -1;

        public DomElement(Parser parser)
        {
            Parser = parser;
        }

        public List<DomElement> Find(string XPath)
        {
            return Parser.Find(XPath, this);
        }

        public List<DomElement> Children(int limit = 0)
        {
            var items = new List<DomElement>();
            if (childIndexes == null) { return items; }
            foreach (var x in childIndexes)
            {
                items.Add(Parser.Elements[x]);
                if (limit > 0) { if (x + 1 == limit) { return items; } }
            }
            return items;
        }

        public DomElement FirstChild
        {
            get
            {
                if (_firstChild >= 0)
                {
                    return Parser.Elements[_firstChild];
                }
                if (childIndexes != null)
                {
                    if (childIndexes.Count > 0)
                    {
                        _firstChild = childIndexes[0];
                        return Parser.Elements[_firstChild];
                    }

                }
                if (_nofirstChild == -1 && index < Parser.Elements.Count - 1)
                {
                    var hierarchy = string.Join(">", hierarchyIndexes);
                    for (var x = index + 1; x < Parser.Elements.Count; x++)
                    {
                        var childhier = string.Join(">", Parser.Elements[x].hierarchyIndexes);
                        if (childhier.IndexOf(hierarchy) >= 0)
                        {
                            var newhier = childhier.Replace(hierarchy, "");
                            if (newhier.IndexOf(">") < 0)
                            {
                                return Parser.Elements[x];
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    _nofirstChild = 1;
                }



                return null;
            }
        }

        public DomElement NextSibling
        {
            get
            {
                if (_nextSibling >= 0) { return Parser.Elements[_nextSibling]; }
                var hierarchy = string.Join(">", hierarchyIndexes);
                DomElement elem; ;
                var childhier = "";
                var x = index;
                while (x < Parser.Elements.Count - 1)
                {
                    x++;
                    elem = Parser.Elements[x];
                    childhier = string.Join(">", elem.hierarchyIndexes);
                    if (childhier == hierarchy && elem.isClosing == false)
                    {
                        return elem;
                    }
                    else if (childhier.IndexOf(hierarchy) < 0)
                    {
                        return null;
                    }
                }
                return null;
            }
        }

        public DomElement Parent
        {
            get
            {
                if (hierarchyIndexes.Length == 0) { return null; }
                return Parser.Elements[hierarchyIndexes[hierarchyIndexes.Length - 1]];
            }
        }

        public int HierarchyTagIndex(string tag)
        {
            for (var x = hierarchyIndexes.Length - 1; x >= 0; x--)
            {
                if (Parser.Elements[hierarchyIndexes[x]].tagName == tag)
                {
                    return x;
                }
            }
            return -1;
        }

        public bool HasTagInHierarchy(string tag)
        {
            return HierarchyTagIndex(tag) >= 0;
        }

        public List<DomElement> Hierarchy()
        {
            var elems = new List<DomElement>();
            for (var x = 0; x < hierarchyIndexes.Length; x++)
            {
                elems.Add(Parser.Elements[hierarchyIndexes[x]]);
            }
            return elems;
        }

        public List<string> HierarchyTags()
        {
            var tags = new List<string>();
            for (var x = 0; x < hierarchyIndexes.Length; x++)
            {
                tags.Add(Parser.Elements[hierarchyIndexes[x]].tagName);
            }
            return tags;
        }

        public List<DomElement> AllChildren(int limitDepth = -1)
        {
            var children = new List<DomElement>();
            TraverseElements(this, children, limitDepth);
            return children;
        }

        private int TraverseElements(DomElement root, List<DomElement> children, int limitDepth = -1)
        {
            var childNodes = root.Children();
            var maxDepth = 1;
            for (var x = 0; x < childNodes.Count; x++)
            {
                children.Add(childNodes[x]);
                if (limitDepth != 0)
                {
                    var depth = TraverseElements(childNodes[x], children, limitDepth -= 1);
                    if (depth > maxDepth) { maxDepth = depth; }
                }
            }
            return maxDepth;
        }
    }

    public class Parser
    {
        public string rawHtml;
        public List<DomElement> Elements;
        public string documentType = "html";

        public Parser(string htm)
        {
            rawHtml = htm;
            Elements = new List<DomElement>();
            Parse(htm);
        }

        public void Parse(string htm)
        {
            if (htm.Length <= 3) { return; }
            bool isClosingTag = false;
            bool isSelfClosing = false;
            bool isInScript = false;
            bool isComment = false;
            bool foundTag = false;
            int s1, s2, s3, xs = -1;
            int parentElement = -1;
            string str1, schar, strTag, strText, docType = "html";
            var hierarchy = new List<string>();
            var hierarchyIndexes = new List<int>();
            var domTag = new DomElement(this);
            var textTag = new DomElement(this);
            var tagNameChars = new string[] { "/", "!", "?" };

            for (var x = 0; x < htm.Length; x++)
            {
                //find HTML tag
                domTag = new DomElement(this);

                if (foundTag == false && xs == 0)
                {
                    //no tags found in htm, create text tag and exit
                    textTag = new DomElement(this)
                    {
                        tagName = "#text",
                        text = htm
                    };
                    AddTag(textTag, parentElement, true, false, hierarchy, hierarchyIndexes);
                    break;
                }
                else if (xs == -1)
                {
                    xs = x;
                }
                else if (foundTag == true)
                {
                    xs = x;
                }

                isClosingTag = false;
                isSelfClosing = false;
                isComment = false;
                foundTag = false;
                if (isInScript == true)
                {
                    //find closing script tag
                    //TODO: make sure </script> tag isn't in a 
                    //      javascript string, but instead is the
                    //      actual closing tag for the script
                    x = htm.IndexOf("</script>", x);
                    if (x == -1) { break; }
                    schar = htm.Substring(x, 9).ToString();
                }
                else
                {
                    //find next html tag
                    x = htm.IndexOf('<', x);
                    if (x == -1) { break; }
                    schar = htm.Substring(x, 3).ToString();
                }
                if (schar[0] == '<')
                {
                    if (schar[1].ToString().OnlyAlphabet(tagNameChars))
                    {
                        //found HTML tag
                        s1 = htm.IndexOf(">", x + 2);
                        s2 = htm.IndexOf("<", x + 2);
                        if (s1 >= 0)
                        {
                            //check for comment
                            if (htm.Substring(x + 1, 3) == "!--")
                            {
                                s1 = htm.IndexOf("-->", x + 1);
                                if (s1 < 0)
                                {
                                    s1 = htm.Length - 1;
                                }
                                else
                                {
                                    s1 += 2;
                                }
                                s2 = -1;
                                isSelfClosing = true;
                                isComment = true;
                            }

                            //check for broken tag
                            if (s2 < s1 && s2 >= 0) { continue; }

                            //found end of tag
                            foundTag = true;
                            strTag = htm.Substring(x + 1, s1 - (x + 1));

                            //check for self-closing tag
                            str1 = strTag.Substring(strTag.Length - 1, 1);
                            if (str1 == "/" || (str1 == "?" && schar[1] == '?')) { isSelfClosing = true; }
                            if (Elements.Count == 0)
                            {
                                if (strTag.IndexOf("?xml") == 0) { docType = "xml"; }
                            }
                            documentType = docType;

                            //check for attributes
                            domTag.className = new List<string>();
                            if (isComment == true)
                            {
                                domTag.tagName = "!--";
                                domTag.text = strTag.Substring(3, strTag.Length - 5);
                            }
                            else
                            {
                                s3 = strTag.IndexOf(" ");
                                if (s3 < 0)
                                {
                                    //tag has no attributes
                                    if (isSelfClosing)
                                    {
                                        if (strTag.Length > 1)
                                        {
                                            domTag.tagName = strTag.Substring(0, strTag.Length - 2).ToLower();
                                        }
                                    }
                                    else
                                    {
                                        //tag has no attributes & no forward-slash
                                        domTag.tagName = strTag.ToLower();
                                    }
                                }
                                else
                                {
                                    //tag has attributes
                                    domTag.tagName = strTag.Substring(0, s3).ToLower();
                                    domTag.attribute = GetAttributes(strTag);
                                    domTag.style = new Dictionary<string, string>();

                                    //set up class name list
                                    if (domTag.attribute.ContainsKey("class"))
                                    {
                                        domTag.className = new List<string>(domTag.attribute["class"].Split(' '));
                                    }
                                    else { domTag.className = new List<string>(); }

                                    //set up style dictionary
                                    if (domTag.attribute.ContainsKey("style"))
                                    {
                                        var domStyle = new List<string>(domTag.attribute["style"].Split(';'));
                                        foreach (string keyval in domStyle)
                                        {
                                            var styleKeyVal = keyval.Trim().Split(new char[] { ':' }, 2);
                                            if (styleKeyVal.Length == 2)
                                            {
                                                var kv = styleKeyVal[0].Trim().ToLower();
                                                if (domTag.style.ContainsKey(kv) == false)
                                                {
                                                    domTag.style.Add(kv, styleKeyVal[1].Trim());
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                            if (domTag.tagName != "")
                            {
                                //check if tag is script
                                if (docType == "html")
                                {
                                    if (isInScript == true)
                                    {
                                        isInScript = false;
                                    }
                                    else if (domTag.tagName == "script" && isSelfClosing == false)
                                    {
                                        isInScript = true;
                                    }

                                    //check if tag is self-closing even if it
                                    //doesn't include a forward-slash at the end
                                    switch (domTag.tagName)
                                    {
                                        case "br":
                                        case "img":
                                        case "input":
                                        case "link":
                                        case "meta":
                                        case "hr":
                                            isSelfClosing = true;
                                            break;
                                    }
                                }

                                if (domTag.tagName.Substring(0, 1) == "!")
                                {
                                    //comments & doctype are self-closing tags
                                    isSelfClosing = true;
                                }

                                if (schar[1] == '/')
                                {
                                    //found closing tag
                                    isClosingTag = true;
                                }

                                //extract text before beginning of tag
                                strText = htm.Substring(xs, x - xs).Trim();
                                if (strText != "")
                                {
                                    textTag = new DomElement(this)
                                    {
                                        tagName = "#text",
                                        text = strText
                                    };
                                    AddTag(textTag, parentElement, true, false, hierarchy, hierarchyIndexes);
                                }

                                //check if domTag is unusable
                                if (domTag.tagName == "" || domTag.tagName == null)
                                {
                                    foundTag = false;
                                    continue;
                                }

                                //add tag to array
                                parentElement = AddTag(domTag, parentElement, isSelfClosing, isClosingTag, hierarchy, hierarchyIndexes);
                                //parentElement = pelem;
                                if (isClosingTag == true)
                                {
                                    //go back one parent if this tag is a closing tag
                                    if (parentElement >= 0)
                                    {
                                        if (Elements[parentElement].tagName != domTag.tagName.Replace("/", ""))
                                        {
                                            //not the same tag as the current parent tag, add missing closing tag
                                            if (Elements[parentElement].parent >= 0)
                                            {
                                                if (Elements[Elements[parentElement].parent].tagName == domTag.tagName.Replace("/", ""))
                                                {
                                                    //replace unknown closing tag with missing closing tag
                                                    domTag.tagName = "/" + Elements[Elements[parentElement].parent].tagName;
                                                }
                                                else
                                                {
                                                    //skip this closing tag because it doesn't have an opening tag
                                                    //Elements.RemoveAt(Elements.Count - 1);
                                                    x = xs = s1;
                                                    continue;
                                                }
                                            }
                                        }
                                        parentElement = Elements[parentElement].parent;
                                        if (hierarchy.Count > 0)
                                        {
                                            hierarchy.RemoveAt(hierarchy.Count - 1);
                                            hierarchyIndexes.RemoveAt(hierarchyIndexes.Count - 1);
                                        }
                                    }
                                }
                            }
                            x = xs = s1;
                        }
                    }
                }
            }
            //finally, add last text tag (if possible)
            if (xs < htm.Length - 1)
            {
                if (htm.Substring(xs).Trim().Replace("\r", "").Replace("\n", "").Length > 0)
                {
                    textTag = new DomElement(this)
                    {
                        tagName = "#text",
                        text = htm.Substring(xs)
                    };
                    AddTag(textTag, parentElement, true, false, hierarchy, hierarchyIndexes);
                }
            }
        }

        public Dictionary<string, string> GetAttributes(string tag)
        {
            var attrs = new Dictionary<string, string>();
            int s1, s2, s3, s4, s5;
            string attrName, str2;
            string[] arr;
            s1 = tag.IndexOf(" ");
            if (s1 >= 1)
            {
                for (var x = s1; x < tag.Length; x++)
                {
                    if (x >= tag.Length - 3) { break; }
                    //look for attribute name
                    s2 = tag.IndexOf("=", x);
                    s3 = tag.IndexOf(" ", x);
                    s4 = tag.IndexOf("\"", x);
                    s5 = tag.IndexOf("'", x);
                    if (s4 < 0) { s4 = tag.Length + 1; }
                    if (s5 < 0) { s5 = tag.Length + 2; }
                    if (s3 < s2 && s3 < s4 && s3 < s5)
                    {
                        //found a space first, then equal sign (=), then quotes
                        attrName = tag.Substring(s3 + 1, s2 - (s3 + 1)).ToLower();
                        //find type of quotes to use
                        if (s4 < s5 && s4 < tag.Length)
                        {
                            //use double quotes
                            arr = tag.Substring(s4 + 1).Replace("\\\"", "{{q}}").Split('"');
                            str2 = arr[0].Replace("{{q}}", "\\\"");
                            if (!attrs.ContainsKey(attrName))
                            {
                                attrs.Add(attrName, str2);
                            }
                            x = s4 + str2.Length + 1;
                        }
                        else if (s5 < tag.Length)
                        {
                            //use single quotes
                            arr = tag.Substring(s5 + 1).Replace("\\'", "{{q}}").Split('\'');
                            str2 = arr[0].Replace("{{q}}", "\\'");
                            if (!attrs.ContainsKey(attrName))
                            {
                                attrs.Add(attrName, str2);
                            }
                            x = s5 + str2.Length + 1;
                        }
                    }
                }
            }
            return attrs;
        }

        private int AddTag(DomElement domTag, int parentElement, bool isSelfClosing, bool isClosingTag, List<string> hierarchy, List<int> hierarchyIndexes)
        {
            domTag.parent = parentElement;
            domTag.index = Elements.Count;
            if (domTag.attribute == null)
            {
                domTag.attribute = new Dictionary<string, string>();
            }
            if (hierarchyIndexes != null)
            {
                if (hierarchyIndexes.Count > 0)
                {
                    if (hierarchyIndexes[hierarchyIndexes.Count - 1] != domTag.index)
                    {
                        parentElement = hierarchyIndexes[hierarchyIndexes.Count - 1];
                    }
                    else if (hierarchyIndexes.Count > 1)
                    {
                        parentElement = hierarchyIndexes[hierarchyIndexes.Count - 2];
                    }

                }
            }

            if (parentElement > -1)
            {
                DomElement parent = Elements[parentElement];
                if (parent.childIndexes == null)
                {
                    parent.childIndexes = new List<int>();
                }
                parent.childIndexes.Add(Elements.Count);
                Elements[parentElement] = parent;
            }

            //make current tag the parent
            if (isSelfClosing == false && isClosingTag == false)
            {
                parentElement = Elements.Count;
                hierarchy.Add(domTag.tagName);
                hierarchyIndexes.Add(parentElement);
            }

            domTag.isSelfClosing = isSelfClosing;
            domTag.isClosing = isClosingTag;
            domTag.hierarchyIndexes = hierarchyIndexes.ToArray();
            Elements.Add(domTag);
            return parentElement;
        }

        #region "Query"
        public List<DomElement> Find(string XPath, DomElement rootElement = null)
        {
            var elements = new List<DomElement>();
            if (XPath == "") { return elements; }
            if (XPath.IndexOf("/") < 0) { return elements; }
            if (Elements.Count == 0) { return elements; }
            var root = rootElement;
            DomElement elem;
            var domIndex = 0;
            if (root == null)
            {
                //search from first element
                root = Elements[0];
            }
            else
            {
                //start search at rootElement;
                domIndex = rootElement.index + 1;
            }

            //search the DOM to find elements based on the XPath query
            var paths = XPath.Split('/');
            var lastPath = "";
            var searchPath = "";
            var searchFunc = "";
            var searchName = "";
            var hierarchy = "";
            var childhier = "";
            foreach (var path in paths)
            {
                if (path == "")
                {
                    //hierarchy symbol
                    if (lastPath == "/")
                    {
                        //look anywhere in the hierarchy
                        searchPath = "//";

                    }
                    else
                    {
                        searchPath = "/";
                    }
                }
                else
                {
                    //check for search function
                    if (path.IndexOf("[") >= 0)
                    {
                        searchFunc = "[" + path.Split('[')[1];
                        searchName = path.Replace(searchFunc, "").ToLower();
                    }
                    else
                    {
                        searchName = path.ToLower();
                    }

                    //find matching elements
                    switch (searchPath)
                    {
                        case "/":
                            //find elements at current hierarchy level
                            foreach (var child in root.Children())
                            {
                                if (child.tagName == searchName)
                                {
                                    //found matching element !!!!!!!
                                    elements.Add(child);
                                }
                            }
                            break;

                        case "//":
                            //find elements at any hierarchy level
                            if (root.hierarchyIndexes.Length > 0)
                            {
                                hierarchy = string.Join(">", root.hierarchyIndexes);
                            }
                            else
                            {
                                hierarchy = "";
                            }
                            for (var x = root.index + 1; x < Elements.Count; x++)
                            {
                                elem = Elements[x];
                                if (elem.hierarchyIndexes.Length > 0)
                                {
                                    childhier = string.Join(">", elem.hierarchyIndexes);
                                }
                                else
                                {
                                    childhier = "";
                                }
                                if (childhier.IndexOf(hierarchy) == 0)
                                {
                                    if (elem.tagName == searchName)
                                    {
                                        //found matching element !!!!!!!
                                        elements.Add(elem);
                                    }
                                }
                            }
                            break;
                    }
                }
                lastPath = path;
                if (lastPath == "") { lastPath = "/"; }
            }

            return elements;
        }

        #endregion
    }
}
