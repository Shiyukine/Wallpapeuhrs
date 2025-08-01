using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wallpapeuhrs.Utils
{
    public class ImageToMemoryStream
    {
        private Thread _animationThread;
        private volatile bool _isAnimating = false;
        private volatile bool _isPaused = false;
        public event Action<MemoryStream>? ImageUpdated;
        public event Action<Exception>? ImageLoadFailed;
        public event Action? ImageLoadCompleted;
        private Uri _uriSource = null;
        private volatile Image _imageSource = null;
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        public bool IsAnimating => _isAnimating;
        public bool IsPaused => _isPaused;

        public int Width
        {
            get
            {
                try
                {
                    if (_imageSource == null || _uriSource == null)
                        return 0;
                    return _imageSource.Width;
                }
                catch(InvalidOperationException ex)
                {
                    return 0; // Return 0 if there's an error
                }
                catch (Exception ex)
                {
                    ImageLoadFailed?.Invoke(ex);
                    return 0; // Return 0 if there's an error
                }
            }
        }

        public int Height
        {
            get
            {
                try
                {
                    if (_imageSource == null || _uriSource == null)
                        return 0;
                    return _imageSource.Height;
                }
                catch (InvalidOperationException ex)
                {
                    return 0; // Return 0 if there's an error
                }
                catch (Exception ex)
                {
                    ImageLoadFailed?.Invoke(ex);
                    return 0; // Return 0 if there's an error
                }
            }
        }

        public Uri UriSource
        {
            get => _uriSource;
            set
            {
                if (_uriSource != value)
                {
                    _uriSource = value;
                    if(_isAnimating) StopAnimation();
                    try
                    {
                        if (value == null) return;
                        if (value.IsFile && !File.Exists(value.LocalPath))
                        {
                            ImageLoadFailed?.Invoke(new FileNotFoundException("File not found", value.LocalPath));
                            return;
                        }
                        _imageSource = System.Drawing.Image.FromFile(UriSource.LocalPath);
                        ImageLoadCompleted?.Invoke();
                        if (!IsAnimated)
                        {
                            var ms = new MemoryStream();
                            _imageSource.Save(ms, ImageFormat.Png);
                            ImageUpdated?.Invoke(ms);
                        }
                        else
                        {
                            var frameCount = _imageSource.GetFrameCount(FrameDimension.Time);
                            if (frameCount > 1)
                            {
                                UpdateImage(_imageSource);
                            }
                            else
                            {
                                // If it's a single frame, just send it once
                                var ms = new MemoryStream();
                                _imageSource.Save(ms, ImageFormat.Png);
                                ImageUpdated?.Invoke(ms);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ImageLoadFailed?.Invoke(ex);
                    }
                }
            }
        }

        public bool IsAnimated
        {
            get
            {
                if (_imageSource == null)
                    return false;
                if (!_imageSource.RawFormat.Equals(ImageFormat.Gif))
                    return false;

                var frameDimensionsList = _imageSource.FrameDimensionsList;
                foreach (var guid in frameDimensionsList)
                {
                    if (guid.Equals(FrameDimension.Time.Guid))
                    {
                        return _imageSource.GetFrameCount(FrameDimension.Time) > 1;
                    }
                }
                return false;
            }
        }

        public ImageToMemoryStream()
        {
            _animationThread = new Thread(() =>
            {
                if (_imageSource == null)
                    return;
                var frameCount = _imageSource.GetFrameCount(FrameDimension.Time);
                var frameDelays = GetAllFrameDelays(_imageSource);
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                int currentFrame = 0;

                while (_isAnimating)
                {
                    if (_isPaused)
                    {
                        stopwatch.Stop(); // Stop timing when paused
                        _pauseEvent.Wait();

                        if (!_isAnimating) break;

                        stopwatch.Restart(); // Restart timing fresh when resumed
                    }

                    if (stopwatch.ElapsedMilliseconds >= frameDelays[currentFrame])
                    {
                        // Update frame on UI thread if needed
                        var frame = currentFrame;
                        Task.Run(() =>
                        {
                            _imageSource.SelectActiveFrame(FrameDimension.Time, frame);
                            var ms = new MemoryStream();
                            _imageSource.Save(ms, ImageFormat.Png);
                            ImageUpdated?.Invoke(ms);
                        });

                        currentFrame = (currentFrame + 1) % frameCount;
                        stopwatch.Restart(); // Fresh timing for next frame
                    }

                    Thread.Sleep(1); // Small sleep to prevent CPU spinning
                }
            })
            { IsBackground = true, Name = "WallpapeuhrsGifAnimThread" };
        }

        private List<int> GetAllFrameDelays(System.Drawing.Image image)
        {
            var delays = new List<int>();
            var frameCount = image.GetFrameCount(FrameDimension.Time);

            for (int i = 0; i < frameCount; i++)
            {
                image.SelectActiveFrame(FrameDimension.Time, i);
                var delay = image.GetPropertyItem(0x5100);

                if (delay != null && delay.Value.Length >= 4)
                {
                    int milliseconds = BitConverter.ToInt32(delay.Value, 0) * 10;
                    delays.Add(milliseconds);
                }
                else
                {
                    delays.Add(100); // Default delay
                }
            }

            return delays;
        }

        private async void UpdateImage(System.Drawing.Image image)
        {
            _isAnimating = true;
            _isPaused = false;
            _pauseEvent.Set(); // Start unpaused
            _animationThread.Start();
        }

        public void StopAnimation()
        {
            if (!IsAnimated || !_isAnimating) return;
            _isAnimating = false;
            _isPaused = false;
            _pauseEvent.Set(); // Unblock if paused
            _animationThread?.Join(1000); // Wait up to 1 second for thread to finish
        }

        public void PauseAnimation()
        {
            if (!IsAnimated) return;
            if (_isAnimating && !_isPaused)
            {
                _isPaused = true;
                _pauseEvent.Reset(); // Block the animation thread
            }
        }

        public void ResumeAnimation()
        {
            if (!IsAnimated) return;
            if (_isAnimating && _isPaused)
            {
                _isPaused = false;
                _pauseEvent.Set(); // Unblock the animation thread
            }
        }

        public void Dispose()
        {
            StopAnimation();
            _pauseEvent?.Dispose();
            _imageSource?.Dispose();
            _imageSource = null;
            _uriSource = null;
            _animationThread = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
