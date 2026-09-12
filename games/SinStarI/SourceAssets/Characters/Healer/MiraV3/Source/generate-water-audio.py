"""Original deterministic water cues, authored for Mira; Python standard library only."""
import array
import math
import random
import wave
from pathlib import Path

RATE = 44100
OUTPUT = Path(__file__).resolve().parents[1] / "Audio"
OUTPUT.mkdir(exist_ok=True)

def render(name, duration, notes, seed):
    rng = random.Random(seed)
    samples = array.array("h")
    low = 0.0
    for index in range(int(RATE * duration)):
        t = index / RATE
        phase = t / duration
        low = low * 0.91 + rng.uniform(-1, 1) * 0.09
        wash = (math.sin(math.pi * phase) ** 1.6) * low * 0.7
        # Descending resonant bubbles ride the water wash.
        bubble_time = t % 0.19
        bubble = math.sin(2 * math.pi * (900 * bubble_time - 900 * bubble_time ** 2))
        value = wash + bubble * math.exp(-bubble_time * 35) * 0.06 * (1 - phase)
        for start, frequency in notes:
            age = t - start
            if age >= 0:
                envelope = min(1.0, age * 80) * math.exp(-age * 3.4)
                value += math.sin(2 * math.pi * frequency * age) * envelope * 0.09
                value += math.sin(2 * math.pi * frequency * 2.01 * age) * envelope * 0.015
        fade = min(1.0, t * 35, (duration - t) * 12)
        samples.append(round(max(-0.9, min(0.9, value * fade)) * 32767))
    with wave.open(str(OUTPUT / (name + ".wav")), "wb") as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(samples.tobytes())

render("mira-water-attack", 1.6, [], 610)
render("mira-heal-one", 2.4, [(0.05, 523.25), (0.25, 659.25), (0.5, 783.99)], 611)
render("mira-heal-party", 3.0, [(0.05, 523.25), (0.22, 659.25), (0.4, 783.99),
    (0.65, 1046.5), (0.85, 1318.5)], 612)
print("Rendered three mono PCM water cues")
