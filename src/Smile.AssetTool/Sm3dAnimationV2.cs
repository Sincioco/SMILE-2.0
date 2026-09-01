using System.Buffers.Binary;
using System.Numerics;
using System.Text;
using System.Text.Json;

internal static partial class Sm3dV2
{
    private sealed class AnimationModel
    {
        public required List<AnimationNode> Nodes { get; init; }
        public required List<AnimationBone> Bones { get; init; }
        public required List<AnimationClip> Clips { get; init; }
        public required List<AnimationTrack> Tracks { get; init; }
        public required List<AnimationEvent> Events { get; init; }
        public required List<AnimationSocket> Sockets { get; init; }
        public required List<AnimationRoot> Roots { get; init; }
        public int PayloadBytes { get; set; }
    }

    private sealed class AnimationNode
    {
        public required int SourceIndex { get; init; }
        public required string Name { get; init; }
        public required int Parent { get; init; }
        public required Vector3 Translation { get; init; }
        public required Quaternion Rotation { get; init; }
        public required Vector3 Scale { get; init; }
        public uint Flags { get; set; }
    }

    private sealed class AnimationBone
    {
        public required int Node { get; init; }
        public required int Parent { get; init; }
        public required Matrix4x4 InverseBind { get; init; }
    }

    private sealed class AnimationClip
    {
        public required string Name { get; init; }
        public required int DurationMilliseconds { get; init; }
        public required int SampleRate { get; init; }
        public required int SampleCount { get; init; }
        public required bool Loop { get; init; }
        public int FirstTrack { get; set; }
        public int TrackCount { get; set; }
        public int FirstEvent { get; set; }
        public int EventCount { get; set; }
        public int RootIndex { get; set; } = -1;
    }

    private sealed class AnimationTrack
    {
        public required int Clip { get; init; }
        public required int Node { get; init; }
        public float[]? Translation { get; init; }
        public float[]? Rotation { get; init; }
        public float[]? Scale { get; init; }
    }

    private sealed class AnimationEvent
    {
        public required int Clip { get; init; }
        public required int TimeMilliseconds { get; init; }
        public required string Name { get; init; }
        public required int Value { get; init; }
        public required int Order { get; init; }
    }

    private sealed class AnimationSocket
    {
        public required string Name { get; init; }
        public required int Node { get; init; }
        public required Vector3 Translation { get; init; }
        public required Quaternion Rotation { get; init; }
        public required Vector3 Scale { get; init; }
    }

    private sealed class AnimationRoot
    {
        public required int Clip { get; init; }
        public required int Node { get; init; }
        public required uint TranslationAxes { get; init; }
        public required bool Yaw { get; init; }
        public required bool RemoveFromPose { get; init; }
    }

    private sealed class Descriptor
    {
        public int SampleRate { get; init; } = 30;
        public Dictionary<string, DescriptorClip> Clips { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, DescriptorSocket> Sockets { get; } = new(StringComparer.Ordinal);
    }

    private sealed class DescriptorClip
    {
        public int? SampleRate { get; init; }
        public bool Loop { get; init; }
        public List<DescriptorEvent> Events { get; } = [];
        public DescriptorRoot? Root { get; init; }
    }

    private sealed record DescriptorEvent(int TimeMilliseconds, string Name, int Value, int Order);
    private sealed record DescriptorRoot(string Node, uint TranslationAxes, bool Yaw, bool RemoveFromPose);
    private sealed record DescriptorSocket(string Node, Vector3 Translation, Quaternion Rotation, Vector3 Scale);

    private sealed class SourceChannel
    {
        public required int Node { get; init; }
        public required string Path { get; init; }
        public required string Interpolation { get; init; }
        public required float[] Times { get; init; }
        public required float[] Values { get; init; }
        public required int Components { get; init; }
    }

    private sealed record AnimationChunkOutput(string Id, byte[] Data, int Count, int Stride);

    private static List<AnimationChunkOutput> BuildAnimationChunks(AnimationModel animation,
        IReadOnlyList<Part> parts, StringTable strings)
    {
        var nodeNames = animation.Nodes.Select(node => strings.Add(node.Name)).ToArray();
        var clipNames = animation.Clips.Select(clip => strings.Add(clip.Name)).ToArray();
        var eventNames = animation.Events.Select(animationEvent => strings.Add(animationEvent.Name)).ToArray();
        var socketNames = animation.Sockets.Select(socket => strings.Add(socket.Name)).ToArray();

        var nodeBytes = new byte[animation.Nodes.Count * 64];
        for (var index = 0; index < animation.Nodes.Count; index++)
        {
            var node = animation.Nodes[index];
            var offset = index * 64;
            Write32(nodeBytes, offset, nodeNames[index]);
            Write32(nodeBytes, offset + 4, unchecked((uint)node.Parent));
            Write32(nodeBytes, offset + 8, node.Flags);
            WriteVector3(nodeBytes, offset + 16, node.Translation);
            WriteQuaternion(nodeBytes, offset + 28, node.Rotation);
            WriteVector3(nodeBytes, offset + 44, node.Scale);
        }

        var skinCount = parts.Sum(part => part.Vertices.Length / 12);
        var skinBytes = new byte[skinCount * 16];
        var skinVertex = 0;
        foreach (var part in parts)
        {
            Require(part.Joints != null && part.Weights != null,
                "SMA1387: animated parts require joints and weights before writing.");
            var joints = part.Joints!;
            var weights = part.Weights!;
            for (var vertex = 0; vertex < part.Vertices.Length / 12; vertex++, skinVertex++)
            {
                var offset = skinVertex * 16;
                for (var influence = 0; influence < 4; influence++)
                {
                    Write16(skinBytes, offset + influence * 2, joints[vertex * 4 + influence]);
                    Write16(skinBytes, offset + 8 + influence * 2, weights[vertex * 4 + influence]);
                }
            }
        }

        var boneBytes = new byte[animation.Bones.Count * 80];
        for (var index = 0; index < animation.Bones.Count; index++)
        {
            var bone = animation.Bones[index];
            var offset = index * 80;
            Write32(boneBytes, offset, (uint)bone.Node);
            Write32(boneBytes, offset + 4, unchecked((uint)bone.Parent));
            WriteMatrix(boneBytes, offset + 16, bone.InverseBind);
        }

        var clipBytes = new byte[animation.Clips.Count * 40];
        for (var index = 0; index < animation.Clips.Count; index++)
        {
            var clip = animation.Clips[index];
            var offset = index * 40;
            Write32(clipBytes, offset, clipNames[index]);
            Write32(clipBytes, offset + 4, (uint)clip.DurationMilliseconds);
            Write32(clipBytes, offset + 8, (uint)clip.SampleRate);
            Write32(clipBytes, offset + 12, (uint)clip.SampleCount);
            Write32(clipBytes, offset + 16, (uint)clip.FirstTrack);
            Write32(clipBytes, offset + 20, (uint)clip.TrackCount);
            Write32(clipBytes, offset + 24, (uint)clip.FirstEvent);
            Write32(clipBytes, offset + 28, (uint)clip.EventCount);
            Write32(clipBytes, offset + 32, clip.Loop ? 1U : 0U);
            Write32(clipBytes, offset + 36, Reference(clip.RootIndex));
        }

        var frames = new List<float>();
        var trackBytes = new byte[animation.Tracks.Count * 48];
        for (var index = 0; index < animation.Tracks.Count; index++)
        {
            var track = animation.Tracks[index];
            var offset = index * 48;
            var flags = 0U;
            Write32(trackBytes, offset, (uint)track.Clip);
            Write32(trackBytes, offset + 4, (uint)track.Node);
            WriteTrackChannel(trackBytes, offset + 16, track.Translation, 3, 1, 2, frames, ref flags);
            WriteTrackChannel(trackBytes, offset + 24, track.Rotation, 4, 4, 8, frames, ref flags);
            WriteTrackChannel(trackBytes, offset + 32, track.Scale, 3, 16, 32, frames, ref flags);
            Write32(trackBytes, offset + 8, flags);
        }
        var frameBytes = new byte[frames.Count * 4];
        for (var index = 0; index < frames.Count; index++) WriteFloat(frameBytes, index * 4, frames[index]);

        var eventBytes = new byte[animation.Events.Count * 20];
        for (var index = 0; index < animation.Events.Count; index++)
        {
            var animationEvent = animation.Events[index];
            var offset = index * 20;
            Write32(eventBytes, offset, (uint)animationEvent.Clip);
            Write32(eventBytes, offset + 4, (uint)animationEvent.TimeMilliseconds);
            Write32(eventBytes, offset + 8, eventNames[index]);
            Write32(eventBytes, offset + 12, unchecked((uint)animationEvent.Value));
            Write32(eventBytes, offset + 16, (uint)animationEvent.Order);
        }

        var socketBytes = new byte[animation.Sockets.Count * 64];
        for (var index = 0; index < animation.Sockets.Count; index++)
        {
            var socket = animation.Sockets[index];
            var offset = index * 64;
            Write32(socketBytes, offset, socketNames[index]);
            Write32(socketBytes, offset + 4, (uint)socket.Node);
            WriteVector3(socketBytes, offset + 16, socket.Translation);
            WriteQuaternion(socketBytes, offset + 28, socket.Rotation);
            WriteVector3(socketBytes, offset + 44, socket.Scale);
        }

        var rootBytes = new byte[animation.Roots.Count * 24];
        for (var index = 0; index < animation.Roots.Count; index++)
        {
            var root = animation.Roots[index];
            var offset = index * 24;
            Write32(rootBytes, offset, (uint)root.Clip);
            Write32(rootBytes, offset + 4, (uint)root.Node);
            Write32(rootBytes, offset + 8, root.TranslationAxes);
            Write32(rootBytes, offset + 12, root.Yaw ? 1U : 0U);
            Write32(rootBytes, offset + 16, root.RemoveFromPose ? 1U : 0U);
        }

        var result = new List<AnimationChunkOutput>
        {
            new("NODE", nodeBytes, animation.Nodes.Count, 64),
            new("SKIN", skinBytes, skinCount, 16),
            new("SKEL", boneBytes, animation.Bones.Count, 80),
            new("CLIP", clipBytes, animation.Clips.Count, 40),
            new("TRAK", trackBytes, animation.Tracks.Count, 48),
            new("AFRM", frameBytes, frames.Count, 4),
            new("EVNT", eventBytes, animation.Events.Count, 20),
            new("SOCK", socketBytes, animation.Sockets.Count, 64),
            new("ROOT", rootBytes, animation.Roots.Count, 24)
        };
        animation.PayloadBytes = result.Sum(chunk => chunk.Data.Length);
        return result;
    }

