#region Licence
/**
* Copyright (C) 2015 Open Tibia Tools <https://github.com/ottools/open-tibia>
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
#endregion

#region Using Statements
using OpenTibia.Client;
using OpenTibia.Client.Things;
using System.Drawing;
using System.Windows.Forms;
#endregion

namespace OpenTibia.Controls
{
    public class ThingTypeListBox : ListBox
    {
        #region Private Properties

        private const int ItemMargin = 5;

        private Rectangle layoutRect;
        private Rectangle destRect;
        private Rectangle sourceRect;
        private Pen pen;

        #endregion

        #region Constructor

        public ThingTypeListBox()
        {
            this.layoutRect = new Rectangle();
            this.destRect = new Rectangle(ItemMargin, 0, 32, 32);
            this.sourceRect = new Rectangle();
            this.pen = new Pen(Color.Transparent);
            this.MeasureItem += new MeasureItemEventHandler(this.MeasureItemHandler);
            this.DrawItem += new DrawItemEventHandler(this.DrawItemHandler);
            this.DrawMode = DrawMode.OwnerDrawVariable;
        }

        #endregion

        #region Public Properties

        public IClient Client { get; set; }

        #endregion

        #region Public Methods

        public void Add(ThingType thing)
        {
            Items.Add(thing);
        }

        public void AddRange(ThingType[] things)
        {
            Items.AddRange(things);
        }

        public void RemoveSelectedThings()
        {
            if (this.SelectedIndex != -1)
            {
                SelectedObjectCollection selectedItems = this.SelectedItems;

                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    this.Items.Remove(selectedItems[i]);
                }
            }
        }

        public void Clear()
        {
            Items.Clear();
        }

        #endregion

        #region Event Handlers

        private void MeasureItemHandler(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)(32 + (2 * ItemMargin));
        }

        private void DrawItemHandler(object sender, DrawItemEventArgs ev)
        {
            Rectangle bounds = ev.Bounds;
            bounds.Width--;

            // draw background
            ev.DrawBackground();

            // draw border
            ev.Graphics.DrawRectangle(Pens.Gray, bounds);

            if (this.Client != null && ev.Index != -1)
            {
                ThingType thing = (ThingType)this.Items[ev.Index];

                // find the area in which to put the text and draw.
                this.layoutRect.X = bounds.Left + 32 + (3 * ItemMargin);
                this.layoutRect.Y = bounds.Top + (ItemMargin * 2);
                this.layoutRect.Width = bounds.Right - ItemMargin - this.layoutRect.X;
                this.layoutRect.Height = bounds.Bottom - ItemMargin - this.layoutRect.Y;

                // draw thing id end name
                if ((ev.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    this.pen.Brush = WhiteBrush;
                    ev.Graphics.DrawString(thing.ToString(), this.Font, WhiteBrush, this.layoutRect);
                }
                else
                {
                    this.pen.Brush = BlackBrush;
                    ev.Graphics.DrawString(thing.ToString(), this.Font, BlackBrush, this.layoutRect);
                }

                this.destRect.Y = bounds.Top + ItemMargin;

                Bitmap bitmap = this.Client.GetObjectImage(thing);
                if (bitmap != null)
                {
                    this.sourceRect.Width = bitmap.Width;
                    this.sourceRect.Height = bitmap.Height;
                    ev.Graphics.DrawImage(bitmap, this.destRect, this.sourceRect, GraphicsUnit.Pixel);
                }
            }

            // draw view border
            ev.Graphics.DrawRectangle(this.pen, this.destRect);

            // draw focus rect
            ev.DrawFocusRectangle();
        }

        #endregion

        #region Class Properties

        private static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        private static readonly Brush BlackBrush = new SolidBrush(Color.Black);

        #endregion
    }
}
