using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MSNPSharpClient
{
	using MSNPSharp;
	
    public class RtfRichTextBox : RichTextBox
    {
        [DllImport("gdiplus.dll")]
        private static extern uint GdipEmfToWmfBits(IntPtr _hEmf, uint _bufferSize, byte[] _buffer, int _mappingMode, EmfToWmfBitsFlags _flags);
        
        #region .cctor
        static bool hasGdiPlus = false;
        static RtfRichTextBox()
        {
            try
            {
				if(!Settings.IsMono)
				{
					//We are in M$ Windows!
	                GdipEmfToWmfBits(IntPtr.Zero, 0, null, 0, 0);
	                hasGdiPlus = true;
				}
            }
            catch (Exception)
            {
            }
        }
        #endregion


        private float xDpi;
        private float yDpi;

        private RtfColor textColor = RtfColor.Black;
        private RtfColor highlightColor = RtfColor.White;
        private Dictionary<string, Bitmap> emotions = new Dictionary<string, Bitmap>();
        private Dictionary<RtfColor, string> rtfColor = new Dictionary<RtfColor, string>();
        private Dictionary<string, string> rtfFontFamily = new Dictionary<string, string>();

        public RtfRichTextBox()
        {
            rtfColor.Add(RtfColor.Aqua, @"\red0\green255\blue255");
            rtfColor.Add(RtfColor.Black, @"\red0\green0\blue0");
            rtfColor.Add(RtfColor.Blue, @"\red0\green0\blue255");
            rtfColor.Add(RtfColor.Fuchsia, @"\red255\green0\blue255");
            rtfColor.Add(RtfColor.Gray, @"\red128\green128\blue128");
            rtfColor.Add(RtfColor.Green, @"\red0\green128\blue0");
            rtfColor.Add(RtfColor.Lime, @"\red0\green255\blue0");
            rtfColor.Add(RtfColor.Maroon, @"\red128\green0\blue0");
            rtfColor.Add(RtfColor.Navy, @"\red0\green0\blue128");
            rtfColor.Add(RtfColor.Olive, @"\red128\green128\blue0");
            rtfColor.Add(RtfColor.Purple, @"\red128\green0\blue128");
            rtfColor.Add(RtfColor.Red, @"\red255\green0\blue0");
            rtfColor.Add(RtfColor.Silver, @"\red192\green192\blue192");
            rtfColor.Add(RtfColor.Teal, @"\red0\green128\blue128");
            rtfColor.Add(RtfColor.White, @"\red255\green255\blue255");
            rtfColor.Add(RtfColor.Yellow, @"\red255\green255\blue0");

            rtfFontFamily.Add(FontFamily.GenericMonospace.Name, @"\fmodern");
            rtfFontFamily.Add(FontFamily.GenericSansSerif.Name, @"\fswiss");
            rtfFontFamily.Add(FontFamily.GenericSerif.Name, @"\froman");
            rtfFontFamily.Add("UNKNOWN", @"\fnil");

            using (Graphics graphics = CreateGraphics())
            {
                xDpi = graphics.DpiX;
                yDpi = graphics.DpiY;
            }
        }

        public RtfRichTextBox(RtfColor _textColor)
            : this()
        {
            textColor = _textColor;
        }

        public RtfRichTextBox(RtfColor _textColor, RtfColor _highlightColor)
            : this()
        {
            textColor = _textColor;
            highlightColor = _highlightColor;
        }

        public void AppendRtf(string _rtf)
        {
            Select(TextLength, 0);
            SelectionColor = Color.Black;
            SelectedRtf = _rtf;
        }

        public void AppendTextAsRtf(string _text)
        {
            AppendTextAsRtf(_text, Font);
        }

        public void AppendTextAsRtf(string _text, Font _font)
        {
            AppendTextAsRtf(_text, _font, textColor);
        }

        public void AppendTextAsRtf(string _text, Font _font, RtfColor _textColor)
        {
            AppendTextAsRtf(_text, _font, _textColor, highlightColor);
        }

        public void AppendTextAsRtf(string _text, Font _font, RtfColor _textColor, RtfColor _backColor)
        {
            Select(TextLength, 0);
            InsertTextAsRtf(_text, _font, _textColor, _backColor);
        }

        private string GetColorTable(RtfColor _textColor, RtfColor _backColor)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(@"{\colortbl ;");
            builder.Append(rtfColor[_textColor]);
            builder.Append(";");
            builder.Append(rtfColor[_backColor]);
            builder.Append(@";}\n");
            return builder.ToString();
        }

        private string GetDocumentArea(string _text, Font _font)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(@"\viewkind4\uc1\pard\cf1\f0\fs20");
            builder.Append(@"\highlight2");
            if (_font.Bold)
            {
                builder.Append(@"\b");
            }
            if (_font.Italic)
            {
                builder.Append(@"\i");
            }
            if (_font.Strikeout)
            {
                builder.Append(@"\strike");
            }
            if (_font.Underline)
            {
                builder.Append(@"\ul");
            }
            builder.Append(@"\f0");
            builder.Append(@"\fs");
            builder.Append((int)Math.Round((double)(2f * _font.SizeInPoints)));
            builder.Append(" ");
            builder.Append(_text.Replace("\n", @"\par "));
            builder.Append(@"\highlight0");
            if (_font.Bold)
            {
                builder.Append(@"\b0");
            }
            if (_font.Italic)
            {
                builder.Append(@"\i0");
            }
            if (_font.Strikeout)
            {
                builder.Append(@"\strike0");
            }
            if (_font.Underline)
            {
                builder.Append(@"\ulnone");
            }
            builder.Append(@"\f0");
            builder.Append(@"\fs20");
            builder.Append(@"\cf0\fs17}");
            return builder.ToString();
        }

        private string GetFontTable(Font _font)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(@"{\fonttbl{\f0");
            builder.Append(@"\");
            if (rtfFontFamily.ContainsKey(_font.FontFamily.Name))
            {
                builder.Append(rtfFontFamily[_font.FontFamily.Name]);
            }
            else
            {
                builder.Append(rtfFontFamily["UNKNOWN"]);
            }
            builder.Append(@"\fcharset0 ");
            builder.Append(_font.Name);
            builder.Append(";}}");
            return builder.ToString();
        }

        private string GetImagePrefix(Image _image)
        {
            StringBuilder builder = new StringBuilder();
            int picw = (int)Math.Round((double)((((float)_image.Width) / xDpi) * 2540f));
            int pich = (int)Math.Round((double)((((float)_image.Height) / yDpi) * 2540f));
            int picwgoal = (int)Math.Round((double)((((float)_image.Width) / xDpi) * 1440f));
            int pichgoal = (int)Math.Round((double)((((float)_image.Height) / yDpi) * 1440f));
            builder.Append(@"{\pict\wmetafile8");
            builder.Append(@"\picw");
            builder.Append(picw);
            builder.Append(@"\pich");
            builder.Append(pich);
            builder.Append(@"\picwgoal");
            builder.Append(picwgoal);
            builder.Append(@"\pichgoal");
            builder.Append(pichgoal);
            builder.Append(" ");
            return builder.ToString();
        }

        private string GetRtfImage(Image _image)
        {
            MemoryStream stream = null;
            Graphics graphics = null;
            Metafile image = null;
            string ret;
            try
            {
                stream = new MemoryStream();
                using (graphics = CreateGraphics())
                {
                    IntPtr hdc = graphics.GetHdc();
                    image = new Metafile(stream, hdc);
                    graphics.ReleaseHdc(hdc);
                }
                using (graphics = Graphics.FromImage(image))
                {
                    graphics.DrawImage(_image, new Rectangle(0, 0, _image.Width, _image.Height));
                }
                IntPtr henhmetafile = image.GetHenhmetafile();
                uint num = GdipEmfToWmfBits(henhmetafile, 0, null, 1, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);
                byte[] buffer = new byte[num];
                GdipEmfToWmfBits(henhmetafile, num, buffer, 1, EmfToWmfBitsFlags.EmfToWmfBitsFlagsDefault);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < buffer.Length; i++)
                {
                    builder.Append(string.Format("{0:X2}", buffer[i]));
                }
                ret = builder.ToString();
            }
            finally
            {
                if (graphics != null)
                {
                    graphics.Dispose();
                }
                if (image != null)
                {
                    image.Dispose();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            return ret;
        }

        public void InsertEmotion()
        {
            if (hasGdiPlus)
            {
                foreach (string emoticon in emotions.Keys)
                {
                    int start = Find(emoticon, RichTextBoxFinds.None);
                    if (start > -1)
                    {
                        Select(start, emoticon.Length);
                        InsertImage(emotions[emoticon]);
                    }
                }
            }
        }

        public void InsertImage(Image _image)
        {
            if (hasGdiPlus)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
                builder.Append(GetFontTable(Font));
                builder.Append(GetImagePrefix(_image));
                builder.Append(GetRtfImage(_image));
                builder.Append(@"}");

                SelectedRtf = builder.ToString();
            }
        }

        public void InsertRtf(string _rtf)
        {
            SelectedRtf = _rtf;
        }

        public void InsertTextAsRtf(string _text)
        {
            InsertTextAsRtf(_text, Font);
        }

        public void InsertTextAsRtf(string _text, Font _font)
        {
            InsertTextAsRtf(_text, _font, textColor);
        }

        public void InsertTextAsRtf(string _text, Font _font, RtfColor _textColor)
        {
            InsertTextAsRtf(_text, _font, _textColor, highlightColor);
        }

        public void InsertTextAsRtf(string _text, Font _font, RtfColor _textColor, RtfColor _backColor)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033");
            builder.Append(GetFontTable(_font));
            builder.Append(GetColorTable(_textColor, _backColor));
            builder.Append(GetDocumentArea(_text, _font));

            SelectedRtf = builder.ToString();
        }

        private string RemoveBadChars(string _originalRtf)
        {
            return _originalRtf.Replace("\0", "");
        }

        public Dictionary<string, Bitmap> Emotions
        {
            get
            {
                return emotions;
            }
        }

        public bool HasEmotion
        {
            get
            {
                if (hasGdiPlus)
                {
                    foreach (string emoticon in emotions.Keys)
                    {
                        if (Text.IndexOf(emoticon, StringComparison.CurrentCultureIgnoreCase) > -1)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public RtfColor HiglightColor
        {
            get
            {
                return highlightColor;
            }
            set
            {
                highlightColor = value;
            }
        }

        public new string Rtf
        {
            get
            {
                return RemoveBadChars(base.Rtf);
            }
            set
            {
                base.Rtf = value;
            }
        }

        public RtfColor TextColor
        {
            get
            {
                return textColor;
            }
            set
            {
                textColor = value;
            }
        }

        [Flags]
        private enum EmfToWmfBitsFlags
        {
            EmfToWmfBitsFlagsDefault = 0,
            EmfToWmfBitsFlagsEmbedEmf = 1,
            EmfToWmfBitsFlagsIncludePlaceable = 2,
            EmfToWmfBitsFlagsNoXORClip = 4
        }

        public enum RtfColor
        {
            Black,
            Maroon,
            Green,
            Olive,
            Navy,
            Purple,
            Teal,
            Gray,
            Silver,
            Red,
            Lime,
            Yellow,
            Blue,
            Fuchsia,
            Aqua,
            White
        }
    }
};
