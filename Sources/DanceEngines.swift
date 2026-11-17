import AppKit

// 哆嗦毛 / 闪身步 / MJ 太空步 / 科目三：与 Windows 版同一套网页帧驱动引擎的 Swift 移植。
// 网页骨架 190 单位高、脚在 y=118；网页角度用度，y 向下；我们身高为单位、y 向上。

private let webRad = Double.pi / 180.0

private func easeInOutD(_ t: Double) -> Double { t * t * (3 - 2 * t) }
private func easeOutQuart(_ t: Double) -> Double { 1 - pow(1 - t, 4) }
private func easeSnapD(_ t: Double) -> Double { 1 - pow(1 - t, 2.2) }
private func cg(_ x: Double) -> CGFloat { CGFloat(x) }

private struct WebPose {
    var x: Double = 0
    var y: Double = 0
    var rootX: Double = 0
    var rootRot: Double = 0
    var lean: Double = 0
    var head: Double = 0
    var shL: Double = 0
    var shR: Double = 0
    var aUL: Double = -11
    var aFL: Double = 9
    var aUR: Double = 11
    var aFR: Double = -9
    var wPhaseL: Double = 0
    var wPhaseR: Double = 0
    var wAmp: Double = 0
    var knee: Double = 0.25
    var footRollL: Double = 0
    var footRollR: Double = 0
    var heelL: Double = 0
    var heelR: Double = 0
    var trem: Double = 0
    var flx: Double = -30
    var fly: Double = 118
    var frx: Double = 30
    var fry: Double = 118
}

private struct WebBody {
    var lean: Double = 0
    var hipX: Double = 0
    var dip: Double = 0.075
    var torso: Double = 0
    var head: Double = 0
    var shrug: Double = 0
    var handP: CGPoint = .zero
    var handM: CGPoint = .zero
    var footP: CGPoint = .zero
    var footM: CGPoint = .zero
    var footRotP: Double = 0
    var footRotM: Double = 0
    var rod: Double = 0
    var elbP: CGFloat = 1
    var elbM: CGFloat = -1
    var kneP: CGFloat = 1
    var kneM: CGFloat = -1
}

private func webBase(_ flx: Double, _ frx: Double) -> WebPose {
    var p = WebPose()
    p.flx = flx; p.frx = frx
    return p
}

private func webHand(_ shX: Double, _ shY: Double, _ a1: Double, _ a2: Double,
                     _ phaseDeg: Double, _ ampUnits: Double) -> CGPoint {
    let dx = sin(a1 * webRad) * 44 + sin((a1 + a2) * webRad) * 40
    let dy = cos(a1 * webRad) * 44 + cos((a1 + a2) * webRad) * 40
    let s = 0.7 / 190.0
    let w = ampUnits / 190.0
    let ph = phaseDeg * webRad
    return CGPoint(x: cg(shX + dx * s + cos(ph) * w),
                   y: cg(shY - dy * s + sin(ph) * w * 0.62))
}

private func webFootY(_ ly: Double, _ heel: Double) -> Double {
    max(0, (118 - ly) * 0.5 / 190) + heel * 0.03
}

private func webBody(_ p: WebPose, _ a: Double) -> WebBody {
    let dip = 0.075 + (p.knee - 0.25) * 0.17 - p.y / 190
    let hipX = (p.x + p.rootX) / 190
    let shY = 0.7 - dip
    return WebBody(
        lean: (p.lean + p.rootRot) * webRad * a,
        hipX: hipX * a,
        dip: 0.075 + (dip - 0.075) * a,
        torso: (p.shL - p.shR) / 190 * 0.6 * a,
        head: p.head * webRad * a,
        shrug: -(p.shL + p.shR) / 2 / 190 * a,
        handP: webHand(hipX + 0.09, shY, p.aUR, p.aFR, p.wPhaseR, p.wAmp * a),
        handM: webHand(hipX - 0.09, shY, p.aUL, p.aFL, p.wPhaseL, p.wAmp * a),
        footP: CGPoint(x: cg((p.frx + p.rootX) / 190), y: cg(webFootY(p.fry, p.heelR))),
        footM: CGPoint(x: cg((p.flx + p.rootX) / 190), y: cg(webFootY(p.fly, p.heelL))),
        footRotP: (p.footRollR + p.heelR * 22) * webRad * a,
        footRotM: (p.footRollL - p.heelL * 22) * webRad * a,
        rod: min(max(abs(p.rootRot) / 3, 0), 1)
    )
}

private func webMirror(_ p: WebPose) -> WebPose {
    var q = p
    q.aUL = -p.aUR; q.aFL = -p.aFR; q.aUR = -p.aUL; q.aFR = -p.aFL
    q.wPhaseL = p.wPhaseR; q.wPhaseR = p.wPhaseL
    q.shL = p.shR; q.shR = p.shL
    q.lean = -p.lean; q.head = -p.head; q.rootRot = -p.rootRot
    q.footRollL = -p.footRollR; q.footRollR = -p.footRollL
    q.heelL = p.heelR; q.heelR = p.heelL
    q.flx = -p.frx; q.fly = p.fry; q.frx = -p.flx; q.fry = p.fly
    return q
}

private func webLerp(_ a: WebPose, _ b: WebPose, _ t: Double) -> WebPose {
    func l(_ x: Double, _ y: Double) -> Double { x + (y - x) * t }
    var p = WebPose()
    p.x = l(a.x, b.x); p.y = l(a.y, b.y)
    p.rootX = l(a.rootX, b.rootX); p.rootRot = l(a.rootRot, b.rootRot)
    p.lean = l(a.lean, b.lean); p.head = l(a.head, b.head)
    p.shL = l(a.shL, b.shL); p.shR = l(a.shR, b.shR)
    p.aUL = l(a.aUL, b.aUL); p.aFL = l(a.aFL, b.aFL)
    p.aUR = l(a.aUR, b.aUR); p.aFR = l(a.aFR, b.aFR)
    p.wPhaseL = l(a.wPhaseL, b.wPhaseL); p.wPhaseR = l(a.wPhaseR, b.wPhaseR)
    p.wAmp = l(a.wAmp, b.wAmp); p.knee = l(a.knee, b.knee)
    p.footRollL = l(a.footRollL, b.footRollL); p.footRollR = l(a.footRollR, b.footRollR)
    p.heelL = l(a.heelL, b.heelL); p.heelR = l(a.heelR, b.heelR)
    p.trem = l(a.trem, b.trem)
    p.flx = l(a.flx, b.flx); p.fly = l(a.fly, b.fly)
    p.frx = l(a.frx, b.frx); p.fry = l(a.fry, b.fry)
    return p
}

