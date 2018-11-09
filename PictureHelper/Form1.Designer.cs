namespace PictureHelper
{
    partial class Form1
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
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.fileListView = new System.Windows.Forms.ListView();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.buttonClearList = new System.Windows.Forms.Button();
            this.buttonExecCopyAndSort = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.Location = new System.Drawing.Point(12, 352);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxOutput.Size = new System.Drawing.Size(514, 86);
            this.textBoxOutput.TabIndex = 0;
            // 
            // fileListView
            // 
            this.fileListView.LargeImageList = this.imageList;
            this.fileListView.Location = new System.Drawing.Point(12, 12);
            this.fileListView.Name = "fileListView";
            this.fileListView.Size = new System.Drawing.Size(514, 324);
            this.fileListView.TabIndex = 1;
            this.fileListView.UseCompatibleStateImageBehavior = false;
            this.fileListView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.fileListView_KeyUp);
            // 
            // imageList
            // 
            this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth24Bit;
            this.imageList.ImageSize = new System.Drawing.Size(48, 48);
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // buttonClearList
            // 
            this.buttonClearList.Location = new System.Drawing.Point(12, 447);
            this.buttonClearList.Name = "buttonClearList";
            this.buttonClearList.Size = new System.Drawing.Size(75, 23);
            this.buttonClearList.TabIndex = 2;
            this.buttonClearList.Text = "Liste leeren";
            this.buttonClearList.UseVisualStyleBackColor = true;
            this.buttonClearList.Click += new System.EventHandler(this.buttonClearList_Click);
            // 
            // buttonExecCopyAndSort
            // 
            this.buttonExecCopyAndSort.Location = new System.Drawing.Point(429, 447);
            this.buttonExecCopyAndSort.Name = "buttonExecCopyAndSort";
            this.buttonExecCopyAndSort.Size = new System.Drawing.Size(97, 44);
            this.buttonExecCopyAndSort.TabIndex = 3;
            this.buttonExecCopyAndSort.Text = "Bilder kopieren und einsortieren";
            this.buttonExecCopyAndSort.UseVisualStyleBackColor = true;
            this.buttonExecCopyAndSort.Click += new System.EventHandler(this.buttonExecCopyAndSort_Click);
            // 
            // Form1
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(543, 501);
            this.Controls.Add(this.buttonExecCopyAndSort);
            this.Controls.Add(this.buttonClearList);
            this.Controls.Add(this.fileListView);
            this.Controls.Add(this.textBoxOutput);
            this.Name = "Form1";
            this.Text = "PictureHelper";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.ListView fileListView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.Button buttonClearList;
        private System.Windows.Forms.Button buttonExecCopyAndSort;
    }
}

