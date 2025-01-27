namespace spammed_by_begin_invokes
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
            buttonUpdate = new Button();
            flowLayoutPanel = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // buttonUpdate
            // 
            buttonUpdate.Dock = DockStyle.Top;
            buttonUpdate.Font = new Font("Segoe UI", 16F);
            buttonUpdate.Location = new Point(25, 25);
            buttonUpdate.Name = "buttonUpdate";
            buttonUpdate.Size = new Size(428, 81);
            buttonUpdate.TabIndex = 0;
            buttonUpdate.Text = "Update 10000 Buttons";
            buttonUpdate.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanel.AutoScroll = true;
            flowLayoutPanel.Location = new Point(28, 121);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(422, 795);
            flowLayoutPanel.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 944);
            Controls.Add(flowLayoutPanel);
            Controls.Add(buttonUpdate);
            Name = "MainForm";
            Padding = new Padding(25);
            Text = "Main Form";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
        }

        #endregion

        private Button buttonUpdate;
        private FlowLayoutPanel flowLayoutPanel;
    }
}
