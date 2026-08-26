// Procedural Web Audio SFX for Balatro interactions and scoring animations

class SoundEffects {
    constructor() {
        this.ctx = null;
    }

    init() {
        if (!this.ctx) {
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            if (AudioCtx) {
                this.ctx = new AudioCtx();
            }
        }
        if (this.ctx && this.ctx.state === 'suspended') {
            this.ctx.resume();
        }
    }

    getVolume() {
        const saved = localStorage.getItem('balatro_sfx_volume');
        const vol = saved !== null ? parseFloat(saved) : 0.8;
        return isNaN(vol) ? 0.8 : Math.max(0, Math.min(1, vol));
    }

    // Play card selection click
    playCardSelect(pitch = 1.0) {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.4, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'triangle';
            osc.frequency.setValueAtTime(320 * pitch, now);
            osc.frequency.exponentialRampToValueAtTime(160 * pitch, now + 0.04);

            gain.gain.setValueAtTime(1, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.04);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.05);
        } catch (e) {
            // Audio context error fallback
        }
    }

    // Play card whoosh / play hand
    playPlayHand() {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.5, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(200, now);
            osc.frequency.exponentialRampToValueAtTime(600, now + 0.12);

            gain.gain.setValueAtTime(0.8, now);
            gain.gain.linearRampToValueAtTime(0.01, now + 0.14);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.15);
        } catch (e) {}
    }

    // Play scoring chip tone (increasing pitch with index: 0, 1, 2, 3, 4...)
    playCardScore(index = 0) {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.6, now);
            masterGain.connect(this.ctx.destination);

            // Scale notes: pentatonic ascension
            const freqs = [330, 370, 415, 494, 554, 659, 740, 830, 988];
            const freq = freqs[index % freqs.length] || 440;

            const osc1 = this.ctx.createOscillator();
            const osc2 = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc1.type = 'triangle';
            osc1.frequency.setValueAtTime(freq, now);

            osc2.type = 'sine';
            osc2.frequency.setValueAtTime(freq * 2, now);

            gain.gain.setValueAtTime(0.9, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.22);

            osc1.connect(gain);
            osc2.connect(gain);
            gain.connect(masterGain);

            osc1.start(now);
            osc2.start(now);
            osc1.stop(now + 0.24);
            osc2.stop(now + 0.24);
        } catch (e) {}
    }

    // Play Joker activation sound (fire/punchy low pulse)
    playJokerTrigger() {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.7, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'sawtooth';
            osc.frequency.setValueAtTime(140, now);
            osc.frequency.exponentialRampToValueAtTime(440, now + 0.08);
            osc.frequency.exponentialRampToValueAtTime(220, now + 0.2);

            gain.gain.setValueAtTime(0.8, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.22);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.24);
        } catch (e) {}
    }

    // Play Chips x Mult multiplication sound
    playMultiply() {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.8, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'sine';
            osc.frequency.setValueAtTime(520, now);
            osc.frequency.exponentialRampToValueAtTime(1040, now + 0.15);

            gain.gain.setValueAtTime(1.0, now);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.3);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.32);
        } catch (e) {}
    }

    // Play round score addition slam
    playScoreSlam() {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.9, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'triangle';
            osc.frequency.setValueAtTime(120, now);
            osc.frequency.exponentialRampToValueAtTime(60, now + 0.18);

            gain.gain.setValueAtTime(1.0, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.25);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.28);
        } catch (e) {}
    }

    // Play card deal flick
    playCardDeal() {
        try {
            this.init();
            if (!this.ctx) return;
            const now = this.ctx.currentTime;
            const masterGain = this.ctx.createGain();
            masterGain.gain.setValueAtTime(this.getVolume() * 0.35, now);
            masterGain.connect(this.ctx.destination);

            const osc = this.ctx.createOscillator();
            const gain = this.ctx.createGain();

            osc.type = 'triangle';
            osc.frequency.setValueAtTime(480, now);
            osc.frequency.exponentialRampToValueAtTime(240, now + 0.05);

            gain.gain.setValueAtTime(0.8, now);
            gain.gain.exponentialRampToValueAtTime(0.01, now + 0.06);

            osc.connect(gain);
            gain.connect(masterGain);

            osc.start(now);
            osc.stop(now + 0.07);
        } catch (e) {}
    }
}

export const sfx = new SoundEffects();
