using System;
using System.IO;
using System.Text;
using PdfSharp.Drawing;

namespace DocxToPdf.Rendering {
	public static class EmfRasterizer {
		public static bool RenderEmf(byte[] emfBytes, XGraphics gfx, double x, double y, double width, double height) {
			if (emfBytes == null || emfBytes.Length < 80) return false;

			try {
				using MemoryStream ms = new MemoryStream(emfBytes);
				using BinaryReader reader = new BinaryReader(ms);

				// 1. Read Header
				uint type = reader.ReadUInt32(); // 1 = EMR_HEADER
				uint size = reader.ReadUInt32();
				if (type != 1) return false;

				// Read bounds from header (rclBounds: left, top, right, bottom in px)
				int boundsLeft = reader.ReadInt32();
				int boundsTop = reader.ReadInt32();
				int boundsRight = reader.ReadInt32();
				int boundsBottom = reader.ReadInt32();

				double emfW = boundsRight - boundsLeft;
				double emfH = boundsBottom - boundsTop;
				if (emfW <= 0) emfW = width;
				if (emfH <= 0) emfH = height;

				double scaleX = width / emfW;
				double scaleY = height / emfH;

				ms.Position = size; // Skip header

				// GDI Graphics state tracking
				XColor currentTextColor = XColors.White;
				XColor currentFillColor = XColors.Black;
				XFont currentFont = TextMeasurer.CreateFont("Arial", 9.5, true, false);
				bool hasDrawnAnything = false;

				while (ms.Position < ms.Length - 8) {
					long recStart = ms.Position;
					uint recType = reader.ReadUInt32();
					uint recSize = reader.ReadUInt32();
					if (recSize < 8) break;

					long nextRec = recStart + recSize;

					switch (recType) {
						case 24: // EMR_SETTEXTCOLOR
							byte tr = reader.ReadByte();
							byte tg = reader.ReadByte();
							byte tb = reader.ReadByte();
							reader.ReadByte();
							currentTextColor = XColor.FromArgb(255, tr, tg, tb);
							break;

						case 39: // EMR_CREATEBRUSHINDIRECT
							reader.ReadUInt32(); // ihBrush
							uint style = reader.ReadUInt32();
							byte brR = reader.ReadByte();
							byte brG = reader.ReadByte();
							byte brB = reader.ReadByte();
							currentFillColor = XColor.FromArgb(255, brR, brG, brB);
							break;

						case 43: // EMR_RECTANGLE
							int rcLeft = reader.ReadInt32();
							int rcTop = reader.ReadInt32();
							int rcRight = reader.ReadInt32();
							int rcBottom = reader.ReadInt32();
							double rx = x + (rcLeft - boundsLeft) * scaleX;
							double ry = y + (rcTop - boundsTop) * scaleY;
							double rw = (rcRight - rcLeft) * scaleX;
							double rh = (rcBottom - rcTop) * scaleY;
							if (currentFillColor != XColors.Transparent) {
								gfx.DrawRectangle(new XSolidBrush(currentFillColor), rx, ry, rw, rh);
								hasDrawnAnything = true;
							}
							break;

						case 84: // EMR_EXTTEXTOUTW
							// Bounds
							reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt32();
							reader.ReadUInt32(); // iDotMode
							reader.ReadSingle(); // exScale
							reader.ReadSingle(); // eyScale
							int ptx = reader.ReadInt32();
							int pty = reader.ReadInt32();
							uint nChars = reader.ReadUInt32();
							uint offString = reader.ReadUInt32();

							if (offString > 0 && recStart + offString + nChars * 2 <= ms.Length) {
								ms.Position = recStart + offString;
								byte[] strBytes = reader.ReadBytes((int)nChars * 2);
								string text = Encoding.Unicode.GetString(strBytes);

								double tx = x + (ptx - boundsLeft) * scaleX;
								double ty = y + (pty - boundsTop) * scaleY;
								gfx.DrawString(text, currentFont, new XSolidBrush(currentTextColor), tx, ty + 12.0);
								hasDrawnAnything = true;
							}
							break;

						case 14: // EMR_EOF
							return hasDrawnAnything;
					}

					if (nextRec <= ms.Length) {
						ms.Position = nextRec;
					} else {
						break;
					}
				}

				return hasDrawnAnything;
			} catch {
				return false;
			}
		}
	}
}