    private static void WriteTrackChannel(Span<byte> output, int offset, float[]? values, int components,
        uint presentFlag, uint sampledFlag, List<float> frames, ref uint flags)
    {
        if (values == null)
        {
            Write32(output, offset, NoReference);
            return;
        }
        Require(values.Length >= components && values.Length % components == 0,
            "SMA1388: animation track channel payload is invalid.");
        flags |= presentFlag;
        if (values.Length > components) flags |= sampledFlag;
        Write32(output, offset, (uint)frames.Count);
        Write32(output, offset + 4, (uint)(values.Length / components));
        frames.AddRange(values);
    }

    private static void WriteVector3(Span<byte> output, int offset, Vector3 value)
    {
        WriteFloat(output, offset, value.X);
        WriteFloat(output, offset + 4, value.Y);
        WriteFloat(output, offset + 8, value.Z);
    }

    private static void WriteQuaternion(Span<byte> output, int offset, Quaternion value)
    {
        WriteFloat(output, offset, value.X);
        WriteFloat(output, offset + 4, value.Y);
        WriteFloat(output, offset + 8, value.Z);
        WriteFloat(output, offset + 12, value.W);
    }

    private static void WriteMatrix(Span<byte> output, int offset, Matrix4x4 value)
    {
        var fields = new[] { value.M11, value.M12, value.M13, value.M14, value.M21, value.M22, value.M23,
            value.M24, value.M31, value.M32, value.M33, value.M34, value.M41, value.M42, value.M43, value.M44 };
        for (var index = 0; index < fields.Length; index++) WriteFloat(output, offset + index * 4, fields[index]);
    }

    private static AnimationModel ParseAnimationChunks(byte[] bytes, IReadOnlyDictionary<string, Chunk> chunks,
        Chunk strings, IReadOnlyList<Part> parts, int vertexCount)
    {
        Chunk Get(string id, int maximum, int stride)
        {
            var chunk = chunks[id];
            Require(chunk.Flags == ChunkOptional && chunk.Count >= 0 && chunk.Count <= maximum &&
                chunk.Stride == stride && chunk.Length == checked(chunk.Count * stride),
                $"SMA1389: animation chunk '{id}' count, stride, length, or flags are invalid.");
            return chunk;
        }

        var nodeChunk = Get("NODE", MaximumAnimationNodes, 64);
        var skinChunk = Get("SKIN", MaximumVertices, 16);
        var boneChunk = Get("SKEL", MaximumAnimationBones, 80);
        var clipChunk = Get("CLIP", MaximumAnimationClips, 40);
        var trackChunk = Get("TRAK", MaximumAnimationNodes * MaximumAnimationClips, 48);
        var frameChunk = Get("AFRM", MaximumFileBytes / 4, 4);
        var eventChunk = Get("EVNT", MaximumAnimationClips * MaximumAnimationEventsPerClip, 20);
        var socketChunk = Get("SOCK", MaximumAnimationSockets, 64);
        var rootChunk = Get("ROOT", MaximumAnimationClips, 24);
        Require(nodeChunk.Count >= 1 && boneChunk.Count >= 1 && clipChunk.Count >= 1 &&
            skinChunk.Count == vertexCount && frameChunk.Count >= 1,
            "SMA1390: animation chunks require nodes, bones, clips, samples, and one skin record per vertex.");

        var nodes = new List<AnimationNode>(nodeChunk.Count);
        var nodeNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < nodeChunk.Count; index++)
        {
            var offset = nodeChunk.Offset + index * 64;
            var name = ReadString(bytes, strings, Read32(bytes, offset));
            ValidateAnimationName(name, "node");
            Require(nodeNames.Add(name), "SMA1391: animation node names must be unique.");
            var parent = unchecked((int)Read32(bytes, offset + 4));
            var flags = Read32(bytes, offset + 8);
            Require(parent >= -1 && parent < index && (flags & ~3U) == 0 &&
                Read32(bytes, offset + 12) == 0 && Read32(bytes, offset + 56) == 0 &&
                Read32(bytes, offset + 60) == 0,
                "SMA1392: animation node hierarchy, flags, or reserved fields are invalid.");
            var translation = ReadVector3(bytes, offset + 16, "node translation");
            var rotation = ReadStoredQuaternion(bytes, offset + 28, "node rotation");
            var scale = ReadVector3(bytes, offset + 44, "node scale");
            Require(scale.X > 0 && scale.Y > 0 && scale.Z > 0 &&
                MathF.Abs(scale.X - scale.Y) <= BasisTolerance && MathF.Abs(scale.X - scale.Z) <= BasisTolerance,
                "SMA1393: animation node scale must be positive and uniform.");
            nodes.Add(new AnimationNode { SourceIndex = index, Name = name, Parent = parent,
                Translation = translation, Rotation = rotation, Scale = scale, Flags = flags });
        }

        var bones = new List<AnimationBone>(boneChunk.Count);
        var boneNodes = new HashSet<int>();
        var rootBones = 0;
        for (var index = 0; index < boneChunk.Count; index++)
        {
            var offset = boneChunk.Offset + index * 80;
            var node = checked((int)Read32(bytes, offset));
            var parent = unchecked((int)Read32(bytes, offset + 4));
            Require(node >= 0 && node < nodes.Count && boneNodes.Add(node) && parent >= -1 && parent < index &&
                Read32(bytes, offset + 8) == 0 && Read32(bytes, offset + 12) == 0,
                "SMA1394: animation bone node, parent, flags, or reserved fields are invalid.");
            if (parent < 0) rootBones++;
            var matrix = ReadMatrix(bytes, offset + 16, "inverse bind matrix");
            bones.Add(new AnimationBone { Node = node, Parent = parent, InverseBind = matrix });
        }
        Require(rootBones == 1 && bones.All(bone => (nodes[bone.Node].Flags & 1) != 0),
            "SMA1395: animation bones require one root and matching joint nodes.");

