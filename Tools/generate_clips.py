#!/usr/bin/env python3
"""Synthesize the short loops used by the audio feature lab. No third-party recordings."""

import math
import os
import struct
import wave

RATE = 48000
ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "AudioFeatureLab", "Clips")


def clamp(value):
    if value > 1:
        return 1
    if value < -1:
        return -1
    return value


def sample_at(samples, position):
    count = len(samples)
    if count == 0:
        return 0.0
    if position <= 0.0:
        return samples[0]
    if position >= count - 1:
        return samples[count - 1]
    index = int(position)
    fraction = position - index
    return samples[index] * (1.0 - fraction) + samples[index + 1] * fraction


def rising_crossings(samples):
    """Fractional positions where this waveform itself crosses zero going upward."""
    found = []
    last = -1e9
    for index in range(len(samples) - 1):
        current = samples[index]
        nxt = samples[index + 1]
        if current < 0.0 < nxt:
            position = index + (-current) / (nxt - current)
        elif current == 0.0 and nxt > 0.0:
            position = float(index)
        else:
            continue
        if position - last < 2.0:
            continue
        found.append(position)
        last = position
    return found


def any_crossings(samples):
    found = []
    last = -1e9
    for index in range(len(samples) - 1):
        current = samples[index]
        nxt = samples[index + 1]
        if current == 0.0 and nxt == 0.0:
            continue
        if (current < 0.0 < nxt) or (current > 0.0 > nxt):
            position = index + (-current) / (nxt - current)
        elif current == 0.0 and nxt != 0.0:
            position = float(index)
        elif nxt == 0.0 and current != 0.0:
            position = float(index + 1)
        else:
            continue
        if position - last < 1.0:
            continue
        found.append(position)
        last = position
    return found


def slope_at(samples, position):
    return sample_at(samples, position + 1.0) - sample_at(samples, position - 1.0)


def cut_between(samples, start, end):
    """Slice from one zero crossing to a later one. Both endpoints are exactly 0."""
    span = end - start
    length = max(2, int(round(span)))
    output = [0.0] * length
    if length <= 2 or span <= 0.0:
        return output
    for index in range(1, length - 1):
        position = start + span * (index / (length - 1))
        output[index] = sample_at(samples, position)
    return output


def choose_pair(samples, points, target):
    if len(points) < 2:
        return None
    best = None
    best_score = 1e18
    for index, start in enumerate(points):
        goal = start + target
        low = 0
        high = len(points)
        while low < high:
            mid = (low + high) // 2
            if points[mid] < goal:
                low = mid + 1
            else:
                high = mid
        for neighbor in (low - 1, low, low + 1):
            if neighbor <= index or neighbor >= len(points):
                continue
            end = points[neighbor]
            span = end - start
            if span < max(8.0, target * 0.45):
                continue
            length_error = abs(span - target) / target
            start_slope = slope_at(samples, start)
            end_slope = slope_at(samples, end)
            slope_error = abs(end_slope - start_slope) / (abs(start_slope) + 1e-8)
            score = length_error + 0.2 * min(slope_error, 5.0)
            if score < best_score:
                best_score = score
                best = (start, end)
    return best


def already_zero_ended(samples):
    peak = max(max(abs(sample) for sample in samples), 1e-8)
    return abs(samples[0]) <= peak * 1e-4 and abs(samples[-1]) <= peak * 1e-4


def seamless_mono(samples):
    """Loop join on rising zero crossings of this waveform. Silent edges stay put."""
    if len(samples) < 8:
        return samples
    peak = max(max(abs(sample) for sample in samples), 1e-8)
    start_is_zero = abs(samples[0]) <= peak * 1e-4
    end_is_zero = abs(samples[-1]) <= peak * 1e-4
    if start_is_zero and end_is_zero:
        samples[0] = 0.0
        samples[-1] = 0.0
        return samples
    points = rising_crossings(samples)
    if not points:
        return samples
    if end_is_zero:
        start = points[0]
        if start < 1.0:
            samples[0] = 0.0
            samples[-1] = 0.0
            return samples
        return cut_between(samples, start, float(len(samples) - 1))
    if start_is_zero:
        end = min(points, key=lambda position: abs(position - (len(samples) - 1)))
        if end <= 8.0:
            return samples
        return cut_between(samples, 0.0, end)
    pair = choose_pair(samples, points, float(len(samples)))
    if pair is None:
        return samples
    return cut_between(samples, pair[0], pair[1])


def seamless_stereo(left, right):
    count = min(len(left), len(right))
    left = left[:count]
    right = right[:count]
    if already_zero_ended(left) and already_zero_ended(right):
        left[0] = right[0] = 0.0
        left[-1] = right[-1] = 0.0
        return left, right
    mid = [(left[index] + right[index]) * 0.5 for index in range(count)]
    points = rising_crossings(mid)
    pair = choose_pair(mid, points, float(count))
    if pair is None:
        return left, right
    return cut_between(left, pair[0], pair[1]), cut_between(right, pair[0], pair[1])


