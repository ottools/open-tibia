#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License along
* with this program; if not, write to the Free Software Foundation, Inc.,
* 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
#endregion

#region Using Statements
using OpenTibia.Utils;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
#endregion

namespace OpenTibia.Controls
{
    [DefaultProperty("Color")]
    [DefaultEvent("ColorChanged")]
    public class HsiColorGrid : Control
    {
        #region Protected Properties
        
        protected int columns;
        protected int rows;
        protected int length;

        private Color lastRgbColor;
        private Color rgbColor;
        private int overColor;
        private int lastColor;
        private int color;
        private int cellSize;
        private int gap;
        private Pen pen;
        private SolidBrush brush;
        private Rectangle bounds;
        private ToolTip toolTip;

        #endregion

        #region Constructor

        public HsiColorGrid()
        {
            this.columns = 19;
            this.rows = 7;
            this.length = this.columns * this.rows;
            this.overColor = 0;
            this.lastColor = 0;
            this.color = 0;
            this.lastRgbColor = System.Drawing.Color.Black;
            this.rgbColor = System.Drawing.Color.Black;
            this.cellSize = 14;
            this.gap = 2;
            this.pen = new Pen(System.Drawing.Color.Transparent);
            this.brush = new SolidBrush(System.Drawing.Color.Transparent);
            this.bounds = new Rectangle();
            this.toolTip = new ToolTip();
        }

        #endregion

        #region Events
        
        public event ColorChangedHandler ColorChanged;

        #endregion

        #region Public Properties

        public Color RgbColor
        {
            get
            {
                return this.rgbColor;
            }
        }

        public int Color
        {
            get
            {
                return this.color;
            }

            set
            {
                if (value < 0 || value > this.length)
                {
                    value = 0;
                }

                if (this.color != value)
                {
                    this.lastColor = this.color;
                    this.lastRgbColor = this.rgbColor;
                    this.InvalidateColor(this.lastColor);

                    this.color = value;
                    this.rgbColor = this.GetRgbColor(this.color);
                    this.InvalidateColor(this.color);
                    this.UpdateToolTip();

                    if (this.ColorChanged != null)
                    {
                        this.ColorChanged(this, new ColorChangedArgs(this.lastRgbColor, this.rgbColor, this.lastColor, this.color));
                    }
                }
            }
        }

        #endregion

        #region Protected Methods
        
        protected int HitTest(Point point)
        {
            if (point.X > this.bounds.Left && point.X < this.bounds.Right && point.Y > this.bounds.Top && point.Y < this.bounds.Bottom)
            {
                int w = (this.cellSize + this.gap);
                int h = (this.cellSize + this.gap);
                int column = (int)Math.Floor((double)(point.X / w));
                int row = (int)Math.Floor((double)(point.Y / h));
                return row * this.columns + column;
            }

            return -1;
        }

        protected virtual void UpdateToolTip()
        {
            this.toolTip.SetToolTip(this, this.overColor != -1 ? this.overColor.ToString() : null);
        }

        protected virtual Color GetRgbColor(int color)
        {
            return ColorUtils.HsiToRgb(color);
        }

        protected void InvalidateOverColor(int color)
        {
            if (color != this.overColor)
            {
                this.InvalidateColor(this.overColor);
                this.overColor = color;
                this.UpdateToolTip();
                this.InvalidateColor(color);
            }
        }

        protected void InvalidateColor(int Color)
        {
            int row = (int)(Color / this.columns);
            int column = Color - (row * this.columns);
            int px = column * (this.cellSize + this.gap);
            int py = row * (this.cellSize + this.gap);
            Rectangle rect = new Rectangle(px, py, this.cellSize, this.cellSize);
            this.Invalidate(rect);
        }

        #endregion

        #region Event Handlers

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!this.Focused && this.TabStop)
            {
                this.Focus();
            }

            this.Color = this.HitTest(e.Location);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.InvalidateOverColor(this.HitTest(e.Location));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.InvalidateOverColor(-1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            bounds.Width = this.columns * (this.cellSize + this.gap);
            bounds.Height = this.rows * (this.cellSize + this.gap);

            int width = this.cellSize + this.gap;
            int height = this.cellSize + this.gap;
            int color = 0;

            for (int r = 0; r < this.rows; r++)
            {
                for (int c = 0; c < this.columns; c++)
                {
                    int x = c * width;
                    int y = r * height;

                    this.brush.Color = this.GetRgbColor(color);
                    e.Graphics.FillRectangle(this.brush, x, y, this.cellSize, this.cellSize);
                    e.Graphics.Save();

                    if (this.color == color)
                    {
                        this.pen.Color = System.Drawing.Color.Black;
                        e.Graphics.DrawRectangle(this.pen, x, y, this.cellSize - 1, this.cellSize - 1);
                        e.Graphics.DrawRectangle(this.pen, x + 1, y + 1, this.cellSize - 3, this.cellSize - 3);

                        this.pen.Color = System.Drawing.Color.White;
                        e.Graphics.DrawRectangle(this.pen, x + 2, y + 2, this.cellSize - 5, this.cellSize - 5);
                        e.Graphics.Save();
                    }
                    else if (this.overColor == color)
                    {
                        this.pen.Color = System.Drawing.Color.White;
                        e.Graphics.DrawRectangle(this.pen, x, y, this.cellSize - 1, this.cellSize - 1);
                        e.Graphics.Save();

                        this.pen.Color = System.Drawing.Color.Black;
                        e.Graphics.DrawRectangle(this.pen, x + 1, y + 1, this.cellSize - 3, this.cellSize - 3);
                        e.Graphics.Save();
                    }
                    else
                    {
                        this.pen.Color = System.Drawing.Color.Gray;
                        e.Graphics.DrawRectangle(this.pen, x, y, this.cellSize - 1, this.cellSize - 1);
                        e.Graphics.Save();
                    }

                    color++;
                }
            }
        }

        #endregion
    }
}
