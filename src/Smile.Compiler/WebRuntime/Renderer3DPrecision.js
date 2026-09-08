// Typed protocol; all state belongs to the existing renderer and frame queue.
function renderer3DDouble(command, resource, ...values) {
    safe(command); safe(resource);
    renderer3DLastError = 0;
    if (values.length !== 12 || !values.every(value => typeof value === "number" && Number.isFinite(value))) {
        renderer3DLastError = 5; return 0;
    }
    if (command === 1) {
        if (renderer3DFrameActive) { renderer3DLastError = renderer3DCameraErrorFrameActive; return 0; }
        renderer3DClearPendingCamera();
        if (resource !== 0) { renderer3DLastError = 5; return 0; }
        if (!values.slice(0, 9).every(value => Math.abs(value) <= renderer3DCameraWorldBound)) {
            renderer3DLastError = renderer3DCameraErrorInvalidPositionTarget; return 0;
        }
        const candidate = values.map(Math.fround);
        const forwardScale = Math.max(...candidate.slice(0, 3).map((value, index) => Math.abs(candidate[index + 3] - value)));
        const upScale = Math.max(...candidate.slice(6, 9).map(Math.abs));
        if (forwardScale <= 1e-12 || upScale <= 1e-12) {
            renderer3DLastError = forwardScale <= 1e-12 ? renderer3DCameraErrorZeroViewDirection : renderer3DCameraErrorInvalidUp;
            return 0;
        }
        if (values[9] < 10 || values[9] > 160 || values[10] <= 0 || values[11] <= values[10] ||
            values[11] > 2000000 || candidate[10] <= 0 || candidate[11] <= candidate[10]) {
            renderer3DLastError = renderer3DCameraErrorInvalidProjection; return 0;
        }
        renderer3DPendingCamera.position = candidate.slice(0, 3);
        renderer3DPendingCamera.target = candidate.slice(3, 6);
        renderer3DPendingCamera.up = candidate.slice(6, 9);
        renderer3DPendingCamera.fov = candidate[9];
        renderer3DPendingCamera.near = candidate[10];
        renderer3DPendingCamera.far = candidate[11];
        renderer3DPendingCamera.hasProjection = renderer3DPendingCamera.hasUp = true;
        if (!renderer3DValidatePendingCamera()) { renderer3DClearPendingCamera(); return 0; }
        renderer3DPromotePendingCamera();
        renderer3DClearPendingCamera();
        return 1;
    }
    const object = renderer3DObjects.get(resource);
    if (!object || command < 2 || command > 5) { renderer3DLastError = 5; return 0; }
    const count = command === 2 ? 9 : 3;
    for (let index = 0; index < count; index++) {
        const scale = command === 5 || command === 2 && index >= 6;
        if (Math.abs(values[index]) > 1000000 || scale && Math.fround(values[index] / 100) <= 0) {
            renderer3DLastError = 5; return 0;
        }
    }
    if (command === 2 || command === 3) object.position = values.slice(0, 3).map(Math.fround);
    if (command === 2 || command === 4) object.rotation = values.slice(command === 2 ? 3 : 0, command === 2 ? 6 : 3).map(Math.fround);
    if (command === 2 || command === 5) object.scale = values.slice(command === 2 ? 6 : 0, command === 2 ? 9 : 3).map(value => Math.fround(value / 100));
    return 1;
}
function renderer3DPrecisionObjectComponent(object, component) {
    return component < 3 ? object.position[component] : component < 6
        ? object.rotation[component - 3] : object.scale[component - 6] * 100;
}
function renderer3DDoubleValue(command, resource, index, component) {
    [command, resource, index, component].forEach(safe);
    renderer3DLastError = 0;
    if (command === 1 && resource === 0 && index === 0 && component >= 0 && component < 12)
        return component < 3 ? renderer3DCamera.position[component] : component < 6
            ? renderer3DCamera.target[component - 3] : component < 9 ? renderer3DCamera.up[component - 6]
                : component === 9 ? renderer3DCamera.fov : component === 10 ? renderer3DCamera.near : renderer3DCamera.far;
    if (command === 2 && index === 0 && component >= 0 && component < 9) {
        const object = renderer3DObjects.get(resource);
        if (object) return renderer3DPrecisionObjectComponent(object, component);
    }
    if (command === 3 && component >= 0 && component < 6) {
        const object = renderer3DObjects.get(resource);
        if (object) return renderer3DModelSocketValue(renderer3DAnimators.get(object.animator), index,
            component % 3 + 1, resource, component >= 3 ? 1 : 0, true);
        renderer3DLastError = 48; return 0;
    }
    if (command === 4 && resource === 0 && index === 0 && component >= 0 && component < 2)
        return component === 0 ? renderer3DReflection.effectiveFloorHeight : renderer3DReflection.floorHeight;
    if (command === 5 && index >= 0 && index < renderer3DSubmissionCount && component >= 0 && component < 9) {
        const submitted = renderer3DSubmissionObjects[index];
        if (submitted.kind === renderer3DSubmissionObject && (resource === 0 || resource === submitted.source))
            return renderer3DPrecisionObjectComponent(submitted, component);
    }
    renderer3DLastError = 5;
    return 0;
}
