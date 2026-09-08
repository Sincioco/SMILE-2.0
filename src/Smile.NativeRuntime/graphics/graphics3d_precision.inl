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
    if (command == 7)
    {
        for (int index = 3; index < 8; ++index)
            if (floor(values[index]) != values[index] || fabs(values[index]) > 1000000.0)
            { smile_last_error3d = 43; return 0; }
        return smile_3d_set_local_light(resource, 1, a, b, c,
            (long long)d, (long long)e, (long long)f, (long long)g * 10, (long long)h);
    }
    // The point selector stays integral in this reserved command range.
    if (command >= 9999 && command < 18192)
    {
        const long long point_index = command - 10000;
        SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(resource);
        bool valid = batch != 0 && point_index >= 0 && point_index < batch->capacity && g >= 0.0 && g <= 1.0;
        for (int index = 0; index < 6; ++index) valid = valid && fabs(values[index]) <= 1000000.0;
        if (!valid) { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileRibbonPoint3D* point = &batch->points[point_index];
        for (int index = 0; index < 3; ++index)
        { point->left[index] = (float)values[index]; point->right[index] = (float)values[index + 3]; }
        point->u = (float)g;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (command >= 19999 && command < 1068576)
    {
        const long long selector = command - 20000;
        const long long point_index = selector % 4096;
        const long long frame = selector / 4096;
        SmileParticleBatch3D* batch = smile_3d_particle_batch(resource);
        bool valid = batch != 0 && selector >= 0 && point_index < batch->capacity &&
            frame < (long long)batch->atlas_columns * batch->atlas_rows && (float)d > 0.0f;
        for (int index = 0; index < 5; ++index) valid = valid && fabs(values[index]) <= 1000000.0;
        if (!valid) { smile_last_error3d = 54; smile_vfx_rejected_operation_count3d++; return 0; }
        SmileParticleInstance3D* instance = &batch->instances[point_index];
        for (int index = 0; index < 4; ++index) instance->position_size[index] = (float)values[index];
        instance->rotation_uv[0] = (float)(e * 0.017453292519943295);
        instance->rotation_uv[1] = (float)(frame % batch->atlas_columns) / batch->atlas_columns;
        instance->rotation_uv[2] = (float)(frame / batch->atlas_columns) / batch->atlas_rows;
        batch->staging_revision++;
        if (batch->staging_revision == 0) batch->staging_revision = 1;
        return 1;
    }
    if (command >= 1999999 && command < 2032768)
    {
        SmileGpuParticleSystem3D* system = smile_3d_gpu_particle_system(resource);
        if (system == 0) { smile_last_error3d = SMILE_3D_GPU_PARTICLE_ERROR_INVALID; return 0; }
        return smile_3d_stage_gpu_particle_kinematics(system, command - 2000000, a, b, c, d, e, f);
    }
    SmileObject3D* object = smile_3d_object(resource);
    if (object == 0 || command < 2 || command > 6) { smile_last_error3d = 5; return 0; }
    const int count = command == 2 ? 9 : command == 6 ? 6 : 3;
    for (int index = 0; index < count; ++index)
    {
        const int scale = command == 5 || command == 2 && index >= 6;
        if (fabs(values[index]) > 1000000.0 || scale && (float)(values[index] / 100.0) <= 0.0f)
        { smile_last_error3d = 5; return 0; }
        if (command == 6 && index >= 3 && fabs(values[index]) > 360.0)
        { smile_last_error3d = 5; return 0; }
    }
    // Validate the whole candidate before the first field write. Captured frame
    // objects are separate immutable snapshots owned by the normal frame queue.
    for (int index = 0; index < 3; ++index)
    {
        if (command == 2 || command == 3) object->position[index] = (float)values[index];
        if (command == 2 || command == 4) object->rotation[index] = (float)values[index + (command == 2 ? 3 : 0)];
        if (command == 2 || command == 5) object->scale[index] = (float)(values[index + (command == 2 ? 6 : 0)] / 100.0);
        if (command == 6)
        { object->pivot_rotation_position[index] = (float)values[index]; object->pivot_rotation[index] = (float)values[index + 3]; }
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
    if (command == 3 && component >= 0 && component < 24)
    {
        const SmileObject3D* object = smile_3d_object(resource);
        SmileMatrix3D socket;
        const bool ignore_offsets = (component >= 3 && component < 6) || component >= 15;
        if (object != 0 && smile_3d_model_socket_matrix(smile_3d_animator(object->animator_handle),
            index, resource, ignore_offsets ? 1 : 0, &socket))
        {
            if (component < 6) return socket.m[12 + component % 3];
            static const int basis_fields[] = {0, 1, 2, 4, 5, 6, 8, 9, 10};
            return socket.m[basis_fields[(component - 6) % 9]];
        }
        smile_last_error3d = 48;
        return 0.0;
    }
    if (command == 4 && resource == 0 && index == 0 && component >= 0 && component < 2)
        return component == 0 ? smile_reflections_floor_height() : smile_reflections_requested_floor_height();
    if (command == 6 && component >= 0 && component < 6)
    {
        const SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(resource);
        if (batch != 0 && index >= 0 && index < batch->capacity)
            return component < 3 ? batch->points[index].left[component] : batch->points[index].right[component - 3];
    }
    if ((command == 7 || command == 8) && component >= 0 && component < 3)
    {
        const SmileParticleBatch3D* batch = smile_3d_particle_batch(resource);
        if (batch != 0 && index >= 0 && index < (command == 7 ? batch->capacity : batch->count))
            return (command == 7 ? batch->instances : batch->committed_instances)[index].position_size[component];
    }
    if (command == 9 && component >= 0 && component < 6)
    {
        const SmileGpuParticleSystem3D* system = smile_3d_gpu_particle_system(resource);
        if (system != 0 && index >= 0 && index < system->capacity)
            return component < 3 ? system->staged_states[index].position_age[component] :
                system->staged_states[index].velocity_lifetime[component - 3];
    }
    if (command == 10 && index == 0 && resource >= 0 && resource < SMILE_3D_MAX_LOCAL_LIGHTS && component >= 0 && component < 3)
        return smile_local_lights3d[resource].position[component];
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
