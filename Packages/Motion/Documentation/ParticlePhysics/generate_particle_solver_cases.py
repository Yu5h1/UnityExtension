import json, math
import numpy as np

g = -9.81


def r7(v):
    return [round(float(c), 7) for c in v]


def base(**kw):
    w = dict(fixedDeltaTime=0.01, substeps=1, gravity=[0, 0, 0], damping=0.0, sor=1.0, contactWeight=1.0,
             frictionStatic=0.4, frictionKinetic=0.2, maxDepenetrationSpeed=5.0, groundY=-1000.0)
    w.update(kw)
    return w


def P(pos, vel=(0, 0, 0), invMass=1.0, radius=0.1, group=0, category=1):
    return dict(position=list(pos), velocity=list(vel), invMass=invMass, radius=radius,
                group=group, category=category)


TOL_EXACT = dict(position=1e-5, velocity=1e-3)
TOL_DELTA = dict(position=1e-4, velocity=2e-2)
cases = []


def add(**c):
    cases.append(c)


dt = 0.02; n = 30; h = dt / n; N = 10 * n
y = g * h * h * N * (N + 1) / 2
v = g * h * N
add(id="PSOLVE-CASE-01", name="自由落體", covers=["SOLVER-01", "SOLVER-02", "SOLVER-16"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0]), particles=[P((0, 0, 0))], steps=10,
    derivation="每子步驟 v_k = k·h·g，x_k = x_0 + h²·g·k(k+1)/2；N = 10 步 × 30 子步驟 = 300，h = 0.02/30。起點在原點，使 32 位元浮點數重建速度的誤差維持在容差內。",
    expected=dict(particles=[dict(index=0, position=r7([0, y, 0]), velocity=r7([0, v, 0]))]), tolerance=TOL_EXACT)

d = 2.0; vv = 0.0; yy = 1.0
for _ in range(N):
    vp = vv + h * g
    yy = yy + h * vp
    vv = vp * max(0.0, 1 - d * h)
add(id="PSOLVE-CASE-02", name="全域阻尼", covers=["SOLVER-16"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0], damping=d), particles=[P((0, 1, 0))], steps=10,
    derivation="每子步驟 u = v + h·g，x += h·u，v = u·max(0, 1 − damping·h)；遞推 300 次。",
    expected=dict(particles=[dict(index=0, position=r7([0, yy, 0]), velocity=r7([0, vv, 0]))]), tolerance=TOL_EXACT)

add(id="PSOLVE-CASE-03", name="釘住的粒子不動", covers=["SOLVER-02"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0]), particles=[P((0.5, 1, 0), invMass=0.0)], steps=10,
    derivation="invMass = 0 的粒子跳過預測、碰撞與速度更新，位置與速度維持初值。",
    expected=dict(particles=[dict(index=0, position=[0.5, 1, 0], velocity=[0, 0, 0])]), tolerance=TOL_EXACT)

add(id="PSOLVE-CASE-04", name="靜止在地面上", covers=["SOLVER-09"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0], groundY=0.0), particles=[P((0, 0.1, 0))], steps=10,
    derivation="y = groundY + r 起始且 v = 0：預測後低於地面 h²·|g|，推回 groundY + r；前一位置仍是 groundY + r，速度更新得 0。",
    expected=dict(particles=[dict(index=0, position=[0, 0.1, 0], velocity=[0, 0, 0])]), tolerance=TOL_EXACT)

two = [P((0, 0, 0), invMass=0.0), P((0, -1.2, 0))]


def con(**k):
    c = dict(a=0, b=1, restLength=1.0, compliance=0.0, damping=0.0, breakForce=0.0)
    c.update(k)
    return c


