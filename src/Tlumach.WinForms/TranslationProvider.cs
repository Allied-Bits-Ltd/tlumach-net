// <copyright file="TranslationProvider.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;

using Tlumach.Base;

namespace Tlumach.WinForms
{
    /// <summary>
    /// A component that sets the texts of controls, tool strip items, and list view column headers from translations and updates them when the language changes.
    /// <para>Place the component onto a form in the Visual Studio Designer and assign translation keys to components using the "TranslationKey on ..." and "ToolTipKey on ..." properties.
    /// At run time, the component resolves the keys using <see cref="TranslationManager"/> or, if that is not set, <see cref="DefaultTranslationManager"/>,
    /// and re-applies the texts every time the culture of the translation manager changes.</para>
    /// <para>When a key is not found, the text set in the Designer is kept. In design mode, the component never changes any text, so translated texts never get into the form's code.</para>
    /// </summary>
    [ProvideProperty("TranslationKey", typeof(Component))]
    [ProvideProperty("ToolTipKey", typeof(Component))]
    [ToolboxItemFilter("System.Windows.Forms")]
    [Description("Sets the texts of the components on a form from Tlumach translations and updates them when the language changes.")]
    public sealed class TranslationProvider : Component, IExtenderProvider, ISupportInitialize
    {
        private readonly Dictionary<Component, TargetKeys> _targets = new();

        private UiInvoker _invoker = new();
        private TranslationManager? _translationManager;
        private TranslationManager? _subscribedManager;
        private ToolTip? _toolTip;
        private ContainerControl? _containerControl;
        private bool _applyRightToLeft;
        private bool _initializing;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationProvider"/> class.
        /// </summary>
        public TranslationProvider()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationProvider"/> class and adds it to the container. The Designer uses this constructor.
        /// </summary>
        /// <param name="container">The container of the form's components.</param>
        public TranslationProvider(IContainer container)
            : this()
        {
            if (container is null)
                throw new ArgumentNullException(nameof(container));

            container.Add(this);
        }

        /// <summary>
        /// Gets or sets the translation manager used by the providers whose <see cref="TranslationManager"/> property is not set.
        /// <para>Set it once at application startup (e.g., in <c>Program.Main</c>) to the <c>TranslationManager</c> property of the generated class.
        /// A provider picks up the current value of this property every time it applies the translations.</para>
        /// </summary>
        public static TranslationManager? DefaultTranslationManager { get; set; }

