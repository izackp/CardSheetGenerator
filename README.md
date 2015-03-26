# CardSheetGenerator
Takes a bunch of images and creates card sheets with them. Keeping true to the dpi and card sizes.

#### Usage
Input must have 11 arguments:

1.  cardWidth (decimal; inches)
2.  cardHeight (decimal; inches)
3.  dpi (whole number)
4.  guideLineSize (decimal, inches, 0 == no guide lines)
5.  borderSize (decimal; inches)
6.  paperWidth (decimal; inches)
7.  paperHeight (decimal; inches)
8.  separatorSpace (decimal; inches)
9.  contentOffsetX (decimal; inches)
10. contentOffsetY (decimal; inches)
11. image array with images in base64 `[img1blah+blah=, img2blah+blah=, img3blah+blah=]`

Output is a json with a list of images in base64:
`
{
    "cardSheets": [
        "imgSheet1blah+blah=",
        "imgSheet2blah+blah="
    ]
}
`

#### The Process
We take your images and it:
- Trims them to remove any padding
- Scales them to fit cardWidth/cardHeight - cardBorder (using aspectFit ratio)
- Adds padding to fit cardWidth/cardHeight exactly. This creates the border.
- Adds Guidelines to the corners of the card
- Tiles the cards into 3x3 sheets (defined by paper width and height)
  - Here it is also separates the cards using Separator Space

#### Example Result
Heres an example for these settings:

1. 2.5
2. 3.5
3. 60
4. 0.05
5. 0.15
6. 8.5
7. 11
8. -0.01667 (negative separator space causes guidelines to overlap)
9. 0
10. 0
11. 9 images

![Alt text](example.png?raw=true "example cardsheet")

#### To Do
 - Add Parameters:
    - Content Center X, Y Offset, content is normally centered but we may want to offset it to account for the printer
 - Use Lanczos for resizing
 - Accept Json Input
 - Make a non-microservice version that's user friendly
 - Support manual tiling specifications, so the user can decide between 3x3 or 4x2
 - Add an auto tiling mode which will automatically try to find the best tiling method to fill the paper
