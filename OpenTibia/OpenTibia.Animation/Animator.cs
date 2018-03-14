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

using OpenTibia.Utils;
using System;

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
        private static readonly Random Random = new Random();

        private int m_frames;
        private int m_startFrame;
        private int m_loopCount;
        private AnimationMode m_mode;
        private FrameDuration[] m_durations;
        private long m_lastTime;
        private int m_currentFrameDuration;
        private int m_currentFrame;
        private int m_currentLoop;
        private AnimationDirection m_currentDirection;

        public Animator(int frames, int startFrame, int loopCount, AnimationMode mode, FrameDuration[] durations)
        {
            m_frames = frames;
            m_startFrame = startFrame;
            m_loopCount = loopCount;
            m_mode = mode;
            m_durations = durations;
            Frame = (int)FrameMode.Automatic;
        }

        public Animator(FrameGroup frameGroup)
        {
            m_frames = frameGroup.Frames;
            m_startFrame = frameGroup.StartFrame;
            m_loopCount = frameGroup.LoopCount;
            m_mode = frameGroup.AnimationMode;
            m_durations = frameGroup.FrameDurations;
            Frame = (int)FrameMode.Automatic;
        }

        public int Frame
        {
            get => m_currentFrame;
            set
            {
                if (m_currentFrame != value)
                {
                    if (m_mode == AnimationMode.Asynchronous)
                    {
                        if (value == (ushort)FrameMode.Asynchronous)
                        {
                            m_currentFrame = 0;
                        }
                        else if (value == (ushort)FrameMode.Random)
                        {
                            m_currentFrame = Random.Next(0, m_frames);
                        }
                        else if (value >= 0 && value < m_frames)
                        {
                            m_currentFrame = value;
                        }
                        else
                        {
                            m_currentFrame = GetStartFrame();
                        }

                        IsComplete = false;
                        m_lastTime = Clock.ElapsedMilliseconds;
                        m_currentFrameDuration = m_durations[m_currentFrame].Duration;
                    }
                    else
                    {
                        CalculateSynchronous();
                    }
                }
            }
        }

        public bool IsComplete { get; private set; }

        public void Update(long timestamp)
        {
            if (timestamp != m_lastTime && !IsComplete)
            {
                int elapsed = (int)(timestamp - m_lastTime);
                if (elapsed >= m_currentFrameDuration)
                {
                    int frame = m_loopCount < 0 ? GetPingPongFrame() : GetLoopFrame();
                    if (m_currentFrame != frame)
                    {
                        int duration = m_durations[frame].Duration - (elapsed - m_currentFrameDuration);
                        if (duration < 0 && m_mode == AnimationMode.Synchronous)
                        {
                            CalculateSynchronous();
                        }
                        else
                        {
                            m_currentFrame = frame;
                            m_currentFrameDuration = duration < 0 ? 0 : duration;
                        }
                    }
                    else
                    {
                        IsComplete = true;
                    }
                }
                else
                {
                    m_currentFrameDuration = m_currentFrameDuration - elapsed;
                }

                m_lastTime = timestamp;
            }
        }

        public int GetStartFrame()
        {
            if (m_startFrame > -1)
            {
                return m_startFrame;
            }

            return Random.Next(0, m_frames);
        }

        private int GetLoopFrame()
        {
            int nextFrame = (m_currentFrame + 1);
            if (nextFrame < m_frames)
            {
                return nextFrame;
            }

            if (m_loopCount == 0)
            {
                return 0;
            }

            if (m_currentLoop < (m_loopCount - 1))
            {
                m_currentLoop++;
                return 0;
            }

            return m_currentFrame;
        }

        private int GetPingPongFrame()
        {
            int count = m_currentDirection == AnimationDirection.Forward ? 1 : -1;
            int nextFrame = m_currentFrame + count;
            if (m_currentFrame + count < 0 || nextFrame >= m_frames)
            {
                m_currentDirection = m_currentDirection == AnimationDirection.Forward ? AnimationDirection.Backward : AnimationDirection.Forward;
                count *= -1;
            }

            return m_currentFrame + count;
        }

        private void CalculateSynchronous()
        {
            int totalDuration = 0;
            for (int i = 0; i < m_frames; i++)
            {
                totalDuration += m_durations[i].Duration;
            }

            long time = Clock.ElapsedMilliseconds;
            long elapsed = time % totalDuration;
            long totalTime = 0;

            for (int i = 0; i < m_frames; i++)
            {
                long duration = m_durations[i].Duration;
                if (elapsed >= totalTime && elapsed < totalTime + duration)
                {
                    m_currentFrame = i;
                    long timeDiff = elapsed - totalTime;
                    m_currentFrameDuration = (int)(duration - timeDiff);
                    break;
                }

                totalTime += duration;
            }

            m_lastTime = time;
        }
    }
}