private func webMorph(_ f: Double, _ bounds: [Double], _ blends: [Double],
                      _ raw: (Double) -> WebPose) -> WebPose {
    for i in bounds.indices {
        let b = bounds[i], blend = blends[i]
        if f >= b && f < b + blend {
            return webLerp(raw(b - 0.0001), raw(f), smooth(cg((f - b) / blend)))
        }
    }
    return raw(f)
}

private func buildPhase(_ frames: Int, _ freqAt: (Double) -> Double) -> [Double] {
    var arr = [Double](repeating: 0, count: frames + 1)
    var ph = 0.0
    for i in 1...frames {
        ph += .pi * 2 * freqAt(Double(i - 1)) / 30
        arr[i] = ph
    }
    return arr
}

private func wave(_ phase: [Double], _ f: Double, _ lag: Double, _ seed: Double, _ amp: Double) -> Double {
    var i = Int(f)
    if i < 0 { i = 0 }
    if i >= phase.count { i = phase.count - 1 }
    let q = phase[i] - lag * 1.7 * .pi + seed * 0.8
    return amp * (sin(q) + 0.38 * sin(q * 1.87 + 1.1))
}

private func breakIcons(_ ps: inout [Pose], _ phase: [Double], _ f: Double, _ amp: Double) {
    guard amp >= 0.0001 else { return }
    for i in ps.indices {
        let z = Double(ps[i].z)
        var weight = z >= 1000 ? 0.88 : (z >= 900 ? 1.0 : (z >= 400 ? 0.56 : 0.1))
        weight += Double(hash01(i, 311)) * 0.08
        let seed = Double(hash01(i, 293)) * 0.8
        let k = 1 + weight * 2.4
        let w = wave(phase, f, weight, seed, amp)
        let dx = w * k * 0.00526
        let dy = wave(phase, f, weight + 0.17, seed + 1.7, amp) * k * 0.55 * 0.00526
        ps[i].p.x += cg(dx)
        ps[i].p.y += cg(dy)
        ps[i].rot += cg(w * 2.6 * .pi / 180)
        ps[i].sx *= 1 + cg(w * 0.035)
        ps[i].sy *= 1 + cg(w * 0.035)
    }
}

// MARK: - 闪身步（flash-step.html，540 帧 / 每拍 18 帧）

private func flashFreqAt(_ f: Double) -> Double {
    if f >= 72 && f < 114 { return 6 }
    if f >= 114 && f < 150 { return 8 }
    if f >= 150 && f < 180 { return 9 }
    return 4.5
}

private let flashPhase = buildPhase(540, flashFreqAt)

private func flashRootX(_ f: Double) -> Double {
    if f < 36 { return 0 }
    if f < 72 { return lerp(0, 16, easeInOutD((f - 36) / 36)) }
    if f < 114 { return lerp(16, 30, easeInOutD((f - 72) / 42)) }
    if f < 150 { return lerp(30, -95, easeOutQuart((f - 114) / 36)) }
    if f < 216 { return -95 }
    if f < 252 { return lerp(-95, -111, easeInOutD((f - 216) / 36)) }
    if f < 294 { return lerp(-111, -125, easeInOutD((f - 252) / 42)) }
    if f < 330 { return lerp(-125, 95, easeOutQuart((f - 294) / 36)) }
    if f < 396 { return 95 }
    if f < 468 { return lerp(95, 0, easeOutQuart((f - 396) / 72)) }
    return 0
}

private func flashStage(_ id: Int, _ t: Double) -> WebPose {
    let e = easeInOutD(t), q = easeOutQuart(t)
    var p = webBase(-36, 36)
    switch id {
    case 0:
        p.knee = 0.3 + 0.04 * sin(.pi * 2 * t)
        p.aUL = -26; p.aFL = 42; p.aUR = 26; p.aFR = -42
    case 1:
        p.aUL = lerp(-26, 56, e); p.aFL = lerp(42, 8, e)
        p.aUR = lerp(26, 30, e); p.aFR = lerp(-42, -14, e)
        p.lean = 9 * e; p.head = 4 * e; p.rootRot = 3 * e
        p.knee = 0.34 + 0.06 * e
        p.frx = 36 + 9 * e; p.fry = 118 + 9 * e
        p.shL = -3 * e; p.shR = 4 * e
    case 2:
        p.aUL = lerp(56, -48, e); p.aFL = lerp(8, 72, e)
        p.aUR = lerp(30, 48, e); p.aFR = lerp(-14, -72, e)
        p.lean = lerp(9, 15, e); p.rootRot = lerp(3, 7, e)
        p.knee = 0.4 + 0.26 * e
        p.shL = -8; p.shR = 8; p.trem = 0.12 * e
        p.frx = 45; p.fry = 127
    case 3:
        p.aUL = lerp(-48, -104, q); p.aFL = lerp(72, -8, q)
        p.aUR = lerp(48, 32, q); p.aFR = lerp(-72, -8, q)
        p.lean = lerp(15, -8, q); p.rootRot = lerp(7, -5, q)
        p.knee = lerp(0.66, 0.28, q)
        p.shL = lerp(-8, 5, q); p.shR = lerp(8, -5, q)
        p.trem = 0.25 * (1 - q)
        p.flx = lerp(-36, -58, q); p.fly = lerp(118, 106, q)
        p.frx = lerp(45, 30, q); p.fry = lerp(127, 118, q)
    case 4:
        let ft = t < 0.27 ? 0 : (t - 0.27) / 0.73
        let b = easeInOutD(ft)
        p.aUL = lerp(-104, -86, b); p.aFL = lerp(-8, 6, b)
        p.aUR = lerp(32, -28, b); p.aFR = lerp(-8, -4, b)
        p.lean = lerp(-8, 7, b); p.rootRot = lerp(-5, 5, b)
        p.knee = lerp(0.28, 0.58, b); p.trem = 0.34 * (1 - b)
        p.flx = lerp(-58, -36, b); p.fly = lerp(106, 118, b)
        p.frx = lerp(30, 36, b); p.fry = 118
    case 5:
        p.aUL = lerp(-86, -11, e); p.aFL = lerp(6, 9, e)
        p.aUR = lerp(-28, 11, e); p.aFR = lerp(-4, -9, e)
        p.lean = lerp(7, 0, e); p.rootRot = lerp(5, 0, e)
        p.knee = lerp(0.58, 0.25, e)
    case 6:
        let w = sin(.pi * 2 * t * 2)
        p.rootX = 34 * w * (1 - t * 0.65)
        p.x = 5 * w; p.lean = -7 * w; p.rootRot = -3 * w
        p.aUL = -40 - 28 * max(0, w); p.aFL = 16
        p.aUR = 40 + 28 * max(0, -w); p.aFR = -16
        p.knee = 0.34 + 0.1 * abs(w); p.trem = 0.08
    default:
        p.aUL = -18; p.aFL = 26; p.aUR = 18; p.aFR = -26; p.knee = 0.28
    }
    return p
}