def seamless_stereo_independent(left, right):
    """Each channel is cut on its own zero crossings, to the same length."""
    target = float(min(len(left), len(right)))
    left_points = rising_crossings(left)
    right_points = rising_crossings(right)
    left_pair = choose_pair(left, left_points, target)
    right_pair = choose_pair(right, right_points, target)
    if left_pair is None or right_pair is None:
        return left, right
    left_cut = cut_between(left, left_pair[0], left_pair[1])
    right_cut = cut_between(right, right_pair[0], right_pair[1])
    length = min(len(left_cut), len(right_cut))
    left_cut = left_cut[:length]
    right_cut = right_cut[:length]
    left_cut[0] = right_cut[0] = 0.0
    left_cut[-1] = right_cut[-1] = 0.0
    return left_cut, right_cut


def trim_oneshot(samples):
    """Place a one-shot on zero crossings of its own waveform, without a fade."""
    if len(samples) < 8:
        return samples
    if already_zero_ended(samples):
        samples[0] = 0.0
        samples[-1] = 0.0
        return samples
    points = any_crossings(samples)
    if len(points) < 2:
        return samples
    return cut_between(samples, points[0], points[-1])


def write_mono(path, samples, loop=False):
    samples = seamless_mono(samples) if loop else trim_oneshot(samples)
    peak = max(max(abs(sample) for sample in samples), 1e-6)
    gain = 0.8 / peak
    with wave.open(path, "w") as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        frames = bytearray()
        for sample in samples:
            frames += struct.pack("<h", int(clamp(sample * gain) * 32767))
        handle.writeframes(frames)


def write_stereo(path, left, right, loop=False, independent=False):
    if loop and independent:
        left, right = seamless_stereo_independent(left, right)
    elif loop:
        left, right = seamless_stereo(left, right)
    else:
        left = trim_oneshot(left)
        right = trim_oneshot(right)
    peak = 1e-6
    for sample in left:
        peak = max(peak, abs(sample))
    for sample in right:
        peak = max(peak, abs(sample))
    gain = 0.8 / peak
    with wave.open(path, "w") as handle:
        handle.setnchannels(2)
        handle.setsampwidth(2)
        handle.setframerate(RATE)
        frames = bytearray()
        for a, b in zip(left, right):
            frames += struct.pack("<hh", int(clamp(a * gain) * 32767), int(clamp(b * gain) * 32767))
        handle.writeframes(frames)


def zeros(seconds):
    return [0.0] * int(seconds * RATE)


def add_tone(buffer, start, freq, duration, amplitude, decay, harmonics, pan_buffer=None, pan=0.0):
    count = int(duration * RATE)
    angle = (pan + 1.0) * 0.5 * math.pi * 0.5
    left_gain = math.cos(angle)
    right_gain = math.sin(angle)
    for i in range(count):
        index = start + i
        if index < 0 or index >= len(buffer):
            continue
        t = i / RATE
        envelope = math.exp(-decay * t)
        sample = 0.0
        for harmonic, weight in harmonics:
            sample += weight * math.sin(2.0 * math.pi * freq * harmonic * t)
        sample *= amplitude * envelope
        if pan_buffer is None:
            buffer[index] += sample
        else:
            buffer[index] += sample * left_gain
            pan_buffer[index] += sample * right_gain


def music_a():
    left = zeros(4)
    right = zeros(4)
    notes = [220.0, 329.63, 261.63, 440.0, 329.63, 392.0, 523.25, 329.63]
    beat = int(0.5 * RATE)
    for index, freq in enumerate(notes):
        pan = -0.45 if index % 2 == 0 else 0.45
        add_tone(left, index * beat, freq, 0.48, 0.55, 4.2, [(1, 1.0), (2, 0.28), (3, 0.08)], right, pan)
    add_tone(left, 0, 110.0, 1.8, 0.28, 1.4, [(1, 1.0), (2, 0.15)], right, 0.0)
    add_tone(left, 2 * RATE, 110.0, 1.8, 0.28, 1.4, [(1, 1.0), (2, 0.15)], right, 0.0)
    return left, right


def music_b():
    left = zeros(4)
    right = zeros(4)
    notes = [329.63, 415.30, 493.88, 659.25, 493.88, 415.30, 659.25, 830.61]
    for index, freq in enumerate(notes):
        start = int((index * 0.5) * RATE)
        pan = 0.35 if index % 2 == 0 else -0.35
        add_tone(left, start, freq, 0.42, 0.42, 5.5, [(1, 1.0), (2, 0.45), (3, 0.22), (4, 0.08)], right, pan)
    add_tone(left, 0, 164.81, 3.6, 0.22, 0.6, [(1, 1.0), (2, 0.2)], right, 0.0)
    return left, right


