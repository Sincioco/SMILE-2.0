#pragma once

// Resolve immutable horizontal receiver geometry for the shared reflection pass.
static float smile_3d_receiver_backdrop_seam(const SmileSubmission3D* receiver)
{
    SmileMesh3D* mesh;
    SmileMatrix3D model, view, projection, model_view, mvp;
    float aspect;
    float seam = 1.0f;
    int found = 0;
    if (receiver == 0) return 0.5f;
    mesh = smile_3d_mesh(receiver->mesh_handle);
    if (mesh == 0 || mesh->vertices == 0) return 0.5f;
    model = smile_3d_model(&receiver->object);
    view = smile_3d_view();
    aspect = (float)smile_3d_viewport_width() /
        (float)smile_3d_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    model_view = smile_3d_multiply(model, view);
    mvp = smile_3d_multiply(model_view, projection);
    for (unsigned int index = 0; index < mesh->vertex_count; ++index)
    {
        const SmileVertex3D* vertex = &mesh->vertices[index];
        float clip_y = vertex->x * mvp.m[1] + vertex->y * mvp.m[5] +
            vertex->z * mvp.m[9] + mvp.m[13];
        float clip_w = vertex->x * mvp.m[3] + vertex->y * mvp.m[7] +
            vertex->z * mvp.m[11] + mvp.m[15];
        float screen_y;
        if (clip_w <= 0.0001f) continue;
        screen_y = 0.5f - 0.5f * clip_y / clip_w;
        if (screen_y < seam) seam = screen_y;
        found = 1;
    }
    if (!found) return 0.5f;
    if (seam < 0.0f) return 0.0f;
    if (seam > 0.95f) return 0.95f;
    return seam;
}

static int smile_3d_receiver_plane(const SmileSubmission3D* receiver,
    float* floor_height)
{
    SmileMesh3D* mesh;
    SmileMatrix3D model;
    float minimum_y = 0.0f;
    float maximum_y = 0.0f;
    if (receiver == 0 || floor_height == 0 || receiver->animation_mode != 0)
        return 0;
    mesh = smile_3d_mesh(receiver->mesh_handle);
    if (mesh == 0 || mesh->vertices == 0 || mesh->vertex_count < 3) return 0;
    model = smile_3d_model(&receiver->object);
    for (unsigned int index = 0; index < mesh->vertex_count; ++index)
    {
        const SmileVertex3D* vertex = &mesh->vertices[index];
        float world_y = vertex->x * model.m[1] + vertex->y * model.m[5] +
            vertex->z * model.m[9] + model.m[13];
        if (!isfinite(world_y)) return 0;
        if (index == 0 || world_y < minimum_y) minimum_y = world_y;
        if (index == 0 || world_y > maximum_y) maximum_y = world_y;
    }
    if (maximum_y - minimum_y > 0.01f) return 0;
    *floor_height = (minimum_y + maximum_y) * 0.5f;
    return 1;
}

// Only immutable, flat committed water ribbons can receive a planar capture.
// Curved water effects retain their existing screen-space reflection path.
static int smile_3d_water_plane(const SmileSubmission3D* submission, float* height)
{
    if (submission->kind != SMILE_3D_SUBMISSION_RIBBON_BATCH ||
        submission->material.vfx_shading_mode != SMILE_3D_VFX_SHADING_WATER) return 0;
    const SmileRibbonBatch3D* batch = smile_3d_ribbon_batch(submission->source_handle);
    if (!batch || batch->revision != submission->resource_revision ||
        batch->count < 2 || !batch->vertices) return 0;
    *height = batch->vertices[0].position[1];
    for (unsigned int index = 0; index < batch->count * 2; ++index)
        if (!isfinite(batch->vertices[index].position[1]) ||
            fabsf(batch->vertices[index].position[1] - *height) > .01f) return 0;
    return 1;
}

static int smile_3d_is_water_receiver(const SmileSubmission3D* submission, float height)
{
    float water_height;
    return smile_3d_water_plane(submission, &water_height) && fabsf(water_height - height) <= .01f;
}

