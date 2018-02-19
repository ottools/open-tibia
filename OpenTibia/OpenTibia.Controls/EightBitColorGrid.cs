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
using System.Drawing;
using System.Windows.Forms;
#endregion

namespace OpenTibia.Controls
{
    public class EightBitColorGrid : HsiColorGrid
    {
        #region Constructor

        public EightBitColorGrid() : base()
        {
            this.columns = 16;
            this.rows = 14;
            this.length = 215;
        }

        #endregion

        #region Protected Methods

        protected override Color GetRgbColor(int color)
        {
            return ColorUtils.EightBitToRgb(color);
        }

        #endregion
    }
}
