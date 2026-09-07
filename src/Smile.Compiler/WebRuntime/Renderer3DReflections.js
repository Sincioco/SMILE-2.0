            const renderer3DReflectionFallbackNone = 0;
            const renderer3DReflectionFallbackDisabled = 1;
            const renderer3DReflectionFallbackNoReceiver = 2;
            const renderer3DReflectionFallbackCameraBelowFloor = 3;
            const renderer3DReflectionFallbackAllocationFailed = 4;
            const renderer3DReflectionFallbackRenderFailed = 5;
            const renderer3DReflection = {
                requested: false,
                strength: 45,
                softness: 35,
                scale: 50,
                floorHeight: -1,
                includeBackdrop: true,
                effective: false,
                fallbackReason: renderer3DReflectionFallbackDisabled,
                width: 0,
                height: 0,
                configurationRevision: 1,
                appliedRevision: 0,
                resourceGeneration: 1,
                failedRevision: 0,
                failedWidth: 0,
                failedHeight: 0,
                forcedFailureConsumed: false,
                targetBytes: 0,
                captures: 0,
                draws: 0,
                triangles: 0,
                compositions: 0,
                pass: false,
                texture: null,
                framebuffer: null,
                depth: null,
                camera: {
                    position: new Float32Array(3),
                    target: new Float32Array(3),
                    up: new Float32Array(3)
                }
            };

            function renderer3DReflectionDeleteResources() {
                const gl = renderer3DGl;
                if (gl) {
                    if (renderer3DReflection.depth) gl.deleteRenderbuffer(renderer3DReflection.depth);
                    if (renderer3DReflection.framebuffer) gl.deleteFramebuffer(renderer3DReflection.framebuffer);
                    if (renderer3DReflection.texture) gl.deleteTexture(renderer3DReflection.texture);
                }
                renderer3DReflection.texture = null;
                renderer3DReflection.framebuffer = null;
                renderer3DReflection.depth = null;
                renderer3DReflection.width = 0;
                renderer3DReflection.height = 0;
                renderer3DReflection.targetBytes = 0;
                renderer3DReflection.appliedRevision = 0;
            }

            function renderer3DReflectionConfigure(enabled, strength, softness, scale, floorHeight,
                includeBackdrop) {
                if (renderer3DFrameActive || (enabled !== 0 && enabled !== 1) || strength < 0 ||
                    strength > 100 || softness < 0 || softness > 100 || scale < 25 || scale > 100 ||
                    floorHeight < -1000000 || floorHeight > 1000000 ||
                    (includeBackdrop !== 0 && includeBackdrop !== 1)) {
                    renderer3DLastError = 50;
                    return 0;
                }
                const requested = enabled !== 0;
                if (renderer3DReflection.requested === requested &&
                    renderer3DReflection.strength === strength &&
                    renderer3DReflection.softness === softness &&
                    renderer3DReflection.scale === scale &&
                    renderer3DReflection.floorHeight === floorHeight &&
                    renderer3DReflection.includeBackdrop === (includeBackdrop !== 0)) return 1;
                renderer3DReflection.requested = requested;
                renderer3DReflection.strength = strength;
                renderer3DReflection.softness = softness;
                renderer3DReflection.scale = scale;
                renderer3DReflection.floorHeight = floorHeight;
                renderer3DReflection.includeBackdrop = includeBackdrop !== 0;
                renderer3DReflection.configurationRevision += 1;
                if (renderer3DReflection.configurationRevision > 2147483647)
                    renderer3DReflection.configurationRevision = 1;
                if (!requested) {
                    renderer3DReflection.effective = false;
                    renderer3DReflection.fallbackReason = renderer3DReflectionFallbackDisabled;
                }
                return 1;
            }

            function renderer3DReflectionBeginFrame() {
                renderer3DReflection.effective = false;
                renderer3DReflection.fallbackReason = renderer3DReflection.requested
                    ? renderer3DReflectionFallbackNoReceiver
                    : renderer3DReflectionFallbackDisabled;
                renderer3DReflection.captures = 0;
                renderer3DReflection.draws = 0;
                renderer3DReflection.triangles = 0;
                renderer3DReflection.compositions = 0;
                renderer3DReflection.pass = false;
            }

            function renderer3DReflectionEnsureResources() {
                const gl = renderer3DGl;
                let width = Math.max(1, Math.floor(backingWidth * renderer3DReflection.scale / 100));
                let height = Math.max(1, Math.floor(backingHeight * renderer3DReflection.scale / 100));
                const longest = Math.max(width, height);
                if (longest > 2048) {
                    width = Math.max(1, Math.floor(width * 2048 / longest));
                    height = Math.max(1, Math.floor(height * 2048 / longest));
                }
                if (renderer3DReflection.texture && renderer3DReflection.width === width &&
                    renderer3DReflection.height === height) {
                    renderer3DReflection.appliedRevision =
                        renderer3DReflection.configurationRevision;
                    renderer3DReflection.effective = true;
                    renderer3DReflection.fallbackReason = renderer3DReflectionFallbackNone;
                    return true;
                }
                if (renderer3DReflection.failedRevision ===
                        renderer3DReflection.configurationRevision &&
                    renderer3DReflection.failedWidth === width &&
                    renderer3DReflection.failedHeight === height) {
                    renderer3DReflection.effective = false;
                    renderer3DReflection.fallbackReason =
                        renderer3DReflectionFallbackAllocationFailed;
                    return false;
                }
                const forcedFailure = globalThis.SMILE_TEST_RENDERER3D_FORCE_REFLECTION_FAILURE;
                const failThisAttempt = !!forcedFailure &&
                    !(String(forcedFailure).toLowerCase() === "once" &&
                        renderer3DReflection.forcedFailureConsumed);
                if (!gl || failThisAttempt) {
                    if (failThisAttempt) renderer3DReflection.forcedFailureConsumed = true;
                    renderer3DReflection.failedRevision =
                        renderer3DReflection.configurationRevision;
                    renderer3DReflection.failedWidth = width;
                    renderer3DReflection.failedHeight = height;
                    renderer3DReflection.effective = false;
                    renderer3DReflection.fallbackReason =
                        renderer3DReflectionFallbackAllocationFailed;
                    return false;
                }
                const texture = gl.createTexture();
                const framebuffer = gl.createFramebuffer();
                const depth = gl.createRenderbuffer();
                let complete = !!(texture && framebuffer && depth);
                if (complete) {
                    gl.bindTexture(gl.TEXTURE_2D, texture);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, width, height, 0,
                        gl.RGBA, gl.UNSIGNED_BYTE, null);
                    gl.bindRenderbuffer(gl.RENDERBUFFER, depth);
                    gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT24, width, height);
                    gl.bindFramebuffer(gl.FRAMEBUFFER, framebuffer);
                    gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0,
                        gl.TEXTURE_2D, texture, 0);
                    gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT,
                        gl.RENDERBUFFER, depth);
                    complete = gl.checkFramebufferStatus(gl.FRAMEBUFFER) === gl.FRAMEBUFFER_COMPLETE;
                }
                gl.bindFramebuffer(gl.FRAMEBUFFER, null);
                gl.bindRenderbuffer(gl.RENDERBUFFER, null);
                gl.bindTexture(gl.TEXTURE_2D, null);
                if (!complete) {
                    if (depth) gl.deleteRenderbuffer(depth);
                    if (framebuffer) gl.deleteFramebuffer(framebuffer);
                    if (texture) gl.deleteTexture(texture);
                    renderer3DReflection.failedRevision =
                        renderer3DReflection.configurationRevision;
                    renderer3DReflection.failedWidth = width;
                    renderer3DReflection.failedHeight = height;
                    renderer3DReflection.effective = false;
                    renderer3DReflection.fallbackReason =
                        renderer3DReflectionFallbackAllocationFailed;
                    return false;
                }
                renderer3DReflectionDeleteResources();
                renderer3DReflection.texture = texture;
                renderer3DReflection.framebuffer = framebuffer;
                renderer3DReflection.depth = depth;
                renderer3DReflection.width = width;
                renderer3DReflection.height = height;
                renderer3DReflection.targetBytes = width * height * 8;
                renderer3DReflection.appliedRevision = renderer3DReflection.configurationRevision;
                renderer3DReflection.failedRevision = 0;
                renderer3DReflection.failedWidth = 0;
                renderer3DReflection.failedHeight = 0;
                renderer3DReflection.resourceGeneration += 1;
                if (renderer3DReflection.resourceGeneration > 2147483647)
                    renderer3DReflection.resourceGeneration = 1;
                renderer3DReflection.effective = true;
                renderer3DReflection.fallbackReason = renderer3DReflectionFallbackNone;
                return true;
            }

            function renderer3DReflectionCameraValues() {
                if (!renderer3DReflection.pass) return renderer3DCamera;
                const values = renderer3DReflection.camera;
                values.position[0] = renderer3DCamera.position[0];
                values.position[1] = 2 * renderer3DReflection.floorHeight - renderer3DCamera.position[1];
                values.position[2] = renderer3DCamera.position[2];
                values.target[0] = renderer3DCamera.target[0];
                values.target[1] = 2 * renderer3DReflection.floorHeight - renderer3DCamera.target[1];
                values.target[2] = renderer3DCamera.target[2];
                values.up[0] = renderer3DCamera.up[0];
                values.up[1] = -renderer3DCamera.up[1];
                values.up[2] = renderer3DCamera.up[2];
                values.fov = renderer3DCamera.fov;
                values.near = renderer3DCamera.near;
                values.far = renderer3DCamera.far;
                return values;
            }

            function renderer3DReflectionBind(program, object) {
                const gl = renderer3DGl;
                const receiver = !renderer3DReflection.pass && renderer3DReflection.effective &&
                    renderer3DReflectionObjectMode(object) === 2;
                gl.uniform4f(program.reflectionSettings,
                    renderer3DReflection.pass ? 1 : 0,
                    renderer3DReflection.floorHeight,
                    receiver ? renderer3DReflection.strength / 100 : 0,
                    renderer3DReflection.softness / 100);
                gl.uniform2f(program.reflectionViewport, backingWidth, backingHeight);
                gl.activeTexture(gl.TEXTURE6);
                gl.bindTexture(gl.TEXTURE_2D, receiver ? renderer3DReflection.texture : null);
                gl.uniform1i(program.reflectionTexture, 6);
            }

            function renderer3DReflectionRecordDraw(object, mesh) {
                if (renderer3DReflection.pass) {
                    renderer3DReflection.draws += 1;
                    renderer3DReflection.triangles += mesh.indexCount / 3;
                    return true;
                }
                if (renderer3DReflection.effective && renderer3DReflectionObjectMode(object) === 2)
                    renderer3DReflection.compositions += 1;
                return false;
            }

            function renderer3DReflectionObjectMode(object) {
                return object.reflectionMode !== undefined ? object.reflectionMode : 1;
            }

            function renderer3DReflectionBackdropSeam(receiver) {
                const mesh = receiver ? renderer3DMeshes.get(receiver.mesh) : null;
                if (!mesh || !mesh.vertices) return .5;
                renderer3DModelInto(renderer3DModelScratch, receiver);
                renderer3DViewInto(renderer3DViewScratch);
                renderer3DProjectionInto(renderer3DProjectionScratch,
                    backingWidth / backingHeight);
                renderer3DMultiplyInto(renderer3DMatrixScratchA,
                    renderer3DViewScratch, renderer3DModelScratch);
                renderer3DMultiplyInto(renderer3DMvpScratch,
                    renderer3DProjectionScratch, renderer3DMatrixScratchA);
                let seam = 1;
                let found = false;
                for (let index = 0; index < mesh.vertexCount; index += 1) {
                    const offset = index * 20;
                    const x = mesh.vertices[offset];
                    const y = mesh.vertices[offset + 1];
                    const z = mesh.vertices[offset + 2];
                    const clipY = renderer3DMvpScratch[1] * x +
                        renderer3DMvpScratch[5] * y + renderer3DMvpScratch[9] * z +
                        renderer3DMvpScratch[13];
                    const clipW = renderer3DMvpScratch[3] * x +
                        renderer3DMvpScratch[7] * y + renderer3DMvpScratch[11] * z +
                        renderer3DMvpScratch[15];
                    if (clipW <= .0001) continue;
                    seam = Math.min(seam, .5 - .5 * clipY / clipW);
                    found = true;
                }
                return found ? Math.max(0, Math.min(.95, seam)) : .5;
            }

            function renderer3DRenderReflectionPass() {
                if (!renderer3DReflection.requested) return true;
                let receiver = null;
                for (let index = 0; index < renderer3DSubmissionCount; index += 1) {
                    const object = renderer3DSubmissionObjects[index];
                    if (object.kind === renderer3DSubmissionObject &&
                        renderer3DReflectionObjectMode(object) === 2 &&
                        renderer3DSubmissionIsOpaque(object)) {
                        receiver = object;
                        break;
                    }
                }
                if (!receiver) {
                    renderer3DReflection.fallbackReason = renderer3DReflectionFallbackNoReceiver;
                    return true;
                }
                if (renderer3DCamera.position[1] <= renderer3DReflection.floorHeight + .01) {
                    renderer3DReflection.fallbackReason =
                        renderer3DReflectionFallbackCameraBelowFloor;
                    return true;
                }
                if (!renderer3DReflectionEnsureResources()) return true;
                const gl = renderer3DGl;
                gl.activeTexture(gl.TEXTURE6);
                gl.bindTexture(gl.TEXTURE_2D, null);
                gl.bindFramebuffer(gl.FRAMEBUFFER, renderer3DReflection.framebuffer);
                gl.viewport(0, 0, renderer3DReflection.width, renderer3DReflection.height);
                gl.enable(gl.DEPTH_TEST);
                gl.depthFunc(gl.LESS);
                gl.depthMask(true);
                gl.disable(gl.BLEND);
                gl.clearColor(
                    renderer3DHdrEffective
                        ? renderer3DSrgbToLinear(renderer3DClearScratch[0])
                        : renderer3DClearScratch[0],
                    renderer3DHdrEffective
                        ? renderer3DSrgbToLinear(renderer3DClearScratch[1])
                        : renderer3DClearScratch[1],
                    renderer3DHdrEffective
                        ? renderer3DSrgbToLinear(renderer3DClearScratch[2])
                        : renderer3DClearScratch[2],
                    1);
                gl.clearDepth(1);
                gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
                const backdropSeam = renderer3DReflectionBackdropSeam(receiver);
                renderer3DReflection.pass = true;
                let success = true;
                const postDrawCount = renderer3DPostDrawCount;
                if (renderer3DReflection.includeBackdrop && renderer3DBackdropTexture !== 0) {
                    const texture = renderer3DTextures.get(renderer3DBackdropTexture);
                    if (!texture || !renderer3DPostProgram || !renderer3DUploadTexture(texture))
                        success = false;
                    else if (backdropSeam > 0) renderer3DPostPass(renderer3DReflection.framebuffer,
                        renderer3DReflection.width, renderer3DReflection.height,
                        texture.gpu, null, renderer3DHdrEffective ? 6 : 7,
                        0, 0, 1, backdropSeam, 0);
                    renderer3DPostDrawCount = postDrawCount;
                    gl.bindFramebuffer(gl.FRAMEBUFFER, renderer3DReflection.framebuffer);
                    gl.viewport(0, 0, renderer3DReflection.width, renderer3DReflection.height);
                    gl.enable(gl.DEPTH_TEST);
                    gl.depthFunc(gl.LESS);
                    gl.depthMask(true);
                }
                if (success) {
                    for (let index = 0; index < renderer3DSubmissionCount; index += 1) {
                        const object = renderer3DSubmissionObjects[index];
                        if (object.kind !== renderer3DSubmissionObject ||
                            renderer3DReflectionObjectMode(object) !== 1 ||
                            !renderer3DSubmissionIsOpaque(object)) continue;
                        if (!renderer3DDrawImmediate(0, object)) {
                            success = false;
                            break;
                        }
                    }
                }
                renderer3DReflection.pass = false;
                gl.bindFramebuffer(gl.FRAMEBUFFER, null);
                if (success) renderer3DReflection.captures = 1;
                else {
                    renderer3DReflection.effective = false;
                    renderer3DReflection.fallbackReason = renderer3DReflectionFallbackRenderFailed;
                }
                return true;
            }

            function renderer3DReflectionValue(index) {
                if (index === 1) return renderer3DReflection.requested ? 1 : 0;
                if (index === 2) return renderer3DReflection.effective ? 1 : 0;
                if (index === 3) return renderer3DReflection.fallbackReason;
                if (index === 4) return renderer3DReflection.width;
                if (index === 5) return renderer3DReflection.height;
                if (index === 6) return renderer3DReflection.draws;
                if (index === 7) return renderer3DReflection.triangles;
                if (index === 8) return renderer3DReflection.captures;
                if (index === 9) return renderer3DReflection.compositions;
                if (index === 10) return renderer3DReflection.configurationRevision;
                if (index === 11) return renderer3DReflection.resourceGeneration;
                if (index === 12) return renderer3DReflection.targetBytes;
                if (index === 13) return renderer3DReflection.strength;
                if (index === 14) return renderer3DReflection.softness;
                if (index === 15) return renderer3DReflection.scale;
                if (index === 16) return Math.round(renderer3DReflection.floorHeight);
                if (index === 17) return renderer3DReflection.includeBackdrop ? 1 : 0;
                renderer3DLastError = 50;
                return 0;
            }

            function renderer3DReflectionOnContextLost() {
                renderer3DReflection.texture = null;
                renderer3DReflection.framebuffer = null;
                renderer3DReflection.depth = null;
                renderer3DReflection.width = 0;
                renderer3DReflection.height = 0;
                renderer3DReflection.targetBytes = 0;
                renderer3DReflection.appliedRevision = 0;
                renderer3DReflection.failedRevision = 0;
                renderer3DReflection.failedWidth = 0;
                renderer3DReflection.failedHeight = 0;
                renderer3DReflection.effective = false;
                renderer3DReflection.pass = false;
            }

            function renderer3DReflectionReset() {
                renderer3DReflectionDeleteResources();
                renderer3DReflection.requested = false;
                renderer3DReflection.strength = 45;
                renderer3DReflection.softness = 35;
                renderer3DReflection.scale = 50;
                renderer3DReflection.floorHeight = -1;
                renderer3DReflection.includeBackdrop = true;
                renderer3DReflection.forcedFailureConsumed = false;
                renderer3DReflection.configurationRevision += 1;
                if (renderer3DReflection.configurationRevision > 2147483647)
                    renderer3DReflection.configurationRevision = 1;
                renderer3DReflectionBeginFrame();
            }
