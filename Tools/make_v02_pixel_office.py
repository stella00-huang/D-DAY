"""Generate small original pixel sprites for the V0.2 Office art direction.

Furniture, animals and interactive objects are separate transparent sprites. The
two wide images contain only the wall and the tiled floor, never characters.
"""
from pathlib import Path
import random
from PIL import Image, ImageDraw, ImageFont

OUT = Path("Assets/Resources/ToiletBreakV02/Pixel")
OUT.mkdir(parents=True, exist_ok=True)
INK = "#514756"
SHADE = "#776577"
CREAM = "#FFF2D8"
WOOD = "#BD8969"
WOOD_DARK = "#8F6D61"
TEAL = "#6D9F9B"


def canvas(size):
    image = Image.new("RGBA", size, (0, 0, 0, 0))
    return image, ImageDraw.Draw(image)


def save(image, name):
    image.save(OUT / (name + ".png"))


def character(name, kind, frame=0):
    im, d = canvas((40, 52))
    coats = {"Pony": "#DA965F", "Cat": "#E5B9A0", "Bear": "#B58D75", "Rabbit": "#E6D8C3"}
    shirts = {"Pony": "#F9FAE9", "Cat": "#8BB5A6", "Bear": "#87A7B6", "Rabbit": "#D7AB8F"}
    coat, shirt = coats[kind], shirts[kind]
    tailShift = 1 if frame == 1 else 0
    if kind == "Pony":
        d.polygon([(7, 31), (1, 35), (3, 40), (10, 38)], fill=INK)
        d.polygon([(7, 32), (3, 36), (7, 38), (11, 36)], fill="#A66D66")
    elif kind == "Cat":
        d.line([(8, 36), (3, 34), (4, 26)], fill=INK, width=4)
        d.line([(8, 36), (3, 34), (4, 26)], fill=coat, width=2)
    elif kind == "Bear":
        d.ellipse((4, 32, 13, 40), fill=INK)
        d.ellipse((6, 33, 12, 38), fill=coat)
    else:
        d.ellipse((3, 32, 11, 40), fill=INK)
        d.ellipse((5, 34, 10, 38), fill=coat)

    # Short legs and broad silhouette are common to every animal.
    legL = 11 + (2 if frame == 2 else 0)
    legR = 23 - (2 if frame == 2 else 0)
    for x in (legL, legR):
        d.rectangle((x - 1, 38, x + 5, 46), fill=INK)
        d.rectangle((x, 38, x + 4, 44), fill="#596A79")
        d.rectangle((x - 2, 45, x + 6, 48), fill=INK)
        d.rectangle((x - 1, 45, x + 5, 47), fill="#705958")
    d.rounded_rectangle((8, 25, 30, 40), radius=5, fill=INK)
    d.rounded_rectangle((10, 26, 28, 39), radius=4, fill=shirt)
    d.rectangle((18, 28, 20, 37), fill="#DB7770")
    d.polygon([(18, 28), (22, 28), (20, 31)], fill="#EF9B84")
    d.rectangle((11, 37, 28, 39), fill="#6B6B74")
    d.rectangle((12, 39, 16, 42), fill=coat)
    d.rectangle((25, 39, 29, 42), fill=coat)

    if kind == "Pony":
        d.polygon([(10, 9), (7, 1), (12, 0), (17, 8)], fill=INK)
        d.polygon([(12, 7), (10, 2), (13, 3), (16, 10)], fill=coat)
        d.polygon([(26, 10), (29, 1), (34, 4), (31, 14)], fill=INK)
        d.polygon([(28, 11), (30, 4), (32, 5), (30, 13)], fill=coat)
    elif kind == "Cat":
        d.polygon([(7, 13), (8, 2), (18, 9)], fill=INK)
        d.polygon([(24, 9), (34, 2), (34, 15)], fill=INK)
        d.polygon([(10, 11), (11, 6), (17, 10)], fill="#E4A79B")
        d.polygon([(27, 10), (32, 6), (31, 13)], fill="#E4A79B")
    elif kind == "Bear":
        d.ellipse((6, 5, 17, 16), fill=INK)
        d.ellipse((26, 5, 37, 16), fill=INK)
        d.ellipse((8, 7, 15, 14), fill=coat)
        d.ellipse((28, 7, 35, 14), fill=coat)
    else:
        d.rounded_rectangle((8, 0, 16, 16), radius=3, fill=INK)
        d.rounded_rectangle((25, 0, 33, 16), radius=3, fill=INK)
        d.rectangle((11, 2, 13, 13), fill="#ECAAA6")
        d.rectangle((28, 2, 30, 13), fill="#ECAAA6")

    d.ellipse((6, 9, 36, 32), fill=INK)
    d.ellipse((8, 10, 34, 30), fill=coat)
    d.rectangle((10, 17, 15, 19), fill="#F2C8A9")
    d.rectangle((10, 22, 15, 24), fill="#C37F65")
    if kind == "Pony":
        d.polygon([(8, 13), (9, 5), (19, 3), (25, 9), (21, 15), (13, 12)], fill=INK)
        d.polygon([(10, 11), (12, 6), (19, 5), (23, 9), (20, 12)], fill="#7C5263")
        d.rounded_rectangle((24, 20, 39, 30), radius=4, fill=INK)
        d.rounded_rectangle((25, 21, 37, 28), radius=3, fill="#E8B58E")
        d.rectangle((34, 24, 35, 25), fill="#8A645A")
    elif kind == "Cat":
        d.rounded_rectangle((24, 23, 37, 30), radius=3, fill=INK)
        d.rounded_rectangle((25, 23, 35, 28), radius=3, fill="#F4D4C2")
        d.line([(26, 26), (35, 25)], fill="#AF8F83")
    elif kind == "Bear":
        d.ellipse((22, 21, 36, 31), fill="#E6C5A2")
        d.rectangle((33, 25, 35, 27), fill=INK)
    else:
        d.ellipse((23, 22, 36, 30), fill="#F6E7D1")
        d.rectangle((33, 26, 34, 27), fill="#C6908A")
    d.rectangle((23, 17, 26, 20), fill=INK)
    d.rectangle((24, 17, 25, 18), fill="#FFF8E8")
    d.rectangle((18, 31, 22, 34), fill="#F1C86E")

    if name.endswith("Walk1") or frame == 3:
        d.rectangle((6, 31, 10, 35), fill=coat)
    if name.endswith("UsePhone"):
        d.rectangle((31, 27, 37, 38), fill=INK)
        d.rectangle((32, 28, 36, 35), fill="#A8D5CC")
    if name.endswith("Sit"):
        d.rectangle((8, 39, 31, 44), fill=shirt)
        d.rectangle((8, 43, 31, 46), fill=INK)
    if name.endswith("Idle1"):
        d.rectangle((24, 18, 26, 19), fill=coat)
    save(im, name)


