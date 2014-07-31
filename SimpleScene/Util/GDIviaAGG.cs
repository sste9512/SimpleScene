﻿// Copyright(C) David W. Jeske, 2013
// Released to the public domain. Use, modify and relicense at will.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.RasterizerScanline;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Font;

namespace UG
{

	public struct Bitmap {
		internal ImageBuffer buffer;

		/// <summary>
		/// Creates a bitmap with 32bit A-rgb (BGRA) PixelFormat
		/// </summary>
		/// <param name="width">Width.</param>
		/// <param name="height">Height.</param>
		public Bitmap (int width, int height) {
			this.buffer = new ImageBuffer(width,height,32, new BlenderBGRA());
		}

		public System.Drawing.Size Size {
			get { return new System.Drawing.Size (buffer.Width, buffer.Height); }
		}

		public Bitmap (int width, int height, PixelFormat format)
		{
			IRecieveBlenderByte byteBlender;
			int bpp;

			switch (format) {
			case PixelFormat.Format32bppArgb: 
				bpp = 32;
				byteBlender = new BlenderBGRA();
				break;
			case PixelFormat.Format32bppRgb:
				bpp = 32;
				byteBlender = new BlenderBGR();
				break;
			case PixelFormat.Format24bppRgb:
				bpp = 24;
				byteBlender = new BlenderBGR();
				break;
			default:
				throw new NotImplementedException(String.Format("UG.Bitmap unsupported format {0}",format));
			}
			this.buffer = new ImageBuffer(width,height,bpp,byteBlender);
		}

		public static implicit operator ImageBuffer(Bitmap bitmap) {
			return bitmap.buffer;
		}
	} // struct Bitmap

	// -------------------------------[  GDI Graphics work-alike  ] ------------------------------------

	public class GraphicsState {
		Graphics context;
		IVertexSource pipeTail;
		RectangleDouble clipRect;
		Affine transform;

		internal GraphicsState (Graphics context) {
			this.context = context;
			this.pipeTail = context.pipeTail;
			this.clipRect = context.aggGc.GetClippingRect();
			this.transform = context.aggGc.GetTransform();
		}
		internal void Restore() {
			context.pipeTail = this.pipeTail;
			context.aggGc.SetClippingRect(this.clipRect);
			context.aggGc.PushTransform();
			context.aggGc.SetTransform(this.transform);
		}
	}

	public class DeferredVertexSource : IVertexSource {
		public IVertexSource source;
		private void checkSource() {
			if (source == null) {
				throw new Exception("DeferredVertexSource: no source attached");
			}
		}

		public IEnumerable<VertexData> Vertices ()
		{
			checkSource();
			return source.Vertices();
		}
		public void rewind (int pathId)
		{
			checkSource();
			source.rewind(pathId);
		}
		public ShapePath.FlagsAndCommand vertex (out double x, out double y)
		{
			checkSource();
			return source.vertex(out x, out y);
		}

		public DeferredVertexSource () { }
	}

	public class Graphics {
		public Graphics2D aggGc;
		public System.Drawing.Drawing2D.SmoothingMode SmoothingMode;
		public System.Drawing.Text.TextRenderingHint TextRenderingHint;
		internal DeferredVertexSource pipeInput;
		internal IVertexSource pipeTail;

		Stack<GraphicsState> restoreStack = new Stack<GraphicsState>();

		private double DegreesToRadians (float angleDeg)
		{
			return (angleDeg / 180.0 * Math.PI);
		}

		public Graphics (Graphics2D aggGc) {
			this.aggGc = aggGc;

			// this makes whole-numbers fall in the middle of pixels...
			// TODO: fix the AGG coordinates so this isn't necessary?
			this.aggGc.PushTransform();
			this.aggGc.SetTransform(Affine.NewTranslation(0.5,0.5));

			this.pipeInput = new DeferredVertexSource();
			this.pipeTail = this.pipeInput;
		}

		public static Graphics FromImage (Bitmap image) {
			return new Graphics(image.buffer.NewGraphics2D());
		}

		public static Graphics FromImage (ImageBuffer image) {
			return new Graphics(image.NewGraphics2D());
		}

		public void Flush () { }

		public void Clear (System.Drawing.Color color) {			
			aggGc.Clear(new RGBA_Bytes((uint)color.ToArgb()));
		}		

		public void DrawString (string text, Font font, Brush brush, PointF curPoint, StringFormat fmt) {
			this.DrawString(text,font,brush,curPoint.X,curPoint.Y);
		}

		public void DrawString (string text, Font font, Brush brush, float x, float y)
		{
			// TODO: handle different brushes
			// TODO: emulate GDI "bordering" of text?
			SolidBrush colorBrush = brush as SolidBrush;
			var s1 = new TypeFacePrinter (text, font.SizeInPoints, new MatterHackers.VectorMath.Vector2 (0, 0), Justification.Left, Baseline.BoundsTop);	
			var s2 = new VertexSourceApplyTransform (s1, Affine.NewScaling (1, -1));
			if (x != 0.0f || y != 0.0f) {
				s2 = new VertexSourceApplyTransform (s2, Affine.NewTranslation (x, y));
			}

			pipeInput.source = s2;
			aggGc.Render(pipeTail,new MatterHackers.Agg.RGBA_Bytes((uint)colorBrush.Color.ToArgb()));
		}		