static int smile_3d_resolve_reflection_receiver(
    const SmileSubmission3D** receiver)
{
    const SmileSubmission3D* first_receiver = 0;
    float effective_height = smile_reflections_requested_floor_height();
    int automatic = effective_height < 0.0f;
    int resolved = 0;
    for (unsigned int index = 0; index < smile_frame_submission_count3d; ++index)
    {
        const SmileSubmission3D* submission = &smile_frame_submissions3d[index];
        float receiver_height;
        if (!(submission->kind == SMILE_3D_SUBMISSION_OBJECT &&
            submission->object.reflection_mode == 2 &&
            smile_3d_submission_is_opaque(submission)))
            continue;
        if (first_receiver == 0) first_receiver = submission;
        if (!smile_3d_receiver_plane(submission, &receiver_height)) return 0;
        if (automatic && !resolved)
        {
            effective_height = receiver_height;
            resolved = 1;
        }
        else if (fabsf(receiver_height - effective_height) > 0.01f)
            return 0;
    }
    // Explicit object receivers take priority, preserving existing arena behavior.
    if (first_receiver == 0)
        for (unsigned int index = 0; index < smile_frame_submission_count3d; ++index)
        {
            const SmileSubmission3D* submission = &smile_frame_submissions3d[index];
            float water_height;
            if (!smile_3d_water_plane(submission, &water_height)) continue;
            if (!automatic && fabsf(water_height - effective_height) > .01f) continue;
            if (automatic && resolved && fabsf(water_height - effective_height) > .01f) return 0;
            effective_height = water_height;
            resolved = 1;
            first_receiver = submission;
        }
    if (first_receiver == 0) return 2;
    smile_reflections_resolve_floor_height(effective_height);
    *receiver = first_receiver;
    return 1;
}

static long long smile_3d_receiver_sample_error(const SmileSubmission3D* receiver)
{
    SmileMesh3D* mesh;
    SmileMatrix3D model, projection, main_view, reflected_view;
    SmileMatrix3D main_mvp, reflected_mvp;
    float aspect;
    float maximum_error = 0.0f;
    int previous_pass;
    if (receiver == 0) return 0;
    mesh = smile_3d_mesh(receiver->mesh_handle);
    if (mesh == 0 || mesh->vertices == 0) return 0;
    model = smile_3d_model(&receiver->object);
    aspect = (float)smile_3d_viewport_width() /
        (float)smile_3d_viewport_height();
    projection = smile_3d_projection(aspect > 0.0f ? aspect : 1.0f);
    previous_pass = smile_reflection_pass3d;
    smile_reflection_pass3d = 0;
    main_view = smile_3d_view();
    smile_reflection_pass3d = 1;
    reflected_view = smile_3d_view();
    smile_reflection_pass3d = previous_pass;
    main_mvp = smile_3d_multiply(smile_3d_multiply(model, main_view), projection);
    reflected_mvp = smile_3d_multiply(
        smile_3d_multiply(model, reflected_view), projection);
    for (unsigned int index = 0; index < mesh->vertex_count; ++index)
    {
        const SmileVertex3D* vertex = &mesh->vertices[index];
        float main_x = vertex->x * main_mvp.m[0] + vertex->y * main_mvp.m[4] +
            vertex->z * main_mvp.m[8] + main_mvp.m[12];
        float main_y = vertex->x * main_mvp.m[1] + vertex->y * main_mvp.m[5] +
            vertex->z * main_mvp.m[9] + main_mvp.m[13];
        float main_w = vertex->x * main_mvp.m[3] + vertex->y * main_mvp.m[7] +
            vertex->z * main_mvp.m[11] + main_mvp.m[15];
        float reflected_x = vertex->x * reflected_mvp.m[0] +
            vertex->y * reflected_mvp.m[4] + vertex->z * reflected_mvp.m[8] +
            reflected_mvp.m[12];
        float reflected_y = vertex->x * reflected_mvp.m[1] +
            vertex->y * reflected_mvp.m[5] + vertex->z * reflected_mvp.m[9] +
            reflected_mvp.m[13];
        float reflected_w = vertex->x * reflected_mvp.m[3] +
            vertex->y * reflected_mvp.m[7] + vertex->z * reflected_mvp.m[11] +
            reflected_mvp.m[15];
        float x_error;
        float y_error;
        if (main_w <= 0.0001f || reflected_w <= 0.0001f) continue;
        x_error = fabsf(0.5f * (main_x / main_w - reflected_x / reflected_w));
        y_error = fabsf(0.5f * (main_y / main_w - reflected_y / reflected_w));
        if (x_error > maximum_error) maximum_error = x_error;
        if (y_error > maximum_error) maximum_error = y_error;
    }
    return (long long)llroundf(maximum_error * 1000000.0f);
}
