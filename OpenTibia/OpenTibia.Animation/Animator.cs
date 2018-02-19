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
using OpenTibia.Utils;
using System;
#endregion

namespace OpenTibia.Animation
{
    public enum AnimationMode : byte
    {
        Asynchronous = 0,
        Synchronous = 1
    }

    public enum FrameMode : short
    {
        Automatic = -1,
        Random = 0xFE,
        Asynchronous = 0xFF
    }

    public enum AnimationDirection : byte
    {
        Forward = 0,
        Backward = 1
    }

    public class Animator
    {
        #region Private Properties
        
        private static readonly Random Random = new Random();

        private int frames;
        private int startFrame;
        private int loopCount;
        private AnimationMode mode;
        private FrameDuration[] durations;
        private long lastTime;
        private int currentFrameDuration;
        private int currentFrame;
        private int currentLoop;
        private AnimationDirection currentDirection;

        #endregion

        #region Constructor
        
        public Animator(int frames, int startFrame, int loopCount, AnimationMode mode, FrameDuration[] durations)
        {
            this.frames = frames;
            this.startFrame = startFrame;
            this.loopCount = loopCount;
            this.mode = mode;
            this.durations = durations;
            this.Frame = (int)FrameMode.Automatic;
        }

        public Animator(FrameGroup frameGroup)
        {
            this.frames = frameGroup.Frames;
            this.startFrame = frameGroup.StartFrame;
            this.loopCount = frameGroup.LoopCount;
            this.mode = frameGroup.AnimationMode;
            this.durations = frameGroup.FrameDurations;
            this.Frame = (int)FrameMode.Automatic;
        }

        #endregion

        #region Public Properties

        public int Frame
        {
            get
            {
                return this.currentFrame;
            }

            set
            {
                if (this.currentFrame != value)
                {
                    if (this.mode == AnimationMode.Asynchronous)
                    {
                        if (value == (ushort)FrameMode.Asynchronous)
                        {
                            this.currentFrame = 0;
                        }
                        else if (value == (ushort)FrameMode.Random)
                        {
                            this.currentFrame = Random.Next(0, this.frames);
                        }
                        else if (value >= 0 && value < this.frames)
                        {
                            this.currentFrame = value;
                        }
                        else
                        {
                            this.currentFrame = this.GetStartFrame();
                        }

                        this.IsComplete = false;
                        this.lastTime = Clock.ElapsedMilliseconds;
                        this.currentFrameDuration = this.durations[this.currentFrame].Duration;
                    }
                    else
                    {
                        this.CalculateSynchronous();
                    }
                }
            }
        }

        public bool IsComplete { get; private set; }

        #endregion

        #region Public Methods

        public void Update(long timestamp)
        {
            if (timestamp != this.lastTime && !this.IsComplete)
            {
                int elapsed = (int)(timestamp - this.lastTime);
                if (elapsed >= this.currentFrameDuration)
                {
                    int frame = loopCount < 0 ? this.GetPingPongFrame() : this.GetLoopFrame();
                    if (this.currentFrame != frame)
                    {
                        int duration = this.durations[frame].Duration - (elapsed - this.currentFrameDuration);
                        if (duration < 0 && this.mode == AnimationMode.Synchronous)
                        {
                            this.CalculateSynchronous();
                        }
                        else
                        {
                            this.currentFrame = frame;
                            this.currentFrameDuration = duration < 0 ? 0 : duration;
                        }
                    }
                    else
                    {
                        this.IsComplete = true;
                    }
                }
                else
                {
                    this.currentFrameDuration = this.currentFrameDuration - elapsed;
                }
                
                this.lastTime = timestamp;
            }
        }

        public int GetStartFrame()
        {
            if (this.startFrame > -1)
            {
                return this.startFrame;
            }

            return Random.Next(0, this.frames);
        }

        #endregion

        #region Private Methods

        private int GetLoopFrame()
        {
            int nextFrame = (this.currentFrame + 1);
            if (nextFrame < this.frames)
            {
                return nextFrame;
            }
            
            if (this.loopCount == 0)
            {
                return 0;
            }

            if (this.currentLoop < (loopCount - 1))
            {
                this.currentLoop++;
                return 0;
            }

            return this.currentFrame;
        }

        private int GetPingPongFrame()
        {
            int count = this.currentDirection == AnimationDirection.Forward ? 1 : -1;
            int nextFrame = this.currentFrame + count;
            if (this.currentFrame + count < 0 || nextFrame >= frames)
            {
                this.currentDirection = this.currentDirection == AnimationDirection.Forward ? AnimationDirection.Backward : AnimationDirection.Forward;
                count *= -1;
            }

            return this.currentFrame + count;
        }

        private void CalculateSynchronous()
        {
            int totalDuration = 0;
            for (int i = 0; i < this.frames; i++)
            {
                totalDuration += durations[i].Duration;
            }

            long time = Clock.ElapsedMilliseconds;
            long elapsed = time % totalDuration;
            long totalTime = 0;
            
            for (int i = 0; i < this.frames; i++)
            {
                long duration = this.durations[i].Duration;
                if (elapsed >= totalTime && elapsed < totalTime + duration)
                {
                    this.currentFrame = i;
                    long timeDiff = elapsed - totalTime;
                    this.currentFrameDuration = (int)(duration - timeDiff);
                    break;
                }
                
                totalTime += duration;
            }
            
            this.lastTime = time;
        }

        #endregion
    }
}
