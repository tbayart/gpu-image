//  "D:\Program Files\Microsoft DirectX SDK (February 2010)\Utilities\bin\x86\fxc.exe" /T:fx_5_0  /Gec /Fo image.fxo image.fx

uniform extern texture image;		// texture for image1
uniform extern texture image2;		// texture for image2
uniform extern texture image3;		// texture for image3
uniform extern texture image4;		// texture for image4
uniform extern texture imageparm;	// texture for convolution image
uniform extern float2 isize;		// used by dynamic ImConv
uniform extern float4x4 TexProj : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj2 : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj3 : MODELVIEWPROJECTION;
uniform extern float4x4 TexProj4 : MODELVIEWPROJECTION;
uniform extern float2 stepImageXy;
uniform extern float2 stepKernelXy; // == 0.5/XY; == 1/6


//const int PARMS = 100;
uniform extern float parms[100];

struct VS_INPUT   // all in Model space, without colour
{
    float3 modelPosition : POSITION; 
};

// standard structure for vs output, ps input
struct VS_OUTPUT_Image
{
    float4 Position : POSITION;
    float2 TexPos : TEXCOORD0;  // needed because hlsl rules do not let POSITION be read in pixel shader
};

// two transform structure for output, ps input
struct VS_OUTPUT_Image2
{
    float4 Position : POSITION;
    float2 TexPos : TEXCOORD0;  // needed because hlsl rules do not let POSITION be read in pixel shader
    float2 TexPos2 : TEXCOORD1;  // needed because hlsl rules do not let POSITION be read in pixel shader
};


struct PS_OUTPUT
{
    float4 pixClr : COLOR;
};

// point samplers for use in 'normal' image processing
sampler is = sampler_state {
    SRGBTexture = 1; Texture = <image>;
    mipfilter = POINT; minfilter = POINT; magfilter = POINT;
    AddressU = CLAMP; AddressV = CLAMP;
};
sampler is2 = sampler_state {
    SRGBTexture = 1; Texture = <image2>;
    mipfilter = POINT; minfilter = POINT; magfilter = POINT;
    AddressU = CLAMP; AddressV = CLAMP;
};
sampler is3 = sampler_state {
    SRGBTexture = 1; Texture = <image3>;
    mipfilter = POINT; minfilter = POINT; magfilter = POINT;
    AddressU = CLAMP; AddressV = CLAMP;
};
sampler is4 = sampler_state {
    SRGBTexture = 1; Texture = <image4>;
    mipfilter = POINT; minfilter = POINT; magfilter = POINT;
    AddressU = CLAMP; AddressV = CLAMP;
};

// linear sampler for display
sampler isbil = sampler_state {
    SRGBTexture = 1; Texture = <image>;
    mipfilter = LINEAR; minfilter = LINEAR; magfilter = LINEAR;
    AddressU = CLAMP; AddressV = CLAMP;
};
sampler isbil2 = sampler_state {
    SRGBTexture = 1; Texture = <image2>;
    mipfilter = LINEAR; minfilter = LINEAR; magfilter = LINEAR;
    AddressU = CLAMP; AddressV = CLAMP;
};
sampler isparmbil = sampler_state {
    SRGBTexture = 0; Texture = <imageparm>;
    mipfilter = LINEAR; minfilter = LINEAR; magfilter = LINEAR;
    AddressU = CLAMP; AddressV = CLAMP;
};



VS_OUTPUT_Image Image_VS(VS_INPUT Input)
{
    VS_OUTPUT_Image Output;
    Output.Position  = float4( Input.modelPosition, 1 );
float4 tp = mul(Output.Position, TexProj);
//tp.z = 1;
Output.TexPos = tp.xy / tp.w;
//    Output.TexPos = mul(Output.Position, TexProj).xy;
    return Output;
}

