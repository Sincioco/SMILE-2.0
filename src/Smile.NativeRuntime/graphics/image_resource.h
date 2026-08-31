#ifndef SMILE_IMAGE_RESOURCE_H
#define SMILE_IMAGE_RESOURCE_H

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

typedef struct SmileImageResource SmileImageResource;

#ifdef __cplusplus
extern "C" {
#endif

SmileImageResource* smile_image_resource_load(const WCHAR* path);
SmileImageResource* smile_image_resource_retain(SmileImageResource* image);
void smile_image_resource_release(SmileImageResource* image);
long long smile_image_resource_width(const SmileImageResource* image);
long long smile_image_resource_height(const SmileImageResource* image);
const unsigned char* smile_image_resource_pixels(const SmileImageResource* image);
const unsigned char* smile_image_resource_straight_pixels(const SmileImageResource* image);
unsigned int smile_image_resource_stride(const SmileImageResource* image);
void* smile_image_resource_d2d_bitmap(SmileImageResource* image, void* device_context);
void smile_image_resource_release_backend_resources(void);
void smile_image_resource_shutdown(void);
long long smile_image_resource_decode_count(void);
long long smile_image_resource_cache_hit_count(void);
long long smile_image_resource_live_count(void);
long long smile_image_resource_cache_count(void);
long long smile_image_resource_reference_count(void);

#ifdef __cplusplus
}
#endif

#endif