def ambience():
    def channel(seed, seconds):
        state = seed
        value = 0.0
        samples = []
        for _ in range(int(seconds * RATE)):
            state = (1103515245 * state + 12345) & 0x7FFFFFFF
            white = (state / 0x7FFFFFFF) * 2.0 - 1.0
            value += 0.015 * (white - value)
            samples.append(value * 4.0)
        return samples

    return channel(17, 4.0), channel(91, 4.0)


def radio():
    left = zeros(4)
    right = zeros(4)
    chord = [261.63, 329.63, 392.0, 493.88]
    for index, freq in enumerate(chord):
        pan = -0.2 + index * 0.13
        add_tone(left, 0, freq, 4.0, 0.16, 0.02, [(1, 1.0), (2, 0.08)], right, pan)
    for i in range(len(left)):
        tremolo = 0.86 + 0.14 * math.sin(2.0 * math.pi * 0.5 * (i / RATE))
        left[i] *= tremolo
        right[i] *= tremolo
    return left, right


def voice():
    samples = zeros(2)
    for i in range(len(samples)):
        t = i / RATE
        syllable = 0.5 + 0.5 * math.sin(2.0 * math.pi * 4.0 * t)
        syllable = syllable * syllable
        samples[i] = syllable * (
            0.55 * math.sin(2.0 * math.pi * 196.0 * t)
            + 0.22 * math.sin(2.0 * math.pi * 784.0 * t)
            + 0.12 * math.sin(2.0 * math.pi * 1176.0 * t)
        )
    return samples


def sfx_loop():
    samples = zeros(2)
    for hit in range(8):
        add_tone(samples, int(hit * 0.25 * RATE), 1800.0, 0.09, 0.45, 28.0, [(1, 1.0), (2, 0.3)])
        start = int(hit * 0.25 * RATE)
        state = 40 + hit * 17
        noise = 0.0
        for i in range(int(0.05 * RATE)):
            state = (1103515245 * state + 12345) & 0x7FFFFFFF
            white = (state / 0x7FFFFFFF) * 2.0 - 1.0
            noise += 0.25 * (white - noise)
            if start + i < len(samples):
                samples[start + i] += noise * math.exp(-30.0 * (i / RATE)) * 0.35
    return samples


def beacon():
    samples = zeros(1.5)
    add_tone(samples, 0, 880.0, 0.18, 0.7, 14.0, [(1, 1.0), (2, 0.15)])
    add_tone(samples, int(0.75 * RATE), 880.0, 0.18, 0.7, 14.0, [(1, 1.0), (2, 0.15)])
    return samples


def drone():
    samples = zeros(2)
    for i in range(len(samples)):
        t = i / RATE
        samples[i] = 0.55 * math.sin(2.0 * math.pi * 55.0 * t) + 0.28 * math.sin(2.0 * math.pi * 82.5 * t)
    return samples


def doppler_tone():
    samples = zeros(1)
    for i in range(len(samples)):
        t = i / RATE
        samples[i] = (
            0.62 * math.sin(2.0 * math.pi * 440.0 * t)
            + 0.18 * math.sin(2.0 * math.pi * 880.0 * t)
            + 0.08 * math.sin(2.0 * math.pi * 660.0 * t)
        )
    return samples


def oneshot():
    return_samples = zeros(0.22)
    add_tone(return_samples, 0, 988.0, 0.2, 0.7, 12.0, [(1, 1.0), (2, 0.2)])
    return return_samples


def footstep():
    samples = zeros(0.09)
    state = 99
    low = 0.0
    for i in range(len(samples)):
        state = (1103515245 * state + 12345) & 0x7FFFFFFF
        white = (state / 0x7FFFFFFF) * 2.0 - 1.0
        low += 0.35 * (white - low)
        t = i / RATE
        samples[i] = low * math.exp(-38.0 * t) * (1.0 + 0.4 * math.sin(2.0 * math.pi * (180.0 - 400.0 * t) * t))
    return samples


def main():
    os.makedirs(ROOT, exist_ok=True)
    write_stereo(os.path.join(ROOT, "music_a.wav"), *music_a(), loop=True)
    write_stereo(os.path.join(ROOT, "music_b.wav"), *music_b(), loop=True)
    write_stereo(os.path.join(ROOT, "ambience.wav"), *ambience(), loop=True, independent=True)
    write_stereo(os.path.join(ROOT, "radio.wav"), *radio(), loop=True)
    write_mono(os.path.join(ROOT, "voice.wav"), voice(), loop=True)
    write_mono(os.path.join(ROOT, "sfx_loop.wav"), sfx_loop(), loop=True)
    write_mono(os.path.join(ROOT, "beacon.wav"), beacon(), loop=True)
    write_mono(os.path.join(ROOT, "drone.wav"), drone(), loop=True)
    write_mono(os.path.join(ROOT, "doppler_tone.wav"), doppler_tone(), loop=True)
    write_mono(os.path.join(ROOT, "oneshot_beep.wav"), oneshot(), loop=False)
    write_mono(os.path.join(ROOT, "footstep.wav"), footstep(), loop=False)
    print("Wrote clips to", ROOT)


if __name__ == "__main__":
    main()