        var clips = new List<AnimationClip>(clipChunk.Count);
        var clipNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < clipChunk.Count; index++)
        {
            var offset = clipChunk.Offset + index * 40;
            var name = ReadString(bytes, strings, Read32(bytes, offset));
            ValidateAnimationName(name, "clip");
            Require(clipNames.Add(name), "SMA1396: animation clip names must be unique.");
            var duration = checked((int)Read32(bytes, offset + 4));
            var rate = checked((int)Read32(bytes, offset + 8));
            var samples = checked((int)Read32(bytes, offset + 12));
            var firstTrack = checked((int)Read32(bytes, offset + 16));
            var trackCount = checked((int)Read32(bytes, offset + 20));
            var firstEvent = checked((int)Read32(bytes, offset + 24));
            var eventCount = checked((int)Read32(bytes, offset + 28));
            var flags = Read32(bytes, offset + 32);
            var root = Read32(bytes, offset + 36);
            Require(duration is >= 1 and <= MaximumAnimationDurationMilliseconds &&
                rate is >= 15 and <= MaximumAnimationSampleRate && samples >= 2 &&
                samples == checked((int)Math.Ceiling(duration / 1000d * rate) + 1) &&
                firstTrack >= 0 && trackCount >= 0 && firstTrack <= trackChunk.Count &&
                trackCount <= trackChunk.Count - firstTrack && firstEvent >= 0 &&
                eventCount is >= 0 and <= MaximumAnimationEventsPerClip && firstEvent <= eventChunk.Count &&
                eventCount <= eventChunk.Count - firstEvent && flags <= 1 &&
                (root == NoReference || root < rootChunk.Count),
                "SMA1397: animation clip metadata or ranges are invalid.");
            clips.Add(new AnimationClip { Name = name, DurationMilliseconds = duration, SampleRate = rate,
                SampleCount = samples, Loop = flags != 0, FirstTrack = firstTrack, TrackCount = trackCount,
                FirstEvent = firstEvent, EventCount = eventCount,
                RootIndex = root == NoReference ? -1 : checked((int)root) });
        }

        var frameValues = new float[frameChunk.Count];
        for (var index = 0; index < frameValues.Length; index++)
            frameValues[index] = ReadFinite(bytes, frameChunk.Offset + index * 4, float.NegativeInfinity,
                float.PositiveInfinity, "animation sample");
        var usedFrames = new bool[frameValues.Length];
        var tracks = new List<AnimationTrack>(trackChunk.Count);
        var trackKeys = new HashSet<long>();
        for (var index = 0; index < trackChunk.Count; index++)
        {
            var offset = trackChunk.Offset + index * 48;
            var clipIndex = checked((int)Read32(bytes, offset));
            var nodeIndex = checked((int)Read32(bytes, offset + 4));
            var flags = Read32(bytes, offset + 8);
            Require(clipIndex >= 0 && clipIndex < clips.Count && nodeIndex >= 0 && nodeIndex < nodes.Count &&
                (flags & ~63U) == 0 && Read32(bytes, offset + 12) == 0 &&
                Read32(bytes, offset + 40) == 0 && Read32(bytes, offset + 44) == 0 &&
                trackKeys.Add(((long)clipIndex << 32) | (uint)nodeIndex),
                "SMA1398: animation track identity, flags, or reserved fields are invalid.");
            var clip = clips[clipIndex];
            Require(index >= clip.FirstTrack && index < clip.FirstTrack + clip.TrackCount,
                "SMA1399: animation track is outside its owning clip range.");
            var translation = ReadTrackChannel(bytes, offset + 16, 3, flags, 1, 2, clip.SampleCount,
                frameValues, usedFrames, "translation");
            var rotation = ReadTrackChannel(bytes, offset + 24, 4, flags, 4, 8, clip.SampleCount,
                frameValues, usedFrames, "rotation");
            var scale = ReadTrackChannel(bytes, offset + 32, 3, flags, 16, 32, clip.SampleCount,
                frameValues, usedFrames, "scale");
            if (rotation != null)
                for (var sample = 0; sample < rotation.Length / 4; sample++)
                    _ = ReadStoredQuaternion(rotation, sample * 4, "animation rotation");
            if (scale != null)
                for (var sample = 0; sample < scale.Length / 3; sample++)
                    Require(scale[sample * 3] > 0 &&
                        MathF.Abs(scale[sample * 3] - scale[sample * 3 + 1]) <= BasisTolerance &&
                        MathF.Abs(scale[sample * 3] - scale[sample * 3 + 2]) <= BasisTolerance,
                        "SMA1400: animation scale samples must be positive and uniform.");
            tracks.Add(new AnimationTrack { Clip = clipIndex, Node = nodeIndex, Translation = translation,
                Rotation = rotation, Scale = scale });
        }
        Require(usedFrames.All(value => value), "SMA1401: animation sample payload contains unreferenced values.");
        for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            for (var trackIndex = clips[clipIndex].FirstTrack;
                 trackIndex < clips[clipIndex].FirstTrack + clips[clipIndex].TrackCount; trackIndex++)
                Require(tracks[trackIndex].Clip == clipIndex,
                    "SMA1402: animation clip track ranges are not contiguous.");

        var events = new List<AnimationEvent>(eventChunk.Count);
        var priorTime = -1;
        var priorOrder = -1;
        var priorClip = -1;
        for (var index = 0; index < eventChunk.Count; index++)
        {
            var offset = eventChunk.Offset + index * 20;
            var clipIndex = checked((int)Read32(bytes, offset));
            var time = checked((int)Read32(bytes, offset + 4));
            var name = ReadString(bytes, strings, Read32(bytes, offset + 8));
            var value = unchecked((int)Read32(bytes, offset + 12));
            var order = checked((int)Read32(bytes, offset + 16));
            ValidateAnimationName(name, "event");
            Require(clipIndex >= 0 && clipIndex < clips.Count && time >= 0 &&
                time <= clips[clipIndex].DurationMilliseconds && order >= 0 &&
                index >= clips[clipIndex].FirstEvent &&
                index < clips[clipIndex].FirstEvent + clips[clipIndex].EventCount &&
                (clipIndex > priorClip || (clipIndex == priorClip &&
                    (time > priorTime || (time == priorTime && order > priorOrder)))),
                "SMA1403: animation events are out of range or deterministic order.");
            events.Add(new AnimationEvent { Clip = clipIndex, TimeMilliseconds = time, Name = name,
                Value = value, Order = order });
            priorClip = clipIndex; priorTime = time; priorOrder = order;
        }
        for (var clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            for (var eventIndex = clips[clipIndex].FirstEvent;
                 eventIndex < clips[clipIndex].FirstEvent + clips[clipIndex].EventCount; eventIndex++)
                Require(events[eventIndex].Clip == clipIndex,
                    "SMA1404: animation clip event ranges are not contiguous.");

        var sockets = new List<AnimationSocket>(socketChunk.Count);
        var socketNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < socketChunk.Count; index++)
        {
            var offset = socketChunk.Offset + index * 64;
            var name = ReadString(bytes, strings, Read32(bytes, offset));
            var node = checked((int)Read32(bytes, offset + 4));
            ValidateAnimationName(name, "socket");
            Require(socketNames.Add(name) && node >= 0 && node < nodes.Count &&
                Read32(bytes, offset + 8) == 0 && Read32(bytes, offset + 12) == 0 &&
                Read32(bytes, offset + 56) == 0 && Read32(bytes, offset + 60) == 0,
                "SMA1405: animation socket name, target, or reserved fields are invalid.");
            var scale = ReadVector3(bytes, offset + 44, "socket scale");
            Require(scale.X > 0 && scale.Y > 0 && scale.Z > 0,
                "SMA1406: animation socket scale must be positive.");
            sockets.Add(new AnimationSocket { Name = name, Node = node,
                Translation = ReadVector3(bytes, offset + 16, "socket translation"),
                Rotation = ReadStoredQuaternion(bytes, offset + 28, "socket rotation"), Scale = scale });
        }

