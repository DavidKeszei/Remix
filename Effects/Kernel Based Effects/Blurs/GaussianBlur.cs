using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remix.Effect;

/// <summary>
/// Represent a low-pass filter, which using the Gaussian-distribution as filter.
/// </summary>
public class GaussianBlur: Effect {
    private u32 _range = 0;
    private f32 _distribution = .0f;

    private BlurDirection _direction = BlurDirection.Vertical | BlurDirection.Horizontal;

    /// <summary>
    /// Current kernel length of the blurring.
    /// </summary>
    public u32 Range { get => _range; set => _range = value; }

    /// <summary>
    /// Controlling how pixels convoluted by the kernel. 
    /// </summary>
    public f32 Distribution { get => _distribution; set => _distribution = value; }

    /// <summary>
    /// Direction of the blur.
    /// </summary>
    public BlurDirection Direction { get => _direction; set => _direction = value; }

    public GaussianBlur(u32 range, f32 distribution): base(name: nameof(GaussianBlur)) {
        this._range = range;
        this._distribution = distribution;
    }

	/// <summary>
	/// Apply Gaussian-blur effect on the <paramref name="target"/>.
	/// </summary>
	/// <param name="target">Target <see cref="Image"/> of the effect.</param>
	/// <remarks><b>Remark: </b> If the <see cref="GaussianBlur.Range"/> property is <b>even</b> number, then the method add 1 to the range. (Odd kernels is preferred).</remarks>
	public override Task Apply(Image target) {
        unsafe {
            /* 1. Create simple kernel. */
            Task[] workers = new Task[Environment.ProcessorCount];
            if ((_range & 1) == 0) ++_range;

            _range = u32.Clamp(value: _range, min: 1, max: 255);

            f32* stack = stackalloc f32[(i32)_range];
            UMem<f32> kernel = new UMem<f32>(stack, _range);

            kernel.AsSpan(0, (i32)kernel.Length)
                  .Create1DGaussianKernel(range: (i32)_range, _distribution);

            /* 2. Apply the kernel on the image. */
            using UMem2D<RGBA> tempImage = CrateTempImageBuffer(target);

            if ((_direction & BlurDirection.Vertical) == BlurDirection.Vertical) {
                ApplyVerticalBlur(tempImage, target, kernel, workers);
                CopyToImage(tempImage, target);
            }

            if ((_direction & BlurDirection.Horizontal) == BlurDirection.Horizontal) {
                ApplyHorizontalBlur(tempImage, target, kernel, workers);
                CopyToImage(tempImage, target);
            }
        }

        return Task.CompletedTask;
    }

    private void ApplyVerticalBlur(UMem2D<RGBA> buff, Image image, UMem<f32> kernel, Task[] workers) {
        i32 remainedWorkerCount = workers.Length;
        i32 kernelInHalf = (i32)kernel.Length / 2;

        for(i32 x = 0; x < image.Scale.X; x += remainedWorkerCount) {
            i32 xCaptureRef = x;

            remainedWorkerCount = remainedWorkerCount < image.Scale.X - x ? workers.Length : (i32)(image.Scale.X - x);

            for (i32 worker = 0; worker < remainedWorkerCount; ++worker) {
                i32 workerCaptureIndex = worker;

                workers[worker] = Task.Run(action: () => {
                    Span<f32> sumOf = stackalloc f32[4];
                    sumOf.Clear();

                    for(i32 y = 0; y < image.Scale.Y; ++y) {

                        for(i32 kernelIndex = y - kernelInHalf; kernelIndex <= y + kernelInHalf; ++kernelIndex) {
                            RGBA current = 0x0u;

                            if (kernelIndex < 0) current = image[(u32)(xCaptureRef + workerCaptureIndex), (u32)i32.Abs(kernelIndex + kernelInHalf)];
                            else if(kernelIndex > image.Scale.Y - 1) {

                                u32 mirror = image.Scale.Y - (image.Scale.Y % (image.Scale.Y - 1));
                                current = image[(u32)(xCaptureRef + workerCaptureIndex), mirror];
                            }
                            else {
                                current = image[(u32)(xCaptureRef + workerCaptureIndex), (u32)kernelIndex];
                            }

                            sumOf[0] += kernel[(u32)(kernelIndex - y + kernelInHalf)] * current.R;
                            sumOf[1] += kernel[(u32)(kernelIndex - y + kernelInHalf)] * current.G;
                            sumOf[2] += kernel[(u32)(kernelIndex - y + kernelInHalf)] * current.B;
                            sumOf[3] += kernel[(u32)(kernelIndex - y + kernelInHalf)] * current.A;
                        }

                        buff[(u32)(xCaptureRef + workerCaptureIndex), (u32)y].R = (u8)f32.Clamp(sumOf[0], 0, u8.MaxValue);
                        buff[(u32)(xCaptureRef + workerCaptureIndex), (u32)y].G = (u8)f32.Clamp(sumOf[1], 0, u8.MaxValue);
                        buff[(u32)(xCaptureRef + workerCaptureIndex), (u32)y].B = (u8)f32.Clamp(sumOf[2], 0, u8.MaxValue);
                        buff[(u32)(xCaptureRef + workerCaptureIndex), (u32)y].A = (u8)f32.Clamp(sumOf[3], 0, u8.MaxValue);

                        sumOf.Clear();
                    }
                });
            }

            Task.WaitAll(tasks: workers);
        }
    }

