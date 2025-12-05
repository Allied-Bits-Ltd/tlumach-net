// <copyright file="TranslationTree.cs" company="Allied Bits Ltd.">
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

using System;
using System.Collections.Generic;
using System.Text;

#if GENERATOR
namespace Tlumach.Generator
#else
namespace Tlumach.Base
#endif
{
    /// <summary>
    /// Contains translation entries that belong to one locale as a tree.
    /// <para>This class is for internal use and for parsers from other formats.</para>.
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
