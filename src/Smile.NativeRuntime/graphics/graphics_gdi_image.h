#ifndef SMILE_GRAPHICS_GDI_IMAGE_H
#define SMILE_GRAPHICS_GDI_IMAGE_H

#include <windows.h>
#include "image_resource.h"

#ifdef __cplusplus
extern "C" {
#endif
void smile_gdi_draw_image_resource(HDC dc, SmileImageResource* image,
    int source_x, int source_y, int source_width, int source_height,
    int destination_x, int destination_y, int destination_width, int destination_height,
    int opacity, int filter, int flip);
void smile_gdi_image_shutdown(void);
#ifdef __cplusplus
}
#endif
#endif