private let flashSegS = [0, 36, 72, 114, 150, 180, 216, 252, 294, 330, 360, 396, 468]
private let flashSegL = [36.0, 36, 42, 36, 30, 36, 36, 42, 36, 30, 36, 72, 72]
private let flashSegId = [0, 1, 2, 3, 4, 5, 1, 2, 3, 4, 5, 6, 7]
private let flashSegM = [false, false, false, false, false, false, true, true, true, true, true, false, false]
private let flashBounds = [36.0, 72, 114, 150, 180, 216, 252, 294, 330, 360, 396, 468]
private let flashBlends = [Double](repeating: 6, count: 12)

private func flashRawAt(_ f: Double) -> WebPose {
    var i = flashSegS.count - 1
    for k in flashSegS.indices where f >= Double(flashSegS[k]) { i = k }
    var p = flashStage(flashSegId[i], (f - Double(flashSegS[i])) / flashSegL[i])
    if flashSegM[i] { p = webMirror(p) }
    p.rootX = i == 11 ? p.rootX + lerp(95, 0, easeOutQuart((f - 396) / 72)) : flashRootX(f)
    return p
}

private func flashPoseAt(_ f: Double) -> WebPose { webMorph(f, flashBounds, flashBlends, flashRawAt) }

// MARK: - MJ 太空步（mj.html，600 帧 / 每拍 15 帧）

private struct RobotPose {
    var aUL: Double = 0, aFL: Double = 0, aUR: Double = 0, aFR: Double = 0
    var head: Double = 0, shL: Double = 0, shR: Double = 0
}

private let moonRobot = [
    RobotPose(aUL: -90, aFL: 0, aUR: 90, aFR: 0),
    RobotPose(aUL: -90, aFL: -85, aUR: 20, aFR: 30, head: -10, shL: -8, shR: 4),
    RobotPose(aUL: -150, aFL: 30, aUR: 90, aFR: -85, head: 10, shL: 6, shR: -8),
    RobotPose(aUL: -20, aFL: 10, aUR: 150, aFR: -30, shR: 10),
    RobotPose(aUL: -95, aFL: -70, aUR: 95, aFR: 70, head: -6, shL: -6, shR: -6),
    RobotPose(aUL: -12, aFL: 8, aUR: 12, aFR: -8),
]

private let moonPhase = buildPhase(600) { _ in 5 }

private func moonSlide(_ p0: WebPose, _ step: Int, _ t: Double, _ sgn: Int) -> WebPose {
    var p = p0
    let e = easeInOutD(t)
    p.rootX = Double(sgn) * (90 - Double(step) * 45 - 45 * e)
    let slideLeft = step % 2 == 0
    if slideLeft {
        p.flx = lerp(-30, 8, e); p.fly = 118
        p.heelL = 0; p.footRollL = 0
        p.frx = 30; p.fry = 116; p.heelR = 0.9; p.footRollR = -10
    } else {
        p.frx = lerp(30, -8, e); p.fry = 118
        p.heelR = 0; p.footRollR = 0
        p.flx = -30; p.fly = 116; p.heelL = 0.9; p.footRollL = 10
    }
    p.knee = 0.3 + 0.06 * sin(.pi * t)
    p.lean = Double(sgn) * 3; p.rootRot = Double(sgn) * 2
    p.head = Double(sgn) * 4 * sin(.pi * t)
    p.shL = -6 * (slideLeft ? 1 : -1); p.shR = 6 * (slideLeft ? 1 : -1)
    p.aUL = -46; p.aFL = 28; p.aUR = 46; p.aFR = -28
    p.y = 1.5 * sin(.pi * t); p.trem = 0.05
    return p
}

private let moonBounds = [0.0, 30, 60, 180, 210, 220, 230, 240, 250, 260, 270, 330, 390, 420, 480, 540]
private let moonBlends = [6.0, 6, 6, 6, 6, 3, 3, 3, 3, 3, 6, 6, 6, 6, 6, 6]

