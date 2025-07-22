namespace Collector.Common.Models.DOM
{
    //used for deserializing computed DOM object from Charlotte service
    public class Document
    {
        public string[] a { get; set; }//attribute names
        public string[] t { get; set; } //tag names
        public string[] cn { get; set; }//class names
        public Node dom { get; set; }
        public string url { get; set; }
        public string[] err { get; set; }
    }

    public class Dictionaries //used by Html.Traverse
    {
        public string[] a { get; set; }//attribute names
        public string[] t { get; set; } //tag names
        public string[] cn { get; set; } //class names
    }

    public class Node
    {
        public int t { get; set; } = 0; //tag name index
        public int[] s { get; set; } = null; //array of style values [display (0 = none, 1 = block, 2 = inline), font-size, bold, italic]
        public int[] n { get; set; } = null; //array of class names
        public Dictionary<int, string> a { get; set; } //dictionary of element attributes
        public List<Node> c { get; set; } //list of child elements
        public string v { get; set; } //optional #text element value
    }
}
