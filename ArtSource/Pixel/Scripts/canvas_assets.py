"""
canvas_assets: 1x composites for the art-direction canvas (phone states, UI examples, the empty shop,
logo, type specimens, zoom crops). The canvas scales them up with image-rendering: pixelated.

    python canvas_assets.py      # writes ../Preview/canvas
"""

from __future__ import annotations

import os

import bench as B
import cast
import mockups as M
import phone as P
import px
import pxfont as F
import scene
import shop
import ui

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Preview", "canvas"))


def trim(cv: px.Canvas, pad: int = 0) -> px.Canvas:
    ys, xs = cv.alpha_mask().nonzero()
    x0, y0 = max(xs.min() - pad, 0), max(ys.min() - pad, 0)
    x1, y1 = min(xs.max() + pad, cv.w - 1), min(ys.max() + pad, cv.h - 1)
    return cv.crop(x0, y0, x1 - x0 + 1, y1 - y0 + 1)


def build() -> dict[str, px.Canvas]:
    out: dict[str, px.Canvas] = {}
    # phone states
    back = ["phone_back_body", "phone_board"]
    out["phone_front"] = P.assemble(["phone_front_body", "phone_screen"])
    out["phone_front_cracked"] = P.assemble(["phone_front_body", "phone_screen_cracked"])
    out["phone_front_open"] = P.assemble(["phone_front_body"])
    out["phone_back_closed"] = P.assemble(back + ["phone_battery", "phone_battery_conn", "phone_display_conn",
                                                  "phone_cover"], screws=True)
    out["phone_back_open"] = P.assemble(back + ["phone_battery", "phone_battery_conn", "phone_display_conn"])
    out["phone_back_swollen"] = P.assemble(back + ["phone_battery_swollen", "phone_battery_conn_open",
                                                   "phone_display_conn_open"])
    out["phone_back_empty"] = P.assemble(back + ["phone_display_conn_open"])

    # UI examples, laid out exactly as the game does
    cv = px.Canvas(240, 50)
    M.bubble(cv, "Good afternoon, dear! Is the repair person in? Oh, that's you!", x=1, y=360 - 1)
    out["ui_ex_bubble"] = trim(cv)
    cv = px.Canvas(190, 44)
    cv.blit(px.nine_slice(ui.ticket(), 190, 44, (7, 7, 7, 7)), 0, 0)
    F.text(cv, 7, 6, "FUMI  -  Mochi One", px.C("ink", 1))
    M.lines_at(cv, 7, 19, ["Battery gets warm", "Pays ¥2,400"], px.C("ink", 2))
    out["ui_ex_ticket"] = cv
    cv = px.Canvas(56, 15)
    cv.blit(px.nine_slice(ui.pill(), 56, 15, (6, 6, 6, 6)), 0, 0)
    F.text(cv, 6, 3, "DAY 1", M.CREAM)
    out["ui_ex_day"] = cv
    cv = px.Canvas(446, 16)
    cv.blit(px.nine_slice(ui.pill(), 446, 16, (6, 6, 6, 6)), 0, 0)
    F.text(cv, 5, 4, "Hold · Unscrew  -  circle anticlockwise to loosen faster", M.CREAM)
    out["ui_ex_status"] = cv
    cv = px.Canvas(170, 104)
    M.button(cv, 85, 360 - 12, "[1] Leave it with me!")
    M.button(cv, 85, 360 - 36, "[2] Sorry, can't")
    M.button(cv, 85, 360 - 66, "Turn over  [Space]")
    M.button(cv, 85, 360 - 90, "Hand it back  [Enter]")
    out["ui_ex_buttons"] = trim(cv)
    cv = px.Canvas(640, 360)
    M.panel(cv, 320, 200, 300, 178, ["A tiny phone repair shop.", "Fix what they bring, get paid,", "and pet the cat."],
            "Open the shop  [Enter]", logo=ui.logo(256, 80, tagline=False))
    out["ui_ex_title_card"] = trim(cv)
    cv = px.Canvas(640, 360)
    M.panel(cv, 320, 200, 250, 160, ["DAY 1 - CLOSED", "", "Repairs: 3 of 3", "Earned: ¥7,800",
                                     "Tips: a mikan, a business card,", "a cat sticker"], "Next day  [Enter]")
    out["ui_ex_summary"] = trim(cv)

    # the empty shop and a far-away customer
    out["shop_empty"] = M.shop_view(money="¥0")
    out["cust_sasaki_far"] = scene.half(cast.sasaki_body("idle"))

    # logo, emblem, type
    out["logo"] = ui.logo(256, 80, tagline=False)
    out["logo_tagline"] = ui.logo(256, 104, tagline=True)
    out["emblem"] = trim(ui.rice_bowl_phone(48, 48, 24, 32, 1.0), 1)
    out["type_small"] = F.font_sheet(px.C("ink", 1), px.C("rice", 4))
    st = px.Canvas(300, 40, px.C("rice", 4))
    m = F.stroke_text(300, 40, 6, 6, "ABCDEFGHIJKLM", 12, 1.6, 1.4)
    st.fill(m, px.C("ink", 1))
    m2 = F.stroke_text(300, 40, 6, 23, "NOPQRSTUVWXYZ ?!", 12, 1.6, 1.4)
    st.fill(m2, px.C("ink", 1))
    out["type_stroke"] = st
    mc = px.Canvas(150, 24, px.C("rice", 4))
    F.micro(mc, 3, 3, "ABCDEFGHIJKLMNOPQRSTUVWXYZ", px.C("ink", 1))
    F.micro(mc, 3, 10, "0123456789 ¥2,400 3000MAH", px.C("ink", 1))
    F.micro(mc, 3, 17, "SCREWS CABLES SPARES", px.C("coral", 2))
    out["type_micro"] = mc

    # zoom crops for the pixel rules
    out["zoom_fumi_head"] = cast.fumi_body("idle").crop(18, 4, 60, 56)
    out["zoom_battery"] = P.battery_swollen().crop(18, 82, 68, 92)
    return out


def write() -> dict[str, str]:
    os.makedirs(OUT, exist_ok=True)
    paths = {}
    for name, cv in build().items():
        paths[name] = cv.save(os.path.join(OUT, name + ".png"))
    return paths


if __name__ == "__main__":
    for k, v in write().items():
        print(k, v)