private func moonRawAt(_ f0: Double) -> WebPose {
    var f = f0 - floor(f0 / 600) * 600
    if f < 0 { f += 600 }
    var p = webBase(-30, 30)
    if f < 30 {
        p.aUR = 150; p.aFR = 22; p.aUL = -18; p.aFL = 24; p.knee = 0.3; p.head = 4
    } else if f < 60 {
        let pulse = pow(max(0, 1 - (f - 30).truncatingRemainder(dividingBy: 15) / 15 * 2.2), 2)
        p.aUR = 150; p.aFR = 22; p.aUL = -22; p.aFL = 26
        p.shL = -10 * pulse; p.shR = 10 * pulse; p.head = 5 * pulse; p.knee = 0.3 + 0.08 * pulse
    } else if f < 180 {
        p = moonSlide(p, Int(floor((f - 60) / 30)), (f - 60).truncatingRemainder(dividingBy: 30) / 30, 1)
    } else if f < 210 {
        p.heelL = 1; p.heelR = 1; p.flx = -22; p.frx = 22; p.knee = 0.16
        p.aUL = -72; p.aFL = -14; p.aUR = 72; p.aFR = 14; p.rootRot = -2
    } else if f < 270 {
        let r = moonRobot[min(moonRobot.count - 1, Int(floor((f - 210) / 10)))]
        p.aUL = r.aUL; p.aFL = r.aFL; p.aUR = r.aUR; p.aFR = r.aFR
        p.head = r.head; p.shL = r.shL; p.shR = r.shR; p.knee = 0.28
    } else if f < 330 {
        let q1 = easeOutQuart(min(1, (f - 270) / 60 * 2.2))
        let q2 = easeOutQuart((f - 270) / 60)
        p.rootRot = 45 * q1; p.lean = 4 * q2
        p.aUL = -14; p.aFL = 10; p.aUR = 118; p.aFR = 40; p.knee = 0.2; p.head = -3
    } else if f < 390 {
        let a = .pi * 2 * 2 * (f - 330) / 60
        p.rootRot = 6 * sin(a); p.aUL = -70; p.aFL = 40; p.aUR = 70; p.aFR = -40
        p.knee = 0.24; p.trem = 0.2
    } else if f < 420 {
        p.heelL = 1; p.heelR = 0.35; p.frx = 44; p.fry = 106; p.knee = 0.22
        p.aUL = -100; p.aFL = -8; p.aUR = 130; p.aFR = 30; p.rootRot = -3
    } else if f < 480 {
        let s2 = Int(floor((f - 420) / 30))
        let t2 = (f - 420).truncatingRemainder(dividingBy: 30) / 30
        p.rootX = -50 + Double(s2) * 50 + 50 * easeInOutD(t2)
        p.frx = 30 + 26 * sin(.pi * t2); p.fry = 118 - 22 * sin(.pi * t2)
        p.aUL = -80; p.aFL = -12; p.aUR = 84; p.aFR = 12
        p.shL = -8 * sin(.pi * t2); p.shR = 8 * sin(.pi * t2)
        p.knee = 0.3; p.lean = -4 * sin(.pi * t2)
    } else if f < 540 {
        let step = Int(floor((f - 480) / 30))
        let st = (f - 480).truncatingRemainder(dividingBy: 30) / 30
        p = moonSlide(webBase(-30, 30), step, st, -1)
        p.rootX = -45 + Double(step) * 22 + 22 * easeInOutD(st)
    } else {
        p.aUR = 152; p.aFR = 20; p.aUL = -88; p.aFL = -10
        p.knee = 0.38; p.lean = -5; p.head = 6
    }
    return p
}

private func moonPoseAt(_ f: Double) -> WebPose { webMorph(f, moonBounds, moonBlends, moonRawAt) }

// MARK: - 科目三（subject-three.html，540 帧 / 每拍 15 帧，含摇花手显式参数）

private let flowerHotS = 318.0, flowerHotE = 366.0
private let flowerAmp = 32.0, flowerFastAmp = 55.0, flowerRadius = 15.0
private let subj3Phase = buildPhase(540) { f in
    (f >= flowerHotS && f < flowerHotE) ? 3.75 : 2.5
}

private func subj3Arms(_ n: Int, _ t: Double, _ p: inout WebPose) {
    switch n {
    case 1:
        let w = sin(.pi * 2 * 4 * t)
        p.aUL = -55 - 35 * w; p.aFL = 12; p.aUR = 55 - 35 * w; p.aFR = -12
    case 2:
        let a = .pi * 2 * 2 * t
        p.aUL = 70 + 62 * cos(a); p.aFL = 42 * sin(a)
        p.aUR = -70 + 62 * cos(a + .pi); p.aFR = -42 * sin(a + .pi)
    case 3:
        let h = t < 0.5 ? t / 0.5 : (t - 0.5) / 0.5
        p.aUL = -30; p.aFL = 18
        p.aUR = t < 0.5 ? lerp(20, -75, easeInOutD(h)) : lerp(-75, 155, easeInOutD(h))
        p.aFR = t < 0.5 ? -18 : -30
    case 4:
        let k = min(4, Int(floor(t * 5)))
        let u = t * 5 - Double(k)
        let seq = [[20.0, 10, -20, -10], [-60, 150, 60, -150], [-92, -4, 92, 4],
                   [62, -78, -62, 78], [-88, -6, 88, 6]]
        let kn = min(4, k + 1), q = easeInOutD(u)
        p.aUL = lerp(seq[k][0], seq[kn][0], q); p.aFL = lerp(seq[k][1], seq[kn][1], q)
        p.aUR = lerp(seq[k][2], seq[kn][2], q); p.aFR = lerp(seq[k][3], seq[kn][3], q)
    case 5:
        let u = t < 0.45 ? t / 0.45 : (t - 0.45) / 0.55
        if t < 0.45 {
            p.aUL = lerp(-20, 55, easeInOutD(u)); p.aFL = lerp(10, -70, easeInOutD(u))
            p.aUR = lerp(20, -55, easeInOutD(u)); p.aFR = lerp(-10, 70, easeInOutD(u))
        } else {
            p.aUL = lerp(55, 165, easeInOutD(u)); p.aFL = lerp(-70, 18, easeInOutD(u))
            p.aUR = lerp(-55, -165, easeInOutD(u)); p.aFR = lerp(70, -18, easeInOutD(u))
        }
    case 6:
        let lo = t < 0.28 ? 0.0 : (t < 0.72 ? 0.28 : 0.72)
        let hi = t < 0.28 ? 0.28 : (t < 0.72 ? 0.72 : 1.0)
        let cyc = t < 0.28 ? 3.0 : (t < 0.72 ? 1.0 : 3.0)
        let w = sin(.pi * 2 * cyc * ((t - lo) / (hi - lo)))
        p.aUL = -30 + 55 * w; p.aFL = 26; p.aUR = 30 + 55 * w; p.aFR = -26
    case 7:
        let u = easeInOutD(min(1, t * 1.3))
        p.aUL = lerp(62, 168, u); p.aFL = lerp(-72, 12, u)
        p.aUR = lerp(-62, -168, u); p.aFR = lerp(72, -12, u)
    case 8:
        let a = .pi * 2 * 2 * t
        p.aUL = -96; p.aFL = -6; p.aUR = 40 + 75 * cos(a); p.aFR = 60 * sin(a)
    case 9:
        let w = sin(.pi * 2 * 2 * t)
        let up = max(0, w), dn = max(0, -w)
        p.aUL = -96 + 20 * dn; p.aFL = -6 - 60 * up
        p.aUR = 96 - 20 * up; p.aFR = 6 + 60 * dn
    default:
        p.aUL = 70; p.aFL = -85; p.aUR = -70; p.aFR = 85
    }
}

