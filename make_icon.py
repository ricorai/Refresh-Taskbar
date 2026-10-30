import os
import math
from PIL import Image, ImageDraw

def create_icon():
    canvas = 1024
    img = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Coordinates and scale
    center_x = 512
    center_y = 512

    # Modern minimalist dual-arc circular arrows
    # Outer radius = 380, Inner radius = 300 (width = 80)
    radius = 340
    stroke = 64

    # Colors: Crisp Clean White with subtle Electric Cyan accents
    # White primary arrow, Electric Cyan secondary arrow (#00E5FF)
    WHITE = (255, 255, 255, 255)
    CYAN = (0, 229, 255, 255)

    # Arc 1: Top-Right to Bottom-Right (Sweep from -120 to 60 deg)
    # Arc 2: Bottom-Left to Top-Left (Sweep from 60 to 240 deg)
    
    # We will draw a sleek, modern, balanced circular refresh symbol
    # Arc 1 (Cyan): from angle 20 deg to 160 deg
    # Arc 2 (White): from angle 200 deg to 340 deg
    
    bbox = [center_x - radius, center_y - radius, center_x + radius, center_y + radius]
    
    # Draw Arc 1 (Cyan)
    draw.arc(bbox, start=25, end=155, fill=CYAN, width=stroke)
    
    # Draw Arc 2 (White)
    draw.arc(bbox, start=205, end=335, fill=WHITE, width=stroke)
    
    # Arrow heads:
    # Arrow 1 (at angle 160 deg):
    # Center of arc at 160 deg:
    rad1 = math.radians(160)
    tip1_x = center_x + radius * math.cos(rad1)
    tip1_y = center_y + radius * math.sin(rad1)
    
    # Arrow head 1 points outward/tangent in direction of circulation (counter-clockwise or clockwise)
    # Clockwise: at 160 deg (bottom-left quadrant), tangent points down-right (dy/dx)
    # Let's construct a sharp modern triangle arrow head:
    # Tangent vector at angle theta: (-sin(theta), cos(theta))
    # At 160 deg: sin(160)=0.342, cos(160)=-0.940
    # Tangent: (-0.342, -0.940) or (0.342, 0.940)
    # We want arrow head pointing in direction of sweep (end=155)
    
    # Let's use clean geometric polygon coordinates:
    # Arrow 1 at ~160 deg (end of bottom arc):
    # Tip at (tip1_x + 30, tip1_y + 110)
    p1_tip = (tip1_x - 40, tip1_y + 80)
    p1_a = (tip1_x - 110, tip1_y - 20)
    p1_b = (tip1_x + 50, tip1_y - 40)
    draw.polygon([p1_tip, p1_a, p1_b], fill=CYAN)
    
    # Arrow 2 at ~340 deg (end of top arc, top-right quadrant):
    rad2 = math.radians(340)
    tip2_x = center_x + radius * math.cos(rad2)
    tip2_y = center_y + radius * math.sin(rad2)
    
    p2_tip = (tip2_x + 40, tip2_y - 80)
    p2_a = (tip2_x + 110, tip2_y + 20)
    p2_b = (tip2_x - 50, tip2_y + 40)
    draw.polygon([p2_tip, p2_a, p2_b], fill=WHITE)

    # Minimalist taskbar bar at the center bottom to represent 'Taskbar':
    # Sleek pill: x from 312 to 712 (width 400), y from 472 to 552 (height 80)
    bar_w = 340
    bar_h = 56
    bar_x0 = center_x - bar_w // 2
    bar_y0 = center_y - bar_h // 2
    bar_x1 = center_x + bar_w // 2
    bar_y1 = center_y + bar_h // 2
    
    # Rounded bar representing taskbar dock
    draw.rounded_rectangle([bar_x0, bar_y0, bar_x1, bar_y1], radius=28, fill=WHITE)
    
    # 3 small accent taskbar icons inside the bar
    # Icon 1 (Start button cyan accent)
    dot1_x = center_x - 90
    draw.ellipse([dot1_x - 14, center_y - 14, dot1_x + 14, center_y + 14], fill=CYAN)
    
    # Icon 2 (Middle app)
    dot2_x = center_x
    draw.ellipse([dot2_x - 14, center_y - 14, dot2_x + 14, center_y + 14], fill=(180, 200, 220, 255))
    
    # Icon 3 (Right app)
    dot3_x = center_x + 90
    draw.ellipse([dot3_x - 14, center_y - 14, dot3_x + 14, center_y + 14], fill=(180, 200, 220, 255))

    return img

out_dir = r"C:\Users\ricry\Desktop\Scripts\Refresh Taskbar"
os.makedirs(out_dir, exist_ok=True)
highres = create_icon()
png_path = os.path.join(out_dir, "refresh.png")
highres.resize((256, 256), Image.Resampling.LANCZOS).save(png_path, "PNG")

sizes = [(16, 16), (20, 20), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
icon_images = [highres.resize(size, Image.Resampling.LANCZOS) for size in sizes]
ico_path = os.path.join(out_dir, "refresh.ico")
icon_images[-1].save(ico_path, format="ICO", sizes=sizes, append_images=icon_images[:-1])
print("Icon created successfully at:", ico_path)