add(id="PSOLVE-CASE-05", name="單一距離約束剛好補到位", covers=["SOLVER-03", "SOLVER-05"],
    world=base(), particles=two, constraints=[con()], steps=1,
    derivation="C = 0.2，Δλ = −C/(w_A + w_B) = −0.2，δ_B = (0, 0.2, 0)，count = 1，SOR 1.0：x_B = (0, −1, 0)，v = 0.2/0.01 = 20。",
    expected=dict(particles=[dict(index=1, position=[0, -1.0, 0], velocity=[0, 20, 0])]), tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-06", name="多個修正平均後乘 SOR", covers=["SOLVER-05"],
    world=base(sor=1.5), particles=two, constraints=[con(), con()], steps=1,
    derivation="兩條相同約束各給 δ_B = 0.2，count = 2，平均 0.2，乘 SOR 1.5 得 0.3：y = −0.9，v = 30。",
    expected=dict(particles=[dict(index=1, position=[0, -0.9, 0], velocity=[0, 30, 0])]), tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-07", name="柔度", covers=["SOLVER-03"],
    world=base(), particles=two, constraints=[con(compliance=1e-4)], steps=1,
    derivation="α̃ = 1e-4/0.01² = 1，Δλ = −0.2/(1 + 1) = −0.1，δ_B = 0.1：y = −1.1，v = 10。",
    expected=dict(particles=[dict(index=1, position=[0, -1.1, 0], velocity=[0, 10, 0])]), tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-08", name="柔度為 0 時約束阻尼無效", covers=["SOLVER-03"],
    world=base(), particles=two, constraints=[con(damping=5.0)], steps=1,
    derivation="γ = compliance·damping/h = 0，結果與 PSOLVE-CASE-05 相同。",
    expected=dict(particles=[dict(index=1, position=[0, -1.0, 0], velocity=[0, 20, 0])]), tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-09", name="約束斷裂後保持斷開", covers=["SOLVER-04"],
    world=base(), particles=two, constraints=[con(breakForce=1000.0)], steps=3,
    derivation="約束力 |Δλ|/h² = 0.2/0.01² = 2000 N > 1000 N：第一個子步驟斷裂且不產生修正；之後不再求解，B 維持原位。",
    expected=dict(particles=[dict(index=1, position=[0, -1.2, 0], velocity=[0, 0, 0])],
                  constraints=[dict(index=0, broken=True)]), tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-10", name="未達斷裂門檻", covers=["SOLVER-04"],
    world=base(), particles=two, constraints=[con(breakForce=3000.0)], steps=1,
    derivation="約束力 2000 N ≤ 3000 N，不斷裂，結果同 PSOLVE-CASE-05。",
    expected=dict(particles=[dict(index=1, position=[0, -1.0, 0], velocity=[0, 20, 0])],
                  constraints=[dict(index=0, broken=False)]), tolerance=TOL_DELTA)


def contact(dist, ri=0.1, rj=0.1, vmax=5.0, hh=0.01):
    pen = min(ri + rj - dist, vmax * hh)
    return pen / 2


s = contact(0.16)
add(id="PSOLVE-CASE-11", name="粒子接觸每對處理一次", covers=["SOLVER-06"],
    world=base(), particles=[P((0, 0, 0)), P((0.16, 0, 0))], steps=1,
    derivation="穿透 = 0.2 − 0.16 = 0.04，s = 0.04/(1 + 1) = 0.02；這一對只產生一筆修正，各 count = 1；相對位移為 0，無摩擦。",
    expected=dict(particles=[dict(index=0, position=r7([-s, 0, 0]), velocity=r7([-s / 0.01, 0, 0])),
                             dict(index=1, position=r7([0.16 + s, 0, 0]), velocity=r7([s / 0.01, 0, 0]))]),
    tolerance=TOL_DELTA)

s = contact(0.05)
add(id="PSOLVE-CASE-12", name="最大去穿透速度", covers=["SOLVER-06"],
    world=base(), particles=[P((0, 0, 0)), P((0.05, 0, 0))], steps=1,
    derivation="原始穿透 0.15 超過 5 m/s × 0.01 s = 0.05，取 0.05；s = 0.025。",
    expected=dict(particles=[dict(index=0, position=r7([-s, 0, 0])), dict(index=1, position=r7([0.05 + s, 0, 0]))]),
    tolerance=TOL_DELTA)

add(id="PSOLVE-CASE-13", name="同一非零群組不互碰", covers=["SOLVER-08"],
    world=base(), particles=[P((0, 0, 0), group=3), P((0.16, 0, 0), group=3)], steps=1,
    derivation="兩粒子群組皆為 3（非零且相同），略過接觸，位置不變。",
    expected=dict(particles=[dict(index=0, position=[0, 0, 0]), dict(index=1, position=[0.16, 0, 0])]),
    tolerance=TOL_DELTA)

