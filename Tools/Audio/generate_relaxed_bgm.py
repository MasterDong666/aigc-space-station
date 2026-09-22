#!/usr/bin/env python3
"""Generate two original, loop-friendly relaxed sci-fi/cultivation BGM tracks."""

from __future__ import annotations

import argparse
import math
import wave
from pathlib import Path

import numpy as np


SAMPLE_RATE = 44100


def midi_to_hz(note: int) -> float:
    return 440.0 * (2.0 ** ((note - 69) / 12.0))


def envelope(length: int, attack: float, release: float) -> np.ndarray:
    env = np.ones(length, dtype=np.float32)
    attack_samples = min(length, max(1, int(attack * SAMPLE_RATE)))
    release_samples = min(length, max(1, int(release * SAMPLE_RATE)))
    env[:attack_samples] *= np.linspace(0.0, 1.0, attack_samples, dtype=np.float32)
    env[-release_samples:] *= np.linspace(1.0, 0.0, release_samples, dtype=np.float32)
    return env


def add_tone(
    mix: np.ndarray,
    start: float,
    duration: float,
    note: int,
    amplitude: float,
    timbre: str,
    pan: float = 0.0,
) -> None:
    begin = int(start * SAMPLE_RATE)
    length = min(int(duration * SAMPLE_RATE), len(mix) - begin)
    if begin < 0 or length <= 0:
        return

    time = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    frequency = midi_to_hz(note)
    phase = 2.0 * np.pi * frequency * time

    if timbre == "pad":
        signal = (
            np.sin(phase)
            + 0.28 * np.sin(2.0 * phase + 0.2)
            + 0.12 * np.sin(3.0 * phase + 0.7)
        )
        signal *= envelope(length, 1.15, 1.25)
        signal *= 0.90 + 0.10 * np.sin(2.0 * np.pi * 0.055 * time + 0.3)
    elif timbre == "piano":
        signal = (
            np.sin(phase)
            + 0.22 * np.sin(2.0 * phase + 0.15)
            + 0.07 * np.sin(3.0 * phase + 0.5)
        )
        signal *= np.exp(-1.15 * time / max(duration, 0.05))
        signal *= envelope(length, 0.085, 0.55)
    elif timbre == "pluck":
        signal = (
            np.sin(phase)
            + 0.42 * np.sin(2.01 * phase)
            + 0.16 * np.sin(3.98 * phase)
        )
        signal *= np.exp(-4.2 * time / max(duration, 0.05))
        signal *= envelope(length, 0.012, 0.10)
    elif timbre == "bell":
        signal = (
            np.sin(phase)
            + 0.34 * np.sin(2.76 * phase + 0.5)
            + 0.16 * np.sin(5.42 * phase + 0.9)
        )
        signal *= np.exp(-3.2 * time / max(duration, 0.05))
        signal *= envelope(length, 0.018, 0.18)
    elif timbre == "bass":
        signal = np.sin(phase) + 0.10 * np.sin(2.0 * phase)
        signal *= envelope(length, 0.55, 0.85)
    else:
        signal = np.sin(phase) * envelope(length, 0.02, 0.12)

    left = math.sqrt((1.0 - pan) * 0.5)
    right = math.sqrt((1.0 + pan) * 0.5)
    mix[begin : begin + length, 0] += signal * amplitude * left
    mix[begin : begin + length, 1] += signal * amplitude * right


def add_shaker(
    mix: np.ndarray,
    start: float,
    amplitude: float,
    rng: np.random.Generator,
    pan: float,
) -> None:
    duration = 0.075
    begin = int(start * SAMPLE_RATE)
    length = min(int(duration * SAMPLE_RATE), len(mix) - begin)
    if length <= 0:
        return
    noise = rng.normal(0.0, 1.0, length).astype(np.float32)
    # Emphasize the airy high-frequency part without external DSP libraries.
    noise[1:] = noise[1:] - 0.82 * noise[:-1]
    noise *= np.exp(-8.0 * np.arange(length) / max(length, 1))
    left = math.sqrt((1.0 - pan) * 0.5)
    right = math.sqrt((1.0 + pan) * 0.5)
    mix[begin : begin + length, 0] += noise * amplitude * left
    mix[begin : begin + length, 1] += noise * amplitude * right


