#ifndef SMILE_POINTER_STATE_H
#define SMILE_POINTER_STATE_H

typedef struct SmilePointerState
{
    long long x;
    long long y;
    long long delta_x;
    long long delta_y;
    long long wheel_delta;
    long long wheel_remainder;
    unsigned int held_buttons;
    unsigned int pressed_buttons;
    unsigned int released_buttons;
    int inside;
    int position_valid;
} SmilePointerState;

void smile_pointer_state_reset(SmilePointerState* state);
void smile_pointer_state_begin_frame(SmilePointerState* state);
void smile_pointer_state_position(SmilePointerState* state, long long x, long long y, int inside);
int smile_pointer_state_press(SmilePointerState* state, long long button);
int smile_pointer_state_release(SmilePointerState* state, long long button);
void smile_pointer_state_cancel(SmilePointerState* state);
void smile_pointer_state_wheel(SmilePointerState* state, long long raw_delta, long long units_per_step);

#endif
