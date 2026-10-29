using UnityEngine;

// Music and the physical sounds (footsteps, crashes, power-up, clicks) are recordings in Resources/Audio; the
// arcade blips (coin, jump, slide, whistle, horn) are synthesised at startup, as is a fallback music loop.
public class LRAudio {
    const int SR = 44100;
    const float MASTER = 0.45f;
    readonly AudioSource music, sfx;
    readonly AudioClip coin, jump, slide, bump, crash, power, whistle, horn;
    readonly AudioClip[] crashes, bumps, steps; readonly AudioClip powerReal, click, buy;
    public bool Muted { get; private set; }

    public LRAudio(GameObject host) {
        Muted = PlayerPrefs.GetInt("lagosrun.muted", 0) == 1;
        music = host.AddComponent<AudioSource>(); music.loop = true; music.playOnAwake = false; music.volume = MASTER;
        sfx = host.AddComponent<AudioSource>(); sfx.playOnAwake = false; sfx.volume = MASTER;
        AudioClip track = Resources.Load<AudioClip>("Audio/music");
        music.clip = track != null ? track : Loop();
        if (track != null) music.volume = 0.5f;
        crashes = Set("crash", 3); bumps = Set("bump", 3); steps = Set("step", 5);
        powerReal = Resources.Load<AudioClip>("Audio/powerup"); click = Resources.Load<AudioClip>("Audio/click"); buy = Resources.Load<AudioClip>("Audio/buy");
        float[] b;
        b = Buf(0.2f); Tone(b, 0, 988, 0.06f, "square", 0.08f); Tone(b, 0.05f, 1319, 0.1f, "square", 0.08f); coin = Clip("coin", b);
        b = Buf(0.22f); Tone(b, 0, 300, 0.18f, "triangle", 0.18f, 700); jump = Clip("jump", b);
        b = Buf(0.25f); Noise(b, 0, 0.2f, 0.12f, 2500); slide = Clip("slide", b);
        b = Buf(0.25f); Tone(b, 0, 140, 0.2f, "sawtooth", 0.2f, 60); bump = Clip("bump", b);
        b = Buf(0.6f); Noise(b, 0, 0.5f, 0.5f, 200); Tone(b, 0, 180, 0.5f, "sawtooth", 0.3f, 40); crash = Clip("crash", b);
        b = Buf(0.35f); float[] notes = { 523, 659, 784, 1047 };
        for (int i = 0; i < 4; i++) Tone(b, i * 0.06f, notes[i], 0.12f, "square", 0.08f);
        power = Clip("power", b);
        b = Buf(0.45f);
        foreach (float d in new[] { 0f, 0.22f }) { Tone(b, d, 2300, 0.16f, "square", 0.06f); Tone(b, d + 0.04f, 2450, 0.12f, "square", 0.04f); }
        whistle = Clip("whistle", b);
        b = Buf(0.3f); Tone(b, 0, 392, 0.25f, "sawtooth", 0.07f); Tone(b, 0, 494, 0.25f, "sawtooth", 0.06f); horn = Clip("horn", b);
        ApplyMute();
    }