        var roots = new List<AnimationRoot>(rootChunk.Count);
        var rootClips = new HashSet<int>();
        for (var index = 0; index < rootChunk.Count; index++)
        {
            var offset = rootChunk.Offset + index * 24;
            var clipIndex = checked((int)Read32(bytes, offset));
            var node = checked((int)Read32(bytes, offset + 4));
            var axes = Read32(bytes, offset + 8);
            var yaw = Read32(bytes, offset + 12);
            var remove = Read32(bytes, offset + 16);
            Require(clipIndex >= 0 && clipIndex < clips.Count && rootClips.Add(clipIndex) &&
                clips[clipIndex].RootIndex == index && node >= 0 && node < nodes.Count && axes is >= 1 and <= 7 &&
                yaw <= 1 && remove <= 1 && Read32(bytes, offset + 20) == 0,
                "SMA1407: root-motion metadata is invalid.");
            roots.Add(new AnimationRoot { Clip = clipIndex, Node = node, TranslationAxes = axes,
                Yaw = yaw != 0, RemoveFromPose = remove != 0 });
        }
        for (var index = 0; index < clips.Count; index++)
            Require(clips[index].RootIndex < 0 || rootClips.Contains(index),
                "SMA1408: clip root-motion references are incomplete.");

        var joints = new ushort[vertexCount * 4];
        var weights = new ushort[vertexCount * 4];
        for (var vertex = 0; vertex < vertexCount; vertex++)
        {
            var offset = skinChunk.Offset + vertex * 16;
            var total = 0;
            for (var influence = 0; influence < 4; influence++)
            {
                var joint = Read16(bytes, offset + influence * 2);
                var weight = Read16(bytes, offset + 8 + influence * 2);
                Require(joint < bones.Count && (weight != 0 || joint == 0),
                    "SMA1409: skin joint index or zero-weight normalization is invalid.");
                joints[vertex * 4 + influence] = joint;
                weights[vertex * 4 + influence] = weight;
                total += weight;
            }
            Require(total == 65535, "SMA1410: skin weights must sum to exactly 65535.");
        }
        var firstVertex = 0;
        foreach (var part in parts)
        {
            var count = part.Vertices.Length / 12;
            part.Joints = joints.AsSpan(firstVertex * 4, count * 4).ToArray();
            part.Weights = weights.AsSpan(firstVertex * 4, count * 4).ToArray();
            part.Skin = 0;
            firstVertex += count;
        }