    private void ApplyHorizontalBlur(UMem2D<RGBA> buff, Image image, UMem<f32> kernel, Task[] workers) {
        i32 remainedWorkerCount = workers.Length;
        i32 kernelInHalf = (i32)kernel.Length / 2;

        for(i32 y = 0; y < image.Scale.Y; y += remainedWorkerCount) {
            i32 yCaptureRef = y;

            remainedWorkerCount = remainedWorkerCount < image.Scale.Y - y ? workers.Length : (i32)(image.Scale.Y - y);

            for (i32 worker = 0; worker < remainedWorkerCount; ++worker) {
                i32 workerCaptureIndex = worker;

                workers[worker] = Task.Run(action: () => {
                    Span<f32> sumOf = stackalloc f32[4];
                    sumOf.Clear();

                    for(i32 x = 0; x < image.Scale.X; ++x) {

                        for(i32 kernelIndex = x - kernelInHalf; kernelIndex <= x + kernelInHalf; ++kernelIndex) {
                            RGBA current = 0x0u;

                            if (kernelIndex < 0) current = image[(u32)i32.Abs(kernelIndex + kernelInHalf), (u32)(yCaptureRef + workerCaptureIndex)];
                            else if(kernelIndex > image.Scale.X - 1) {

                                u32 mirror = image.Scale.X - (image.Scale.X % (image.Scale.X - 1));
                                current = image[mirror, (u32)(yCaptureRef + workerCaptureIndex)];
                            }
                            else {
                                current = image[(u32)kernelIndex, (u32)(yCaptureRef + workerCaptureIndex)];
                            }

                            sumOf[0] += kernel[(u32)(kernelIndex - x + kernelInHalf)] * current.R;
                            sumOf[1] += kernel[(u32)(kernelIndex - x + kernelInHalf)] * current.G;
                            sumOf[2] += kernel[(u32)(kernelIndex - x + kernelInHalf)] * current.B;
                            sumOf[3] += kernel[(u32)(kernelIndex - x + kernelInHalf)] * current.A;
                        }

                        buff[(u32)x, (u32)(yCaptureRef + workerCaptureIndex)].R = (u8)f32.Clamp(sumOf[0], 0, u8.MaxValue);
                        buff[(u32)x, (u32)(yCaptureRef + workerCaptureIndex)].G = (u8)f32.Clamp(sumOf[1], 0, u8.MaxValue);
                        buff[(u32)x, (u32)(yCaptureRef + workerCaptureIndex)].B = (u8)f32.Clamp(sumOf[2], 0, u8.MaxValue);
                        buff[(u32)x, (u32)(yCaptureRef + workerCaptureIndex)].A = (u8)f32.Clamp(sumOf[3], 0, u8.MaxValue);

                        sumOf.Clear();
                    }
                });
            }

            Task.WaitAll(tasks: workers);
        }
    }

    private UMem2D<RGBA> CrateTempImageBuffer(Image image) {
        UMem2D<RGBA> tmp = new UMem2D<RGBA>(image.Scale);

        for (u32 y = 0; y < image.Scale.Y; ++y)
            for (u32 x = 0; x < image.Scale.X; ++x)
                tmp[x, y] = image[x, y];

        return tmp;
    }

    private void CopyToImage(UMem2D<RGBA> from, Image to) {
        for (u32 y = 0; y < from.Scale.Y; ++y)
            for (u32 x = 0; x < from.Scale.X; ++x)
                to[x, y] = from[x, y];
    }
}
