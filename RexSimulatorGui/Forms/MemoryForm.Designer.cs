namespace RexSimulatorGui.Forms
{
    partial class MemoryForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.memoryListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.goToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pcToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.raToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.evecToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.earToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rbaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ptableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // memoryListView
            // 
            this.memoryListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.memoryListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memoryListView.FullRowSelect = true;
            this.memoryListView.Location = new System.Drawing.Point(0, 0);
            this.memoryListView.Name = "memoryListView";
            this.memoryListView.Size = new System.Drawing.Size(547, 511);
            this.memoryListView.TabIndex = 0;
            this.memoryListView.UseCompatibleStateImageBehavior = false;
            this.memoryListView.View = System.Windows.Forms.View.Details;
            this.memoryListView.VirtualMode = true;
            this.memoryListView.RetrieveVirtualItem += new System.Windows.Forms.RetrieveVirtualItemEventHandler(this.memoryListView_RetrieveVirtualItem);
            this.memoryListView.DoubleClick += new System.EventHandler(this.memoryListView_DoubleClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Pointer";
            this.columnHeader1.Width = 80;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Address";
            this.columnHeader2.Width = 80;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Value";
            this.columnHeader3.Width = 80;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Disassembly";
            this.columnHeader4.Width = 250;
            // 
            // updateTimer
            // 
            this.updateTimer.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.goToToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(547, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.Visible = false;
            // 
            // goToToolStripMenuItem
            // 
            this.goToToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.pcToolStripMenuItem,
            this.spToolStripMenuItem,
            this.raToolStripMenuItem,
            this.evecToolStripMenuItem,
            this.earToolStripMenuItem,
            this.rbaseToolStripMenuItem,
            this.ptableToolStripMenuItem});
            this.goToToolStripMenuItem.Name = "goToToolStripMenuItem";
            this.goToToolStripMenuItem.Size = new System.Drawing.Size(50, 20);
            this.goToToolStripMenuItem.Text = "Go To";
            // 
            // pcToolStripMenuItem
            // 
            this.pcToolStripMenuItem.Name = "pcToolStripMenuItem";
            this.pcToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.pcToolStripMenuItem.Text = "$pc";
            this.pcToolStripMenuItem.Click += new System.EventHandler(this.pcToolStripMenuItem_Click);
            // 
            // spToolStripMenuItem
            // 
            this.spToolStripMenuItem.Name = "spToolStripMenuItem";
            this.spToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.spToolStripMenuItem.Text = "$sp";
            this.spToolStripMenuItem.Click += new System.EventHandler(this.spToolStripMenuItem_Click);
            // 
            // raToolStripMenuItem
            // 
            this.raToolStripMenuItem.Name = "raToolStripMenuItem";
            this.raToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.raToolStripMenuItem.Text = "$ra";
            this.raToolStripMenuItem.Click += new System.EventHandler(this.raToolStripMenuItem_Click);
            // 
            // evecToolStripMenuItem
            // 
            this.evecToolStripMenuItem.Name = "evecToolStripMenuItem";
            this.evecToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.evecToolStripMenuItem.Text = "$evec";
            this.evecToolStripMenuItem.Click += new System.EventHandler(this.evecToolStripMenuItem_Click);
            // 
            // earToolStripMenuItem
            // 
            this.earToolStripMenuItem.Name = "earToolStripMenuItem";
            this.earToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.earToolStripMenuItem.Text = "$ear";
            this.earToolStripMenuItem.Click += new System.EventHandler(this.earToolStripMenuItem_Click);
            // 
            // rbaseToolStripMenuItem
            // 
            this.rbaseToolStripMenuItem.Name = "rbaseToolStripMenuItem";
            this.rbaseToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.rbaseToolStripMenuItem.Text = "$rbase";
            this.rbaseToolStripMenuItem.Click += new System.EventHandler(this.rbaseToolStripMenuItem_Click);
            // 
            // ptableToolStripMenuItem
            // 
            this.ptableToolStripMenuItem.Name = "ptableToolStripMenuItem";
            this.ptableToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.ptableToolStripMenuItem.Text = "$ptable";
            this.ptableToolStripMenuItem.Click += new System.EventHandler(this.ptableToolStripMenuItem_Click);
            // 
            // MemoryForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(547, 511);
            this.Controls.Add(this.memoryListView);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MemoryForm";
            this.Text = "MemoryForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MemoryForm_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView memoryListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem goToToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pcToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem spToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem raToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem evecToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem earToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rbaseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ptableToolStripMenuItem;
    }
}