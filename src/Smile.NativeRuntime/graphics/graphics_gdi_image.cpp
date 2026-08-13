#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <objidl.h>
#include <gdiplus.h>
#include "graphics_gdi_image.h"

using namespace Gdiplus;
static ULONG_PTR smile_gdiplus_token;

static int smile_gdiplus_start(void)
{
    GdiplusStartupInput input;
    return smile_gdiplus_token != 0 || GdiplusStartup(&smile_gdiplus_token, &input, 0) == Ok;
}

extern "C" void smile_gdi_draw_image_resource(HDC dc, SmileImageResource* image,
    int source_x, int source_y, int source_width, int source_height,
    int destination_x, int destination_y, int destination_width, int destination_height,
    int opacity, int filter, int flip)
{
    if (!smile_gdiplus_start()) return;
    Bitmap bitmap((INT)smile_image_resource_width(image), (INT)smile_image_resource_height(image),
        (INT)smile_image_resource_stride(image), PixelFormat32bppPARGB,
        const_cast<BYTE*>(smile_image_resource_pixels(image)));
    Graphics graphics(dc);
    ImageAttributes attributes;
    ColorMatrix matrix = {{
        {1, 0, 0, 0, 0}, {0, 1, 0, 0, 0}, {0, 0, 1, 0, 0},
        {0, 0, 0, (REAL)opacity / 100.0f, 0}, {0, 0, 0, 0, 1}
    }};
    GraphicsState saved = graphics.Save();
    graphics.SetInterpolationMode(filter == 1 ? InterpolationModeNearestNeighbor : InterpolationModeHighQualityBicubic);
    graphics.SetPixelOffsetMode(filter == 1 ? PixelOffsetModeHalf : PixelOffsetModeHighQuality);
    attributes.SetColorMatrix(&matrix, ColorMatrixFlagsDefault, ColorAdjustTypeBitmap);
    if ((flip & 1) != 0 || (flip & 2) != 0)
    {
        graphics.TranslateTransform((REAL)(destination_x + ((flip & 1) ? destination_width : 0)),
            (REAL)(destination_y + ((flip & 2) ? destination_height : 0)));
        graphics.ScaleTransform((flip & 1) ? -1.0f : 1.0f, (flip & 2) ? -1.0f : 1.0f);
        destination_x = destination_y = 0;
    }
    Rect destination(destination_x, destination_y, destination_width, destination_height);
    graphics.DrawImage(&bitmap, destination, source_x, source_y, source_width, source_height, UnitPixel, &attributes);
    graphics.Restore(saved);
}

extern "C" void smile_gdi_image_shutdown(void)
{
    if (smile_gdiplus_token != 0) GdiplusShutdown(smile_gdiplus_token);
    smile_gdiplus_token = 0;
}
