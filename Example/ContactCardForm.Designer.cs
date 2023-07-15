namespace MSNPSharpClient
{
    partial class ContactCardForm
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
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlSpace = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.lblSpaceTitle = new System.Windows.Forms.LinkLabel();
            this.lblDisplayName = new System.Windows.Forms.Label();
            this.picDisplayImage = new System.Windows.Forms.PictureBox();
            this.pnlAlbum = new System.Windows.Forms.Panel();
            this.tlpnlAlbum = new System.Windows.Forms.TableLayoutPanel();
            this.lnkAlbumName = new System.Windows.Forms.LinkLabel();
            this.pnlBlog = new System.Windows.Forms.Panel();
            this.lnkBlogContent = new System.Windows.Forms.LinkLabel();
            this.lnkBlogTitle = new System.Windows.Forms.LinkLabel();
            this.pnlProfile = new System.Windows.Forms.Panel();
            this.ttips = new System.Windows.Forms.ToolTip(this.components);
            this.pnlSpace.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picDisplayImage)).BeginInit();
            this.pnlAlbum.SuspendLayout();
            this.pnlBlog.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlSpace
            // 
            this.pnlSpace.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.pnlSpace.Controls.Add(this.panel3);
            this.pnlSpace.Controls.Add(this.picDisplayImage);
            this.pnlSpace.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlSpace.Location = new System.Drawing.Point(0, 0);
            this.pnlSpace.Name = "pnlSpace";
            this.pnlSpace.Padding = new System.Windows.Forms.Padding(10);
            this.pnlSpace.Size = new System.Drawing.Size(272, 76);
            this.pnlSpace.TabIndex = 1;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.lblSpaceTitle);
            this.panel3.Controls.Add(this.lblDisplayName);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(69, 10);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(193, 56);
            this.panel3.TabIndex = 2;
            // 
            // lblSpaceTitle
            // 
            this.lblSpaceTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblSpaceTitle.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblSpaceTitle.Location = new System.Drawing.Point(0, 23);
            this.lblSpaceTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblSpaceTitle.Name = "lblSpaceTitle";
            this.lblSpaceTitle.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblSpaceTitle.Size = new System.Drawing.Size(193, 19);
            this.lblSpaceTitle.TabIndex = 2;
            this.lblSpaceTitle.TabStop = true;
            this.lblSpaceTitle.Text = "Space Title";
            this.lblSpaceTitle.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblSpaceTitle_LinkClicked);
            // 
            // lblDisplayName
            // 
            this.lblDisplayName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDisplayName.Location = new System.Drawing.Point(0, 0);
            this.lblDisplayName.Margin = new System.Windows.Forms.Padding(0);
            this.lblDisplayName.Name = "lblDisplayName";
            this.lblDisplayName.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.lblDisplayName.Size = new System.Drawing.Size(193, 23);
            this.lblDisplayName.TabIndex = 1;
            this.lblDisplayName.Text = "Display Name";
            // 
            // picDisplayImage
            // 
            this.picDisplayImage.BackColor = System.Drawing.Color.White;
            this.picDisplayImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picDisplayImage.Dock = System.Windows.Forms.DockStyle.Left;
            this.picDisplayImage.Location = new System.Drawing.Point(10, 10);
            this.picDisplayImage.Name = "picDisplayImage";
            this.picDisplayImage.Size = new System.Drawing.Size(59, 56);
            this.picDisplayImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picDisplayImage.TabIndex = 1;
            this.picDisplayImage.TabStop = false;
            // 
            // pnlAlbum
            // 
            this.pnlAlbum.BackColor = System.Drawing.Color.Linen;
            this.pnlAlbum.Controls.Add(this.tlpnlAlbum);
            this.pnlAlbum.Controls.Add(this.lnkAlbumName);
            this.pnlAlbum.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlAlbum.Location = new System.Drawing.Point(0, 76);
            this.pnlAlbum.Name = "pnlAlbum";
            this.pnlAlbum.Padding = new System.Windows.Forms.Padding(5);
            this.pnlAlbum.Size = new System.Drawing.Size(272, 65);
            this.pnlAlbum.TabIndex = 2;
            // 
            // tlpnlAlbum
            // 
            this.tlpnlAlbum.ColumnCount = 6;
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.tlpnlAlbum.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpnlAlbum.Location = new System.Drawing.Point(5, 17);
            this.tlpnlAlbum.Name = "tlpnlAlbum";
            this.tlpnlAlbum.RowCount = 1;
            this.tlpnlAlbum.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpnlAlbum.Size = new System.Drawing.Size(262, 43);
            this.tlpnlAlbum.TabIndex = 1;
            // 
            // lnkAlbumName
            // 
            this.lnkAlbumName.Dock = System.Windows.Forms.DockStyle.Top;
            this.lnkAlbumName.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkAlbumName.Location = new System.Drawing.Point(5, 5);
            this.lnkAlbumName.Name = "lnkAlbumName";
            this.lnkAlbumName.Size = new System.Drawing.Size(262, 12);
            this.lnkAlbumName.TabIndex = 0;
            this.lnkAlbumName.TabStop = true;
            this.lnkAlbumName.Text = "Album Name";
            this.lnkAlbumName.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkAlbumName_LinkClicked);
            // 
            // pnlBlog
            // 
            this.pnlBlog.BackColor = System.Drawing.Color.Linen;
            this.pnlBlog.Controls.Add(this.lnkBlogContent);
            this.pnlBlog.Controls.Add(this.lnkBlogTitle);
            this.pnlBlog.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBlog.Location = new System.Drawing.Point(0, 141);
            this.pnlBlog.Name = "pnlBlog";
            this.pnlBlog.Padding = new System.Windows.Forms.Padding(5);
            this.pnlBlog.Size = new System.Drawing.Size(272, 58);
            this.pnlBlog.TabIndex = 3;
            // 
            // lnkBlogContent
            // 
            this.lnkBlogContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lnkBlogContent.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkBlogContent.Location = new System.Drawing.Point(5, 17);
            this.lnkBlogContent.Name = "lnkBlogContent";
            this.lnkBlogContent.Size = new System.Drawing.Size(262, 36);
            this.lnkBlogContent.TabIndex = 1;
            this.lnkBlogContent.TabStop = true;
            this.lnkBlogContent.Text = "Blog Content";
            this.lnkBlogContent.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkBlogContent_LinkClicked);
            // 
            // lnkBlogTitle
            // 
            this.lnkBlogTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lnkBlogTitle.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lnkBlogTitle.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.lnkBlogTitle.Location = new System.Drawing.Point(5, 5);
            this.lnkBlogTitle.Name = "lnkBlogTitle";
            this.lnkBlogTitle.Size = new System.Drawing.Size(262, 12);
            this.lnkBlogTitle.TabIndex = 0;
            this.lnkBlogTitle.TabStop = true;
            this.lnkBlogTitle.Text = "Blog Title";
            this.lnkBlogTitle.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkBlogTitle_LinkClicked);
            // 
            // pnlProfile
            // 
            this.pnlProfile.BackColor = System.Drawing.Color.Linen;
            this.pnlProfile.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlProfile.Location = new System.Drawing.Point(0, 199);
            this.pnlProfile.Name = "pnlProfile";
            this.pnlProfile.Padding = new System.Windows.Forms.Padding(5);
            this.pnlProfile.Size = new System.Drawing.Size(272, 35);
            this.pnlProfile.TabIndex = 4;
            // 
            // ContactCardForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(272, 234);
            this.Controls.Add(this.pnlProfile);
            this.Controls.Add(this.pnlBlog);
            this.Controls.Add(this.pnlAlbum);
            this.Controls.Add(this.pnlSpace);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ContactCardForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ContactCard";
            this.Load += new System.EventHandler(this.ContactCardForm_Load);
            this.pnlSpace.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picDisplayImage)).EndInit();
            this.pnlAlbum.ResumeLayout(false);
            this.pnlBlog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlSpace;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.LinkLabel lblSpaceTitle;
        private System.Windows.Forms.Label lblDisplayName;
        private System.Windows.Forms.PictureBox picDisplayImage;
        private System.Windows.Forms.Panel pnlAlbum;
        private System.Windows.Forms.TableLayoutPanel tlpnlAlbum;
        private System.Windows.Forms.LinkLabel lnkAlbumName;
        private System.Windows.Forms.Panel pnlBlog;
        private System.Windows.Forms.Panel pnlProfile;
        private System.Windows.Forms.LinkLabel lnkBlogContent;
        private System.Windows.Forms.LinkLabel lnkBlogTitle;
        private System.Windows.Forms.ToolTip ttips;

    }
}