		// TODO: adjust measure string to handle "StringFormat.GenericTypographic" differently than normal padding
		public SizeF MeasureString(string text, Font font) {
			// TODO: teach agg-sharp to render windows fonts
			var tfp = new TypeFacePrinter(text, font.SizeInPoints);
			var bounds = tfp.LocalBounds;
			return new SizeF((float)bounds.Width,(float)bounds.Height);
		}

		public SizeF MeasureString (string text, Font font, PointF origin, StringFormat format) {
			var tfp = new TypeFacePrinter(text, font.SizeInPoints);
			var bounds = tfp.LocalBounds;
			return new SizeF((float)bounds.Width,(float)bounds.Height);
		}

		public void FillRectangle (Brush brush, float x, float y, float width, float height) {
			SolidBrush solidBrush = brush as SolidBrush;

			var path = new PathStorage();
			path.MoveTo(x,y);
			path.LineTo(x,y+height);
			path.LineTo(x+width,y+height);
			path.LineTo(x+width,y);
			path.LineTo(x,y);			

			pipeInput.source = path;
			aggGc.Render ( pipeTail, new RGBA_Bytes((uint)solidBrush.Color.ToArgb()) );
		}


		public void DrawRectangle (Pen pen, float x, float y, float width, float height) {
			var path = new PathStorage();
			path.MoveTo(x,y);
			path.LineTo(x,y+height);
			path.LineTo(x+width,y+height);
			path.LineTo(x+width,y);
			path.LineTo(x,y);			
			var stroke = new Stroke(path, (double)pen.Width);

			pipeInput.source = stroke;
			aggGc.Render ( pipeTail, new RGBA_Bytes((uint)pen.Color.ToArgb()) );
		}
	
		public void DrawArc (Pen pen, System.Drawing.Rectangle rect, float startAngleDeg, float endAngleDeg) {
			
		}

		public void DrawLine (Pen pen, int x1, int y1, int x2, int y2) {
			var path = new PathStorage();
			path.MoveTo(x1,y1);
			path.LineTo(x2,y2);
			var stroke = new Stroke(path, (double)pen.Width);

			pipeInput.source = stroke;
			aggGc.Render ( pipeTail, new RGBA_Bytes((uint)pen.Color.ToArgb()) );
		}

		public void SetClip(System.Drawing.Drawing2D.GraphicsPath path, System.Drawing.Drawing2D.CombineMode mode) {
			// TODO: implement clip paths somehow...

			// bitmap masking?
			// http://www.antigrain.com/demo/alpha_mask.cpp.html	
		}


		public void SetClip(System.Drawing.Rectangle rect) {
			// this is just a simple rectangular clip

			// make sure it doesn't go outside the bounds of the image itself..
			var bounds = aggGc.DestImage.GetBounds();

			aggGc.SetClippingRect(
				new RectangleDouble(
					Math.Min(rect.Left,bounds.Width),
					Math.Min(rect.Bottom,bounds.Height),
					Math.Min(rect.Right,bounds.Width),
					Math.Min(rect.Top,bounds.Height)));
		}

		public void RotateTransform (float angleDeg)
		{
			aggGc.PushTransform();
			aggGc.SetTransform( Affine.NewRotation(DegreesToRadians(angleDeg)) * aggGc.GetTransform() );			
		}
                            

		public void TranslateTransform (double x, double y) {
			aggGc.PushTransform();
			aggGc.SetTransform( Affine.NewTranslation(x,y) * aggGc.GetTransform());			
		}

        public void FillPie (Brush brush, System.Drawing.Rectangle rect, float startAngleDeg, float endAngleDeg)
		{
			SolidBrush solidBrush = brush as SolidBrush;
			// throw new NotImplementedException();
			double originX = (rect.Left + rect.Right) / 2.0;
			double originY = (rect.Top + rect.Bottom) / 2.0;
			double radiusX = (rect.Height) / 2.0;
			double radiusY = (rect.Width) / 2.0;
			
			var arc = new arc(originX,originY,radiusX,radiusY,DegreesToRadians(startAngleDeg),DegreesToRadians(endAngleDeg));
			pipeInput.source = arc;
			aggGc.Render ( pipeTail, new RGBA_Bytes((uint)solidBrush.Color.ToArgb()) );
		}


		public GraphicsState Save () {
			var currentState = new GraphicsState(this);
			restoreStack.Push(currentState);
			return currentState;
		}
	
		public void Restore (GraphicsState state)
		{
			var restoreState = restoreStack.Pop();
			if (state != restoreState) {
				throw new Exception("UG.Graphics Restore state match failure");
			}
			
			restoreState.Restore();
		}

	} // class Graphics
}