private let subj3Bounds = [30.0, 78, 126, 174, 222, 270, 318, 366, 414, 462, 510]
private let subj3Blends = [Double](repeating: 6, count: 11)

private func subj3RawAt(_ f: Double) -> WebPose {
    var p = webBase(-30, 30)
    let hip = .pi * 2 * 1.25 * f / 30
    let sw = sin(hip), sw2 = sin(2 * hip)
    p.x = 11 * sw; p.lean = 7 * sw; p.head = -2 * sw
    p.knee = 0.3 + 0.1 * abs(sw); p.y = 2 * abs(sw)
    p.shL = -4 * sw; p.shR = 4 * sw
    p.heelL = max(0, sw); p.heelR = max(0, -sw)
    p.footRollL = -38 * max(0, sw); p.footRollR = 38 * max(0, -sw)
    p.flx = -30 - 7 * sw; p.fly = 118
    p.frx = 30 - 7 * sw; p.fry = 118
    p.rootX = 5 * sw2
    if f < 30 {
        let s0 = sin(.pi * f / 30)
        p.aUL = 70; p.aFL = -85; p.aUR = -70; p.aFR = 85
        p.lean = 12 * s0; p.rootRot = 4 * s0; p.head = 8 * s0
    } else if f < 510 {
        subj3Arms(Int((f - 30) / 48) + 1, (f - 30).truncatingRemainder(dividingBy: 48) / 48, &p)
    } else {
        p.aUL = 70; p.aFL = -85; p.aUR = -70; p.aFR = 85
    }
    let hot = f >= flowerHotS && f < flowerHotE
    p.wAmp = flowerRadius * (hot ? flowerFastAmp : flowerAmp) / flowerFastAmp
    let phase = subj3Phase[min(Int(f), 539)] * 180 / .pi
    p.wPhaseL = phase; p.wPhaseR = phase + 180
    p.trem = hot ? 0.14 : 0.05
    return p
}

private func subj3PoseAt(_ f: Double) -> WebPose { webMorph(f, subj3Bounds, subj3Blends, subj3RawAt) }

// MARK: - 哆嗦毛（原网页关键帧 + 肩部律动叠加，600 帧 / 每拍 18 帧）

private struct ShiverKey {
    var f: Double, lean: Double, head: Double, shL: Double, shR: Double
    var aUL: Double, aFL: Double, aUR: Double, aFR: Double
    var knee: Double, trem: Double, freq: Double
    init(_ f: Double, _ lean: Double, _ head: Double, _ shL: Double, _ shR: Double,
         _ aUL: Double, _ aFL: Double, _ aUR: Double, _ aFR: Double,
         _ knee: Double, _ trem: Double, _ freq: Double) {
        self.f = f; self.lean = lean; self.head = head; self.shL = shL; self.shR = shR
        self.aUL = aUL; self.aFL = aFL; self.aUR = aUR; self.aFR = aFR
        self.knee = knee; self.trem = trem; self.freq = freq
    }
}

private let shiverKeys = [
    ShiverKey(0, 0, 0, 0, 0, -11, 9, 11, -9, 0.25, 0, 4.5),
    ShiverKey(18, 0, 1, 0, 0, -11, 9, 11, -9, 0.33, 0, 4.5),
    ShiverKey(36, 0, -3, 0, 0, -16, 12, 16, -12, 0.2, 0, 4.5),
    ShiverKey(54, 0, 1, 0, 0, -11, 9, 11, -9, 0.38, 0, 4.5),
    ShiverKey(72, 0, 0, 0, 0, -16, 14, 16, -14, 0.3, 0, 4.5),
    ShiverKey(78, 0, 0, 0, 0, 180, -45, -180, 45, 0.18, 0, 4.5),
    ShiverKey(84, 3, 0, 0, 0, 70, -85, -70, 85, 0.66, 0, 4.5),
    ShiverKey(96, 0, 0, 0, 0, 70, -85, -70, 85, 0.3, 0, 4.5),
    ShiverKey(108, 0, 0, 0, 0, -16, 14, 16, -14, 0.3, 0, 4.5),
    ShiverKey(114, 0, 0, 0, 0, 180, -45, -180, 45, 0.18, 0, 4.5),
    ShiverKey(120, -3, 0, 0, 0, 70, -85, -70, 85, 0.66, 0, 4.5),
    ShiverKey(132, 0, 0, 0, 0, 70, -85, -70, 85, 0.3, 0, 4.5),
    ShiverKey(144, 0, 0, 0, 0, -11, 9, 11, -9, 0.42, 0.05, 4.6),
    ShiverKey(216, 0, 0, 0, 0, -11, 9, 11, -9, 0.44, 0.2, 5.2),
    ShiverKey(288, 0, 0, 0, 0, -11, 9, 11, -9, 0.46, 0.3, 5.8),
    ShiverKey(360, 0, 0, 0, 0, -90, -6, 90, 6, 0.54, 0.48, 6.6),
    ShiverKey(432, 0, 0, 0, 0, -90, -6, 90, 6, 0.68, 0.88, 8.4),
    ShiverKey(480, 0, 0, 0, 0, -90, -6, 90, 6, 0.76, 1.15, 9.4),
    ShiverKey(504, 0, 0, 0, 0, -11, 9, 11, -9, 0.72, 0.95, 8.6),
    ShiverKey(522, 0, 0, 0, 0, -24, 12, 24, -12, 0.56, 0.28, 6.2),
    ShiverKey(534, 0, 0, 0, 0, 70, -85, -70, 85, 0.82, 0.06, 5.0),
    ShiverKey(552, 0, 0, 0, 0, -11, 9, 11, -9, 0.35, 0, 4.5),
    ShiverKey(600, 0, 0, 0, 0, -11, 9, 11, -9, 0.25, 0, 4.5),
]

