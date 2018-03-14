#region Licence
/**
* Copyright © 2015-2018 OTTools <https://github.com/ottools/open-tibia>
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

using OpenTibia.Assets;
using System.Drawing;
using System.Windows.Forms;

namespace OpenTibia.Controls
{
    public class SpriteListBox : ListBox
    {
        #region Private Properties

        private const int ItemMargin = 5;

        private Rectangle layoutRect;
        private Rectangle destRect;
        private Rectangle sourceRect;
        private Pen pen;

        #endregion

        #region Constructor

        public SpriteListBox()
        {
            this.layoutRect = new Rectangle();
            this.destRect = new Rectangle(ItemMargin, 0, Sprite.DefaultSize, Sprite.DefaultSize);
            this.sourceRect = new Rectangle();
            this.pen = new Pen(Color.DimGray);
            this.MeasureItem += new MeasureItemEventHandler(this.MeasureItemHandler);
            this.DrawItem += new DrawItemEventHandler(this.DrawItemHandler);
            this.DrawMode = DrawMode.OwnerDrawVariable;
        }

        #endregion

        #region Public Methods

        public void Add(Sprite sprite)
        {
            this.Items.Add(sprite);
        }

        public void AddRange(Sprite[] sprites)
        {
            this.Items.AddRange(sprites);
        }

        public void RemoveSelectedSprites()
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
            this.Items.Clear();
        }

        #endregion

        #region Event Handlers

        private void MeasureItemHandler(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)(Sprite.DefaultSize + (2 * ItemMargin));
        }

        private void DrawItemHandler(object sender, DrawItemEventArgs ev)
        {
            if (this.Items.Count == 0 || ev.Index == -1)
            {
                return;
            }

            Rectangle bounds = ev.Bounds;

            // draw background
            ev.DrawBackground();

            // draw border
            ev.Graphics.DrawRectangle(Pens.Gray, bounds);

            Sprite sprite = (Sprite)this.Items[ev.Index];

            // find the area in which to put the text and draw.
            this.layoutRect.X = bounds.Left + Sprite.DefaultSize + (3 * ItemMargin);
            this.layoutRect.Y = bounds.Top + (ItemMargin * 2);
            this.layoutRect.Width = bounds.Right - ItemMargin - this.layoutRect.X;
            this.layoutRect.Height = bounds.Bottom - ItemMargin - this.layoutRect.Y;

            // draw sprite id
            if ((ev.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                this.pen.Brush = WhiteBrush;
                ev.Graphics.DrawString(sprite.ToString(), this.Font, WhiteBrush, this.layoutRect);
            }
            else
            {
                this.pen.Brush = BlackBrush;
                ev.Graphics.DrawString(sprite.ToString(), this.Font, BlackBrush, this.layoutRect);
            }

            this.destRect.Y = bounds.Top + ItemMargin;

            Bitmap bitmap = sprite.GetBitmap();
            if (bitmap != null)
            {
                this.sourceRect.Width = bitmap.Width;
                this.sourceRect.Height = bitmap.Height;
                ev.Graphics.DrawImage(bitmap, this.destRect, this.sourceRect, GraphicsUnit.Pixel);
            }

            // draw sprite border
            ev.Graphics.DrawRectangle(this.pen, this.destRect);

            // draw focus rectangle
            ev.DrawFocusRectangle();
        }

        #endregion

        #region Class Properties

        private static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        private static readonly Brush BlackBrush = new SolidBrush(Color.Black);

        #endregion
    }
}
