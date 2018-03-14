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
    public class ThingTypeListBox : ListBox
    {
        private const int ItemMargin = 5;

        private Rectangle m_layoutRect;
        private Rectangle m_destRect;
        private Rectangle m_sourceRect;
        private Pen m_pen;

        public ThingTypeListBox()
        {
            m_layoutRect = new Rectangle();
            m_destRect = new Rectangle(ItemMargin, 0, 32, 32);
            m_sourceRect = new Rectangle();
            m_pen = new Pen(Color.Transparent);
            MeasureItem += MeasureItem_Handler;
            DrawItem += DrawItem_Handler;
            DrawMode = DrawMode.OwnerDrawVariable;
        }

        public IAssetsManager AssetsManager { get; set; }

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
            if (SelectedIndex != -1)
            {
                SelectedObjectCollection selectedItems = SelectedItems;

                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    Items.Remove(selectedItems[i]);
                }
            }
        }

        public void Clear()
        {
            Items.Clear();
        }

        private void MeasureItem_Handler(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)(32 + (2 * ItemMargin));
        }

        private void DrawItem_Handler(object sender, DrawItemEventArgs ev)
        {
            Rectangle bounds = ev.Bounds;
            bounds.Width--;

            // draw background
            ev.DrawBackground();

            // draw border
            ev.Graphics.DrawRectangle(Pens.Gray, bounds);

            if (AssetsManager != null && ev.Index != -1)
            {
                ThingType thing = (ThingType)Items[ev.Index];

                // find the area in which to put the text and draw.
                m_layoutRect.X = bounds.Left + 32 + (3 * ItemMargin);
                m_layoutRect.Y = bounds.Top + (ItemMargin * 2);
                m_layoutRect.Width = bounds.Right - ItemMargin - m_layoutRect.X;
                m_layoutRect.Height = bounds.Bottom - ItemMargin - m_layoutRect.Y;

                // draw thing id end name
                if ((ev.State & DrawItemState.Selected) == DrawItemState.Selected)
                {
                    m_pen.Brush = WhiteBrush;
                    ev.Graphics.DrawString(thing.ToString(), Font, WhiteBrush, m_layoutRect);
                }
                else
                {
                    m_pen.Brush = BlackBrush;
                    ev.Graphics.DrawString(thing.ToString(), Font, BlackBrush, m_layoutRect);
                }

                m_destRect.Y = bounds.Top + ItemMargin;

                Bitmap bitmap = AssetsManager.GetObjectImage(thing);
                if (bitmap != null)
                {
                    m_sourceRect.Width = bitmap.Width;
                    m_sourceRect.Height = bitmap.Height;
                    ev.Graphics.DrawImage(bitmap, m_destRect, m_sourceRect, GraphicsUnit.Pixel);
                }
            }

            // draw view border
            ev.Graphics.DrawRectangle(m_pen, m_destRect);

            // draw focus rect
            ev.DrawFocusRectangle();
        }


        private static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        private static readonly Brush BlackBrush = new SolidBrush(Color.Black);
    }
}