    static AudioClip[] Set(string name, int n) {
        var list = new System.Collections.Generic.List<AudioClip>();
        for (int i = 0; i < n; i++) { AudioClip c = Resources.Load<AudioClip>("Audio/" + name + i); if (c != null) list.Add(c); }
        return list.ToArray();
    }
    void One(AudioClip[] set, AudioClip fallback, float volume) {
        if (set.Length > 0) sfx.PlayOneShot(set[Random.Range(0, set.Length)], volume); else if (fallback != null) sfx.PlayOneShot(fallback);
    }
    static float[] Buf(float seconds) { return new float[(int)(seconds * SR)]; }
    static AudioClip Clip(string name, float[] data) {
        AudioClip c = AudioClip.Create(name, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
    // oscillator with an exponential pitch slide and an exponential fade, as the Web Audio version did; wraps round the buffer
    static void Tone(float[] buf, float t, float f, float dur, string type, float vol, float f2 = 0) {
        int start = (int)(t * SR), n = (int)(dur * SR);
        double phase = 0;
        for (int i = 0; i < n; i++) {
            float k = i / (float)n;
            float freq = f2 > 0 ? f * Mathf.Pow(f2 / f, k) : f;
            phase += freq / SR;
            double p = phase - System.Math.Floor(phase);
            float s;
            if (type == "sine") s = Mathf.Sin((float)(p * 2 * Mathf.PI));
            else if (type == "square") s = p < 0.5 ? 1 : -1;
            else if (type == "triangle") s = (float)(p < 0.5 ? 4 * p - 1 : 3 - 4 * p);
            else s = (float)(2 * p - 1);
            float gain = vol * Mathf.Pow(0.0008f / vol, k);
            buf[(start + i) % buf.Length] += s * gain;
        }
    }
    static void Noise(float[] buf, float t, float dur, float vol, float highpass) {
        int start = (int)(t * SR), n = (int)(dur * SR);
        float rc = 1f / (2 * Mathf.PI * highpass), a = rc / (rc + 1f / SR), prevIn = 0, prevOut = 0;
        for (int i = 0; i < n; i++) {
            float x = Random.value * 2 - 1;
            float y = a * (prevOut + x - prevIn);
            prevIn = x; prevOut = y;
            buf[(start + i) % buf.Length] += y * vol * Mathf.Pow(0.0008f / vol, i / (float)n);
        }
    }
    static AudioClip Loop() {
        const float STEP = 60f / 108f / 4f;
        float[] log = { 110, 0, 0, 98, 0, 0, 82.4f, 0, 110, 0, 0, 130.8f, 0, 98, 0, 0 };
        float[] b = new float[Mathf.RoundToInt(STEP * 16 * SR)];
        for (int s = 0; s < 16; s++) {
            float t = s * STEP;
            if (s % 4 == 0) Tone(b, t, 110, 0.18f, "sine", 0.55f, 40);          // kick
            Noise(b, t, 0.04f, s % 2 == 1 ? 0.05f : 0.09f, 7000);                // shaker
            if (s == 4 || s == 12) Noise(b, t, 0.09f, 0.14f, 1800);              // clap
            if (log[s] > 0) Tone(b, t, log[s] * 2, 0.28f, "sine", 0.32f, log[s] * 1.2f);   // log drum
            if (s == 3 || s == 11) Tone(b, t, 880, 0.05f, "triangle", 0.05f);    // bell
        }
        return Clip("loop", b);
    }

    void ApplyMute() { music.mute = Muted; sfx.mute = Muted; }
    public bool Toggle() { Muted = !Muted; PlayerPrefs.SetInt("lagosrun.muted", Muted ? 1 : 0); PlayerPrefs.Save(); ApplyMute(); return Muted; }
    public void Music(bool on) { if (on && !music.isPlaying) music.Play(); if (!on && music.isPlaying) music.Pause(); }
    public void Coin() { sfx.PlayOneShot(coin); }
    public void Jump() { sfx.PlayOneShot(jump); }
    public void Slide() { sfx.PlayOneShot(slide); }
    public void Bump() { One(bumps, bump, 2f); }
    public void Crash() { One(crashes, crash, 2.2f); }
    public void Power() { if (powerReal != null) sfx.PlayOneShot(powerReal, 1.2f); else sfx.PlayOneShot(power); }
    public void Step() { One(steps, null, 0.55f); }
    public void Click() { if (click != null) sfx.PlayOneShot(click, 1.5f); }
    public void Buy() { if (buy != null) sfx.PlayOneShot(buy, 1.5f); else sfx.PlayOneShot(power); }
    public void Whistle() { sfx.PlayOneShot(whistle); }
    public void Horn() { sfx.PlayOneShot(horn); }
}
