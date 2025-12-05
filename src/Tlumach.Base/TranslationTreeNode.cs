// <copyright file="TranslationTreeNode.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// Contains one translation entry in the tree.
    /// <para>This class is for internal use and for parsers from other formats.</para>.
    /// </summary>
    public class TranslationTreeLeaf
    {
        public string Key { get; }

        public bool IsTemplated { get;  }

        public TranslationTreeLeaf(string key, bool isTemplated)
        {
            Key = key;
            IsTemplated = isTemplated;
        }
    }

    /// <summary>
    /// Contains a group of translation entries in the tree.
    /// <para>This class is for internal use and for parsers from other formats.</para>.
    /// </summary>
    public class TranslationTreeNode
    {
        public Dictionary<string, TranslationTreeNode> ChildNodes { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Contains the list of keys in the node.
        /// Each key is a string without parent node names in it.
        /// </summary>
        public Dictionary<string, TranslationTreeLeaf> Keys { get; } = [];

        /// <summary>
        /// Contains the own name of the node (without parent names).
        /// </summary>
        public string Name { get; }

        public TranslationTreeNode(string name)
        {
            Name = name;
        }

        public TranslationTreeNode? FindNode(string name)
        {
            TranslationTreeNode? result = null;
            if (string.IsNullOrEmpty(name))
                return null;
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
            int idx = name.IndexOf('.');
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

            if (idx == -1)
            {
                if (ChildNodes.TryGetValue(name, out result))
                    return result;
            }
            else
            if (idx > 0)
            {
                if (ChildNodes.TryGetValue(name.Substring(0, idx), out result) && result is not null)
                {
                    return result.FindNode(name.Substring(idx + 1));
                }
            }

            return null;
        }

        public TranslationTreeNode? MakeNode(string name)
        {
            TranslationTreeNode? result = null;
            if (string.IsNullOrEmpty(name))
                return null;
#pragma warning disable CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.
            int idx = name.IndexOf('.');
#pragma warning restore CA1307 // '...' has a method overload that takes a 'StringComparison' parameter. Replace this call ... for clarity of intent.

            if (idx == -1)
            {
                if (ChildNodes.TryGetValue(name, out result))
                    return result;
                result = new TranslationTreeNode(name);
                ChildNodes.Add(name, result);
                return result;
            }
            else
            if (idx > 0)
            {
                string ownName = name.Substring(0, idx);
                string subName = name.Substring(idx + 1);
                if (ChildNodes.TryGetValue(ownName, out result) && result is not null)
                {
                    return result.MakeNode(subName);
                }

                result = new TranslationTreeNode(ownName);
                ChildNodes.Add(ownName, result);
                return result.MakeNode(subName);
            }
            else
            {
                return null;
            }
        }
    }
}
