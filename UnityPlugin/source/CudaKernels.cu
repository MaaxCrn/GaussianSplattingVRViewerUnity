#include "CudaKernels.h"

__global__ void fill_kernel(int width, int height, float value, cudaSurfaceObject_t surface) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {
		float4 rgba;
		rgba.x = value * x / width * y / height;
		rgba.y = value;
		rgba.z = value * (width - x) / width * (height - y) / height;
		rgba.w = value;
		surf2Dwrite(rgba, surface, (int)sizeof(float4)*x, y, cudaBoundaryModeClamp);
	}
}

__global__ void fill_depth_kernel(int width, int height, float value, cudaSurfaceObject_t surface) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {
		float r = value * x / width * y / height;
		surf2Dwrite(r, surface, (int)sizeof(float) * x, y, cudaBoundaryModeClamp);
	}
}

__global__ void copy_depth_kernel(int width, int height, cudaSurfaceObject_t source, cudaSurfaceObject_t cible) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {
		float r;
		surf2Dread(&r, source, (int)sizeof(float) * x, y, cudaBoundaryModeClamp);
		surf2Dwrite(r, cible, (int)sizeof(float) * x, y, cudaBoundaryModeClamp);
	}
}

__global__ void splat_to_texture_kernel(int width, int height, int channel, float* InData, cudaSurfaceObject_t surface) {
	uint32_t x = threadIdx.x + blockDim.x * blockIdx.x;
	uint32_t y = threadIdx.y + blockDim.y * blockIdx.y;

	if (x < width && y < height) {

		//Flip y
		uint32_t y_flip = height - 1 - y;

		if (channel == 4) {
			float4 rgba;
			rgba.x = InData[0 * width * height + (y_flip * width + x)];
			rgba.y = InData[1 * width * height + (y_flip * width + x)];
			rgba.z = InData[2 * width * height + (y_flip * width + x)];
			rgba.w = InData[3 * width * height + (y_flip * width + x)];

			surf2Dwrite(rgba, surface, (int)sizeof(float4) * x, y, cudaBoundaryModeClamp);
		}

		if (channel == 1) {
			float r = InData[0 * width * height + (y_flip * width + x)];
			surf2Dwrite(r, surface, (int)sizeof(float) * x, y, cudaBoundaryModeClamp);
		}
	}
}

template <typename T> T div_round_up(T val, T divisor) {
	return (val + divisor - 1) / divisor;
}

void cuda_fill(int width, int height, float value, cudaSurfaceObject_t surface) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up<uint32_t>((uint32_t)width, threads.x), div_round_up<uint32_t>((uint32_t)height, threads.y), 1 };
	fill_kernel<<<blocks, threads>>> (width, height, value, surface);
}

void cuda_fill_depth(int width, int height, float value, cudaSurfaceObject_t surface) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up<uint32_t>((uint32_t)width, threads.x), div_round_up<uint32_t>((uint32_t)height, threads.y), 1 };
	fill_depth_kernel<<<blocks, threads>>> (width, height, value, surface);
}

void cuda_copy_depth_kernel(int width, int height, cudaSurfaceObject_t source, cudaSurfaceObject_t cible) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up<uint32_t>((uint32_t)width, threads.x), div_round_up<uint32_t>((uint32_t)height, threads.y), 1 };
	copy_depth_kernel<<<blocks, threads>>> (width, height, source, cible);
}

void cuda_splat_to_texture(int width, int height, int channel, float* rgb, cudaSurfaceObject_t surface) {
	const dim3 threads = { 16, 16, 1 };
	const dim3 blocks = { div_round_up((uint32_t)width, threads.x), div_round_up((uint32_t)height, threads.y), 1 };
	splat_to_texture_kernel<<<blocks, threads>>>(width, height, channel, rgb, surface);
}
