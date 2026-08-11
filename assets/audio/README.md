# Character Sound Assets

Every finished sound is a mono, 16-bit, 22,050 Hz PCM WAV file. The app plays one short nonverbal sound when a character appears. It does not use text-to-speech or spoken dialogue.

| File | Content | Duration | Source |
| --- | --- | ---: | --- |
| `cat.wav` | One real cat meow | 0.970 seconds | CC0 field recording |
| `corgi.wav` | One real dog bark | 0.380 seconds | CC BY-SA 3.0 field recording; the breed was not identified |
| `red-panda.wav` | One real red panda chirp | 0.440 seconds | Public-domain field recording |
| `mech.wav` | One mech servo startup | 0.860 seconds | Original synthesized effect |
| `web-ranger.wav` | One filament launcher effect | 0.380 seconds | Original synthesized effect |

## Recording sources and licenses

### Cat

- Source file: `source/cat-cc0.wav`
- Work: [Meow of a Siamese cat - freemaster2.wav](https://commons.wikimedia.org/wiki/File:Meow_of_a_Siamese_cat_-_freemaster2.wav)
- Creator: freemaster2
- License: [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/)
- Changes: trimmed to one meow, DC offset removed, downsampled to 22,050 Hz, normalized, and given short fades

### Dog

- Source file: `source/dog-cc-by-sa.ogg`
- Work: [Sound-of-dog.ogg](https://commons.wikimedia.org/wiki/File:Sound-of-dog.ogg)
- Creator: Kriplozoik at English Wikipedia
- License: [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)
- Changes: converted to mono, trimmed to the first bark, downsampled to 22,050 Hz, normalized, and given short fades
- `corgi.wav` is distributed under CC BY-SA 3.0. The source recording does not identify the dog's breed, so it is described only as a general dog bark.

### Red panda

- Source file: `source/red-panda-public-domain.ogg`
- Work: [Red panda twittering.ogg](https://commons.wikimedia.org/wiki/File:Red_panda_twittering.ogg)
- Recorder/uploader: Mizunoryu
- License: Public domain
- Changes: trimmed to the first short chirp, converted to mono, downsampled to 22,050 Hz, normalized, and given short fades

## Original effects

`mech.wav` and `web-ranger.wav` were created with deterministic waveform and noise synthesis. They contain no text-to-speech, human voice, movie audio, game audio, or third-party samples.