s = contact(0.36, 0.1, 0.3)
add(id="PSOLVE-CASE-14", name="每粒子半徑", covers=["SOLVER-06", "WORLD-11"],
    world=base(), particles=[P((0, 0, 0), radius=0.1), P((0.36, 0, 0), radius=0.3)], steps=1,
    derivation="接觸距離 = r_i + r_j = 0.4，穿透 0.04，s = 0.02。兩粒子半徑都是 0.1 時不會接觸。",
    expected=dict(particles=[dict(index=0, position=r7([-s, 0, 0])), dict(index=1, position=r7([0.36 + s, 0, 0]))]),
    tolerance=TOL_DELTA)

sphere = dict(type="sphere", center=[0, 0, 0], radius=1.0, mask=4294967295)
add(id="PSOLVE-CASE-15", name="球推出", covers=["SOLVER-10"],
    world=base(), particles=[P((0, 1.05, 0))], colliders=[sphere], steps=1,
    derivation="距離 1.05 < R + r = 1.1：推到 1.1；前一位置沿法向補到 1.1；修正前位移為 0，無摩擦；速度 0。",
    expected=dict(particles=[dict(index=0, position=[0, 1.1, 0], velocity=[0, 0, 0])]), tolerance=TOL_EXACT)

add(id="PSOLVE-CASE-16", name="碰撞分類不符時略過", covers=["SOLVER-13"],
    world=base(), particles=[P((0, 1.05, 0), category=1)], colliders=[dict(sphere, mask=2)], steps=1,
    derivation="粒子分類 0b01 與碰撞體的碰撞對象 0b10 交集為 0，不推出。",
    expected=dict(particles=[dict(index=0, position=[0, 1.05, 0], velocity=[0, 0, 0])]), tolerance=TOL_EXACT)

box = dict(type="box", center=[0, 0, 0], halfExtents=[1, 1, 1], axisX=[1, 0, 0], axisY=[0, 1, 0], axisZ=[0, 0, 1],
           mask=4294967295)
add(id="PSOLVE-CASE-17", name="方塊內部推到最近面", covers=["SOLVER-12"],
    world=base(), particles=[P((0, 0.9, 0))], colliders=[box], steps=1,
    derivation="中心在方塊內，到各面距離 x 1、y 0.1、z 1，最近為 +y；推到面上點 (0,1,0) 加 r，得 (0,1.1,0)；前一位置補到相同高度，速度 0。",
    expected=dict(particles=[dict(index=0, position=[0, 1.1, 0], velocity=[0, 0, 0])]), tolerance=TOL_EXACT)

dt = 0.02; n = 1; h = dt / n; N = 10 * n; muk = 0.2
vx = N * muk * (-g) * h
x = muk * (-g) * h * h * N * (N + 1) / 2
mbox = dict(box, center=[0, -0.5, 0], halfExtents=[100, 0.5, 100], linearVelocity=[1, 0, 0])
add(id="PSOLVE-CASE-18", name="移動方塊以動摩擦帶動粒子", covers=["SOLVER-12", "SOLVER-14"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0]), particles=[P((0, 0.1, 0))], colliders=[mbox], steps=10,
    derivation="頂面以 1 m/s 沿 x 移動，每步 1 個子步驟（h = 0.02），使穿透量 |g|·h² 遠大於 32 位元浮點數的捨入誤差。相對切向位移 h·(1 − v_x) 大於 μs·|g|·h²，取動摩擦：x 多移 μk·|g|·h²，v_x 每步增加 μk·|g|·h。10 步後 v_x = 10·μk·|g|·h，x = μk·|g|·h²·N(N+1)/2，尚未追上 1 m/s。靜止方塊時 x 與 v_x 維持 0。",
    expected=dict(particles=[dict(index=0, position=r7([x, 0.1, 0]), velocity=r7([vx, 0, 0]))]),
    tolerance=dict(position=1e-4, velocity=2e-3))

