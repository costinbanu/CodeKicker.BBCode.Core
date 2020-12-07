using CodeKicker.BBCode.Core.SyntaxTree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeKicker.BBCode.Core
{
    /// <summary>
    /// This class is useful for creating a custom parser. You can customize which tags are available
    /// and how they are translated to HTML.
    /// In order to use this library, we require a link to http://codekicker.de/ from you. Licensed unter the Creative Commons Attribution 3.0 Licence: http://creativecommons.org/licenses/by/3.0/.
    /// </summary>
    public class BBCodeParser
    {
        public BBCodeParser(IList<BBTag> tags) : this(ErrorMode.ErrorFree, null, tags) { }

        public BBCodeParser(ErrorMode errorMode, string textNodeHtmlTemplate, IList<BBTag> tags)
        {
            if (!Enum.IsDefined(typeof(ErrorMode), errorMode)) throw new ArgumentOutOfRangeException("errorMode");
            ErrorMode = errorMode;
            TextNodeHtmlTemplate = textNodeHtmlTemplate;
            Tags = tags ?? throw new ArgumentNullException("tags");
            Bitfield = new Bitfield();
        }

        public IList<BBTag> Tags { get; private set; }
        public string TextNodeHtmlTemplate { get; private set; }
        public ErrorMode ErrorMode { get; private set; }
        public Bitfield Bitfield { get; private set; }

        private static readonly string nonAlphaNumericUrlCharsNoForwardSlash = "_-.,&#%!@$?;:+=*|";
        private static readonly string nonAlphaNumericUrlChars = $"{nonAlphaNumericUrlCharsNoForwardSlash}/";
        private static readonly Regex _urlAllowedCharsRegex = new Regex(@$"[a-z0-9\u00a1-\uffff{Regex.Escape(nonAlphaNumericUrlChars).Replace("-", @"\-")}]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(20));
        private static readonly string[] _urlCandidateStartString = new[] { "https", "http", "www" };

        public virtual string ToHtml(string bbCode, string code = "")
        {
            if (bbCode == null) throw new ArgumentNullException("bbCode");
            return ParseSyntaxTree(bbCode, code).ToHtml();
        }

        public virtual SequenceNode ParseSyntaxTree(string bbCode, string code = "")
            => ParseSyntaxTreeImpl(bbCode, false, false, code).node;

        private (SequenceNode node, Bitfield bitfield) ParseSyntaxTreeImpl(string bbCode, bool setBitfield, bool preserveWhitespace, string code = "", bool transformUrls = true)
        {
            if (bbCode == null) throw new ArgumentNullException("bbCode");

            Stack<SyntaxTreeNode> stack = new Stack<SyntaxTreeNode>();
            var rootNode = new SequenceNode();
            stack.Push(rootNode);

            var bitfield = IterateInText(bbCode, stack, preserveWhitespace, code, setBitfield, transformUrls);

            while (stack.Count > 1) //close all tags that are still open and can be closed implicitly
            {
                var node = (TagNode)stack.Pop();
                if (node.Tag.RequiresClosingTag && ErrorMode == ErrorMode.Strict) throw new BBCodeParsingException(MessagesHelper.GetString("TagNotClosed", node.Tag.Name));
            }

            if (stack.Count != 1)
            {
                Debug.Assert(ErrorMode != ErrorMode.ErrorFree);
                throw new BBCodeParsingException(""); //only the root node may be left
            }

            return (rootNode, bitfield);
        }

        /// <summary>
        /// Transforms a bbcode text so that it will be readable by at least a phpbb 3.0.x platform. Plain text is already HTML encoded.
        /// </summary>
        /// <param name="text">text with bb code</param>
        /// <param name="uid">BBCode UID</param>
        /// <param name="uidLength">BBCode UID length</param>
        /// <returns><see cref="Tuple{T1, T2, T3}"/> where the first item is the transformed text, the second is the bbcode uid field and the third is the bbcode bitfield</returns>
        public (string bbCode, string uid, string bitfield) TransformForBackwardsCompatibility(string text, string uid = "", int uidLength = 8)
        {
            try
            {
                var actualUid = string.IsNullOrWhiteSpace(uid) ? ToBase36(Math.Abs(Convert.ToInt64($"0x{Guid.NewGuid().ToString("n").Substring(4, 16)}", 16))).Substring(0, uidLength) : uid;
                var dummyParser = new BBCodeParser(ErrorMode.TryErrorCorrection, null, Tags.Select(x => new BBTag(x.Name, x.OpenTagTemplate, x.CloseTagTemplate, x.AutoRenderContent, x.TagClosingStyle, x.ContentTransformer, x.EnableIterationElementBehavior, x.Id, actualUid, x.AllowUrlProcessingAsText, x.Attributes)).ToList());
                var (node, bitfield) = dummyParser.ParseSyntaxTreeImpl(text, true, true, uid, false);
                return (node.ToLegacyBBCode(), actualUid, bitfield.GetBase64());
            }
            catch
            {
                return (text, string.Empty, string.Empty);
            }
        }

        Bitfield IterateInText(string bbCode, Stack<SyntaxTreeNode> stack, bool preserveWhitespace, string code, bool setBitfield, bool transformUrls)
        {
            var end = 0;
            var bitfield = setBitfield ? new Bitfield() : null;
            while (end < bbCode.Length)
            {
                if (MatchTagEnd(bbCode, code, ref end, stack, preserveWhitespace, bitfield))
                    continue;

                if (MatchStartTag(bbCode, code, ref end, stack, preserveWhitespace))
                    continue;

                if (MatchTextNode(bbCode, ref end, stack, transformUrls))
                    continue;

                if (ErrorMode != ErrorMode.ErrorFree)
                    throw new BBCodeParsingException(""); //there is no possible match at the current position

                AppendText(bbCode[end].ToString(), stack); //if the error free mode is enabled force interpretation as text if no other match could be made
                end++;
            }

            Debug.Assert(end == bbCode.Length); //assert bbCode was matched entirely

            return bitfield;
        }

        string ToBase36(long value)
        {
            var chars = "0123456789abcdefghijklmnopqrstuvwxyz"; 
            var result = new StringBuilder();

            while (value > 0L)
            {
                result.Insert(0, chars[unchecked((int)(value % 36L))]);
                value /= 36L;
            }

            return result.ToString();
        }

        bool MatchTagEnd(string bbCode, string code, ref int pos, Stack<SyntaxTreeNode> stack, bool preserveWhitespace, Bitfield bitfield = null)
        {
            var openingNode = stack.Peek() as TagNode; //could also be a SequenceNode
            int end = pos;
            var tagEnd = ParseTagEnd(bbCode, code, ref end, preserveWhitespace);

            if (!(tagEnd?.Equals("code", StringComparison.InvariantCultureIgnoreCase) ?? false) && (openingNode?.Tag?.Name?.Equals("code", StringComparison.InvariantCultureIgnoreCase) ?? false))
            {
                //we are inside a [code] tag that contains BBCode. The inner code is supposed to be displayed 'as is', and thus we do not parse it
                return false;
            }

            if (tagEnd != null)
            {
                while (true)
                {
                    openingNode = stack.Peek() as TagNode;
                    if (bitfield != null && openingNode?.Tag?.Id != null)
                    {
                        bitfield.Set(openingNode.Tag.Id);
                    }
                    if (openingNode == null && ErrorOrReturn("TagNotOpened", tagEnd)) return false;
                    Debug.Assert(openingNode != null); //ErrorOrReturn will either or throw make this stack frame exit

                    if (!openingNode.Tag.Name.Equals(tagEnd, StringComparison.OrdinalIgnoreCase))
                    {
                        //a nesting imbalance was detected

                        if (openingNode.Tag.RequiresClosingTag && ErrorOrReturn("TagNotMatching", tagEnd, openingNode.Tag.Name))
                            return false;
                        else
                            stack.Pop();
                    }
                    else
                    {
                        //the opening node properly matches the closing node
                        stack.Pop();
                        break;
                    }
                }
                pos = end;
                return true;
            }

            return false;
        }
        bool MatchStartTag(string bbCode, string code, ref int pos, Stack<SyntaxTreeNode> stack, bool preserveWhitespace)
        {
            if (stack.OfType<TagNode>().Any(n => n.Tag.Name.Equals("code", StringComparison.InvariantCultureIgnoreCase)))
            {
                //we are inside a [code] tag that contains BBCode. The inner code is supposed to be displayed 'as is', and thus we do not parse it
                return false;
            }

            int end = pos;
            var tag = ParseTagStart(bbCode, code, ref end, preserveWhitespace);
            if (tag != null)
            {
                if (tag.Tag.EnableIterationElementBehavior)
                {
                    //this element behaves like a list item: it allows tags as content, it auto-closes and it does not nest.
                    //the last property is ensured by closing all currently open tags up to the opening list element

                    var isThisTagAlreadyOnStack = stack.OfType<TagNode>().Any(n => n.Tag == tag.Tag);
                    //if this condition is false, no nesting would occur anyway

                    if (isThisTagAlreadyOnStack)
                    {
                        var openingNode = stack.Peek() as TagNode; //could also be a SequenceNode
                        Debug.Assert(openingNode != null); //isThisTagAlreadyOnStack would have been false

                        if (openingNode.Tag != tag.Tag && ErrorMode == ErrorMode.Strict && ErrorOrReturn("TagNotMatching", tag.Tag.Name, openingNode.Tag.Name)) return false;

                        if (tag.Tag.Name == "*" && openingNode.Tag == tag.Tag) //allow nesting within lists
                        {
                            stack.Pop();
                        }
                        else if(tag.Tag.Name != "*") //this is not a list, so close all tags
                        {
                            while (true)
                            {
                                var poppedOpeningNode = (TagNode)stack.Pop();

                                if (poppedOpeningNode.Tag != tag.Tag)
                                {
                                    //a nesting imbalance was detected

                                    if (openingNode.Tag.RequiresClosingTag && ErrorMode == ErrorMode.Strict && ErrorOrReturn("TagNotMatching", tag.Tag.Name, openingNode.Tag.Name))
                                        return false;
                                    //close the (wrongly) open tag. we have already popped so do nothing.
                                }
                                else
                                {
                                    //the opening node matches the closing node
                                    //close the already open li-item. we have already popped. we have already popped so do nothing.
                                    break;
                                }
                            }
                        }
                    }
                }

                stack.Peek().SubNodes.Add(tag);
                if (tag.Tag.TagClosingStyle != BBTagClosingStyle.LeafElementWithoutContent)
                    stack.Push(tag); //leaf elements have no content - they are closed immediately
                pos = end;
                return true;
            }

            return false;
        }
        bool MatchTextNode(string bbCode, ref int pos, Stack<SyntaxTreeNode> stack, bool transformUrls)
        {
            int end = pos;

            var textNode = ParseText(bbCode, ref end, stack, transformUrls);
            if (textNode != null)
            {
                AppendText(textNode, stack);
                pos = end;
                return true;
            }

            return false;
        }
        void AppendText(string textToAppend, Stack<SyntaxTreeNode> stack)
        {
            var currentNode = stack.Peek();
            var lastChild = currentNode.SubNodes.Count == 0 ? null : currentNode.SubNodes[currentNode.SubNodes.Count - 1] as TextNode;

            TextNode newChild;
            if (lastChild == null)
                newChild = new TextNode(textToAppend, TextNodeHtmlTemplate);
            else
                newChild = new TextNode(lastChild.Text + textToAppend, TextNodeHtmlTemplate);

            if (currentNode.SubNodes.Count != 0 && currentNode.SubNodes[currentNode.SubNodes.Count - 1] is TextNode)
                currentNode.SubNodes[currentNode.SubNodes.Count - 1] = newChild;
            else
                currentNode.SubNodes.Add(newChild);
        }

        TagNode ParseTagStart(string input, string code, ref int pos, bool preserveWhitespace)
        {
            var end = pos;

            if (!ParseChar(input, ref end, '[')) return null;

            var tagName = ParseName(input, code, ref end);
            if (tagName == null) return null;

            var tag = Tags.SingleOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (tag == null && ErrorOrReturn("UnknownTag", tagName)) return null;

            var result = new TagNode(tag);

            var defaultAttrValue = ParseAttributeValue(input, code, ref end, tag.GreedyAttributeProcessing);
            if (defaultAttrValue != null)
            {
                var attr = tag.FindAttribute("");
                if (attr == null && ErrorOrReturn("UnknownAttribute", tag.Name, "\"Default Attribute\"")) return null;
                result.AttributeValues.Add(attr, defaultAttrValue);
            }

            while (true)
            {
                ParseWhitespace(input, ref end);
                var attrName = ParseName(input, code, ref end);
                if (attrName == null) break;

                var attrVal = ParseAttributeValue(input, code, ref end);
                if (attrVal == null && ErrorOrReturn("")) return null;

                if (tag.Attributes == null && ErrorOrReturn("UnknownTag", tag.Name)) return null;
                var attr = tag.FindAttribute(attrName);
                if (attr == null && ErrorOrReturn("UnknownTag", tag.Name, attrName)) return null;

                if (result.AttributeValues.ContainsKey(attr) && ErrorOrReturn("DuplicateAttribute", tagName, attrName)) return null;
                result.AttributeValues.Add(attr, attrVal);
            }
            if (!ParseChar(input, ref end, ']', code) && ErrorOrReturn("TagNotClosed", tagName)) return null;

            if (!preserveWhitespace)
            {
                ParseWhitespace(input, ref end);
            }

            pos = end;
            return result;
        }
        string ParseTagEnd(string input, string code, ref int pos, bool preserveWhitespace)
        {
            var end = pos;

            if (!ParseChar(input, ref end, '[')) return null;
            if (!ParseChar(input, ref end, '/')) return null;

            var tagName = ParseName(input, code, ref end);
            if (tagName == null) return null;

            ParseWhitespace(input, ref end);

            if (!ParseChar(input, ref end, ']', code))
            {
                if (ErrorMode == ErrorMode.ErrorFree) return null;
                else throw new BBCodeParsingException("");
            }

            var tag = Tags.SingleOrDefault(t => t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

            if (tag != null && tag.SuppressFirstNewlineAfter && !preserveWhitespace)
            {
                ParseLimitedWhitespace(input, ref end, 1);
            }

            pos = end;
            return tagName;
        }
        string ParseText(string input, ref int pos, Stack<SyntaxTreeNode> stack, bool transformUrls)
        {
            int end = pos;
            bool escapeFound = false;
            bool anyEscapeFound = false;
            int? urlCandidateStartPos = null;
            var lastOpenTag = stack.Peek() as TagNode;
            var foundUrlIndexes = new List<(int startPos, int endPos)>(input.Length / 7 + 1);
            var insideHtmlTag = false;
            var openedHtmlTag = false;

            while (end < input.Length)
            {
                if (input[end] == '[' && !escapeFound) break;
                if (input[end] == ']' && !escapeFound)
                {
                    if (ErrorMode == ErrorMode.Strict)
                        throw new BBCodeParsingException(MessagesHelper.GetString("NonescapedChar"));
                }

                if (input[end] == '\\' && !escapeFound)
                {
                    escapeFound = true;
                    anyEscapeFound = true;
                }
                else if (escapeFound)
                {
                    if (!(input[end] == '[' || input[end] == ']' || input[end] == '\\'))
                    {
                        if (ErrorMode == ErrorMode.Strict)
                            throw new BBCodeParsingException(MessagesHelper.GetString("EscapeChar"));
                    }
                    escapeFound = false;
                }

                if (input[end] == '<' && !insideHtmlTag)
                {
                    insideHtmlTag = true;
                }

                if (input[end] == '>' && insideHtmlTag && !openedHtmlTag && end > 0 && input[end - 1] != '/' && input[end - 1] != '-')
                {
                    openedHtmlTag = true;
                }

                if (input[end] == '>' && insideHtmlTag)
                {
                    insideHtmlTag = false;
                }

                if (input[end] == '<' && !insideHtmlTag && openedHtmlTag)
                {
                    openedHtmlTag = false;
                }

                var urlStart = _urlCandidateStartString.FirstOrDefault(s => end - s.Length + 1 >= 0 && input[(end - s.Length + 1)..(end + 1)].Equals(s, StringComparison.OrdinalIgnoreCase));
                if (transformUrls && !insideHtmlTag && !openedHtmlTag && !urlCandidateStartPos.HasValue && !string.IsNullOrWhiteSpace(urlStart) 
                    && (end - urlStart.Length == -1 || !_urlAllowedCharsRegex.IsMatch(input[end - urlStart.Length].ToString())) 
                    && !Tags.Any(t => !t.AllowUrlProcessingAsText && t.Name.Equals(lastOpenTag?.Tag?.Name ?? "", StringComparison.OrdinalIgnoreCase)))
                {
                    urlCandidateStartPos = end - urlStart.Length + 1;
                }
                else if (urlCandidateStartPos.HasValue && !_urlAllowedCharsRegex.IsMatch(input[end].ToString()))
                {
                    foundUrlIndexes.Add((urlCandidateStartPos.Value - pos, end - pos));
                    urlCandidateStartPos = null;
                }
                end++;
            }

            if (urlCandidateStartPos.HasValue)
            {
                foundUrlIndexes.Add((urlCandidateStartPos.Value - pos, end - pos));
            }

            if (escapeFound)
            {
                if (ErrorMode == ErrorMode.Strict)
                    throw new BBCodeParsingException("");
            }

            var result = input[pos..end];
            var resultCopy = result;

            try
            {
                var offset = 0;
                foreach (var (startPos, endPos) in foundUrlIndexes)
                {
                    var value = result[(startPos + offset)..(endPos + offset)].TrimEnd(nonAlphaNumericUrlCharsNoForwardSlash.ToCharArray());
                    var linkText = value;
                    var linkAddress = value;
                    if (!linkAddress.StartsWith("http", StringComparison.InvariantCultureIgnoreCase) && !linkAddress.StartsWith("//", StringComparison.InvariantCultureIgnoreCase))
                    {
                        linkAddress = $"//{linkAddress}";
                    }
                    if (!Uri.TryCreate(linkAddress, UriKind.Absolute, out var dummy) || _urlCandidateStartString.Any(x => dummy.Host.Equals(x, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        continue;
                    }
                    if (linkText.Length > 53)
                    {
                        linkText = $"{linkText[0..40]} ... {linkText[^8..^0]}";
                    }
                    var (replaceResult, curOffset) = TextHelper.ReplaceAtIndex(result, value, $"<!-- m --><a href=\"{linkAddress}\" target=\"_blank\">{linkText}</a><!-- m -->", startPos + offset);
                    result = replaceResult;
                    offset += curOffset;
                }
            }
            catch
            {
                result = resultCopy;
            }

            if (anyEscapeFound)
            {
                var result2 = new char[result.Length];
                int writePos = 0;
                bool lastWasEscapeChar = false;
                for (int i = 0; i < result.Length; i++)
                {
                    if (!lastWasEscapeChar && result[i] == '\\')
                    {
                        if (i < result.Length - 1)
                        {
                            if (!(result[i + 1] == '[' || result[i + 1] == ']' || result[i + 1] == '\\'))
                                result2[writePos++] = result[i]; //the next char was not escapable. write the slash into the output array
                            else
                                lastWasEscapeChar = true; //the next char is meant to be escaped so the backslash is skipped
                        }
                        else
                        {
                            result2[writePos++] = '\\'; //the backslash was the last char in the string. just write it into the output array
                        }
                    }
                    else
                    {
                        result2[writePos++] = result[i];
                        lastWasEscapeChar = false;
                    }
                }
                result = new string(result2, 0, writePos);
            }

            pos = end;
            return result == "" ? null : result;
        }

        static string ParseName(string input, string code, ref int pos)
        {
            int end = pos;
            for (; end < input.Length && (char.ToLower(input[end]) >= 'a' && char.ToLower(input[end]) <= 'z' || (input[end]) >= '0' && (input[end]) <= '9' || input[end] == '*' || input[end] == '-'); end++) ;

            if (end - pos == 0) return null;

            var result = input[pos..end];

            pos = end;
            return result;
        }
        static string ParseAttributeValue(string input, string code, ref int pos, bool greedyProcessing = false)
        {
            var end = pos;

            if (end >= input.Length || input[end] != '=') return null;
            end++;

            int endIndex;
            if (!greedyProcessing)
            {
                endIndex = input.IndexOfAny(" []".ToCharArray(), end);
            }
            else
            {
                endIndex = input.IndexOfAny("[]".ToCharArray(), end);
            }

            if (endIndex == -1) endIndex = input.Length;

            var diff = 0;
            if (code.Length > 0 && input[endIndex] == ']' && endIndex >= code.Length && input[endIndex - code.Length - 1] == ':')
            {
                diff = code.Length + 1;
                endIndex -= diff;
            }

            var valStart = pos + 1;
            var result = input[valStart..endIndex];
            pos = endIndex + diff;
            return result;
        }
        static bool ParseWhitespace(string input, ref int pos)
        {
            int end = pos;
            while (end < input.Length && char.IsWhiteSpace(input[end]))
                end++;

            var found = pos != end;
            pos = end;
            return found;
        }
        static bool ParseLimitedWhitespace(string input, ref int pos, int maxNewlinesToConsume)
        {
            int end = pos;
            int consumedNewlines = 0;

            while (end < input.Length && consumedNewlines < maxNewlinesToConsume)
            {
                char thisChar = input[end];
                if (thisChar == '\r')
                {
                    end++;
                    consumedNewlines++;

                    if (end < input.Length && input[end] == '\n')
                    {
                        // Windows newline - just consume it
                        end++;
                    }
                }
                else if (thisChar == '\n')
                {
                    // Unix newline
                    end++;
                    consumedNewlines++;
                }
                else if (char.IsWhiteSpace(thisChar))
                {
                    // Consume the whitespace
                    end++;
                }
                else
                {
                    break;
                }
            }

            var found = pos != end;
            pos = end;
            return found;
        }
        static bool ParseChar(string input, ref int pos, char c, string code = null)
        {
            if (pos < input.Length && input[pos] == c)
            {
                pos++;
                return true;
            }
            if (!string.IsNullOrEmpty(code))
            {
                while (pos < input.Length)
                {
                    if (input[pos] == ':' && input.Substring(pos + 1, code.Length) == code)
                    {
                        break;
                    }
                    pos++;
                }
                if (pos < input.Length && input[pos] == ':' && input.Substring(pos + 1, code.Length) == code)
                {
                    pos += code.Length + 1;
                }
            }
            if (pos >= input.Length || input[pos] != c) return false;
            pos++;
            return true;
        }

        bool ErrorOrReturn(string msgKey, params string[] parameters)
        {
            if (ErrorMode == ErrorMode.ErrorFree) return true;
            else throw new BBCodeParsingException(string.IsNullOrEmpty(msgKey) ? "" : MessagesHelper.GetString(msgKey, parameters));
        }
    }

    public enum ErrorMode
    {
        /// <summary>
        /// Every syntax error throws a BBCodeParsingException.
        /// </summary>
        Strict,

        /// <summary>
        /// Syntax errors with obvious meaning will be corrected automatically.
        /// </summary>
        TryErrorCorrection,

        /// <summary>
        /// The parser will never throw an exception. Invalid tags like "array[0]" will be interpreted as text.
        /// </summary>
        ErrorFree,
    }
}
