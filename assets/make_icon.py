import os
import math
from PIL import Image, ImageDraw

def create_icon():
    canvas = 1024
    img = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    center_x = 512
    center_y = 512
    radius = 340
    stroke = 64

    # Strict Monochrome: Crisp White and Silver/Slate White
    WHITE = (255, 255, 255, 255)
    LIGHT_GRAY = (200, 205, 215, 255)

    bbox = [center_x - radius, center_y - radius, center_x + radius, center_y + radius]
    
    # Arc 1
    draw.arc(bbox, start=25, end=155, fill=WHITE, width=stroke)
    # Arc 2
    draw.arc(bbox, start=205, end=335, fill=WHITE, width=stroke)
    
    # Arrow 1 at 160 deg
    rad1 = math.radians(160)
    tip1_x = center_x + radius * math.cos(rad1)
    tip1_y = center_y + radius * math.sin(rad1)
    p1_tip = (tip1_x - 40, tip1_y + 80)
    p1_a = (tip1_x - 110, tip1_y - 20)
    p1_b = (tip1_x + 50, tip1_y - 40)
    draw.polygon([p1_tip, p1_a, p1_b], fill=WHITE)
    
    # Arrow 2 at 340 deg
    rad2 = math.radians(340)
    tip2_x = center_x + radius * math.cos(rad2)
    tip2_y = center_y + radius * math.sin(rad2)
    p2_tip = (tip2_x + 40, tip2_y - 80)
    p2_a = (tip2_x + 110, tip2_y + 20)
    p2_b = (tip2_x - 50, tip2_y + 40)
    draw.polygon([p2_tip, p2_a, p2_b], fill=WHITE)

    # Minimalist taskbar bar at center
    bar_w = 340
    bar_h = 56
    bar_x0 = center_x - bar_w // 2
    bar_y0 = center_y - bar_h // 2
    bar_x1 = center_x + bar_w // 2
    bar_y1 = center_y + bar_h // 2
    
    draw.rounded_rectangle([bar_x0, bar_y0, bar_x1, bar_y1], radius=28, fill=WHITE)
    
    # Taskbar indicator dots
    dot1_x = center_x - 90
    draw.ellipse([dot1_x - 14, center_y - 14, dot1_x + 14, center_y + 14], fill=(70, 75, 85, 255))
    dot2_x = center_x
    draw.ellipse([dot2_x - 14, center_y - 14, dot2_x + 14, center_y + 14], fill=(70, 75, 85, 255))
    dot3_x = center_x + 90
    draw.ellipse([dot3_x - 14, center_y - 14, dot3_x + 14, center_y + 14], fill=(70, 75, 85, 255))

    return img

out_dir = r"C:\Users\ricry\Desktop\Scripts\Refresh Taskbar"
highres = create_icon()
png_path = os.path.join(out_dir, "refresh.png")
highres.resize((256, 256), Image.Resampling.LANCZOS).save(png_path, "PNG")

sizes = [(16, 16), (20, 20), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
icon_images = [highres.resize(size, Image.Resampling.LANCZOS) for size in sizes]
ico_path = os.path.join(out_dir, "refresh.ico")
icon_images[-1].save(ico_path, format="ICO", sizes=sizes, append_images=icon_images[:-1])
print("Updated monochrome icon generated.")