def pony_direction(name, back_view=False, step=False):
    im, d = canvas((40, 52))
    coat, shirt = "#DA965F", "#F9FAE9"
    for x, offset in ((10, 2 if step else 0), (24, -2 if step else 0)):
        d.rectangle((x + offset, 37, x + 5 + offset, 47), fill=INK)
        d.rectangle((x + 1 + offset, 38, x + 4 + offset, 44), fill="#596A79")
        d.rectangle((x - 1 + offset, 45, x + 6 + offset, 48), fill="#705958")
    d.rounded_rectangle((8, 25, 31, 40), radius=5, fill=INK)
    d.rounded_rectangle((10, 27, 29, 39), radius=4, fill=shirt)
    if back_view:
        d.polygon([(16, 39), (19, 39), (16, 45), (12, 42)], fill="#A66D66")
    else:
        d.rectangle((18, 28, 20, 37), fill="#DB7770")
        d.polygon([(17, 28), (22, 28), (20, 32)], fill="#EF9B84")
        d.rectangle((17, 33, 22, 36), fill="#F1C86E")
    for x in (9, 28):
        d.polygon([(x, 14), (x - 2, 2), (x + 4, 0), (x + 7, 14)], fill=INK)
        d.polygon([(x + 2, 11), (x, 3), (x + 3, 3), (x + 5, 12)], fill=coat)
    d.ellipse((5, 9, 35, 32), fill=INK)
    d.ellipse((7, 11, 33, 30), fill=coat)
    if back_view:
        d.polygon([(7, 13), (10, 5), (18, 3), (30, 9), (33, 20),
                   (27, 26), (18, 22), (10, 24)], fill="#7C5263")
        d.line((11, 18, 26, 21), fill="#A47178", width=2)
    else:
        d.polygon([(7, 13), (10, 5), (19, 3), (26, 10), (22, 15)], fill="#7C5263")
        d.ellipse((11, 21, 29, 31), fill="#E8B58E", outline=INK, width=1)
        d.rectangle((15, 24, 16, 25), fill="#8A645A")
        d.rectangle((24, 24, 25, 25), fill="#8A645A")
        d.rectangle((11, 17, 13, 19), fill=INK)
        d.rectangle((26, 17, 28, 19), fill=INK)
    save(im, name)


