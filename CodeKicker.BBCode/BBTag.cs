using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CodeKicker.BBCode.Core
{
    public class BBTag
    {
        public const string ContentPlaceholderName = "content";

        /// <summary>
        /// Create a new instance of this class.
        /// </summary>
        /// <param name="name"><inheritdoc cref="Name" path="/summary/node()"/></param>
        /// <param name="openTagTemplate"><inheritdoc cref="OpenTagTemplate" path="/summary/node()"/></param>
        /// <param name="closeTagTemplate"><inheritdoc cref="CloseTagTemplate" path="/summary/node()"/></param>
        /// <param name="id"><inheritdoc cref="Id" path="/summary/node()"/></param>
        /// <param name="autoRenderContent"><inheritdoc cref="AutoRenderContent" path="/summary/node()"/></param>
        /// <param name="tagClosingStyle"><inheritdoc cref="TagClosingStyle" path="/summary/node()"/></param>
        /// <param name="contentTransformer"><inheritdoc cref="ContentTransformer" path="/summary/node()"/></param>
        /// <param name="enableIterationElementBehavior"><inheritdoc cref="EnableIterationElementBehavior" path="/summary/node()"/></param>
        /// <param name="bbcodeUid"><inheritdoc cref="BBCodeUid" path="/summary/node()"/></param>
        /// <param name="greedyAttributeProcessing"><inheritdoc cref="GreedyAttributeProcessing" path="/summary/node()"/></param>
        /// <param name="suppressFirstNewlineAfter"><inheritdoc cref="SuppressFirstNewlineAfter" path="/summary/node()"/></param>
        /// <param name="allowUrlProcessingAsText"><inheritdoc cref="AllowUrlProcessingAsText" path="/summary/node()"/></param>
        /// <param name="allowChildren"><inheritdoc cref="AllowChildren" path="/summary/node()"/></param>
        /// <param name="attributes"><inheritdoc cref="Attributes" path="/summary/node()"/></param>
        /// <exception cref="ArgumentNullException">When the name is null.</exception>
        /// <exception cref="ArgumentException">When <paramref name="tagClosingStyle"/> is set to <see cref="BBTagClosingStyle.RequiresClosingTag"/> and <paramref name="enableIterationElementBehavior"/> is true. </exception>
        public BBTag(
            string name,
            string openTagTemplate,
            string closeTagTemplate,
            int id,
            bool autoRenderContent = true,
            BBTagClosingStyle tagClosingStyle = BBTagClosingStyle.RequiresClosingTag,
            Func<string, string>? contentTransformer = null,
            bool enableIterationElementBehavior = false,
            string? bbcodeUid = null,
            bool greedyAttributeProcessing = false,
            bool suppressFirstNewlineAfter = false,
            bool allowUrlProcessingAsText = true,
            bool allowChildren = true,
            IEnumerable<BBAttribute>? attributes = null)
        {
            if (tagClosingStyle == BBTagClosingStyle.RequiresClosingTag && enableIterationElementBehavior)
            {
                throw new ArgumentException("Illegal value.", nameof(tagClosingStyle));
            }

            Name = name ?? throw new ArgumentNullException(nameof(name));
            OpenTagTemplate = openTagTemplate ?? throw new ArgumentNullException(nameof(openTagTemplate));
            CloseTagTemplate = closeTagTemplate ?? throw new ArgumentNullException(nameof(closeTagTemplate));
            AutoRenderContent = autoRenderContent;
            TagClosingStyle = tagClosingStyle;
            ContentTransformer = contentTransformer;
            EnableIterationElementBehavior = enableIterationElementBehavior;
            GreedyAttributeProcessing = greedyAttributeProcessing;
            SuppressFirstNewlineAfter = suppressFirstNewlineAfter;
            Attributes = attributes?.ToList() ?? new List<BBAttribute>();
            Id = id;
            BBCodeUid = bbcodeUid ?? string.Empty;
            AllowUrlProcessingAsText = allowUrlProcessingAsText;
            AllowChildren = allowChildren;
        }

        /// <summary>
        /// Tag name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Open html tag template. Supports wildcards such as ${content} or ${attributeName} that will be replaced accordingly.
        /// </summary>
        public string OpenTagTemplate { get; }

        /// <summary>
        /// Close html tag template. Supports wildcards such as ${content} or ${attributeName} that will be replaced accordingly.
        /// </summary>
        public string CloseTagTemplate { get; }

        /// <summary>
        /// Whether the content within the bb tag will be rendered automatically within the html tags or not. If false, the content will be rendered only where the ${content} wildcard is encountered.
        /// </summary>
        public bool AutoRenderContent { get; }

        /// <summary>
        /// This tag allows for a single attribute whose value supports whitespace. By default, multiple attributes are allowed with the limitation of not allowing whitespace in their values.
        /// </summary>
        public bool GreedyAttributeProcessing { get; }

        /// <summary>
        /// Ignores the first new line char after this tag.
        /// </summary>
        public bool SuppressFirstNewlineAfter { get; }

        /// <summary>
        /// This element behaves like a list item: it allows tags as content, it auto-closes and it does not nest.
        /// </summary>
        public bool EnableIterationElementBehavior { get; }

        /// <summary>
        /// Whether this element requires a closing tag.
        /// </summary>
        public bool RequiresClosingTag => TagClosingStyle == BBTagClosingStyle.RequiresClosingTag;

        /// <summary>
        /// Expected behavior on tag close. See allowed values in <see cref="BBTagClosingStyle"/>.
        /// </summary>
        public BBTagClosingStyle TagClosingStyle { get; }

        /// <summary>
        /// Allows for custom modification of the tag content before rendering takes place.
        /// </summary>
        public Func<string, string>? ContentTransformer { get; } //allows for custom modification of the tag content before rendering takes place

        /// <summary>
        /// This tag's attributes.
        /// </summary>
        public IReadOnlyList<BBAttribute> Attributes { get; }

        /// <summary>
        /// Unique tag identifier. All <see cref="BBTag"/> instances passed to an instance of <see cref="BBCodeParser"/> must have unique ids.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Legacy bb tag unique identifier used in phpbb v3 and lower.
        /// </summary>
        public string BBCodeUid { get; }

        /// <summary>
        /// Whether URLs are ignored. By default, they are rendered as a &lt;a href...&gt; tag even if not explicitly defined as a link/url in bb code.
        /// </summary>
        public bool AllowUrlProcessingAsText { get; }

        /// <summary>
        /// Whether this tags can nest other tags. If false, all bb code detected within this tag will be left unparsed.
        /// </summary>
        public bool AllowChildren { get; }

        /// <summary>
        /// Searches for an attribute in this tag, by name.
        /// </summary>
        /// <param name="name">Name of the attribute to search for.</param>
        /// <param name="attribute">Search result, if found, null otherwise.</param>
        /// <returns>true if found, null otherwise</returns>
        public bool FindAttribute(string name, [MaybeNullWhen(false)] out BBAttribute attribute)
        {
            attribute = Attributes.FirstOrDefault(a => a.Name == name);
            return attribute is not null;
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