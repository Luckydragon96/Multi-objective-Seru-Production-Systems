namespace MOEM
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.MOEM = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // MOEM
            // 
            this.MOEM.Font = new System.Drawing.Font("Times New Roman", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MOEM.Location = new System.Drawing.Point(74, 40);
            this.MOEM.Name = "MOEM";
            this.MOEM.Size = new System.Drawing.Size(143, 51);
            this.MOEM.TabIndex = 0;
            this.MOEM.Text = "MOEM";
            this.MOEM.UseVisualStyleBackColor = true;
            this.MOEM.Click += new System.EventHandler(this.MOEM_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(316, 135);
            this.Controls.Add(this.MOEM);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button MOEM;
    }
}

