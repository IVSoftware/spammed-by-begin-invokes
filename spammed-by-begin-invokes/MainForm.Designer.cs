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
            recycler = new DataGridView();
            ((System.ComponentModel.ISupportInitialize)recycler).BeginInit();
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
            // recycler
            // 
            recycler.BackgroundColor = Color.Azure;
            recycler.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            recycler.Location = new Point(28, 112);
            recycler.Name = "recycler";
            recycler.RowHeadersWidth = 62;
            recycler.Size = new Size(422, 813);
            recycler.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 944);
            Controls.Add(recycler);
            Controls.Add(buttonUpdate);
            Name = "MainForm";
            Padding = new Padding(25);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Main Form";
            ((System.ComponentModel.ISupportInitialize)recycler).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonUpdate;
        private DataGridView recycler;
    }
}