private struct ShiverFoot {
    var f: Double, lx: Double, ly: Double, rx: Double, ry: Double, step: Int
    init(_ f: Double, _ lx: Double, _ ly: Double, _ rx: Double, _ ry: Double, _ step: Int) {
        self.f = f; self.lx = lx; self.ly = ly; self.rx = rx; self.ry = ry; self.step = step
    }
}

private let shiverFeet = [
    ShiverFoot(0, -36, 118, 36, 118, 0),
    ShiverFoot(216, -36, 118, 36, 118, 1),
    ShiverFoot(234, -8, 106, 36, 118, 2),
    ShiverFoot(252, -8, 106, 2, 112, 1),
    ShiverFoot(270, -52, 118, 2, 112, 2),
    ShiverFoot(288, -52, 118, 44, 128, 1),
    ShiverFoot(306, -8, 116, 44, 128, 2),
    ShiverFoot(324, -8, 116, 10, 106, 1),
    ShiverFoot(342, -46, 118, 10, 106, 2),
    ShiverFoot(360, -46, 118, 48, 126, 1),
    ShiverFoot(378, -36, 118, 36, 118, 0),
    ShiverFoot(600, -36, 118, 36, 118, 0),
]

private struct ShiverPose {
    var lean: Double = 0, head: Double = 0, shL: Double = 0, shR: Double = 0
    var aUL: Double = 0, aFL: Double = 0, aUR: Double = 0, aFR: Double = 0
    var knee: Double = 0, trem: Double = 0, freq: Double = 0
    var x: Double = 0, flx: Double = 0, fly: Double = 0
    var frx: Double = 0, fry: Double = 0, lift: Double = 0, step: Int = 0
}

private func shiverPoseAt(_ f: Double) -> ShiverPose {
    func ramp(_ d: Double) -> Double { min(max(d / 8, 0), 1) }
    var i = 0
    while i < shiverKeys.count - 2 && shiverKeys[i + 1].f <= f { i += 1 }
    let a = shiverKeys[i], b = shiverKeys[min(i + 1, shiverKeys.count - 1)]
    let t = b.f == a.f ? 0 : min(max((f - a.f) / (b.f - a.f), 0), 1)
    let e = b.f - a.f <= 12 ? easeSnapD(t) : easeInOutD(t)
    func l(_ x: Double, _ y: Double) -> Double { x + (y - x) * e }
    var p = ShiverPose()
    p.lean = l(a.lean, b.lean); p.head = l(a.head, b.head)
    p.shL = l(a.shL, b.shL); p.shR = l(a.shR, b.shR)
    p.aUL = l(a.aUL, b.aUL); p.aFL = l(a.aFL, b.aFL)
    p.aUR = l(a.aUR, b.aUR); p.aFR = l(a.aFR, b.aFR)
    p.knee = l(a.knee, b.knee); p.trem = l(a.trem, b.trem); p.freq = l(a.freq, b.freq)
    var j = 0
    while j < shiverFeet.count - 2 && shiverFeet[j + 1].f <= f { j += 1 }
    let fa = shiverFeet[j], fb = shiverFeet[min(j + 1, shiverFeet.count - 1)]
    let u = fb.f == fa.f ? 0 : min(max((f - fa.f) / (fb.f - fa.f), 0), 1)
    let eu = easeInOutD(u)
    p.flx = lerp(fa.lx, fb.lx, eu); p.fly = lerp(fa.ly, fb.ly, eu)
    p.frx = lerp(fa.rx, fb.rx, eu); p.fry = lerp(fa.ry, fb.ry, eu)
    p.step = fa.step
    if p.step != 0 { p.lift = sin(.pi * u) }
    if f >= 144 && f < 216 {
        let r = ramp(f - 144) * ramp(216 - f), ph = .pi * 2 * ((f - 144) / 36)
        p.lean += r * 8 * sin(ph)
        p.shL += r * 9 * cos(ph); p.shR += r * 9 * cos(ph + 1.9477874452256718)
        p.aUL += r * 32 * sin(ph); p.aUR += r * 32 * sin(ph + .pi)
        p.aFL += r * 10 * cos(ph); p.aFR -= r * 10 * cos(ph)
        p.knee += r * 0.06 * (1 + cos(ph))
    }
    if f >= 216 && f < 360 {
        let r = ramp(f - 216) * ramp(360 - f)
        let k = Int(floor((f - 216) / 18)), u2 = (f - 216).truncatingRemainder(dividingBy: 18) / 18
        let even = k % 2 == 0
        let b1 = even ? 128.0 : -28, b2 = even ? 18.0 : -12
        let b3 = even ? 28.0 : -128, b4 = even ? 12.0 : -18
        let a1 = even ? -28.0 : 128, a2 = even ? -12.0 : 18
        let a3 = even ? -128.0 : 28, a4 = even ? -18.0 : 12
        let t4 = min(max(u2 / 0.45, 0), 1)
        let hold = u2 < 0.45 ? smooth(cg(u2 / 0.45)) : (u2 < 0.55 ? 1.0 : 1 - smooth(cg((u2 - 0.55) / 0.3)))
        let holdD = Double(hold)
        p.aUL = lerp(p.aUL, lerp(lerp(a1, b1, t4), 180, holdD * 0.82), r)
        p.aFL = lerp(p.aFL, lerp(lerp(a2, b2, t4), -45, holdD * 0.82), r)
        p.aUR = lerp(p.aUR, lerp(lerp(a3, b3, t4), -180, holdD * 0.82), r)
        p.aFR = lerp(p.aFR, lerp(lerp(a4, b4, t4), 45, holdD * 0.82), r)
        p.lean += r * Double(k % 4 == 1 ? 5 : -5) * sin(.pi * u2)
        p.x += r * (p.flx + p.frx) * 0.1
    }
    if f >= 360 && f < 432 {
        let r = ramp(f - 360) * ramp(432 - f)
        let w = 0.86 + 0.14 * sin(.pi * 2 * (f - 360) / 36)
        p.aUL = lerp(p.aUL, -90 * w, r); p.aUR = lerp(p.aUR, 90 * w, r)
        p.lean += r * 2.5 * sin(.pi * 2 * (f - 360) / 72)
    }
    if f >= 432 && f < 522 {
        let r = ramp(f - 432) * ramp(522 - f), w = sin(.pi * 2 * (f - 432) / 24)
        p.aUL += r * w * 9; p.aUR -= r * w * 9; p.head += r * w * 2.5
    }
    if f >= 36 && f < 534 {
        let r = ramp(f - 36) * ramp(534 - f)
        let u2 = (((f - 36).truncatingRemainder(dividingBy: 18)) + 18).truncatingRemainder(dividingBy: 18) / 18
        let q = pow(max(0, 1 - u2 * 2.2), 2)
        p.knee += r * q * 0.17 * (0.5 + p.trem)
    }
    if p.step == 1 { p.x += 6 * p.lift } else if p.step == 2 { p.x -= 6 * p.lift }
    return p
}

