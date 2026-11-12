"""Saatan Historia - Convierte el boceto de la selva en el mapa del laberinto.

Uso:
    pip install pillow numpy
    python Herramientas/boceto_a_laberinto.py Herramientas/boceto_selva.webp Assets/Mapas/selva_laberinto.png

Cómo lee el boceto (foto del cuaderno):
    - Trazos negros  -> borde del laberinto (fila de árboles con colisión).
    - Manchas verdes -> hierba alta.
    - Lo tachado en rojo (arriba) se ignora: solo se usa el recuadro RECORTE.
    - La abertura izquierda es la ENTRADA y la derecha la SALIDA.

El PNG resultante usa 1 píxel = 1 metro y la paleta de ConstructorSelva.cs:
    blanco = pasillo, negro = borde, verde (0,170,0) = hierba alta,
    verde oscuro (0,90,0) = bosque cerrado, azul = inicio, rosa = salida,
    transparente = nada.
"""
import sys
from collections import deque

import numpy as np
from PIL import Image

# ---- Ajustes (en píxeles del boceto) ----
RECORTE = (100, 495, 1955, 1435)   # x0, y0, x1, y1: zona del laberinto en la foto
PX_POR_METRO = 10                  # 10 px del dibujo = 1 m en el juego (pasillos de ~4-8 m)
ENTRADA_Y = (945, 1131)            # abertura del lado izquierdo
SALIDA_Y = (792, 880)              # abertura del lado derecho
MARGEN = 5                         # metros de bosque alrededor del laberinto
F = 5                              # resolución interna de análisis (px)


def dilatar(a, k):
    o = a.copy()
    for dj in range(-k, k + 1):
        for di in range(-k, k + 1):
            if dj * dj + di * di <= k * k:
                o |= np.roll(np.roll(a, dj, 0), di, 1)
    return o


def inundar(pared, semillas, conn8=False):
    h, w = pared.shape
    vec = [(1, 0), (-1, 0), (0, 1), (0, -1)]
    if conn8:
        vec += [(1, 1), (1, -1), (-1, 1), (-1, -1)]
    out = np.zeros((h, w), bool)
    q = deque()
    for s in semillas:
        if not pared[s] and not out[s]:
            out[s] = True
            q.append(s)
    while q:
        j, i = q.popleft()
        for dj, di in vec:
            a, c = j + dj, i + di
            if 0 <= a < h and 0 <= c < w and not pared[a, c] and not out[a, c]:
                out[a, c] = True
                q.append((a, c))
    return out


def vecinos(a, conn8=True):
    o = a.copy()
    for dj in (-1, 0, 1):
        for di in (-1, 0, 1):
            if conn8 or dj == 0 or di == 0:
                o |= np.roll(np.roll(a, dj, 0), di, 1)
    return o


def main(entrada, salida):
    im = np.asarray(Image.open(entrada).convert("RGB")).astype(int)
    x0, y0, x1, y1 = RECORTE
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    negro = (im.max(2) < 90)[y0:y1, x0:x1]
    verde = ((g > r + 40) & (g > b + 20))[y0:y1, x0:x1]

    # Rejilla fina de análisis.
    h, w = negro.shape[0] // F, negro.shape[1] // F

    def fino(a):
        return a[:h * F, :w * F].reshape(h, F, w, F).mean(axis=(1, 3))

    neg = fino(negro) > 0
    ver = fino(verde)

    e0, e1 = (ENTRADA_Y[0] - y0) // F, (ENTRADA_Y[1] - y0) // F
    s0, s1 = (SALIDA_Y[0] - y0) // F, (SALIDA_Y[1] - y0) // F

    # Exterior: se engrosan los trazos para cerrar huecos pequeños del dibujo
    # y se tapan la entrada y la salida.
    barrera = dilatar(neg, 2)
    barrera[e0:e1, 0:2] = True
    barrera[s0:s1, w - 2:] = True
    borde = [(j, 0) for j in range(h)] + [(j, w - 1) for j in range(h)] + \
            [(0, i) for i in range(w)] + [(h - 1, i) for i in range(w)]
    fuera = inundar(barrera, borde)
    alcanzable = inundar(neg, [((e0 + e1) // 2, 3)]) & ~fuera

    # A metros.
    k = PX_POR_METRO // F
    H, W = h // k, w // k

    def metro(a):
        return a[:H * k, :W * k].reshape(H, k, W, k).mean(axis=(1, 3))

    piso = metro(alcanzable) >= 0.4
    verde_m = metro(ver)
    fuera_m = metro(fuera) > 0.5

    P = MARGEN
    H2, W2 = H + 2 * P, W + 2 * P

    def margen(a, v):
        o = np.full((H2, W2), v, dtype=a.dtype)
        o[P:P + H, P:P + W] = a
        return o

    piso = margen(piso, False)
    verde_m = margen(verde_m, 0.0)
    fuera_m = margen(fuera_m, True)

    filas_e = sorted({P + int((y - y0) / PX_POR_METRO) for y in range(ENTRADA_Y[0] + 15, ENTRADA_Y[1] - 10, 4)})
    filas_s = sorted({P + int((y - y0) / PX_POR_METRO) for y in range(SALIDA_Y[0] + 13, SALIDA_Y[1] - 10, 4)})
    for j in filas_e:
        piso[j, P:P + 2] = True
    for j in filas_s:
        piso[j, P + W - 2:P + W] = True

    inicio = (filas_e[len(filas_e) // 2], P + 2)
    piso = inundar(~piso, [inicio])               # solo lo conectado con la entrada
    if not any(piso[j, P + W - 2] for j in filas_s):
        sys.exit("La salida no está conectada con la entrada: revisa ENTRADA_Y / SALIDA_Y.")

    muro = vecinos(piso) & ~piso                  # contorno de los pasillos = árboles

    cerca = muro.copy()
    for _ in range(4):
        cerca = vecinos(cerca, conn8=False)
    candidato = ~piso & ~muro & (~fuera_m | cerca)
    # El bosque tiene que tocar el borde (quita restos sueltos de la foto).
    bosque = inundar(~candidato, [tuple(p) for p in np.argwhere(vecinos(muro) & candidato)], conn8=True)

    pasto = vecinos(verde_m > 0.10, conn8=False) & piso

    out = np.zeros((H2, W2, 4), np.uint8)
    out[piso] = [255, 255, 255, 255]
    out[pasto] = [0, 170, 0, 255]
    out[muro] = [0, 0, 0, 255]
    out[bosque] = [0, 90, 0, 255]
    out[inicio] = [0, 100, 255, 255]
    for j in filas_s:
        if piso[j, P + W - 1]:
            out[j, P + W - 1] = [255, 105, 180, 255]

    Image.fromarray(out, "RGBA").save(salida)
    print(f"{salida}: {W2} x {H2} m | pasillo {int(piso.sum())} m2 | borde {int(muro.sum())} | "
          f"bosque {int(bosque.sum())} | hierba alta {int(pasto.sum())}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    main(sys.argv[1], sys.argv[2])