def wall():
    im = Image.new("RGBA", (480, 100), "#FFF0D9")
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 479, 7), fill="#E9D8C5")
    d.rectangle((0, 80, 479, 85), fill="#D8BA9B")
    d.rectangle((0, 86, 479, 99), fill="#A98777")
    for x in range(0, 480, 48):
        d.line((x, 9, x, 77), fill="#F7E5D0")
    save(im, "OfficeWall")


def floor():
    im = Image.new("RGBA", (480, 190), "#E7D2AF")
    d = ImageDraw.Draw(im)
    r = random.Random(77)
    for y in range(0, 190, 20):
        for x in range(0, 480, 24):
            fill = r.choice(["#E8D2AC", "#EAD6B3", "#E5CAA6", "#E9D1A9"])
            d.rectangle((x + 1, y + 1, x + 23, y + 19), fill=fill)
            if r.random() < .24:
                sx, sy = x + r.randint(4, 19), y + r.randint(4, 15)
                d.rectangle((sx, sy, sx + 1, sy), fill="#D6BD9D")
    d.rectangle((0, 0, 479, 5), fill="#AC8B76")
    save(im, "OfficeFloor")


def desk(name, color=WOOD):
    im, d = canvas((76, 50))
    d.polygon([(5, 16), (61, 7), (74, 22), (17, 32)], fill=INK)
    d.polygon([(8, 17), (60, 10), (70, 22), (18, 29)], fill="#D9AB82")
    d.polygon([(17, 31), (74, 22), (74, 35), (17, 44)], fill=WOOD_DARK)
    d.polygon([(5, 17), (17, 31), (17, 44), (5, 29)], fill="#8C645C")
    d.line([(18, 33), (73, 24)], fill="#EAC69E", width=2)
    d.rectangle((12, 41, 18, 49), fill=INK)
    d.rectangle((64, 35, 70, 49), fill=INK)
    d.rectangle((12, 42, 16, 48), fill=color)
    d.rectangle((65, 35, 69, 48), fill=color)
    save(im, name)


def computer():
    im, d = canvas((40, 38))
    d.polygon([(5, 5), (29, 1), (37, 8), (13, 13)], fill=INK)
    d.polygon([(5, 5), (13, 13), (13, 31), (5, 23)], fill="#586575")
    d.polygon([(13, 13), (37, 8), (37, 28), (13, 33)], fill=INK)
    d.polygon([(16, 15), (34, 11), (34, 26), (16, 30)], fill="#A5D5D0")
    d.polygon([(18, 17), (28, 15), (28, 20), (18, 22)], fill="#D9E9D8")
    d.rectangle((21, 31, 27, 34), fill=INK)
    d.polygon([(17, 34), (31, 31), (35, 35), (20, 38)], fill="#66737B")
    save(im, "Computer")


