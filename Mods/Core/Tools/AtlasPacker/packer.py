from PIL import Image
import os
import json

def create_sprite_atlas(input_folder, atlas_name, w_gap=0, h_gap=0):
    # Get a list of all image files in the input folder
    image_files = [f for f in os.listdir(input_folder) if f.endswith('.png')]

    # Sort the image files based on their names
    image_files.sort()

    # Dictionary to store maximum column height for each row
    max_row_heights = {}

    # List to store sprite information
    sprites = []

    # Loop through image files to determine max_row_heights and collect sprite information
    for file in image_files:
        parts = file.split('_')
        if len(parts) != 3:
            print(f"Ignoring file '{file}' due to invalid naming convention.")
            continue
        try:
            row = int(parts[0])
            max_row_heights.setdefault(row, 0)
            image = Image.open(os.path.join(input_folder, file))
            max_row_heights[row] = max(max_row_heights[row], image.height)
            
        except (ValueError, IOError) as e:
            print(f"Ignoring file '{file}' due to error in processing: {e}")

    # Determine the number of columns
    max_cols = max(int(col.split('_')[1]) for col in image_files if len(col.split('_')) == 3 and col.split('_')[1].isdigit())

    # Calculate total width and height of the atlas
    # Calculate total width of the atlas
    total_width = 0
    for col in range(max_cols + 1):
        col_images = [f for f in image_files if f.split('_')[1] == str(col)]
        col_width = sum(Image.open(os.path.join(input_folder, img)).width for img in col_images) + w_gap * (len(col_images) - 1)
        total_width = max(total_width, col_width)
    total_height = max(sum(max_row_heights.values()) + h_gap * (len(max_row_heights) - 1), max(max_row_heights.values()))

        
    # Create a blank image for the atlas
    atlas = Image.new('RGBA', (total_width, total_height), (0, 0, 0, 0))

    # Paste images onto the atlas
    current_y = 0
    for row in sorted(max_row_heights.keys()):
        current_x = 0
        for file in image_files:
            parts = file.split('_')
            if len(parts) != 3:
                continue
            try:
                if int(parts[0]) != row:
                    continue
                image = Image.open(os.path.join(input_folder, file))
                atlas.paste(image, (current_x, current_y))
                sprite_info = {
                    "TextureName": atlas_name,
                    "ID": parts[2].split('.')[0],  # Remove file extension from ID
                    "SourceRect": f"{current_x} {current_y} {image.width} {image.height}"
                }
                sprites.append(sprite_info)
                current_x += image.width + w_gap
            except (ValueError, IOError):
                print(f"Ignoring file '{file}' due to error in processing.")
        current_y += max_row_heights[row] + h_gap

    # Save the atlas image
    atlas_filename = f"{atlas_name}.png"
    atlas.save(atlas_filename)
    print(f"Atlas image '{atlas_filename}' has been created successfully.")

    # Create a JSON object
    json_data = {"Sprites": sprites}

    # Write JSON data to file
    json_filename = f"{atlas_name}.json"
    with open(json_filename, 'w') as json_file:
        json.dump(json_data, json_file, indent=4)

    print(f"JSON file '{json_filename}' has been created successfully.")

# Ask for parameters
input_folder = input("Enter the input folder path: ")
atlas_name = input("Enter the atlas name (without extension): ")
w_gap = int(input("Enter the horizontal gap (default is 0): ") or 0)
h_gap = int(input("Enter the vertical gap (default is 0): ") or 0)

# Create sprite atlas and JSON file
create_sprite_atlas(input_folder, atlas_name, w_gap, h_gap)
