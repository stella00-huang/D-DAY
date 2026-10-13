"""Extend the project's own 40x52 pixel animal set for D-DAY."""
from pathlib import Path
from PIL import Image, ImageDraw

SOURCE = Path("Assets/Resources/ToiletBreakV02/Pixel")
OUT = Path("Assets/Resources/DDay/Pixel")
OUT.mkdir(parents=True, exist_ok=True)
INK = (81, 71, 86, 255)


def recolor(source, replacements):
    image = Image.open(SOURCE / (source + ".png")).convert("RGBA")
    pixels = image.load()
    for y in range(image.height):
        for x in range(image.width):
            pixels[x, y] = replacements.get(pixels[x, y], pixels[x, y])
    return image


cat_coat = (229, 185, 160, 255)
cat_muzzle = (244, 212, 194, 255)
cat_inner = (228, 167, 155, 255)
cat_shirt = (139, 181, 166, 255)
bear_coat = (181, 141, 117, 255)
bear_shirt = (135, 167, 182, 255)

wolf = recolor("CatIdle", {
    cat_coat: (151, 163, 174, 255), cat_muzzle: (203, 213, 216, 255),
    cat_inner: (168, 146, 154, 255), cat_shirt: (86, 119, 143, 255),
})
wolf.save(OUT / "WolfIdle.png")

fox = recolor("CatIdle", {
    cat_coat: (224, 139, 83, 255), cat_muzzle: (255, 226, 188, 255),
    cat_inner: (194, 102, 95, 255), cat_shirt: (230, 221, 185, 255),
})
fox.save(OUT / "FoxIdle.png")

shiba = recolor("BearIdle", {
    bear_coat: (215, 155, 91, 255), (230, 197, 162, 255): (252, 228, 181, 255),
    bear_shirt: (106, 164, 158, 255),
})
ImageDraw.Draw(shiba).polygon([(7, 14), (8, 1), (18, 10)], fill=INK)
ImageDraw.Draw(shiba).polygon([(27, 10), (34, 1), (35, 16)], fill=INK)
ImageDraw.Draw(shiba).polygon([(10, 9), (11, 5), (16, 10)], fill=(215, 155, 91, 255))
ImageDraw.Draw(shiba).polygon([(29, 10), (33, 5), (33, 13)], fill=(215, 155, 91, 255))
shiba.save(OUT / "ShibaIdle.png")

panda = recolor("BearIdle", {
    bear_coat: (240, 236, 221, 255), (230, 197, 162, 255): (250, 244, 225, 255),
    bear_shirt: (116, 154, 165, 255),
})
d = ImageDraw.Draw(panda)
d.ellipse((8, 7, 15, 14), fill=INK)
d.ellipse((28, 7, 35, 14), fill=INK)
d.ellipse((20, 13, 30, 23), fill=(100, 95, 104, 255))
d.rectangle((24, 17, 26, 19), fill=INK)
d.rectangle((25, 17, 25, 18), fill=(255, 250, 235, 255))
panda.save(OUT / "PandaIdle.png")

coffee = Image.new("RGBA", (39, 59), (0, 0, 0, 0))
d = ImageDraw.Draw(coffee)
d.rounded_rectangle((2, 7, 37, 56), radius=4, fill=INK)
d.rounded_rectangle((4, 9, 35, 54), radius=3, fill="#91B4AE")
d.rectangle((7, 13, 32, 28), fill=INK)
d.rectangle((9, 15, 30, 26), fill="#E6E4CF")
d.ellipse((17, 17, 22, 23), fill="#8F6D61")
d.rectangle((10, 32, 29, 34), fill=INK)
d.rectangle((14, 38, 25, 48), fill="#FFF2D8")
d.rectangle((24, 40, 28, 44), fill=INK)
d.rectangle((17, 35, 22, 38), fill="#BD8969")
d.rectangle((5, 53, 34, 57), fill="#776577")
coffee.save(OUT / "CoffeeMachine.png")
print("Created", len(list(OUT.glob('*.png'))), "D-DAY sprites")