def chair():
    im, d = canvas((34, 43))
    d.rounded_rectangle((4, 3, 30, 29), radius=6, fill=INK)
    d.rounded_rectangle((7, 6, 27, 26), radius=5, fill="#7F9B78")
    d.polygon([(2, 27), (28, 23), (32, 32), (8, 37)], fill=INK)
    d.polygon([(6, 28), (27, 26), (29, 31), (9, 34)], fill="#8FB184")
    d.rectangle((16, 34, 19, 41), fill="#626573")
    d.line((5, 41, 30, 41), fill=INK, width=3)
    save(im, "Chair")


def door():
    im, d = canvas((48, 88))
    d.rectangle((3, 0, 44, 87), fill=INK)
    d.rectangle((6, 3, 41, 84), fill="#87AAA0")
    d.rectangle((9, 6, 38, 76), fill="#A7CBC0")
    d.rectangle((12, 10, 35, 38), fill="#9BBDB4")
    d.rectangle((15, 17, 32, 32), fill="#E9E1CA")
    d.rectangle((17, 19, 30, 29), fill="#69918A")
    d.rectangle((11, 42, 36, 44), fill="#6B9188")
    d.rectangle((11, 52, 36, 54), fill="#6B9188")
    d.rectangle((35, 59, 39, 65), fill=INK)
    d.rectangle((35, 60, 38, 63), fill="#F6D47C")
    d.rectangle((7, 79, 41, 84), fill="#6C8D84")
    save(im, "ToiletDoor")


def window():
    im, d = canvas((76, 58))
    d.rectangle((0, 0, 75, 57), fill=INK)
    d.rectangle((4, 4, 71, 53), fill="#E3EBD2")
    d.rectangle((7, 7, 68, 50), fill="#A7D6D4")
    d.polygon([(7, 47), (21, 31), (38, 45), (53, 33), (68, 44), (68, 50), (7, 50)], fill="#8EC5A7")
    d.polygon([(8, 31), (23, 8), (36, 8), (15, 43)], fill="#DBF1D8")
    d.rectangle((36, 5, 40, 53), fill="#F3E4C8")
    d.rectangle((4, 51, 71, 55), fill="#C79777")
    save(im, "Window")


def plant():
    im, d = canvas((34, 48))
    d.rectangle((12, 26, 21, 32), fill="#597D68")
    leaves = [(9, 14, 15, 29), (17, 6, 25, 27), (1, 18, 13, 30),
              (21, 18, 33, 31), (10, 1, 19, 21)]
    for i, box in enumerate(leaves):
        d.ellipse(box, fill=INK)
        d.ellipse((box[0] + 2, box[1] + 2, box[2] - 2, box[3] - 2),
                  fill=["#7CA974", "#A3C184", "#6F9A75"][i % 3])
    d.polygon([(5, 30), (29, 30), (25, 46), (9, 46)], fill=INK)
    d.polygon([(8, 33), (26, 33), (23, 43), (11, 43)], fill="#CE9677")
    save(im, "Plant")


def cooler():
    im, d = canvas((31, 59))
    d.rectangle((6, 22, 26, 56), fill=INK)
    d.rectangle((8, 24, 24, 53), fill="#D9E7DB")
    d.rounded_rectangle((4, 2, 28, 27), radius=5, fill=INK)
    d.rounded_rectangle((6, 4, 26, 23), radius=5, fill="#A9D9DC")
    d.rectangle((13, 27, 18, 32), fill="#81B1B3")
    d.rectangle((9, 37, 23, 44), fill="#96BAB5")
    d.rectangle((11, 38, 15, 41), fill="#F59E8D")
    save(im, "WaterCooler")