VS_OUTPUT_Image2 Image2_VS(VS_INPUT Input)
{
    VS_OUTPUT_Image2 Output;
    Output.Position  = float4( Input.modelPosition, 1 );
    Output.TexPos = mul(Output.Position, TexProj).xy;
    Output.TexPos2 = mul(Output.Position, TexProj2).xy;
    return Output;
}
// END OF GENERIC CODE

//// GreyMin: convert to grey using the min of the rgb components ~ no parameters
PS_OUTPUT GreyMin_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 v = tex2D(is, Input.TexPos);
    float MIN = min(min(v.r, v.g), v.b);
    col.pixClr = float4(MIN,MIN,MIN,1);
    return col;
	}
technique GreyMin { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
    PixelShader = compile ps_3_0 GreyMin_PS(); } }

//// GreyMax: convert to grey using the max of the rgb components ~ no parameters
PS_OUTPUT GreyMax_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 v = tex2D(is, Input.TexPos);
    float MAX = max(max(v.r, v.g), v.b);
    col.pixClr = float4(MAX,MAX,MAX,1);
    return col;
	}
technique GreyMax { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
    PixelShader = compile ps_3_0 GreyMax_PS(); } }

//// GreyMed: convert to grey using the median of the rgb components ~ no parameters
PS_OUTPUT GreyMed_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 v = tex2D(is, Input.TexPos);
    float MIN = min(min(v.r, v.g), v.b);
    float MAX = max(max(v.r, v.g), v.b);
    float MED = v.r + v.g + v.b - MIN - MAX;
    col.pixClr = float4(MED,MED,MED,1);
    return col;
	}
technique GreyMed { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
    PixelShader = compile ps_3_0 GreyMed_PS(); } }


// median implemented from FilterMeister sample
//// Median33:  fixed 3x3 median: computes a separate median in each of RGB (and A)
PS_OUTPUT Median33_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 v1 = tex2D(is, Input.TexPos + float2(-stepImageXy.x,-stepImageXy.y));
    float4 v2 = tex2D(is, Input.TexPos + float2(     0,-stepImageXy.y));
    float4 v3 = tex2D(is, Input.TexPos + float2(+stepImageXy.x,-stepImageXy.y));
    float4 v4 = tex2D(is, Input.TexPos + float2(-stepImageXy.x,     0));
    float4 v5 = tex2D(is, Input.TexPos + float2(     0,     0));
    float4 v6 = tex2D(is, Input.TexPos + float2(+stepImageXy.x,     0));
    float4 v7 = tex2D(is, Input.TexPos + float2(-stepImageXy.x, stepImageXy.y));
    float4 v8 = tex2D(is, Input.TexPos + float2(     0, stepImageXy.y));
    float4 v9 = tex2D(is, Input.TexPos + float2(-stepImageXy.x, stepImageXy.y));
	
	// this code derived from FilterMeister documentation,
	// in turn derived from 
	// Sort the cell values with the high-speed sort method by John L. Smith
	// http://www.eso.org/~ndevilla/median/ .. now  http://ndevilla.free.fr/median/
	float4 v10 = (max(max(min((v1),min((v2),(v3))),
		min((v4),min((v5),(v6)))),min((v7),min((v8),(v9)))));

	float4 v11 = (min(min(max(max((v8),(v9)),max((v7),min((v8),(v9)))),
		max(max((v5),(v6)),max((v4),min((v5),(v6))))),
		max(max((v2),(v3)),max((v1),min((v2),(v3))))));

	float4 v12 = (min(max(min(max((v8),(v9)),max((v7),min((v8),(v9)))),
		min( min(max((v5),(v6)),max((v4),min((v5),(v6)))),
		min(max((v2),(v3)),max((v1),min((v2),(v3)))) )),
		max( min(max((v5),(v6)),max((v4),min((v5),(v6)))),
		min(max((v2),(v3)),max((v1),min((v2),(v3)))) )));
		
    col.pixClr = v12; // tex2D(is, Input.TexPos);
    return col;
	}
technique Median33 { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
    PixelShader = compile ps_3_0 Median33_PS(); } }


