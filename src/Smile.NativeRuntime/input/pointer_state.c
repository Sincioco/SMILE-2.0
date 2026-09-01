#include <string.h>
#include "pointer_state.h"

static unsigned int smile_pointer_state_button_mask(long long button)
{
    if (button < 1 || button > 3)
        return 0;
    return 1U << (unsigned int)(button - 1);
}

void smile_pointer_state_reset(SmilePointerState* state)
{
    if (state != 0)
        memset(state, 0, sizeof(*state));
}

void smile_pointer_state_begin_frame(SmilePointerState* state)
{
    if (state == 0)
        return;
    state->delta_x = 0;
    state->delta_y = 0;
    state->wheel_delta = 0;
    state->pressed_buttons = 0;
    state->released_buttons = 0;
}

void smile_pointer_state_position(SmilePointerState* state, long long x, long long y, int inside)
{
    if (state == 0)
        return;
    if (state->position_valid)
    {
        state->delta_x += x - state->x;
        state->delta_y += y - state->y;
    }
    state->x = x;
    state->y = y;
    state->inside = inside != 0;
    state->position_valid = 1;
}

int smile_pointer_state_press(SmilePointerState* state, long long button)
{
    unsigned int mask = smile_pointer_state_button_mask(button);
    if (state == 0 || mask == 0 || (state->held_buttons & mask) != 0)
        return 0;
    state->held_buttons |= mask;
    state->pressed_buttons |= mask;
    return 1;
}

int smile_pointer_state_release(SmilePointerState* state, long long button)
{
    unsigned int mask = smile_pointer_state_button_mask(button);
    if (state == 0 || mask == 0 || (state->held_buttons & mask) == 0)
        return 0;
    state->held_buttons &= ~mask;
    state->released_buttons |= mask;
    return 1;
}

void smile_pointer_state_cancel(SmilePointerState* state)
{
    if (state == 0)
        return;
    state->released_buttons |= state->held_buttons;
    state->held_buttons = 0;
    state->inside = 0;
    state->position_valid = 0;
}

void smile_pointer_state_wheel(SmilePointerState* state, long long raw_delta, long long units_per_step)
{
    long long whole_steps;
    if (state == 0 || units_per_step <= 0)
        return;
    state->wheel_remainder += raw_delta;
    whole_steps = state->wheel_remainder / units_per_step;
    state->wheel_remainder -= whole_steps * units_per_step;
    state->wheel_delta += whole_steps;
}