def add_soft_kick(mix: np.ndarray, start: float, amplitude: float) -> None:
    begin = int(start * SAMPLE_RATE)
    length = min(int(0.22 * SAMPLE_RATE), len(mix) - begin)
    if length <= 0:
        return
    time = np.arange(length, dtype=np.float32) / SAMPLE_RATE
    phase = 2.0 * np.pi * (62.0 * time - 19.0 * time * time)
    signal = np.sin(phase) * np.exp(-18.0 * time) * amplitude
    mix[begin : begin + length, 0] += signal * 0.707
    mix[begin : begin + length, 1] += signal * 0.707


def render_track(
    output: Path,
    bpm: float,
    chords: list[list[int]],
    melody: list[int],
    station_style: bool,
) -> None:
    beat = 60.0 / bpm
    bar = beat * 4.0
    bars = 20
    duration = bars * bar
    mix = np.zeros((int(duration * SAMPLE_RATE), 2), dtype=np.float32)
    for bar_index in range(bars):
        chord = chords[bar_index % len(chords)]
        bar_start = bar_index * bar

        # Long pads overlap across chord changes, removing any rhythmic pumping.
        for index, note in enumerate(chord):
            add_tone(
                mix,
                bar_start,
                min(beat * 5.0, duration - bar_start),
                note - 12,
                0.047 if station_style else 0.052,
                "pad",
                -0.5 + index * (1.0 / max(1, len(chord) - 1)),
            )

        # One sustained, very soft bass note per bar—no heartbeat-like accents.
        add_tone(
            mix,
            bar_start,
            min(beat * 4.65, duration - bar_start),
            chord[0] - 24,
            0.060,
            "bass",
            0.0,
        )

        # Overlapping electric-piano tones flow through the harmony.
        pattern = [0, 2, 1, 3]
        for step, chord_index in enumerate(pattern):
            add_tone(
                mix,
                bar_start + step * beat,
                min(beat * 1.65, duration - (bar_start + step * beat)),
                chord[chord_index % len(chord)],
                0.021 if station_style else 0.025,
                "piano",
                -0.32 + step * 0.21,
            )

        # A slow three-note phrase appears only once every four bars.
        if bar_index % 4 == 1:
            offset = (bar_index // 4) % len(melody)
            phrase = melody[offset:] + melody[:offset]
            for step, note in enumerate(phrase[:3]):
                add_tone(
                    mix,
                    bar_start + beat * (0.35 + step * 1.18),
                    min(
                        beat * 1.55,
                        duration - (bar_start + beat * (0.35 + step * 1.18)),
                    ),
                    note,
                    0.028 if station_style else 0.032,
                    "piano",
                    0.18 if step % 2 == 0 else -0.14,
                )

    # A touch of stereo echo creates space while retaining a clean loop.
    echo = int((beat * 1.35) * SAMPLE_RATE)
    mix[echo:, 0] += mix[:-echo, 1] * 0.075
    mix[echo:, 1] += mix[:-echo, 0] * 0.075

    # Remove click at the exact loop point with a very short transparent gap.
    fade = int(0.008 * SAMPLE_RATE)
    mix[:fade] *= np.linspace(0.0, 1.0, fade, dtype=np.float32)[:, None]
    mix[-fade:] *= np.linspace(1.0, 0.0, fade, dtype=np.float32)[:, None]

    mix = np.tanh(mix * 1.15)
    peak = float(np.max(np.abs(mix)))
    if peak > 0:
        mix *= 0.66 / peak

    pcm = np.clip(mix * 32767.0, -32768, 32767).astype("<i2")
    output.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(output), "wb") as handle:
        handle.setnchannels(2)
        handle.setsampwidth(2)
        handle.setframerate(SAMPLE_RATE)
        handle.writeframes(pcm.tobytes())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("output_dir", type=Path)
    args = parser.parse_args()

    render_track(
        args.output_dir / "MainMenu_RelaxedCultivation.wav",
        bpm=76.0,
        chords=[
            [60, 64, 67, 71],
            [57, 60, 64, 67],
            [53, 57, 60, 64],
            [55, 59, 62, 64],
        ],
        melody=[72, 76, 79, 76, 74, 72],
        station_style=False,
    )
    render_track(
        args.output_dir / "Station_GentleOrbit.wav",
        bpm=72.0,
        chords=[
            [62, 66, 69, 73],
            [59, 62, 66, 69],
            [55, 59, 62, 66],
            [57, 62, 64, 69],
        ],
        melody=[74, 78, 81, 78, 76, 74],
        station_style=True,
    )


if __name__ == "__main__":
    main()
