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

namespace OpenTibia.Assets
{
    public enum DatFlags1010 : byte
    {
        Ground = 0x00,
        GroundBorder = 0x01,
        OnBottom = 0x02,
        OnTop = 0x03,
        Container = 0x04,
        Stackable = 0x05,
        ForceUse = 0x06,
        MultiUse = 0x07,
        Writable = 0x08,
        WritableOnce = 0x09,
        FluidContainer = 0x0A,
        Fluid = 0x0B,
        IsUnpassable = 0x0C,
        IsUnmovable = 0x0D,
        BlockMissiles = 0x0E,
        BlockPathfinder = 0x0F,
        NoMoveAnimation = 0x10,
        Pickupable = 0x11,
        Hangable = 0x12,
        HookSouth = 0x13,
        HookEast = 0x14,
        Rotatable = 0x15,
        HasLight = 0x16,
        DontHide = 0x17,
        Translucent = 0x18,
        HasOffset = 0x19,
        HasElevation = 0x1A,
        LyingObject = 0x1B,
        AnimateAlways = 0x1C,
        Minimap = 0x1D,
        LensHelp = 0x1E,
        FullGround = 0x1F,
        IgnoreLook = 0x20,
        Cloth = 0x21,
        Market = 0x22,
        DefaultAction = 0x23,
        Wrappable = 0x24,
        Unwrappable = 0x25,
        TopEffect = 0x26,
        Usable = 0xFE,

        LastFlag = 0xFF
    }
}
