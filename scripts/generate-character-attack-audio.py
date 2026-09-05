"""Rebuild original, deterministic PCM attack cues for the native Party preview.

Only Python's standard library is required. --check verifies the checked-in WAVs
and manifests without writing. These are synthesized sounds, not sampled media.
"""
import argparse
import hashlib
import io
import json
import math
import random
import struct
import wave
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100
PACKAGES = {
    'Arin': ROOT / 'games/SinStarI/SourceAssets/Characters/Paladin/ArinV57',
    'Dragon': ROOT / 'games/SinStarI/SourceAssets/Bosses/RedDragon/RedDragonV11',
}
CUES = [
    ('Arin', 'SwordAttack', 'arin-flame-slash.wav', 280, 1.12, 0),
    ('Arin', 'SwordAttack2', 'arin-flame-crosscut.wav', 160, .92, 1),
    ('Dragon', 'FireBreath', 'dragon-fire-breath.wav', 100, 3.10, 2),
    ('Dragon', 'ClawStrike', 'dragon-claw-strike.wav', 200, 1.65, 3),
    ('Dragon', 'Fireball', 'dragon-fireball-impact.wav', 2500, 2.35, 4),
]


def pulse(t, start, length):
    x = (t - start) / length
    return math.sin(math.pi * x) ** 2 if 0 < x < 1 else 0.0


def decay(t, start, rate):
    x = t - start
    return (1 - math.exp(-x * 500)) * math.exp(-x * rate) if x > 0 else 0.0


def synthesize(duration, kind):
    rng = random.Random(202609050 + kind)
    low = mid = phase = 0.0
    values = []
    for i in range(round(duration * RATE)):
        t = i / RATE
        noise = rng.uniform(-1, 1)
        low += .012 * (noise - low)
        mid += .19 * (noise - mid)
        if kind < 2:
            contact = .36 if kind == 0 else .24
            sweep = pulse(t, 0, contact + .09)
            ring = decay(t, contact, 9)
            phase += math.tau * (1800 - 1250 * min(1, t / (contact + .1))) / RATE
            whoosh = sweep * (mid * 1.5 + (noise - mid) * .22 + math.sin(phase) * .045)
            metal = sum(math.sin(math.tau * f * (t - contact)) * a
                        for f, a in [(780, .15), (1273, .095), (2131, .035)])
            impact = decay(t, contact, 28) * (low * 3 + noise * .5)
            flame = (mid * .5 + low) * decay(t, contact, 4.6)
            sample = whoosh + ring * metal + impact + flame
        else:
            # Layer throat harmonics, low turbulent air and a brighter fire/swish.
            pitch = 64 + 23 * math.sin(min(1, t / .8) * math.pi)
            phase += math.tau * pitch / RATE
            throat = (math.sin(phase) + .42 * math.sin(phase * 2.03)
                      + .19 * math.sin(phase * 4.01)) * .15
            throat *= .73 + .27 * math.sin(math.tau * 27 * t)
            if kind == 2:
                roar = pulse(t, 0, 1.3)
                fire = pulse(t, .65, 2.45) ** .4
                crackle = max(0, abs(noise) - .985) * 12
                sample = roar * (throat + low * 1.6)
                sample += fire * (low * 2.3 + mid * .40 + crackle * .2)
                sample += decay(t, .75, 10) * noise * .22
            elif kind == 3:
                roar = pulse(t, 0, 1.1)
                swipe = pulse(t, .42, .54)
                slam = decay(t, .90, 12)
                sample = roar * (throat + low) + swipe * (mid * 1.2 + noise * .18)
                sample += slam * (low * 3.5 + noise * .25 + math.sin(math.tau * 52 * t) * .18)
            else:
                boom = decay(t, 0, 3.4)
                crack = decay(t, 0, 36)
                rumble = decay(t, .10, 2.5)
                sample = boom * (low * 3 + mid * .3) + crack * noise * .65
                sample += rumble * (throat * .55 + low * 1.2)
        # Short edge fades and bounded normalization prevent clicks and clipping.
        sample *= min(1, t / .006, (duration - t) / .04)
        values.append(sample)
    gain = .78 / max(abs(v) for v in values)
    pcm = b''.join(struct.pack('<h', round(v * gain * 32767)) for v in values)
    output = io.BytesIO()
    with wave.open(output, 'wb') as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(pcm)
    return output.getvalue()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    manifests = {name: {'owner': name, 'sampleRate': RATE, 'encoding': 'PCM16 mono',
                       'provenance': 'Original deterministic synthesis; no third-party samples.',
                       'generator': 'scripts/generate-character-attack-audio.py',
                       'timing': 'Viewer presentation cues in clip milliseconds; not authored SM3D events.',
                       'cues': []} for name in PACKAGES}

    def publish(path, data):
        if args.check:
            assert path.read_bytes() == data, f'Asset differs: {path}'
        else:
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(data)

    for owner, clip, filename, cue_time, duration, kind in CUES:
        data = synthesize(duration, kind)
        publish(PACKAGES[owner] / 'Audio' / filename, data)
        manifests[owner]['cues'].append({'clip': clip, 'file': filename, 'cueTimeMs': cue_time,
                                       'durationMs': round(duration * 1000), 'bytes': len(data),
                                       'sha256': hashlib.sha256(data).hexdigest()})
    for owner, manifest in manifests.items():
        publish(PACKAGES[owner] / 'Audio' / 'attack-audio.json',
                (json.dumps(manifest, indent=2) + '\n').encode())
    print(('Verified' if args.check else 'Generated') + ' five original attack WAVs and two manifests.')


if __name__ == '__main__':
    main()