rest = np.array([[0.2, 0, 0], [-0.2, 0, 0], [0, 0.2, 0], [0, 0, 0.3]])
cm0 = rest.mean(0); q = rest - cm0
ang = math.radians(30)
R = np.array([[math.cos(ang), 0, math.sin(ang)], [0, 1, 0], [-math.sin(ang), 0, math.cos(ang)]])
cur = (R @ q.T).T + np.array([1, 0, 0])
pose_position = np.array([1, 0, 0]) - R @ cm0
add(id="PSOLVE-CASE-19", name="剛體群保持剛性", covers=["SOLVER-15"],
    world=base(), particles=[P(c) for c in rest], pose=dict(position=r7(pose_position), rotation=r7([math.cos(ang / 2), 0, math.sin(ang / 2), 0])),
    rigidGroups=[dict(particles=[0, 1, 2, 3])],
    steps=1,
    derivation="粒子的靜止佈局以姿勢（繞 y 軸 30°，位置使質心落在 (1,0,0)）放置，本身已是剛性；剛體群以配置姿勢的旋轉為起點，形狀匹配後位置不變，旋轉維持 (cos15°, 0, sin15°, 0)。",
    expected=dict(particles=[dict(index=i, position=r7(c)) for i, c in enumerate(cur)],
                  rigidGroups=[dict(index=0, rotation=r7([math.cos(ang / 2), 0, math.sin(ang / 2), 0]))]),
    tolerance=dict(position=1e-4, velocity=2e-2, rotation=1e-4))

dt = 0.02; n = 30; h = dt / n; r = 0.1; V = 4 / 3 * math.pi * r ** 3; ratio = 0.5; rho = ratio / V; Cd = 2.0
flow = np.array([1.0, 0, 0])
v = np.array([0, g * dt, 0])
yy = 1.0 + g * h * h * n * (n + 1) / 2
v = v - np.array([0, g, 0]) * ratio * dt
b = 1 - math.exp(-ratio * Cd * dt)
v = v + (flow - v) * b
med = dict(shape="box", center=[0, 0, 0], halfExtents=[100, 100, 100], density=rho, flow=flow.tolist(), dragCoefficient=Cd)
add(id="PSOLVE-CASE-20", name="介質受力一步", covers=["SOLVER-17"],
    world=base(fixedDeltaTime=dt, substeps=n, gravity=[0, g, 0]), particles=[P((0, 1, 0), radius=r)], media=[med], steps=1,
    derivation="求解步驟後 v = (0, g·dt, 0)。介質階段：比值 = 密度·(4/3)πr³·w = 0.5；v −= g·0.5·dt；blend = 1 − exp(−0.5·2·dt)；v += (flow − v)·blend。位置只受求解步驟影響。",
    expected=dict(particles=[dict(index=0, position=r7([0, yy, 0]), velocity=r7(v))]), tolerance=TOL_EXACT)

dt = 0.05
b = 1 - math.exp(-ratio * Cd * dt)
vt = g * dt * (1 - ratio) * (1 - b) / b
add(id="PSOLVE-CASE-21", name="介質中浮力、拖曳與流速的平衡", covers=["SOLVER-17"],
    world=base(fixedDeltaTime=dt, substeps=1, gravity=[0, g, 0]), particles=[P((0, 0, 0), vel=(0, -3, 0), radius=r)],
    media=[dict(med, flow=r7([0, -vt, 0]), halfExtents=[1000, 1000, 1000])], steps=240,
    derivation="無碰撞時每步求解後 v += g·dt；介質階段 v = (v − g·ratio·dt)·(1 − b) + flow·b，b = 1 − exp(−ratio·Cd·dt)。靜水中的穩態是終端速度 v_t = g·dt·(1 − ratio)·(1 − b)/b（dt 趨近 0 時趨近 g(1 − ratio)/(ratio·Cd)）；有垂直流速時穩態為 v_t + flow。本例 flow = −v_t，粒子以 −3 m/s 進入後減速到 0；240 步（12 秒，時間常數 1 秒）後與穩態差小於 1e-4。粒子留在原點附近，使 32 位元浮點數重建速度的誤差小於容差。只比對速度。",
    expected=dict(particles=[dict(index=0, velocity=[0, 0, 0])]), tolerance=dict(velocity=1e-3))

