# CardSheetGenerator
Takes a bunch of images and creates card sheets with them. Keeping true to the dpi and card sizes.

#### Usage
Input must have 5 arguements:

1. cardWidth (double; inches)
2. cardHeight (double; inches)
3. dpi (int)
4. guideLineSize (double, inches)
5. image array with images in base64 `[img1blah+blah=, img2blah+blah=, img3blah+blah=]`

Output is a json with a list of images in base64:
`
{
    "cardSheets": [
        "imgSheet1blah+blah=",
        "imgSheet2blah+blah="
    ]
}
`
#### Example Result
Heres an example for these settings:

1. 2.5
2. 3.5
3. 60
4. 0.05
5. 9 images

![Alt text](example.png?raw=true "example cardsheet")

#### To Do
 - Add Parameters:
    - Paper Width Height, so we can generate correctly sized sheets
    - Content Center X, Y Offset, content is normally centered but we may want to offset it to account for the printer
    - Separator Space, to add space inbetween the cards
    - Card Border Space
 - Use Lanczos for resizing 
 - Accept Json Input
 - Make a non-microservice version that's user friendly
