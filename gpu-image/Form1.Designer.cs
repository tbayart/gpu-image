namespace Images {
    partial class Form1 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.image = new System.Windows.Forms.Control();
            this.programBox = new System.Windows.Forms.TextBox();
            this.imageSurrogate = new System.Windows.Forms.TextBox();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.AutoScroll = true;
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(961, 497);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.Size = new System.Drawing.Size(961, 522);
            this.toolStripContainer1.TabIndex = 1;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            this.toolStripContainer1.TopToolStripPanel.Click += new System.EventHandler(this.toolStripContainer1_TopToolStripPanel_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(111, 25);
            this.toolStrip1.TabIndex = 0;
            // 
            // image
            // 
            this.image.Location = new System.Drawing.Point(0, 0);
            this.image.Name = "image";
            this.image.Size = new System.Drawing.Size(500, 331);
            this.image.TabIndex = 2;
            this.image.TabStop = false;
            // 
            // programBox
            // 
            this.programBox.Location = new System.Drawing.Point(0, 28);
            this.programBox.Multiline = true;
            this.programBox.Name = "programBox";
            this.programBox.Size = new System.Drawing.Size(204, 494);
            this.programBox.TabIndex = 1;
            this.programBox.DoubleClick += new System.EventHandler(this.programBox_DoubleClick);
            this.programBox.TextChanged += new System.EventHandler(this.programBox_TextChanged);
            this.programBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.programBox_KeyDown);
            this.programBox.Leave += new System.EventHandler(this.programBox_Leave);
            // 
            // imageSurrogate
            // 
            this.imageSurrogate.Location = new System.Drawing.Point(239, 383);
            this.imageSurrogate.Name = "imageSurrogate";
            this.imageSurrogate.Size = new System.Drawing.Size(99, 20);
            this.imageSurrogate.TabIndex = 3;
            this.imageSurrogate.Text = "surrogate";
            this.imageSurrogate.KeyDown += new System.Windows.Forms.KeyEventHandler(this.imageSurrogate_KeyDown);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(961, 522);
            this.Controls.Add(this.imageSurrogate);
            this.Controls.Add(this.programBox);
            this.Controls.Add(this.image);
            this.Controls.Add(this.toolStripContainer1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStripContainer toolStripContainer1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TextBox programBox;
        private System.Windows.Forms.TextBox imageSurrogate;
        private System.Windows.Forms.Control image;
    }
}