dt = 0.02; n = 4; h = dt / n; r = 0.05; xa, xb, va, vb = -0.14, 0.14, 5.0, -5.0
for _ in range(n):
    pa, pb = xa, xb
    xa, xb = xa + h * va, xb + h * vb
    dist = xb - xa
    if dist < 2 * r:
        s = min(2 * r - dist, 5.0 * h) / 2
        xa, xb = xa - s, xb + s
    va, vb = (xa - pa) / h, (xb - pb) / h
add(id="PSOLVE-CASE-22", name="快速相向的粒子在步驟中途接觸", covers=["SOLVER-01", "SOLVER-06"],
    world=base(fixedDeltaTime=dt, substeps=n), particles=[P((-0.14, 0, 0), vel=(5, 0, 0), radius=r), P((0.14, 0, 0), vel=(-5, 0, 0), radius=r)],
    steps=1,
    derivation="半徑 0.05，格子邊長 0.1；步驟開始時兩粒子相距 0.28，所在格子相差 3 格，不是鄰格。每子步驟（h = 0.005）各移動 0.025，第 4 個子步驟預測後相距 0.08，穿透 0.02 小於 5 m/s × h，各推開 0.01，得 x = ∓0.05、v = ±3 m/s。鄰近結構若只在步驟開始建立，這一步會漏掉接觸，結果變成 x = ∓0.04、v = ±5 m/s。",
    expected=dict(particles=[dict(index=0, position=r7([xa, 0, 0]), velocity=r7([va, 0, 0])),
                             dict(index=1, position=r7([xb, 0, 0]), velocity=r7([vb, 0, 0]))]),
    tolerance=TOL_DELTA)

wc = 2.0
xb = (wc * 0.05 - 0.05) / (wc + 1)
add(id="PSOLVE-CASE-23", name="接觸權重", covers=["SOLVER-05", "SOLVER-06"],
    world=base(contactWeight=wc),
    particles=[P((-0.3, 0, 0), invMass=0.0), P((0, 0, 0)), P((-0.15, 0, 0), invMass=0.0)],
    constraints=[con(a=0, b=1, restLength=0.25)], steps=1,
    derivation="A、C 釘住，B 在原點。A–B 約束靜止長度 0.25、目前 0.3，給 B δ = −0.05（拉向 A）；C–B 相距 0.15 < 0.2，穿透 0.05，給 B δ = +0.05（推離 C）。A–C 兩者都釘住，略過。contactWeight = 2 時 Σδ = 2·0.05 − 0.05 = 0.05，count = 1 + 2 = 3，x_B = 0.05/3，v = x_B/0.01。contactWeight = 1 時兩者抵消，B 不動。",
    expected=dict(particles=[dict(index=1, position=r7([xb, 0, 0]), velocity=r7([xb / 0.01, 0, 0]))]),
    tolerance=TOL_DELTA)

doc = dict(
    schema="yu5h1.motion.particle-solver-cases", version="1.3.0", spec="ParticleSolver 1.2.0",
    units=dict(length="m", time="s", mass="kg"),
    comparison="只比對 expected 列出的欄位，逐分量絕對誤差不得超過該案例的 tolerance。steps 是固定步數，每步 fixedDeltaTime 秒、substeps 個子步驟。未列出的世界設定採 ParticleSolver 預設值。media 在步後階段依列出順序作用。collider 的 linearVelocity 表示每步開始時姿勢依此速度平移、表面速度等於該速度。constraints 的 restLength 為明確指定的靜止長度。32 位元浮點數下由 (x − p)/h 重建的速度誤差約為 ulp(|x|)/h，案例的起點、子步驟與容差依此設定。particles 的 position 是實體本地座標；有 pose 的案例以 pose（position 與 [w,x,y,z] rotation）放置，沒有 pose 時即世界座標。rigidGroups 的 particles 是群組成員索引，靜止形狀取自粒子本地位置。",
    cases=cases)
import os
with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "ParticleSolverCases.json"), "w", encoding="utf-8", newline="\n") as f:
    f.write(json.dumps(doc, ensure_ascii=False, indent=2) + "\n")
print(len(cases))
for c in cases:
    print(c["id"], json.dumps(c["expected"]["particles"][0]))
