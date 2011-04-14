using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Images {
    /// <summary>
    /// This class contains basic shader code fragments that are dynamically assembled into shaders
    /// by the Effects code.
    /// </summary>
    class DynamicFx {

        /// <summary>
        /// Extract the framework for image code from image.fx, and add a 'Standard' pixel shader and technique.
        /// The %CODE% will then be dynamically substituted with appropriate PS code.
        /// The image.fx contains all the parameters, texture definitions, and vector shader routines.
        /// </summary>
        public static string Framework;
        static DynamicFx() {
                    Framework = File.OpenText(@"Content\image.fx").ReadToEnd();
                    Framework = Framework.Split(new String[] { "// END OF GENERIC CODE" }, StringSplitOptions.None)[0];
                    Framework += @"
PS_OUTPUT Standard_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT rcol;
    float4 col;
    %CODE%
    rcol.pixClr = col;
    return rcol;
}
technique Standard { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Standard_PS(); } }
       
";
                }

        /// <summary>base code for an XxY convolution.  $X$ abd $Y$ will be substituted on dynamic generation.</summary>
        public static string ImConv = @"  // property for debug change during execution
    clip(Input.TexPos); 
    clip(1-Input.TexPos);
    
    int X = $X$; 
    int Y = $Y$;
    int2 XY = int2(X,Y);
	float4 r = float4(0, 0, 0, 0);
	// float2 stepImageXy = float2(stepx, stepy);  // TODO: use stepImageXy as (only) uniform

    Input.TexPos -= .5*stepImageXy;           // offset to get exact lineup
    float2 fpos = Input.TexPos * isize;  // fpos in pixels
    int2 ipos = round(fpos);             // ipos integral pixels
    float2 rpos = fpos - ipos;           // rpos fractional pixel -0.5 .. 0.5
    float2 rrpos = -stepKernelXy*rpos;   // offset to allow for rpos in kernel lookup, -1/12 .. 1/12
    float2 rrposc = rrpos+0.5;           // additional offset to centre in 0..1
    float2 lookupbase = (ipos+0.5) * stepImageXy;  // make sure we hit the right points
 
	float d = 0;                         // cumulative convolution weights
	int2 xy = -XY;                       // x,y in integral pixels, -X .. X
    for (int i = 0; i < (2*X+1)*(2*Y+1); i++) {
		float k = tex2D(isparmbil, (xy*stepKernelXy + rrposc)).x; // even monochrome texture seen as float4
		r += k * tex2D(is, lookupbase + stepImageXy * xy);
		d += k;
		xy.x++; 
		if (xy.x > X) { xy.x = -X; xy.y++; }
	}

    col = r/d;
";

        /// <summary>
        /// base code for an XxY unsharp mask.  $X$ abd $Y$ will be substituted on dynamic generation.
        /// </summary>
        public static string Unsharp = @"  
    clip(Input.TexPos); 
    clip(1-Input.TexPos);
    // parms [1] strength, [2] threshold, [3...] convolution parameters (1d)
    
    int X = $X$; 
    int Y = $Y$;
    int2 XY = int2(X,Y);
    
    float sumk = 0;
	float suml = 0;

	int x=-X; int y=-Y;
    for (int i = 0; i < (2*X+1)*(2*Y+1); i++) {
        float4 rgb = tex2D(is, Input.TexPos + float2(x,y)*stepImageXy);
        float l = max(rgb.r, max(rgb.g, rgb.b));
        float k = parms[abs(x)+3] * parms[abs(y)+3];
		suml += k * l;
		sumk += k;
		x++;
		if (x > X) { x = -X; y++; }
	}
	float smoothl = suml / sumk;
	
    float4 myrgb = tex2D(is, Input.TexPos);
    float myl = max(myrgb.r, max(myrgb.g, myrgb.b));
    
    if (abs(smoothl - myl) < parms[2]) {
		col = myrgb;
	} else {
	    float newl = (1+parms[1])*myl - parms[1]*smoothl;	
		col = myrgb * newl / myl;
		col.a = 1;
		}
";

    }
}