        return new AnimationModel { Nodes = nodes, Bones = bones, Clips = clips, Tracks = tracks,
            Events = events, Sockets = sockets, Roots = roots,
            PayloadBytes = AnimationChunkIds.Sum(id => chunks[id].Length) };
    }

    private static float[]? ReadTrackChannel(byte[] bytes, int offset, int components, uint flags,
        uint presentFlag, uint sampledFlag, int sampleCount, float[] frameValues, bool[] usedFrames, string name)
    {
        var first = Read32(bytes, offset);
        var count = checked((int)Read32(bytes, offset + 4));
        var present = (flags & presentFlag) != 0;
        var sampled = (flags & sampledFlag) != 0;
        Require(present ? first != NoReference && count == (sampled ? sampleCount : 1) :
            first == NoReference && count == 0,
            $"SMA1411: animation {name} channel flags and count disagree.");
        if (!present) return null;
        var firstValue = checked((int)first);
        var valueCount = checked(count * components);
        Require(firstValue >= 0 && firstValue <= frameValues.Length && valueCount <= frameValues.Length - firstValue,
            $"SMA1412: animation {name} sample range is invalid.");
        for (var index = firstValue; index < firstValue + valueCount; index++)
        {
            Require(!usedFrames[index], $"SMA1413: animation {name} sample ranges overlap.");
            usedFrames[index] = true;
        }
        return frameValues.AsSpan(firstValue, valueCount).ToArray();
    }

    private static Vector3 ReadVector3(byte[] bytes, int offset, string name) => new(
        ReadFinite(bytes, offset, float.NegativeInfinity, float.PositiveInfinity, name),
        ReadFinite(bytes, offset + 4, float.NegativeInfinity, float.PositiveInfinity, name),
        ReadFinite(bytes, offset + 8, float.NegativeInfinity, float.PositiveInfinity, name));

    private static Quaternion ReadStoredQuaternion(byte[] bytes, int offset, string name)
    {
        var value = new Quaternion(ReadFinite(bytes, offset, -1, 1, name),
            ReadFinite(bytes, offset + 4, -1, 1, name), ReadFinite(bytes, offset + 8, -1, 1, name),
            ReadFinite(bytes, offset + 12, -1, 1, name));
        return ValidateStoredQuaternion(value, name);
    }

    private static Quaternion ReadStoredQuaternion(float[] values, int offset, string name) =>
        ValidateStoredQuaternion(new Quaternion(values[offset], values[offset + 1], values[offset + 2],
            values[offset + 3]), name);

    private static Quaternion ValidateStoredQuaternion(Quaternion value, string name)
    {
        Require(MathF.Abs(value.LengthSquared() - 1) <= BasisTolerance,
            $"SMA1414: {name} must be normalized.");
        Require(CanonicalQuaternionSign(value), $"SMA1416: {name} sign is not canonical.");
        return value;
    }

    private static Matrix4x4 ReadMatrix(byte[] bytes, int offset, string name)
    {
        Span<float> value = stackalloc float[16];
        for (var index = 0; index < value.Length; index++)
            value[index] = ReadFinite(bytes, offset + index * 4, float.NegativeInfinity, float.PositiveInfinity, name);
        return new Matrix4x4(value[0], value[1], value[2], value[3], value[4], value[5], value[6], value[7],
            value[8], value[9], value[10], value[11], value[12], value[13], value[14], value[15]);
    }

    private static ushort[] ReadJointAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, int expectedCount)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var componentType = Required(accessor, "componentType").GetInt32();
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) &&
            normalizedValue.ValueKind == JsonValueKind.True;
        Require(count == expectedCount && Required(accessor, "type").GetString() == "VEC4" &&
            componentType is 5121 or 5123 && !normalized,
            "SMA1306: JOINTS_0 must be non-normalized unsigned-byte or unsigned-short VEC4 data matching POSITION.");
        var size = componentType == 5121 ? 1 : 2;
        var stride = view.Stride == 0 ? size * 4 : view.Stride;
        Require(view.Target is 0 or 34962 && (view.Offset + offset) % size == 0 && stride >= size * 4 &&
            offset + (long)Math.Max(0, count - 1) * stride + size * 4 <= view.Length,
            "SMA1307: JOINTS_0 accessor range or stride is invalid.");
        var result = new ushort[count * 4];
        for (var vertex = 0; vertex < count; vertex++)
        {
            for (var influence = 0; influence < 4; influence++)
            {
                var source = view.Buffer.AsSpan(view.Offset + offset + vertex * stride + influence * size, size);
                result[vertex * 4 + influence] = componentType == 5121
                    ? source[0]
                    : BinaryPrimitives.ReadUInt16LittleEndian(source);
            }
        }
        return result;
    }

    private static ushort[] ReadWeightAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, int expectedCount)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var componentType = Required(accessor, "componentType").GetInt32();
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) &&
            normalizedValue.ValueKind == JsonValueKind.True;
        Require(count == expectedCount && Required(accessor, "type").GetString() == "VEC4" &&
            ((componentType == 5126 && !normalized) || (componentType is 5121 or 5123 && normalized)),
            "SMA1308: WEIGHTS_0 must be float or normalized unsigned-byte/unsigned-short VEC4 data matching POSITION.");
        var size = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4;
        var stride = view.Stride == 0 ? size * 4 : view.Stride;
        Require(view.Target is 0 or 34962 && (view.Offset + offset) % size == 0 && stride >= size * 4 &&
            offset + (long)Math.Max(0, count - 1) * stride + size * 4 <= view.Length,
            "SMA1309: WEIGHTS_0 accessor range or stride is invalid.");
        var result = new ushort[count * 4];
        Span<float> values = stackalloc float[4];
        for (var vertex = 0; vertex < count; vertex++)
        {
            var total = 0f;
            var largest = 0;
            for (var influence = 0; influence < 4; influence++)
            {
                var source = view.Buffer.AsSpan(view.Offset + offset + vertex * stride + influence * size, size);
                var value = componentType switch
                {
                    5121 => source[0] / 255f,
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(source) / 65535f,
                    _ => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(source))
                };
                Require(float.IsFinite(value) && value >= 0, "SMA1310: WEIGHTS_0 contains a negative or non-finite value.");
                values[influence] = value;
                total += value;
                if (value > values[largest]) largest = influence;
            }
            Require(float.IsFinite(total) && total > 0, "SMA1311: WEIGHTS_0 contains a zero-sum vertex.");
            var quantizedTotal = 0;
            for (var influence = 0; influence < 4; influence++)
            {
                var quantized = checked((int)MathF.Round(values[influence] / total * 65535f,
                    MidpointRounding.AwayFromZero));
                quantized = Math.Clamp(quantized, 0, 65535);
                result[vertex * 4 + influence] = (ushort)quantized;
                quantizedTotal += quantized;
            }
            var corrected = result[vertex * 4 + largest] + (65535 - quantizedTotal);
            Require(corrected is >= 0 and <= 65535, "SMA1312: WEIGHTS_0 normalization overflowed.");
            result[vertex * 4 + largest] = (ushort)corrected;
            Require(result.AsSpan(vertex * 4, 4).ToArray().Sum(value => value) == 65535,
                "SMA1313: WEIGHTS_0 normalization did not produce an exact sum.");
        }
        return result;
    }

    private static AnimationModel ReadAnimation(JsonElement root, string? descriptorPath, JsonElement nodes,
        JsonElement activeNodes, JsonElement accessors, IReadOnlyList<BufferView> views, List<Part> parts)
    {
        var descriptor = ReadDescriptor(descriptorPath);
        var sourceParents = Enumerable.Repeat(-2, nodes.GetArrayLength()).ToArray();
        var retainedSources = new List<int>();

        void RetainNode(int source, int parent)
        {
            Require(source >= 0 && source < nodes.GetArrayLength(), "SMA1314: animation node index is invalid.");
            Require(sourceParents[source] == -2,
                "SMA1315: the animation node hierarchy must be a single-parent acyclic tree.");
            sourceParents[source] = parent;
            retainedSources.Add(source);
            Require(retainedSources.Count <= MaximumAnimationNodes,
                $"SMA1316: animated models support at most {MaximumAnimationNodes} retained nodes.");
            var node = nodes[source];
            if (!node.TryGetProperty("children", out var children)) return;
            foreach (var child in children.EnumerateArray()) RetainNode(child.GetInt32(), source);
        }

        foreach (var rootNode in activeNodes.EnumerateArray()) RetainNode(rootNode.GetInt32(), -1);
        var sourceToRetained = Enumerable.Repeat(-1, nodes.GetArrayLength()).ToArray();
        for (var index = 0; index < retainedSources.Count; index++) sourceToRetained[retainedSources[index]] = index;
        var retainedNodes = new List<AnimationNode>(retainedSources.Count);
        foreach (var source in retainedSources)
        {
            var node = nodes[source];
            var transform = ReadNodeTransform(node);
            Require(Matrix4x4.Decompose(transform, out var scale, out var rotation, out var translation),
                "SMA1317: animation node transform cannot be represented as TRS.");
            Require(Finite(scale) && scale.X > 0 && scale.Y > 0 && scale.Z > 0 &&
                MathF.Abs(scale.X - scale.Y) <= 0.0001f && MathF.Abs(scale.X - scale.Z) <= 0.0001f,
                "SMA1318: production animation bind scale must be positive and uniform.");
            rotation = CanonicalQuaternion(rotation, "SMA1319: animation node rotation is invalid.");
            retainedNodes.Add(new AnimationNode
            {
                SourceIndex = source,
                Name = OptionalName(node, $"Node {source + 1}"),
                Parent = sourceParents[source] < 0 ? -1 : sourceToRetained[sourceParents[source]],
                Translation = ToAnimationTranslation(translation),
                Rotation = ToAnimationRotation(rotation),
                Scale = scale
            });
        }

        var skin = Required(root, "skins")[0];
        Require(skin.ValueKind == JsonValueKind.Object, "SMA1320: skin must be an object.");
        RejectExtensions(skin, "skin");
        var joints = Required(skin, "joints");
        Require(joints.ValueKind == JsonValueKind.Array &&
            joints.GetArrayLength() is >= 1 and <= MaximumAnimationBones,
            $"SMA1321: a skin must contain 1 to {MaximumAnimationBones} joints.");
        var jointSources = joints.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        Require(jointSources.Distinct().Count() == jointSources.Length,
            "SMA1322: a skin may not contain duplicate joints.");
        foreach (var source in jointSources)
            Require(source >= 0 && source < sourceToRetained.Length && sourceToRetained[source] >= 0,
                "SMA1323: every skin joint must be reachable from the active scene.");
        if (skin.TryGetProperty("skeleton", out var skeletonValue))
        {
            var skeletonSource = skeletonValue.GetInt32();
            Require(skeletonSource >= 0 && skeletonSource < sourceToRetained.Length &&
                sourceToRetained[skeletonSource] >= 0,
                "SMA1324: skin skeleton root must be reachable.");
        }
        var inverseAccessor = Required(skin, "inverseBindMatrices").GetInt32();
        var inverseBind = ReadMatrixAccessor(accessors, views, inverseAccessor, jointSources.Length);
        var jointSet = jointSources.ToHashSet();
        var runtimeJointSources = retainedSources.Where(jointSet.Contains).ToArray();
        var sourceJointOrdinal = new Dictionary<int, int>();
        for (var ordinal = 0; ordinal < jointSources.Length; ordinal++) sourceJointOrdinal.Add(jointSources[ordinal], ordinal);
        var sourceToBone = new Dictionary<int, int>();
        for (var bone = 0; bone < runtimeJointSources.Length; bone++) sourceToBone.Add(runtimeJointSources[bone], bone);
        var bones = new List<AnimationBone>(runtimeJointSources.Length);
        var rootBones = 0;
        foreach (var source in runtimeJointSources)
        {
            var parentSource = sourceParents[source];
            while (parentSource >= 0 && !sourceToBone.ContainsKey(parentSource)) parentSource = sourceParents[parentSource];
            var parentBone = parentSource < 0 ? -1 : sourceToBone[parentSource];
            if (parentBone < 0) rootBones++;
            var sourceOrdinal = sourceJointOrdinal[source];
            bones.Add(new AnimationBone
            {
                Node = sourceToRetained[source],
                Parent = parentBone,
                InverseBind = ReflectMatrix(inverseBind[sourceOrdinal])
            });
            retainedNodes[sourceToRetained[source]].Flags |= 1;
        }
        Require(rootBones == 1, "SMA1325: skin joints must form one rooted hierarchy.");

        foreach (var part in parts)
        {
            Require(part.Skin == 0 && part.Joints != null && part.Weights != null,
                "SMA1326: every animated model part must use the one production skin.");
            var partJoints = part.Joints!;
            var partWeights = part.Weights!;
            for (var influence = 0; influence < partJoints.Length; influence++)
            {
                var sourceOrdinal = partJoints[influence];
                Require(sourceOrdinal < jointSources.Length, "SMA1327: JOINTS_0 references a joint outside the skin.");
                partJoints[influence] = (ushort)sourceToBone[jointSources[sourceOrdinal]];
                if (partWeights[influence] == 0) partJoints[influence] = 0;
            }
        }

        var clips = new List<AnimationClip>();
        var tracks = new List<AnimationTrack>();
        var events = new List<AnimationEvent>();
        var roots = new List<AnimationRoot>();
        ReadClips(root, descriptor, nodes, sourceToRetained, accessors, views, clips, tracks, events, roots);
        var sockets = ReadSockets(descriptor, retainedNodes);
        return new AnimationModel
        {
            Nodes = retainedNodes,
            Bones = bones,
            Clips = clips,
            Tracks = tracks,
            Events = events,
            Sockets = sockets,
            Roots = roots
        };
    }

    private static Descriptor ReadDescriptor(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return new Descriptor();
        var fullPath = Path.GetFullPath(path);
        Require(File.Exists(fullPath), "SMA1328: animation descriptor file was not found.");
        var length = new FileInfo(fullPath).Length;
        Require(length is >= 2 and <= 1024 * 1024,
            "SMA1329: animation descriptor must use 2 bytes through 1 MiB.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(fullPath), new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 32
        });
        var root = document.RootElement;
        Require(root.ValueKind == JsonValueKind.Object, "SMA1330: animation descriptor root must be an object.");
        RequireOnly(root, "descriptor", "version", "sampleRate", "clips", "sockets");
        Require(Required(root, "version").GetInt32() == 1, "SMA1331: animation descriptor version must be 1.");
        var sampleRate = root.TryGetProperty("sampleRate", out var sampleRateValue)
            ? sampleRateValue.GetInt32()
            : 30;
        Require(sampleRate is >= 15 and <= MaximumAnimationSampleRate,
            $"SMA1332: descriptor sampleRate must be 15 through {MaximumAnimationSampleRate}.");
        var descriptor = new Descriptor { SampleRate = sampleRate };

        if (root.TryGetProperty("clips", out var clips))
        {
            Require(clips.ValueKind == JsonValueKind.Object &&
                clips.EnumerateObject().Count() <= MaximumAnimationClips,
                $"SMA1333: descriptor clips must be an object with at most {MaximumAnimationClips} entries.");
            foreach (var property in clips.EnumerateObject())
            {
                ValidateAnimationName(property.Name, "clip");
                var value = property.Value;
                Require(value.ValueKind == JsonValueKind.Object, "SMA1334: each descriptor clip must be an object.");
                RequireOnly(value, $"clip '{property.Name}'", "sampleRate", "loop", "events", "rootMotion");
                var clipRate = value.TryGetProperty("sampleRate", out var clipRateValue)
                    ? clipRateValue.GetInt32()
                    : (int?)null;
                Require(clipRate == null || clipRate is >= 15 and <= MaximumAnimationSampleRate,
                    $"SMA1335: clip sampleRate must be 15 through {MaximumAnimationSampleRate}.");
                var loop = value.TryGetProperty("loop", out var loopValue) && GetBoolean(loopValue, "clip loop");
                var rootMotion = value.TryGetProperty("rootMotion", out var rootValue)
                    ? ReadDescriptorRoot(rootValue)
                    : null;
                var descriptorClip = new DescriptorClip
                {
                    SampleRate = clipRate,
                    Loop = loop,
                    Root = rootMotion
                };
                if (value.TryGetProperty("events", out var eventValues))
                {
                    Require(eventValues.ValueKind == JsonValueKind.Array &&
                        eventValues.GetArrayLength() <= MaximumAnimationEventsPerClip,
                        $"SMA1336: each clip supports at most {MaximumAnimationEventsPerClip} events.");
                    var order = 0;
                    foreach (var eventValue in eventValues.EnumerateArray())
                    {
                        Require(eventValue.ValueKind == JsonValueKind.Object,
                            "SMA1337: each descriptor event must be an object.");
                        RequireOnly(eventValue, "event", "timeMs", "name", "value");
                        var time = Required(eventValue, "timeMs").GetInt32();
                        var name = Required(eventValue, "name").GetString() ?? string.Empty;
                        var eventPayload = eventValue.TryGetProperty("value", out var payloadValue)
                            ? payloadValue.GetInt32()
                            : 0;
                        Require(time >= 0, "SMA1338: event timeMs must be nonnegative.");
                        ValidateAnimationName(name, "event");
                        descriptorClip.Events.Add(new DescriptorEvent(time, name, eventPayload, order++));
                    }
                }
                descriptor.Clips.Add(property.Name, descriptorClip);
            }
        }

        if (root.TryGetProperty("sockets", out var sockets))
        {
            Require(sockets.ValueKind == JsonValueKind.Object &&
                sockets.EnumerateObject().Count() <= MaximumAnimationSockets,
                $"SMA1339: descriptor sockets must be an object with at most {MaximumAnimationSockets} entries.");
            foreach (var property in sockets.EnumerateObject())
            {
                ValidateAnimationName(property.Name, "socket");
                var value = property.Value;
                Require(value.ValueKind == JsonValueKind.Object, "SMA1340: each socket must be an object.");
                RequireOnly(value, $"socket '{property.Name}'", "node", "translation", "rotation", "scale");
                var node = Required(value, "node").GetString() ?? string.Empty;
                ValidateAnimationName(node, "socket node");
                var translation = value.TryGetProperty("translation", out var translationValue)
                    ? ReadVector3(translationValue, "socket translation")
                    : Vector3.Zero;
                var rotation = value.TryGetProperty("rotation", out var rotationValue)
                    ? ReadQuaternion(rotationValue, "socket rotation")
                    : Quaternion.Identity;
                var scale = value.TryGetProperty("scale", out var scaleValue)
                    ? ReadVector3(scaleValue, "socket scale")
                    : Vector3.One;
                Require(scale.X > 0 && scale.Y > 0 && scale.Z > 0 &&
                    MathF.Abs(scale.X - scale.Y) <= 0.0001f && MathF.Abs(scale.X - scale.Z) <= 0.0001f,
                    "SMA1341: socket scale must be positive and uniform.");
                descriptor.Sockets.Add(property.Name,
                    new DescriptorSocket(node, ToAnimationTranslation(translation),
                        ToAnimationRotation(rotation), scale));
            }
        }
        return descriptor;
    }

    private static DescriptorRoot ReadDescriptorRoot(JsonElement value)
    {
        Require(value.ValueKind == JsonValueKind.Object, "SMA1342: rootMotion must be an object.");
        RequireOnly(value, "rootMotion", "node", "translation", "yaw", "removeFromPose");
        var node = Required(value, "node").GetString() ?? string.Empty;
        ValidateAnimationName(node, "root-motion node");
        var axes = 0U;
        if (value.TryGetProperty("translation", out var translation))
        {
            Require(translation.ValueKind == JsonValueKind.Array && translation.GetArrayLength() <= 3,
                "SMA1343: rootMotion translation must contain at most X, Y, and Z.");
            foreach (var axisValue in translation.EnumerateArray())
            {
                var axis = axisValue.GetString();
                var bit = axis switch
                {
                    "X" => 1U,
                    "Y" => 2U,
                    "Z" => 4U,
                    _ => 0U
                };
                Require(bit != 0 && (axes & bit) == 0,
                    "SMA1344: rootMotion translation axes must be unique X, Y, or Z values.");
                axes |= bit;
            }
        }
        var yaw = value.TryGetProperty("yaw", out var yawValue) && GetBoolean(yawValue, "rootMotion yaw");
        var remove = value.TryGetProperty("removeFromPose", out var removeValue) &&
            GetBoolean(removeValue, "rootMotion removeFromPose");
        Require(axes != 0 || yaw, "SMA1345: rootMotion must extract translation or yaw.");
        return new DescriptorRoot(node, axes, yaw, remove);
    }

    private static void ReadClips(JsonElement root, Descriptor descriptor, JsonElement nodes,
        IReadOnlyList<int> sourceToRetained, JsonElement accessors, IReadOnlyList<BufferView> views,
        List<AnimationClip> clips, List<AnimationTrack> tracks, List<AnimationEvent> events,
        List<AnimationRoot> roots)
    {
        var animations = Required(root, "animations");
        var sourceNames = new HashSet<string>(StringComparer.Ordinal);
        for (var clipIndex = 0; clipIndex < animations.GetArrayLength(); clipIndex++)
        {
            var animation = animations[clipIndex];
            Require(animation.ValueKind == JsonValueKind.Object, "SMA1346: each animation must be an object.");
            RejectExtensions(animation, "animation");
            var name = animation.TryGetProperty("name", out var nameValue) ? nameValue.GetString() ?? string.Empty : string.Empty;
            ValidateAnimationName(name, "animation");
            Require(sourceNames.Add(name), "SMA1347: animation names must be unique.");
            descriptor.Clips.TryGetValue(name, out var descriptorClip);
            var channels = ReadSourceChannels(animation, accessors, views, nodes.GetArrayLength());
            Require(channels.Count > 0, "SMA1348: animation must contain at least one supported channel.");
            var durationSeconds = channels.Max(channel => channel.Times[^1]);
            Require(float.IsFinite(durationSeconds) && durationSeconds > 0 &&
                durationSeconds <= MaximumAnimationDurationMilliseconds / 1000f,
                $"SMA1349: clip duration must be positive and at most {MaximumAnimationDurationMilliseconds} ms.");
            var durationMilliseconds = checked((int)MathF.Round(durationSeconds * 1000f,
                MidpointRounding.AwayFromZero));
            Require(durationMilliseconds > 0 && durationMilliseconds <= MaximumAnimationDurationMilliseconds,
                "SMA1350: rounded clip duration is outside the supported range.");
            var sampleRate = descriptorClip?.SampleRate ?? descriptor.SampleRate;
            var sampleTimes = BuildSampleTimes(durationSeconds, sampleRate);
            var clip = new AnimationClip
            {
                Name = name,
                DurationMilliseconds = durationMilliseconds,
                SampleRate = sampleRate,
                SampleCount = sampleTimes.Length,
                Loop = descriptorClip?.Loop ?? false,
                FirstTrack = tracks.Count,
                FirstEvent = events.Count
            };

            foreach (var group in channels.GroupBy(channel => channel.Node).OrderBy(group => group.Key))
            {
                Require(group.Key >= 0 && group.Key < sourceToRetained.Count && sourceToRetained[group.Key] >= 0,
                    "SMA1351: animation channel targets an unreachable node.");
                float[]? translation = null;
                float[]? rotation = null;
                float[]? scale = null;
                foreach (var channel in group)
                {
                    var sampled = SampleChannel(channel, sampleTimes);
                    if (channel.Path == "translation")
                    {
                        for (var sample = 0; sample < sampled.Length / 3; sample++)
                            sampled[sample * 3 + 2] = -sampled[sample * 3 + 2];
                        translation = ElideConstant(sampled, 3);
                    }
                    else if (channel.Path == "rotation")
                    {
                        for (var sample = 0; sample < sampled.Length / 4; sample++)
                        {
                            var value = CanonicalQuaternion(new Quaternion(sampled[sample * 4],
                                sampled[sample * 4 + 1], sampled[sample * 4 + 2], sampled[sample * 4 + 3]),
                                "SMA1352: sampled animation rotation is invalid.");
                            value = ToAnimationRotation(value);
                            sampled[sample * 4] = value.X;
                            sampled[sample * 4 + 1] = value.Y;
                            sampled[sample * 4 + 2] = value.Z;
                            sampled[sample * 4 + 3] = value.W;
                        }
                        rotation = ElideConstant(sampled, 4);
                    }
                    else
                    {
                        for (var sample = 0; sample < sampled.Length / 3; sample++)
                        {
                            var x = sampled[sample * 3];
                            var y = sampled[sample * 3 + 1];
                            var z = sampled[sample * 3 + 2];
                            Require(x > 0 && y > 0 && z > 0 && MathF.Abs(x - y) <= 0.0001f &&
                                MathF.Abs(x - z) <= 0.0001f,
                                "SMA1353: production PBR animation scale must be positive and uniform.");
                        }
                        scale = ElideConstant(sampled, 3);
                    }
                }
                tracks.Add(new AnimationTrack
                {
                    Clip = clipIndex,
                    Node = sourceToRetained[group.Key],
                    Translation = translation,
                    Rotation = rotation,
                    Scale = scale
                });
            }
            clip.TrackCount = tracks.Count - clip.FirstTrack;

            foreach (var descriptorEvent in (descriptorClip?.Events ?? []).OrderBy(value => value.TimeMilliseconds)
                .ThenBy(value => value.Order))
            {
                Require(descriptorEvent.TimeMilliseconds <= durationMilliseconds,
                    "SMA1354: event time exceeds its clip duration.");
                events.Add(new AnimationEvent
                {
                    Clip = clipIndex,
                    TimeMilliseconds = descriptorEvent.TimeMilliseconds,
                    Name = descriptorEvent.Name,
                    Value = descriptorEvent.Value,
                    Order = descriptorEvent.Order
                });
            }
            clip.EventCount = events.Count - clip.FirstEvent;
            if (descriptorClip?.Root != null)
            {
                var nodeIndex = UniqueNodeIndex(nodes, descriptorClip.Root.Node);
                Require(sourceToRetained[nodeIndex] >= 0, "SMA1355: root-motion node is unreachable.");
                clip.RootIndex = roots.Count;
                roots.Add(new AnimationRoot
                {
                    Clip = clipIndex,
                    Node = sourceToRetained[nodeIndex],
                    TranslationAxes = descriptorClip.Root.TranslationAxes,
                    Yaw = descriptorClip.Root.Yaw,
                    RemoveFromPose = descriptorClip.Root.RemoveFromPose
                });
            }
            clips.Add(clip);
        }
        foreach (var descriptorName in descriptor.Clips.Keys)
            Require(sourceNames.Contains(descriptorName),
                $"SMA1356: descriptor clip '{descriptorName}' does not match an imported animation.");
    }

    private static List<SourceChannel> ReadSourceChannels(JsonElement animation, JsonElement accessors,
        IReadOnlyList<BufferView> views, int nodeCount)
    {
        var samplers = Required(animation, "samplers");
        var channels = Required(animation, "channels");
        Require(samplers.ValueKind == JsonValueKind.Array && samplers.GetArrayLength() is >= 1 and <= 4096,
            "SMA1357: animation samplers must be a bounded nonempty array.");
        Require(channels.ValueKind == JsonValueKind.Array && channels.GetArrayLength() is >= 1 and <= 4096,
            "SMA1358: animation channels must be a bounded nonempty array.");
        var result = new List<SourceChannel>();
        var targets = new HashSet<(int Node, string Path)>();
        foreach (var channel in channels.EnumerateArray())
        {
            Require(channel.ValueKind == JsonValueKind.Object, "SMA1359: each animation channel must be an object.");
            RejectExtensions(channel, "animation channel");
            var samplerIndex = Required(channel, "sampler").GetInt32();
            Require(samplerIndex >= 0 && samplerIndex < samplers.GetArrayLength(),
                "SMA1360: animation channel sampler index is invalid.");
            var target = Required(channel, "target");
            Require(target.ValueKind == JsonValueKind.Object, "SMA1361: animation target must be an object.");
            RejectExtensions(target, "animation target");
            var node = Required(target, "node").GetInt32();
            var path = Required(target, "path").GetString() ?? string.Empty;
            Require(node >= 0 && node < nodeCount && path is "translation" or "rotation" or "scale",
                "SMA1362: animation target node/path is unsupported.");
            Require(targets.Add((node, path)), "SMA1363: animation contains a duplicate node/path channel.");
            var sampler = samplers[samplerIndex];
            Require(sampler.ValueKind == JsonValueKind.Object, "SMA1364: animation sampler must be an object.");
            RequireOnly(sampler, "animation sampler", "input", "output", "interpolation");
            var interpolation = sampler.TryGetProperty("interpolation", out var interpolationValue)
                ? interpolationValue.GetString() ?? string.Empty
                : "LINEAR";
            Require(interpolation is "LINEAR" or "STEP",
                "SMA1365: CUBICSPLINE is not supported; export sampled LINEAR or STEP animation.");
            var times = ReadScalarFloatAccessor(accessors, views, Required(sampler, "input").GetInt32(),
                "animation input");
            for (var index = 0; index < times.Length; index++)
                Require(times[index] >= 0 && (index == 0 || times[index] > times[index - 1]),
                    "SMA1366: animation input times must be finite, nonnegative, and strictly increasing.");
            var components = path == "rotation" ? 4 : 3;
            var values = ReadFloatAccessor(accessors, views, Required(sampler, "output").GetInt32(),
                components, $"animation {path}", 1000000);
            Require(values.Length == times.Length * components,
                "SMA1367: animation output count must match input times.");
            result.Add(new SourceChannel
            {
                Node = node,
                Path = path,
                Interpolation = interpolation,
                Times = times,
                Values = values,
                Components = components
            });
        }
        return result;
    }

    private static float[] ReadScalarFloatAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, string semantic)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) &&
            normalizedValue.ValueKind == JsonValueKind.True;
        Require(Required(accessor, "componentType").GetInt32() == 5126 &&
            Required(accessor, "type").GetString() == "SCALAR" && !normalized,
            $"SMA1368: {semantic} must use non-normalized float SCALAR data.");
        var stride = view.Stride == 0 ? 4 : view.Stride;
        Require(view.Target == 0 && (view.Offset + offset) % 4 == 0 && stride >= 4 &&
            offset + (long)Math.Max(0, count - 1) * stride + 4 <= view.Length,
            $"SMA1369: {semantic} accessor range or stride is invalid.");
        var result = new float[count];
        for (var index = 0; index < count; index++)
        {
            result[index] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(
                view.Buffer.AsSpan(view.Offset + offset + index * stride, 4)));
            Require(float.IsFinite(result[index]), $"SMA1370: {semantic} contains a non-finite value.");
        }
        return result;
    }

    private static Matrix4x4[] ReadMatrixAccessor(JsonElement accessors, IReadOnlyList<BufferView> views,
        int accessorIndex, int expectedCount)
    {
        var accessor = Accessor(accessors, accessorIndex, views, out var view, out var offset, out var count);
        var normalized = accessor.TryGetProperty("normalized", out var normalizedValue) &&
            normalizedValue.ValueKind == JsonValueKind.True;
        Require(count == expectedCount && Required(accessor, "componentType").GetInt32() == 5126 &&
            Required(accessor, "type").GetString() == "MAT4" && !normalized,
            "SMA1371: inverseBindMatrices must be non-normalized float MAT4 data matching the joint count.");
        var stride = view.Stride == 0 ? 64 : view.Stride;
        Require(view.Target == 0 && (view.Offset + offset) % 4 == 0 && stride >= 64 &&
            offset + (long)Math.Max(0, count - 1) * stride + 64 <= view.Length,
            "SMA1372: inverseBindMatrices accessor range or stride is invalid.");
        var result = new Matrix4x4[count];
        Span<float> values = stackalloc float[16];
        for (var index = 0; index < count; index++)
        {
            for (var field = 0; field < 16; field++)
            {
                values[field] = BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(
                    view.Buffer.AsSpan(view.Offset + offset + index * stride + field * 4, 4)));
                Require(float.IsFinite(values[field]), "SMA1373: inverse bind matrix contains a non-finite value.");
            }
            var matrix = new Matrix4x4(values[0], values[1], values[2], values[3], values[4], values[5],
                values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13],
                values[14], values[15]);
            Require(Matrix4x4.Invert(matrix, out _) && MathF.Abs(matrix.GetDeterminant()) > 1e-8f,
                "SMA1374: inverse bind matrix must be finite and nonsingular.");
            result[index] = matrix;
        }
        return result;
    }

    private static float[] BuildSampleTimes(float durationSeconds, int sampleRate)
    {
        var regular = checked((int)MathF.Floor(durationSeconds * sampleRate));
        var result = new List<float>(regular + 2);
        for (var sample = 0; sample <= regular; sample++) result.Add(sample / (float)sampleRate);
        if (result[^1] < durationSeconds - 0.000001f) result.Add(durationSeconds);
        else result[^1] = durationSeconds;
        return result.ToArray();
    }

    private static float[] SampleChannel(SourceChannel channel, float[] sampleTimes)
    {
        var result = new float[sampleTimes.Length * channel.Components];
        var interval = 0;
        for (var sample = 0; sample < sampleTimes.Length; sample++)
        {
            var time = sampleTimes[sample];
            while (interval + 1 < channel.Times.Length && channel.Times[interval + 1] < time) interval++;
            var next = Math.Min(interval + 1, channel.Times.Length - 1);
            var amount = next == interval || channel.Interpolation == "STEP"
                ? 0f
                : Math.Clamp((time - channel.Times[interval]) /
                    (channel.Times[next] - channel.Times[interval]), 0, 1);
            if (channel.Path == "rotation")
            {
                var first = CanonicalQuaternion(ReadQuaternion(channel.Values, interval),
                    "SMA1375: animation rotation key is invalid.");
                var second = CanonicalQuaternion(ReadQuaternion(channel.Values, next),
                    "SMA1375: animation rotation key is invalid.");
                var value = channel.Interpolation == "STEP"
                    ? first
                    : Quaternion.Normalize(Quaternion.Slerp(first, second, amount));
                result[sample * 4] = value.X;
                result[sample * 4 + 1] = value.Y;
                result[sample * 4 + 2] = value.Z;
                result[sample * 4 + 3] = value.W;
            }
            else
            {
                for (var component = 0; component < channel.Components; component++)
                {
                    var first = channel.Values[interval * channel.Components + component];
                    var second = channel.Values[next * channel.Components + component];
                    result[sample * channel.Components + component] = first + (second - first) * amount;
                }
            }
        }
        return result;
    }

    private static float[] ElideConstant(float[] values, int components)
    {
        var constant = true;
        for (var sample = 1; sample < values.Length / components && constant; sample++)
            for (var component = 0; component < components; component++)
                if (MathF.Abs(values[sample * components + component] - values[component]) > 0.000001f)
                {
                    constant = false;
                    break;
                }
        return constant ? values.AsSpan(0, components).ToArray() : values;
    }

    private static List<AnimationSocket> ReadSockets(Descriptor descriptor, IReadOnlyList<AnimationNode> nodes)
    {
        var result = new List<AnimationSocket>();
        foreach (var property in descriptor.Sockets.OrderBy(value => value.Key, StringComparer.Ordinal))
        {
            var matches = nodes.Select((node, index) => (node, index))
                .Where(value => value.node.Name == property.Value.Node).ToArray();
            Require(matches.Length == 1,
                $"SMA1376: socket '{property.Key}' node '{property.Value.Node}' is missing or ambiguous.");
            result.Add(new AnimationSocket
            {
                Name = property.Key,
                Node = matches[0].index,
                Translation = property.Value.Translation,
                Rotation = property.Value.Rotation,
                Scale = property.Value.Scale
            });
            nodes[matches[0].index].Flags |= 2;
        }
        return result;
    }

    private static int UniqueNodeIndex(JsonElement nodes, string name)
    {
        var result = -1;
        for (var index = 0; index < nodes.GetArrayLength(); index++)
        {
            if (!nodes[index].TryGetProperty("name", out var nameValue) || nameValue.GetString() != name) continue;
            Require(result < 0, $"SMA1377: node name '{name}' is ambiguous.");
            result = index;
        }
        Require(result >= 0, $"SMA1378: node name '{name}' was not found.");
        return result;
    }

    private static Matrix4x4 ReflectMatrix(Matrix4x4 value)
    {
        Span<float> fields = stackalloc float[16]
        {
            value.M11, value.M12, value.M13, value.M14,
            value.M21, value.M22, value.M23, value.M24,
            value.M31, value.M32, value.M33, value.M34,
            value.M41, value.M42, value.M43, value.M44
        };
        for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
                if ((row == 2) != (column == 2)) fields[row * 4 + column] = -fields[row * 4 + column];
        return new Matrix4x4(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6],
            fields[7], fields[8], fields[9], fields[10], fields[11], fields[12], fields[13], fields[14], fields[15]);
    }

    private static Vector3 ToAnimationTranslation(Vector3 value) => new(value.X, value.Y, -value.Z);

    private static Quaternion ToAnimationRotation(Quaternion value) =>
        CanonicalQuaternion(new Quaternion(-value.X, -value.Y, value.Z, value.W),
            "SMA1379: reflected animation rotation is invalid.");

    private static Quaternion CanonicalQuaternion(Quaternion value, string error)
    {
        Require(float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z) &&
            float.IsFinite(value.W) && value.LengthSquared() > 1e-12f, error);
        value = Quaternion.Normalize(value);
        if (!CanonicalQuaternionSign(value))
            value = new Quaternion(-value.X, -value.Y, -value.Z, -value.W);
        return value;
    }

    private static bool CanonicalQuaternionSign(Quaternion value) =>
        value.W > 0 || (value.W == 0 && (value.Z > 0 || (value.Z == 0 &&
            (value.Y > 0 || (value.Y == 0 && value.X >= 0)))));

    private static Quaternion ReadQuaternion(float[] values, int index) => new(values[index * 4],
        values[index * 4 + 1], values[index * 4 + 2], values[index * 4 + 3]);

    private static Quaternion ReadQuaternion(JsonElement value, string name)
    {
        Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 4,
            $"SMA1380: {name} must contain four values.");
        return CanonicalQuaternion(new Quaternion(value[0].GetSingle(), value[1].GetSingle(),
            value[2].GetSingle(), value[3].GetSingle()), $"SMA1381: {name} is invalid.");
    }

    private static Vector3 ReadVector3(JsonElement value, string name)
    {
        Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 3,
            $"SMA1382: {name} must contain three values.");
        var result = new Vector3(value[0].GetSingle(), value[1].GetSingle(), value[2].GetSingle());
        Require(Finite(result), $"SMA1383: {name} contains a non-finite value.");
        return result;
    }

    private static bool GetBoolean(JsonElement value, string name)
    {
        Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False,
            $"SMA1384: {name} must be Boolean.");
        return value.GetBoolean();
    }

    private static void RequireOnly(JsonElement value, string context, params string[] names)
    {
        var allowed = names.ToHashSet(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            Require(allowed.Contains(property.Name),
                $"SMA1385: unknown {context} field '{property.Name}' is not supported.");
    }

    private static void ValidateAnimationName(string value, string kind)
    {
        Require(value.Length > 0 && StrictUtf8.GetByteCount(value) <= MaximumNameBytes &&
            !value.Contains('\0') && value.All(character => !char.IsControl(character)),
            $"SMA1386: {kind} names must use 1 to {MaximumNameBytes} printable UTF-8 bytes.");
    }
}
