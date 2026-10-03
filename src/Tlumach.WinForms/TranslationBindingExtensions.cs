// <copyright file="TranslationBindingExtensions.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel;
using System.Windows.Forms;

namespace Tlumach.WinForms
{
    /// <summary>
    /// Methods that bind properties of Windows Forms components to translation units, so that the properties are updated when the language changes.
    /// </summary>
    public static class TranslationBindingExtensions
    {
        /// <summary>
        /// Binds the <see cref="Control.Text"/> property of the control to the translation unit.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the control.</returns>
        public static TranslationBinding BindTranslation(this Control control, TranslationUnit unit)
            => BindTranslation(control, unit, static (c, text) => c.Text = text);

        /// <summary>
        /// Binds the <see cref="ToolStripItem.Text"/> property of the tool strip item (menu item, toolbar button, status label, etc.) to the translation unit.
        /// </summary>
        /// <param name="item">The tool strip item.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the item.</returns>
        public static TranslationBinding BindTranslation(this ToolStripItem item, TranslationUnit unit)
            => BindTranslation(item, unit, static (i, text) => i.Text = text);

        /// <summary>
        /// Binds the <see cref="ColumnHeader.Text"/> property of the list view column header to the translation unit.
        /// </summary>
        /// <param name="column">The column header.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the column header.</returns>
        public static TranslationBinding BindTranslation(this ColumnHeader column, TranslationUnit unit)
            => BindTranslation(column, unit, static (c, text) => c.Text = text);

        /// <summary>
        /// Binds the tooltip that the <see cref="ToolTip"/> component shows for the control to the translation unit.
        /// </summary>
        /// <param name="toolTip">The ToolTip component.</param>
        /// <param name="control">The control, for which the tooltip is shown.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <returns>The binding. It is disposed of automatically together with the control.</returns>
        public static TranslationBinding BindTranslation(this ToolTip toolTip, Control control, TranslationUnit unit)
        {
            if (toolTip is null)
                throw new ArgumentNullException(nameof(toolTip));

            return BindTranslation(control, unit, (c, text) => toolTip.SetToolTip(c, text));
        }

        /// <summary>
        /// Binds an arbitrary property of the component to the translation unit, using the provided method to set the property.
        /// </summary>
        /// <typeparam name="T">The type of the component (anything that implements <see cref="IComponent"/>, including <see cref="DataGridViewColumn"/>).</typeparam>
        /// <param name="component">The component.</param>
        /// <param name="unit">The translation unit (usually, a member of the generated class).</param>
        /// <param name="apply">The method that sets the text, e.g., <c>(control, text) =&gt; control.AccessibleDescription = text</c>.</param>
        /// <returns>The binding. It is disposed of automatically together with the component.</returns>
        public static TranslationBinding BindTranslation<T>(this T component, TranslationUnit unit, Action<T, string> apply)
            where T : IComponent
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            if (unit is null)
                throw new ArgumentNullException(nameof(unit));

            if (apply is null)
                throw new ArgumentNullException(nameof(apply));

            return new TranslationBinding(component, unit, text => apply(component, text));
        }
    }
}
