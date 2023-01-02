using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CodeKicker.BBCode.Core
{
    public class BBTag
    {
        public const string ContentPlaceholderName = "content";

        public BBTag(
            string name, 
            string openTagTemplate, 
            string closeTagTemplate,
            int id,
            bool autoRenderContent = true, 
            BBTagClosingStyle tagClosingClosingStyle = BBTagClosingStyle.RequiresClosingTag, 
            Func<string, string>? contentTransformer = null, 
            bool enableIterationElementBehavior = false, 
            string bbcodeUid = "", 
            bool allowUrlProcessingAsText = true, 
            bool allowChildren = true, 
            IEnumerable<BBAttribute>? attributes = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            OpenTagTemplate = openTagTemplate ?? throw new ArgumentNullException(nameof(openTagTemplate));
            CloseTagTemplate = closeTagTemplate ?? throw new ArgumentNullException(nameof(closeTagTemplate));
            AutoRenderContent = autoRenderContent;
            TagClosingStyle = tagClosingClosingStyle;
            ContentTransformer = contentTransformer;
            EnableIterationElementBehavior = enableIterationElementBehavior;
            Attributes = attributes?.ToArray() ?? Array.Empty<BBAttribute>();
            Id = id;
            BBCodeUid = bbcodeUid;
            AllowUrlProcessingAsText = allowUrlProcessingAsText;
            AllowChildren = allowChildren;
        }

        public string Name { get; private set; }
        public string OpenTagTemplate { get; private set; }
        public string CloseTagTemplate { get; private set; }
        public bool AutoRenderContent { get; private set; }
        public bool StopProcessing { get; set; }
        public bool GreedyAttributeProcessing { get; set; }
        public bool SuppressFirstNewlineAfter { get; set; }
        public bool EnableIterationElementBehavior { get; set; }
        public bool RequiresClosingTag => TagClosingStyle == BBTagClosingStyle.RequiresClosingTag;
        public BBTagClosingStyle TagClosingStyle { get; private set; }
        public Func<string, string>? ContentTransformer { get; private set; } //allows for custom modification of the tag content before rendering takes place
        public BBAttribute[] Attributes { get; private set; }
        public int Id { get; private set; }
        public string BBCodeUid { get; }
        public bool AllowUrlProcessingAsText { get; }
        public bool AllowChildren { get; }

        public BBAttribute? FindAttribute(string name)
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