private let shiverPhase = buildPhase(600) { shiverPoseAt($0).freq }

private func shiverHand(_ shX: Double, _ shY: Double, _ a1: Double, _ a2: Double) -> CGPoint {
    let dx = sin(a1 * webRad) * 44 + sin((a1 + a2) * webRad) * 40
    let dy = cos(a1 * webRad) * 44 + cos((a1 + a2) * webRad) * 40
    let k = 0.7 / 190.0
    return CGPoint(x: cg(shX + dx * k), y: cg(shY - dy * k))
}

private func shiverFootY(_ ly: Double, _ stepping: Bool, _ lift: Double) -> Double {
    max(0, (118 - ly) * 0.5 / 190) + (stepping ? lift * 15 / 190 : 0)
}

extension Choreographer {
    private func skeleton(from b: WebBody) -> Skeleton {
        let lean = min(max(b.lean, -0.95), 0.95)
        let dip = min(max(b.dip, 0), 0.24)
        let hipX = min(max(b.hipX, -0.42), 0.42)
        let up = CGPoint(x: cg(sin(lean)), y: cg(cos(lean)))
        let right = CGPoint(x: cg(cos(lean)), y: cg(-sin(lean)))
        let t = min(max(b.rod, 0), 1)
        let hip0 = CGPoint(x: cg(hipX), y: 0) + up * cg(0.4 - dip)
        let hip = lerpP(CGPoint(x: cg(hipX), y: cg(0.4 - dip)), hip0, cg(t))
        let neck = hip + up * 0.3
        let torsoLean = lean + min(max(b.torso, -0.6), 0.6)
        let up2 = CGPoint(x: cg(sin(torsoLean)), y: cg(cos(torsoLean)))
        let right2 = CGPoint(x: cg(cos(torsoLean)), y: cg(-sin(torsoLean)))
        let headLean = lean + min(max(b.head, -0.5), 0.5)
        let head = neck + CGPoint(x: cg(sin(headLean)), y: cg(cos(headLean))) * 0.115
        let shrug = min(max(b.shrug, -0.05), 0.06)
        let shP = neck + right2 * 0.09 - up2 * cg(0.015 - shrug)
        let shM = neck - right2 * 0.09 - up2 * cg(0.015 - shrug)
        let aP = ik(shP, b.handP, 0.155, 0.155, bend: b.elbP)
        let aM = ik(shM, b.handM, 0.155, 0.155, bend: b.elbM)
        let hipP = hip + right * 0.05, hipM = hip - right * 0.05
        let lP = ik(hipP, b.footP, 0.215, 0.215, bend: b.kneP)
        let lM = ik(hipM, b.footM, 0.215, 0.215, bend: b.kneM)
        return Skeleton(head: head, neck: neck, hip: hip, shP: shP, elbP: aP.joint, handP: aP.end,
                        shM: shM, elbM: aM.joint, handM: aM.end, hipP: hipP, kneeP: lP.joint,
                        footP: b.footP, hipM: hipM, kneeM: lM.joint, footM: b.footM,
                        ball: .zero, lean: cg(lean), ballAlpha: 0,
                        footRotP: cg(b.footRotP), footRotM: cg(b.footRotM))
    }

