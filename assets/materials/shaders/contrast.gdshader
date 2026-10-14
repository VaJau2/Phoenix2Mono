shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

mat4 brightnessMatrix( float brightness )
{
    return mat4( vec4(1, 0, 0, 0),
                 vec4(0, 1, 0, 0),
                 vec4(0, 0, 1, 0),
                 vec4(brightness, brightness, brightness, 1));
}

mat4 contrastMatrix( float contrast )
{
	float t = ( 1.0 - contrast ) / 2.0;
    
    return mat4( vec4(contrast, 0, 0, 0),
                 vec4(0, contrast, 0, 0),
                 vec4(0, 0, contrast, 0),
                 vec4(t, t, t, 1) );

}

mat4 saturationMatrix( float saturation )
{
    vec3 luminance = vec3( 0.3086, 0.6094, 0.0820 );
    
    float oneMinusSat = 1.0 - saturation;
    
    vec3 red = vec3( luminance.x * oneMinusSat );
    red+= vec3( saturation, 0, 0 );
    
    vec3 green = vec3( luminance.y * oneMinusSat );
    green += vec3( 0, saturation, 0 );
    
    vec3 blue = vec3( luminance.z * oneMinusSat );
    blue += vec3( 0, 0, saturation );
    
    return mat4( vec4(red,     0),
                 vec4(green,   0),
                 vec4(blue,    0),
                 vec4(0, 0, 0, 1));
}

uniform float brightness: hint_range(0, 1);
uniform float contrast: hint_range(0, 1);
uniform float saturation: hint_range(0, 1);

void fragment()
{
	COLOR = texture(SCREEN_TEXTURE, SCREEN_UV);
	
	COLOR = brightnessMatrix( brightness ) *
        	contrastMatrix( contrast ) * 
        	saturationMatrix( saturation ) *
        	COLOR;
}