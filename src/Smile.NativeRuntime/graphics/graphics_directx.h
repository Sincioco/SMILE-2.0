#ifndef SMILE_GRAPHICS_DIRECTX_H
#define SMILE_GRAPHICS_DIRECTX_H

#include "graphics_backend.h"

#ifdef __cplusplus
extern "C" {
#endif

void smile_graphics_directx_create(SmileGraphicsBackend* backend);
void* smile_graphics_directx_device(void);
void* smile_graphics_directx_context(void);
void* smile_graphics_directx_render_target(void);
int smile_graphics_directx_physical_width(void);
int smile_graphics_directx_physical_height(void);
double smile_graphics_directx_viewport_x(void);
double smile_graphics_directx_viewport_y(void);
double smile_graphics_directx_viewport_width(void);
double smile_graphics_directx_viewport_height(void);
int smile_graphics_directx_suspend_2d(void);
void smile_graphics_directx_resume_2d(void);

#ifdef __cplusplus
}
#endif

#endif
