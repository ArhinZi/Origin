from PIL import Image

def alpha_bleed(image, padding):
    """
    Apply alpha bleeding to the given image with the specified padding.
    """
    # Create a new image with additional padding
    new_width = image.width + 2 * padding
    new_height = image.height + 2 * padding
    padded_image = Image.new('RGBA', (new_width, new_height), (0, 0, 0, 0))
    padded_image.paste(image, (padding, padding))

    # Apply alpha bleeding
    alpha_bled_image = Image.new('RGBA', (new_width, new_height), (0, 0, 0, 0))
    for x in range(new_width):
        for y in range(new_height):
            if padded_image.getpixel((x, y))[3] == 0:
                # Current pixel is transparent, blend it with surrounding pixels
                neighbor_pixels = [padded_image.getpixel((i, j)) for i, j in [(x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)] if 0 <= i < new_width and 0 <= j < new_height]
                if neighbor_pixels:
                    alpha_bled_image.putpixel((x, y), tuple(neighbor_pixels[0][:3] + (0,)))  # Set alpha to 0 for transparent pixels
                else:
                    alpha_bled_image.putpixel((x, y), (0, 0, 0, 0))  # Set pixel to transparent black
            else:
                # Copy non-transparent pixel from original image
                alpha_bled_image.putpixel((x, y), padded_image.getpixel((x, y)))

    # Crop the image to remove padding
    alpha_bled_image = alpha_bled_image.crop((padding, padding, new_width - padding, new_height - padding))
    
    return alpha_bled_image

# Example usage:
input_image_path = input("Enter the input image path: ")
output_image_path = input("Enter the output image path: ")
padding = int(input("Enter the padding size in pixels: "))

# Open the input image
input_image = Image.open(input_image_path).convert('RGBA')

# Apply alpha bleeding
alpha_bled_image = alpha_bleed(input_image, padding)

# Save the alpha bled image
alpha_bled_image.save(output_image_path)

print(f"Alpha bled image saved as '{output_image_path}'.")
