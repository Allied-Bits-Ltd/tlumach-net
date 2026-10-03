namespace Tlumach.Sample.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            translationProvider1 = new Tlumach.WinForms.TranslationProvider(components);
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            welcomeLabel = new System.Windows.Forms.Label();
            helloLabel = new System.Windows.Forms.Label();
            languageLabel = new System.Windows.Forms.Label();
            languageComboBox = new System.Windows.Forms.ComboBox();
            nameLabel = new System.Windows.Forms.Label();
            nameTextBox = new System.Windows.Forms.TextBox();
            helloNameLabel = new System.Windows.Forms.Label();
            translationsListView = new System.Windows.Forms.ListView();
            keyColumnHeader = new System.Windows.Forms.ColumnHeader();
            textColumnHeader = new System.Windows.Forms.ColumnHeader();
            copyrightLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)translationProvider1).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            //
            // translationProvider1
            //
            translationProvider1.ApplyRightToLeft = true;
            translationProvider1.ContainerControl = this;
            translationProvider1.ToolTip = toolTip1;
            //
            // menuStrip1
            //
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(584, 24);
            menuStrip1.TabIndex = 0;
            //
            // fileMenuItem
            //
            fileMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exitMenuItem });
            fileMenuItem.Name = "fileMenuItem";
            fileMenuItem.Size = new System.Drawing.Size(37, 20);
            fileMenuItem.Text = "File";
            translationProvider1.SetTranslationKey(fileMenuItem, "MenuFile");
            //
            // exitMenuItem
            //
            exitMenuItem.Name = "exitMenuItem";
            exitMenuItem.Size = new System.Drawing.Size(180, 22);
            exitMenuItem.Text = "Exit";
            translationProvider1.SetToolTipKey(exitMenuItem, "MenuExitHint");
            translationProvider1.SetTranslationKey(exitMenuItem, "MenuExit");
            exitMenuItem.Click += ExitMenuItem_Click;
            //
            // welcomeLabel
            //
            welcomeLabel.AutoSize = true;
            welcomeLabel.Font = new System.Drawing.Font("Segoe UI", 14F);
            welcomeLabel.Location = new System.Drawing.Point(12, 36);
            welcomeLabel.Name = "welcomeLabel";
            welcomeLabel.Size = new System.Drawing.Size(86, 25);
            welcomeLabel.TabIndex = 1;
            welcomeLabel.Text = "Welcome";
            translationProvider1.SetTranslationKey(welcomeLabel, "Welcome");
            //
            // helloLabel
            //
            helloLabel.AutoSize = true;
            helloLabel.Location = new System.Drawing.Point(12, 70);
            helloLabel.Name = "helloLabel";
            helloLabel.Size = new System.Drawing.Size(71, 15);
            helloLabel.TabIndex = 2;
            helloLabel.Text = "Hello world";
            translationProvider1.SetTranslationKey(helloLabel, "Hello");
            //
            // languageLabel
            //
            languageLabel.AutoSize = true;
            languageLabel.Location = new System.Drawing.Point(12, 104);
            languageLabel.Name = "languageLabel";
            languageLabel.Size = new System.Drawing.Size(62, 15);
            languageLabel.TabIndex = 3;
            languageLabel.Text = "Language:";
            translationProvider1.SetTranslationKey(languageLabel, "Language");
            //
            // languageComboBox
            //
            languageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            languageComboBox.FormattingEnabled = true;
            languageComboBox.Location = new System.Drawing.Point(140, 100);
            languageComboBox.Name = "languageComboBox";
            languageComboBox.Size = new System.Drawing.Size(300, 23);
            languageComboBox.TabIndex = 4;
            translationProvider1.SetToolTipKey(languageComboBox, "LanguageHint");
            languageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            //
            // nameLabel
            //
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(12, 138);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(67, 15);
            nameLabel.TabIndex = 5;
            nameLabel.Text = "Your name:";
            translationProvider1.SetTranslationKey(nameLabel, "YourName");
            //
            // nameTextBox
            //
            nameTextBox.Location = new System.Drawing.Point(140, 134);
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new System.Drawing.Size(300, 23);
            nameTextBox.TabIndex = 6;
            nameTextBox.Text = "World";
            nameTextBox.TextChanged += NameTextBox_TextChanged;
            //
            // helloNameLabel
            //
            helloNameLabel.AutoSize = true;
            helloNameLabel.Location = new System.Drawing.Point(12, 172);
            helloNameLabel.Name = "helloNameLabel";
            helloNameLabel.Size = new System.Drawing.Size(0, 15);
            helloNameLabel.TabIndex = 7;
            //
            // translationsListView
            //
            translationsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { keyColumnHeader, textColumnHeader });
            translationsListView.FullRowSelect = true;
            translationsListView.Location = new System.Drawing.Point(12, 200);
            translationsListView.Name = "translationsListView";
            translationsListView.Size = new System.Drawing.Size(560, 110);
            translationsListView.TabIndex = 8;
            translationsListView.UseCompatibleStateImageBehavior = false;
            translationsListView.View = System.Windows.Forms.View.Details;
            //
            // keyColumnHeader
            //
            keyColumnHeader.Text = "Key";
            keyColumnHeader.Width = 150;
            translationProvider1.SetTranslationKey(keyColumnHeader, "ColumnKey");
            //
            // textColumnHeader
            //
            textColumnHeader.Text = "Text";
            textColumnHeader.Width = 380;
            translationProvider1.SetTranslationKey(textColumnHeader, "ColumnText");
            //
            // copyrightLabel
            //
            copyrightLabel.AutoSize = true;
            copyrightLabel.Location = new System.Drawing.Point(12, 328);
            copyrightLabel.Name = "copyrightLabel";
            copyrightLabel.Size = new System.Drawing.Size(0, 15);
            copyrightLabel.TabIndex = 9;
            //
            // MainForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(584, 361);
            Controls.Add(copyrightLabel);
            Controls.Add(translationsListView);
            Controls.Add(helloNameLabel);
            Controls.Add(nameTextBox);
            Controls.Add(nameLabel);
            Controls.Add(languageComboBox);
            Controls.Add(languageLabel);
            Controls.Add(helloLabel);
            Controls.Add(welcomeLabel);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Tlumach Windows Forms Sample";
            translationProvider1.SetTranslationKey(this, "Welcome");
            ((System.ComponentModel.ISupportInitialize)translationProvider1).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Tlumach.WinForms.TranslationProvider translationProvider1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.Label welcomeLabel;
        private System.Windows.Forms.Label helloLabel;
        private System.Windows.Forms.Label languageLabel;
        private System.Windows.Forms.ComboBox languageComboBox;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label helloNameLabel;
        private System.Windows.Forms.ListView translationsListView;
        private System.Windows.Forms.ColumnHeader keyColumnHeader;
        private System.Windows.Forms.ColumnHeader textColumnHeader;
        private System.Windows.Forms.Label copyrightLabel;
    }
}