//// LMedian3: computes a fixed 1d 3 point median.  parms 1 and 2 are x-step and y-step (in input pixels)
PS_OUTPUT LMedian3_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 a = tex2D(is, Input.TexPos - float2( parms[1], parms[2]) * stepImageXy);
    float4 b = tex2D(is, Input.TexPos);
    float4 c = tex2D(is, Input.TexPos + float2( parms[1], parms[2]) * stepImageXy);
	
	/* funny median processing */
	float4 mx = max(max(a,b),c);  // max value
	float4 mn = min(min(a,b),c);  // min value
	float4 mm = a+b+c-mx-mn;      // median value
    float4 d1 = mx-mm;            // delta above
    float4 d2 = mm-mn;            // delta below
    float4 dd = min(d1,d2);       // smaller delta
	float4 m = mm + dd * parms[3];    // move scaled by smaller delta in direction/proportion indicated                
		
    col.pixClr = m; // tex2D(is, Input.TexPos);
    return col;
	}
technique LMedian3 {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 LMedian3_PS(); } }


//// Avg3: performs 3x3 convolution (average).  parms 1..9 are convolution parameters
PS_OUTPUT Avg3_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    float4 v1 = tex2D(is, Input.TexPos + float2(-stepImageXy.x,-stepImageXy.y));
    float4 v2 = tex2D(is, Input.TexPos + float2(             0,-stepImageXy.y));
    float4 v3 = tex2D(is, Input.TexPos + float2(+stepImageXy.x,-stepImageXy.y));
    float4 v4 = tex2D(is, Input.TexPos + float2(-stepImageXy.x,             0));
    float4 v5 = tex2D(is, Input.TexPos + float2(             0,             0));
    float4 v6 = tex2D(is, Input.TexPos + float2(+stepImageXy.x,             0));
    float4 v7 = tex2D(is, Input.TexPos + float2(-stepImageXy.x, stepImageXy.y));
    float4 v8 = tex2D(is, Input.TexPos + float2(             0, stepImageXy.y));
    float4 v9 = tex2D(is, Input.TexPos + float2(-stepImageXy.x, stepImageXy.y));
	float4 r = parms[1]*v1 + parms[2]*v2 + parms[3]*v3 + parms[4]*v4 + parms[5]*v5 + parms[6]*v6
				 + parms[7]*v7 + parms[8]*v8 + parms[9]*v9;
    col.pixClr = r;
    return col;
}
technique Avg3 {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Avg3_PS(); } }


//// Conv: general convolution.  parms 1 and 2 are width and height (2*width+1 x 2*height+1), parm3 is divisor, then (2*width+1 x 2*height+1) convolution parameters
PS_OUTPUT Conv_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    //clip(Input.TexPos);
    //clip(1-Input.TexPos);
    
    int X = parms[1]; 
    int Y = parms[2];
    float d = parms[3];
	float4 r = float4(0, 0, 0, 0);

	int x=-X; int y=-Y;
    for (int i = 0; i < (2*X+1)*(2*Y+1); i++) {
		r += parms[i+4] * tex2D(is, Input.TexPos + float2(x,y)*stepImageXy);
		x++;
		if (x > X) { x = -X; y++; }
	}

/*
    int i = 4;
	for (int x=-X; x<=X; x++) for (int y=-Y; y<=Y; y++) {
		r += parms[i] * tex2D(is, Input.TexPos + float2(x,y)*stepImageXy);
		i++;
	}
*/	
	
	
    col.pixClr = r/d;
    return col;
}
technique Conv {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Conv_PS(); } }


