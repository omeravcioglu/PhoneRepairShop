"""
mockups: the game's screens composed from the real sprites, placed the way the S1 scene builder does
(anchors from the contract, whole-pixel pivots, 9-slice sizes from the code, UI strings from the game).
Each screen is 640x360, the game's native resolution.
"""

from __future__ import annotations

import os

import bench as B
import cast
import phone as P
import px
import pxfont as F
import scene
import shop
import ui

LINE = 11                                  # PixelText line height in the game
INK = px.C("ink", 1)
CREAM = px.C("rice", 4)
MINT = px.C("teal", 5)
SHADOW = px.C("ink", 0)
BENCH = -360                               # world y of the bench view's bottom row


def lines_at(cv, x, y, lines, colour, align="left", shadow=None):
    for i, s in enumerate(lines):
        if align == "center":
            F.text_center(cv, x, y + i * LINE, s, colour, shadow=shadow)
        else:
            F.text(cv, x, y + i * LINE, s, colour, shadow=shadow)


def text_size(lines):
    return max(F.measure(s) for s in lines), (len(lines) - 1) * LINE + 9


def button(scr, cx, cy, label, view_y0=0):
    """PixelButton: 9-sliced ui_button around a centred label, padding (7,5), min width 40, even sizes."""
    tw, th = text_size([label])
    w, h = max(40, tw + 14), th + 10
    w += w % 2
    h += h % 2
    b = px.nine_slice(ui.button(), w, h, (7, 7, 7, 7))
    scr.blit(b, cx - w // 2, scene.row(cy, view_y0) - h // 2)
    F.text_center(scr, cx, scene.row(cy + th // 2, view_y0), label, INK)


def panel(scr, cx, cy, w, h, body_lines, button_label, logo=None):
    paper = px.nine_slice(ui.paper(), w, h, (8, 8, 8, 8))
    scr.blit(paper, cx - w // 2, scene.row(cy) - h // 2)
    top = h // 2 - 12
    if logo is not None:
        lg = logo
        scr.blit(lg, cx - lg.w // 2, scene.row(cy + top) - 4)
        top -= lg.h - 6
    lines_at(scr, cx, scene.row(cy + top), body_lines, INK, "center")
    button(scr, cx, cy - h // 2 + 22, button_label)


def bubble(scr, text, x=360, y=312):
    lines = []
    for para in text.split("\n"):
        lines += F.wrap(para, 200)
    tw, th = text_size(lines)
    w, h = tw + 16, th + 14 + 4
    top = scene.row(y)
    scr.blit(px.nine_slice(ui.bubble(), w, h, (8, 8, 8, 8)), x, top)
    scr.blit(ui.bubble_tail(), x + 14, top + h - 1)
    scr.blit(ui.more_arrow(), x + w - 10 - 3, top + h - 9)
    lines_at(scr, x + 8, top + 7, lines, INK)


def day_plate(scr, day):
    scr.blit(px.nine_slice(ui.pill(), 56, 15, (6, 6, 6, 6)), 10, scene.row(350))
    F.text(scr, 16, scene.row(347), f"DAY {day}", CREAM)


# --------------------------------------------------------------------------------------------------
# Shop
# --------------------------------------------------------------------------------------------------

def shop_view(customer=None, expr="idle", at_door=None, sign_open=True, door_open=False, cat_blink=False,
              phone_on_counter=False, money="¥0", day=1, bell_pressed=False):
    scr = px.Canvas(640, 360)
    scr.blit(shop.build_shop_bg(), 0, 0)
    scene.place(scr, shop.door(door_open), 150, 112)
    if at_door:
        body, _ = cast.CAST[at_door]
        scene.place(scr, scene.half(body("idle")), 150, 112)
    scene.place(scr, shop.sign("OPEN" if sign_open else "CLOSED", not sign_open), 596, 330, "Center")
    if customer:
        body, _ = cast.CAST[customer]
        scene.place(scr, body(expr), 320, 44)
    scene.place(scr, shop.build_counter(), 0, 0, "BottomLeft")
    scene.place(scr, shop.register(), 548, 100)
    F.text_center(scr, 548, scene.row(149), money, MINT)
    scene.place(scr, shop.bell(bell_pressed), 470, 100)
    scene.place(scr, shop.cat(cat_blink), 84, 100)
    if phone_on_counter:
        scene.place(scr, shop.phone_on_counter(), 322, 101)
    day_plate(scr, day)
    return scr


def screen_title():
    scr = shop_view(sign_open=False, money="¥0")
    panel(scr, 320, 200, 300, 178,
          ["A tiny phone repair shop.", "Fix what they bring, get paid,", "and pet the cat."],
          "Open the shop  [Enter]", logo=ui.logo(256, 80, tagline=False))
    return scr


def screen_counter_talk():
    scr = shop_view("fumi", "talk_open", money="¥0")
    bubble(scr, "Good afternoon, dear! Is the repair person in? Oh, that's you!")
    return scr


def screen_choice():
    scr = shop_view("fumi", "talk", phone_on_counter=True, money="¥0")
    bubble(scr, "My Mochi One gets warm. Like a little hand warmer. Lovely in winter, but now it's all puffy...")
    button(scr, 500, 200, "[1] Leave it with me!")
    button(scr, 500, 176, "[2] Sorry, can't")
    return scr


def screen_walk_in():
    return shop_view(at_door="ren", door_open=True, cat_blink=True, money="¥7,800", day=2)


def screen_paid():
    scr = shop_view("fumi", "happy", money="¥2,400", bell_pressed=True)
    bubble(scr, "Cool as a cucumber! Here, dear, and take a mikan too.")
    return scr


def screen_summary():
    scr = shop_view(sign_open=False, money="¥7,800", cat_blink=True)
    panel(scr, 320, 200, 250, 160,
          ["DAY 1 - CLOSED", "", "Repairs: 3 of 3", "Earned: ¥7,800", "Tips: a mikan, a business card,", "a cat sticker"],
          "Next day  [Enter]")
    return scr


# --------------------------------------------------------------------------------------------------
# Bench
# --------------------------------------------------------------------------------------------------

def bench_view(customer, face="neutral", tool=1, status="", ticket=None):
    scr = px.Canvas(640, 360)
    scr.blit(B.build_bench_bg(), 0, 0)
    scene.place(scr, B.spares_box(), 540, -320, "Center", BENCH)
    for i, y in enumerate((-100, -142, -184, -226)):
        scene.place(scr, ui.tool_slot(i == tool), 32, y, "Center", BENCH)
        scene.place(scr, ui.tool_icon(("hands", "screwdriver", "spudger", "suction")[i]), 32, y, "Center", BENCH)
        F.text(scr, 32 - 17, scene.row(y + 18, BENCH), str(i + 1), CREAM, shadow=SHADOW)
    # ticket
    t = px.nine_slice(ui.ticket(), 190, 44, (7, 7, 7, 7))
    scr.blit(t, 8, 8)
    name, model, problem, pay = ticket
    F.text(scr, 15, 14, f"{name.upper()}  -  {model}", px.C("ink", 1))
    lines_at(scr, 15, 27, [problem, f"Pays {pay}"], px.C("ink", 2))
    # portrait
    scene.place(scr, ui.portrait_frame(), 226, -32, "Center", BENCH)
    _, face_fn = cast.CAST[customer]
    scene.place(scr, face_fn(face), 226, -32, "Center", BENCH)
    F.text(scr, 258, scene.row(-28, BENCH), f"{name} is watching", CREAM, shadow=SHADOW)
    # buttons and status line
    button(scr, 232, -326, "Turn over  [Space]", BENCH)
    button(scr, 404, -326, "Hand it back  [Enter]", BENCH)
    scr.blit(px.nine_slice(ui.pill(), 446, 16, (6, 6, 6, 6)), 5, 338)
    F.text(scr, 10, 342, status, CREAM)
    return scr


def place_phone(scr, layers, screws=(), screw_frame=0):
    ph = P.assemble(layers)
    scene.place(scr, ph, 232, -200, "Center", BENCH)
    s = P.screw(screw_frame)
    for i in screws:
        sx, sy = [(-40, 88), (40, 88), (40, -88), (-40, -88)][i]
        scene.place(scr, s, 232 + sx, -200 + sy, "Center", BENCH)


def in_spot(scr, sprite, cx, cy):
    """Centre a part's opaque pixels on a spot, as PartsTray2D does."""
    a = sprite.alpha_mask()
    ys, xs = a.nonzero()
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    part = sprite.crop(x0, y0, x1 - x0 + 1, y1 - y0 + 1)
    scene.place(scr, part, cx, cy, "Center", BENCH)


def cursor(scr, kind, x, y_row, ring=None):
    c = ui.cursor(kind)
    pv = {"hand": (4, 15), "hand_pressed": (4, 15), "screwdriver": (1, 1), "spudger": (1, 1), "suction": (6, 2)}[kind]
    if ring is not None:
        r = ui.ring(ring)
        scr.blit(r, x - 14 - 8, y_row - 12 - 8)
    scr.blit(c, x - pv[0], y_row - (c.h - 1 - pv[1]))


FUMI_TICKET = ("Fumi", "Mochi One", "Battery gets warm", "¥2,400")


def screen_bench_unscrew():
    scr = bench_view("fumi", "neutral", tool=1, status="Hold · Unscrew  -  circle anticlockwise to loosen faster",
                     ticket=FUMI_TICKET)
    place_phone(scr, ["phone_back_body", "phone_board", "phone_battery_swollen", "phone_battery_conn",
                      "phone_display_conn", "phone_cover"], screws=(0, 3))
    # the bottom-right screw is already out, in the tray
    in_spot(scr, P.screw(0), 484, -34)
    # the screwdriver on the top-right screw, ring 5/8
    scene.place(scr, P.screw(1), 272, -112, "Center", BENCH)
    cursor(scr, "screwdriver", 272, 112, ring=5)
    return scr


def screen_bench_open():
    scr = bench_view("fumi", "worried", tool=0, status="Swollen battery! Drop it in the spares box for a fresh one.",
                     ticket=FUMI_TICKET)
    place_phone(scr, ["phone_back_body", "phone_board", "phone_display_conn"])
    in_spot(scr, P.cover(), 366, -200)
    for i, (x, y) in enumerate(B.SLOTS[:4]):
        in_spot(scr, P.screw(0), x, -y)
    # the swollen battery (with its unplugged connector) lifted toward the spares box
    bat = P.battery_swollen()
    bat.blit(P.battery_conn(True), 0, 0)
    a = bat.alpha_mask()
    ys, xs = a.nonzero()
    part = bat.crop(xs.min(), ys.min(), xs.max() - xs.min() + 1, ys.max() - ys.min() + 1)
    # a soft shadow under it: it is in the air
    sh = px.Canvas(part.w, part.h)
    sh.fill(part.alpha_mask(), px.C("ink", 0, 90))
    scr.blit(sh, 470 - part.w // 2 - 6, 208 - part.h // 2 + 8)
    scr.blit(part, 470 - part.w // 2, 208 - part.h // 2)
    cursor(scr, "hand_pressed", 474, 196)
    return scr


SCREENS = {
    "01_title": screen_title,
    "02_counter_talk": screen_counter_talk,
    "03_choice": screen_choice,
    "04_walk_in": screen_walk_in,
    "05_bench_unscrew": screen_bench_unscrew,
    "06_bench_open": screen_bench_open,
    "07_paid": screen_paid,
    "08_day_summary": screen_summary,
}


def render_all(out_dir: str, scale: int = 2) -> list[str]:
    os.makedirs(out_dir, exist_ok=True)
    paths = []
    for name, fn in SCREENS.items():
        scr = fn()
        paths.append(scr.save(os.path.join(out_dir, f"screen_{name}.png"), scale))
    return paths
