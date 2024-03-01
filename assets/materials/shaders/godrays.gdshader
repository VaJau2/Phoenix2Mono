shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

float rayStrength(vec2 raySource, vec2 rayRefDirection, vec2 coord, float seedA, float seedB, float speed, float time, vec2 iResolution)
{
	vec2 sourceToCoord = coord - raySource;
	float cosAngle = dot(normalize(sourceToCoord), rayRefDirection);
	
	return clamp(
		(0.45 + 0.15 * sin(cosAngle * seedA + time * speed)) +
		(0.3 + 0.2 * cos(-cosAngle * seedB + time * speed)),
		0.0, 1.0) *
		clamp((iResolution.x - length(sourceToCoord)) / iResolution.x, 0.5, 1.0);
}

void fragment()
{
	vec2 iResolution = 1.0 / SCREEN_PIXEL_SIZE;
	
	vec2 coord = vec2(FRAGCOORD.x, iResolution.y - FRAGCOORD.y);
	
	// Set the parameters of the sun rays
	vec2 rayPos1 = vec2(iResolution.x * 0.0, iResolution.y * -0.4);
	vec2 rayRefDir1 = normalize(vec2(1, -0.5));
	float raySeedA1 = 13.2214;
	float raySeedB1 = 5.11349;
	float raySpeed1 = 0.3;
	
	// Calculate the colour of the sun rays on the current fragment
	vec4 rays1 =
		vec4(1.0, 1.0, 1.0, 1.0) *
		rayStrength(rayPos1, rayRefDir1, coord, raySeedA1, raySeedB1, raySpeed1, TIME, iResolution);
	
	COLOR = rays1 * 0.5;
	
	// Attenuate brightness towards the bottom, simulating light-loss due to depth.
	// Give the whole thing a blue-green tinge as well.
	float brightness = (1.0 - (coord.y / iResolution.y)) * 1.0;
	COLOR.x *= 1.0 + (brightness);
	COLOR.y *= 1.0 + (brightness);
	COLOR.z *= 1.0 + (brightness);
	
	vec2 uv2 = FRAGCOORD.xy / iResolution.xy;  
	vec4 texColor = texture(SCREEN_TEXTURE, uv2);
	
	COLOR *= texColor * 3.0f;
}