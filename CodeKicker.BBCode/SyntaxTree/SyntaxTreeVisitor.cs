using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeKicker.BBCode.Core.SyntaxTree
{
    public class SyntaxTreeVisitor
    {
        public SyntaxTreeNode? Visit(SyntaxTreeNode? node)
        {
            if (node is null) return null;
            return node.AcceptVisitor(this);
        }
        protected internal virtual SyntaxTreeNode? Visit(SequenceNode? node)
        {
            if (node is null) return null;

            var modifiedSubNodes = GetModifiedSubNodes(node);

            if (modifiedSubNodes is null)
                return node; //unmodified
            else
                return node.SetSubNodes(modifiedSubNodes); //subnodes were modified
        }
        protected internal virtual SyntaxTreeNode? Visit(TagNode? node)
        {
            if (node is null) return null;

            var modifiedSubNodes = GetModifiedSubNodes(node);

            if (modifiedSubNodes is null)
                return node; //unmodified
            else
                return node.SetSubNodes(modifiedSubNodes); //subnodes were modified
        }
        protected internal virtual SyntaxTreeNode? Visit(TextNode? node)
        {
            return node;
        }

        SyntaxTreeNodeCollection? GetModifiedSubNodes(SyntaxTreeNode node)
        {
            SyntaxTreeNodeCollection? modifiedSubNodes = null; //lazy

            for (int i = 0; i < node.SubNodes.Count; i++)
            {
                var subNode = node.SubNodes[i];

                var replacement = Visit(subNode);
                if (replacement != subNode)
                {
                    if (modifiedSubNodes is null) //lazy init
                    {
                        modifiedSubNodes = new SyntaxTreeNodeCollection();
                        for (int j = 0; j < i; j++) //copy unmodified nodes
                            modifiedSubNodes.Add(node.SubNodes[j]);
                    }

                    if (replacement is not null) //insert replacement
                        modifiedSubNodes.Add(replacement);
                }
                else
                {
                    if (modifiedSubNodes is not null) //only insert unmodified subnode if the lazy collection has been initialized
                        modifiedSubNodes.Add(subNode);
                }
            }
            return modifiedSubNodes;
        }
    }
}