def printer():
    im, d = canvas((48, 40))
    d.polygon([(3, 14), (35, 7), (45, 18), (13, 25)], fill=INK)
    d.polygon([(6, 15), (34, 10), (42, 18), (14, 23)], fill="#EEECE0")
    d.polygon([(13, 25), (45, 18), (45, 34), (13, 39)], fill="#AAB4AE")
    d.polygon([(3, 14), (13, 25), (13, 39), (3, 29)], fill="#879C9C")
    d.polygon([(20, 4), (37, 1), (39, 13), (21, 16)], fill="#FFF8EA")
    d.line((19, 30, 39, 26), fill="#596C73", width=2)
    save(im, "Printer")


def cabinet():
    im, d = canvas((56, 51))
    d.polygon([(3, 12), (43, 3), (53, 13), (13, 22)], fill=INK)
    d.polygon([(5, 12), (42, 5), (50, 13), (13, 20)], fill="#CCAC8F")
    d.polygon([(13, 22), (53, 13), (53, 46), (13, 51)], fill="#A98370")
    d.polygon([(3, 12), (13, 22), (13, 51), (3, 41)], fill="#896E67")
    for y in (27, 37):
        d.line((18, y + 4, 48, y), fill=INK, width=2)
        d.rectangle((32, y + 1, 36, y + 3), fill="#F1D090")
    save(im, "Cabinet")


def small_props():
    im, d = canvas((19, 21))
    d.rectangle((1, 3, 16, 20), fill=INK)
    d.rectangle((3, 5, 14, 17), fill="#F8D796")
    d.rectangle((5, 8, 12, 9), fill="#CF8A73")
    d.rectangle((5, 12, 11, 13), fill="#CF8A73")
    save(im, "Folder")
    im, d = canvas((19, 20))
    d.rectangle((3, 5, 14, 17), fill=INK)
    d.rectangle((5, 6, 12, 16), fill="#F3B999")
    d.rectangle((14, 7, 18, 13), outline=INK, width=2)
    d.rectangle((6, 3, 11, 5), fill="#FFF5E3")
    save(im, "Mug")
    im, d = canvas((44, 31))
    d.ellipse((0, 3, 43, 28), fill="#786971")
    d.ellipse((3, 4, 40, 25), fill="#E8C7A7")
    d.ellipse((8, 8, 36, 23), fill="#F6DDB4")
    save(im, "Rug")
    im, d = canvas((22, 22))
    d.ellipse((1, 1, 20, 20), fill=INK)
    d.ellipse((3, 3, 18, 18), fill="#FFD773")
    d.rectangle((9, 5, 12, 12), fill="#775D58")
    d.rectangle((10, 15, 12, 17), fill="#775D58")
    save(im, "Interact")
    im, d = canvas((55, 34))
    d.rectangle((0, 0, 54, 33), fill=INK)
    d.rectangle((3, 3, 51, 30), fill="#C7BCA1")
    for x, y, color in [(8, 6, "#F6E6AA"), (24, 10, "#F8BB9F"), (37, 5, "#DFE5CB")]:
        d.rectangle((x, y, x + 9, y + 15), fill=color)
        d.rectangle((x + 2, y + 4, x + 7, y + 5), fill="#A28A7E")
    save(im, "NoticeBoard")


def main():
    wall(); floor(); desk("Desk"); computer(); chair(); door(); window()
    plant(); cooler(); printer(); cabinet(); small_props()
    character("PonyIdle0", "Pony", 0)
    character("PonyIdle1", "Pony", 1)
    character("PonyWalk0", "Pony", 2)
    character("PonyWalk1", "Pony", 3)
    character("PonyUsePhone", "Pony", 0)
    character("PonySit", "Pony", 0)
    pony_direction("PonyFront0")
    pony_direction("PonyFront1", step=True)
    pony_direction("PonyBack0", back_view=True)
    pony_direction("PonyBack1", back_view=True, step=True)
    character("CatIdle", "Cat")
    character("BearIdle", "Bear")
    character("RabbitIdle", "Rabbit")
    print("Generated", len(list(OUT.glob("*.png"))), "original pixel Office sprites in", OUT)


if __name__ == "__main__":
    main()
