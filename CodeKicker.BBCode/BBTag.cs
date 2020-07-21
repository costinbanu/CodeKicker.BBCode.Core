using System;

namespace CodeKicker.BBCode.Core
{
    public class BBTag
    {
        public const string ContentPlaceholderName = "content";

        public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, BBTagClosingStyle tagClosingClosingStyle, Func<string, string> contentTransformer, bool enableIterationElementBehavior, int id, string bbcodeUid = "", params BBAttribute[] attributes)
        {
            if (!Enum.IsDefined(typeof(BBTagClosingStyle), tagClosingClosingStyle)) throw new ArgumentException("tagClosingClosingStyle");

            Name = name ?? throw new ArgumentNullException("name");
            OpenTagTemplate = openTagTemplate ?? throw new ArgumentNullException("openTagTemplate");
            CloseTagTemplate = closeTagTemplate ?? throw new ArgumentNullException("closeTagTemplate");
            AutoRenderContent = autoRenderContent;
            TagClosingStyle = tagClosingClosingStyle;
            ContentTransformer = contentTransformer;
            EnableIterationElementBehavior = enableIterationElementBehavior;
            Attributes = attributes ?? new BBAttribute[0];
            Id = id;
            BBCodeUid = bbcodeUid;
        }
        
        public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, BBTagClosingStyle tagClosingClosingStyle, Func<string, string> contentTransformer, int id, string bbcodeUid = "", params BBAttribute[] attributes)
            : this(name, openTagTemplate, closeTagTemplate, autoRenderContent, tagClosingClosingStyle, contentTransformer, false, id, bbcodeUid, attributes)
        {
        }

        public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, bool requireClosingTag, Func<string, string> contentTransformer, int id, string bbcodeUid = "", params BBAttribute[] attributes)
            : this(name, openTagTemplate, closeTagTemplate, autoRenderContent, requireClosingTag ? BBTagClosingStyle.RequiresClosingTag : BBTagClosingStyle.AutoCloseElement, contentTransformer, id, bbcodeUid, attributes)
        {
        }

        public BBTag(string name, string openTagTemplate, string closeTagTemplate, bool autoRenderContent, bool requireClosingTag, int id, string bbcodeUid = "", params BBAttribute[] attributes)
            : this(name, openTagTemplate, closeTagTemplate, autoRenderContent, requireClosingTag, null, id, bbcodeUid, attributes)
        {
        }

        public BBTag(string name, string openTagTemplate, string closeTagTemplate, int id, string bbcodeUid = "", params BBAttribute[] attributes)
            : this(name, openTagTemplate, closeTagTemplate, true, true, id, bbcodeUid, attributes)
        {
        }

        public string Name { get; private set; }
        public string OpenTagTemplate { get; private set; }
        public string CloseTagTemplate { get; private set; }
        public bool AutoRenderContent { get; private set; }
        public bool StopProcessing { get; set; }
        public bool GreedyAttributeProcessing { get; set; }
        public bool SuppressFirstNewlineAfter { get; set; }
        public bool EnableIterationElementBehavior { get; set; }
        public bool RequiresClosingTag
        {
            get { return TagClosingStyle == BBTagClosingStyle.RequiresClosingTag; }
        }
        public BBTagClosingStyle TagClosingStyle { get; private set; }
        public Func<string, string> ContentTransformer { get; private set; } //allows for custom modification of the tag content before rendering takes place
        public BBAttribute[] Attributes { get; private set; }
        public int Id { get; private set; }
        public string BBCodeUid { get; }

        public BBAttribute FindAttribute(string name)
        {
            return Array.Find(Attributes, a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public enum BBTagClosingStyle
    {
        RequiresClosingTag = 0,
        AutoCloseElement = 1,
        LeafElementWithoutContent = 2, //leaf elements have no content - they are closed immediately
    }

    public enum HtmlEncodingMode
    {
        HtmlEncode = 0,
        HtmlAttributeEncode = 1,
        UnsafeDontEncode = 2,
    }
}