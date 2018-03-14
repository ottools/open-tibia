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
        private struct ListElement
        {
            private Bitmap m_bitmap;

            public ListElement(AssetsManager manager, Sprite sprite)
            {
                AssetsManager = manager;
                Sprite = sprite;
                m_bitmap = null;
            }

            public AssetsManager AssetsManager { get; }
            public Sprite Sprite { get; }
            public Bitmap Bitmap
            {
                get
                {
                    if (m_bitmap != null)
                    {
                        return m_bitmap;
                    }

                    m_bitmap = AssetsManager.GetSpriteBitmap(Sprite.ID);
                    return m_bitmap;
                }
            }

            public override string ToString()
            {
                return Sprite.ID.ToString();
            }
        }

        private const int ItemMargin = 5;

        private Rectangle m_layoutRect;
        private Rectangle m_destRect;
        private Rectangle m_sourceRect;
        private Pen m_pen;

        public SpriteListBox()
        {
            m_layoutRect = new Rectangle();
            m_destRect = new Rectangle(ItemMargin, 0, Sprite.DefaultSize, Sprite.DefaultSize);
            m_sourceRect = new Rectangle();
            m_pen = new Pen(Color.DimGray);

            MeasureItem += MeasureItemHandler;
            DrawItem += DrawItemHandler;
            DrawMode = DrawMode.OwnerDrawVariable;
        }

        public AssetsManager AssetsManager { get; set; }

        public void Add(Sprite sprite)
        {
            Items.Add(sprite);
        }

        public void AddRange(Sprite[] sprites)
        {
            Items.AddRange(sprites);
        }

        public void RemoveSelectedSprites()
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

        private void MeasureItemHandler(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)(Sprite.DefaultSize + (2 * ItemMargin));
        }

        private void DrawItemHandler(object sender, DrawItemEventArgs ev)
        {
            if (Items.Count == 0 || ev.Index == -1)
            {
                return;
            }

            Rectangle bounds = ev.Bounds;

            // draw background
            ev.DrawBackground();

            // draw border
            ev.Graphics.DrawRectangle(Pens.Gray, bounds);

            ListElement element = (ListElement)Items[ev.Index];

            // find the area in which to put the text and draw.
            m_layoutRect.X = bounds.Left + Sprite.DefaultSize + (3 * ItemMargin);
            m_layoutRect.Y = bounds.Top + (ItemMargin * 2);
            m_layoutRect.Width = bounds.Right - ItemMargin - m_layoutRect.X;
            m_layoutRect.Height = bounds.Bottom - ItemMargin - m_layoutRect.Y;

            // draw sprite id
            if ((ev.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                m_pen.Brush = WhiteBrush;
                ev.Graphics.DrawString(element.ToString(), Font, WhiteBrush, m_layoutRect);
            }
            else
            {
                m_pen.Brush = BlackBrush;
                ev.Graphics.DrawString(element.ToString(), Font, BlackBrush, m_layoutRect);
            }

            m_destRect.Y = bounds.Top + ItemMargin;

            Bitmap bitmap = element.Bitmap;
            if (bitmap != null)
            {
                m_sourceRect.Width = bitmap.Width;
                m_sourceRect.Height = bitmap.Height;
                ev.Graphics.DrawImage(bitmap, m_destRect, m_sourceRect, GraphicsUnit.Pixel);
            }

            // draw sprite border
            ev.Graphics.DrawRectangle(m_pen, m_destRect);

            // draw focus rectangle
            ev.DrawFocusRectangle();
        }

        private static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        private static readonly Brush BlackBrush = new SolidBrush(Color.Black);
    }
}
