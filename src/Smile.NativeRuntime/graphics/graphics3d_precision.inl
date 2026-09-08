// Typed dispatch in the existing renderer translation unit. No resource/camera
// state is owned here; all writes and queries use the production pools below.
// Protocol: docs/libraries/precision3d-boundary.md (separate from legacy IDs).

static double smile_3d_precision_object_component(const SmileObject3D* object, int component)
{
    if (component < 3) return object->position[component];
    if (component < 6) return object->rotation[component - 3];
    return (double)object->scale[component - 6] * 100.0;
}

extern "C" long long smile_renderer3d_double(long long command, long long resource,
    double a, double b, double c, double d, double e, double f,
    double g, double h, double i, double j, double k, double l)
{
    const double values[12] = { a, b, c, d, e, f, g, h, i, j, k, l };
    smile_last_error3d = 0;
    for (int index = 0; index < 12; ++index)
        if (!_finite(values[index])) { smile_last_error3d = 5; return 0; }
    if (command == 1)
    {
        if (smile_frame_active3d)
        { smile_last_error3d = SMILE_3D_CAMERA_ERROR_FRAME_ACTIVE; return 0; }
        smile_3d_clear_pending_camera();
        if (resource != 0) { smile_last_error3d = 5; return 0; }
        for (int index = 0; index < 9; ++index)
            if (fabs(values[index]) > SMILE_3D_CAMERA_WORLD_BOUND)
            { smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_POSITION_TARGET; return 0; }
        if (j < 10.0 || j > 160.0 || k <= 0.0 || l <= k || l > 2000000.0 ||
            (float)k <= 0.0f || (float)l <= (float)k)
        { smile_last_error3d = SMILE_3D_CAMERA_ERROR_INVALID_PROJECTION; return 0; }
        // Deliberate binary64 -> renderer float32 boundary, before acceptance.
        // The existing validator also rejects vectors collapsed by narrowing.
        for (int index = 0; index < 3; ++index)
        {
            smile_pending_camera_position3d[index] = (float)values[index];
            smile_pending_camera_target3d[index] = (float)values[index + 3];
            smile_pending_camera_up3d[index] = (float)values[index + 6];
        }
        smile_pending_camera_fov3d = (float)j;
        smile_pending_camera_near3d = (float)k;
        smile_pending_camera_far3d = (float)l;
        smile_pending_camera_has_projection3d = smile_pending_camera_has_up3d = 1;
        double forward_scale = 0.0, up_scale = 0.0;
        for (int index = 0; index < 3; ++index)
        {
            forward_scale = fmax(forward_scale, fabs((double)smile_pending_camera_target3d[index] - smile_pending_camera_position3d[index]));
            up_scale = fmax(up_scale, fabs((double)smile_pending_camera_up3d[index]));
        }
        if (forward_scale <= 1e-12 || up_scale <= 1e-12)
        {
            smile_last_error3d = forward_scale <= 1e-12 ? SMILE_3D_CAMERA_ERROR_ZERO_VIEW_DIRECTION : SMILE_3D_CAMERA_ERROR_INVALID_UP;
            smile_3d_clear_pending_camera(); return 0;
        }
        if (!smile_3d_validate_pending_camera())
        { smile_3d_clear_pending_camera(); return 0; }
        smile_3d_promote_pending_camera();
        smile_3d_clear_pending_camera();
        return 1;
    }
    SmileObject3D* object = smile_3d_object(resource);
    if (object == 0 || command < 2 || command > 5) { smile_last_error3d = 5; return 0; }
    const int count = command == 2 ? 9 : 3;
    for (int index = 0; index < count; ++index)
    {
        const int scale = command == 5 || command == 2 && index >= 6;
        if (fabs(values[index]) > 1000000.0 || scale && (float)(values[index] / 100.0) <= 0.0f)
        { smile_last_error3d = 5; return 0; }
    }
    // Validate the whole candidate before the first field write. Captured frame
    // objects are separate immutable snapshots owned by the normal frame queue.
    for (int index = 0; index < 3; ++index)
    {
        if (command == 2 || command == 3) object->position[index] = (float)values[index];
        if (command == 2 || command == 4) object->rotation[index] = (float)values[index + (command == 2 ? 3 : 0)];
        if (command == 2 || command == 5) object->scale[index] = (float)(values[index + (command == 2 ? 6 : 0)] / 100.0);
    }
    return 1;
}

extern "C" double smile_renderer3d_double_value(long long command, long long resource,
    long long index, long long component)
{
    smile_last_error3d = 0;
    if (command == 1 && resource == 0 && index == 0 && component >= 0 && component < 12)
    {
        if (component < 3) return smile_camera_position3d[component];
        if (component < 6) return smile_camera_target3d[component - 3];
        if (component < 9) return smile_camera_up3d[component - 6];
        return component == 9 ? smile_camera_fov3d : component == 10 ? smile_camera_near3d : smile_camera_far3d;
    }
    if (command == 2 && index == 0 && component >= 0 && component < 9)
    {
        const SmileObject3D* object = smile_3d_object(resource);
        if (object != 0) return smile_3d_precision_object_component(object, (int)component);
    }
    if (command == 3 && component >= 0 && component < 6)
    {
        const SmileObject3D* object = smile_3d_object(resource);
        SmileMatrix3D socket;
        if (object != 0 && smile_3d_model_socket_matrix(smile_3d_animator(object->animator_handle),
            index, resource, component >= 3 ? 1 : 0, &socket))
            return socket.m[12 + component % 3];
        smile_last_error3d = 48;
        return 0.0;
    }
    if (command == 4 && resource == 0 && index == 0 && component >= 0 && component < 2)
        return component == 0 ? smile_reflections_floor_height() : smile_reflections_requested_floor_height();
    if (command == 5 && index >= 0 && index < smile_frame_submission_count3d && component >= 0 && component < 9)
    {
        const SmileSubmission3D* submitted = &smile_frame_submissions3d[index];
        if (submitted->kind == SMILE_3D_SUBMISSION_OBJECT &&
            (resource == 0 || resource == submitted->source_handle))
            return smile_3d_precision_object_component(&submitted->object, (int)component);
    }
    smile_last_error3d = 5;
    return 0.0;
}
