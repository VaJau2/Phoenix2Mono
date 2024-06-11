shader_type canvas_item;

uniform float fisheye_strength = -0.05; // Control the fisheye effect strength
uniform float alpha = 0.0;

uniform vec2 r_displacement = vec2(3.0, 0.0);
uniform vec2 g_displacement = vec2(0.0, 0.0);
uniform vec2 b_displacement = vec2(-3.0, 0.0);

vec2 fisheye(vec2 uv) {
	vec2 d = uv - 0.51;
	float r = length(d);
	float theta = atan(d.y, d.x);
	float anim_value = sin(TIME * 1.15) * 0.03 + 1.0;
	float rf = pow(r, fisheye_strength) / pow(0.5, fisheye_strength - 1.0) * anim_value;
	return vec2(0.5) + rf * normalize(d);
}

void fragment() {
	vec2 iResolution = 1.0 / SCREEN_PIXEL_SIZE;
	vec2 q = FRAGCOORD.xy / iResolution.xy;
	
	// Apply fisheye distortion
	q = fisheye(q);
	
	//vec3 col = texture(SCREEN_TEXTURE, q).rgb;
	
	float r = texture(SCREEN_TEXTURE, q + vec2(SCREEN_PIXEL_SIZE*r_displacement), 0.0).r;
	float g = texture(SCREEN_TEXTURE, q + vec2(SCREEN_PIXEL_SIZE*g_displacement), 0.0).g;
	float b = texture(SCREEN_TEXTURE, q + vec2(SCREEN_PIXEL_SIZE*b_displacement), 0.0).b;
	
	COLOR = vec4(r, g, b, alpha);
} 
