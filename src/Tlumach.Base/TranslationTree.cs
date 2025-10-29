using System;
using System.Collections.Generic;
using System.Text;

namespace Tlumach.Base
{
    /// <summary>
    /// Contains translation entries that belong to one locale as a tree - this .
    /// </summary>
    public class TranslationTree
    {
        public TranslationTreeNode RootNode { get; } = new(string.Empty);

        public TranslationTreeNode? FindNode(string name)
        {
            return RootNode.FindNode(name);
        }

        public TranslationTreeNode? MakeNode(string name)
        {
            return RootNode.MakeNode(name);
        }
    }
}
