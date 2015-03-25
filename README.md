# CardSheetGenerator
Takes a bunch of images and creates card sheets with them. Keeping true to the dpi and card sizes.

#### Usage
Input must have 5 arguements:

1 - cardWidth (double; inches)
2 - cardHeight (double; inches)
3 - dpi (int)
4 - guideLineSize (double, inches)
5 - image array with images in base64 `[img1blah+blah=, img2blah+blah=, img3blah+blah=]`

Output is a json with a list of images in base64:
`
{
    "cardSheets": [
        "imgSheet1blah+blah=",
        "imgSheet2blah+blah="
    ]
}
`

#### To Do
 - Add Paper Width + Height parameters, so we can generate correctly sized sheets
 - Add Content Center X + Y parameters, content is normally centered but we may want to offset it to account for the printer
 - Add Card Border parameters
 - Accept Json Input
 - Make a non-microservice version that's user friendly
