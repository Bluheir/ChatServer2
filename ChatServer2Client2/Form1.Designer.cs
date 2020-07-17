namespace ChatServer2Client2
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
			this.log = new System.Windows.Forms.RichTextBox();
			this.input = new System.Windows.Forms.TextBox();
			this.sendButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// log
			// 
			this.log.Location = new System.Drawing.Point(0, 0);
			this.log.Name = "log";
			this.log.ReadOnly = true;
			this.log.Size = new System.Drawing.Size(666, 416);
			this.log.TabIndex = 1;
			this.log.Text = "";
			// 
			// input
			// 
			this.input.Location = new System.Drawing.Point(0, 422);
			this.input.Name = "input";
			this.input.Size = new System.Drawing.Size(666, 31);
			this.input.TabIndex = 2;
			// 
			// sendButton
			// 
			this.sendButton.Location = new System.Drawing.Point(672, 422);
			this.sendButton.Name = "sendButton";
			this.sendButton.Size = new System.Drawing.Size(124, 34);
			this.sendButton.TabIndex = 3;
			this.sendButton.Text = "Send";
			this.sendButton.UseVisualStyleBackColor = true;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.sendButton);
			this.Controls.Add(this.input);
			this.Controls.Add(this.log);
			this.Name = "Form1";
			this.Text = "Form1";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox log;
		private System.Windows.Forms.TextBox input;
		private System.Windows.Forms.Button sendButton;
	}
}