    func figureFromSkeleton(_ n: Int, _ sk: Skeleton, wob: Double) -> [Pose] {
        let H = min(stage.height * 0.94, stage.width * 1.3)
        let origin = CGPoint(x: stage.midX, y: stage.minY + stage.height * 0.02)
        let weights: [CGFloat] = [.pi * 0.17, 0.3, 0.4, 0.4, 0.43, 0.43]
        let counts = allocate(n, weights)
        let spacing = weights.reduce(0, +) * H / CGFloat(max(1, n))
        let k = clamp(spacing / iconSize * 1.25, 0.34, 1.0)
        func scr(_ p: CGPoint) -> CGPoint { origin + p * H }
        var out: [Pose] = []
        out.reserveCapacity(n)
        let hc = counts[0]
        for j in 0..<hc {
            let a = .pi * 2 * CGFloat(j) / CGFloat(hc) + .pi / 2
            let p = hc < 3 ? sk.head : sk.head + CGPoint(x: cos(a), y: sin(a)) * 0.085
            out.append(Pose(p: scr(p), rot: sk.lean * 0.6 + cg(wob), sx: k, sy: k, z: 900 + CGFloat(j)))
        }
        func addLine(_ pts: [CGPoint], _ count: Int, _ z: CGFloat, _ rot: CGFloat) {
            for j in 0..<count {
                out.append(Pose(p: scr(samplePolyline(pts, (CGFloat(j) + 0.5) / CGFloat(count))),
                                rot: rot, sx: k, sy: k, z: z + CGFloat(j)))
            }
        }
        addLine([sk.neck, sk.hip], counts[1], 500, sk.lean * 0.6)
        addLine([sk.neck, sk.shP, sk.elbP, sk.handP], counts[2], 1000, cg(wob))
        addLine([sk.neck, sk.shM, sk.elbM, sk.handM], counts[3], 1100, cg(-wob))
        addLine([sk.hipP, sk.kneeP, sk.footP], counts[4], 300, sk.footRotP)
        addLine([sk.hipM, sk.kneeM, sk.footM], counts[5], 200, sk.footRotM)
        while out.count < n { out.append(Pose(p: scr(sk.hip), sx: k, sy: k)) }
        return Array(out.prefix(n))
    }

    func flash(_ n: Int, _ T: Tempo) -> [Pose] {
        guard n >= 8 else { return ring(n, T) }
        var f = (T.beat - modeBeat0) * 18
        f -= floor(f / 540) * 540
        if f < 0 { f += 540 }
        let a = min(Double(amp), 1.6)
        let p = flashPoseAt(f)
        var poses = figureFromSkeleton(n, skeleton(from: webBody(p, a)), wob: 0)
        breakIcons(&poses, flashPhase, f, p.trem * (0.8 + 0.45 * Double(T.energy)) * a * 1.6)
        return poses
    }

    func moonwalk(_ n: Int, _ T: Tempo) -> [Pose] {
        guard n >= 8 else { return ring(n, T) }
        var f = (T.beat - modeBeat0) * 15
        f -= floor(f / 600) * 600
        if f < 0 { f += 600 }
        let a = min(Double(amp), 1.6)
        let p = moonPoseAt(f)
        var poses = figureFromSkeleton(n, skeleton(from: webBody(p, a)), wob: 0)
        let tilting = f >= 270 && f < 330
        breakIcons(&poses, moonPhase, f, (tilting ? 0 : p.trem + 0.04) * a * 1.4)
        return poses
    }

    func subject3(_ n: Int, _ T: Tempo) -> [Pose] {
        guard n >= 8 else { return ring(n, T) }
        var f = (T.beat - modeBeat0) * 15
        f -= floor(f / 540) * 540
        if f < 0 { f += 540 }
        let a = min(Double(amp), 1.6)
        let p = subj3PoseAt(f)
        var poses = figureFromSkeleton(n, skeleton(from: webBody(p, a)), wob: 0)
        breakIcons(&poses, subj3Phase, f, p.trem * (0.9 + 0.6 * Double(T.energy)) * a * 1.6)
        return poses
    }

    func shiver(_ n: Int, _ T: Tempo) -> [Pose] {
        guard n >= 8 else { return ring(n, T) }
        var f = (T.beat - modeBeat0) * 18
        f -= floor(f / 600) * 600
        if f < 0 { f += 600 }
        let sp = shiverPoseAt(f)
        let a = min(Double(amp), 1.6)
        var dip = 0.075 + (sp.knee - 0.25) * 0.17
        dip += 0.018 * a * min(max(Double(T.accent) * 0.8 + Double(T.pulse) * 0.6, 0), 1.2)
        let shY = 0.7 - dip
        let hx = sp.x / 190
        var b = WebBody()
        b.lean = sp.lean * webRad; b.hipX = hx; b.dip = dip
        b.torso = (sp.shL - sp.shR) / 190 * 0.6
        b.head = sp.head * webRad
        b.shrug = -(sp.shL + sp.shR) / 2 / 190
        b.handP = shiverHand(hx + 0.09, shY, sp.aUR, sp.aFR)
        b.handM = shiverHand(hx - 0.09, shY, sp.aUL, sp.aFL)
        b.footP = CGPoint(x: cg(sp.frx / 190), y: cg(shiverFootY(sp.fry, sp.step == 2, sp.lift)))
        b.footM = CGPoint(x: cg(sp.flx / 190), y: cg(shiverFootY(sp.fly, sp.step == 1, sp.lift)))
        var poses = figureFromSkeleton(n, skeleton(from: b), wob: 0)
        breakIcons(&poses, shiverPhase, f, sp.trem * (0.8 + 0.45 * Double(T.energy)) * a)
        return poses
    }

    func rehearsal(_ n: Int, _ T: Tempo) -> [Pose] {
        let beats = [4.0, 100.0 / 3.0, 8.0, 8.0, 28.0]
        let modes: [DanceMode] = [.flash, .shiver, .moonwalk, .subject3, .figure]
        let total = beats.reduce(0, +)
        var q = T.beat - modeBeat0
        q -= floor(q / total) * total
        if q < 0 { q += total }
        var idx = modes.count - 1
        var rem = q
        for j in beats.indices {
            if rem < beats[j] || j == beats.count - 1 { idx = j; break }
            rem -= beats[j]
        }
        rem = min(max(rem, 0), beats[idx])
        let savedBeat = modeBeat0, savedClip = clipTime, savedFig = figPhase
        modeBeat0 = T.beat - rem
        let result: [Pose]
        switch modes[idx] {
        case .flash: result = flash(n, T)
        case .shiver: result = shiver(n, T)
        case .moonwalk: result = moonwalk(n, T)
        case .subject3: result = subject3(n, T)
        default:
            let frac = rem / beats[idx]
            clipTime = frac * (clip?.duration ?? 0)
            figPhase = frac * 16
            result = figure(n, T)
        }
        modeBeat0 = savedBeat; clipTime = savedClip; figPhase = savedFig
        return result
    }
}
