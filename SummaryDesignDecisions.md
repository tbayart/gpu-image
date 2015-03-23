## Introduction ##

The basic method uses graphics textures to hold images, and effects to process these images.  The images seen to the programmer as ImageTex objects, which wrap the textures and provide image processing operations such as arithmetic operators (+, `*`, etc), filters (convolutions, unsharp mask, median, percentile, etc).  The programmer need not be aware of the GPU implementation of the operations.

The project may be used in three ways.
  1. As an image viewer, with very fast and high quality zoom/pan.
  1. For simple image processing pipeline experimentation.  A very basic programmer oriented interface is provided for this.
  1. As a set of classes for implementing image processing within a DotNet framework.


## Details ##

### sRGB ###
gpu-image is aware that the numbers stored in typical images (such as jpeg photos) do not relate to linear rgb values.  Simple image operations (such as average) that ignore this get the wrong answer, as can often be seen in more basic image processing programs.  Images are assumed to be stored and displayed in sRGB space; at least as far as colour intensities, the system is not currently aware of the exact gamut of rgb positions in colour space, but are processed in linear space.  This is primarily handled by using **SRGBTexture = 1** when loading images, and **SRGBWriteEnable = 1** when writing them.  Thus external images are in sRGB space, but internal handling is in a linear space.

We generally use intermediate formats as 8bits per colour.  There is an option to use 32 bit floating point, but this needs further optimization.  8bits per colour is not ideal, but are adequate for many purposes as long as the 256 levels relate correctly to sRGB values, and so are reasonably spread in perceptual space.