//// Changes: counts changes along a scanline.  parms[1] is value change threshold, parms[2] is divisor. Fixed 128 pixel spread
PS_OUTPUT Changes_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    //clip(Input.TexPos);
    //clip(1-Input.TexPos);
	float d = parms[2];
	float p=0, n=0;
	int X = 128; // min(500, parms[1]); 

	float4 v = tex2D(is, Input.TexPos + float2(-X/2,0)*stepImageXy); 
	float last = max(max(v.r, v.g), v.b); 
	for (int x = -X/2; x <= X/2; x++) {
		v = tex2D(is, Input.TexPos + float2(x,0)*stepImageXy);
		float MAX = max(max(v.r, v.g), v.b);
		p += MAX-last > parms[1] ? 1 : 0;
		n += last-MAX > parms[1] ? 1 : 0;
		last = MAX;
	}
	float pn = p+n;
    col.pixClr = float4(pn,pn,pn,d)/d;
    return col;
}
technique Changes {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Changes_PS(); } }

//// Direct: does a direct conversion from input to output; using closest point lookup
PS_OUTPUT Direct_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    float4 v00 = tex2D(is, Input.TexPos);
    col.pixClr = v00; // tex2D(is, Input.TexPos);
    return col;
}
technique Direct {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Direct_PS(); } }

//// Tonemap: tonemaps the main image, using image2 as curve.  Only the r component and centre row of image2 are used
PS_OUTPUT Tonemap_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    float4 v = tex2D(is, Input.TexPos);
    float MAX = max(max(v.r, v.g), v.b);
    float newmax = tex2D(isbil2, float2(MAX,0.5)).r;  
    col.pixClr = v * (newmax/MAX); 
    col.pixClr.a = 1; 
    return col;
}
technique Tonemap {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Tonemap_PS(); } }

//// lt: compares to threshold
PS_OUTPUT lt_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    float4 v00 = tex2D(is, Input.TexPos);
    col.pixClr = v00.r < parms[1] ? 1 : 0; // tex2D(is, Input.TexPos);
    return col;
}
technique lt {  pass P0  { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 lt_PS(); } }



//// Bilinear: does a direct conversion from input to output; using standard bilinear lookup
PS_OUTPUT Bilinear_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    col.pixClr = tex2D(isbil, Input.TexPos);
    return col;
}
technique Bilinear { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Bilinear_PS(); } }


//// Plus: adds two images
PS_OUTPUT Plus_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = tex2D(is, Input.TexPos) + tex2D(is2, Input.TexPos);
    return col;
}
technique Plus  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Plus_PS(); } }

//// Minus: subtracts image 2 from image 1
PS_OUTPUT Minus_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = tex2D(is, Input.TexPos) - tex2D(is2, Input.TexPos);
    return col;
}
technique Minus  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Minus_PS(); } }


//// Times: multiplies two images
PS_OUTPUT Times_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = tex2D(is, Input.TexPos) * tex2D(is2, Input.TexPos);
    return col;
}
technique Times  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Times_PS(); } }


//// Divide: divides image1 by image2
PS_OUTPUT Divide_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = tex2D(is, Input.TexPos) / tex2D(is2, Input.TexPos);
    return col;
}
technique Divide  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Divide_PS(); } }


//// Min: takes minimum of two images (in each of rgb)
PS_OUTPUT Min_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = min(tex2D(is, Input.TexPos), tex2D(is2, Input.TexPos));
    return col;
}
technique Min  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Min_PS(); } }

//// Max: takes maximum of two images (in each of rgb)
PS_OUTPUT Max_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = max(tex2D(is, Input.TexPos), tex2D(is2, Input.TexPos));
    return col;
}
technique Max  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Max_PS(); } }


//// Mad: multiply and add.  Takes four images and parms 1..4 are four scalar multipliers
PS_OUTPUT Mad_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = parms[1]*tex2D(is, Input.TexPos)
        + parms[2]*tex2D(is2, Input.TexPos)
        + parms[3]*tex2D(is3, Input.TexPos)
        + parms[4]*tex2D(is4, Input.TexPos);
    return col;
}
technique Mad  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Mad_PS(); } }