        /// <summary>
        /// Gets or sets the translation manager that resolves the keys. When <see langword="null"/>, <see cref="DefaultTranslationManager"/> is used.
        /// <para>The property is not available in the Designer; set it in code, e.g., after the call to <c>InitializeComponent</c>. Setting it applies the translations.</para>
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TranslationManager? TranslationManager
        {
            get => _translationManager;
            set
            {
                if (ReferenceEquals(_translationManager, value))
                    return;

                _translationManager = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Windows.Forms.ToolTip"/> component that shows the tooltips assigned to controls using the "ToolTipKey" property.
        /// <para>Tool strip items show their own tooltips and do not need this component.</para>
        /// </summary>
        [Category("Localization")]
        [DefaultValue(null)]
        [Description("The ToolTip component that shows the tooltips assigned to controls using the ToolTipKey property.")]
        public ToolTip? ToolTip
        {
            get => _toolTip;
            set
            {
                if (ReferenceEquals(_toolTip, value))
                    return;

                _toolTip = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the provider sets the <see cref="Control.RightToLeft"/> property of <see cref="ContainerControl"/>
        /// (and <see cref="Form.RightToLeftLayout"/> when it is a form) according to the writing direction of the current culture.
        /// </summary>
        [Category("Localization")]
        [DefaultValue(false)]
        [Description("Indicates whether the RightToLeft property of ContainerControl (and RightToLeftLayout of a form) follows the writing direction of the current culture.")]
        public bool ApplyRightToLeft
        {
            get => _applyRightToLeft;
            set
            {
                if (_applyRightToLeft == value)
                    return;

                _applyRightToLeft = value;
                ApplyTranslations();
            }
        }

        /// <summary>
        /// Gets or sets the form or user control, the writing direction of which is updated when <see cref="ApplyRightToLeft"/> is enabled.
        /// <para>In the Designer, this property is set automatically to the form or user control being designed.</para>
        /// </summary>
        [Category("Localization")]
        [DefaultValue(null)]
        [Description("The form or user control whose writing direction is updated when ApplyRightToLeft is enabled.")]
        public ContainerControl? ContainerControl
        {
            get => _containerControl;
            set
            {
                if (ReferenceEquals(_containerControl, value))
                    return;

                _containerControl = value;
                ApplyTranslations();
            }
        }

        /// <inheritdoc/>
        public override ISite? Site
        {
            get => base.Site;
            set
            {
                base.Site = value;

                // Like ErrorProvider, pick the root component (the form or user control being designed) as the container control,
                // so that the Designer serializes "translationProvider1.ContainerControl = this;".
                if (value?.GetService(typeof(IDesignerHost)) is IDesignerHost host && host.RootComponent is ContainerControl root)
                    _containerControl = root;
            }
        }

        private bool CanApply => !_initializing && !_disposed && !DesignMode;

        /// <summary>
        /// Tells the Designer which components get the "TranslationKey" and "ToolTipKey" properties: controls (including forms), tool strip items, and list view column headers.
        /// </summary>
        /// <param name="extendee">The component to check.</param>
        /// <returns><see langword="true"/> if the component can have translation keys.</returns>
        public bool CanExtend(object extendee) => extendee is Control or ToolStripItem or ColumnHeader;

        /// <summary>
        /// Gets the key of the translation assigned to the <c>Text</c> property of the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The key, or an empty string if no key is assigned.</returns>
        [DefaultValue("")]
        [Category("Localization")]
        [Description("The key of the translation assigned to the Text property.")]
        [Localizable(false)]
        public string GetTranslationKey(Component component)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            return _targets.TryGetValue(component, out TargetKeys? keys) ? keys.TranslationKey ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Assigns the key of the translation to the <c>Text</c> property of the component and, at run time, applies the translation.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="key">The key. <see langword="null"/> or an empty string removes the assignment; the current text is left as is.</param>
        public void SetTranslationKey(Component component, string? key)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            SetKey(component, key, isToolTip: false);
        }

        /// <summary>
        /// Gets the key of the translation assigned to the tooltip of the component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>The key, or an empty string if no key is assigned.</returns>
        [DefaultValue("")]
        [Category("Localization")]
        [Description("The key of the translation assigned to the tooltip. Controls need the ToolTip property of the provider to be set; tool strip items use their ToolTipText property.")]
        [Localizable(false)]
        public string GetToolTipKey(Component component)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            return _targets.TryGetValue(component, out TargetKeys? keys) ? keys.ToolTipKey ?? string.Empty : string.Empty;
        }

        /// <summary>
        /// Assigns the key of the translation to the tooltip of the component and, at run time, applies the translation.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <param name="key">The key. <see langword="null"/> or an empty string removes the assignment; the current tooltip is left as is.</param>
        public void SetToolTipKey(Component component, string? key)
        {
            if (component is null)
                throw new ArgumentNullException(nameof(component));

            SetKey(component, key, isToolTip: true);
        }

        /// <summary>
        /// Applies the translations for the current culture of the translation manager to all components that have keys assigned.
        /// <para>The provider calls this method itself when it is initialized, when the translation manager is set, and when the culture changes.
        /// Call it if the translations change in some other way (e.g., after reloading translation files).</para>
        /// </summary>
        public void ApplyTranslations()
        {
            if (!CanApply)
                return;

            TranslationManager? manager = PrepareManager();
            if (manager is null)
                return;

            // A copy is iterated because setting a text may raise events whose handlers add or remove keys.
            foreach (KeyValuePair<Component, TargetKeys> pair in _targets.ToArray())
                ApplyTo(manager, pair.Key, pair.Value);

            ApplyRightToLeftSetting(manager);
        }

        /// <summary>
        /// Signals the beginning of initialization. The Designer calls this method at the beginning of <c>InitializeComponent</c>; translations are not applied until <see cref="EndInit"/>.
        /// </summary>
        public void BeginInit() => _initializing = true;

        /// <summary>
        /// Signals the end of initialization and applies the translations if a translation manager is available.
        /// </summary>
        public void EndInit()
        {
            _initializing = false;

            // InitializeComponent runs on the UI thread, which makes this the right moment to capture its synchronization context.
            _invoker = new UiInvoker();
            ApplyTranslations();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (_subscribedManager is not null)
                {
                    _subscribedManager.OnCultureChanged -= TranslationManager_OnCultureChanged;
                    _subscribedManager = null;
                }

                foreach (Component component in _targets.Keys.ToArray())
                    component.Disposed -= Target_Disposed;

                _targets.Clear();
            }

            base.Dispose(disposing);
        }

        private static string? Translate(TranslationManager manager, string key)
        {
            TranslationEntry entry = manager.GetValue(key, manager.CurrentCulture);

            // TranslationManager returns the TranslationEntry.Empty instance when the key is not found; the designed text is kept then.
            return ReferenceEquals(entry, TranslationEntry.Empty) ? null : entry.Text;
        }

        private void SetKey(Component component, string? key, bool isToolTip)
        {
            string? normalizedKey = string.IsNullOrEmpty(key) ? null : key;

            if (!_targets.TryGetValue(component, out TargetKeys? keys))
            {
                if (normalizedKey is null)
                    return;

                keys = new TargetKeys();
                _targets.Add(component, keys);
                component.Disposed += Target_Disposed;
            }

            if (isToolTip)
                keys.ToolTipKey = normalizedKey;
            else
                keys.TranslationKey = normalizedKey;

            if (keys.TranslationKey is null && keys.ToolTipKey is null)
            {
                RemoveTarget(component);
                return;
            }

            if (CanApply)
            {
                TranslationManager? manager = PrepareManager();
                if (manager is not null)
                    ApplyTo(manager, component, keys);
            }
        }

        /// <summary>
        /// Determines the effective translation manager and makes sure that the provider listens to the culture changes of that manager (and only of that manager).
        /// </summary>
        private TranslationManager? PrepareManager()
        {
            TranslationManager? manager = _translationManager ?? DefaultTranslationManager;

            if (!ReferenceEquals(manager, _subscribedManager))
            {
                if (_subscribedManager is not null)
                    _subscribedManager.OnCultureChanged -= TranslationManager_OnCultureChanged;

                _subscribedManager = manager;

                if (manager is not null)
                    manager.OnCultureChanged += TranslationManager_OnCultureChanged;
            }

            return manager;
        }

        private void ApplyTo(TranslationManager manager, Component component, TargetKeys keys)
        {
            if (component is Control { IsDisposed: true })
                return;

            string? text = keys.TranslationKey is null ? null : Translate(manager, keys.TranslationKey);
            string? toolTip = keys.ToolTipKey is null ? null : Translate(manager, keys.ToolTipKey);

            if (component is Control control)
            {
                if (text is not null)
                    control.Text = text;

                if (toolTip is not null)
                    _toolTip?.SetToolTip(control, toolTip);
            }
            else if (component is ToolStripItem item)
            {
                if (text is not null)
                    item.Text = text;

                if (toolTip is not null)
                    item.ToolTipText = toolTip;
            }
            else if (component is ColumnHeader column && text is not null)
            {
                column.Text = text;
            }
        }

        private void ApplyRightToLeftSetting(TranslationManager manager)
        {
            if (!_applyRightToLeft || _containerControl is null || _containerControl.IsDisposed)
                return;

            bool isRightToLeft = manager.CurrentCulture.TextInfo.IsRightToLeft;
            _containerControl.RightToLeft = isRightToLeft ? RightToLeft.Yes : RightToLeft.No;

            if (_containerControl is Form form)
                form.RightToLeftLayout = isRightToLeft;
        }

        private void RemoveTarget(Component component)
        {
            component.Disposed -= Target_Disposed;
            _targets.Remove(component);
        }

        private void Target_Disposed(object? sender, EventArgs e)
        {
            if (sender is Component component)
                RemoveTarget(component);
        }

        private void TranslationManager_OnCultureChanged(object? sender, CultureChangedEventArgs e)
        {
            // The culture may be changed on any thread, while the controls may only be updated on the UI thread.
            _invoker.Invoke(ApplyTranslations);
        }

        private sealed class TargetKeys
        {
            public string? TranslationKey { get; set; }

            public string? ToolTipKey { get; set; }
        }
    }
}