//// TR2: multiply and add two images, with different input transforms (mainly for performance comparison)
PS_OUTPUT TR2_PS(VS_OUTPUT_Image2 Input)
{
    PS_OUTPUT col;
    col.pixClr = parms[1]*tex2D(is, Input.TexPos)
        + parms[2]*tex2D(is2, Input.TexPos2);
    return col;
}
technique TR2  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image2_VS();
        PixelShader = compile ps_3_0 TR2_PS(); } }


//// Col: set colour to given four parms: mainly for performance comparison
PS_OUTPUT Col_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = float4(parms[1], parms[2], parms[3], parms[4]);
    return col;
}
technique Col  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 Col_PS(); } }


/******************************************************/
// standard  ps input
struct INPUT
{
    VS_OUTPUT_Image2 v;
    int ipos;   //interpreter position
};

struct PS_OUTPUT_II
{
    PS_OUTPUT v : COLOR;
    int ipos;  //interpreter position
};

/***********************************************************8
// test interpreter
PS_OUTPUT II_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    float4 stack[16];
    int sp = 0;  // stack pos
    int pp = 1;  // program pos
    float4 c = float4(0,0,0,1);
    // switch does not compile
    int L = parms[0];
    while (pp < L) {
		float k = parms[pp];
		if (k==1) c += float4(0.1,0,0,0);
		if (k==2) c += float4(0,0.1,0,0);
		// if (k==3) c += float4(0,0,0.1,0);  // too much, just gets it all wrong ...
		// if (k==4) c += float4(1,1,1,0);
		if (k > 3) c += float4(0.3,0.3,0.3,0);
/**		
		c += 
			k == 1 ? float4(0.1,0,0,0) :
			(k == 2 ? float4(0,0.1,0,0) :
//			k == 3 ? float4(0,0,0.1,0) :
//			k == 4 ? float4(1,1,1,0) :
			// k == 4 ? II_PS(Input).pixClr : // no recursion
			float4(0,0,0,1));
** /			
			
		// c += float4(0.1,0,0,0);	
		pp++;	 // end for while
    }
    c.a = 1;
    col.pixClr = c;
    return col;
}
technique II  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 II_PS(); } }


        


/************** template
~~~~~~~~~~~~~~ template
PS_OUTPUT _PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    col.pixClr = 
    return col;
}
technique  { pass P0 { SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
        PixelShader = compile ps_3_0 _PS(); } }
 
 *******/       
        
/****************************************************** missing support for writable arrays *
float4 kth_smallest(float4 a[81], int n, int k)
	{
		int i,j,l,m ;
		float x ;

		l=0 ; m=n-1 ;
		while (l<m) {
			x=a[k].g ;
			i=l ;
			j=m ;
			do {
				while (a[i].g<x) i++ ;
				while (x<a[j].g) j-- ;
				if (i<=j) {
					float4 t=a[i];a[i]=a[j];a[j]=t; 
					// ELEM_SWAP(a[i],a[j]) ;
					i++ ; j-- ;
				}
			} while (i<=j) ;
			if (j<k) l=i ;
			if (k<i) m=j ;
		}
		return a[k] ;
}


PS_OUTPUT GMedian_PS(VS_OUTPUT_Image Input)
{
    PS_OUTPUT col;
    clip(Input.TexPos);
    clip(1-Input.TexPos);
    
    int x; int y; int X = parms[1]; int Y = parms[2];
    float4 v[81];
    int i = 0;
    for (y=-Y; y<=Y; y++) {
	    for (x=-X; x<=X; x++) {
	        // v[i] = tex2D(is, Input.TexPos - stepImageXy * float2(x,y)); // <<< FAILS under ps_3
	        i++;
	    }
    }
	float4 m = kth_smallest(v, i, parms[3]);    
		
    col.pixClr = m; // tex2D(is, Input.TexPos);
    return col;
}
	
technique GMedian { pass P0 {
    SRGBWriteEnable = 1; VertexShader = compile vs_3_0 Image_VS();
    PixelShader = compile ps_3_0 GMedian_PS(); } }
        

/****************************************************** missing support for writable arrays */
	
